@echo off
setlocal enabledelayedexpansion

REM Check if the required parameters are passed
REM (3rd param will be blank if there are not enough)
if "%~3" == "" (
    echo ERROR: Missing required parameters
    exit /b 1
)

REM Extract parameters and remove quotes
set NAME=%~1
set SOURCE=%~2
set BIN64=%~3

REM Remove trailing backslash if applicable
if "%NAME:~-1%"=="\" set NAME=%NAME:~0,-1%
if "%SOURCE:~-1%"=="\" set SOURCE=%SOURCE:~0,-1%
if "%BIN64:~-1%"=="\" set BIN64=%BIN64:~0,-1%

REM Get the plugin directory
set PLUGIN_DIR=%BIN64%\Plugins\Local

REM Create this directory if it does not exist
if not exist "%PLUGIN_DIR%" (
    echo Creating "Local\" folder in "%BIN64%\Plugins\"
    mkdir "%PLUGIN_DIR%" >NUL 2>&1
)

REM Copy the plugin and dependency assemblies into the plugin directory
echo Copying plugin output from "%SOURCE%" to "%PLUGIN_DIR%\"

for /l %%i in (1, 1, 10) do (
    if not exist "%PLUGIN_DIR%\atomic.fm.libs" mkdir "%PLUGIN_DIR%\atomic.fm.libs" >NUL 2>&1
    copy /y "%SOURCE%\atomic.fm.dll" "%PLUGIN_DIR%\" >NUL
    copy /y "%SOURCE%\atomic.fm.pdb" "%PLUGIN_DIR%\" >NUL 2>&1
    copy /y "%SOURCE%\NAudio*.dll" "%PLUGIN_DIR%\atomic.fm.libs\" >NUL 2>&1
    copy /y "%SOURCE%\Microsoft.Win32.Registry.dll" "%PLUGIN_DIR%\atomic.fm.libs\" >NUL 2>&1
    copy /y "%SOURCE%\System.Security.AccessControl.dll" "%PLUGIN_DIR%\atomic.fm.libs\" >NUL 2>&1
    copy /y "%SOURCE%\System.Security.Principal.Windows.dll" "%PLUGIN_DIR%\atomic.fm.libs\" >NUL 2>&1
    copy /y "%~dp0PluginHub.xml" "%PLUGIN_DIR%\plugin.xml" >NUL

    if !ERRORLEVEL! NEQ 0 (
        REM "timeout" requires input redirection which is not supported,
        REM so we use ping as a way to delay the script between retries.
        ping -n 2 127.0.0.1 >NUL 2>&1
    ) else (
        goto BREAK_LOOP
    )
)

REM This part will only be reached if the loop has been exhausted
REM Any success would skip to the BREAK_LOOP label below
echo ERROR: Could not copy plugin output.
exit /b 1

:BREAK_LOOP
if exist "D:\Pulsar\Legacy\Local" (
    echo Copying plugin output to "D:\Pulsar\Legacy\Local\"
    if not exist "D:\Pulsar\Legacy\Local\atomic.fm.libs" mkdir "D:\Pulsar\Legacy\Local\atomic.fm.libs" >NUL 2>&1
    copy /y "%SOURCE%\atomic.fm.dll" "D:\Pulsar\Legacy\Local\" >NUL
    copy /y "%SOURCE%\atomic.fm.pdb" "D:\Pulsar\Legacy\Local\" >NUL 2>&1
    copy /y "%SOURCE%\NAudio*.dll" "D:\Pulsar\Legacy\Local\atomic.fm.libs\" >NUL 2>&1
    copy /y "%SOURCE%\Microsoft.Win32.Registry.dll" "D:\Pulsar\Legacy\Local\atomic.fm.libs\" >NUL 2>&1
    copy /y "%SOURCE%\System.Security.AccessControl.dll" "D:\Pulsar\Legacy\Local\atomic.fm.libs\" >NUL 2>&1
    copy /y "%SOURCE%\System.Security.Principal.Windows.dll" "D:\Pulsar\Legacy\Local\atomic.fm.libs\" >NUL 2>&1
    copy /y "%~dp0PluginHub.xml" "D:\Pulsar\Legacy\Local\plugin.xml" >NUL
)

if exist "D:\Pulsar\Legacy\Local\Atomic-Radio" (
    echo Copying plugin output to "D:\Pulsar\Legacy\Local\Atomic-Radio\"
    if not exist "D:\Pulsar\Legacy\Local\Atomic-Radio\atomic.fm.libs" mkdir "D:\Pulsar\Legacy\Local\Atomic-Radio\atomic.fm.libs" >NUL 2>&1
    copy /y "%SOURCE%\atomic.fm.dll" "D:\Pulsar\Legacy\Local\Atomic-Radio\plugin.dll" >NUL
    copy /y "%SOURCE%\atomic.fm.pdb" "D:\Pulsar\Legacy\Local\Atomic-Radio\plugin.pdb" >NUL 2>&1
    copy /y "%SOURCE%\NAudio*.dll" "D:\Pulsar\Legacy\Local\Atomic-Radio\atomic.fm.libs\" >NUL 2>&1
    copy /y "%SOURCE%\Microsoft.Win32.Registry.dll" "D:\Pulsar\Legacy\Local\Atomic-Radio\atomic.fm.libs\" >NUL 2>&1
    copy /y "%SOURCE%\System.Security.AccessControl.dll" "D:\Pulsar\Legacy\Local\Atomic-Radio\atomic.fm.libs\" >NUL 2>&1
    copy /y "%SOURCE%\System.Security.Principal.Windows.dll" "D:\Pulsar\Legacy\Local\Atomic-Radio\atomic.fm.libs\" >NUL 2>&1
    copy /y "%~dp0PluginHub.xml" "D:\Pulsar\Legacy\Local\Atomic-Radio\plugin.xml" >NUL
)

exit /b 0
