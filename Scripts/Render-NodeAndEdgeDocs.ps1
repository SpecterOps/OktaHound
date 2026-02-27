<#
.SYNOPSIS
    Generates MDX documentation pages for nodes and edges.

.DESCRIPTION
    Reads node kinds and edge kinds from the BloodHound extension schema and
    creates one MDX file per kind under Documentation/OfficialDocs/opengraph/extensions/<ExtensionName>/reference.

    Generated files contain frontmatter and the content from Documentation/NodeDescriptions or Documentation/EdgeDescriptions.

    The following transformations are applied to the description content:
    - H1 headers are removed (the MDX frontmatter title is used instead).
    - Image paths are rewritten to point to /images/extensions/<ExtensionName>/.
    - Links to ../NodeDescriptions/ are rewritten to ../nodes/.
    - Links to other markdown files have their .md extension stripped.
    - GitHub-flavored callouts (NOTE, IMPORTANT, WARNING, TIP, CAUTION) are converted to Mintlify components.
    - Node documentation includes Inbound Edges and Outbound Edges sections (inserted between Overview and
      Properties), with tables generated from the Edge Schema sections of edge descriptions.
#>

#Requires -Version 5.1

[CmdletBinding()]
param (
    [Parameter(Mandatory = $true)]
    [ValidateNotNullOrEmpty()]
    [string] $InputPath,

    [Parameter(Mandatory = $false)]
    [ValidateNotNullOrEmpty()]
    [string] $NodeDescriptionsDir = (Join-Path -Path $PSScriptRoot -ChildPath '../Documentation/NodeDescriptions'),

    [Parameter(Mandatory = $false)]
    [ValidateNotNullOrEmpty()]
    [string] $EdgeDescriptionsDir = (Join-Path -Path $PSScriptRoot -ChildPath '../Documentation/EdgeDescriptions'),

    [Parameter(Mandatory = $false)]
    [ValidateNotNullOrEmpty()]
    [string] $OutputRootDir = (Join-Path -Path $PSScriptRoot -ChildPath '../Documentation/OfficialDocs/opengraph/extensions')
)

Set-StrictMode -Version Latest

[string] $nodeDescDirName = Split-Path -Leaf $NodeDescriptionsDir

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

            if ($rawTarget -match '^(?i:https?:|data:|/)') {
                return $match.Value
            }

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
        [string] $Markdown,

        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [string] $NodeDescDirName
    )

    [regex] $linkRegex = [regex]'(?<!!)\[([^\]]+)\]\(([^)]+)\)'

    return $linkRegex.Replace($Markdown, {
            param([System.Text.RegularExpressions.Match] $match)

            [string] $linkText = $match.Groups[1].Value
            [string] $rawTarget = $match.Groups[2].Value.Trim()

            if ($rawTarget -match '^(?i:https?:|mailto:|data:|/)') {
                return $match.Value
            }

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
            $rewrittenPath = $rewrittenPath -replace ('^\.\.\/' + [regex]::Escape($NodeDescDirName) + '/'), '../nodes/'
            return '[{0}]({1}{2})' -f $linkText, $rewrittenPath, $titleSuffix
        })
}

function Convert-Callouts {
    [OutputType([string])]
    param(
        [Parameter(Mandatory = $true)]
        [AllowEmptyString()]
        [string] $Markdown
    )

    [regex] $calloutRegex = [regex]'(?m)^> \[!(NOTE|IMPORTANT|WARNING|TIP|CAUTION)\]\r?\n(?:^> .*(?:\r?\n|$))+'

    return $calloutRegex.Replace($Markdown, {
            param([System.Text.RegularExpressions.Match] $match)

            [string] $type = $match.Groups[1].Value
            [string] $tag = switch ($type) {
                'NOTE' { 'Info' }
                'IMPORTANT' { 'Note' }
                'WARNING' { 'Warning' }
                'TIP' { 'Tip' }
                'CAUTION' { 'Danger' }
            }

            [string[]] $contentLines = $match.Value -split '\r?\n' |
                Select-Object -Skip 1 |
                Where-Object { $_ -ne '' } |
                ForEach-Object { $_ -replace '^> ?', '' }

            [string] $body = ($contentLines -join "`n").TrimEnd()
            return "<$tag>`n$body`n</$tag>"
        })
}

