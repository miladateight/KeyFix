#define MyAppName "KeyFix"
#define MyAppVersion "0.2.0"
#define MyAppPublisher "Milad AT8"
#define MyAppURL "https://github.com/miladateight/KeyFix"
#define MyAppExeName "KeyFix.exe"

[Setup]
AppId={{C0C242A7-DA07-4A22-B73C-BC0F909831F2}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
AppUpdatesURL={#MyAppURL}
AppContact={#MyAppURL}
DefaultDirName={autopf}\{#MyAppName}
DefaultGroupName={#MyAppName}
DisableProgramGroupPage=yes
OutputDir=..\artifacts\installer
OutputBaseFilename=KeyFixSetup-{#MyAppVersion}
SetupIconFile=..\assets\keyfix-icon.ico
UninstallDisplayIcon={app}\{#MyAppExeName}
WizardImageFile=assets\wizard-image.bmp
WizardSmallImageFile=assets\wizard-small.bmp
InfoBeforeFile=INFO-BEFORE.txt
LicenseFile=..\LICENSE
VersionInfoCompany={#MyAppPublisher}
VersionInfoDescription={#MyAppName} installer
VersionInfoProductName={#MyAppName}
VersionInfoProductVersion={#MyAppVersion}
VersionInfoVersion={#MyAppVersion}
VersionInfoCopyright=Copyright (c) 2026 {#MyAppPublisher}. Special thanks to Ashkan Gharib for the original idea.
Compression=lzma
SolidCompression=yes
WizardStyle=modern

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
Source: "..\artifacts\publish\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{group}\Milad AT8 on GitHub"; Filename: "{#MyAppURL}"
Name: "{group}\Special thanks to Ashkan Gharib"; Filename: "{#MyAppURL}"
Name: "{group}\Uninstall {#MyAppName}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent
