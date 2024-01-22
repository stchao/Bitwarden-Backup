namespace Bitwarden_Backup.Models
{
    internal class StandardInput
    {
        public string Prompt { get; set; } = string.Empty;

        public string ValidationResultErrorMessage { get; set; } = string.Empty;

        public string Value { get; set; } = string.Empty;

        public bool IsSecret { get; set; }

        public char? InputMask { get; set; }
    }
}
