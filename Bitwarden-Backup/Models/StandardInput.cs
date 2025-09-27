using Bitwarden_Backup.Extensions;
using Spectre.Console;

namespace Bitwarden_Backup.Models
{
    public class StandardInput
    {
        public string Prompt { get; set; } = string.Empty;

        public Func<ValidatorParams, ValidationResult> Validator { get; set; } =
            SpectreConsoleExtension.DefaultStringValidator;

        public ValidatorParams ValidatorParams { get; set; } = new ValidatorParams();

        public bool IsSecret { get; set; }

        public char? InputMask { get; set; }
    }
}
