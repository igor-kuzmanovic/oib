using System;
using System.IO;

namespace Server.Services
{
    public class FileSystemHelper
    {
        public static void CopyDirectory(string sourceDir, string targetDir)
        {
            try
            {
                Directory.CreateDirectory(targetDir);

                foreach (string file in Directory.GetFiles(sourceDir))
                {
                    string fileName = Path.GetFileName(file);
                    string targetPath = Path.Combine(targetDir, fileName);
                    File.Copy(file, targetPath, true);
                }

                foreach (string directory in Directory.GetDirectories(sourceDir))
                {
                    string dirName = Path.GetFileName(directory);
                    string targetPath = Path.Combine(targetDir, dirName);
                    CopyDirectory(directory, targetPath);
                }

                Console.WriteLine($"Copied directory: {sourceDir} -> {targetDir}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error copying directory {sourceDir}: {ex.Message}");
            }
        }
    }
}
