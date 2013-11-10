; Script generated by the Inno Setup Script Wizard.
; SEE THE DOCUMENTATION FOR DETAILS ON CREATING INNO SETUP SCRIPT FILES!

#define MyAppName "Google Music Desktop Wrapper"
#define MyAppVersion "0.1"
#define MyAppPublisher "UberMouse"
#define MyAppExeName "GoogleMusicWrapper.exe"

[Setup]
; NOTE: The value of AppId uniquely identifies this application.
; Do not use the same AppId value in installers for other applications.
; (To generate a new GUID, click Tools | Generate GUID inside the IDE.)
AppId={{C77F623B-0026-40AA-8552-1AD954A72CD2}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
;AppVerName={#MyAppName} {#MyAppVersion}
AppPublisher={#MyAppPublisher}
DefaultDirName={pf}\{#MyAppName}
DefaultGroupName={#MyAppName}
AllowNoIcons=yes
OutputBaseFilename=setup
Compression=lzma
SolidCompression=yes

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
Source: "C:\Users\Taylor\Dropbox\Programming\C#\GoogleMusicWrapper\GoogleMusicWrapper\bin\Release\GoogleMusicWrapper.exe"; DestDir: "{app}"; Flags: ignoreversion
Source: "C:\Users\Taylor\Dropbox\Programming\C#\GoogleMusicWrapper\GoogleMusicWrapper\bin\Release\js\*"; DestDir: "{app}\js"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "C:\Users\Taylor\Dropbox\Programming\C#\GoogleMusicWrapper\GoogleMusicWrapper\bin\Release\avcodec-53.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "C:\Users\Taylor\Dropbox\Programming\C#\GoogleMusicWrapper\GoogleMusicWrapper\bin\Release\avformat-53.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "C:\Users\Taylor\Dropbox\Programming\C#\GoogleMusicWrapper\GoogleMusicWrapper\bin\Release\avutil-51.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "C:\Users\Taylor\Dropbox\Programming\C#\GoogleMusicWrapper\GoogleMusicWrapper\bin\Release\Awesomium.Core.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "C:\Users\Taylor\Dropbox\Programming\C#\GoogleMusicWrapper\GoogleMusicWrapper\bin\Release\Awesomium.Core.xml"; DestDir: "{app}"; Flags: ignoreversion
Source: "C:\Users\Taylor\Dropbox\Programming\C#\GoogleMusicWrapper\GoogleMusicWrapper\bin\Release\awesomium.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "C:\Users\Taylor\Dropbox\Programming\C#\GoogleMusicWrapper\GoogleMusicWrapper\bin\Release\Awesomium.Windows.Controls.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "C:\Users\Taylor\Dropbox\Programming\C#\GoogleMusicWrapper\GoogleMusicWrapper\bin\Release\Awesomium.Windows.Controls.xml"; DestDir: "{app}"; Flags: ignoreversion
Source: "C:\Users\Taylor\Dropbox\Programming\C#\GoogleMusicWrapper\GoogleMusicWrapper\bin\Release\Awesomium.Windows.Forms.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "C:\Users\Taylor\Dropbox\Programming\C#\GoogleMusicWrapper\GoogleMusicWrapper\bin\Release\Awesomium.Windows.Forms.xml"; DestDir: "{app}"; Flags: ignoreversion
Source: "C:\Users\Taylor\Dropbox\Programming\C#\GoogleMusicWrapper\GoogleMusicWrapper\bin\Release\awesomium_process"; DestDir: "{app}"; Flags: ignoreversion
Source: "C:\Users\Taylor\Dropbox\Programming\C#\GoogleMusicWrapper\GoogleMusicWrapper\bin\Release\icudt.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "C:\Users\Taylor\Dropbox\Programming\C#\GoogleMusicWrapper\GoogleMusicWrapper\bin\Release\inspector.pak"; DestDir: "{app}"; Flags: ignoreversion
Source: "C:\Users\Taylor\Dropbox\Programming\C#\GoogleMusicWrapper\GoogleMusicWrapper\bin\Release\libEGL.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "C:\Users\Taylor\Dropbox\Programming\C#\GoogleMusicWrapper\GoogleMusicWrapper\bin\Release\libGLESv2.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "C:\Users\Taylor\Dropbox\Programming\C#\GoogleMusicWrapper\GoogleMusicWrapper\bin\Release\Lpfm.LastFmScrobbler.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "C:\Users\Taylor\Dropbox\Programming\C#\GoogleMusicWrapper\GoogleMusicWrapper\bin\Release\xinput9_1_0.dll"; DestDir: "{app}"; Flags: ignoreversion
; NOTE: Don't use "Flags: ignoreversion" on any shared system files

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{commondesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent

