using System.Diagnostics;
using System.Text;
using Bitwarden_Backup.Extensions;
using Bitwarden_Backup.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Spectre.Console;

namespace Bitwarden_Backup.Services
{
    internal class BitwardenService(ILogger<BitwardenService> logger, IConfiguration configuration)
        : IBitwardenService
    {
        private string sessionKey = string.Empty;
        private string bitwardenExecutableName = "bw";

        private readonly BitwardenConfiguration bitwardenConfiguration =
            configuration.GetSection(BitwardenConfiguration.Key).Get<BitwardenConfiguration>()
            ?? new BitwardenConfiguration();
        private readonly ExportFileProperty exportFileProperty =
            configuration.GetSection(ExportFileProperty.Key).Get<ExportFileProperty>()
            ?? new ExportFileProperty();

        public async Task<BitwardenConfiguration> GetBitwardenConfiguration(
            CancellationToken cancellationToken
        )
        {
            var bitwardenCredentials =
                configuration.GetSection(BitwardenCredentials.Key).Get<BitwardenCredentials>()
                ?? new BitwardenCredentials();

            // Set log in method to avoid prompt if at least one required value is in appsettings
            bitwardenConfiguration.SetLogInMethod(bitwardenCredentials);

            if (bitwardenConfiguration.LogInMethod == LogInMethod.None)
            {
                logger.LogDebug("Getting log in method using Spectre.Console.");

                bitwardenConfiguration.LogInMethod = await new SelectionPrompt<LogInMethod>()
                    .Title(Prompts.LoginMethod)
                    .PageSize(5)
                    .MoreChoicesText(Texts.MoreChoices)
                    .AddChoices([LogInMethod.ApiKey, LogInMethod.EmailPw, LogInMethod.None])
                    .UseConverter(
                        logInMethod =>
                            logInMethod switch
                            {
                                LogInMethod.ApiKey => "Using Api Key",
                                LogInMethod.EmailPw => "Using Email and Password",
                                LogInMethod.None => "Cancel",
                                _
                                    => throw new NotImplementedException(
                                        ErrorMessages.InvalidLogInMethod
                                    )
                            }
                    )
                    .ShowAsync(AnsiConsole.Console, cancellationToken);
            }

            return bitwardenConfiguration;
        }

        public async Task<BitwardenResponse> SetBitwardenServer(CancellationToken cancellationToken)
        {
            if (!string.IsNullOrEmpty(bitwardenConfiguration.Url))
            {
                logger.LogDebug("Saving Bitwarden Server config.");
            }

            return await RunBitwardenCommand(
                $"config server {bitwardenConfiguration.Url} --response",
                string.Empty,
                null,
                cancellationToken
            );
        }

        public async Task<BitwardenResponse> LogIn(
            EmailPasswordCredential credential,
            CancellationToken cancellationToken
        )
        {
            logger.LogDebug("Sanity log out.");
            await LogOut(cancellationToken);

            // Set client secret regardless of value in case additional auth is required
            Environment.SetEnvironmentVariable("BW_CLIENTSECRET", credential.ClientSecret);

            var inputs = new List<StandardInput>()
            {
                new()
                {
                    Prompt = Prompts.MasterPassword,
                    Validator = SpectreConsoleExtension.StringLengthValidator,
                    ValidationResultErrorMessage = ErrorMessages.MasterPasswordValidationResult,
                    Value = credential.MasterPassword,
                    IsSecret = true,
                    InputMask = null
                }
            };

            var authenticationMethod = credential.TwoFactorMethod switch
            {
                TwoFactorMethod.Authenticator
                or TwoFactorMethod.YubiKey
                    => $" --method {(int)credential.TwoFactorMethod} --code {credential.TwoFactorCode}",
                TwoFactorMethod.Email => $" --method {(int)credential.TwoFactorMethod}",
                _ => string.Empty,
            };

            if (credential.TwoFactorMethod.Equals(TwoFactorMethod.Email))
            {
                inputs.Add(
                    new()
                    {
                        Prompt = Prompts.TwoFactorCode,
                        ValidationResultErrorMessage = ErrorMessages.TwoFactorCodeValidationResult,
                        Value = credential.TwoFactorCode
                    }
                );
            }

            logger.LogDebug("Running login command with email address and master password.");
            var bitwardenLogInResponse = await RunBitwardenCommand(
                $"login {credential.Email}{authenticationMethod} --response",
                string.Empty,
                inputs,
                cancellationToken
            );

            Environment.SetEnvironmentVariable("BW_CLIENTSECRET", null);

            if (bitwardenLogInResponse.Success && bitwardenLogInResponse.Data is not null)
            {
                logger.LogDebug("Setting session key.");
                sessionKey = bitwardenLogInResponse.Data.Raw;
            }

            return bitwardenLogInResponse;
        }

        public async Task<BitwardenResponse> LogIn(
            ApiKeyCredential credential,
            CancellationToken cancellationToken
        )
        {
            logger.LogDebug("Sanity log out.");
            await LogOut(cancellationToken);

            Environment.SetEnvironmentVariable("BW_CLIENTID", credential.ClientId);
            Environment.SetEnvironmentVariable("BW_CLIENTSECRET", credential.ClientSecret);

            if (!string.IsNullOrEmpty(bitwardenConfiguration.Url))
            {
                logger.LogDebug("Saving Bitwarden Server config.");
                var bitwardenConfigResponse = await RunBitwardenCommand(
                    $"config server {bitwardenConfiguration.Url} --response",
                    string.Empty,
                    null,
                    cancellationToken
                );

                if (!bitwardenConfigResponse.Success)
                {
                    return bitwardenConfigResponse;
                }

                logger.LogInformation("Set config server to {url}", bitwardenConfiguration.Url);
                logger.LogDebug("Bitwarden config response: \n{response}", bitwardenConfigResponse);
            }

            logger.LogDebug("Running login command with api key.");
            var bitwardenLogInResponse = await RunBitwardenCommand(
                "login --apikey --response",
                string.Empty,
                null,
                cancellationToken
            );

            if (!bitwardenLogInResponse.Success)
            {
                return bitwardenLogInResponse;
            }

            logger.LogDebug("Running unlock command.");
            var bitwardenUnlockResponse = await RunBitwardenCommand(
                $"unlock --response",
                string.Empty,
                [
                    new()
                    {
                        Prompt = Prompts.MasterPassword,
                        Validator = SpectreConsoleExtension.StringLengthValidator,
                        ValidationResultErrorMessage = ErrorMessages.MasterPasswordValidationResult,
                        Value = credential.MasterPassword,
                        IsSecret = true,
                        InputMask = null
                    }
                ],
                cancellationToken
            );

            Environment.SetEnvironmentVariable("BW_CLIENTID", null);
            Environment.SetEnvironmentVariable("BW_CLIENTSECRET", null);

            if (
                !bitwardenUnlockResponse.Success
                && !bitwardenUnlockResponse.Message.StartsWith(ErrorMessages.AlreadyLoggedIn)
            )
            {
                return bitwardenUnlockResponse;
            }

            if (bitwardenUnlockResponse.Data is not null)
            {
                logger.LogDebug("Setting session key.");
                sessionKey = bitwardenUnlockResponse.Data.Raw;
            }

            return bitwardenUnlockResponse;
        }

        public async Task<BitwardenResponse> LogOut(
            CancellationToken cancellationToken = default
        ) => await RunBitwardenCommand("logout --response", string.Empty, null, cancellationToken);

        public async Task<BitwardenResponse> ExportVault(CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(sessionKey))
            {
                return new BitwardenResponse()
                {
                    Success = false,
                    Message = "The session key cannot be null or empty."
                };
            }

            var filePath = FilePathExtension.GetFilePath(
                exportFileProperty.Path,
                exportFileProperty.ExportFormat,
                "bw_export",
                exportFileProperty.DateInFileNameFormat
            );

            var command =
                $"export --session \"{sessionKey}\" --format {exportFileProperty.ExportFormat} --output {filePath} --response";

            var additionalCommand = string.Empty;

            if (
                !string.IsNullOrWhiteSpace(exportFileProperty.CustomExportPassword)
                && exportFileProperty.ExportFormat.Equals(ExportFormat.encrypted_json)
            )
            {
                additionalCommand = $" --password {exportFileProperty.CustomExportPassword}";
            }

            logger.LogDebug("Running export vault command.");
            return await RunBitwardenCommand(command, additionalCommand, null, cancellationToken);
        }

