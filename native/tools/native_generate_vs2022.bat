@echo off

SET RootPath=../
SET BuildPath=../generated/vs2022/
SET CMakeExePath=build/cmake/windows/bin/cmake.exe

echo Generating "%RootPath%"
echo "%CMakeExePath%" -S "%RootPath%" -B "%BuildPath%"

"%CMakeExePath%" -S "%RootPath%" -B "%BuildPath%" -G "Visual Studio 17 2022"
