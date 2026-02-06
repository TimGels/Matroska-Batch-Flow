<#
.SYNOPSIS
    Build script for Matroska Batch Flow application with multiple distribution modes.

.DESCRIPTION
    Builds the application in various configurations for different distribution methods:
    - SingleFile: Self-contained single executable (unpackaged, WinUI3)
    - MultiFile: Multi-file deployment (unpackaged, WinUI3)
    - Store: MSIX package for Microsoft Store submission
    - SelfSigned: MSIX package with self-signed certificate for sideloading
    - SkiaDesktopWin: Skia Desktop single-file for Windows (Experimental)
    - SkiaDesktopLinux: Skia Desktop single-file for Linux (Experimental)
    - SkiaDesktopWinMulti: Skia Desktop multi-file for Windows (Experimental)
    - SkiaDesktopLinuxMulti: Skia Desktop multi-file for Linux (Experimental)

.PARAMETER BuildType
    The type of build to create. Valid values: SingleFile, MultiFile, Store, SelfSigned, SkiaDesktopWin, SkiaDesktopLinux, SkiaDesktopWinMulti, SkiaDesktopLinuxMulti

.PARAMETER Configuration
    Build configuration. Valid values: Debug, Release (default: Release)

.PARAMETER Platform
    Target platform. Valid values: x64 (default: x64)

.PARAMETER OutputPath
    Optional custom output directory. If not specified, uses default project output paths.

.PARAMETER Clean
    Clean the build output before building.

.PARAMETER SkipRestore
    Skip NuGet package restore step.

.EXAMPLE
    .\Publish-Application.ps1 -BuildType Store
    Builds a Release MSIX package for Microsoft Store

.EXAMPLE
    .\Publish-Application.ps1 -BuildType SingleFile -Configuration Debug
    Builds a Debug single-file executable

.EXAMPLE
    .\Publish-Application.ps1 -BuildType MultiFile -Clean
    Cleans and builds a Release multi-file deployment

.NOTES
    Author: Tim Gels
    Designed for the Matroska CI/CD pipelines and local development
#>

[CmdletBinding()]
param(
    [Parameter(Mandatory = $true, Position = 0)]
    [ValidateSet('SingleFile', 'MultiFile', 'Store', 'SelfSigned', 'SkiaDesktopWin', 'SkiaDesktopLinux', 'SkiaDesktopWinMulti', 'SkiaDesktopLinuxMulti')]
    [string]$BuildType,

    [Parameter(Mandatory = $false)]
    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Release',

    [Parameter(Mandatory = $false)]
    [ValidateSet('x64')]
    [string]$Platform = 'x64',

    [Parameter(Mandatory = $false)]
    [string]$OutputPath,

    [Parameter(Mandatory = $false)]
    [switch]$Clean,

    [Parameter(Mandatory = $false)]
    [switch]$SkipRestore
)

$ErrorActionPreference = 'Stop'

# ═══════════════════════════════════════════════════════════════
# Constants
# ═══════════════════════════════════════════════════════════════

$BORDER_LINE = "═══════════════════════════════════════════════════════════════"
$DOTNET_COMMON_ARGS = @('--nologo', '-v', 'minimal')
$FRAMEWORK_SKIA_DESKTOP = 'net10.0-desktop'
$FRAMEWORK_WINDOWS = 'net10.0-windows10.0.19041'

# ═══════════════════════════════════════════════════════════════
# Platform Validation
# ═══════════════════════════════════════════════════════════════

