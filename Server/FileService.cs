using System;
using System.IO;
using System.Security.Cryptography;
using System.ServiceModel;
using System.Threading;
using Contracts;
using SecurityManager;

namespace Server
{
    public class FileService : IWCFService
    {
        private string GetCurrentUser()
        {
            CustomPrincipal principal = Thread.CurrentPrincipal as CustomPrincipal;
            return Formatter.ParseName(principal?.Identity?.Name ?? "Unknown");
        }

        private string GetAction()
        {
            return OperationContext.Current?.IncomingMessageHeaders?.Action ?? "UnknownAction";
        }

        private void LogSuccess(string user)
        {
            try
            {
                Audit.AuthorizationSuccess(user, GetAction());
            }
            catch (Exception e)
            {
                Console.WriteLine("Audit success log failed: " + e.Message);
            }
        }

        private void LogFailure(string user, string reason)
        {
            try
            {
                Audit.AuthorizationFailed(user, GetAction(), reason);
            }
            catch (Exception e)
            {
                Console.WriteLine("Audit failure log failed: " + e.Message);
            }
        }

        public bool CreateFile(string path, FileData fileData)
        {
            string user = GetCurrentUser();
            var principal = Thread.CurrentPrincipal as CustomPrincipal;

            if (principal == null || !principal.IsInRole("Change"))
            {
                LogFailure(user, "CreateFile requires Change permission.");
                throw new FaultException($"User {user} is not authorized for CreateFile.");
            }

            try
            {
                using (Aes aes = Aes.Create())
                {
                    aes.Mode = CipherMode.CBC;
                    aes.Key = EncryptionHelper.GetSecretKey(); // Implement securely
                    aes.IV = fileData.InitializationVector;

                    using (FileStream fs = new FileStream(path, FileMode.Create))
                    using (CryptoStream cs = new CryptoStream(fs, aes.CreateDecryptor(), CryptoStreamMode.Write))
                    {
                        cs.Write(fileData.Content, 0, fileData.Content.Length);
                    }
                }

                LogSuccess(user);
                return true;
            }
            catch (Exception ex)
            {
                LogFailure(user, $"Exception in CreateFile: {ex.Message}");
                throw new FaultException("Error while creating file.");
            }
        }

        public bool CreateFolder(string path)
        {
            string user = GetCurrentUser();
            var principal = Thread.CurrentPrincipal as CustomPrincipal;

            if (principal == null || !principal.IsInRole("Change"))
            {
                LogFailure(user, "CreateFolder requires Change permission.");
                throw new FaultException($"User {user} is not authorized for CreateFolder.");
            }

            try
            {
                Directory.CreateDirectory(path);
                LogSuccess(user);
                return true;
            }
            catch (Exception ex)
            {
                LogFailure(user, $"Exception in CreateFolder: {ex.Message}");
                throw new FaultException("Error while creating folder.");
            }
        }

        public bool Delete(string path)
        {
            string user = GetCurrentUser();
            var principal = Thread.CurrentPrincipal as CustomPrincipal;

            if (principal == null || !principal.IsInRole("Delete"))
            {
                LogFailure(user, "Delete requires Delete permission.");
                throw new FaultException($"User {user} is not authorized for Delete.");
            }

            try
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
                else if (Directory.Exists(path))
                {
                    Directory.Delete(path, true);
                }
                else
                {
                    throw new FileNotFoundException("File or folder not found.");
                }

                LogSuccess(user);
                return true;
            }
            catch (Exception ex)
            {
                LogFailure(user, $"Exception in Delete: {ex.Message}");
                throw new FaultException("Error while deleting.");
            }
        }

        public bool MoveTo(string sourcePath, string destinationPath)
        {
            string user = GetCurrentUser();
            var principal = Thread.CurrentPrincipal as CustomPrincipal;

            if (principal == null || !principal.IsInRole("Change"))
            {
                LogFailure(user, "MoveTo requires Change permission.");
                throw new FaultException($"User {user} is not authorized for MoveTo.");
            }

            try
            {
                if (File.Exists(sourcePath))
                {
                    File.Move(sourcePath, destinationPath);
                }
                else if (Directory.Exists(sourcePath))
                {
                    Directory.Move(sourcePath, destinationPath);
                }
                else
                {
                    throw new FileNotFoundException("Source path does not exist.");
                }

                LogSuccess(user);
                return true;
            }
            catch (Exception ex)
            {
                LogFailure(user, $"Exception in MoveTo: {ex.Message}");
                throw new FaultException("Error while moving.");
            }
        }

        public bool Rename(string sourcePath, string destinationPath)
        {
            return MoveTo(sourcePath, destinationPath);
        }

        public FileData ReadFile(string path)
        {
            string user = GetCurrentUser();
            var principal = Thread.CurrentPrincipal as CustomPrincipal;

            if (principal == null || !principal.IsInRole("See"))
            {
                LogFailure(user, "ReadFile requires See permission.");
                throw new FaultException($"User {user} is not authorized for ReadFile.");
            }

            try
            {
                byte[] encryptedData = File.ReadAllBytes(path);
                byte[] iv = new byte[16]; // AES block size = 16 bytes

                Array.Copy(encryptedData, 0, iv, 0, 16);
                byte[] cipher = new byte[encryptedData.Length - 16];
                Array.Copy(encryptedData, 16, cipher, 0, cipher.Length);

                LogSuccess(user);

                return new FileData
                {
                    InitializationVector = iv,
                    Content = cipher
                };
            }
            catch (Exception ex)
            {
                LogFailure(user, $"Exception in ReadFile: {ex.Message}");
                throw new FaultException("Error while reading file.");
            }
        }

        public string[] ShowFolderContent(string path)
        {
            string user = GetCurrentUser();
            var principal = Thread.CurrentPrincipal as CustomPrincipal;

            if (principal == null || !principal.IsInRole("See"))
            {
                LogFailure(user, "ShowFolderContent requires See permission.");
                throw new FaultException($"User {user} is not authorized for ShowFolderContent.");
            }

            try
            {
                string[] entries = Directory.GetFileSystemEntries(path);
                LogSuccess(user);
                return entries;
            }
            catch (Exception ex)
            {
                LogFailure(user, $"Exception in ShowFolderContent: {ex.Message}");
                throw new FaultException("Error while listing folder content.");
            }
        }
    }
}