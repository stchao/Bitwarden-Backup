using System.Text.RegularExpressions;
using Bitwarden_Backup.Models;
using Spectre.Console;

namespace Bitwarden_Backup.Extensions
{
    internal static partial class SpectreConsoleExtension
    {
        [GeneratedRegex(
            "^\\w+([-+.']\\w+)*@\\w+([-.]\\w+)*\\.\\w+([-.]\\w+)*$",
            RegexOptions.IgnoreCase
        )]
        private static partial Regex ValidEmailRegex();

        public static async Task<string> GetStringInputWithConsole(
            string? initialValue,
            string prompt,
            Func<string, string, ValidationResult> validator,
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
                arg => validator(arg, validationResultErrorMessage)
            );

            if (isSecret)
            {
                textPrompt.Secret(inputMask);
            }

            return await textPrompt.ShowAsync(AnsiConsole.Console, cancellationToken);
        }

        public static ValidationResult DefaultStringValidator(
            string arg,
            string validationResultErrorMessage = Texts.DefaultValidationResult
        )
        {
            if (string.IsNullOrWhiteSpace(arg))
            {
                return ValidationResult.Error(validationResultErrorMessage);
            }

            return ValidationResult.Success();
        }

        public static ValidationResult StringLengthValidator(
            string arg,
            string validationResultErrorMessage = Texts.DefaultValidationResult
        )
        {
            if (string.IsNullOrWhiteSpace(arg) || arg.Length < 12)
            {
                return ValidationResult.Error(validationResultErrorMessage);
            }

            return ValidationResult.Success();
        }

        public static ValidationResult EmailStringValidator(
            string arg,
            string validationResultErrorMessage = Texts.DefaultValidationResult
        )
        {
            if (string.IsNullOrWhiteSpace(arg) || !ValidEmailRegex().IsMatch(arg))
            {
                return ValidationResult.Error(validationResultErrorMessage);
            }

            return ValidationResult.Success();
        }
    }
}
