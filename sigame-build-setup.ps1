param (
    [string]$version = "1.0.0"
)

& "$Env:PROGRAMFILES\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe" .\deploy\SIGame.Bootstrapper\SIGame.Bootstrapper.wixproj /p:Configuration=Release /p:OutputPath=../../bin/.Release/SIGame.Bootstrapper /p:BuildProjectReferences=false /p:MsiProductVersion=$version /t:Rebuild
