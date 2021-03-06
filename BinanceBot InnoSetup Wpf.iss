#define MyAppName "BinanceBotWpf"
#define MyExeNameUI "BinanceBotWpf"
;#define MyExeNameCLI "BinanceBotConsole"
#define MyAppParentDir "BinanceBotWpf\bin\Release\netcoreapp3.1\"
#define MyAppPath MyAppParentDir + MyExeNameUI + ".exe"
#dim Version[4]
#expr ParseVersion(MyAppPath, Version[0], Version[1], Version[2], Version[3])
#define MyAppVersion Str(Version[0]) + "." + Str(Version[1]) + "." + Str(Version[2])
#define MyAppPublisher "ShareX Developers"


[Setup]
AllowNoIcons=true
AppId={#MyAppName}
AppMutex={{C86B9172-59B0-4577-9811-2E200C7D8807}
AppName={#MyAppName}
AppPublisher={#MyAppPublisher}
AppPublisherURL=https://github.com/McoreD/BinanceBot
AppSupportURL=https://github.com/McoreD/BinanceBot/issues
AppUpdatesURL=https://github.com/McoreD/BinanceBot/releases
AppVerName={#MyAppName} {#MyAppVersion}
AppVersion={#MyAppVersion}
ArchitecturesAllowed=x86 x64 ia64
ArchitecturesInstallIn64BitMode=x64 ia64
Compression=lzma/ultra64
CreateAppDir=true
DefaultDirName={autopf}\{#MyAppName}
DefaultGroupName={#MyAppName}
DirExistsWarning=no
InternalCompressLevel=ultra64
LanguageDetectionMethod=uilanguage
MinVersion=6
OutputBaseFilename={#MyAppName}-{#MyAppVersion}-setup
OutputDir=Output\
PrivilegesRequired=lowest
PrivilegesRequiredOverridesAllowed=dialog
ShowLanguageDialog=auto
ShowUndisplayableLanguages=false
SignedUninstaller=false
SolidCompression=true
Uninstallable=true
UninstallDisplayIcon={app}\{#MyExeNameUI}
UsePreviousAppDir=yes
UsePreviousGroup=yes
VersionInfoCompany={#MyAppName}
VersionInfoTextVersion={#MyAppVersion}
VersionInfoVersion={#MyAppVersion}
WizardStyle=modern

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
Source: BinanceBotWpf\bin\Release\netcoreapp3.1\*.exe; Excludes: *.vshost.exe; DestDir: {app}; Flags: ignoreversion
Source: BinanceBotWpf\bin\Release\netcoreapp3.1\*.dll; DestDir: {app}; Flags: ignoreversion
Source: BinanceBotWpf\bin\Release\netcoreapp3.1\*.pdb; DestDir: {app}; Flags: ignoreversion

[Icons]
Name: "{group}\{#MyExeNameUI}"; Filename: "{app}\{#MyExeNameUI}.exe"
;Name: "{group}\{#MyExeNameCLI}"; Filename: "{app}\{#MyExeNameCLI}.exe"
Name: "{userdesktop}\{#MyAppName}"; Filename: "{app}\{#MyExeNameUI}.exe"; Tasks: desktopicon

[Run]
Filename: {app}\{#MyExeNameUI}.exe; Description: {cm:LaunchProgram,{#MyAppName} UI}; Flags: nowait postinstall skipifsilent
;Filename: {app}\{#MyExeNameCLI}.exe; Description: {cm:LaunchProgram,{#MyAppName} Console}; Flags: nowait postinstall skipifsilent unchecked