cd E:\RustDev\Scripts
.\BackupServers.ps1 -backup $true;
.\InstallServers.ps1;
.\InstallPlugins.ps1;
.\UpdateOxide.ps1;
.\BackupServers.ps1 -backup $false;