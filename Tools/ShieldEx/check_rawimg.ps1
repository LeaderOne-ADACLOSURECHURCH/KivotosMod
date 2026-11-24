$ErrorActionPreference='Stop'
$root=Split-Path (Split-Path $PSScriptRoot -Parent) -Parent
$runner=Join-Path $env:TEMP 'KivotosAlphaRawProbe'
New-Item -ItemType Directory -Force $runner | Out-Null
@'
<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework><ImplicitUsings>enable</ImplicitUsings></PropertyGroup></Project>
'@ | Set-Content (Join-Path $runner 'Probe.csproj')
@'
using System.Reflection;
using System.Runtime.Loader;
using System.Runtime.InteropServices;
var game = @"D:\SteamLibrary\steamapps\common\tModLoader";
var libraries = Directory.GetFiles(Path.Combine(game,"Libraries"),"*.dll",SearchOption.AllDirectories)
    .Where(p=>!p.Contains("\\Native\\")&&!p.Contains("\\runtime"))
    .GroupBy(Path.GetFileNameWithoutExtension,StringComparer.OrdinalIgnoreCase)
    .ToDictionary(g=>g.Key!,g=>g.First(),StringComparer.OrdinalIgnoreCase);
AssemblyLoadContext.Default.Resolving += (ctx,name)=> {
    if(!libraries.TryGetValue(name.Name!,out var path))return null;
    var loaded=ctx.LoadFromAssemblyPath(path);
    if(name.Name=="FNA")NativeLibrary.SetDllImportResolver(loaded,(n,a,p)=> {
        var dll=Path.Combine(game,"Libraries","Native","Windows",n.EndsWith(".dll")?n:n+".dll");
        return File.Exists(dll)?NativeLibrary.Load(dll):IntPtr.Zero;
    });
    return loaded;
};
var assembly=AssemblyLoadContext.Default.LoadFromAssemblyPath(Path.Combine(game,"tModLoader.dll"));
var method=assembly.GetType("Terraria.ModLoader.IO.ImageIO")!.GetMethod("ToRaw")!;
Directory.CreateDirectory(args[1]);
int count=0;
foreach(var folder in new[]{"Shield","BlackHole"})foreach(var png in Directory.GetFiles(Path.Combine(args[0],"Assets","Skills",folder),"*.png")) {
    using var input=File.OpenRead(png);
    var output=new MemoryStream();
    if(!(bool)method.Invoke(null,new object[]{input,output})!)throw new Exception("ToRaw failed");
    File.WriteAllBytes(Path.Combine(args[1],folder+"_"+Path.GetFileNameWithoutExtension(png)+".rawimg"),output.ToArray());
    using var original=File.OpenRead(Path.Combine(args[0],"Tools","ShieldEx","SourceTextures",folder,Path.GetFileName(png)));
    var before=new MemoryStream();
    if(!(bool)method.Invoke(null,new object[]{original,before})!)throw new Exception("Original ToRaw failed");
    File.WriteAllBytes(Path.Combine(args[1],"original_"+folder+"_"+Path.GetFileNameWithoutExtension(png)+".rawimg"),before.ToArray());
    count++;
}
Console.WriteLine($"Converted {count} original and {count} fixed PNGs with installed tML ImageIO.ToRaw; no graphics device or mod assembly created.");
'@ | Set-Content (Join-Path $runner 'Program.cs')
& dotnet run --project (Join-Path $runner 'Probe.csproj') -- $root (Join-Path $runner 'raw')
if($LASTEXITCODE -ne 0){throw 'Installed rawimg conversion failed'}
& python (Join-Path $PSScriptRoot 'check_alpha.py') (Join-Path $runner 'raw')
if($LASTEXITCODE -ne 0){throw 'Alpha verification failed'}