# Verify running on Windows
if (-not $IsWindows -and -not ($PSVersionTable.PSVersion.Major -le 5)) {
    Write-Host ""
    Write-Host "  $BORDER_LINE" -ForegroundColor Red
    Write-Host ""
    Write-Host "    ✗ PLATFORM NOT SUPPORTED" -ForegroundColor Red
    Write-Host ""
    Write-Host "  $BORDER_LINE" -ForegroundColor Red
    Write-Host ""
    Write-Host "  This script builds Windows applications (WinUI3/MSIX) and must run on Windows." -ForegroundColor Yellow
    Write-Host ""
    Write-Host "  Current platform: $($PSVersionTable.Platform)" -ForegroundColor Gray
    Write-Host ""
    Write-Host "  For CI/CD pipelines:" -ForegroundColor Cyan
    Write-Host "    • Use a Windows runner (e.g., runs-on: windows-latest)" -ForegroundColor Gray
    Write-Host "    • Or use a Windows container" -ForegroundColor Gray
    Write-Host ""
    Write-Host "  This script cannot build Windows apps on Linux/macOS." -ForegroundColor Yellow
    Write-Host ""
    exit 1
}

# ═══════════════════════════════════════════════════════════════
# Configuration
# ═══════════════════════════════════════════════════════════════

# Script paths
$ProjectPath = Join-Path $PSScriptRoot 'src' 'MatroskaBatchFlow.Uno' 'MatroskaBatchFlow.Uno.csproj'

# Verify project file exists
if (-not (Test-Path $ProjectPath)) {
    Write-Error "Project file not found: $ProjectPath"
    exit 1
}

# Build type configurations
$BuildConfigs = @{
    SingleFile = @{
        DisplayName = 'Single-File Executable (Unpackaged)'
        PublishProfile = 'win-x64-singlefile'
        TargetFramework = $FRAMEWORK_WINDOWS
        UsePublish = $true
        OutputPattern = '*.exe'
        OutputDescription = 'Single executable ready for distribution'
        DefaultOutputPath = 'src\MatroskaBatchFlow.Uno\bin\publish\win-{Platform}-singlefile'
    }
    MultiFile = @{
        DisplayName = 'Multi-File Deployment (Unpackaged)'
        PublishProfile = 'win-x64'
        TargetFramework = $FRAMEWORK_WINDOWS
        UsePublish = $true
        OutputPattern = 'MatroskaBatchFlow.exe'
        OutputDescription = 'Multi-file application with separate dependencies'
        DefaultOutputPath = 'src\MatroskaBatchFlow.Uno\bin\publish\win-{Platform}-multifile'
    }
    Store = @{
        DisplayName = 'Microsoft Store MSIX Package'
        TargetFramework = $FRAMEWORK_WINDOWS
        UsePublish = $false
        UseMSBuild = $true
        OutputPattern = '*.msix'
        OutputDescription = 'Unsigned MSIX package for Store upload'
        DefaultOutputPath = 'src\MatroskaBatchFlow.Uno\bin\{Platform}\{Configuration}\{Framework}\win-{Platform}\AppPackages'
        MSBuildProperties = @{
            UapAppxPackageBuildMode = 'StoreUpload'
            AppxBundle = 'Never'
            GenerateAppxPackageOnBuild = 'true'
            AppxPackageSigningEnabled = 'false'
        }
    }
    SelfSigned = @{
        DisplayName = 'Self-Signed MSIX Package'
        TargetFramework = $FRAMEWORK_WINDOWS
        UsePublish = $false
        UseMSBuild = $true
        OutputPattern = '*.msix'
        OutputDescription = 'Self-signed MSIX package for sideloading'
        DefaultOutputPath = 'src\MatroskaBatchFlow.Uno\bin\{Platform}\{Configuration}\{Framework}\win-{Platform}\AppPackages'
        MSBuildProperties = @{
            UapAppxPackageBuildMode = 'SideloadOnly'
            AppxBundle = 'Never'
            GenerateAppxPackageOnBuild = 'true'
            AppxPackageSigningEnabled = 'true'
        }
    }
    SkiaDesktopWin = @{
        DisplayName = 'Skia Desktop Single-File (Windows) - EXPERIMENTAL'
        PublishProfile = 'skia-desktop-win-x64'
        TargetFramework = $FRAMEWORK_SKIA_DESKTOP
        UsePublish = $true
        OutputPattern = '*.exe'
        OutputDescription = 'Experimental Skia Desktop build for Windows'
        DefaultOutputPath = 'src\MatroskaBatchFlow.Uno\bin\publish\skia-win-{Platform}'
        Experimental = $true
    }
    SkiaDesktopLinux = @{
        DisplayName = 'Skia Desktop Single-File (Linux) - EXPERIMENTAL'
        PublishProfile = 'skia-desktop-linux-x64'
        TargetFramework = $FRAMEWORK_SKIA_DESKTOP
        UsePublish = $true
        OutputPattern = 'MatroskaBatchFlow'
        OutputDescription = 'Experimental Skia Desktop build for Linux'
        DefaultOutputPath = 'src\MatroskaBatchFlow.Uno\bin\publish\skia-linux-{Platform}'
        Experimental = $true
    }
    SkiaDesktopWinMulti = @{
        DisplayName = 'Skia Desktop Multi-File (Windows) - EXPERIMENTAL'
        PublishProfile = 'skia-desktop-win-x64-multi'
        TargetFramework = $FRAMEWORK_SKIA_DESKTOP
        UsePublish = $true
        OutputPattern = 'MatroskaBatchFlow.exe'
        OutputDescription = 'Experimental Skia Desktop multi-file build for Windows'
        DefaultOutputPath = 'src\MatroskaBatchFlow.Uno\bin\publish\skia-win-{Platform}-multi'
        Experimental = $true
    }
    SkiaDesktopLinuxMulti = @{
        DisplayName = 'Skia Desktop Multi-File (Linux) - EXPERIMENTAL'
        PublishProfile = 'skia-desktop-linux-x64-multi'
        TargetFramework = $FRAMEWORK_SKIA_DESKTOP
        UsePublish = $true
        OutputPattern = 'MatroskaBatchFlow'
        OutputDescription = 'Experimental Skia Desktop multi-file build for Linux'
        DefaultOutputPath = 'src\MatroskaBatchFlow.Uno\bin\publish\skia-linux-{Platform}-multi'
        Experimental = $true
    }
}

