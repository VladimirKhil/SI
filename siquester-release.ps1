param (
    [string]$version = "1.0.0"
)

.\siquester-publish $version x64
.\siquester-publish $version x86
.\siquester-build-msi $version x64
.\siquester-build-msi $version x86
.\siquester-build-setup $version