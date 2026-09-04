#define MyAppName "MyMediaImport"
#define MyAppPublisher "Christian Pistor"
#define MyAppExeName "MyMediaImport.App.exe"

#if !FileExists("..\artifacts\publish\app\" + MyAppExeName)
  #error "The MyMediaImport application is missing from the publish directory."
#endif
#if !FileExists("..\artifacts\publish\cli\MyMediaImport.Cli.exe")
  #error "The MyMediaImport CLI is missing from the publish directory."
#endif
#if !FileExists("..\artifacts\publish\app\LICENSE")
  #error "The MyMediaImport license is missing from the publish directory."
#endif
#if !FileExists("..\artifacts\publish\app\DOTNET-LICENSE.txt")
  #error "The .NET runtime license is missing from the publish directory."
#endif
#if !FileExists("..\artifacts\publish\app\DOTNET-THIRD-PARTY-NOTICES.txt")
  #error "The .NET runtime third-party notices are missing from the publish directory."
#endif
#if !FileExists("..\artifacts\publish\app\DOTNET-WINDOWS-DESKTOP-LICENSE.txt")
  #error "The Windows Desktop runtime license is missing from the publish directory."
#endif
#if !FileExists("..\artifacts\installer\DOTNET-LICENSE.txt")
  #error "The combined Unicode .NET license file for the installer is missing."
#endif

#define MyAppVersion GetVersionNumbersString("..\artifacts\publish\app\" + MyAppExeName)

[Setup]
AppId={{D8BB8298-F02B-45BF-A771-241109AD0C0F}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppVerName={#MyAppName} {#MyAppVersion}
AppPublisher={#MyAppPublisher}
LicenseFile=..\artifacts\installer\DOTNET-LICENSE.txt
DefaultDirName={autopf}\{#MyAppName}
DisableProgramGroupPage=yes
PrivilegesRequired=admin
OutputBaseFilename={#MyAppName}_{#MyAppVersion}
SetupIconFile=..\src\MyMediaImport.App\Assets\MyMediaImport.ico
UninstallDisplayIcon={app}\{#MyAppExeName}
Compression=lzma
SolidCompression=yes
WizardStyle=modern
ArchitecturesAllowed=x64
ArchitecturesInstallIn64BitMode=x64

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Messages]
WizardLicense=Microsoft .NET License Terms
LicenseLabel=The license terms below apply only to the bundled Microsoft .NET components.
LicenseLabel3=The license terms below apply only to the bundled Microsoft .NET components. You must accept them before continuing with the installation.
LicenseAccepted=I &accept the license terms for the bundled Microsoft .NET components
LicenseNotAccepted=I &do not accept the license terms for the bundled Microsoft .NET components

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
Source: "..\artifacts\publish\app\*"; Excludes: "*.pdb"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs
Source: "..\artifacts\publish\cli\*"; Excludes: "*.pdb"; DestDir: "{app}\cli"; Flags: ignoreversion recursesubdirs

[Icons]
Name: "{autoprograms}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{autoprograms}\{#MyAppName} CLI"; Filename: "{cmd}"; Parameters: "/K ""MyMediaImport.Cli.exe help"""; WorkingDir: "{app}\cli"; IconFilename: "{cmd}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon
Name: "{autodesktop}\{#MyAppName} CLI"; Filename: "{cmd}"; Parameters: "/K ""MyMediaImport.Cli.exe help"""; WorkingDir: "{app}\cli"; IconFilename: "{cmd}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent
