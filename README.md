# FileServer

It is necessary to implement a system for storing text files on a server.

Users authenticate using the Windows authentication protocol.

To authorize the system, implement the RBAC mechanism. The RBAC permissions that the system should have are:  
● See  
● Change  
● Delete  

Mapping permissions to groups should be made configurable.

In order for an authenticated user to be able to call any service method, they must be a member of the Reader group.

The service should provide:  
● ShowFolderContent  
● ReadFile – enable secure transmission of content using the AES cryptographic algorithm in CBC mode  
● CreateFolder (must also be a member of the Editor group)  
● CreateFile (must also be a member of the Editor group) – enable secure transmission of content using the AES cryptographic algorithm in CBC mode  
● Delete (must also be a member of the Editor group)  
● Rename (must also be a member of the Editor group)  
● MoveTo (must also be a member of the Editor group)  

Every change to folders and files is performed by the system through impersonation of the Editor user.

It is necessary to create a backup server that authenticates using a certificate with the primary server.  
If the primary server goes down, the client switches to the secondary server, and from that moment it becomes the primary until it goes down and switches back to the secondary server.

All actions in the system, starting from authentication, authorization, as well as operations on the database itself, must be logged within a specific log file in the Windows Event Log.

The system must log every change to folders and files in the Windows Event Log.
