; ============================================================================
;  CarCare 360 — установщик десктопного приложения (Inno Setup 6.3+)
;  ----------------------------------------------------------------------------
;  Сборка установщика на машине разработчика:
;    1) опубликовать десктоп (self-contained, win-x64, без .pdb) из корня репо:
;         Remove-Item -Recurse -Force CarCare360_Publish -ErrorAction SilentlyContinue
;         dotnet publish CarCare360.Desktop\CarCare360.Desktop.csproj -c Release `
;           -r win-x64 --self-contained true /p:DebugType=None /p:DebugSymbols=false `
;           -o CarCare360_Publish
;    2) скомпилировать установщик:
;         "C:\Program Files (x86)\Inno Setup 6\ISCC.exe" installer\CarCare360Setup.iss
;    Результат: installer\Output\CarCare360Setup.exe
;
;  Доработка не меняет структуру БД и не запрашивает учётные данные при установке.
; ============================================================================

#define MyAppName "CarCare 360"
#define MyAppVersion "1.0.0"
#define MyAppPublisher "CarCare 360"
#define MyAppExeName "CarCare360.Desktop.exe"

[Setup]
AppId={{C9F4B2A1-7E83-4D5C-9A6B-1F2E3D4C5B6A}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL=https://github.com/dan51566/CarCare360
DefaultDirName={autopf}\CarCare360
DefaultGroupName={#MyAppName}
DisableProgramGroupPage=yes
PrivilegesRequired=admin
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
MinVersion=10.0.19041
OutputDir=Output
OutputBaseFilename=CarCare360Setup
Compression=lzma2/ultra
SolidCompression=yes
WizardStyle=modern
UninstallDisplayName={#MyAppName}
UninstallDisplayIcon={app}\{#MyAppExeName}

[Languages]
Name: "russian"; MessagesFile: "compiler:Languages\Russian.isl"

[Tasks]
Name: "desktopicon"; Description: "Создать значок на рабочем столе"; GroupDescription: "Дополнительные значки:"

[Files]
Source: "..\CarCare360_Publish\*"; DestDir: "{app}"; Flags: recursesubdirs createallsubdirs ignoreversion; Excludes: "*.pdb"

[Icons]
Name: "{group}\{#MyAppName}";          Filename: "{app}\{#MyAppExeName}"
Name: "{group}\Удалить {#MyAppName}";  Filename: "{uninstallexe}"
Name: "{autodesktop}\{#MyAppName}";    Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "Запустить {#MyAppName}"; Flags: nowait postinstall skipifsilent

; ----------------------------------------------------------------------------
; [UninstallDelete] намеренно отсутствует.
;   Строка подключения к БД хранится в {app}\CarCare360.Desktop.dll.config и
;   удаляется вместе с приложением (это файл приложения, не данные пользователя).
;   Данные пользователя — в %AppData%\CarCare360\ (avatars.json, saved_login.json):
;   штатное удаление Inno их НЕ затрагивает. Отдельного файла адреса сервера нет.
; ----------------------------------------------------------------------------