# ═══════════════════════════════════════════════════════════════
# Helper Functions
# ═══════════════════════════════════════════════════════════════

function Format-FileSize {
    <#
    .SYNOPSIS
        Converts bytes to human-readable file size format.
    .DESCRIPTION
        Automatically selects the appropriate unit (GB, MB, KB, bytes) based on file size.
        Returns a formatted string with 2 decimal places for non-byte values.
    .EXAMPLE
        Format-FileSize -Bytes 1073741824  # Returns "1 GB"
    #>
    param(
        [Parameter(Mandatory = $true)]
        [long]$Bytes
    )
    
    # Size definitions (easy to extend with TB, PB, etc.)
    $sizes = @(
        @{ Unit = 'GB'; Divisor = 1GB; Threshold = 1GB }
        @{ Unit = 'MB'; Divisor = 1MB; Threshold = 1MB }
        @{ Unit = 'KB'; Divisor = 1KB; Threshold = 1KB }
    )
    
    # Iterate through sizes and return the first matching unit.
    foreach ($size in $sizes) {
        if ($Bytes -ge $size.Threshold) {
            $value = [math]::Round($Bytes / $size.Divisor, 2)
            return "$value $($size.Unit)"
        }
    }
    
    return "$Bytes bytes"
}

function Write-Header {
    <#
    .SYNOPSIS
        Displays a formatted header with build information.
    #>
    param(
        [string]$Title,
        [hashtable]$BuildInfo
    )
    
    Write-Host ""
    Write-Host "  $BORDER_LINE" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "    $Title" -ForegroundColor White
    Write-Host ""
    Write-Host "  ───────────────────────────────────────────────────────────────" -ForegroundColor DarkCyan
    Write-Host ""
    
    foreach ($key in $BuildInfo.Keys | Sort-Object) {
        $label = $key.PadRight(14)
        Write-Host "    $label" -NoNewline -ForegroundColor Gray
        Write-Host $BuildInfo[$key] -ForegroundColor White
    }
    
    Write-Host ""
    Write-Host "  $BORDER_LINE" -ForegroundColor Cyan
    Write-Host ""
}

