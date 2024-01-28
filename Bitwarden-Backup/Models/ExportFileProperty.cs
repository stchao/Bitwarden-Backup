namespace Bitwarden_Backup.Models
{
    internal class ExportFileProperty
    {
        public static string Key = "ExportFile";

        private string _path = "bw_export";

        public string Path
        {
            get => _path;
            set
            {
                if (!string.IsNullOrWhiteSpace(value))
                {
                    _path = value;
                }
            }
        }

        public string DateInFileNameFormat { get; set; } = string.Empty;

        public string Format
        {
            get => ExportFormat.ToString();
            set
            {
                if (string.IsNullOrWhiteSpace(value))
                {
                    return;
                }

                if (!Enum.TryParse(value, out ExportFormat tempExportFormat))
                {
                    throw new FormatException($"{value} is not a valid value for ExportFormat.");
                }

                ExportFormat = tempExportFormat;
            }
        }

        public ExportFormat ExportFormat { get; set; } = ExportFormat.json;

        public string CustomExportPassword { get; set; } = string.Empty;
    }
}
