@echo off
setlocal
set "BF4_COMPILER=%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
if not exist "%BF4_COMPILER%" exit /b 1
if not exist "%~dp0dist" mkdir "%~dp0dist"
set "BF4_MANIFEST=%~dp0admin.manifest"
set "BF4_OUTPUT=%~dp0dist\BF4 Central.exe"
if "%~1"=="--test" (
  set "BF4_MANIFEST=%~dp0app.manifest"
  set "BF4_OUTPUT=%~dp0dist\BF4 Central.Tests.exe"
)
"%BF4_COMPILER%" /nologo /target:winexe /platform:x64 /optimize+ /main:BF4Bots.UnifiedProgram /out:"%BF4_OUTPUT%" /win32manifest:"%BF4_MANIFEST%" /reference:System.Windows.Forms.dll /reference:System.Drawing.dll /reference:System.Core.dll /reference:System.Management.dll /reference:System.Web.Extensions.dll /resource:"%~dp0resources\maps.json",BF4Bots.Catalog "%~dp0src\*.cs"
exit /b %ERRORLEVEL%
