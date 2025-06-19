using System;
using System.Runtime.InteropServices;
using System.Security.Principal;

namespace SecurityManager
{
    /// <summary>
    /// Utility class for Windows impersonation
    /// </summary>
    public class ImpersonationHelper : IDisposable
    {
        private WindowsImpersonationContext _impersonationContext = null;
        private WindowsIdentity _editorIdentity = null;
        private bool _disposed = false;

        [DllImport("advapi32.dll", SetLastError = true)]
        private static extern bool LogonUser(
            string lpszUsername,
            string lpszDomain,
            string lpszPassword,
            int dwLogonType,
            int dwLogonProvider,
            out IntPtr phToken);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool CloseHandle(IntPtr handle);

        private const int LOGON32_LOGON_INTERACTIVE = 2;
        private const int LOGON32_PROVIDER_DEFAULT = 0;

        /// <summary>
        /// Impersonates the Editor user
        /// </summary>
        /// <param name="domain">Domain name</param>
        /// <param name="username">Username (Editor)</param>
        /// <param name="password">Password for the Editor account</param>
        /// <returns>True if impersonation succeeds, false otherwise</returns>
        public bool Impersonate(string domain, string username, string password)
        {
            try
            {
                IntPtr token = IntPtr.Zero;
                bool success = LogonUser(
                    username,
                    domain,
                    password,
                    LOGON32_LOGON_INTERACTIVE,
                    LOGON32_PROVIDER_DEFAULT,
                    out token);

                if (!success)
                {
                    int error = Marshal.GetLastWin32Error();
                    throw new Exception($"LogonUser failed with error code: {error}");
                }

                _editorIdentity = new WindowsIdentity(token);
                _impersonationContext = _editorIdentity.Impersonate();

                CloseHandle(token);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Impersonation failed: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Reverts impersonation back to the original identity
        /// </summary>
        public void Revert()
        {
            if (_impersonationContext != null)
            {
                _impersonationContext.Undo();
                _impersonationContext = null;
            }
        }

        /// <summary>
        /// Executes an action while impersonating the Editor user
        /// </summary>
        /// <param name="domain">Domain name</param>
        /// <param name="username">Username (Editor)</param>
        /// <param name="password">Password for the Editor account</param>
        /// <param name="action">The action to execute while impersonating</param>
        /// <returns>True if the action completed successfully, false otherwise</returns>
        public static bool ExecuteAsEditor(string domain, string username, string password, Action action)
        {
            using (var impersonator = new ImpersonationHelper())
            {
                if (impersonator.Impersonate(domain, username, password))
                {
                    try
                    {
                        action();
                        return true;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Action failed during impersonation: {ex.Message}");
                        return false;
                    }
                    finally
                    {
                        impersonator.Revert();
                    }
                }
                return false;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    Revert();

                    if (_editorIdentity != null)
                    {
                        _editorIdentity.Dispose();
                        _editorIdentity = null;
                    }
                }
                _disposed = true;
            }
        }

        ~ImpersonationHelper()
        {
            Dispose(false);
        }
    }
}
