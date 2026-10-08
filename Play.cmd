@echo off
setlocal
set "HEIR_PACKAGE_DIR=%~dp0"
set "HEIR_GAME_DIR=%~dp0.."
if not defined HEIR_DATA_DIR set "HEIR_DATA_DIR=%LOCALAPPDATA%\HeirOfTheSpire"
if not exist "%HEIR_GAME_DIR%\desktop-1.0.jar" (
  echo Slay the Spire is missing. Install this package through Melty into your owned game.
  exit /b 1
)
"%HEIR_PACKAGE_DIR%Reader\ReadRogueLegacy.exe" --host "%HEIR_GAME_DIR%" --sheet "%HEIR_PACKAGE_DIR%assets.json" --cache "%HEIR_DATA_DIR%\cache"
if errorlevel 1 exit /b 1
pushd "%HEIR_GAME_DIR%"
"%HEIR_GAME_DIR%\jre\bin\java.exe" -Xmx1G -jar "%HEIR_PACKAGE_DIR%ModTheSpire.jar" --skip-launcher --skip-intro --mods basemod,heir
set "HEIR_RESULT=%ERRORLEVEL%"
popd
exit /b %HEIR_RESULT%
