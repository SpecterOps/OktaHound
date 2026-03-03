<#
.SYNOPSIS
    Generates all files required for the official BloodHound extension documentation.

.DESCRIPTION
    Orchestrates the full official docs build by running each render script in the
    correct order and copying static image assets to the output directory.
#>

#Requires -Version 5.1

[CmdletBinding()]
[OutputType([void])]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

[string] $repoRoot = Join-Path -Path $PSScriptRoot -ChildPath '..'
[string] $officialDocsDir = Join-Path -Path $repoRoot -ChildPath 'Documentation/OfficialDocs'
[string] $imagesOutputDirRelPath = '/images/extensions/OktaHound/reference'
[string] $imagesOutputDirFullPath = Join-Path -Path $officialDocsDir -ChildPath $imagesOutputDirRelPath
[string] $opengraphRefDir = Join-Path -Path $officialDocsDir -ChildPath 'opengraph/extensions/OktaHound/reference'

# Step 0: Clean the output directory
Write-Host '== Step 0: Cleaning output directory ==' -ForegroundColor Cyan
if (Test-Path -Path $officialDocsDir -PathType Container) {
    Get-ChildItem -Path $officialDocsDir | Remove-Item -Recurse -Force -Verbose
}
New-Item -Path $imagesOutputDirFullPath -ItemType Directory -Force | Out-Null
New-Item -Path $opengraphRefDir -ItemType Directory -Force | Out-Null

# Step 1: Render custom node icons into the official docs images directory
Write-Host '== Step 1: Rendering custom node icons ==' -ForegroundColor Cyan
[string] $packageCachePath = Join-Path -Path ([System.IO.Path]::GetTempPath()) -ChildPath 'BloodHound-IconRender'
& (Join-Path -Path $PSScriptRoot -ChildPath 'Render-CustomNodeIcons.ps1') -OutputDir $imagesOutputDirFullPath -PackageCachePath $packageCachePath

# Step 2: Copy static images from Documentation/Images to the official docs images directory
Write-Host '== Step 2: Copying static images ==' -ForegroundColor Cyan
[string] $sourceImagesDir = Join-Path -Path $repoRoot -ChildPath 'Documentation/Images'
Copy-Item -Path (Join-Path -Path $sourceImagesDir -ChildPath '*') -Destination $imagesOutputDirFullPath -Force -Verbose

# Step 3: Render custom queries MDX
Write-Host '== Step 3: Rendering custom queries ==' -ForegroundColor Cyan
[string] $queriesOutputPath = Join-Path -Path $opengraphRefDir -ChildPath 'queries.mdx'
& (Join-Path -Path $PSScriptRoot -ChildPath 'Render-CustomQueries.ps1') -OutputFilePath $queriesOutputPath -QueriesPath 'https://github.com/SpecterOps/OktaHound/tree/main/Src/Queries' -OfficialDocs

# Step 4: Render privilege zone selectors MDX
Write-Host '== Step 4: Rendering privilege zone selectors ==' -ForegroundColor Cyan
[string] $privilegeZonePath = Join-Path -Path $opengraphRefDir -ChildPath 'privilege-zone-selectors.mdx'
& (Join-Path -Path $PSScriptRoot -ChildPath 'Render-PrivilegeZoneSelectors.ps1') -OutputFilePath $privilegeZonePath -SelectorsLinkPath 'https://github.com/SpecterOps/OktaHound/tree/main/Src/PrivilegeZoneSelectors' -OfficialDocs

# Step 5: Render node and edge documentation MDX files
Write-Host '== Step 5: Rendering node and edge docs ==' -ForegroundColor Cyan
& (Join-Path -Path $PSScriptRoot -ChildPath 'Render-NodeAndEdgeDocs.ps1') -InputPath (Join-Path -Path $repoRoot -ChildPath 'Src/Extensions/bhce-okta-extension.json') -IconBasePath $imagesOutputDirRelPath

# Step 6: Render schema MDX
Write-Host '== Step 6: Rendering schema ==' -ForegroundColor Cyan
[string] $schemaOutputPath = Join-Path -Path $opengraphRefDir -ChildPath 'schema.mdx'
& (Join-Path -Path $PSScriptRoot -ChildPath 'Render-Schema.ps1') -OutputPath $schemaOutputPath -NodeLinkBasePath 'nodes' -EdgeLinkBasePath 'edges' -IconBasePath $imagesOutputDirRelPath -OfficialDocs

# Step 7: Render official docs navigation JSON
Write-Host '== Step 7: Rendering docs.json ==' -ForegroundColor Cyan
& (Join-Path -Path $PSScriptRoot -ChildPath 'Render-OfficialDocsJson.ps1')

Write-Host '== Done ==' -ForegroundColor Green
