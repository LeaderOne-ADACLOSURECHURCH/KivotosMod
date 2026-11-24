$ErrorActionPreference='Stop'
$root=Split-Path (Split-Path $PSScriptRoot -Parent) -Parent
$runner=Join-Path $env:TEMP 'KivotosMikaDomainGpuProbe'
New-Item -ItemType Directory -Force -Path $runner | Out-Null
$game='D:/SteamLibrary/steamapps/common/tModLoader'
$project=@"
<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework><ImplicitUsings>enable</ImplicitUsings></PropertyGroup><ItemGroup><Reference Include="FNA"><HintPath>$game/Libraries/FNA/1.0.0/FNA.dll</HintPath></Reference></ItemGroup></Project>
"@
[IO.File]::WriteAllText((Join-Path $runner 'Probe.csproj'),$project)
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'GpuProbe.cs') -Destination (Join-Path $runner 'Program.cs')
$savedPath=$env:PATH
$savedDriver=$env:FNA3D_FORCE_DRIVER
try {
    $env:PATH="$game/Libraries/Native/Windows;$savedPath"
    $env:FNA3D_FORCE_DRIVER='D3D11'
    & dotnet run --project (Join-Path $runner 'Probe.csproj') -- $root
    if($LASTEXITCODE -ne 0){throw 'GPU track-matte probe failed'}
} finally {
    $env:PATH=$savedPath
    $env:FNA3D_FORCE_DRIVER=$savedDriver
}
