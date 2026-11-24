$ErrorActionPreference = 'Stop'
$modRoot = Split-Path (Split-Path $PSScriptRoot -Parent) -Parent
$appearance = [IO.File]::ReadAllText((Join-Path $modRoot 'Content/Tablet/TabletStudentAppearancePlayer.cs'))
$catalog = [IO.File]::ReadAllText((Join-Path $modRoot 'Content/Tablet/TabletStudentCatalog.cs'))
function Get-ProductionMethod([string]$name) {
    $match = [regex]::Match($appearance, '(?m)^    (?:public|private|internal)[^\r\n]*\b' + $name + '\(')
    if (-not $match.Success) { throw "Production method missing: $name" }
    $start = $match.Index
    $brace = $appearance.IndexOf('{', $start)
    $arrow = $appearance.IndexOf('=>', $start)
    if ($arrow -ge 0 -and $arrow -lt $brace) {
        return $appearance.Substring($start, $appearance.IndexOf(';', $arrow) - $start + 1)
    }
    $depth = 0
    for ($i = $brace; $i -lt $appearance.Length; $i++) {
        if ($appearance[$i] -eq '{') { $depth++ }
        if ($appearance[$i] -eq '}') { $depth--; if ($depth -eq 0) { return $appearance.Substring($start, $i - $start + 1) } }
    }
    throw "Unclosed production method: $name"
}
$allStudents = @('Yuuka','Hoshino','Hina','Noa','Ibuki','Iroha','Hikari','Mika','Haruka','Kayoko','Mutsuki','Nozomi','Nagisa','Seia')
$imports = ($allStudents | ForEach-Object { "using KivotosMod.Content.Items.Vanity.$_;" }) -join "`n"
$enum = [regex]::Match($catalog, '(?ms)public enum TabletStudentId : byte\s*\{.*?^}').Value
$properties = ([regex]::Matches($appearance, '(?m)^    public (?:TabletStudentId|bool) [^\r\n]+') | ForEach-Object Value) -join "`n"
$methods = (@('Select','ClearSelection','IsFormationStudent','ReceiveSelection','SaveData','LoadData','ApplyStudentVisualSlots') | ForEach-Object { Get-ProductionMethod $_ }) -join "`n"
$sources = [Collections.Generic.List[string]]::new()
$sources.Add([IO.File]::ReadAllText((Join-Path $PSScriptRoot 'rules_stubs.cs')))
$sources.Add("$imports`nusing KivotosMod.Content.Items.Vanity; using Terraria; using Terraria.ModLoader; using Terraria.ModLoader.IO; namespace KivotosMod.Content.Tablet { $enum public class TabletStudentAppearancePlayer : ModPlayer { $properties $methods } }")
$items = ''
foreach ($student in $allStudents) {
    $items += "namespace KivotosMod.Content.Items.Vanity.$student { "
    foreach ($part in @('Head','Body','Boots')) {
        $items += "public class $student$part : Terraria.ModLoader.OutfitItem { } "
        if ($student -in @('Mika','Yuuka','Ibuki','Iroha','Hikari')) {
            $items += "public class ${student}2x2$part : Terraria.ModLoader.OutfitItem { } "
        }
    }
    $items += "}`n"
}
$sources.Add($items)
foreach ($student in @('Haruka','Kayoko','Mutsuki','Ibuki','Iroha','Hikari','Nozomi','Mika','Yuuka','Noa','Nagisa','Seia')) {
    $source = [IO.File]::ReadAllText((Join-Path $modRoot "Content/Items/Vanity/$student/${student}Set.cs"))
    # Execute the real slot visibility predicates. Other equipment APIs stay stubbed.
    $predicates = ([regex]::Matches($source, 'public static bool (?:HeadVisible|BodyVisible|BootsVisible|FullSetVisible)\([^;]+;') | ForEach-Object Value) -join "`n"
    $sources.Add("using Terraria; using Terraria.ModLoader; namespace KivotosMod.Content.Items.Vanity.$student { public static class ${student}VanitySet { $predicates } }")
}
$sources.Add([IO.File]::ReadAllText((Join-Path $modRoot 'Content/Items/Vanity/StudentHaloLightPlayer.cs')))
$sources.Add([IO.File]::ReadAllText((Join-Path $modRoot 'Content/Items/Vanity/StudentHurtVoicePlayer.cs')))
$sources.Add([IO.File]::ReadAllText((Join-Path $modRoot 'Content/Items/Vanity/StudentSpriteVariants.cs')))
$sources.Add([IO.File]::ReadAllText((Join-Path $modRoot 'Tools/CostumeRevision/voice_probe.cs')))
$sources.Add([IO.File]::ReadAllText((Join-Path $PSScriptRoot 'rules_probe.cs')))
$tempDir = Join-Path ([IO.Path]::GetTempPath()) ('kivotos-students-' + [Guid]::NewGuid().ToString('N'))
[IO.Directory]::CreateDirectory($tempDir) | Out-Null
$paths = @()
try {
    for ($i = 0; $i -lt $sources.Count; $i++) {
        $path = Join-Path $tempDir "$i.cs"
        [IO.File]::WriteAllText($path, $sources[$i]); $paths += $path
    }
    Add-Type -Path $paths
    [StudentBatchProbe]::Validate()
    [StudentVoiceProbe]::Validate()
}
finally {
    foreach ($path in $paths) { if (Test-Path -LiteralPath $path) { Remove-Item -LiteralPath $path } }
    [IO.Directory]::Delete($tempDir)
}
$report = [ordered]@{ passed = $true; productionHooks = $true; engineDependenciesStubbed = $true
    oldIdsStable = $true; allFormationIdsRoundTrip = $true; newVisualSlots = $true
    networkWhitelistAndInvalidSaveFallback = $true; haloLightOutfits = 12
    lightSuppressedForDeadGhostInvisibleInactiveServerAndMenu = $true
    lightRemovedWhenOutfitRemoved = $true; reverseGravityLightPosition = $true; maximumLightChannel = 0.55 }
$report.hurtVoiceStudents = 5
$report.hurtVoiceCooldownTicks = 45
$report.hurtVoiceHeavyGroupsAndVanillaSuppression = $true
$report.environmentLightBeforeVisualSlotAssignment = $true
$report.perPlayerOutfitsAndCanonicalIdentity = $true
$json = $report | ConvertTo-Json
[IO.File]::WriteAllText((Join-Path $PSScriptRoot 'rules_validation.json'), $json)
$json
