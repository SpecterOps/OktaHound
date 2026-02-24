<#
.SYNOPSIS
    Generates MDX documentation pages for nodes and edges.

.DESCRIPTION
    Reads node kinds and edge kinds from the OktaHound BloodHound extension schema and
    creates one MDX file per kind under Documentation/OfficialDocs/opengraph/extensions/<ExtensionName>/reference.
    
    Generated files contain frontmatter and the content from Documentation/NodeDescriptions or Documentation/EdgeDescriptions.
    
    Image paths in the descriptions are rewritten to point to /images/extensions/<ExtensionName>/, 
    and links to other markdown files have their .md extension stripped.
#>

#Requires -Version 5.1

[CmdletBinding()]
param (
    [Parameter(Mandatory = $false)]
    [ValidateNotNullOrEmpty()]
    [string] $InputPath = (Join-Path -Path $PSScriptRoot -ChildPath '../Src/Extensions/bhce-okta-extension.json'),

    [Parameter(Mandatory = $false)]
    [ValidateNotNullOrEmpty()]
    [string] $NodeDescriptionsDir = (Join-Path -Path $PSScriptRoot -ChildPath '../Documentation/NodeDescriptions'),

    [Parameter(Mandatory = $false)]
    [ValidateNotNullOrEmpty()]
    [string] $EdgeDescriptionsDir = (Join-Path -Path $PSScriptRoot -ChildPath '../Documentation/EdgeDescriptions'),

    [Parameter(Mandatory = $false)]
    [ValidateNotNullOrEmpty()]
    [string] $EdgesTablePath = (Join-Path -Path $PSScriptRoot -ChildPath '../Documentation/Edges.md'),

    [Parameter(Mandatory = $false)]
    [ValidateNotNullOrEmpty()]
    [string] $OutputRootDir = (Join-Path -Path $PSScriptRoot -ChildPath '../Documentation/OfficialDocs/opengraph/extensions')
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
        [string] $Markdown
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
            return '[{0}]({1}{2})' -f $linkText, $rewrittenPath, $titleSuffix
        })
}

function Get-NodeKindsFromTableCell {
    [OutputType([string[]])]
    param(
        [Parameter(Mandatory = $true)]
        [AllowEmptyString()]
        [string] $CellValue
    )

    [System.Collections.Generic.List[string]] $nodeKinds = [System.Collections.Generic.List[string]]::new()
    [System.Text.RegularExpressions.MatchCollection] $matches = [regex]::Matches($CellValue, '\[([^\]]+)\]')

    foreach ($match in $matches) {
        [string] $nodeKind = $match.Groups[1].Value.Trim()
        if (-not [string]::IsNullOrWhiteSpace($nodeKind) -and -not $nodeKinds.Contains($nodeKind)) {
            $nodeKinds.Add($nodeKind)
        }
    }

    if ($nodeKinds.Count -eq 0) {
        [string] $fallback = ($CellValue -replace '\s+', ' ').Trim()
        if (-not [string]::IsNullOrWhiteSpace($fallback)) {
            $nodeKinds.Add($fallback)
        }
    }

    return $nodeKinds.ToArray()
}

