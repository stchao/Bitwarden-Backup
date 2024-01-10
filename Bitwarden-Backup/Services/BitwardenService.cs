using System.Diagnostics;
using System.Text;
using Bitwarden_Backup.Extensions;
using Bitwarden_Backup.Models;
using Microsoft.Extensions.Logging;

namespace Bitwarden_Backup.Services
{
    internal class BitwardenService(ILogger<BitwardenService> logger)
        : IBitwardenService,
            IDisposable
    {
        private string sessionKey = string.Empty;

        public bool LogIn(string password, string userName, int otp = -1, string url = "")
        {
            // Sanity logout!
            LogOut();

            if (!string.IsNullOrEmpty(url))
            {
                // Saved setting `config`.
                RunCommand($"config server {url}");
            }

            var authenticationMethod = (otp > -1) ? $"--method 0 --code {otp}" : "";
            var (isLoginSuccessful, loginOutput) = RunCommand(
                $"login {userName} {password} --raw {authenticationMethod}",
                true
            );

            if (isLoginSuccessful)
            {
                sessionKey = loginOutput;
            }

            return isLoginSuccessful;
        }

        public bool LogIn(string password, string clientId, string clientSecret, string url = "")
        {
            // Sanity logout!
            LogOut();

            Environment.SetEnvironmentVariable("BW_CLIENTID", clientId);
            Environment.SetEnvironmentVariable("BW_CLIENTSECRET", clientSecret);

            if (!string.IsNullOrEmpty(url))
            {
                // Saved setting `config`.
                RunCommand($"config server {url}");
            }

            var (isLoginSuccessful, _) = RunCommand("login --apikey");

            if (!isLoginSuccessful)
            {
                return false;
            }

            var (isUnlockSuccessful, unlockOutput) = RunCommand($"unlock {password} --raw", true);

            if (isUnlockSuccessful)
            {
                sessionKey = unlockOutput;
            }

            return isUnlockSuccessful;
        }

        public bool LogOut() => RunCommand("logout").isSuccessful;

        public bool ExportVault(
            string fullFilePath,
            ExportFormat exportFormat = ExportFormat.json,
            string password = "",
            bool appendDate = false,
            string baseFileName = "fileName"
        )
        {
            if (string.IsNullOrWhiteSpace(sessionKey))
            {
                logger.LogError("Session key is null or empty.");
                return false;
            }

            var availableFullFilePath = FilePathHelper.GetAvailableFullFilePath(
                fullFilePath,
                appendDate,
                baseFileName
            );

            var exportCommand = new StringBuilder();
            exportCommand.Append($"export --format {exportFormat} ");
            exportCommand.Append($" --output {availableFullFilePath} ");

            if (
                exportFormat.Equals(ExportFormat.encrypted_json)
                && !string.IsNullOrWhiteSpace(password)
            )
            {
                exportCommand.Append($" --password {password} ");
            }

            var (isExportSuccessful, _) = RunCommand(exportCommand.ToString(), true);

            return isExportSuccessful;
        }

        public (bool isSuccessful, string output) RunCommand(
            string command,
            bool isCommandSensitive = false
        )
        {
            var output = new StringBuilder();
            var error = new StringBuilder();

            var process = new Process()
            {
                StartInfo = new ProcessStartInfo()
                {
                    FileName = GetBitwardenPath(),
                    Arguments = command,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    ErrorDialog = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    RedirectStandardInput = false
                },
            };

            var tempCommand = isCommandSensitive ? "[redacted]" : command;

            process.ErrorDataReceived += (sender, args) =>
            {
                if (!string.IsNullOrWhiteSpace(args.Data))
                {
                    error.AppendLine(args.Data);
                    logger.LogError(
                        "Error when running the command '{tempCommand}' - {errorData}",
                        tempCommand,
                        args.Data
                    );
                }
            };

            process.OutputDataReceived += (sender, args) =>
            {
                if (!string.IsNullOrWhiteSpace(args.Data))
                {
                    output.AppendLine(args.Data);
                    logger.LogInformation(
                        "Output when running the command '{tempCommand}' - {outputData}",
                        tempCommand,
                        args.Data
                    );
                }
            };

            process.Start();
            process.BeginErrorReadLine();
            process.BeginOutputReadLine();
            process.WaitForExit();

            var errorResponse = error.ToString();

            if (
                !string.IsNullOrWhiteSpace(errorResponse)
                && !errorResponse.Contains("You are already logged")
            )
            {
                return (false, errorResponse);
            }

            return (true, output.ToString());
        }

        public void Dispose() => LogOut();

        private static string GetBitwardenPath()
        {
            var bitwardenFileName = "bw";
            var currentAppPath = Path.GetDirectoryName(Environment.ProcessPath) ?? ".";

            if (Environment.OSVersion.Platform.ToString().StartsWith("Win"))
            {
                bitwardenFileName = "bw.exe";
                currentAppPath =
                    Path.GetDirectoryName(Environment.ProcessPath)
                    ?? Directory.GetCurrentDirectory();
            }

            var filePath = Path.Combine(currentAppPath, bitwardenFileName);

            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException(
                    $"The file '{bitwardenFileName}' is not in the app directory. "
                        + $"Please download the latest version of Bitwarden CLI from https://bitwarden.com/help/cli/ and move the .exe file into the app directory."
                );
            }

            return filePath;
        }
    }

    internal interface IBitwardenService { }
}
