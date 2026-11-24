"""Verify packaged assets against the supplied archives, plus framing/save IDs."""
from pathlib import Path
from PIL import Image
import hashlib, io, json, re, wave, zipfile

tools = Path(__file__).resolve().parent
root = tools.parents[1]
out = root/'Content/Items/Vanity/Mika'
downloads = Path('C:/Users/Sunbinbin/Downloads')
with zipfile.ZipFile(downloads/'\u672a\u82b11X1.zip') as z:
    src = {}
    for f in z.infolist():
        if f.is_dir() or f.filename.startswith('__MACOSX/'): continue
        name = f.filename.encode('cp437').decode('utf8') if not f.flag_bits & 0x800 else f.filename
        src[Path(name).stem] = Image.open(io.BytesIO(z.read(f))).convert('RGBA')
head = src['\u5934'].copy()
expected = {'MikaHead_Head':head, 'MikaBoots_Legs':src['\u817f'], 'MikaBackHair':src['\u540e\u53d1'],
    'MikaSideLocks':src['\u9b13\u53d1'], 'MikaTorso':src['\u8eab\u5b50'], 'MikaHalo':src['\u5149\u73af'],
    'MikaDecorWings':src['\u7fc5\u8180'], 'MikaFlight':src['\u7fc5\u8180\uff08\u98de\u884c\uff09']}
for name, image in expected.items():
    actual = Image.open(out/(name+'.png')).convert('RGBA')
    assert actual.size == image.size and actual.tobytes() == image.tobytes(), name+' pixels changed'
    if name=='MikaHalo': continue
    h = 68 if name=='MikaFlight' else 74 if name=='MikaDecorWings' else 56
    frames = 5 if name=='MikaFlight' else 20
    assert actual.height == h*frames
    for frame in range(frames):
        if name=='MikaSideLocks': continue  # Source hides the locks in some raised-arm poses.
        assert actual.crop((0,frame*h,actual.width,(frame+1)*h)).getbbox(), (name, frame)
assert Image.open(out/'MikaBody_Body.png').size == (360,224)
arm_source = Image.open(tools/'SourceArms.png').convert('RGBA')
arm_output = Image.open(out/'MikaBody_Body.png').convert('RGBA')
assert arm_output.tobytes() == arm_source.tobytes() and arm_output.getbbox()
for row in range(4):
    for col in range(2,9):
        assert arm_output.crop((col*40,row*56,(col+1)*40,(row+1)*56)).getbbox(), 'Missing composite arm pose'
assert not (out/'MikaWings.cs').exists() and not (out/'MikaWings.png').exists() and not (out/'MikaWingsBlank.png').exists()
for name in ['MikaHead','MikaBody','MikaBoots']:
    assert Image.open(out/(name+'.png')).getbbox(), 'Missing inventory icon'
# Wide sheets keep the reference's body origin; Hina's different canvas padding
# must not be applied to Mika a second time.
mika_draw = (out/'MikaDrawLayers.cs').read_text('utf8')
assert 'MikaArtLayout.CanvasOrigin(height, info.playerEffect)' in mika_draw
assert 'MikaArtLayout.CanvasOrigin(74, info.playerEffect)' in mika_draw
assert 'horizontalCompensation' not in mika_draw
assert 'Color.White * (1f - info.shadow)' in mika_draw
assert 'MikaArtLayout.HaloOffset(Main.GlobalTimeWrappedHourly) * p.gravDir' in mika_draw
assert 'new Between(PlayerDrawLayers.Wings, ModContent.GetInstance<MikaWingLayer>())' in mika_draw
assert Image.open(root/'Assets/Tablet/Students/Mika/Thumbnail.png').getbbox()
with zipfile.ZipFile(downloads/'\u672a\u82b1.zip') as z:
    files = [f for f in z.infolist() if not f.is_dir() and f.filename.lower().endswith('.wav')]
    assert len(files)==4
    for i,f in enumerate(files,1):
        packaged = (root/f'Assets/Sounds/Students/Mika/Hurt{i}.wav').read_bytes()
        assert hashlib.sha256(packaged).digest()==hashlib.sha256(z.read(f)).digest()
        with wave.open(io.BytesIO(packaged)) as w:
            assert w.getnchannels()==2 and w.getsampwidth()==2 and w.getframerate()==44100
            assert w.getnframes()>0
catalog = (root/'Content/Tablet/TabletStudentCatalog.cs').read_text('utf8')
enum = catalog.split('public enum TabletStudentId : byte')[1].split('}')[0]
ids = dict((name,int(value)) for name,value in re.findall(r'(\w+)\s*=\s*(\d+)',enum))
assert ids==dict(Yuuka=0,Hoshino=1,Shiroko=2,Arona=3,Sora=4,Plana=5,Hina=6,Noa=7,Ibuki=8,Iroha=9,Hikari=10,Mika=11,
    Haruka=12,Kayoko=13,Mutsuki=14,Nozomi=15,Nagisa=16,Seia=17,Mika2x2=18,Yuuka2x2=19,Ibuki2x2=20,Iroha2x2=21,Hikari2x2=22)
formation = catalog.split('FormationStudents =')[1].split('};')[0]
assert 'Get(TabletStudentId.Mika)' in formation
appearance = (root/'Content/Tablet/TabletStudentAppearancePlayer.cs').read_text('utf8')
assert 'raw <= (byte)TabletStudentId.Hikari2x2' in appearance
assert 'or TabletStudentId.Mika' in appearance.split('internal static bool IsFormationStudent')[1].split(';')[0]
assert 'GetInstance<MikaHead>()' in appearance and 'GetInstance<MikaBody>()' in appearance and 'GetInstance<MikaBoots>()' in appearance
for locale in ['zh-Hans','en-US']:
    loc = (root/f'Localization/{locale}/Mods.KivotosMod.Items.hjson').read_text('utf8')
    assert not re.search(r'^MikaWings:\s*\{',loc,re.M)
    for name in ['MikaHead','MikaBody','MikaBoots']:
        assert len(re.findall(r'^'+name+r':\s*\{',loc,re.M))==1
report=dict(passed=True, originalScale=1, spritePixelComparisons=len(expected), completeTwentyPoseSheets=True,
    flightFrames=5, compositeArmPoses=28, originalPcmVoices=4, validPcmFrames=True, oldStudentIdsPreserved=True,
    formationAndAppearanceSaveAndNetworkIds=True, itemIconsAndThumbnailAndLocalization=True,
    sharedReferenceCanvasOrigin=True, standaloneWingItemRemoved=True, haloFullBrightAndBob=True)
(tools/'asset_validation.json').write_text(json.dumps(report,indent=2),encoding='utf8')
print(json.dumps(report,indent=2))
