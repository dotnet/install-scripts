@echo off
REM Runs tests filtered to a single test class on Helix (Windows).
REM Usage: helix-test-class.cmd Install_Scripts.Test.AkaMsLinksTests
REM
REM Sets TestRunnerAdditionalArguments as an environment variable so MSBuild
REM picks it up without needing %20 escaping through cmd.exe -> PowerShell -> MSBuild.

set "TestRunnerAdditionalArguments=-class %~1"
call "%~dp0..\Build.cmd" -test /p:UseMicrosoftTestingPlatformRunner=false
exit /b %ERRORLEVEL%
