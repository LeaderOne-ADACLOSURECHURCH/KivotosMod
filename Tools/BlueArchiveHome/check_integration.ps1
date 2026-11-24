param(
    [string]$TmlRoot = 'D:/SteamLibrary/steamapps/common/tModLoader',
    [string]$RefRoot = 'C:/Program Files/dotnet/packs/Microsoft.NETCore.App.Ref/8.0.6/ref/net8.0'
)
$ErrorActionPreference = 'Stop'
$modRoot = Split-Path (Split-Path $PSScriptRoot -Parent) -Parent
$roslynRoot = Join-Path $TmlRoot 'Libraries'
$codeAnalysis = Join-Path $roslynRoot 'microsoft.codeanalysis.common/4.12.0/lib/net8.0/Microsoft.CodeAnalysis.dll'
$csharpAnalysis = Join-Path $roslynRoot 'microsoft.codeanalysis.csharp/4.12.0/lib/net8.0/Microsoft.CodeAnalysis.CSharp.dll'
$null = [Reflection.Assembly]::LoadFrom($codeAnalysis)
$null = [Reflection.Assembly]::LoadFrom($csharpAnalysis)
$references = [Collections.Generic.List[Microsoft.CodeAnalysis.MetadataReference]]::new()
$referenceNames = @{}
foreach ($file in Get-ChildItem -LiteralPath $RefRoot -Filter '*.dll') {
    $references.Add([Microsoft.CodeAnalysis.MetadataReference]::CreateFromFile($file.FullName))
    $referenceNames[$file.BaseName] = $true
}
$libraries = @(Get-ChildItem -LiteralPath $roslynRoot -Filter '*.dll' -Recurse | Where-Object {
    $_.FullName -notmatch '[\\/]Native[\\/]|[\\/]runtime[^\\/]*[\\/]|[\\/]tModCodeAssist[\\/]|\.resources\.dll$'
})
$libraries += Get-Item -LiteralPath (Join-Path $TmlRoot 'tModLoader.dll')
foreach ($file in $libraries) {
    $name = [Reflection.AssemblyName]::GetAssemblyName($file.FullName).Name
    if ($referenceNames.ContainsKey($name)) { continue }
    $references.Add([Microsoft.CodeAnalysis.MetadataReference]::CreateFromFile($file.FullName))
    $referenceNames[$name] = $true
}
$options = [Microsoft.CodeAnalysis.CSharp.CSharpParseOptions]::Default.WithLanguageVersion([Microsoft.CodeAnalysis.CSharp.LanguageVersion]::CSharp12)
$options = $options.WithPreprocessorSymbols([string[]]@('TML_2026_08', 'JETBRAINS_ANNOTATIONS'))
$trees = [Collections.Generic.List[Microsoft.CodeAnalysis.SyntaxTree]]::new()
foreach ($file in Get-ChildItem -LiteralPath $modRoot -Filter '*.cs' -Recurse) {
    $relative = $file.FullName.Substring($modRoot.Length + 1)
    if ($relative -match '^(Tools|bin|obj)[\\/]') { continue }
    $trees.Add([Microsoft.CodeAnalysis.CSharp.CSharpSyntaxTree]::ParseText(
        [IO.File]::ReadAllText($file.FullName), $options, $file.FullName,
        [Text.Encoding]::UTF8, [Threading.CancellationToken]::None))
}
$compilationOptions = [Microsoft.CodeAnalysis.CSharp.CSharpCompilationOptions]::new([Microsoft.CodeAnalysis.OutputKind]::DynamicallyLinkedLibrary).WithAllowUnsafe($true)
# Check the actual entry class, all project source, both runtimes and real tML/FNA APIs
# in one compilation context. GetDiagnostics only: no Emit, DLL, .tmod or game launch.
$compilation = [Microsoft.CodeAnalysis.CSharp.CSharpCompilation]::Create('KivotosMod', $trees, $references, $compilationOptions)
$diagnostics = @($compilation.GetDiagnostics([Threading.CancellationToken]::None))
$errors = @($diagnostics | Where-Object Severity -eq 'Error')
$warnings = @($diagnostics | Where-Object Severity -eq 'Warning')
$report = @{
    passed = ($errors.Count -eq 0)
    verification = 'Combined project semantic diagnostics against installed tML and net8 references; no emit/package/run'
    sourceFiles = $trees.Count
    references = $references.Count
    errors = @($errors | ForEach-Object { $_.ToString() })
    warningCount = $warnings.Count
}
[IO.File]::WriteAllText((Join-Path $PSScriptRoot 'integration_validation.json'), ($report | ConvertTo-Json -Depth 5))
$errors | ForEach-Object { Write-Output $_.ToString() }
Write-Output "Combined semantic check: $($trees.Count) source files, $($references.Count) references, $($errors.Count) errors, $($warnings.Count) warnings. No assembly emitted."
if ($errors.Count) { exit 1 }
