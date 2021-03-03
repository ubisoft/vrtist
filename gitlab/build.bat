REM @ECHO OFF
REM Build Unity from a builder script
%1 -quit -batchmode -projectPath %~dp0..\ -executeMethod VRtist.Builder.PerformBuild -logFile - %2 %3

REM Build unity from the default build settings
REM %0 -quit -batchmode -buildWindows64Player "Build/VRtist.exe" -logFile "Build/build.log"
