namespace Bitwarden_Backup.Models
{
    public class ValidatorParams
    {
        public string Arg { get; set; } = string.Empty;

        public string ValidationResultErrorMessage { get; set; } = Texts.DefaultValidationResult;

        public int MinLength { get; set; }

        public HashSet<string> ValidArgsHash { get; set; } = [];

        public bool IsArgNullOrWhiteSpace
        {
            get { return string.IsNullOrWhiteSpace(Arg); }
        }

        public bool IsArgInValidHash
        {
            get { return ValidArgsHash.Contains(Arg); }
        }

        public bool IsArgMinLength
        {
            get { return Arg.Length >= MinLength; }
        }
    }
}
