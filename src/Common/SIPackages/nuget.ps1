dotnet build
dotnet pack
dotnet nuget push "..\..\..\bin\SIPackages\SIPackages.7.6.0.nupkg" --api-key <...> --source https://api.nuget.org/v3/index.json