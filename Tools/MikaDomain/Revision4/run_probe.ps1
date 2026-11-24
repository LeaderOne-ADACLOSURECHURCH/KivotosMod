$ErrorActionPreference = 'Stop'
$root = (Resolve-Path (Join-Path $PSScriptRoot '../../..')).Path
$runner = Join-Path $env:TEMP 'KivotosMikaResonanceProbe'
$game = 'D:/SteamLibrary/steamapps/common/tModLoader'
New-Item -ItemType Directory -Force -Path $runner | Out-Null
$project = @"
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings><EnableDefaultCompileItems>false</EnableDefaultCompileItems>
  </PropertyGroup>
  <ItemGroup>
    <Reference Include="FNA"><HintPath>$game/Libraries/FNA/1.0.0/FNA.dll</HintPath></Reference>
    <Compile Include="$PSScriptRoot/ResonanceProbe.cs" />
    <Compile Include="$root/Content/Skills/BlackHole/MikaResonanceRenderer.cs" />
    <Compile Include="$root/Content/Skills/BlackHole/MikaStarBloom.cs" />
    <Compile Include="$root/Content/Skills/BlackHole/MikaExState.cs" />
    <Compile Include="$root/Content/Skills/BlackHole/MikaShatter.cs" />
  </ItemGroup>
</Project>
"@
[IO.File]::WriteAllText((Join-Path $runner 'Probe.csproj'), $project)
$savedPath = $env:PATH
$savedDriver = $env:FNA3D_FORCE_DRIVER
try {
    $env:PATH = "$game/Libraries/Native/Windows;$savedPath"
    $env:FNA3D_FORCE_DRIVER = 'D3D11'
    & dotnet run --project (Join-Path $runner 'Probe.csproj') -- $root
    if ($LASTEXITCODE -ne 0) { throw 'Mika EX timing or FNA probe failed.' }
} finally {
    $env:PATH = $savedPath
    $env:FNA3D_FORCE_DRIVER = $savedDriver
}
