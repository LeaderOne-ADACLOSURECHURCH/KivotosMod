$ErrorActionPreference = 'Stop'
$root = Split-Path (Split-Path $PSScriptRoot -Parent) -Parent
$runner = Join-Path $env:TEMP 'KivotosSummary2SemanticRunner'
New-Item -ItemType Directory -Force -Path $runner | Out-Null
$sdkLine = (& dotnet --list-sdks | Where-Object { $_ -match '^8\.' } | Select-Object -Last 1)
if (!$sdkLine) { throw '.NET 8 SDK is required.' }
$sdkVersion = $sdkLine.Split(' ')[0]
$sdkBase = [regex]::Match($sdkLine, '\[(.*)\]').Groups[1].Value
$roslyn = Join-Path (Join-Path $sdkBase $sdkVersion) 'Roslyn/bincore'
$project = @"
<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework><ImplicitUsings>enable</ImplicitUsings></PropertyGroup><ItemGroup><Reference Include="Microsoft.CodeAnalysis"><HintPath>$roslyn/Microsoft.CodeAnalysis.dll</HintPath></Reference><Reference Include="Microsoft.CodeAnalysis.CSharp"><HintPath>$roslyn/Microsoft.CodeAnalysis.CSharp.dll</HintPath></Reference></ItemGroup></Project>
"@
[IO.File]::WriteAllText((Join-Path $runner 'Check.csproj'), $project)
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'semantic_check.cs') -Destination (Join-Path $runner 'Program.cs')
& dotnet run --project (Join-Path $runner 'Check.csproj') -- $root
if ($LASTEXITCODE -ne 0) { throw 'Semantic check failed.' }