function Get-EdgeSchemaMap {
    [OutputType([hashtable])]
    param(
        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [string] $EdgeDescriptionsDir
    )

    [hashtable] $map = @{}
    [regex] $linkRegex = [regex]'\[([^\]]+)\]\(([^)]+)\)'

    foreach ($file in Get-ChildItem -Path $EdgeDescriptionsDir -Filter '*.md') {
        [string] $content = Get-Content -Path $file.FullName -Raw
        [string] $edgeName = $file.BaseName
        [psobject[]] $sources = @()
        [psobject[]] $destinations = @()

        if ($content -match '(?m)^- Source:\s*(.+)$') {
            $sources = @($linkRegex.Matches($matches[1]) | ForEach-Object {
                    [PSCustomObject]@{
                        Name = $_.Groups[1].Value
                        Url  = $_.Groups[2].Value
                    }
                })
        }

        if ($content -match '(?m)^- Destination:\s*(.+)$') {
            $destinations = @($linkRegex.Matches($matches[1]) | ForEach-Object {
                    [PSCustomObject]@{
                        Name = $_.Groups[1].Value
                        Url  = $_.Groups[2].Value
                    }
                })
        }

        $map[$edgeName] = [PSCustomObject]@{
            Sources      = $sources
            Destinations = $destinations
        }
    }

    return $map
}

function Format-EdgeTable {
    [OutputType([string])]
    param(
        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [string] $Heading,

        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [string] $PeerColumnName,

        [Parameter(Mandatory = $true)]
        [AllowEmptyCollection()]
        [string[]] $Rows,

        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [string] $EmptyMessage
    )

    [string] $result = "`n### $Heading`n`n"
    if ($Rows.Count -gt 0) {
        $result += "| Edge Type | $PeerColumnName |`n"
        $result += "| --------- | $('-' * $PeerColumnName.Length) |`n"
        $result += ($Rows -join "`n") + "`n"
    }
    else {
        $result += "$EmptyMessage`n"
    }

    return $result
}

function New-EdgeSectionMarkdown {
    [OutputType([string])]
    param(
        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [string] $NodeName,

        [Parameter(Mandatory = $true)]
        [hashtable] $EdgeSchemaMap
    )

    [System.Collections.Generic.List[string]] $inboundRows = [System.Collections.Generic.List[string]]::new()
    [System.Collections.Generic.List[string]] $outboundRows = [System.Collections.Generic.List[string]]::new()

    foreach ($edgeName in ($EdgeSchemaMap.Keys | Sort-Object)) {
        [psobject] $schema = $EdgeSchemaMap[$edgeName]
        [string] $edgeLink = '[{0}](../edges/{0})' -f $edgeName

        if ($NodeName -in $schema.Destinations.Name) {
            [string] $sourceLinks = ($schema.Sources | ForEach-Object {
                    '[{0}]({1})' -f $_.Name, $_.Url
                }) -join ', '
            $inboundRows.Add("| $edgeLink | $sourceLinks |")
        }

        if ($NodeName -in $schema.Sources.Name) {
            [string] $destLinks = ($schema.Destinations | ForEach-Object {
                    '[{0}]({1})' -f $_.Name, $_.Url
                }) -join ', '
            $outboundRows.Add("| $edgeLink | $destLinks |")
        }
    }

    [string] $result = "## Edges`n`n"
    $result += "<Note>`n"
    $result += "The tables below list edges defined by the $extensionName extension only. Additional edges to or from this node may be created by other extensions.`n"
    $result += "</Note>`n"

    $result += Format-EdgeTable `
        -Heading 'Inbound Edges' `
        -PeerColumnName 'Source Node Types' `
        -Rows $inboundRows `
        -EmptyMessage "No inbound edges are defined by the $extensionName extension for this node."

    $result += Format-EdgeTable `
        -Heading 'Outbound Edges' `
        -PeerColumnName 'Destination Node Types' `
        -Rows $outboundRows `
        -EmptyMessage "No outbound edges are defined by the $extensionName extension for this node."

    return $result
}

