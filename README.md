# FileServer

## Project

```
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
If the primary server goes down, the client switches to the backup server, and from that moment it becomes the primary until it goes down and switches back to the backup server.

All actions in the system, starting from authentication, authorization, as well as operations on the database itself, must be logged within a specific log file in the Windows Event Log.

The system must log every change to folders and files in the Windows Event Log.
```

## Users

* TBD

## Certificates

* Creating a Root CA (use `password` for password)

```powershell
& "C:\\Program Files (x86)\\Windows Kits\\10\\bin\\10.0.19041.0\\x86\\makecert.exe" -n "CN=FileServerRootCA" -r -sv FileServerRootCA.pvk FileServerRootCA.cer
```

* Creating an invalid Root CA (use `password` for password)

```powershell
& "C:\\\\Program Files (x86)\\\\Windows Kits\\\\10\\\\bin\\\\10.0.19041.0\\\\x86\\\\makecert.exe" -n "CN=FileServerInvalidRootCA" -r -sv FileServerInvalidRootCA.pvk FileServerInvalidRootCA.cer
```

* Creating a primary server certificate (use `password` for password)

```powershell
& "C:\\Program Files (x86)\\Windows Kits\\10\\bin\\10.0.19041.0\\x86\\makecert.exe" -sv FileServerPrimary.pvk -iv FileServerRootCA.pvk -n "CN=FileServerPrimary" -pe -ic FileServerRootCA.cer FileServerPrimary.cer -sr localmachine -ss My -sky exchange
```

```powershell
& "C:\\Program Files (x86)\\Windows Kits\\10\\bin\\10.0.19041.0\\x86\\pvk2pfx.exe" /pvk FileServerPrimary.pvk /pi password /spc FileServerPrimary.cer /pfx FileServerPrimary.pfx
```

* Creating a backup server certificate (use `password` for password)

```powershell
& "C:\\\\Program Files (x86)\\\\Windows Kits\\\\10\\\\bin\\\\10.0.19041.0\\\\x86\\\\makecert.exe" -sv FileServerBackup.pvk -iv FileServerRootCA.pvk -n "CN=FileServerBackup" -pe -ic FileServerRootCA.cer FileServerBackup.cer -sr localmachine -ss My -sky exchange
```

```powershell
& "C:\\\\Program Files (x86)\\\\Windows Kits\\\\10\\\\bin\\\\10.0.19041.0\\\\x86\\\\pvk2pfx.exe" /pvk FileServerBackup.pvk /pi password /spc FileServerBackup.cer /pfx FileServerBackup.pfx
```

* Creating a invalid server certificate (use `password` for password)

```powershell
& "C:\\\\\\\\Program Files (x86)\\\\\\\\Windows Kits\\\\\\\\10\\\\\\\\bin\\\\\\\\10.0.19041.0\\\\\\\\x86\\\\\\\\makecert.exe" -sv FileServerInvalid.pvk -iv FileServerInvalidRootCA.pvk -n "CN=FileServerInvalid" -pe -ic FileServerInvalidRootCA.cer FileServerInvalid.cer -sr localmachine -ss My -sky exchange
```

```
& "C:\\\\Program Files (x86)\\\\Windows Kits\\\\10\\\\bin\\\\10.0.19041.0\\\\x86\\\\pvk2pfx.exe" /pvk FileServerInvalid.pvk /pi password /spc FileServerInvalid.cer /pfx FileServerInvalid.pfx
```

* Install the valid root CA .cer into Trusted CA
* Don't install the invalid root CA .cer
* Install primary and backup .cer into Trusted People
* Don't install the invalid .cer
* Install primary, backup and invalid .pfx into Personal
* Assign each certificate to matching user

## Audit log

* Run as an administrator

```powershell
New-EventLog -LogName "FileServerAuditLog" -Source "FileServer.Audit"
```