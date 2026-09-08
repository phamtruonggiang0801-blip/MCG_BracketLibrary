@echo off
REM ============================================================================
REM  MCG Bracket Library - cai dat auto-load cho Inventor 2023
REM  Copy toan bo folder DLL + .addin tu C:\CustomTools\Inventor\MCG_BracketLibrary
REM  sang %APPDATA%\Autodesk\Inventor 2023\Addins\MCG_BracketLibrary\
REM  (giong cach MCG_CheckListInventor deploy)
REM ============================================================================
setlocal
set SRC=C:\CustomTools\Inventor\MCG_BracketLibrary
set DST=%APPDATA%\Autodesk\Inventor 2023\Addins\MCG_BracketLibrary

if not exist "%SRC%\MCG_BracketLibrary.dll" (
  echo [LOI] Khong tim thay %SRC%\MCG_BracketLibrary.dll
  echo       Hay build project truoc: dotnet build -c Debug
  pause
  exit /b 1
)

if not exist "%DST%" mkdir "%DST%"
xcopy /Y /I /E "%SRC%\*" "%DST%\"

echo.
echo [OK] Da cai dat vao "%DST%".
echo      Khoi dong lai Inventor. Tab "MCG TOOLS" -^> panel "Model" -^> nut "Bracket Library".
pause
