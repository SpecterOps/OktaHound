<#
.SYNOPSIS
    Converts privilege zone rule files (*.json) into markdown (*.md) or MDX.
#>

#Requires -Version 5.1

[CmdletBinding()]
param (
    [Parameter(Mandatory = $false)]
    [ValidateNotNullOrEmpty()]
    [string] $InputDirectory = (Join-Path -Path $PSScriptRoot -ChildPath '../Src/PrivilegeZoneRules/'),

    [Parameter(Mandatory = $false)]
    [ValidateNotNullOrEmpty()]
    [string] $OutputFilePath = (Join-Path -Path $PSScriptRoot -ChildPath '../Documentation/PrivilegeZoneRules.md'),

    [Parameter(Mandatory = $false)]
    [ValidateNotNullOrEmpty()]
    [string] $RulesLinkPath = '../Src/PrivilegeZoneRules',

    [Parameter(Mandatory = $false)]
    [ValidateNotNullOrEmpty()]
    [string] $ExtensionName = 'OktaHound',

    [Parameter(Mandatory = $false)]
    [string] $TitlePrefix = '',

    [Parameter(Mandatory = $false)]
    [switch] $OfficialDocs
)

Set-StrictMode -Version Latest

[string] $markdown = ''
if ($OfficialDocs) {
    $markdown += @'
---
title: Privilege Zone Rules
description: "Default Privilege Zone rules for the {0} extension"
icon: "gem"
---

<img noZoom src="/assets/enterprise-AND-community-edition-pill-tag.svg" alt="Applies to BloodHound Enterprise and CE"/>

'@ -f $ExtensionName
} else {
    $markdown += @'
# Privilege Zone Rules

'@
}

$markdown += @'
The following Cypher rules define the default Privilege Zone for the {1} extension.
Each rule is defined in a JSON file located in the [PrivilegeZoneRules]({0}) directory of the {1} repository.

'@ -f $RulesLinkPath, $ExtensionName

Get-ChildItem -File -Path $InputDirectory -Filter '*.json' | Sort-Object -Property Name | ForEach-Object {
    # Parse the JSON content of the privilege zone rule file
    [psobject] $json = Get-Content -Path $PSItem.FullName | ConvertFrom-Json

    # Remove optional title prefix for cleaner headings
    [string] $title = if ([string]::IsNullOrEmpty($TitlePrefix)) {
        $json.name
    } else {
        $json.name -replace [regex]::Escape($TitlePrefix)
    }

    # Sanitize line breaks in description and cypher
    [string] $description = $json.description -replace '\n',"`n"
    [string] $cypher = $json.cypher -replace '\n',"`n"

    [string] $fileName = $PSItem.Name

    # Append file-specific markdown
    $markdown += @'

## {0}

{1}

```cypher
{2}
```

This rule is defined in the [{3}]({4}/{3}) file.

'@ -f $title, $description, $cypher, $fileName, $RulesLinkPath
}

# Normalize line endings to CRLF for Git working tree
$markdown = $markdown -replace "`r?`n", "`r`n"

Set-Content -Path $OutputFilePath -Value $markdown -Encoding UTF8 -Verbose
