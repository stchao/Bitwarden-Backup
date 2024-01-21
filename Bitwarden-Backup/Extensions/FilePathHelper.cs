namespace Bitwarden_Backup.Extensions
{
    internal static class FilePathHelper
    {
        internal static string GetAvailableFullFilePath(
            string directoryPath,
            string fileName,
            string dateFormat = ""
        )
        {
            var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(fileName);
            var fileNameSuffix = string.IsNullOrWhiteSpace(dateFormat)
                ? string.Empty
                : $"_{DateTime.Now.ToString(dateFormat)}";
            var tempFileName = $"{fileNameWithoutExtension}{fileNameSuffix}.json";
            var tempFullPath = Path.Combine(directoryPath, tempFileName);

            if (!string.IsNullOrWhiteSpace(fileName) && !File.Exists(tempFullPath))
            {
                return tempFullPath;
            }

            return GetNextAvailableFullFilePath(
                directoryPath,
                fileNameWithoutExtension,
                dateFormat
            );
        }

        internal static string GetNextAvailableFullFilePath(
            string directoryPath,
            string baseFileName,
            string dateFormat = ""
        )
        {
            var tempFileName = string.IsNullOrWhiteSpace(dateFormat)
                ? $"{baseFileName}_{DateTime.Now.ToString(dateFormat)}"
                : baseFileName;
            var tempFullFilePath = Path.Combine(directoryPath, tempFileName);
            var counter = 0;

            while (File.Exists(tempFullFilePath))
            {
                tempFullFilePath = Path.Combine(directoryPath, $"{tempFileName}_{counter}.json");
                counter++;

                if (counter % 10 == 0)
                {
                    tempFileName = $"{tempFileName}_{Guid.NewGuid().ToString()[..8]}";
                }
            }

            return tempFullFilePath;
        }
    }
}
