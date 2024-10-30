@echo off
cd /D %~dp0

echo Build all projects using the Build system
call %~dp0.stbuild/build.cmd --target Compile

pause

EXIT /B %EXIT_CODE%