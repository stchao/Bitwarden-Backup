using System.Text.RegularExpressions;
using Bitwarden_Backup.Models;
using Spectre.Console;

namespace Bitwarden_Backup.Extensions
{
    public static partial class SpectreConsoleExtension
    {
        [GeneratedRegex(
            "^\\w+([-+.']\\w+)*@\\w+([-.]\\w+)*\\.\\w+([-.]\\w+)*$",
            RegexOptions.IgnoreCase
        )]
        private static partial Regex ValidEmailRegex();

        public static async Task<string> GetStringInputWithConsole(
            string? initialValue,
            string prompt,
            Func<ValidatorParams, ValidationResult> validator,
            ValidatorParams validatorParams,
            bool isSecret = false,
            char? inputMask = null,
            CancellationToken cancellationToken = default
        )
        {
            if (!string.IsNullOrEmpty(initialValue))
            {
                return initialValue;
            }

            var textPrompt = new TextPrompt<string>(prompt).Validate(arg =>
            {
                validatorParams.Arg = arg;
                return validator(validatorParams);
            });

            if (isSecret)
            {
                textPrompt.Secret(inputMask);
            }

            return await textPrompt.ShowAsync(AnsiConsole.Console, cancellationToken);
        }

        public static ValidationResult DefaultStringValidator(ValidatorParams validatorParams)
        {
            if (validatorParams.IsArgNullOrWhiteSpace)
            {
                return ValidationResult.Error(validatorParams.ValidationResultErrorMessage);
            }

            return ValidationResult.Success();
        }

        public static ValidationResult StringInHashValidator(ValidatorParams validatorParams)
        {
            if (validatorParams.IsArgNullOrWhiteSpace || !validatorParams.IsArgInValidHash)
            {
                return ValidationResult.Error(validatorParams.ValidationResultErrorMessage);
            }

            return ValidationResult.Success();
        }

        public static ValidationResult StringLengthValidator(ValidatorParams validatorParams)
        {
            if (validatorParams.IsArgNullOrWhiteSpace || !validatorParams.IsArgMinLength)
            {
                return ValidationResult.Error(validatorParams.ValidationResultErrorMessage);
            }

            return ValidationResult.Success();
        }

        public static ValidationResult EmailStringValidator(ValidatorParams validatorParams)
        {
            if (
                validatorParams.IsArgNullOrWhiteSpace
                || !ValidEmailRegex().IsMatch(validatorParams.Arg)
            )
            {
                return ValidationResult.Error(validatorParams.ValidationResultErrorMessage);
            }

            return ValidationResult.Success();
        }
    }
}
