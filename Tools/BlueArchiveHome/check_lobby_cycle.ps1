$ErrorActionPreference = 'Stop'
$modRoot = Split-Path (Split-Path $PSScriptRoot -Parent) -Parent
$lobbySource = [IO.File]::ReadAllText((Join-Path $modRoot 'Content/Tablet/TabletUiSystem.Home.cs'))
$ui = [IO.File]::ReadAllText((Join-Path $modRoot 'Content/Tablet/TabletUiSystem.cs'))
$water = [IO.File]::ReadAllText((Join-Path $modRoot 'Content/Tablet/TabletUiSystem.LegacyPresentation.cs'))
$openLobby = [regex]::Match($lobbySource, '(?ms)^    private static void OpenLobby\(\).*?^    }').Value
$updateLobby = [regex]::Match($lobbySource, '(?ms)^    private static void UpdateLobby\(float seconds\).*?^    }').Value
# Execute the actual opening guard and scene initialization, before unrelated UI setup.
$open = [regex]::Match($ui, '(?ms)^    public static void Open\(\).*?        OpenLobby\(\);').Value + "`n    }"
if (-not $openLobby -or -not $updateLobby -or -not $open.Contains('if (Visible)')) { throw 'Lobby lifecycle source not found' }
if ($ui -notmatch 'layers = _lobbyDayTime \?' -or $water -match 'Main.dayTime' -or $water -notmatch 'ground = \(_lobbyDayTime \?') {
    throw 'Background and water do not share the opening snapshot'
}
# Asset/GPU dependencies are stubs; lifecycle methods above are read from production.
$stubs = @'
using System;
public static class Main { public static bool dayTime; public static Player LocalPlayer = new(); }
public class Player {
    public TabletCompanionPlayer Companion = new();
    public T GetModPlayer<T>() => (T)(object)Companion;
}
public enum TabletStudentId { Arona, Plana, Sora }
public class TabletCompanionPlayer { public TabletStudentId Companion; public void SetCompanion(TabletStudentId id) => Companion = id; }
public class TabletSpineScene { public TabletSpineScene(object mod, string name, string stem, string idle, bool blink) { } public void Update(float seconds) { } }
public class Mod { public Logger Logger = new(); }
public class Logger { public void Error(string message, Exception exception) => throw exception; }
public class Instance { public Mod Mod = new(); }
public class StartupStub { public void Start() { } }
public class LobbyCycleProbe {
    static bool Visible, _lobbyDayTime, _lobbyLoadFailed;
    static TabletSpineScene _lobbyArona = new(null, "", "", "", true);
    static Instance _instance = new();
    static StartupStub Startup = new();
    static float _lobbyBannerTime;
    static int resets;
    static void ResetLobbyMemoryScene() => resets++;
    static void AssertScene(bool day, TabletStudentId companion, int resetCount) {
        if (_lobbyDayTime != day || Main.LocalPlayer.Companion.Companion != companion || resets != resetCount)
            throw new Exception("Scene changed outside a fresh opening");
    }
    public static void Validate() {
        Main.dayTime = true; Open(); AssertScene(true, TabletStudentId.Arona, 1);
        Main.dayTime = false;
        for (int i = 0; i < 120; i++) UpdateLobby(1f / 60);
        Open(); AssertScene(true, TabletStudentId.Arona, 1); // Duplicate Open is ignored.
        Main.LocalPlayer.Companion.SetCompanion(TabletStudentId.Sora);
        UpdateLobby(.1f); AssertScene(true, TabletStudentId.Sora, 1);
        Visible = false; Open(); AssertScene(false, TabletStudentId.Plana, 2);
        Main.dayTime = true;
        for (int i = 0; i < 120; i++) UpdateLobby(1f / 60);
        AssertScene(false, TabletStudentId.Plana, 2);
        Visible = false; Open(); AssertScene(true, TabletStudentId.Arona, 3);
    }
'@
Add-Type -TypeDefinition ($stubs + "`n" + $openLobby + "`n" + $updateLobby + "`n" + $open + "`n}")
[LobbyCycleProbe]::Validate()
$report = [ordered]@{
    passed = $true
    actualOpeningAndUpdateMethods = $true
    dayToNightHeldUntilReopen = $true
    nightToDayHeldUntilReopen = $true
    duplicateOpenIgnored = $true
    manualCompanionPreservedWhileOpen = $true
    backgroundAndWaterUseSameSnapshot = $true
    assetAndGpuDependenciesStubbed = $true
}
$json = $report | ConvertTo-Json
[IO.File]::WriteAllText((Join-Path $PSScriptRoot 'lobby_cycle_validation.json'), $json)
$json
