param (
    [string]$version = "1.0.0",
	[string]$platform = "x64"
)

dotnet publish ..\src\SIQuester\SIQuester\SIQuester.csproj -c Release -p:PublishSingleFile=true -r win-$platform --self-contained true -p:EnableCompressionInSingleFile=true /property:Version=$version