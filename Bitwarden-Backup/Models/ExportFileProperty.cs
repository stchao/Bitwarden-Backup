namespace Bitwarden_Backup.Models
{
    internal class ExportFileProperty
    {
        public static string Key = "ExportFile";

        public string Path { get; set; } = string.Empty;

        public string DateInFileNameFormat { get; set; } = string.Empty;

        public ExportFormat ExportFormat { get; set; } = ExportFormat.json;

        public string CustomExportPassword { get; set; } = string.Empty;
    }
}