        public async Task<BitwardenResponse> RunBitwardenCommand(
            string command,
            string additionalCommands = "",
            List<StandardInput>? inputs = null,
            CancellationToken cancellationToken = default
        )
        {
            var output = new StringBuilder();
            var error = new StringBuilder();
            var tempCommand = new StringBuilder();
            var isProcessStarted = false;

            tempCommand.Append(command);

            if (
                command.Contains($"--format {ExportFormat.encrypted_json}")
                && !string.IsNullOrWhiteSpace(additionalCommands)
            )
            {
                tempCommand.Append(' ');
                tempCommand.Append(additionalCommands);
            }

            logger.LogDebug("Getting Bitwarden path and creating process.");
            var process = new Process()
            {
                StartInfo = new ProcessStartInfo()
                {
                    FileName = GetBitwardenPath(),
                    Arguments = tempCommand.ToString(),
                    RedirectStandardOutput = true,
                    RedirectStandardInput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                },
            };

            process.OutputDataReceived += (sender, args) =>
            {
                if (!string.IsNullOrWhiteSpace(args.Data))
                {
                    output.AppendLine(args.Data);
                }
            };

            _ = cancellationToken.Register(() =>
            {
                if (!isProcessStarted && !process.HasExited)
                {
                    logger.LogDebug("Killing process.");
                    process.Kill();
                }
            });

            logger.LogDebug("Starting command '{command}'.", command);
            isProcessStarted = process.Start();
            process.BeginOutputReadLine();

            var inputIndex = 0;
            var inputCount = inputs?.Count ?? 0;

            logger.LogDebug("Starting user input loop with inputCount: {inputCount}.", inputCount);
            while (!process.HasExited && inputIndex < inputCount)
            {
                var currentInput = inputs![inputIndex];
                logger.LogDebug(
                    "Index {index} with prompt '{prompt}'.",
                    inputIndex,
                    currentInput.Prompt
                );
                var userInput = await SpectreConsoleExtension.GetStringInputWithConsole(
                    currentInput.Value,
                    currentInput.Prompt,
                    currentInput.Validator,
                    currentInput.ValidationResultErrorMessage,
                    currentInput.IsSecret,
                    currentInput.InputMask,
                    cancellationToken
                );
                inputIndex++;
                process.StandardInput.WriteLine(userInput);
            }

            var processExited = process.WaitForExit(TimeSpan.FromSeconds(20));
            logger.LogDebug("Closing process.");
            process.Close();

            var outputMessage = output.ToString();
            var bitwardenOutputResponse =
                JsonConvert.DeserializeObject<BitwardenResponse>(outputMessage)
                ?? new BitwardenResponse() { Success = processExited, Message = outputMessage };

            if (!processExited)
            {
                bitwardenOutputResponse.Message += "Process did not exit after 20s.";
            }

            logger.LogDebug(
                "Output while running the '{command}': \n{@bitwardenResponse}",
                command,
                bitwardenOutputResponse
            );

            return bitwardenOutputResponse;
        }

