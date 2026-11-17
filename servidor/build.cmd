@echo off
setlocal
set "CSC=%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
if not exist "%~dp0..\dist" mkdir "%~dp0..\dist"
"%CSC%" /nologo /target:winexe /platform:x64 /optimize+ /out:"%~dp0..\dist\BF4 Servidor.exe" /win32manifest:"%~dp0..\admin.manifest" /win32icon:"%~dp0..\resources\central.ico" /reference:System.Windows.Forms.dll /reference:System.Drawing.dll "%~dp0ServidorPainel.cs"
exit /b %ERRORLEVEL%
