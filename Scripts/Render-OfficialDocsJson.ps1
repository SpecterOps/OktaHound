<#
.SYNOPSIS
    Generates docs.json navigation metadata for an extension's official docs.

.DESCRIPTION
    Scans MDX pages under Documentation/OfficialDocs/opengraph/extensions/<ExtensionName>
    and writes a docs.json file with grouped navigation entries.

    The images/ directory and docs.json itself are ignored.
#>

#Requires -Version 5.1

[CmdletBinding()]
param (
    [Parameter(Mandatory = $false)]
    [ValidateNotNullOrEmpty()]
    [string] $ExtensionRootDir = (Join-Path -Path $PSScriptRoot -ChildPath '../Documentation/OfficialDocs/opengraph/extensions/OktaHound'),

    [Parameter(Mandatory = $false)]
    [ValidateNotNullOrEmpty()]
    [string] $OutputPath = (Join-Path -Path $PSScriptRoot -ChildPath '../Documentation/OfficialDocs/docs.json')
)

Set-StrictMode -Version Latest

if (-not (Test-Path -Path $ExtensionRootDir -PathType Container)) {
    throw "Extension directory not found: $ExtensionRootDir"
}

[string] $extensionName = Split-Path -Path $ExtensionRootDir -Leaf
[string] $referenceDir = Join-Path -Path $ExtensionRootDir -ChildPath 'reference'
[string] $nodesDir = Join-Path -Path $referenceDir -ChildPath 'nodes'
[string] $edgesDir = Join-Path -Path $referenceDir -ChildPath 'edges'

# MDX files directly in the extension root (e.g. schema.mdx)
[string[]] $rootPages = @(Get-ChildItem -Path $ExtensionRootDir -Filter '*.mdx' -File |
        Sort-Object -Property BaseName |
        ForEach-Object { "opengraph/extensions/$extensionName/$($_.BaseName)" })

# MDX files directly in reference/ but not in nodes/ or edges/ (e.g. queries.mdx, privilege-zone-selectors.mdx)
[string[]] $referencePages = @()
if (Test-Path -Path $referenceDir -PathType Container) {
    $referencePages = @(Get-ChildItem -Path $referenceDir -Filter '*.mdx' -File |
            Sort-Object -Property BaseName |
            ForEach-Object { "opengraph/extensions/$extensionName/reference/$($_.BaseName)" })
}

# MDX files in reference/nodes/
[string[]] $nodePages = @()
if (Test-Path -Path $nodesDir -PathType Container) {
    $nodePages = @(Get-ChildItem -Path $nodesDir -Filter '*.mdx' -File |
            Sort-Object -Property BaseName |
            ForEach-Object { "opengraph/extensions/$extensionName/reference/nodes/$($_.BaseName)" })
}

# MDX files in reference/edges/
[string[]] $edgePages = @()
if (Test-Path -Path $edgesDir -PathType Container) {
    $edgePages = @(Get-ChildItem -Path $edgesDir -Filter '*.mdx' -File |
            Sort-Object -Property BaseName |
            ForEach-Object { "opengraph/extensions/$extensionName/reference/edges/$($_.BaseName)" })
}

$docs = [ordered]@{
    group = $extensionName
    pages = @(
        $rootPages +
        @(
            [ordered]@{
                group = 'Reference'
                pages = @(
                    $referencePages +
                    @(
                        [ordered]@{
                            group = 'Nodes'
                            pages = $nodePages
                        },
                        [ordered]@{
                            group = 'Edges'
                            pages = $edgePages
                        }
                    )
                )
            }
        )
    )
}

[string] $json = $docs | ConvertTo-Json -Depth 8
$json = $json -replace "`r?`n", "`r`n"

Set-Content -Path $OutputPath -Value $json -Encoding UTF8 -Verbose