function Get-EdgeSchemasFromDocumentation {
    [OutputType([hashtable])]
    param(
        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [string] $Path
    )

    if (-not (Test-Path -Path $Path -PathType Leaf)) {
        throw "Edges documentation file not found: $Path"
    }

    [hashtable] $edgeSchemas = @{}
    [string[]] $lines = Get-Content -Path $Path
    [string] $currentEdgeType = ''

    foreach ($line in $lines) {
        [string] $trimmedLine = $line.Trim()
        if (-not $trimmedLine.StartsWith('|')) {
            continue
        }

        [string[]] $columns = @($trimmedLine.Trim('|').Split('|') | ForEach-Object { $_.Trim() })
        if ($columns.Count -lt 4) {
            continue
        }

        [string] $edgeColumn = $columns[0]
        [string] $sourceColumn = $columns[1]
        [string] $targetColumn = $columns[2]

        if ($edgeColumn -match '^(Edge Type|-+)$') {
            continue
        }

        if ($edgeColumn -match '\[([^\]]+)\]') {
            $currentEdgeType = $matches[1].Trim()
        }

        if ([string]::IsNullOrWhiteSpace($currentEdgeType)) {
            continue
        }

        if (-not $edgeSchemas.ContainsKey($currentEdgeType)) {
            $edgeSchemas[$currentEdgeType] = @{
                Source = [System.Collections.Generic.List[string]]::new()
                Target = [System.Collections.Generic.List[string]]::new()
            }
        }

        [string[]] $sourceKinds = Get-NodeKindsFromTableCell -CellValue $sourceColumn
        [string[]] $targetKinds = Get-NodeKindsFromTableCell -CellValue $targetColumn

        foreach ($sourceKind in $sourceKinds) {
            if (-not $edgeSchemas[$currentEdgeType].Source.Contains($sourceKind)) {
                $edgeSchemas[$currentEdgeType].Source.Add($sourceKind)
            }
        }

        foreach ($targetKind in $targetKinds) {
            if (-not $edgeSchemas[$currentEdgeType].Target.Contains($targetKind)) {
                $edgeSchemas[$currentEdgeType].Target.Add($targetKind)
            }
        }
    }

    return $edgeSchemas
}

function Get-MarkdownReferenceLinks {
    [OutputType([hashtable])]
    param(
        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [string] $Path
    )

    if (-not (Test-Path -Path $Path -PathType Leaf)) {
        throw "Edges documentation file not found: $Path"
    }

    [hashtable] $referenceLinks = @{}
    [string[]] $lines = Get-Content -Path $Path

    foreach ($line in $lines) {
        [string] $trimmedLine = $line.Trim()
        if ($trimmedLine -match '^\[([^\]]+)\]:\s*(.+)$') {
            [string] $label = $matches[1].Trim()
            [string] $rawTarget = $matches[2].Trim()
            [string] $target = $rawTarget.Trim('<', '>')

            if (-not [string]::IsNullOrWhiteSpace($label) -and -not [string]::IsNullOrWhiteSpace($target)) {
                $referenceLinks[$label] = $target
            }
        }
    }

    return $referenceLinks
}

function Format-NodeKindsForEdgeSchema {
    [OutputType([string])]
    param(
        [Parameter(Mandatory = $true)]
        [string[]] $NodeKinds,

        [Parameter(Mandatory = $true)]
        [hashtable] $LocalNodeKinds,

        [Parameter(Mandatory = $true)]
        [hashtable] $ReferenceLinks
    )

    [System.Collections.Generic.List[string]] $formattedNodeKinds = [System.Collections.Generic.List[string]]::new()

    foreach ($nodeKind in $NodeKinds) {
        [string] $trimmedNodeKind = $nodeKind.Trim()
        if ([string]::IsNullOrWhiteSpace($trimmedNodeKind)) {
            continue
        }

        if ($LocalNodeKinds.ContainsKey($trimmedNodeKind)) {
            $formattedNodeKinds.Add("[$trimmedNodeKind](../nodes/$trimmedNodeKind)")
        } elseif ($ReferenceLinks.ContainsKey($trimmedNodeKind)) {
            $formattedNodeKinds.Add("[$trimmedNodeKind]($($ReferenceLinks[$trimmedNodeKind]))")
        } else {
            $formattedNodeKinds.Add($trimmedNodeKind)
        }
    }

    if ($formattedNodeKinds.Count -eq 0) {
        return 'Unknown'
    }

    return ($formattedNodeKinds -join ', ')
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

        [Parameter(Mandatory = $false)]
        [AllowEmptyString()]
        [string] $PrefixMarkdown,

        [Parameter(Mandatory = $false)]
        [AllowEmptyString()]
        [string] $IconPath
    )

    if (-not (Test-Path -Path $DescriptionFilePath -PathType Leaf)) {
        Write-Warning "Skipping ${Name}: description file not found at $DescriptionFilePath"
        return
    }

    [string] $bodyMarkdown = Get-Content -Path $DescriptionFilePath -Raw
    $bodyMarkdown = Convert-ImagePaths -Markdown $bodyMarkdown -ExtensionName $ExtensionName
    $bodyMarkdown = Convert-MarkdownLinks -Markdown $bodyMarkdown

    [string] $iconLine = ''
    if (-not [string]::IsNullOrWhiteSpace($IconPath)) {
        $iconLine = ('icon: {0}' -f (ConvertTo-YamlSingleQuoted -Value $IconPath)) + "`r`n"
    }

    [string] $prefixContent = ''
    if (-not [string]::IsNullOrWhiteSpace($PrefixMarkdown)) {
        $prefixContent = $PrefixMarkdown.TrimEnd() + "`r`n`r`n"
    }

    [string] $mdx = @'