function New-OfficialDoc {
    [OutputType([void])]
    param(
        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [string] $Name,

        [Parameter(Mandatory = $true)]
        [AllowEmptyString()]
        [string] $Description,

        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [string] $DescriptionFilePath,

        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [string] $OutputFilePath,

        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [string] $ExtensionName,

        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [string] $NodeDescDirName,

        [Parameter(Mandatory = $false)]
        [AllowEmptyString()]
        [string] $IconPath,

        [Parameter(Mandatory = $false)]
        [AllowEmptyString()]
        [string] $Traversable,

        [Parameter(Mandatory = $false)]
        [AllowEmptyString()]
        [string] $EdgeSectionMarkdown
    )

    if (-not (Test-Path -Path $DescriptionFilePath -PathType Leaf)) {
        Write-Warning "Skipping ${Name}: description file not found at $DescriptionFilePath"
        return
    }

    [string] $bodyMarkdown = Get-Content -Path $DescriptionFilePath -Raw
    $bodyMarkdown = $bodyMarkdown -replace '(?m)^# .+\r?\n\r?\n', ''

    if (-not [string]::IsNullOrWhiteSpace($EdgeSectionMarkdown)) {
        $bodyMarkdown = $bodyMarkdown -replace '(?m)^(## Properties)', ($EdgeSectionMarkdown + "`n" + '$1')
    }

    if (-not [string]::IsNullOrWhiteSpace($Traversable)) {
        $bodyMarkdown = $bodyMarkdown -replace '(?m)^- Destination:.*$', "`$0`n- Traversable: $Traversable"
    }

    $bodyMarkdown = Convert-ImagePaths -Markdown $bodyMarkdown -ExtensionName $ExtensionName
    $bodyMarkdown = Convert-MarkdownLinks -Markdown $bodyMarkdown -NodeDescDirName $NodeDescDirName
    $bodyMarkdown = Convert-Callouts -Markdown $bodyMarkdown

    [string] $iconLine = ''
    if (-not [string]::IsNullOrWhiteSpace($IconPath)) {
        $iconLine = ('icon: {0}' -f (ConvertTo-YamlSingleQuoted -Value $IconPath)) + "`r`n"
    }

    [string] $mdx = @'
---
title: {0}
description: {1}
{2}---

<img noZoom src="/assets/enterprise-AND-community-edition-pill-tag.svg" alt="Applies to BloodHound Enterprise and CE"/>

{3}
'@ -f (
        (ConvertTo-YamlSingleQuoted -Value $Name),
        (ConvertTo-YamlSingleQuoted -Value $Description),
        $iconLine,
        $bodyMarkdown.TrimEnd()
    )

    $mdx = $mdx -replace "`r?`n", "`r`n"

    Set-Content -Path $OutputFilePath -Value $mdx -Encoding UTF8 -Verbose
}

# Parse extension schema
[psobject] $json = Get-Content -Path $InputPath | ConvertFrom-Json
[psobject[]] $nodeKinds = @($json.node_kinds | Sort-Object -Property name)
[psobject[]] $relationshipKinds = @($json.relationship_kinds | Sort-Object -Property name)
[string] $extensionName = [string] $json.schema.name

if ([string]::IsNullOrWhiteSpace($extensionName)) {
    throw "schema.name is missing in extension file: $InputPath"
}

[string] $referenceRoot = Join-Path -Path (Join-Path -Path $OutputRootDir -ChildPath $extensionName) -ChildPath 'reference'
[string] $nodesOutputDir = Join-Path -Path $referenceRoot -ChildPath 'nodes'
[string] $edgesOutputDir = Join-Path -Path $referenceRoot -ChildPath 'edges'

foreach ($directory in @($nodesOutputDir, $edgesOutputDir)) {
    if (-not (Test-Path -Path $directory -PathType Container)) {
        New-Item -Path $directory -ItemType Directory -Force | Out-Null
    }
}

# Parse edge schemas for node edge sections
[hashtable] $edgeSchemaMap = Get-EdgeSchemaMap -EdgeDescriptionsDir $EdgeDescriptionsDir

foreach ($nodeKind in $nodeKinds) {
    [string] $name = [string] $nodeKind.name
    [string] $description = [string] $nodeKind.description

    if ([string]::IsNullOrWhiteSpace($name)) {
        Write-Warning 'Skipping node kind with empty name.'
        continue
    }

    [string] $descriptionFilePath = Join-Path -Path $NodeDescriptionsDir -ChildPath "$name.md"
    [string] $outputFilePath = Join-Path -Path $nodesOutputDir -ChildPath "$name.mdx"
    [string] $iconPath = "/images/extensions/$extensionName/$name.png"
    [string] $edgeSectionMarkdown = New-EdgeSectionMarkdown -NodeName $name -EdgeSchemaMap $edgeSchemaMap

    New-OfficialDoc -Name $name -Description $description -DescriptionFilePath $descriptionFilePath -OutputFilePath $outputFilePath -ExtensionName $extensionName -NodeDescDirName $nodeDescDirName -IconPath $iconPath -EdgeSectionMarkdown $edgeSectionMarkdown
}

foreach ($relationshipKind in $relationshipKinds) {
    [string] $name = [string] $relationshipKind.name
    [string] $description = [string] $relationshipKind.description

    if ([string]::IsNullOrWhiteSpace($name)) {
        Write-Warning 'Skipping relationship kind with empty name.'
        continue
    }

    [string] $descriptionFilePath = Join-Path -Path $EdgeDescriptionsDir -ChildPath "$name.md"
    [string] $outputFilePath = Join-Path -Path $edgesOutputDir -ChildPath "$name.mdx"
    [string] $traversable = if ([bool] $relationshipKind.is_traversable) { '✅' } else { '❌' }

    New-OfficialDoc -Name $name -Description $description -DescriptionFilePath $descriptionFilePath -OutputFilePath $outputFilePath -ExtensionName $extensionName -NodeDescDirName $nodeDescDirName -Traversable $traversable
}
