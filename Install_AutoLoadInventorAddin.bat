@echo off
REM ============================================================================
REM  MCG Bracket Library - cai dat auto-load cho Inventor 2023
REM  Copy .addin (tro toi DLL trong C:\CustomTools\Inventor\MCG_BracketLibrary)
REM  vao %APPDATA%\Autodesk\Inventor 2023\Addins\MCG_BracketLibrary\
REM ============================================================================
setlocal
set SRC=C:\CustomTools\Inventor\MCG_BracketLibrary
set DST=%APPDATA%\Autodesk\Inventor 2023\Addins\MCG_BracketLibrary

if not exist "%SRC%\MCG_BracketLibrary.addin" (
  echo [LOI] Khong tim thay %SRC%\MCG_BracketLibrary.addin
  echo       Hay build project truoc ^(dotnet build -c Debug^).
  pause
  exit /b 1
)

if not exist "%DST%" mkdir "%DST%"
copy /Y "%SRC%\MCG_BracketLibrary.addin" "%DST%\MCG_BracketLibrary.addin"

echo.
echo [OK] Da cai dat. Khoi dong lai Inventor de nap add-in.
echo      Tab "MCG TOOLS" -> panel "Model" -> nut "Bracket Library".
pause
