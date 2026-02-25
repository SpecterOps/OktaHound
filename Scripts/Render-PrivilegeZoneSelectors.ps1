<#
.SYNOPSIS
    Converts privilege zone selector files (*.json) into an MDX page.
#>

#Requires -Version 5.1

[CmdletBinding()]
param (
    [Parameter(Mandatory = $false)]
    [ValidateNotNullOrEmpty()]
    [string] $InputDirectory = (Join-Path -Path $PSScriptRoot -ChildPath '../Documentation/PrivilegeZoneSelectors/'),

    [Parameter(Mandatory = $false)]
    [ValidateNotNullOrEmpty()]
    [string] $OutputFilePath = (Join-Path -Path $PSScriptRoot -ChildPath '../Documentation/OfficialDocs/opengraph/extensions/OktaHound/reference/privilege-zone-selectors.mdx')
)

Set-StrictMode -Version Latest

[string] $markdown = @'
---
title: Privilege Zone Selectors
description: "Default Privilege Zone selectors for the OktaHound extension"
icon: "gem"
---

The following Cypher selectors define the default Privilege Zone for the OktaHound extension.
Each selector is defined in a JSON file located in the [PrivilegeZoneSelectors](https://github.com/SpecterOps/OktaHound/tree/main/Documentation/PrivilegeZoneSelectors) directory of the OktaHound repository.

'@

Get-ChildItem -File -Path $InputDirectory -Filter '*.json' | Sort-Object -Property Name | ForEach-Object {
    # Parse the JSON content of the privilege zone selector file
    [psobject] $json = Get-Content -Path $PSItem.FullName | ConvertFrom-Json

    # Remove 'Okta: ' prefix from title for cleaner headings
    [string] $title = $json.name -replace 'Okta: '

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

This selector is defined in the [{3}](https://github.com/SpecterOps/OktaHound/tree/main/Documentation/PrivilegeZoneSelectors/{3}) file.

'@ -f $title, $description, $cypher, $fileName
}

# Normalize line endings to CRLF for Git working tree
$markdown = $markdown -replace "`r?`n", "`r`n"

Set-Content -Path $OutputFilePath -Value $markdown -Encoding UTF8 -Verbose
