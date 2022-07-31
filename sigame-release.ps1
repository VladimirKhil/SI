param (
    [string]$version = "1.0.0"
)

.\sigame-publish $version x64
.\sigame-publish $version x86
.\sigame-build-msi $version x64
.\sigame-build-msi $version x86
.\sigame-build-setup $version