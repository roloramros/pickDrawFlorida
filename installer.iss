; ===== installer.iss =====

[Setup]
AppName=FloridaLotteryApp
AppVersion=1.0
DefaultGroupName=FloridaLotteryApp
OutputDir=Output
OutputBaseFilename=FloridaLotteryInstaller
SetupIconFile=E:\pickDrawFlorida\iconoApp.ico
UsePreviousAppDir=no
DefaultDirName={code:GetNoDefaultDir}
PrivilegesRequired=lowest



[Files]
Source: "E:\pickDrawFlorida\bin\Release\net8.0-windows\win-x64\publish\*"; DestDir: "{app}"; Flags: recursesubdirs
Source: "E:\pickDrawFlorida\florida_pick3_pick4_fixed.sqlite"; DestDir: "{app}"; Flags: ignoreversion

[Tasks]
Name: "desktopicon"; Description: "Crear acceso directo en el escritorio"; Flags: unchecked
Name: "startup"; Description: "Iniciar FloridaLotteryApp al iniciar Windows"; Flags: unchecked

[Icons]
; Menú Inicio
Name: "{group}\FloridaLotteryApp"; Filename: "{app}\FloridaLotteryApp.exe"

; Escritorio (si marcan la tarea)
Name: "{userdesktop}\FloridaLotteryApp"; Filename: "{app}\FloridaLotteryApp.exe"; Tasks: desktopicon
; Para todos los usuarios (requiere admin): {commondesktop}

; Inicio con Windows (si marcan la tarea)
Name: "{userstartup}\FloridaLotteryApp"; Filename: "{app}\FloridaLotteryApp.exe"; Tasks: startup
; Para todos los usuarios (requiere admin): {commonstartup}

[Code]
function GetNoDefaultDir(Param: string): string;
begin
  Result := '';  // deja el campo de carpeta vacío
end;

procedure DirEditChange(Sender: TObject);
begin
  // Habilitar "Siguiente" solo si hay texto en la ruta
  WizardForm.NextButton.Enabled := Trim(WizardForm.DirEdit.Text) <> '';
end;

procedure InitializeWizard;
begin
  // Estado inicial: campo vacío y Next deshabilitado
  WizardForm.DirEdit.Text := '';
  WizardForm.NextButton.Enabled := False;
  WizardForm.DirEdit.OnChange := @DirEditChange;
end;

function NextButtonClick(CurPageID: Integer): Boolean;
begin
  Result := True;
  if CurPageID = wpSelectDir then
  begin
    if Trim(WizardForm.DirEdit.Text) = '' then
    begin
      MsgBox('Debes elegir una carpeta de instalación.', mbError, MB_OK);
      Result := False;
      // Intentar enfocar el campo (si tu versión lo soporta)
      try
        WizardForm.ActiveControl := WizardForm.DirEdit;
      except
        { ignorar si no existe }
      end;
    end;
  end;
end;
