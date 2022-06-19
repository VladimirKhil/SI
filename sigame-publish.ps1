param (
    [string]$version = "1.0.0"
)

dotnet publish src\SIGame\SIGame\SIGame.csproj -c Release -p:PublishSingleFile=true -r win-x64 --self-contained true -p:EnableCompressionInSingleFile=true /property:Version=$version
dotnet publish src\SIGame\SIGame\SIGame.csproj -c Release -p:PublishSingleFile=true -r win-x86 --self-contained true -p:EnableCompressionInSingleFile=true /property:Version=$version