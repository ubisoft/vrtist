@ECHO OFF
REM Build Unity from a builder script
"C:\Program Files\Unity\Editor\Unity.exe" -quit -batchmode -projectPath %~dp0 -executeMethod VRtist.Builder.PerformBuild -logFile - %*

REM Build unity from the default build settings
REM "C:\Program Files\Unity\Editor\Unity.exe" -quit -batchmode -buildWindows64Player "Build/VRtist.exe" -logFile "Build/build.log"
