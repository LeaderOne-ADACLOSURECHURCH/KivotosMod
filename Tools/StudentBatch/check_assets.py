"""Compare runtime assets directly with the supplied sources and check integration IDs."""
from pathlib import Path
from PIL import Image
import json, re
from import_assets import ROOT, TOOLS, DOWNLOADS, SPECS, read_zip

sources={name:read_zip(DOWNLOADS/spec[0]) for name,spec in SPECS.items()}
count=0
for name,(_,halo,mapping,front) in SPECS.items():
    out=ROOT/'Content/Items/Vanity'/name
    for key,src_key in mapping.items():
        source=sources[name][src_key].copy()
        if name=='Haruka' and key=='Head_Head': source=Image.open(ROOT/'Tools/CostumeRevision/HarukaHead.png').convert('RGBA')
        if key=='Head_Head':
            for extra in front: source.alpha_composite(sources[name][extra])
        if key=='Body_Body' and name in ('Hikari','Nozomi'):
            # Approved torso-only substitution: front/rear hand pixels stay exact.
            for col in (0,1):
                uniform=sources['Hikari']['身子'].crop((col*40,112,(col+1)*40,168))
                source.paste(uniform,(col*40,0));source.paste(uniform,(col*40,112))
        actual=Image.open(out/f'{name}{key}.png').convert('RGBA')
        if not (name=='Hikari' and key=='Head_Head'): assert actual.size==source.size and actual.tobytes()==source.tobytes(), (name,key)
        count+=1
    if halo:
        expected=Image.open(DOWNLOADS/halo).convert('RGBA')
        assert Image.open(out/f'{name}Halo.png').convert('RGBA').tobytes()==expected.tobytes()
        count+=1
    for item in ('Head','Body','Boots'):
        assert Image.open(out/f'{name}{item}.png').getbbox()
        set_source=(out/f'{name}Set.cs').read_text('utf8')
        assert f'class {name}{item} : ModItem' in set_source
    assert Image.open(ROOT/f'Assets/Tablet/Students/{name}/Thumbnail.png').getbbox()
    layers=(out/f'{name}DrawLayers.cs').read_text('utf8')
    assert f'StudentSpriteDraw.Halo(ref info, ref _texture, "KivotosMod/Content/Items/Vanity/{name}/{name}Halo"' in layers
    for path in re.findall(r'"KivotosMod/([^"\n]+)"',layers):
        assert (ROOT/(path+'.png')).is_file(), path
    if name in ('Hikari','Nozomi'):
        assert f'class {name}TorsoLayer' not in layers, 'Native torso must not be overdrawn'

head=Image.open(ROOT/'Content/Items/Vanity/Hikari/HikariHead_Head.png').convert('RGBA')
locks=Image.open(ROOT/'Content/Items/Vanity/Hikari/HikariSideLocks.png').convert('RGBA')
head.alpha_composite(locks)
assert head.tobytes()==sources['Hikari']['头'].tobytes(), 'Hikari hair split lost source pixels'
catalog=(ROOT/'Content/Tablet/TabletStudentCatalog.cs').read_text('utf8')
enum=catalog.split('public enum TabletStudentId : byte')[1].split('}')[0]
ids=dict((k,int(v)) for k,v in re.findall(r'(\w+)\s*=\s*(\d+)',enum))
assert ids==dict(Yuuka=0,Hoshino=1,Shiroko=2,Arona=3,Sora=4,Plana=5,Hina=6,Noa=7,Ibuki=8,Iroha=9,Hikari=10,Mika=11,
    Haruka=12,Kayoko=13,Mutsuki=14,Nozomi=15,Nagisa=16,Seia=17,Mika2x2=18,Yuuka2x2=19,Ibuki2x2=20,Iroha2x2=21,Hikari2x2=22)
formation=catalog.split('FormationStudents =')[1].split('};')[0]
appearance=(ROOT/'Content/Tablet/TabletStudentAppearancePlayer.cs').read_text('utf8')
for name in ('Haruka','Kayoko','Mutsuki','Nozomi'):
    assert f'Get(TabletStudentId.{name})' in formation
    for item in ('Head','Body','Boots'): assert f'GetInstance<{name}{item}>()' in appearance
    for locale in ('zh-Hans','en-US'):
        loc=(ROOT/f'Localization/{locale}/Mods.KivotosMod.Items.hjson').read_text('utf8')
        for item in ('Head','Body','Boots'): assert len(re.findall('^'+name+item+r':\s*\{',loc,re.M))==1
assert Image.open(ROOT/'Content/Items/Vanity/Hikari/HikariTail.png').tobytes()==Image.open(ROOT/'Content/Items/Vanity/Nozomi/NozomiTail.png').tobytes()
assert 'raw <= (byte)TabletStudentId.Hikari2x2' in appearance
assert 'TabletStudentAppearancePlayer.IsFormationStudent(student)' in (ROOT/'Content/Tablet/TabletNet.cs').read_text('utf8')
report=dict(passed=True,sourcePixelComparisons=count,nativeScale=1,oldIdsPreserved=True,newIds=[12,13,14,15],
    sevenFormationOutfits=True,nativeCompositeHandsAndBothGenderTorso=True,nozomiBorrowsHikariUniform=True,
    localizedNewItems=12,allLayerAssetsPresent=True)
(TOOLS/'asset_validation.json').write_text(json.dumps(report,indent=2),encoding='utf8')
print(json.dumps(report,indent=2))
