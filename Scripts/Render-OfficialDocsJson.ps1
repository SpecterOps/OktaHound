<#
.SYNOPSIS
    Generates docs.json navigation metadata for an extension's official docs.

.DESCRIPTION
    Scans node and edge MDX pages under Documentation/OfficialDocs/opengraph/extensions/<ExtensionName>/reference
    and writes a docs.json file containing grouped navigation entries for Overview, Nodes, and Edges.
#>

#Requires -Version 5.1

[CmdletBinding()]
param (
    [Parameter(Mandatory = $false)]
    [ValidateNotNullOrEmpty()]
    [string] $ExtensionRootDir = (Join-Path -Path $PSScriptRoot -ChildPath '../Documentation/OfficialDocs/opengraph/extensions/OktaHound'),

    [Parameter(Mandatory = $false)]
    [ValidateNotNullOrEmpty()]
    [string] $OutputPath = (Join-Path -Path $PSScriptRoot -ChildPath '../Documentation/OfficialDocs/opengraph/extensions/OktaHound/docs.json')
)

Set-StrictMode -Version Latest

if (-not (Test-Path -Path $ExtensionRootDir -PathType Container)) {
    throw "Extension directory not found: $ExtensionRootDir"
}

[string] $extensionName = Split-Path -Path $ExtensionRootDir -Leaf
[string] $nodesDir = Join-Path -Path $ExtensionRootDir -ChildPath 'reference/nodes'
[string] $edgesDir = Join-Path -Path $ExtensionRootDir -ChildPath 'reference/edges'

[string[]] $nodePages = @()
if (Test-Path -Path $nodesDir -PathType Container) {
    $nodePages = @(Get-ChildItem -Path $nodesDir -Filter '*.mdx' -File |
            Sort-Object -Property BaseName |
            ForEach-Object { "opengraph/extensions/$extensionName/reference/nodes/$($_.BaseName)" })
}

[string[]] $edgePages = @()
if (Test-Path -Path $edgesDir -PathType Container) {
    $edgePages = @(Get-ChildItem -Path $edgesDir -Filter '*.mdx' -File |
            Sort-Object -Property BaseName |
            ForEach-Object { "opengraph/extensions/$extensionName/reference/edges/$($_.BaseName)" })
}

[hashtable] $docs = [ordered]@{
    group = $extensionName
    pages = @(
        "opengraph/extensions/$extensionName/overview",
        [ordered]@{
            group = 'Reference'
            pages = @(
                [ordered]@{
                    group = 'Nodes'
                    pages = $nodePages
                },
                [ordered]@{
                    group = 'Edges'
                    pages = $edgePages
                }
            )
        }
    )
}

[string] $json = $docs | ConvertTo-Json -Depth 8
$json = $json -replace "`r?`n", "`r`n"

Set-Content -Path $OutputPath -Value $json -Encoding UTF8 -Verbose
