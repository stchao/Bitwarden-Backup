using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bitwarden_Backup.Models
{
    internal class ExportFileProperty
    {
        public static string AppSettingsKey = "ExportFile";

        public string Directory { get; set; } = string.Empty;

        public string Name { get; set; } = "bw_export";

        public string DateInFileNameFormat { get; set; } = "yyyyMMdd";

        public ExportFormat Format { get; set; } = ExportFormat.json;

        public string CustomExportPassword { get; set; } = string.Empty;
    }
}
