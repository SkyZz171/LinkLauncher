# Publication de LinkLauncher et fabrication de l'installeur.
#   .\installer\build.ps1                     -> publie artifacts\publish
#   .\installer\build.ps1 -Installer          -> publie puis compile l'installeur
#   .\installer\build.ps1 -Version 0.2.0 -Installer
# Script ASCII uniquement : PowerShell 5.1 lit les fichiers sans BOM en ANSI.
param(
    [string]$Version = "0.1.0",
    [switch]$Installer,
    [string]$Runtime = "win-x64"
)

$ErrorActionPreference = "Stop"

$root = Split-Path -Parent $PSScriptRoot
$project = Join-Path $root "src\LinkLauncher.App\LinkLauncher.App.csproj"
$publishDirectory = Join-Path $root "artifacts\publish"

Write-Host "Publication de LinkLauncher $Version ($Runtime)..."
if (Test-Path $publishDirectory) {
    Remove-Item $publishDirectory -Recurse -Force
}

dotnet publish $project `
    -c Release `
    -r $Runtime `
    --self-contained true `
    -p:PublishSingleFile=true `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    -p:EnableCompressionInSingleFile=true `
    -p:DebugType=None `
    -p:DebugSymbols=false `
    -p:Version=$Version `
    -o $publishDirectory

if ($LASTEXITCODE -ne 0) {
    throw "La publication a echoue (code $LASTEXITCODE)."
}

$exe = Join-Path $publishDirectory "LinkLauncher.exe"
if (-not (Test-Path $exe)) {
    throw "LinkLauncher.exe est absent de $publishDirectory."
}

Write-Host "Publication terminee : $exe"

if (-not $Installer) {
    Write-Host "Utilisez -Installer pour compiler l'installeur Inno Setup."
    return
}

$iscc = $null
$command = Get-Command "ISCC.exe" -ErrorAction SilentlyContinue
if ($command) {
    $iscc = $command.Source
}
else {
    $candidates = @(
        (Join-Path $env:LOCALAPPDATA "Programs\Inno Setup 6\ISCC.exe"),
        (Join-Path ${env:ProgramFiles(x86)} "Inno Setup 6\ISCC.exe"),
        (Join-Path $env:ProgramFiles "Inno Setup 6\ISCC.exe")
    )

    foreach ($candidate in $candidates) {
        if ($candidate -and (Test-Path $candidate)) {
            $iscc = $candidate
            break
        }
    }
}

if (-not $iscc) {
    throw "Inno Setup 6 (ISCC.exe) est introuvable. Installez-le depuis https://jrsoftware.org/isdl.php puis relancez ce script."
}

$script = Join-Path $PSScriptRoot "LinkLauncher.iss"
Write-Host "Compilation de l'installeur avec $iscc..."

& $iscc "/DAppVersion=$Version" $script

if ($LASTEXITCODE -ne 0) {
    throw "La compilation de l'installeur a echoue (code $LASTEXITCODE)."
}

$setup = Join-Path $root "artifacts\installer\LinkLauncher-$Version-setup.exe"
if (-not (Test-Path $setup)) {
    throw "L'installeur attendu est absent : $setup"
}

$hash = (Get-FileHash -Algorithm SHA256 -Path $setup).Hash.ToLowerInvariant()
Write-Host "Installeur : $setup"
Write-Host "SHA-256    : $hash"
Write-Host "Publiez cette empreinte dans les notes de la release GitHub : LinkLauncher la verifie avant installation."
