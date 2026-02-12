<#
 .SYNOPSIS
     Renders PNG icons for node kinds defined in a BloodHound extension schema file.

.DESCRIPTION
    Reads a BloodHound extension schema JSON file and generates PNG icons in the Icons directory.
    Each PNG has a transparent background, a filled circle in the node color with
    a 2px black border, and a black Font Awesome icon centered inside the circle.

.PARAMETER ImageSize
    The width and height, in pixels, for the generated PNG icons. Default is 128.

.PARAMETER IconScale
    The relative scale for the Font Awesome icon within the circle. Default is 0.55.

.PARAMETER InputFile
    The path to the JSON schema file. Default is the hardcoded main extension file.

.PARAMETER OutputDir
    The directory where PNG icons are written. Default is the Icons directory.

.PARAMETER PackageCachePath
    The directory where NuGet packages are cached. Default is the BloodHound-IconRender
    subdirectory under the temp directory.
.NOTES
    Author: Michael Grafnetter
    Version: 3.1
#>

#requires -Version 7

[CmdletBinding()]
[OutputType([void])]
param(
    [Parameter(Mandatory = $false)]
    [ValidateNotNullOrEmpty()]
    [string] $InputFile = (Join-Path -Path $PSScriptRoot -ChildPath '../Src/Extensions/bhce-okta-extension.json'),

    [Parameter(Mandatory = $false)]
    [ValidateNotNullOrEmpty()]
    [string] $OutputDir = (Join-Path -Path $PSScriptRoot -ChildPath '../Documentation/Icons'),

    [Parameter(Mandatory = $false)]
    [ValidateRange(16, 512)]
    [int] $ImageSize = 32,

    [Parameter(Mandatory = $false)]
    [ValidateRange(0.1, 1.0)]
    [double] $IconScale = 0.55,

    [Parameter(Mandatory = $false)]
    [ValidateNotNullOrEmpty()]
    [string] $PackageCachePath = (Join-Path -Path $env:TEMP -ChildPath 'BloodHound-IconRender')
)

Set-StrictMode -Version Latest

class NodeDefinition {
    [string] $NodeName
    [string] $IconName
    [string] $IconColor
    [string] $IconType

    NodeDefinition([string] $nodeName, [string] $iconName, [string] $iconColor, [string] $iconType) {
        $this.NodeName = $nodeName
        $this.IconName = $iconName
        $this.IconColor = $iconColor
        $this.IconType = $iconType
    }
}

<#
.SYNOPSIS
    Main entry point for the script.
#>
function Main {
    # Download and import dependencies from NuGet
    Import-SkiaDependencies -CacheRoot $PackageCachePath

    if (-not ('SkiaSharp.SKBitmap' -as [type])) {
        throw 'SkiaSharp types are not available. Ensure SkiaSharp dependencies loaded correctly.'
    }

    if (-not ('Svg.Skia.SKSvg' -as [type])) {
        throw 'Svg.Skia types are not available. Ensure Svg.Skia dependencies loaded correctly.'
    }

    # Create output directory if it doesn't exist
    if (-not (Test-Path -Path $OutputDir -PathType Container)) {
        New-Item -Path $OutputDir -ItemType Directory -Force | Out-Null
    }

    # Parse the JSON file
    [psobject] $json = Get-Content -Path $InputFile | ConvertFrom-Json
    [NodeDefinition[]] $nodeDefinitions = Get-NodeDefinitions -Json $json

    # Generate PNG icons for each node kind
    foreach ($nodeDefinition in $nodeDefinitions) {
        [string] $nodeName = $nodeDefinition.NodeName
        [string] $iconName = $nodeDefinition.IconName
        [string] $iconColor = $nodeDefinition.IconColor
        [string] $iconType = $nodeDefinition.IconType

        if ([string]::IsNullOrWhiteSpace($iconName)) {
            Write-Warning "Skipping ${nodeName}`: icon name is missing."
            continue
        }

        if ([string]::IsNullOrWhiteSpace($iconColor)) {
            Write-Warning "Skipping ${nodeName}`: icon color is missing."
            continue
        }

        if (-not [string]::IsNullOrWhiteSpace($iconColor)) {
            $iconColor = Normalize-IconColor -Color $iconColor
        }

        New-NodeIcon -NodeName $nodeName -Icon $iconName -Color $iconColor -IconType $iconType -OutputDir $OutputDir -ImageSize $ImageSize -IconScale $IconScale
    }
}

