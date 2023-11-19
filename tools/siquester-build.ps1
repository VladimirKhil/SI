param (
    [string]$version = "1.0.0"
)

dotnet build ..\src\SIQuester\SIQuester\SIQuester.csproj -c Release /property:Version=$version