        private string GetBitwardenPath()
        {
            var filePath = Path.Combine(
                bitwardenConfiguration.ExecutablePath,
                bitwardenExecutableName
            );

            if (File.Exists(filePath))
            {
                return filePath;
            }

            bitwardenExecutableName = "bw";
            bitwardenConfiguration.ExecutablePath =
                Path.GetDirectoryName(Environment.ProcessPath) ?? ".";

            if (Environment.OSVersion.Platform.ToString().StartsWith("Win"))
            {
                bitwardenExecutableName = "bw.exe";
                bitwardenConfiguration.ExecutablePath =
                    Path.GetDirectoryName(Environment.ProcessPath)
                    ?? Directory.GetCurrentDirectory();
            }

            filePath = Path.Combine(bitwardenConfiguration.ExecutablePath, bitwardenExecutableName);

            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException(ErrorMessages.BwExeNotFound);
            }

            return filePath;
        }
    }

    internal interface IBitwardenService
    {
        public Task<BitwardenConfiguration> GetBitwardenConfiguration(
            CancellationToken cancellationToken = default
        );

        public Task<BitwardenResponse> SetBitwardenServer(
            CancellationToken cancellationToken = default
        );

        public Task<BitwardenResponse> LogIn(
            EmailPasswordCredential credential,
            CancellationToken cancellationToken = default
        );

        public Task<BitwardenResponse> LogIn(
            ApiKeyCredential credential,
            CancellationToken cancellationToken = default
        );

        public Task<BitwardenResponse> LogOut(CancellationToken cancellationToken = default);

        public Task<BitwardenResponse> ExportVault(CancellationToken cancellationToken = default);

        public Task<BitwardenResponse> RunBitwardenCommand(
            string command,
            string additionalCommands = "",
            List<StandardInput>? inputs = null,
            CancellationToken cancellationToken = default
        );
    }
}
