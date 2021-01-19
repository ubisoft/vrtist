@ECHO OFF
REM Build Unity from a builder script
%0 -quit -batchmode -projectPath %~dp1..\ -executeMethod VRtist.Builder.PerformBuild -logFile - %*

REM Build unity from the default build settings
REM %0 -quit -batchmode -buildWindows64Player "Build/VRtist.exe" -logFile "Build/build.log"
