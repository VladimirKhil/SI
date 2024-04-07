param (
    [string]$version = "1.0.0",
    [string]$platform = "x64"
)

dotnet build ..\deploy\SIQuester.Setup\SIQuester.Setup.wixproj /p:Configuration=Release /p:Platform=$platform /p:MsiProductVersion=$version /p:OutputPath=../../bin/.Release/SIQuester.Setup/$platform /p:BuildProjectReferences=false