---
title: {0}
description: {1}
{2}---

<img noZoom src="/assets/enterprise-AND-community-edition-pill-tag.svg" alt="Applies to BloodHound Enterprise and CE"/>

{3}
{4}
'@ -f (
        (ConvertTo-YamlSingleQuoted -Value $Name),
        (ConvertTo-YamlSingleQuoted -Value $Description),
        $iconLine,
        $prefixContent,
        $bodyMarkdown.TrimEnd()
    )

    $mdx = $mdx -replace "`r?`n", "`r`n"

    Set-Content -Path $OutputFilePath -Value $mdx -Encoding UTF8 -Verbose
}

# Parse extension schema
[psobject] $json = Get-Content -Path $InputPath | ConvertFrom-Json
[psobject[]] $nodeKinds = @($json.node_kinds | Sort-Object -Property name)
[psobject[]] $relationshipKinds = @($json.relationship_kinds | Sort-Object -Property name)
[hashtable] $edgeSchemas = Get-EdgeSchemasFromDocumentation -Path $EdgesTablePath
[hashtable] $referenceLinks = Get-MarkdownReferenceLinks -Path $EdgesTablePath
[string] $extensionName = [string] $json.schema.name
[hashtable] $localNodeKinds = @{}

foreach ($nodeKind in $nodeKinds) {
    [string] $nodeKindName = [string] $nodeKind.name
    if (-not [string]::IsNullOrWhiteSpace($nodeKindName)) {
        $localNodeKinds[$nodeKindName] = $true
    }
}

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

    New-OfficialDoc -Name $name -Description $description -DescriptionFilePath $descriptionFilePath -OutputFilePath $outputFilePath -ExtensionName $extensionName -IconPath $iconPath
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

    [string] $sourceKinds = 'Unknown'
    [string] $targetKinds = 'Unknown'
    if ($edgeSchemas.ContainsKey($name)) {
        if ($edgeSchemas[$name].Source.Count -gt 0) {
            $sourceKinds = Format-NodeKindsForEdgeSchema -NodeKinds $edgeSchemas[$name].Source.ToArray() -LocalNodeKinds $localNodeKinds -ReferenceLinks $referenceLinks
        }

        if ($edgeSchemas[$name].Target.Count -gt 0) {
            $targetKinds = Format-NodeKindsForEdgeSchema -NodeKinds $edgeSchemas[$name].Target.ToArray() -LocalNodeKinds $localNodeKinds -ReferenceLinks $referenceLinks
        }
    } else {
        Write-Warning "No source/target schema found for ${name} in $EdgesTablePath"
    }

    [string] $traversable = if ([bool] $relationshipKind.is_traversable) { 'Yes' } else { 'No' }
    [string] $edgeSchemaSection = @'
## Edge Schema

- Source: {0}
- Destination: {1}
- Traversable: {2}
'@ -f $sourceKinds, $targetKinds, $traversable

    New-OfficialDoc -Name $name -Description $description -DescriptionFilePath $descriptionFilePath -OutputFilePath $outputFilePath -ExtensionName $extensionName -PrefixMarkdown $edgeSchemaSection
}