<#
.SYNOPSIS
    Normalizes node definitions from supported extension formats.

.PARAMETER Json
    The parsed JSON object.

.OUTPUTS
    Array of objects with Name, IconName, IconColor, and IconType properties.
#>
function Get-NodeDefinitions {
    [OutputType([NodeDefinition])]
    param(
        [Parameter(Mandatory = $true)]
        [psobject] $Json
    )
<#
New extension format:
{
  "node_kinds": [
    {
      "name": "AZ_Tenant",
      "display_name": "Azure Tenant",
      "description": "An Azure tenant environment",
      "is_display_kind": "true",
      "icon": "cloud",
      "color": "0xFF00FF"
    }
  ]
}
#>
    if ($null -ne $Json.PSObject.Properties['node_kinds']) {
        foreach ($node in $Json.node_kinds) {
            [NodeDefinition]::new($node.name, $node.icon, $node.color, 'font-awesome')
        }
        return
    }
<#
Old custom types format:
{
    "custom_types": {
        "OktaOrganization": {
            "icon": {
                "color": "#16a5a5",
                "name": "globe",
                "type": "font-awesome"
            }
        }
    }
}
#>
    if ($null -ne $Json.PSObject.Properties['custom_types']) {
        [string[]] $nodeNames = $Json.custom_types | Get-Member -MemberType NoteProperty | Select-Object -ExpandProperty Name
        foreach ($nodeName in $nodeNames) {
            [psobject] $nodeDefinition = $Json.custom_types.$nodeName
            [NodeDefinition]::new($nodeName, $nodeDefinition.icon.name, $nodeDefinition.icon.color, $nodeDefinition.icon.type)
        }
        return
    }

    throw 'Unsupported schema format: expected node_kinds or custom_types.'
}

<#
.SYNOPSIS
    Sanitizes a string to be safe for use as a filename by replacing invalid characters with underscores.
    This is used to generate valid filenames for node icons based on their names.

.PARAMETER Name
    The raw node name to sanitize into a safe filename.

.OUTPUTS
    The sanitized filename string.
#>
function Get-SafeFileName {
    [OutputType([string])]
    param(
        [Parameter(Mandatory = $true)]
        [string] $Name
    )

    [char[]] $invalidChars = [IO.Path]::GetInvalidFileNameChars()
    foreach ($char in $invalidChars) {
        $Name = $Name -replace [Regex]::Escape($char), '_'
    }

    return $Name
}

<#
.SYNOPSIS
    Normalizes a hex color string to a format understood by SkiaSharp.

