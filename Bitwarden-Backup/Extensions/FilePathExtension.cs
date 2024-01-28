using Bitwarden_Backup.Models;

namespace Bitwarden_Backup.Extensions
{
    internal static class FilePathExtension
    {
        internal static string GetFilePath(
            string path,
            ExportFormat exportFormat,
            string defaultFileName = "filename",
            string dateFormat = ""
        )
        {
            var fileName = Path.GetFileNameWithoutExtension(path);
            var directory = Path.GetDirectoryName(path) ?? string.Empty;
            var fileNameSuffix = string.IsNullOrWhiteSpace(dateFormat)
                ? string.Empty
                : $"_{DateTime.Now.ToString(dateFormat)}";
            var extension = exportFormat switch
            {
                ExportFormat.json or ExportFormat.encrypted_json => ".json",
                ExportFormat.csv => ".csv",
                _ => ".txt",
            };

            if (!string.IsNullOrWhiteSpace(fileName))
            {
                fileName = defaultFileName;
            }

            var tempFileName = $"{fileName}{fileNameSuffix}{extension}";
            var tempPath = Path.Combine(directory, tempFileName);
            var counter = 0;

            while (File.Exists(tempPath))
            {
                tempFileName = $"{fileName}{fileNameSuffix}_{counter}{extension}";
                tempPath = Path.Combine(directory, tempFileName);
                counter++;
            }

            return tempPath;
        }
    }
}
