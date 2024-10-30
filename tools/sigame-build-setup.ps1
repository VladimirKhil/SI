param (
    [string]$version = "1.0.0"
)

dotnet build ..\deploy\SIGame.Bootstrapper\SIGame.Bootstrapper.wixproj /p:Configuration=Release /p:OutputPath=../../bin/.Release/SIGame.Bootstrapper /p:BuildProjectReferences=false /p:MsiProductVersion=$version /t:Rebuild
