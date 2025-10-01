param (
    [string]$version = "1.0.0",
    [string]$apikey = ""
)

dotnet pack ..\src\Common\SIPackages\SIPackages.csproj -c Release /property:Version=$version
dotnet nuget push ..\bin\.Release\SIPackages\SIPackages.$version.nupkg --api-key $apikey --source https://api.nuget.org/v3/index.json