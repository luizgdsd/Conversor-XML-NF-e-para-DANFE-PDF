#define MyAppName "Conversor XML NF-e para DANFE PDF"
#define MyAppVersion "1.0.4"
#define MyAppPublisher "Gugu Solucoes"
#define MyAppExeName "Conversor XML NF-e para DANFE PDF.exe"

[Setup]
AppId={{A17A3A8C-37F4-4A67-B5B1-DA08F3C44D28}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
DefaultDirName={autopf}\{#MyAppName}
DefaultGroupName={#MyAppName}
DisableProgramGroupPage=yes
OutputDir=..\dist
OutputBaseFilename=Instalador_Conversor_XML_NFe_DANFE_PDF
SetupIconFile=..\Assets\barcode-scanner.ico
UninstallDisplayIcon={app}\{#MyAppExeName}
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
PrivilegesRequired=lowest
CloseApplications=yes

[Languages]
Name: "brazilianportuguese"; MessagesFile: "compiler:Languages\BrazilianPortuguese.isl"

[Tasks]
Name: "desktopicon"; Description: "Criar atalho na area de trabalho"; GroupDescription: "Atalhos:"; Flags: unchecked

[Files]
Source: "..\bin\Release\net8.0-windows\win-x64\publish\*"; DestDir: "{app}"; Excludes: "*.pdb"; Flags: ignoreversion recursesubdirs createallsubdirs

[InstallDelete]
Type: filesandordirs; Name: "{app}\*"

[UninstallDelete]
Type: filesandordirs; Name: "{app}\*"

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; IconFilename: "{app}\Assets\barcode-scanner.ico"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; IconFilename: "{app}\Assets\barcode-scanner.ico"; Tasks: desktopicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "Abrir {#MyAppName}"; Flags: nowait postinstall skipifsilent
Filename: "{app}\{#MyAppExeName}"; Flags: nowait skipifnotsilent
