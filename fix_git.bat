@echo off
rem Connects a copy of the project that was not made with "git clone" (ZIP download, USB stick) to GitHub,
rem so that "git pull" works. Run it once, in the project folder, with Unity closed. See README chapter 9.
cd /d "%~dp0"
if not exist Assets (echo Put this file into the project folder, next to Assets. & pause & exit /b 1)
if exist .git (echo This folder is already a Git repository. Just run: git pull & pause & exit /b 0)
where git >nul 2>nul || (echo Git is not installed. Install Git for Windows first: https://git-scm.com/download/win & pause & exit /b 1)

echo This replaces the project's files with the current version from GitHub.
echo Your own files (robot, arena, team asset, models) stay.
echo Changes you made to project files (konfig.yaml, project scenes) will be overwritten - copy them aside first.
echo Close Unity before you continue.
pause

git init -b master
git remote add origin https://github.com/LuLa-90/AI-JAM-2025.git
git fetch --depth 1 origin master || (echo Download failed. Check the internet connection, then delete the hidden .git folder and run this file again. & pause & exit /b 1)
git reset -q origin/master
git branch --set-upstream-to=origin/master
git restore .
echo.
echo Done. From now on use: git pull
pause
