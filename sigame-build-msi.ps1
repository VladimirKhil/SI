param (
    [string]$platform = "x64"
)

& "$Env:PROGRAMFILES\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe" .\deploy\SIGame.Setup\SIGame.Setup.wixproj /p:Configuration=Release /p:Platform=$platform /p:OutputPath=bin/Release/$platform
