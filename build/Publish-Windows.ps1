param(
    [string]$Runtime = "win-x64",
    [string]$Configuration = "Release"
)

$ErrorActionPreference = "Stop"

$Root = Split-Path -Parent $PSScriptRoot
$Project = Join-Path $Root "src/ZaldrionPasswordIntelligence/ZaldrionPasswordIntelligence.csproj"
$Output = Join-Path $Root "publish/$Runtime"
$Zip = Join-Path $Root "ZaldrionPasswordIntelligence-$Runtime.zip"

if (Test-Path $Output) {
    Remove-Item $Output -Recurse -Force
}

if (Test-Path $Zip) {
    Remove-Item $Zip -Force
}

dotnet restore $Project

dotnet publish $Project `
    -c $Configuration `
    -r $Runtime `
    --self-contained true `
    /p:PublishSingleFile=true `
    /p:EnableCompressionInSingleFile=true `
    /p:IncludeNativeLibrariesForSelfExtract=true `
    -o $Output

Compress-Archive -Path "$Output/*" -DestinationPath $Zip -Force

Write-Host "Ready: $Zip"
