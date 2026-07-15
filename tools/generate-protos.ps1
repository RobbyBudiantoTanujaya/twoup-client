# Regenerates C# from proto/twoup.proto into Assets/Scripts/Generated/.
# Downloads protoc (from the Google.Protobuf.Tools NuGet package) into tools/protoc/ if missing.
# Keep the protoc major.minor aligned with the vendored Google.Protobuf.dll in Assets/Plugins/Protobuf/.
$ErrorActionPreference = "Stop"
$root = Split-Path $PSScriptRoot -Parent
$protoc = Join-Path $PSScriptRoot "protoc\bin\protoc.exe"
$toolsVersion = "3.35.1"

if (-not (Test-Path $protoc)) {
    Write-Host "protoc not found, downloading Google.Protobuf.Tools $toolsVersion from nuget.org..."
    [Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12
    $tmp = Join-Path $env:TEMP "google.protobuf.tools.$toolsVersion.zip"
    Invoke-WebRequest "https://api.nuget.org/v3-flatcontainer/google.protobuf.tools/$toolsVersion/google.protobuf.tools.$toolsVersion.nupkg" -OutFile $tmp
    $extract = Join-Path $env:TEMP "google.protobuf.tools.$toolsVersion"
    Expand-Archive $tmp -DestinationPath $extract -Force
    New-Item -ItemType Directory -Force (Split-Path $protoc) | Out-Null
    Copy-Item (Join-Path $extract "tools\windows_x64\protoc.exe") $protoc
}

$outDir = Join-Path $root "Assets\Scripts\Generated"
New-Item -ItemType Directory -Force $outDir | Out-Null
& $protoc --proto_path="$root\proto" --csharp_out="$outDir" "$root\proto\twoup.proto"
if ($LASTEXITCODE -ne 0) { throw "protoc failed with exit code $LASTEXITCODE" }
Write-Host "Generated $outDir\Twoup.cs"