function Write-StatusMessage {
    <#
    .SYNOPSIS
        Displays a status message with consistent formatting.
    #>
    param(
        [string]$Message,
        [ValidateSet('Info', 'Success', 'Error')]
        [string]$Type = 'Info'
    )
    
    $color = switch ($Type) {
        'Info' { 'Yellow' }
        'Success' { 'Green' }
        'Error' { 'Red' }
    }
    
    Write-Host "  $Message" -ForegroundColor $color
    Write-Host ""
}

function Write-BuildResult {
    <#
    .SYNOPSIS
        Displays build success or failure with formatted output.
    #>
    param(
        [Parameter(Mandatory)]
        [bool]$Success,
        
        [Parameter(Mandatory)]
        [AllowEmptyString()]
        [string]$OutputLocation,
        
        [Parameter(Mandatory)]
        [string]$OutputPattern,
        
        [Parameter(Mandatory)]
        [string]$Description,
        
        [Parameter(Mandatory)]
        [string]$BuildType
    )
    
    Write-Host ""
    
    if ($Success) {
        Write-Host "  $BORDER_LINE" -ForegroundColor Green
        Write-Host ""
        Write-Host "    ✓ BUILD SUCCEEDED" -ForegroundColor Green
        Write-Host ""
        Write-Host "  $BORDER_LINE" -ForegroundColor Green
        Write-Host ""
        
        Show-BuildOutput -OutputLocation $OutputLocation -OutputPattern $OutputPattern `
                         -Description $Description -BuildType $BuildType
    } else {
        Write-Host "  $BORDER_LINE" -ForegroundColor Red
        Write-Host ""
        Write-Host "    ✗ BUILD FAILED" -ForegroundColor Red
        Write-Host ""
        Write-Host "  $BORDER_LINE" -ForegroundColor Red
        Write-Host ""
        Write-Host "  Check the build output above for errors" -ForegroundColor Yellow
        Write-Host ""
    }
}

function Show-BuildOutput {
    <#
    .SYNOPSIS
        Displays information about build output files.
    #>
    param(
        [string]$OutputLocation,
        [string]$OutputPattern,
        [string]$Description,
        [string]$BuildType
    )
    
    if (-not $OutputLocation -or -not (Test-Path $OutputLocation)) {
        Write-Host "  Output location not found or inaccessible" -ForegroundColor Yellow
        Write-Host "  Expected: $OutputLocation" -ForegroundColor Gray
        Write-Host ""
        return
    }
    
    Write-Host "  Output Location" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "    $OutputLocation" -ForegroundColor White
    Write-Host ""
    
    $outputFiles = Get-ChildItem -Path $OutputLocation -Recurse -Include $OutputPattern -ErrorAction SilentlyContinue
    
    if (-not $outputFiles) {
        Write-Host "  No output files matching '$OutputPattern' found" -ForegroundColor Yellow
        Write-Host "  Check the output directory for build artifacts" -ForegroundColor Gray
        Write-Host ""
        return
    }
    
    Write-Host "  Output Files" -ForegroundColor Cyan
    Write-Host ""
    
    foreach ($file in $outputFiles) {
        $sizeText = Format-FileSize -Bytes $file.Length
        Write-Host "    • " -NoNewline -ForegroundColor DarkGray
        Write-Host $file.Name -ForegroundColor White -NoNewline
        Write-Host "  ($sizeText)" -ForegroundColor Gray
        Write-Host "      $($file.FullName)" -ForegroundColor DarkGray
    }
    
    Write-Host ""
    Write-Host "  $Description" -ForegroundColor Yellow
    Write-Host ""
    
    # Build-type-specific messages
    switch ($BuildType) {
        'Store' {
            Write-Host "  Upload to Microsoft Partner Center for certification" -ForegroundColor Cyan
            Write-Host ""
        }
        'SelfSigned' {
            Write-Host "  Install the certificate before sideloading the package" -ForegroundColor Cyan
            Write-Host ""
        }
    }
}

function Invoke-DotNetCommand {
    <#
    .SYNOPSIS
        Executes a dotnet command and returns success status.
    .DESCRIPTION
        Runs dotnet with specified arguments. Output is streamed directly to console.
        Returns $true if exit code is 0, $false otherwise.
    #>
    param(
        [string[]]$Arguments,
        [string]$OperationName
    )
    
    Write-Verbose "Executing: dotnet $($Arguments -join ' ')"
    
    # Start process to preserve color output
    $psi = New-Object System.Diagnostics.ProcessStartInfo
    $psi.FileName = 'dotnet'
    $psi.Arguments = $Arguments -join ' '
    $psi.UseShellExecute = $false
    $psi.RedirectStandardOutput = $false
    $psi.RedirectStandardError = $false
    
    $process = [System.Diagnostics.Process]::Start($psi)
    $process.WaitForExit()
    
    return ($process.ExitCode -eq 0)
}

function Remove-DebugSymbols {
    <#
    .SYNOPSIS
        Removes debug symbol files (.pdb) from publish output.
    .DESCRIPTION
        Cleans up .pdb files from NuGet package dependencies that get copied to the publish directory.
        These files are useful during development but not needed for distribution.
    .PARAMETER OutputPath
        The directory to search for .pdb files.
    .EXAMPLE
        Remove-DebugSymbols -OutputPath "bin\publish\win-x64"
    #>
    param(
        [Parameter(Mandatory = $true)]
        [string]$OutputPath
    )
    
    if (-not (Test-Path $OutputPath)) {
        return
    }
    
    $pdbFiles = Get-ChildItem -Path $OutputPath -Filter "*.pdb" -Recurse -ErrorAction SilentlyContinue
    
    if ($pdbFiles) {
        Write-Host ""
        Write-StatusMessage "Removing debug symbol files..." -Type Info
        $pdbFiles | Remove-Item -Force -ErrorAction SilentlyContinue
        Write-Host ""
    }
}

function Resolve-BuildOutputPath {
    <#
    .SYNOPSIS
        Resolves the output path for build artifacts using template placeholders.
    #>
    param(
        [Parameter(Mandatory)]
        [hashtable]$Config,
        
        [Parameter(Mandatory)]
        [string]$Platform,
        
        [string]$Configuration,
        
        [string]$Framework,
        
        [string]$CustomOutputPath
    )
    
    if ($CustomOutputPath) {
        return $CustomOutputPath
    }
    
    $path = $Config.DefaultOutputPath -replace '\{Platform\}', $Platform
    if ($Configuration) {
        $path = $path -replace '\{Configuration\}', $Configuration
    }
    if ($Framework) {
        $path = $path -replace '\{Framework\}', $Framework
    }
    
    return Join-Path $PSScriptRoot $path
}

function Invoke-DotNetBuildStep {
    <#
    .SYNOPSIS
        Executes a dotnet build step with error handling and consistent messaging.
    #>
    param(
        [Parameter(Mandatory)]
        [string]$StepName,
        
        [Parameter(Mandatory)]
        [string[]]$Arguments
    )
    
    Write-StatusMessage "$StepName..." -Type Info
    
    if (-not (Invoke-DotNetCommand -Arguments $Arguments -OperationName $StepName)) {
        Write-StatusMessage "$StepName failed with exit code $LASTEXITCODE" -Type Error
        exit $LASTEXITCODE
    }
    
    Write-StatusMessage "$StepName completed" -Type Success
}

# ═══════════════════════════════════════════════════════════════
# Main Execution
# ═══════════════════════════════════════════════════════════════

try {
    $config = $BuildConfigs[$BuildType]
    $TargetFramework = $config.TargetFramework
    
    # Display build information
    Write-Header -Title $config.DisplayName -BuildInfo @{
        Configuration = $Configuration
        Platform = $Platform
        Framework = $TargetFramework
    }
    
    # Display experimental warning if applicable
    if ($config.Experimental) {
        Write-Host ""
        Write-Host "  ⚠ WARNING: This is an EXPERIMENTAL build type" -ForegroundColor Yellow
        Write-Host "  • Not officially supported" -ForegroundColor Gray
        Write-Host "  • Expect bugs and compatibility issues" -ForegroundColor Gray
        Write-Host ""
    }
    
    # Clean if requested
    if ($Clean) {
        $cleanArgs = @('clean', $ProjectPath, '-c', $Configuration) + $DOTNET_COMMON_ARGS
        Invoke-DotNetBuildStep -StepName 'Cleaning previous build outputs' -Arguments $cleanArgs
    }
    
    # Restore packages unless skipped
    if (-not $SkipRestore) {
        $restoreArgs = @('restore', $ProjectPath) + $DOTNET_COMMON_ARGS
        Invoke-DotNetBuildStep -StepName 'Restoring NuGet packages' -Arguments $restoreArgs
    }
    
    # Build based on type
    $success = $false
    $outputLocation = $null
    
    if ($config.UsePublish) {
        # Publish-based builds (SingleFile, MultiFile)
        Write-StatusMessage "Publishing application..." -Type Info
        
        $publishArgs = @(
            'publish',
            $ProjectPath,
            "-p:PublishProfile=$($config.PublishProfile)",
            "-c:$Configuration",
            "-f:$TargetFramework"
        ) + $DOTNET_COMMON_ARGS
        
        if ($OutputPath) {
            $publishArgs += '-o', $OutputPath
        }
        
        $success = Invoke-DotNetCommand -Arguments $publishArgs -OperationName 'Publish'
        
        # Determine output location
        $outputLocation = Resolve-BuildOutputPath -Config $config -Platform $Platform -CustomOutputPath $OutputPath
        
        # Clean up debug symbols from publish output
        if ($success -and $outputLocation) {
            Remove-DebugSymbols -OutputPath $outputLocation
        }
        
    } elseif ($config.UseMSBuild) {
        # MSBuild-based builds (Store, SelfSigned)
        Write-StatusMessage "Building MSIX package..." -Type Info
        
        $msbuildArgs = @(
            'msbuild',
            $ProjectPath,
            '/t:Build',
            "/p:Configuration=$Configuration",
            "/p:Platform=$Platform",
            "/p:TargetFramework=$TargetFramework",
            '/nologo',
            '/v:minimal'
        )
        
        # Add build-type-specific properties
        foreach ($prop in $config.MSBuildProperties.GetEnumerator()) {
            $msbuildArgs += "/p:$($prop.Key)=$($prop.Value)"
        }
        
        if ($OutputPath) {
            $msbuildArgs += "/p:AppxPackageDir=$OutputPath\"
        }
        
        $success = Invoke-DotNetCommand -Arguments $msbuildArgs -OperationName 'Build'
        
        # Determine output location
        $outputLocation = Resolve-BuildOutputPath -Config $config -Platform $Platform -Configuration $Configuration -Framework $TargetFramework -CustomOutputPath $OutputPath
    }
    
    # Display results
    Write-BuildResult `
        -Success:$success `
        -OutputLocation $outputLocation `
        -OutputPattern $config.OutputPattern `
        -Description $config.OutputDescription `
        -BuildType $BuildType
    
    if ($success) {
        exit 0
    } else {
        exit 1
    }
    
} catch {
    Write-Host ""
    Write-Host "  $BORDER_LINE" -ForegroundColor Red
    Write-Host ""
    Write-Host "    ✗ BUILD FAILED WITH EXCEPTION" -ForegroundColor Red
    Write-Host ""
    Write-Host "  $BORDER_LINE" -ForegroundColor Red
    Write-Host ""
    Write-Error $_.Exception.Message
    Write-Host ""
    exit 1
}
