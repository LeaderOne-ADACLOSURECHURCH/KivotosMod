using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.DataStructures;
using Terraria.ObjectData;
using Terraria.GameContent.ObjectInteractions;

namespace KivotosMod.Content.Items.Placeable;

public sealed class StepByStepMusicBox : ModItem
{
    public override string Texture => "Terraria/Images/Item_" + ItemID.MusicBox;
    public override LocalizedText DisplayName => Language.GetOrRegister("Mods.KivotosMod.Items.StepByStepMusicBox.DisplayName", () => "音乐盒（Step by Step）");
    public override LocalizedText Tooltip => Language.GetOrRegister("Mods.KivotosMod.Items.StepByStepMusicBox.Tooltip", () => "播放基沃托斯主界面音乐\n可装备，或放置后右键及电线开关");
    public override void SetStaticDefaults()
    {
        ItemID.Sets.CanGetPrefixes[Type] = false;
        ItemID.Sets.ShimmerTransformToItem[Type] = ItemID.MusicBox;
        MusicLoader.AddMusicBox(Mod, MusicLoader.GetMusicSlot(Mod, "Assets/Music/Step_by_Step"), Type, ModContent.TileType<StepByStepMusicBoxTile>());
    }
    public override void SetDefaults() => Item.DefaultToMusicBox(ModContent.TileType<StepByStepMusicBoxTile>(), 0);
    public override void AddRecipes() => CreateRecipe().AddIngredient(ItemID.MusicBox).AddIngredient(ItemID.FallenStar, 5).AddTile(TileID.WorkBenches).Register();
}

public sealed class StepByStepMusicBoxTile : ModTile
{
    public override string Texture => "Terraria/Images/Tiles_" + TileID.MusicBoxes;
    public override void SetStaticDefaults()
    {
        Main.tileFrameImportant[Type] = true;
        Main.tileObsidianKill[Type] = true;
        // Vanilla music-box art has no Tiles_139_Highlight asset.
        TileID.Sets.DisableSmartCursor[Type] = true;
        TileObjectData.newTile.CopyFrom(TileObjectData.Style2x2);
        TileObjectData.newTile.Origin = new Point16(0, 1);
        TileObjectData.newTile.LavaDeath = false;
        TileObjectData.newTile.DrawYOffset = 2;
        TileObjectData.newTile.StyleLineSkip = 2;
        TileObjectData.addTile(Type);
        AddMapEntry(new Color(100, 200, 240), ModContent.GetInstance<StepByStepMusicBox>().DisplayName);
    }
    public override bool HasSmartInteract(int i, int j, SmartInteractScanSettings settings) => true;
    public override void MouseOver(int i, int j)
    {
        Main.LocalPlayer.noThrow = 2;
        Main.LocalPlayer.cursorItemIconEnabled = true;
        Main.LocalPlayer.cursorItemIconID = ModContent.ItemType<StepByStepMusicBox>();
    }
    // Registered mod music boxes are switched and synced by tML's native
    // Player interaction and Wiring paths. A custom toggle would switch twice.
}
