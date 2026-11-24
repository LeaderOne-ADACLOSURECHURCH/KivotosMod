$ErrorActionPreference = 'Stop'
$game = 'D:/SteamLibrary/steamapps/common/tModLoader'
Add-Type -Path "$game/Libraries/mono.cecil/0.11.6/lib/netstandard2.0/Mono.Cecil.dll"
$assembly = [Mono.Cecil.AssemblyDefinition]::ReadAssembly("$game/tModLoader.dll")
try {
    $main = $assembly.MainModule.GetType('Terraria.Main')
    $draw = $main.Methods | Where-Object Name -eq 'DoDraw' | Select-Object -First 1
    $instructions = $draw.Body.Instructions
    $matches = @()
    for ($i=1; $i -lt $instructions.Count; $i++) {
        if ($instructions[$i].OpCode.Name -eq 'call' -and
            $instructions[$i].Operand.Name -eq 'DrawStarsInBackground' -and
            $instructions[$i-1].OpCode.Name -eq 'ldc.i4.1') { $matches += $i }
    }
    if ($matches.Count -ne 1) { throw "Expected one shimmer-star insertion point; found $($matches.Count)" }
    $index = $matches[0]
    $before = $instructions[([Math]::Max(0,$index-12))..$index] | ForEach-Object ToString
    $after = $instructions[$index..([Math]::Min($instructions.Count-1,$index+12))] | ForEach-Object ToString
    if (!($before -match 'SetZoom_Unscaled') -or !($before -match 'SpriteBatch::Begin')) {
        throw 'Expected unscaled active sprite batch around shimmer draw'
    }
    if (!($after -match 'SpriteBatch::End')) { throw 'Expected vanilla batch end after insertion' }
    foreach ($name in @('DrawBackground','DrawBlack')) {
        $method = $main.Methods | Where-Object Name -eq $name | Select-Object -First 1
        if (!(($method.Body.Instructions | Select-Object -First 2 | ForEach-Object ToString) -match 'Main::shimmerAlpha')) {
            throw "$name no longer follows shimmerAlpha"
        }
    }
    Write-Output 'PASS: installed tML has exactly one shimmer-star hook with active unscaled batch; native background/black-pass suppression still reads shimmerAlpha.'
} finally { $assembly.Dispose() }
