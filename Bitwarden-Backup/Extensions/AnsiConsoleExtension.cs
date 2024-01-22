using Spectre.Console;

namespace Bitwarden_Backup.Extensions
{
    internal static class AnsiConsoleExtension
    {
        public static string GetUserInputAsStringUsingConsole(
            this string? initialValue,
            string prompt,
            string validationResultErrorMessage,
            bool isSecret = false,
            char? inputMask = null
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

            return AnsiConsole.Prompt(textPrompt);
        }
    }
}
