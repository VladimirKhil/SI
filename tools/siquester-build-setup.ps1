param (
    [string]$version = "1.0.0"
)

dotnet build  ..\deploy\SIQuester.Bootstrapper\SIQuester.Bootstrapper.wixproj /p:Configuration=Release /p:OutputPath=../../bin/.Release/SIQuester.Bootstrapper /p:BuildProjectReferences=false /p:MsiProductVersion=$version /t:Rebuild
