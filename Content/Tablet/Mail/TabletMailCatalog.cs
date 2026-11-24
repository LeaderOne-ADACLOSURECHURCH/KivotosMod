using System;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using KivotosMod.Content.Items.Placeable;

namespace KivotosMod.Content.Tablet.Mail;

public sealed record MailDefinition(int Id, string Sender, string Title, string Body, string Portrait, int Time, bool Windy = false);
public readonly record struct MailGift(int Type, int Stack = 1);

public static class TabletMailCatalog
{
    public static readonly MailDefinition[] Stories = {
        new(0, "阿罗娜", "新的旅程！", "您好，老师！新的旅程马上就要开始了，虽说是一次突如其来的意外将我们卷入到这里，但是既然来了，那就暂时抛下所有的工作和烦恼，好好享受这次旅行吧！温暖的阳光，宁静的山谷，还有很多等待我们去探索和发掘的秘密...哦！最重要的是，还有老师在，嘿嘿~！光是想一想，心里就开始激动起来了呢。", "Hoshino", 0, false),
        new(1, "优香", "午饭时间到了。", "老师，您好！无论您现在正忙于四处奔跑，还是忙于挥动手中的工具，都希望您能先停下来休息休息。还有...这是我亲手做的苹果派，希望您能尝尝我的手艺......请一定要好好吃饭哦！", "Yuuka", 27000, false),
        new(2, "渚", "老师，近来可好？", "您好老师！最近过得怎么样？即使到了另一个世界，老师依然在为我们不停费心操劳，我在此代表全体崔尼蒂学员向您致谢！我最近依然在负责处理崔尼蒂的相关事务，毕竟还有其他不少的学员也被卷了进来。不过，在这个难得的下午茶时光中，希望您也能休息片刻。", "Iroha", 37800, false),
        new(3, "花子", "老师，可以吗？", "老师可爱起来，会是什么样子呢♥？", "Hoshino", 27000, false),
        new(4, "优香", "美味的卡路里。", "为什么这里的食物都那么好吃啊！？明明都已经计算好了每天的摄入量的.....再这样下去，摄入的卡路里就要超标了。可以请老师帮忙分担一些吗？", "Yuuka", 48600, false),
        new(5, "爱丽丝", "真正的勇者！", "我再此赐予汝等光芒之希望，用意志穿透那黑暗之腐朽，勇者啊，快去照亮幽暗的长夜吧！", "Hikari", 59400, false),
        new(6, "未花", "老师，快来许愿吧！", "老师！我捡到了一颗从天上掉下来的流星，一闪一闪可漂亮了。大家都说对着流星许愿会特别灵验，可是流星划过夜空的时间实在是太短暂了，但这次，我把它抓了起来，现在可以慢慢地，将所有的愿望都告诉它啦~！我们快来一起许愿吧，老师！", "Hoshino", 70200, false),
        new(7, "白子", "沉迷于钓鱼。", "老师，最近我一直在这个世界的各处钓鱼，这里的鱼总是稀奇古怪的，每次拉杆都能给我意想不到的惊喜。感谢老师平日里对我的关照，这是我刚钓到的一条金色鲤鱼，希望老师会喜欢。", "Shiroko", 30600, false),
        new(8, "春香", "老师，对不起...可以收下吗？", "老师...我...，这是我亲手培育...种植的...海草，希望您可以收下。要是不喜欢的话...也可以不用收下...呃...对不起...给老师添麻烦了...\n对不起...", "Haruka", 34200, false),
        new(9, "伊吹", "要来一起玩吗，老师？", "今天天气很适合放风筝呢，前辈给伊吹买了好多的风筝和玩具，伊吹送老师一些。希望老师有空了常来陪我玩。", "Ibuki", 27000, true),
        new(10, "小光", "呜——呜——！", "呜~~呜~~,小矿车来了——，咻~~咻~~。", "Hikari", 19800, false)
    };
    public static readonly MailDefinition[] Daily = {
        new(100,"阿罗娜","每天开心！","欢迎，老师，这是为您准备的礼物，希望您会喜欢！","Hoshino",0),
        new(101,"普拉娜","欢迎回来！","欢迎回来，老，师，这是我们送您的礼物！","Hina_dress",0),
        new(102,"阿罗娜","辛苦啦~！","老师！今天也请您多多关照啦~","Hoshino",0)
    };
    public static MailDefinition Definition(int template) => template >= 100 ? Daily[Math.Clamp(template-100,0,2)] : Stories[Math.Clamp(template,0,Stories.Length-1)];
    public static List<MailGift> StoryGifts(int id) => id switch {
        0 => new() { new(ItemID.HerbBag), new(ItemID.Apple),new(ModContent.ItemType<MailOrange>()),new(ItemID.Banana),new(ItemID.CloudinaBottle),new(ItemID.Wood,300),new(ItemID.StoneBlock,50),new(ItemID.IronBar,10),new(ModContent.ItemType<StepByStepMusicBox>()) },
        1 => new() { new(ModContent.ItemType<YuukaApplePie>()) },
        2 => new() { new(ItemID.Teacup),new(ItemID.NaturesGift) },
        3 => new() {new(ItemID.Daybloom),new(ItemID.Moonglow),new(ItemID.Blinkroot),new(ItemID.Waterleaf),new(ItemID.Deathweed),new(ItemID.Shiverthorn),new(ItemID.Fireblossom)},
        4 => new() {new(ItemID.ChickenNugget,3),new(ItemID.Burger,2),new(ItemID.Fries,2)},
        5 => new() {new(ItemID.EnchantedSword)},
        6 => new() {new(ItemID.StarinaBottle)},
        7 => new() {new(ItemID.GoldenCarp)},
        8 => new() {new(ItemID.SeaweedPlanter)},
        9 => new() {new(ItemID.PinWheel),new(ItemID.KiteGoldfish)},
        10 => new() {new(ItemID.ShroomMinecart)},
        _ => new()
    };
    private static readonly int[] Foods = {
        ItemID.Bacon,ItemID.BBQRibs,ItemID.Milkshake,ItemID.Steak,ItemID.ApplePie,ItemID.Apple,ItemID.Cherry,ItemID.Mango,ItemID.Banana,ItemID.Peach,ItemID.Burger,ItemID.Sashimi,ItemID.GrapeJuice,ItemID.Hotdog,ItemID.Pizza,ItemID.ChocolateChipCookie,ItemID.ShrimpPoBoy,ItemID.Grapes,ItemID.Nachos,ItemID.IceCream,ItemID.ChickenNugget,ItemID.FriedEgg,ItemID.CoffeeCup,ItemID.BananaSplit,ItemID.RoastedDuck,ItemID.LobsterTail,ItemID.CookedShrimp,ItemID.Fries,ItemID.PumpkinPie,ItemID.BowlofSoup,ItemID.Dragonfruit,ItemID.Starfruit,ItemID.PotatoChips,ItemID.FruitSalad,ItemID.MilkCarton,ItemID.TropicalSmoothie,ItemID.FruitJuice,ItemID.Lemonade,ItemID.AppleJuice
    };
    private static readonly int[] Special = { ItemID.MagicConch,ItemID.RodofDiscord,ItemID.FairyBell,ItemID.ResplendentDessert,ItemID.PDA,ItemID.VoidVault,ItemID.BottomlessBucket };
    public static readonly int[][] Accessories = {
        new int[] {ItemID.LuckyHorseshoe,ItemID.BandofRegeneration,ItemID.CreativeWings,ItemID.SandstorminaBottle,ItemID.TsunamiInABottle,ItemID.ShinyRedBalloon,ItemID.HermesBoots,ItemID.ClimbingClaws,ItemID.IceSkates,ItemID.ShoeSpikes,ItemID.LavaCharm,ItemID.FeralClaws},
        new int[] {ItemID.PhilosophersStone,ItemID.CrossNecklace,ItemID.StarCloak,ItemID.ReconScope,ItemID.RangerEmblem,ItemID.LavaWaders,ItemID.BundleofBalloons},
        new int[] {ItemID.MoonStone,ItemID.SunStone,ItemID.PaladinsShield,ItemID.ReflectiveShades,ItemID.CountercurseMantra,ItemID.ThePlan,ItemID.MedicatedBandage,ItemID.ArmorBracing}
    };
    public static int Stage => NPC.downedMoonlord ? 3 : NPC.downedPlantBoss ? 2 : Main.hardMode ? 1 : 0;
    public static List<MailGift> DailyGifts(int stage)
    {
        MailGift Pick(MailGift[] pool) => pool[Main.rand.Next(pool.Length)];
        var primary = stage switch {
            0 => new[] {new MailGift(ItemID.IronBar,40),new(ItemID.GoldBar,30),new(ItemID.PlatinumBar,25),new(WorldGen.crimson?ItemID.CrimtaneBar:ItemID.DemoniteBar,20)},
            1 => new[] {new MailGift(ItemID.CobaltBar,35),new(ItemID.PalladiumBar,35),new(ItemID.MythrilBar,25),new(ItemID.OrichalcumBar,25),new(ItemID.TitaniumBar,20),new(ItemID.AdamantiteBar,20),new(ItemID.HallowedBar,15),new(ItemID.ChlorophyteBar,10)},
            2 => new[] {new MailGift(ItemID.HallowedBar,40),new(ItemID.ChlorophyteBar,30),new(ItemID.SpectreBar,25),new(ItemID.ShroomiteBar,25)},
            _ => new[] {new MailGift(ItemID.ChlorophyteBar,40),new(ItemID.LunarBar,30)}
        };
        var secondary = stage switch {
            0 => new[] {new MailGift(ItemID.Wood,200),new(ItemID.Silk,20),new(ItemID.StoneBlock,200)},
            1 => new[] {new MailGift(ItemID.SoulofLight,30),new(ItemID.SoulofNight,30),new(ItemID.SoulofFlight,15),new(ItemID.SoulofSight,10),new(ItemID.SoulofMight,10),new(ItemID.SoulofFright,10)},
            2 => new[] {new MailGift(ItemID.SoulofLight,50),new(ItemID.SoulofNight,50),new(ItemID.SoulofFlight,40),new(ItemID.SoulofSight,30),new(ItemID.SoulofMight,30),new(ItemID.SoulofFright,30),new(ItemID.BrokenHeroSword)},
            _ => new[] {new MailGift(ItemID.FragmentSolar,30),new(ItemID.FragmentStardust,30),new(ItemID.FragmentVortex,30),new(ItemID.FragmentNebula,30)}
        };
        int box = stage == 0 ? ModContent.ItemType<PreHardmodeMailBox>() : stage == 1 ? ModContent.ItemType<HardmodeMailBox>() : ModContent.ItemType<PlanteraMailBox>();
        var gifts = new List<MailGift> {new(Foods[Main.rand.Next(Foods.Length)],Main.rand.Next(3,8)),Pick(primary),Pick(secondary),new(box),new(ItemID.HerbBag)};
        if (stage == 3 || Main.rand.Next(10) == 0) gifts.Add(new(Special[Main.rand.Next(Special.Length)]));
        return gifts;
    }
}
