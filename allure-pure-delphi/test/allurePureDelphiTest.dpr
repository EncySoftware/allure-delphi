program allurePureDelphiTest;

{$APPTYPE CONSOLE}

{$R *.res}

uses
  System.SysUtils,
  pureTestFramework in '..\src\pureTestFramework.pas',
  pureTestAssert in '..\src\pureTestAssert.pas',
  pureTestExceptions in '..\src\pureTestExceptions.pas',
  pureTestConsoleLogger in '..\src\pureTestConsoleLogger.pas',
  allureDelphiInterface,
  allureDelphiHelper,
  pureTestExample in 'pureTestExample.pas',
  pureTestAllureLogger in '..\src\pureTestAllureLogger.pas';

procedure Main;
var
  context: TPureTests;
begin
  context := TPureTests.Context;
  try
    context.Listeners.Add(TPureTestConsoleLogger.Create);
    context.Listeners.Add(TPureTestAllureLogger.Create);
    ExitCode := context.RunTests;
    Allure.GenerateReport('allure-report');
    //ReadLn;
  finally
    context.Finalize;
  end;
end;

begin
  try
    Main;
  except
    on E: Exception do
      Writeln(E.ClassName, ': ', E.Message);
  end;
end.
