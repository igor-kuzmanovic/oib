using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Resources;
using System.Text;

namespace SecurityManager
{
	public enum AuditEventTypes
	{
		AuthenticationSuccess = 0,
		AuthorizationSuccess = 1,
		AuthorizationFailed = 2,
		FileCreated = 3,
		FolderCreated = 4,
		FileDeleted = 5,
		FolderDeleted = 6,
		FileMoved = 7,
		FolderMoved = 8
	}

	public class AuditEvents
	{
		private static ResourceManager resourceManager = null;
		private static object resourceLock = new object();

		private static ResourceManager ResourceMgr
		{
			get
			{
				lock (resourceLock)
				{
					if (resourceManager == null)
					{
						resourceManager = new ResourceManager
							(typeof(AuditEventFile).ToString(),
							Assembly.GetExecutingAssembly());
					}
					return resourceManager;
				}
			}
		}

		public static string AuthenticationSuccess
		{
			get
			{
				return ResourceMgr.GetString(AuditEventTypes.AuthenticationSuccess.ToString());
			}
		}

		public static string AuthorizationSuccess
		{
			get
			{
				return ResourceMgr.GetString(AuditEventTypes.AuthorizationSuccess.ToString());
			}
		}

		public static string AuthorizationFailed
		{
			get
			{
				return ResourceMgr.GetString(AuditEventTypes.AuthorizationFailed.ToString());
			}
		}

		public static string FileCreated
		{
			get
			{
				return ResourceMgr.GetString(AuditEventTypes.FileCreated.ToString());
			}
		}

		public static string FolderCreated
		{
			get
			{
				return ResourceMgr.GetString(AuditEventTypes.FolderCreated.ToString());
			}
		}

		public static string FileDeleted
		{
			get
			{
				return ResourceMgr.GetString(AuditEventTypes.FileDeleted.ToString());
			}
		}

		public static string FolderDeleted
		{
			get
			{
				return ResourceMgr.GetString(AuditEventTypes.FolderDeleted.ToString());
			}
		}

		public static string FileMoved
		{
			get
			{
				return ResourceMgr.GetString(AuditEventTypes.FileMoved.ToString());
			}
		}

		public static string FolderMoved
		{
			get
			{
				return ResourceMgr.GetString(AuditEventTypes.FolderMoved.ToString());
			}
		}
	}
}
