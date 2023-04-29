param (
    [string]$version = "1.0.0",
    [string]$apikey = ""
)

dotnet build src\Common\ZipUtils\ZipUtils.csproj -c Release /property:Version=$version
dotnet pack src\Common\ZipUtils\ZipUtils.csproj -c Release /property:Version=$version
dotnet nuget push bin\.Release\ZipUtils\VKhil.ZipUtils.$version.nupkg --api-key $apikey --source https://api.nuget.org/v3/index.json