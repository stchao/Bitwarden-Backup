﻿namespace Bitwarden_Backup.Extensions
{
    internal static class FilePathHelper
    {
        internal static string GetAvailableFullFilePath(string fullFilePath, string baseFileName = "filename", bool appendDate = false) 
        {
            var tempFileName = Path.GetFileName(fullFilePath);            

            if (!string.IsNullOrWhiteSpace(tempFileName) && !File.Exists(fullFilePath))
            {
                return fullFilePath;
            }

            var filePath = Path.GetFullPath(fullFilePath);
            return GetNextAvailableFullFilePath(filePath, baseFileName, appendDate);
        }

        internal static string GetNextAvailableFullFilePath(string filePath, string baseFileName, bool appendDate)
        {
            var tempFileName = appendDate ? $"{baseFileName}_{DateTime.Now:yyyyMMdd}" : baseFileName;
            var tempFullFilePath = Path.Combine(filePath, tempFileName);
            var counter = 0;

            while (File.Exists(tempFullFilePath))
            {
                tempFullFilePath = Path.Combine(filePath, $"{tempFileName}_{counter}");
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