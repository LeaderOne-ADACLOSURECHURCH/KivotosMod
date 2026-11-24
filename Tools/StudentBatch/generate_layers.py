"""Create the small per-student layers using the existing costume hooks."""
from pathlib import Path
import json
from import_assets import ROOT, TOOLS, SPECS

manifest=json.loads((TOOLS/'import_manifest.json').read_text('utf8'))
set_template=(ROOT/'Content/Items/Vanity/Ibuki/IbukiSet.cs').read_text('utf8')
hide_template=(ROOT/'Content/Items/Vanity/Ibuki/IbukiVanityPlayer.cs').read_text('utf8')
for name in SPECS:
    out=ROOT/'Content/Items/Vanity'/name
    if name in ('Haruka','Kayoko','Mutsuki','Nozomi'):
        (out/f'{name}Set.cs').write_text(set_template.replace('Ibuki',name),encoding='utf8')
        # Same equipment-only silhouette suppression; gameplay wing visuals stay available.
        source=hide_template.replace('Ibuki',name)
        (out/f'{name}VanityPlayer.cs').write_text(source,encoding='utf8')
    header=f'''using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria.DataStructures;
using Terraria.ModLoader;

namespace KivotosMod.Content.Items.Vanity.{name};
'''
    code=header
    assets=manifest[name]['assets']
    for suffix in ('Torso','BackHair','Tail','Wings','Coat','Halo'):
        if suffix not in assets: continue
        head=suffix in ('BackHair','Halo')
        visible='HeadVisible' if head else 'FullSetVisible' if suffix=='Wings' else 'BodyVisible'
        position='new BeforeParent(PlayerDrawLayers.Torso)' if suffix=='Torso' else \
            'new AfterParent(PlayerDrawLayers.Head)' if suffix=='Halo' else \
            'new Between(PlayerDrawLayers.Head, PlayerDrawLayers.ArmOverItem)' if suffix=='Coat' else \
            'new Between(PlayerDrawLayers.Wings, PlayerDrawLayers.BackAcc)'
        path=f'KivotosMod/Content/Items/Vanity/{name}/{name}{suffix}'
        if name=='Hikari' and suffix=='Tail': path='KivotosMod/Content/Items/Vanity/Nozomi/NozomiTail'
        draw=f'StudentSpriteDraw.Halo(ref info, ref _texture, "{path}", {manifest[name]["headTop"]})' if suffix=='Halo' else \
            f'StudentSpriteDraw.Strip(ref info, ref _texture, "{path}", head: {str(head).lower()}, legs: {str(suffix in ("Tail","Coat")).lower()})'
        if suffix=='BackHair' and name in ('Mutsuki','Nozomi'):
            # Reference screenshots place the body 16px into the 56px hair canvas.
            # Centering already accounts for 8px, so add the remaining -8px here.
            draw=draw[:-1]+', offsetX: -8f)'
        code+=f'''
[Autoload(Side = ModSide.Client)]
public sealed class {name}{suffix}Layer : PlayerDrawLayer
{{
    private static Asset<Texture2D> _texture;
    public override bool GetDefaultVisibility(PlayerDrawSet info)
        => !info.drawPlayer.dead && !info.drawPlayer.ghost && !info.drawPlayer.invis
            && {name}VanitySet.{visible}(info.drawPlayer);
    public override Position GetDefaultPosition() => {position};
    public override bool IsHeadLayer => {str(head).lower()};
    protected override void Draw(ref PlayerDrawSet info)
        => {draw};
}}
'''
    if name=='Haruka':
        code+='''
[Autoload(Side = ModSide.Client)]
public sealed class HarukaSideLocksLayer : PlayerDrawLayer
{
    private static Asset<Texture2D> _texture;
    internal static bool Grounded(PlayerDrawSet info) => !info.drawPlayer.mount.Active
        && info.drawPlayer.itemAnimation <= 0 && System.Math.Abs(info.drawPlayer.velocity.Y) < .1f;
    internal static bool HasHair(PlayerDrawSet info) => !info.drawPlayer.dead && !info.drawPlayer.ghost
        && !info.drawPlayer.invis && HarukaVanitySet.HeadVisible(info.drawPlayer);
    public override bool GetDefaultVisibility(PlayerDrawSet info) => HasHair(info) && !Grounded(info);
    public override Position GetDefaultPosition() => new AfterParent(PlayerDrawLayers.Head);
    public override bool IsHeadLayer => true;
    internal static void DrawHair(ref PlayerDrawSet info) => StudentSpriteDraw.Strip(ref info, ref _texture,
        "KivotosMod/Content/Items/Vanity/Haruka/HarukaSideLocks", head: true);
    protected override void Draw(ref PlayerDrawSet info) => DrawHair(ref info);
}

[Autoload(Side = ModSide.Client)]
public sealed class HarukaIdleSideLocksLayer : PlayerDrawLayer
{
    public override bool GetDefaultVisibility(PlayerDrawSet info)
        => HarukaSideLocksLayer.HasHair(info) && HarukaSideLocksLayer.Grounded(info);
    public override Position GetDefaultPosition() => new Between(PlayerDrawLayers.ArmOverItem, PlayerDrawLayers.BladedGlove);
    public override bool IsHeadLayer => true;
    protected override void Draw(ref PlayerDrawSet info) => HarukaSideLocksLayer.DrawHair(ref info);
}
'''
    (out/f'{name}DrawLayers.cs').write_text(code,encoding='utf8')
print('Generated seven costume layer files and four new vanity sets using existing hooks.')
