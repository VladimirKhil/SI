<#
.SYNOPSIS
Regenerates Resources.Designer.cs from Resources.resx using the standard .NET generator.

.DESCRIPTION
This script uses strongly typed resource builder to generate the C# class from a .resx file.
#>

param (
    [Parameter(Mandatory=$true)]
    [string]$ResxPath,
    [Parameter(Mandatory=$true)]
    [string]$DesignerPath,
    [string]$Namespace = "SICore.Properties",
    [string]$ClassName = "Resources"
)

Add-Type -AssemblyName System.Design
Add-Type -AssemblyName System.Windows.Forms

$provider = New-Object Microsoft.CSharp.CSharpCodeProvider
$unmatched = [string[]]::new(0)

Write-Host "Generating: $DesignerPath"
$code = [System.Resources.Tools.StronglyTypedResourceBuilder]::Create($ResxPath, $ClassName, $Namespace, $provider, $false, [ref]$unmatched)

# Write to file
$writer = New-Object System.IO.StreamWriter($DesignerPath, $false, [System.Text.Encoding]::UTF8)
$provider.GenerateCodeFromCompileUnit($code, $writer, $null)
$writer.Close()

Write-Host "Done generating $DesignerPath"
