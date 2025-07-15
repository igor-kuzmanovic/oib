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

It is necessary to create a backup server that authenticates using a certificate with the beta server.
If the beta server goes down, the client switches to the backup server, and from that moment it becomes the beta until it goes down and switches back to the backup server.

All actions in the system, starting from authentication, authorization, as well as operations on the database itself, must be logged within a specific log file in the Windows Event Log.

The system must log every change to folders and files in the Windows Event Log.
```

## Users

- TBD

## Certificates

- Creating a Root CA (use `password` for password)

```powershell
& "C:\\Program Files (x86)\\Windows Kits\\10\\bin\\10.0.19041.0\\x86\\makecert.exe" -n "CN=FileServerRootCA" -r -sv FileServerRootCA.pvk FileServerRootCA.cer
```

- Creating a alpha server certificate (use `password` for password)

```powershell
& "C:\\Program Files (x86)\\Windows Kits\\10\\bin\\10.0.19041.0\\x86\\makecert.exe" -sv FileServerAlpha.pvk -iv FileServerRootCA.pvk -n "CN=FileServerAlpha" -pe -ic FileServerRootCA.cer FileServerAlpha.cer -sr localmachine -ss My -sky exchange
```

```powershell
& "C:\\Program Files (x86)\\Windows Kits\\10\\bin\\10.0.19041.0\\x86\\pvk2pfx.exe" /pvk FileServerAlpha.pvk /pi password /spc FileServerAlpha.cer /pfx FileServerAlpha.pfx
```

- Creating a beta server certificate (use `password` for password)

```powershell
& "C:\\Program Files (x86)\\Windows Kits\\10\\bin\\10.0.19041.0\\x86\\makecert.exe" -sv FileServerBeta.pvk -iv FileServerRootCA.pvk -n "CN=FileServerBeta" -pe -ic FileServerRootCA.cer FileServerBeta.cer -sr localmachine -ss My -sky exchange
```

```powershell
& "C:\\Program Files (x86)\\Windows Kits\\10\\bin\\10.0.19041.0\\x86\\pvk2pfx.exe" /pvk FileServerBeta.pvk /pi password /spc FileServerBeta.cer /pfx FileServerBeta.pfx
```

- Creating a gamma server certificate (use `password` for password)

```powershell
& "C:\\Program Files (x86)\\Windows Kits\\10\\bin\\10.0.19041.0\\x86\\makecert.exe" -sv FileServerGamma.pvk -iv FileServerRootCA.pvk -n "CN=FileServerGamma" -pe -ic FileServerRootCA.cer FileServerGamma.cer -sr localmachine -ss My -sky exchange
```

```powershell
& "C:\\Program Files (x86)\\Windows Kits\\10\\bin\\10.0.19041.0\\x86\\pvk2pfx.exe" /pvk FileServerGamma.pvk /pi password /spc FileServerGamma.cer /pfx FileServerGamma.pfx
```

- Creating a invalid server certificate (use `password` for password)

```powershell
& "C:\\Program Files (x86)\\Windows Kits\\10\\bin\\10.0.19041.0\\x86\\makecert.exe" -sv FileServerInvalid.pvk -n "CN=FileServerInvalid" -pe FileServerInvalid.cer -sr localmachine -ss My -sky exchange -r
```

```
& "C:\\Program Files (x86)\\Windows Kits\\10\\bin\\10.0.19041.0\\x86\\pvk2pfx.exe" /pvk FileServerInvalid.pvk /pi password /spc FileServerInvalid.cer /pfx FileServerInvalid.pfx
```

- Install the valid root CA .cer into Trusted CA
- Install alpha, beta, gamma and invalid .cer into Trusted People
- Install alpha, beta, gamma and invalid .pfx into Personal
- Assign each certificate to matching user

## Audit log

- Run as an administrator

```powershell
New-EventLog -LogName "FileServerAuditLog" -Source "FileServer.Audit"
```
