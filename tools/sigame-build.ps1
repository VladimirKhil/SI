param (
    [string]$version = "1.0.0"
)

dotnet build ..\src\SIGame\SIGame\SIGame.csproj -c Release /property:Version=$version