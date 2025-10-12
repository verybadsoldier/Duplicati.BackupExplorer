; -- Duplicati.BackupExplorer Installer Script for Inno Setup --
;
; See the Inno Setup documentation for details on this script file:
; https://jrsoftware.org/ishelp/

[Setup]
; NOTE: The value of AppId uniquely identifies this application.
; Do not use the same AppId value for other applications.
; It is recommended to use a GUID. You can generate one here: https://www.guidgenerator.com/
AppId={{C62A8AFB-503A-4F79-A152-6A816A5A357E}
AppName=Duplicati.BackupExplorer
; Remember to update the AppVersion for each new release.
; The version will be passed by the GitHub Action. This default is for local builds.
#ifndef AppVersion
  #define AppVersion "0.0.0-local"
#endif
AppVersion={#AppVersion}

// Entfernt alle doppelten Anführungszeichen, falls welche vorhanden sind
#define AppVersionClean StringChange(AppVersion, '"', '')
AppVersion={#AppVersionClean}

AppPublisher=verybadsoldier
AppPublisherURL=https://github.com/verybadsoldier/Duplicati.BackupExplorer
AppSupportURL=https://github.com/verybadsoldier/Duplicati.BackupExplorer/issues
AppUpdatesURL=https://github.com/verybadsoldier/Duplicati.BackupExplorer/releases
; Default installation directory in Program Files. {autopf} detects 32-bit vs 64-bit.
DefaultDirName={autopf}\Duplicati.BackupExplorer
; Default Start Menu folder name.
DefaultGroupName=Duplicati.BackupExplorer
; The name of the final setup executable. Include the version number.
OutputBaseFilename=Duplicati.BackupExplorer-Setup-{#AppVersionClean}
; The directory where the final setup executable will be created.
OutputDir=InstallerOutput
Compression=lzma
SolidCompression=yes
WizardStyle=modern
; Set the icon for the installer/uninstaller in "Add or remove programs".
UninstallDisplayIcon={app}\Duplicati.BackupExplorer.exe

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
; This adds an optional checkbox in the wizard to create a desktop icon.
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}";

[Files]
; This section lists all the files to be installed.
; This script assumes your compiled application files (all the contents of your release .zip)
; are in a folder named 'release_files' relative to this script.
Source: "release_files\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
; This section creates the Start Menu shortcuts.
Name: "{group}\Duplicati.BackupExplorer"; Filename: "{app}\Duplicati.BackupExplorer.exe"
Name: "{group}\{cm:UninstallProgram,Duplicati.BackupExplorer}"; Filename: "{uninstallexe}"
; This creates the optional desktop icon if the user selected the task.
Name: "{commondesktop}\Duplicati.BackupExplorer"; Filename: "{app}\Duplicati.BackupExplorer.exe"; Tasks: desktopicon

[Run]
; This section offers to run the application after the installation is complete.
Filename: "{app}\Duplicati.BackupExplorer.exe"; Description: "{cm:LaunchProgram,Duplicati.BackupExplorer}"; Flags: nowait postinstall skipifsilent