.PARAMETER Color
    A hex color string (e.g., 0x3B82F6, #3B82F6, 3B82F6, or AARRGGBB).

.OUTPUTS
    A normalized hex color string prefixed with #, or $null if invalid.
#>
function Normalize-IconColor {
    [OutputType([string])]
    param(
        [Parameter(Mandatory = $true)]
        [string] $Color
    )

    [string] $trimmed = $Color.Trim()
    if ($trimmed.StartsWith('0x')) {
        $trimmed = $trimmed.Substring(2)
    }

    if ($trimmed.StartsWith('#')) {
        $trimmed = $trimmed.Substring(1)
    }

    if ($trimmed -match '^[0-9a-fA-F]{6}$') {
        return "#$trimmed"
    }

    if ($trimmed -match '^[0-9a-fA-F]{8}$') {
        return "#$trimmed"
    }

    return $null
}

<#
.SYNOPSIS
    Downloads a NuGet package and extracts it to a cache directory if not already present.

.PARAMETER Name
    The NuGet package ID.

.PARAMETER Version
    The NuGet package version.

.PARAMETER CacheRoot
    The directory where packages are cached and extracted.

.OUTPUTS
    The root directory where the NuGet package is extracted.
#>
function Get-NuGetPackage {
    [OutputType([string])]
    param(
        [Parameter(Mandatory = $true)]
        [string] $Name,

        [Parameter(Mandatory = $true)]
        [string] $Version,

        [Parameter(Mandatory = $true)]
        [string] $CacheRoot
    )

    [string] $packageRoot = Join-Path -Path $CacheRoot -ChildPath "$Name.$Version"
    [string] $nupkgPath = Join-Path -Path $CacheRoot -ChildPath "$Name.$Version.nupkg"

    # Check if the package is already extracted by looking for any .dll files in the package root
    [bool] $needsExtract = $true
    if (Test-Path -Path $packageRoot -PathType Container) {
        $needsExtract = -not (Get-ChildItem -Path $packageRoot -Recurse -Filter '*.dll' -ErrorAction SilentlyContinue | Select-Object -First 1)
    }

    if ($needsExtract) {
        # Download the .nupkg file if it doesn't exist in the cache
        if (-not (Test-Path -Path $nupkgPath -PathType Leaf)) {
            [string] $lowerName = $Name.ToLowerInvariant()
            [string] $lowerVersion = $Version.ToLowerInvariant()
            [string] $downloadUrl = "https://api.nuget.org/v3-flatcontainer/$lowerName/$lowerVersion/$lowerName.$lowerVersion.nupkg"
            Invoke-WebRequest -Uri $downloadUrl -OutFile $nupkgPath -UseBasicParsing -ErrorAction Stop
        }

        # Remove existing package root if it exists to ensure a clean extraction
        if (Test-Path -Path $packageRoot -PathType Container) {
            Remove-Item -Path $packageRoot -Recurse -Force
        }

        # Extract the .nupkg file to the package root directory
        New-Item -Path $packageRoot -ItemType Directory -Force | Out-Null
        Add-Type -AssemblyName System.IO.Compression.FileSystem
        [IO.Compression.ZipFile]::ExtractToDirectory($nupkgPath, $packageRoot)
    }

    return $packageRoot
}

<#
.SYNOPSIS
    Downloads and loads a managed assembly from a NuGet package.

.PARAMETER PackageName
    The NuGet package ID.

.PARAMETER PackageVersion
    The NuGet package version.

.PARAMETER TargetFrameworkMoniker
    The target framework moniker to prefer when selecting assemblies.

.PARAMETER AssemblyName
    The assembly name to locate and load.

.PARAMETER CacheRoot
    The directory where packages are cached and extracted.

.OUTPUTS
    The full path to the loaded assembly DLL.
#>
function Import-NuGetLibrary {
    [OutputType([string])]
    param(
        [Parameter(Mandatory = $true)]
        [string] $PackageName,

        [Parameter(Mandatory = $true)]
        [string] $PackageVersion,

        [Parameter(Mandatory = $true)]
        [string] $TargetFrameworkMoniker,

        [Parameter(Mandatory = $true)]
        [string] $AssemblyName,

        [Parameter(Mandatory = $true)]
        [string] $CacheRoot
    )

    [string] $packageRoot = Get-NuGetPackage -Name $PackageName -Version $PackageVersion -CacheRoot $CacheRoot
    [System.IO.FileInfo] $assemblyInfo = Get-ChildItem -Path $packageRoot -Recurse -Filter "$AssemblyName.dll" -ErrorAction SilentlyContinue |
        Where-Object { $PSItem.FullName -notmatch '\\ref\\' } |
        Sort-Object { $PSItem.FullName -like "*lib*$TargetFrameworkMoniker*" } -Descending |
        Select-Object -First 1

    if ($null -eq $assemblyInfo) {
        throw "$AssemblyName.dll not found under $packageRoot."
    }

    Add-Type -Path $assemblyInfo.FullName -ErrorAction Stop | Out-Null

    return $assemblyInfo.FullName
}

<#
.SYNOPSIS
    Imports SkiaSharp and Svg.Skia dependencies by downloading them from NuGet if necessary and loading the assemblies.
    This ensures that the required types for rendering icons are available in the script.
#>
function Import-SkiaDependencies {
    [OutputType([void])]
    param(
        [Parameter(Mandatory = $true)]
        [string] $CacheRoot
    )

    New-Item -Path $CacheRoot -ItemType Directory -Force | Out-Null

    [string] $targetFrameworkMoniker = 'netstandard2.0'

    [string] $skiaDll = Import-NuGetLibrary -PackageName 'SkiaSharp' -PackageVersion '2.88.9' -TargetFrameworkMoniker $targetFrameworkMoniker -AssemblyName 'SkiaSharp' -CacheRoot $CacheRoot
    [string] $shimSkiaDll = Import-NuGetLibrary -PackageName 'ShimSkiaSharp' -PackageVersion '3.4.1' -TargetFrameworkMoniker $targetFrameworkMoniker -AssemblyName 'ShimSkiaSharp' -CacheRoot $CacheRoot
    [string] $exCssDll = Import-NuGetLibrary -PackageName 'ExCSS' -PackageVersion '4.3.1' -TargetFrameworkMoniker $targetFrameworkMoniker -AssemblyName 'ExCSS' -CacheRoot $CacheRoot
    [string] $svgCustomDll = Import-NuGetLibrary -PackageName 'Svg.Custom' -PackageVersion '3.4.1' -TargetFrameworkMoniker $targetFrameworkMoniker -AssemblyName 'Svg.Custom' -CacheRoot $CacheRoot
    [string] $svgModelDll = Import-NuGetLibrary -PackageName 'Svg.Model' -PackageVersion '3.4.1' -TargetFrameworkMoniker $targetFrameworkMoniker -AssemblyName 'Svg.Model' -CacheRoot $CacheRoot
    [string] $svgDll = Import-NuGetLibrary -PackageName 'Svg.Skia' -PackageVersion '3.4.1' -TargetFrameworkMoniker $targetFrameworkMoniker -AssemblyName 'Svg.Skia' -CacheRoot $CacheRoot

    [string] $nativeRoot = Get-NuGetPackage -Name 'SkiaSharp.NativeAssets.Win32' -Version '2.88.9' -CacheRoot $CacheRoot

    [string] $architecture = $env:PROCESSOR_ARCHITECTURE
    [string] $nativePath = switch ($architecture) {
        'ARM64' { Join-Path -Path $nativeRoot -ChildPath 'runtimes/win-arm64/native' }
        'x86' { Join-Path -Path $nativeRoot -ChildPath 'runtimes/win-x86/native' }
        'AMD64' { Join-Path -Path $nativeRoot -ChildPath 'runtimes/win-x64/native' }
        default { Join-Path -Path $nativeRoot -ChildPath 'runtimes/win-x64/native' }
    }

    if (-not (Test-Path -Path $nativePath -PathType Container)) {
        throw "SkiaSharp native assets not found for architecture '$architecture' in $nativeRoot."
    }

    if (-not ($env:PATH -split ';' | Where-Object { $PSItem -eq $nativePath })) {
        $env:PATH = "$nativePath;$env:PATH"
    }
}

<#
.SYNOPSIS
    Downloads and parses a Font Awesome SVG icon as a SkiaSharp picture.

.PARAMETER IconName
    The Font Awesome icon name (solid style) to fetch and parse.

.OUTPUTS
    A SkiaSharp picture representing the SVG icon.
#>
function Get-FontAwesomeIcon {
    [OutputType([SkiaSharp.SKPicture])]
    param(
        [Parameter(Mandatory = $true)]
        [string] $IconName
    )

    [string] $svgUrl = "https://raw.githubusercontent.com/FortAwesome/Font-Awesome/refs/heads/7.x/svgs/solid/$IconName.svg?sanitize=true"
    [string] $svgContent = (Invoke-WebRequest -Uri $svgUrl -UseBasicParsing -ErrorAction Stop).Content

    [Svg.Skia.SKSvg] $svg = [Svg.Skia.SKSvg]::new()
    [System.IO.MemoryStream] $svgStream = [System.IO.MemoryStream]::new([System.Text.Encoding]::UTF8.GetBytes($svgContent))
    try {
        $svg.Load($svgStream) | Out-Null
    }
    finally {
        $svgStream.Dispose()
    }

    return $svg.Picture
}

<#
.SYNOPSIS
    Writes a SkiaSharp bitmap to a PNG file.

.PARAMETER Bitmap
    The SkiaSharp bitmap containing the rendered icon.

.PARAMETER OutputFile
    The full path to the output PNG file.
#>
function Write-NodeIconPng {
    [OutputType([void])]
    param(
        [Parameter(Mandatory = $true)]
        [SkiaSharp.SKBitmap] $Bitmap,

        [Parameter(Mandatory = $true)]
        [string] $OutputFile
    )

    [SkiaSharp.SKImage] $image = [SkiaSharp.SKImage]::FromBitmap($Bitmap)
    try {
        [SkiaSharp.SKData] $data = $image.Encode([SkiaSharp.SKEncodedImageFormat]::Png, 100)
        try {
            [byte[]] $bytes = $data.ToArray()
            [System.IO.File]::WriteAllBytes($OutputFile, $bytes)
        }
        finally {
            $data.Dispose()
        }
    }
    finally {
        $image.Dispose()
    }
}

<#
.SYNOPSIS
    Generates a PNG icon for a single node definition.

.PARAMETER NodeName
    The node name to use for the output filename.

 .PARAMETER Icon
     The icon name to render (Font Awesome icon name when using font-awesome type).

 .PARAMETER Color
     The icon background color in hex (e.g., 0x3B82F6).

 .PARAMETER IconType
     The icon type to render. Only font-awesome is supported.

.PARAMETER OutputDir
    The directory where PNG icons are written.

.PARAMETER ImageSize
    The width and height, in pixels, for the generated PNG icon.

.PARAMETER IconScale
    The relative scale for the Font Awesome icon within the circle.
#>
function New-NodeIcon {
    [CmdletBinding()]
    [OutputType([void])]
    param(
        [Parameter(Mandatory = $true)]
        [string] $NodeName,

        [Parameter(Mandatory = $true)]
        [string] $Icon,

        [Parameter(Mandatory = $true)]
        [string] $Color,

        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [ValidateSet('font-awesome')]
        [string] $IconType = 'font-awesome',

        [Parameter(Mandatory = $true)]
        [string] $OutputDir,

        [Parameter(Mandatory = $true)]
        [int] $ImageSize,

        [Parameter(Mandatory = $true)]
        [double] $IconScale
    )

    try {
        [string] $safeName = Get-SafeFileName -Name $NodeName
        [string] $outputFile = Join-Path -Path $OutputDir -ChildPath "$safeName.png"

        # Fetch and parse the Font Awesome SVG icon
        [SkiaSharp.SKPicture] $picture = Get-FontAwesomeIcon -IconName $Icon
        if ($null -eq $picture) {
            Write-Warning "Skipping ${NodeName}`: SVG failed to load for '$iconName'."
            return
        }

        [SkiaSharp.SKBitmap] $bitmap = [SkiaSharp.SKBitmap]::new($ImageSize, $ImageSize)
        try {
            [SkiaSharp.SKCanvas] $canvas = [SkiaSharp.SKCanvas]::new($bitmap)
            try {
                $canvas.Clear([SkiaSharp.SKColors]::Transparent)

                [float] $radius = ($ImageSize / 2) - 1
                [float] $center = $ImageSize / 2

                [SkiaSharp.SKPaint] $fillPaint = [SkiaSharp.SKPaint]::new()
                try {
                    $fillPaint.IsAntialias = $true
                    $fillPaint.Style = [SkiaSharp.SKPaintStyle]::Fill
                    $fillPaint.Color = [SkiaSharp.SKColor]::Parse($Color)

                    [SkiaSharp.SKPaint] $strokePaint = [SkiaSharp.SKPaint]::new()
                    try {
                        $strokePaint.IsAntialias = $true
                        $strokePaint.Style = [SkiaSharp.SKPaintStyle]::Stroke
                        $strokePaint.Color = [SkiaSharp.SKColors]::Black
                        $strokePaint.StrokeWidth = 2

                        $canvas.DrawCircle($center, $center, $radius, $fillPaint)
                        $canvas.DrawCircle($center, $center, $radius, $strokePaint)
                    }
                    finally {
                        $strokePaint.Dispose()
                    }
                }
                finally {
                    $fillPaint.Dispose()
                }

                [SkiaSharp.SKRect] $bounds = $picture.CullRect
                [float] $targetSize = $ImageSize * $IconScale
                [float] $scale = [Math]::Min($targetSize / $bounds.Width, $targetSize / $bounds.Height)

                $canvas.Save() | Out-Null
                try {
                    $canvas.Translate($center, $center)
                    $canvas.Scale($scale)
                    $canvas.Translate(-$bounds.MidX, -$bounds.MidY)

                    [SkiaSharp.SKPaint] $iconPaint = [SkiaSharp.SKPaint]::new()
                    try {
                        $iconPaint.IsAntialias = $true
                        $iconPaint.ColorFilter = [SkiaSharp.SKColorFilter]::CreateBlendMode([SkiaSharp.SKColors]::Black, [SkiaSharp.SKBlendMode]::SrcIn)

                        $canvas.DrawPicture($picture, $iconPaint)
                    }
                    finally {
                        $iconPaint.Dispose()
                    }
                }
                finally {
                    $canvas.Restore()
                }

                Write-NodeIconPng -Bitmap $bitmap -OutputFile $outputFile

                Write-Host "Wrote $outputFile"
            }
            finally {
                $canvas.Dispose()
            }
        }
        finally {
            $bitmap.Dispose()
        }
    }
    catch {
        Write-Warning "Skipping ${NodeName}`: $($_.Exception.Message)"
    }
}

Main
