<#
.SYNOPSIS
    Generates MDX documentation pages for extension node kinds.

.DESCRIPTION
    Reads node kinds from the OktaHound BloodHound extension schema and creates one MDX file
    per node under Documentation/OfficialDocs. Each generated file contains frontmatter
    with title, description, and icon path, followed by the content from Src/NodeDescriptions.
#>

#Requires -Version 5.1

[CmdletBinding()]
param (
    [Parameter(Mandatory = $false)]
    [ValidateNotNullOrEmpty()]
    [string] $InputPath = (Join-Path -Path $PSScriptRoot -ChildPath '../Src/Extensions/bhce-okta-extension.json'),

    [Parameter(Mandatory = $false)]
    [ValidateNotNullOrEmpty()]
    [string] $NodeDescriptionsDir = (Join-Path -Path $PSScriptRoot -ChildPath '../Src/NodeDescriptions'),

    [Parameter(Mandatory = $false)]
    [ValidateNotNullOrEmpty()]
    [string] $OutputDir = (Join-Path -Path $PSScriptRoot -ChildPath '../Documentation/OfficialDocs/opengraph/extensions/OktaHound/reference/nodes')
)

Set-StrictMode -Version Latest

function ConvertTo-YamlSingleQuoted {
    [OutputType([string])]
    param(
        [Parameter(Mandatory = $true)]
        [AllowEmptyString()]
        [string] $Value
    )

    return "'" + $Value.Replace("'", "''") + "'"
}

function Convert-ImagePaths {
    [OutputType([string])]
    param(
        [Parameter(Mandatory = $true)]
        [AllowEmptyString()]
        [string] $Markdown,

        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [string] $ExtensionName
    )

    [regex] $imageRegex = [regex]'!\[([^\]]*)\]\(([^)]+)\)'

    return $imageRegex.Replace($Markdown, {
            param([System.Text.RegularExpressions.Match] $match)

            [string] $altText = $match.Groups[1].Value
            [string] $rawTarget = $match.Groups[2].Value.Trim()

            # Preserve remote and root-relative URLs.
            if ($rawTarget -match '^(?i:https?:|data:|/)') {
                return $match.Value
            }

            # Preserve optional quoted title in markdown image syntax: ![alt](path "title")
            [string] $imagePath = $rawTarget
            [string] $titleSuffix = ''
            if ($rawTarget -match '^(\S+)\s+(("[^"]*")|(\''[^\'']*\''))$') {
                $imagePath = $matches[1]
                $titleSuffix = ' ' + $matches[2]
            }

            [string] $pathWithoutQuery = ($imagePath -split '[?#]', 2)[0]
            [string] $fileName = [System.IO.Path]::GetFileName($pathWithoutQuery)

            if ([string]::IsNullOrWhiteSpace($fileName)) {
                return $match.Value
            }

            return '![{0}](/images/extensions/{1}/{2}{3})' -f $altText, $ExtensionName, $fileName, $titleSuffix
        })
}

function Convert-MarkdownLinks {
    [OutputType([string])]
    param(
        [Parameter(Mandatory = $true)]
        [AllowEmptyString()]
        [string] $Markdown
    )

    [regex] $linkRegex = [regex]'(?<!!)\[([^\]]+)\]\(([^)]+)\)'

    return $linkRegex.Replace($Markdown, {
            param([System.Text.RegularExpressions.Match] $match)

            [string] $linkText = $match.Groups[1].Value
            [string] $rawTarget = $match.Groups[2].Value.Trim()

            # Preserve remote, mailto/data, and root-relative URLs.
            if ($rawTarget -match '^(?i:https?:|mailto:|data:|/)') {
                return $match.Value
            }

            # Preserve optional quoted title in markdown link syntax: [text](path "title")
            [string] $linkPath = $rawTarget
            [string] $titleSuffix = ''
            if ($rawTarget -match '^(\S+)\s+(("[^"]*")|(\''[^\'']*\''))$') {
                $linkPath = $matches[1]
                $titleSuffix = ' ' + $matches[2]
            }

            [string] $pathWithoutQuery = ($linkPath -split '[?#]', 2)[0]
            if ($pathWithoutQuery -notmatch '\.md$') {
                return $match.Value
            }

            [string] $rewrittenPath = $linkPath -replace '\.md(?=($|[?#]))', ''
            return '[{0}]({1}{2})' -f $linkText, $rewrittenPath, $titleSuffix
        })
}

# Parse extension schema
[psobject] $json = Get-Content -Path $InputPath | ConvertFrom-Json
[psobject[]] $nodeKinds = @($json.node_kinds | Sort-Object -Property name)
[string] $extensionName = [string] $json.schema.name

if ($nodeKinds.Count -eq 0) {
    throw "No node_kinds found in extension file: $InputPath"
}

if ([string]::IsNullOrWhiteSpace($extensionName)) {
    throw "schema.name is missing in extension file: $InputPath"
}

# Ensure output directory exists
if (-not (Test-Path -Path $OutputDir -PathType Container)) {
    New-Item -Path $OutputDir -ItemType Directory -Force | Out-Null
}

foreach ($nodeKind in $nodeKinds) {
    [string] $nodeName = [string] $nodeKind.name
    [string] $nodeDescription = [string] $nodeKind.description

    if ([string]::IsNullOrWhiteSpace($nodeName)) {
        Write-Warning 'Skipping node kind with empty name.'
        continue
    }

    [string] $descriptionFilePath = Join-Path -Path $NodeDescriptionsDir -ChildPath "$nodeName.md"
    [string] $outputFilePath = Join-Path -Path $OutputDir -ChildPath "$nodeName.mdx"
    [string] $iconPathForFrontmatter = "/images/extensions/$extensionName/$nodeName.png"

    if (-not (Test-Path -Path $descriptionFilePath -PathType Leaf)) {
        Write-Warning "Skipping ${nodeName}: node description file not found at $descriptionFilePath"
        continue
    }

    [string] $nodeDescriptionMarkdown = Get-Content -Path $descriptionFilePath -Raw
    $nodeDescriptionMarkdown = Convert-ImagePaths -Markdown $nodeDescriptionMarkdown -ExtensionName $extensionName
    $nodeDescriptionMarkdown = Convert-MarkdownLinks -Markdown $nodeDescriptionMarkdown

    [string] $mdx = @'
---
title: {0}
description: {1}
icon: {2}
---

<img noZoom src="/assets/enterprise-AND-community-edition-pill-tag.svg" alt="Applies to BloodHound Enterprise and CE"/>

{3}
'@ -f (
        (ConvertTo-YamlSingleQuoted -Value $nodeName),
        (ConvertTo-YamlSingleQuoted -Value $nodeDescription),
        (ConvertTo-YamlSingleQuoted -Value $iconPathForFrontmatter),
        $nodeDescriptionMarkdown.TrimEnd()
    )

    # Normalize line endings to CRLF for Git working tree
    $mdx = $mdx -replace "`r?`n", "`r`n"

    Set-Content -Path $outputFilePath -Value $mdx -Encoding UTF8 -Verbose
}
