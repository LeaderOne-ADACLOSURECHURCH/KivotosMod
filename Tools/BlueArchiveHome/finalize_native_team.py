"""Document and verify the native export, without building KivotosMod."""
import csv
import hashlib
import json
import shutil
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont
from export_native_team import OUT, ROOT, write_json, write_csv


def read(relative):
    return json.loads((OUT / relative).read_text('utf8'))


def main():
    manifest=read('来源与校验/导出清单.json')
    widgets=read('布局/UILobby_完整组件.json')
    sprites=list(csv.DictReader((OUT/'UI/切图与九宫格参数.csv').open(encoding='utf-8-sig')))
    atlases=read('UI/图集清单.json')
    atlas_by_id={a['pathId']:a for a in atlases}
    sprite_by_name={(r['atlas'],r['name']):r for r in sprites}
    references=[];missing=[];used={}
    for w in widgets:
        d=w['parameters'];name=d.get('mSpriteName')
        if not name:continue
        atlas=atlas_by_id.get(str(d.get('mAtlas',{}).get('m_PathID')))
        sprite=sprite_by_name.get((atlas['name'],name)) if atlas else None
        row=dict(path=w['path'],scriptType=w['scriptType'],spriteName=name,
            atlas=atlas['name'] if atlas else '',file=sprite['file'] if sprite else '',
            width=d.get('mWidth'),height=d.get('mHeight'),depth=d.get('mDepth'),spriteType=d.get('mType'))
        if sprite:
            row.update({k:sprite[k] for k in ('borderLeft','borderRight','borderTop','borderBottom',
                'paddingLeft','paddingRight','paddingTop','paddingBottom')})
            used[(sprite['atlas'],sprite['name'])]=sprite
        else:missing.append(row)
        references.append(row)
    write_csv(OUT/'UI/大厅素材引用.csv',references)
    write_json(OUT/'来源与校验/大厅切片引用缺失.json',missing)
    write_csv(OUT/'布局/UIGrid_布局参数.csv',[
        dict(path=w['path'],**{k:v for k,v in w['parameters'].items() if k not in ('m_GameObject','m_Script')})
        for w in widgets if w['scriptType']=='UIGrid'])
    write_json(OUT/'布局/UIPanel_裁剪参数.json',[
        dict(path=w['path'],parameters=w['parameters']) for w in widgets if w['scriptType']=='UIPanel'])

    columns=6;cell_w=310;cell_h=180
    sheet=Image.new('RGB',(columns*cell_w,((len(used)+columns-1)//columns)*cell_h),'#c5d2df')
    draw=ImageDraw.Draw(sheet);font=ImageFont.truetype('C:/Windows/Fonts/arial.ttf',13)
    for i,r in enumerate(sorted(used.values(),key=lambda r:(r['atlas'],r['name']))):
        im=Image.open(OUT/r['file']).convert('RGBA');im.thumbnail((150,125),Image.Resampling.LANCZOS)
        x=i%columns*cell_w;y=i//columns*cell_h
        sheet.paste(im,(x+(cell_w-im.width)//2,y+3),im)
        draw.text((x+5,y+135),r['name'],font=font,fill='#183a58')
        draw.text((x+5,y+155),f"{r['width']}x{r['height']} / {r['atlas']}",font=font,fill='#183a58')
    sheet.save(OUT/'UI/大厅使用素材索引.jpg',quality=94)
    manifest['ui']['usedSprites']=len(used)

    sizes=sorted({w['parameters']['mFontSize'] for w in widgets if w['scriptType']=='UILabel'})
    write_json(OUT/'字体/DFG编译任务.json',dict(
        status='awaiting_user_format_or_tool',requestedExtension='.dfg',generatedFiles=[],
        reason='用户指定实际 .dfg 文件，待提供工具或格式说明后编译。',
        source='字体/原始字体；全部字体直接来自游戏 resources.assets',
        pairs=[dict(locale='日文',normal='U-OTF-ShinMGoUpr-Medium.otf',bold='U-OTF-ShinMGoUpr-DeBold.otf',
                    nativeNormalWeight=505,nativeBoldWeight=600),
               dict(locale='简体中文',normal='SourceHanSansSC-Regular.otf',bold='SourceHanSansSC-Bold.otf',
                    nativeNormalWeight=400,nativeBoldWeight=700)],
        nativeLobbyFontSizes=sizes,characterSets='字符集/*.txt；按原字体 cmap 导出',
        note='原生粗体字体已存在，不需要从普通字体合成加粗。字体尺寸/字符范围/打包方式依实际工具确定。'))

    guide='''# 碧蓝档案日本 PC 客户端：原生主大厅 UI 参考包

来源：`Z:/YostarGames/BlueArchive_JP/BlueArchive_Data`。
主预制体地址：`Assets/_MX/AddressableAsset/UI/UILobby.prefab`。
本包直接读取游戏资源，包含原生预制体、图集、切片、字体及参数。数据没有经过平板缩放或 KivotosMod 的布局修改。

## 从这里开始

| 需求 | 文件或目录 |
| --- | --- |
| 查看大厅所用图标 | `UI/大厅使用素材索引.jpg` |
| 从控件找到原图 | `UI/大厅素材引用.csv`、`UI/直接纹理引用.json` |
| 原图集及全部切片 | `UI/图集`、`UI/切图` |
| 九宫格与透明留白 | `UI/切图与九宫格参数.csv` |
| 查位置、尺寸、锚点、层次 | `布局/UILobby_布局参数.csv` |
| 查字号、加粗、字距与样式 | `布局/UILobby_文字与字体样式.csv` |
| 查网格排布、裁剪 | `布局/UIGrid_布局参数.csv`、`布局/UIPanel_裁剪参数.json` |
| 完整节点和组件参数 | `布局/原生预制体全部层级.json`、`布局/UILobby_完整组件.json` |
| 原始字体和字符覆盖 | `字体/原始字体`、`字体/字符集` |
| 查看字体形态 | `字体/字体预览.png` |
| 复查原始游戏数据 | `原始数据/AssetBundles`、`原始数据/预制体全部对象.typetree.json` |
| 来源与完整性 | `来源与校验/导出清单.json`、`来源与校验/验证结果.json`、`来源与校验/文件校验.csv` |

`Common` 是大厅引用的图集。`Combat`、`Emoji`、`Floater` 是同一资源包里的附带图集；`lobbyReferenced` 标记可区分。`UI/原始纹理` 也保留已载入依赖包的附带纹理，优先用“大​​厅素材引用”和“直接纹理引用”寻找主界面素材。

PNG 为解码后的原始 RGBA，没有预乘 alpha、调色或尺寸调整。同名及仅大小写不同的资源使用 `__来源标识` 后缀分开保存，原名称仍在清单中。切片预览图用了底色与缩略图，仅用于索引；开发时使用透明 PNG。

## 字体

| 原生字族 | 普通 | 原生加粗 |
| --- | --- | --- |
| U-OTF Shin Maru Go Upr | `U-OTF-ShinMGoUpr-Medium.otf`，weight 505 | `U-OTF-ShinMGoUpr-DeBold.otf`，weight 600 |
| Source Han Sans SC | `SourceHanSansSC-Regular.otf`，weight 400 | `SourceHanSansSC-Bold.otf`，weight 700 |

额外导出了游戏内嵌的 `PerfectDOSVGA437.ttf`、`LiberationSans.ttf`。六个文件均是游戏 Font 对象中的原始二进制。元数据保留 Unity 字号、行距等字段，字体数据单独存为 OTF/TTF。

多数大厅 UILabel 的 `mTrueTypeFont` 是空引用，并设有 `UseLocalizeFont` 和 `IsBold`，由运行时本地化字体服务绑定。包内提供实际字体和原始选择标记，不把空引用伪装成固定的字体绑定。

`.dfg` 尚未编译：用户确认需要实际 `.dfg` 文件，将提供工具或格式说明。`字体/DFG编译任务.json` 记录普通/粗体源文件、字符集、原生字号及待处理状态。

## 坐标和参数

1. `localX/Y/Z`、`scaleX/Y` 来自 Unity Transform；Y 向上。位置单位及宽高是原生 NGUI 布局单位，不能直接当成任意分辨率屏幕上的像素坐标。
2. `nominalWorldX/Y`、`nominalWorldMatrix` 是按序列化父子 Transform 计算的静态值。运行时锚点、UIGrid 重排、屏幕缩放、可见性和文字内容会改变最终显示。包内没有模拟这些运行逻辑。
3. 四个 Anchor 的 `target`、`relative`、`absolute` 保留原始值；CSV 将本预制体内部目标补为层级路径。存在的空引用保持为空。
4. `mWidth/mHeight` 为控件尺寸；`mDepth` 为 NGUI 绘制深度，另有 UIPanel 和特效渲染顺序。`mPivot`、`mType` 等枚举按游戏序列化整数保留。
5. 图集切片 `x/y/width/height` 对应导出 PNG 的左上角矩形；`borderLeft/Right/Top/Bottom` 是九宫格边距，`padding*` 是透明留白参数。切片 PNG 不自动补 padding。
6. UILabel 的 `mFontStyle` 和游戏扩展 `IsBold` 分别保留；同时保留 `mFontSize`、`mAlignment`、字距、行距、颜色、渐变、溢出和描边/阴影参数。字体样式的全部数据以完整组件 JSON 为准。
7. `serializedText` 是预制体保存的样例或占位文字；实际语言文字与账号数值由运行时填充。引用本地化文字时使用 `LocalizeKey`，不要把占位文字视为最终文案。
8. `activeSelf`、组件 `m_Enabled` 是序列化默认状态。功能解锁、活动 Banner、角色切换与服务器数据会控制实时状态。
9. JSON 中顶层 `pathId` 用字符串保存；原始 typetree 内的 `m_PathID` 仍是 64 位整数。JavaScript 读取原始 typetree 时需用支持无损整数的解析器，避免引用精度丢失。

例如：底栏 Menu/BG/Grid 的 cellWidth=258、cellHeight=200；任务 TagGrid 为 200×98；Banner Indicator 网格为 22×22。这些是原生网格间距，并非单个图标 PNG 的尺寸。

## 导出与校验

`工具/export_native_team.py` 负责从游戏读取，`工具/finalize_native_team.py` 生成参考索引及验证，依赖 Python、UnityPy、Pillow、numpy、fontTools。`catalog.py` 解析该客户端 Addressables 目录；重跑前按实际安装位置修改源路径与输出路径。

验证覆盖清单文件 SHA-256、复制的原生 AssetBundle 与游戏文件一致、全部切片与图集矩形逐像素一致、原始字体哈希一致、预制体解析情况和大厅素材引用情况。校验结果的具体数量以 JSON 为准。
'''
    guide=guide.replace('大​​厅','大厅')
    (OUT/'使用说明.md').write_text(guide,encoding='utf8')
    for name in ('export_native_team.py','finalize_native_team.py','catalog.py'):
        target=OUT/'工具'/name;target.parent.mkdir(exist_ok=True)
        shutil.copy2(Path(__file__).parent/name,target)

    failures=[]
    for r in manifest['files']:
        path=OUT/r['path']
        if not path.exists() or hashlib.sha256(path.read_bytes()).hexdigest()!=r['sha256']:
            failures.append(dict(check='manifestHash',file=r['path']))
        if r['kind']=='original game bundle':
            if hashlib.sha256((ROOT/'AssetBundles'/r['source']).read_bytes()).hexdigest()!=r['sha256']:
                failures.append(dict(check='sourceBundleHash',file=r['path']))
    originals={a['name']:Image.open(OUT/a['texture']).convert('RGBA') for a in atlases}
    for r in sprites:
        x,y,w,h=(int(r[k]) for k in ('x','y','width','height'))
        original=originals[r['atlas']].crop((x,y,x+w,y+h))
        with Image.open(OUT/r['file']) as cut:
            if cut.size!=(w,h) or cut.convert('RGBA').tobytes()!=original.tobytes():
                failures.append(dict(check='spritePixels',file=r['file']))
    for r in manifest['fonts']:
        if hashlib.sha256((OUT/r['file']).read_bytes()).hexdigest()!=r['originalSha256']:
            failures.append(dict(check='fontHash',file=r['file']))
    failures+= [dict(check='missingLobbySprite',**r) for r in missing]
    verification=dict(passed=not failures,manifestHashes=len(manifest['files']),
        nativeBundleHashes=sum(r['kind']=='original game bundle' for r in manifest['files']),
        exactSpritePixelComparisons=len(sprites),nativeFonts=len(manifest['fonts']),
        lobbySpriteReferences=len(references),uniqueLobbySprites=len(used),missingLobbySprites=len(missing),
        unparsedObjects=manifest['ui']['unparsedObjects'],directTextureUnresolved=manifest['ui']['directTextureUnresolved'],
        fontCompilation='pending actual .dfg format/tool supplied by user',failures=failures)
    if verification['unparsedObjects'] or verification['directTextureUnresolved']:verification['passed']=False
    write_json(OUT/'来源与校验/导出清单.json',manifest)
    write_json(OUT/'来源与校验/验证结果.json',verification)
    inventory=[]
    for path in sorted(OUT.rglob('*')):
        if not path.is_file() or path==OUT/'来源与校验/文件校验.csv':continue
        inventory.append(dict(file=path.relative_to(OUT).as_posix(),bytes=path.stat().st_size,
            sha256=hashlib.sha256(path.read_bytes()).hexdigest()))
    write_csv(OUT/'来源与校验/文件校验.csv',inventory)
    print(json.dumps(verification,ensure_ascii=False,indent=2))
    print('Inventory',len(inventory),'files;',sum(r['bytes'] for r in inventory),'bytes')
    if not verification['passed']:raise SystemExit(1)


if __name__=='__main__':main()
