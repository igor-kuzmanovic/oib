using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace SecurityManager
{
	public class Audit : IDisposable
	{

		private static EventLog customLog = null;
		const string SourceName = "SecurityManager.Audit";
		const string LogName = "FileServerAuditLog";

		static Audit()
		{
			try
			{
				if (!EventLog.SourceExists(SourceName))
				{
					EventLog.CreateEventSource(SourceName, LogName);
				}
				customLog = new EventLog(LogName,
					Environment.MachineName, SourceName);
			}
			catch (Exception e)
			{
				customLog = null;
				Console.WriteLine("Error while trying to create log handle. Error = {0}", e.Message);
			}
		}


		public static void AuthenticationSuccess(string userName)
		{
			if (customLog != null)
			{
				string UserAuthenticationSuccess =
					AuditEvents.AuthenticationSuccess;
				string message = String.Format(UserAuthenticationSuccess,
					userName);
				customLog.WriteEntry(message);
			}
			else
			{
				throw new ArgumentException(string.Format("Error while trying to write event (eventid = {0}) to event log.",
					(int)AuditEventTypes.AuthenticationSuccess));
			}
		}

		public static void AuthorizationSuccess(string userName, string serviceName)
		{
			if (customLog != null)
			{
				string AuthorizationSuccess =
					AuditEvents.AuthorizationSuccess;
				string message = String.Format(AuthorizationSuccess,
					userName, serviceName);
				customLog.WriteEntry(message);
			}
			else
			{
				throw new ArgumentException(string.Format("Error while trying to write event (eventid = {0}) to event log.",
					(int)AuditEventTypes.AuthorizationSuccess));
			}
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="userName"></param>
		/// <param name="serviceName"> should be read from the OperationContext as follows: OperationContext.Current.IncomingMessageHeaders.Action</param>
		/// <param name="reason">permission name</param>
		public static void AuthorizationFailed(string userName, string serviceName, string reason)
		{
			if (customLog != null)
			{
				string AuthorizationFailed =
					AuditEvents.AuthorizationFailed;
				string message = String.Format(AuthorizationFailed,
					userName, serviceName, reason);
				customLog.WriteEntry(message);
			}
			else
			{
				throw new ArgumentException(string.Format("Error while trying to write event (eventid = {0}) to event log.",
					(int)AuditEventTypes.AuthorizationFailed));
			}
		}

		public static void FileCreated(string userName, string filePath)
		{
			if (customLog != null)
			{
				string message = $"User {userName} created file {filePath}";
				customLog.WriteEntry(message);
			}
			else
			{
				throw new ArgumentException("Error while trying to write file creation event to event log.");
			}
		}

		public static void FolderCreated(string userName, string folderPath)
		{
			if (customLog != null)
			{
				string message = $"User {userName} created folder {folderPath}";
				customLog.WriteEntry(message);
			}
			else
			{
				throw new ArgumentException("Error while trying to write folder creation event to event log.");
			}
		}

		public static void FileDeleted(string userName, string filePath)
		{
			if (customLog != null)
			{
				string message = $"User {userName} deleted file {filePath}";
				customLog.WriteEntry(message);
			}
			else
			{
				throw new ArgumentException("Error while trying to write file deletion event to event log.");
			}
		}

		public static void FolderDeleted(string userName, string folderPath)
		{
			if (customLog != null)
			{
				string message = $"User {userName} deleted folder {folderPath}";
				customLog.WriteEntry(message);
			}
			else
			{
				throw new ArgumentException("Error while trying to write folder deletion event to event log.");
			}
		}

		public static void FileMoved(string userName, string sourcePath, string destinationPath)
		{
			if (customLog != null)
			{
				string message = $"User {userName} moved file from {sourcePath} to {destinationPath}";
				customLog.WriteEntry(message);
			}
			else
			{
				throw new ArgumentException("Error while trying to write file move event to event log.");
			}
		}
		public static void FolderMoved(string userName, string sourcePath, string destinationPath)
		{
			if (customLog != null)
			{
				string message = $"User {userName} moved folder from {sourcePath} to {destinationPath}";
				customLog.WriteEntry(message);
			}
			else
			{
				throw new ArgumentException("Error while trying to write folder move event to event log.");
			}
		}

		public static void FileAccessed(string userName, string filePath)
		{
			if (customLog != null)
			{
				string message = $"User {userName} accessed file {filePath}";
				customLog.WriteEntry(message);
			}
			else
			{
				throw new ArgumentException("Error while trying to write file access event to event log.");
			}
		}

		public static void FolderAccessed(string userName, string folderPath)
		{
			if (customLog != null)
			{
				string message = $"User {userName} accessed folder {folderPath}";
				customLog.WriteEntry(message);
			}
			else
			{
				throw new ArgumentException("Error while trying to write folder access event to event log.");
			}
		}

		public void Dispose()
		{
			if (customLog != null)
			{
				customLog.Dispose();
				customLog = null;
			}
		}
	}
}
