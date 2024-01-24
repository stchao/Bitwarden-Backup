using Spectre.Console;

namespace Bitwarden_Backup.Extensions
{
    internal static class SpectreConsoleExtension
    {
        public static async Task<string> GetUserInputAsStringUsingConsole(
            string? initialValue,
            string prompt,
            string validationResultErrorMessage,
            bool isSecret = false,
            char? inputMask = null,
            CancellationToken cancellationToken = default
        )
        {
            if (!string.IsNullOrEmpty(initialValue))
            {
                return initialValue;
            }

            var textPrompt = new TextPrompt<string>(prompt).Validate(
                arg =>
                    string.IsNullOrWhiteSpace(arg)
                        ? ValidationResult.Error(validationResultErrorMessage)
                        : ValidationResult.Success()
            );

            if (isSecret)
            {
                textPrompt.Secret(inputMask);
            }

            return await textPrompt.ShowAsync(AnsiConsole.Console, cancellationToken);
        }
    }
}
