using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
var root = args[0];
var game = @"D:\SteamLibrary\steamapps\common\tModLoader";
var pack = Directory.GetDirectories(@"C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref").Where(x=>Path.GetFileName(x).StartsWith("8.")).OrderBy(x=>x).Last();
var refs = new Dictionary<string,string>(StringComparer.OrdinalIgnoreCase);
foreach(var p in Directory.GetFiles(Path.Combine(game,"Libraries"),"*.dll",SearchOption.AllDirectories)) {
 var n=p.Replace('\\','/'); if(n.Contains("/Native/")||n.Contains("/runtime")||n.Contains("/tModCodeAssist/")||n.EndsWith(".resources.dll"))continue;
 refs[Path.GetFileName(p)] = p;
}
foreach(var p in Directory.GetFiles(Path.Combine(pack,"ref","net8.0"),"*.dll"))refs[Path.GetFileName(p)]=p;
refs["tModLoader.dll"]=Path.Combine(game,"tModLoader.dll");
var files=Directory.GetFiles(root,"*.cs",SearchOption.AllDirectories).Where(p=>!(Path.GetRelativePath(root,p).Split(Path.DirectorySeparatorChar)[0] is "Tools" or "obj" or "bin")).ToArray();
var opt=new CSharpParseOptions(LanguageVersion.CSharp12,preprocessorSymbols:new[]{"TML_2026_08","JETBRAINS_ANNOTATIONS"});
var trees=files.Select(p=>CSharpSyntaxTree.ParseText(File.ReadAllText(p),opt,p));
var comp=CSharpCompilation.Create("KivotosMod",trees,refs.Values.Select(p=>MetadataReference.CreateFromFile(p)),new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary,allowUnsafe:true));
var diagnostics=comp.GetDiagnostics();
foreach(var d in diagnostics.Where(x=>x.Severity==DiagnosticSeverity.Error))Console.WriteLine(d);
Console.WriteLine($"Sources={files.Length}, references={refs.Count}, errors={diagnostics.Count(d=>d.Severity==DiagnosticSeverity.Error)}, warnings={diagnostics.Count(d=>d.Severity==DiagnosticSeverity.Warning)}; no mod assembly emitted");
Environment.ExitCode=diagnostics.Any(d=>d.Severity==DiagnosticSeverity.Error)?1:0;
