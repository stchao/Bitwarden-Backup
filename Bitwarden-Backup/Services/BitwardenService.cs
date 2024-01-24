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

        public BitwardenConfiguration GetBitwardenConfiguration()
        {
            var bitwardenCredentials =
                configuration.GetSection(BitwardenCredentials.Key).Get<BitwardenCredentials>()
                ?? new BitwardenCredentials();

            // Set log in method to avoid reprompt if set in appsettings or enough credentials were provided
            bitwardenConfiguration.SetLogInMethod(bitwardenCredentials);

            if (bitwardenConfiguration.LogInMethod == LogInMethod.None)
            {
                logger.LogDebug("Getting log in method using Spectre.Console.");
                bitwardenConfiguration.LogInMethod = AnsiConsole.Prompt(
                    new SelectionPrompt<LogInMethod>()
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
                );
            }

            return bitwardenConfiguration;
        }

        public BitwardenResponse LogIn(EmailPasswordCredential credential)
        {
            logger.LogDebug("Sanity log out.");
            LogOut();

            // Logic for when user selects none or cancels

            if (!string.IsNullOrEmpty(bitwardenConfiguration.Url))
            {
                logger.LogDebug("Saving Bitwarden Server config.");
                RunBitwardenCommand($"config server {bitwardenConfiguration.Url}");
            }

            var inputs = new List<StandardInput>()
            {
                new()
                {
                    Prompt = Prompts.MasterPassword,
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
            var bitwardenLogInResponse = RunBitwardenCommand(
                $"login {credential.Email}{authenticationMethod} --response",
                string.Empty,
                inputs
            );

            if (bitwardenLogInResponse.Success && bitwardenLogInResponse.Data is not null)
            {
                logger.LogDebug("Setting session key.");
                sessionKey = bitwardenLogInResponse.Data.Raw;
            }

            return bitwardenLogInResponse;
        }

        public BitwardenResponse LogIn(ApiKeyCredential credential)
        {
            logger.LogDebug("Sanity log out.");
            LogOut();

            // Logic for when user selects none or cancels

            Environment.SetEnvironmentVariable("BW_CLIENTID", credential.ClientId);
            Environment.SetEnvironmentVariable("BW_CLIENTSECRET", credential.ClientSecret);

            if (!string.IsNullOrEmpty(bitwardenConfiguration.Url))
            {
                logger.LogDebug("Saving Bitwarden Server config.");
                RunBitwardenCommand($"config server {bitwardenConfiguration.Url} --response");
            }

            logger.LogDebug("Running login command with api key.");
            var bitwardenLogInResponse = RunBitwardenCommand("login --apikey --response");

            if (!bitwardenLogInResponse.Success)
            {
                return bitwardenLogInResponse;
            }

            logger.LogDebug("Running unlock command.");
            var bitwardenUnlockResponse = RunBitwardenCommand($"unlock --response");

            Environment.SetEnvironmentVariable("BW_CLIENTID", null);
            Environment.SetEnvironmentVariable("BW_CLIENTSECRET", null);

            if (!bitwardenUnlockResponse.Success && bitwardenUnlockResponse.Data is not null)
            {
                logger.LogDebug("Setting session key.");
                sessionKey = bitwardenUnlockResponse.Data.Raw;
            }

            return bitwardenUnlockResponse;
        }

        public BitwardenResponse LogOut() => RunBitwardenCommand("logout --response");

        public BitwardenResponse ExportVault()
        {
            if (string.IsNullOrWhiteSpace(sessionKey))
            {
                return new BitwardenResponse()
                {
                    Success = false,
                    Message = "The session key cannot be null or empty."
                };
            }

            var availableFullFilePath = FilePathExtension.GetAvailableFullFilePath(
                exportFileProperty.Directory,
                exportFileProperty.Name,
                exportFileProperty.DateInFileNameFormat
            );

            var command =
                $"export --session \"{sessionKey}\" --format {exportFileProperty.Format} --output {availableFullFilePath} --response";

            var additionalCommand = string.Empty;

            if (!string.IsNullOrWhiteSpace(exportFileProperty.CustomExportPassword))
            {
                additionalCommand = $" --password {exportFileProperty.CustomExportPassword}";
            }

            logger.LogDebug("Running export vault command.");
            return RunBitwardenCommand(command, additionalCommand);
        }

        public BitwardenResponse RunBitwardenCommand(
            string command,
            string additionalCommands = "",
            List<StandardInput>? inputs = null
        )
        {
            var output = new StringBuilder();
            var error = new StringBuilder();
            var tempCommand = new StringBuilder();

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

            logger.LogDebug("Starting command '{command}'.", command);
            process.Start();
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
                var userInput = SpectreConsoleExtension.GetUserInputAsStringUsingConsole(
                    currentInput.Value,
                    currentInput.Prompt,
                    currentInput.ValidationResultErrorMessage,
                    currentInput.IsSecret,
                    currentInput.InputMask
                );
                inputIndex++;
                process.StandardInput.WriteLine(userInput);
            }

            var processExited = process.WaitForExit(TimeSpan.FromSeconds(60));
            logger.LogDebug("Closing process.");
            process.Close();

            var outputMessage = output.ToString();
            var bitwardenOutputResponse =
                JsonConvert.DeserializeObject<BitwardenResponse>(outputMessage)
                ?? new BitwardenResponse() { Success = processExited, Message = outputMessage };

            if (!processExited)
            {
                bitwardenOutputResponse.Message += "Process did not exit after 60s.";
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
                throw new FileNotFoundException(
                    $"The file '{bitwardenExecutableName}' is not in the app directory. "
                        + $"Please download the latest version of Bitwarden CLI from https://bitwarden.com/help/cli/ and move the .exe file into the app directory."
                );
            }

            return filePath;
        }
    }

    internal interface IBitwardenService
    {
        public BitwardenConfiguration GetBitwardenConfiguration();

        public BitwardenResponse LogIn(EmailPasswordCredential credential);

        public BitwardenResponse LogIn(ApiKeyCredential credential);

        public BitwardenResponse LogOut();

        public BitwardenResponse ExportVault();

        public BitwardenResponse RunBitwardenCommand(
            string command,
            string additionalCommands = "",
            List<StandardInput>? inputs = null
        );
    }
}
