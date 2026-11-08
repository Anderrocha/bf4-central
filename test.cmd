@echo off
setlocal
call "%~dp0build.cmd" --test
if errorlevel 1 exit /b 1
set "BF4_CENTRAL_TEST_ROOT=%TEMP%\bf4-central-fixture-%RANDOM%-%RANDOM%"
if exist "%BF4_CENTRAL_TEST_ROOT%" exit /b 1
mkdir "%BF4_CENTRAL_TEST_ROOT%"
copy /y "%~dp0tests\fixtures\config.example.txt" "%BF4_CENTRAL_TEST_ROOT%\config.txt" >nul
if not exist "%~dp0tests\results" mkdir "%~dp0tests\results"
start /wait "" "%~dp0dist\BF4 Central.Tests.exe" --unified-test "%~dp0tests\results\unified-results.txt"
set "BF4_TEST_RESULT=%ERRORLEVEL%"
if exist "%~dp0tests\results\unified-results.txt" type "%~dp0tests\results\unified-results.txt"
exit /b %BF4_TEST_RESULT%
