using System.Collections.Generic;

namespace KivotosMod.Content.Tablet;

/// <summary>
/// Clean-room, UI-only student metadata used by the tablet Formation page.
/// It deliberately contains no NPC type, AI, spawning, combat controller, or team deployment.
/// Portrait/thumbnail art is loaded as UI content only.
/// </summary>
public enum TabletStudentId : byte
{
    // These values are persisted in existing character data. Do not renumber.
    Yuuka = 0,
    Hoshino = 1,
    Shiroko = 2,
    Arona = 3,
    Sora = 4,
    Plana = 5,
    Hina = 6,
    Noa = 7,
    Ibuki = 8,
    Iroha = 9,
    Hikari = 10,
    Mika = 11,
    Haruka = 12,
    Kayoko = 13,
    Mutsuki = 14,
    Nozomi = 15,
    Nagisa = 16,
    Seia = 17,
    Mika2x2 = 18,
    Yuuka2x2 = 19,
    Ibuki2x2 = 20,
    Iroha2x2 = 21,
    Hikari2x2 = 22,
    Himari2x2 = 23,
    Ako2x2 = 24,
    Mutsuki2x2 = 25,
    GehennaStudent2x2 = 26,
    Haruka2x2 = 27,
    Nonomi2x2 = 28,
    Serika2x2 = 29,
    Koyuki2x2 = 30,
    Kayoko2x2 = 31,
    Aru2x2 = 32,
    Megu2x2 = 33,
    Hina2x2 = 34,
    Shiroko2x2 = 35,
    Nozomi2x2 = 36,
    Hoshino2x2 = 37,
    Noa2x2 = 38,
    Akari2x2 = 39,
    MeguSauna2x2 = 40,
    MeguSaunaSmile2x2 = 41,


}

public sealed class TabletStudentInfo
{
    public TabletStudentId Id { get; }
    public string DisplayName { get; }
    public string GivenName { get; }
    public string FactionName { get; }
    public int LegacyFactionFrame { get; }
    public int AttackTypeFrame { get; }
    public int RoleFrame { get; }
    public string ThumbnailPath { get; }
    public bool HasSimpleLayeredPortrait { get; }
    public string PortraitBasePath { get; }
    public string PortraitFacePath { get; }
    public string PortraitHaloPath { get; }
    public int BaseFrameCount { get; }
    public int FaceFrameCount { get; }
    public Microsoft.Xna.Framework.Vector2 BaseOffset { get; }
    public Microsoft.Xna.Framework.Vector2 BaseOrigin { get; }
    public Microsoft.Xna.Framework.Vector2 HeadOffset { get; }
    public Microsoft.Xna.Framework.Vector2 FaceOrigin { get; }
    public Microsoft.Xna.Framework.Vector2 HaloOffset { get; }

    public TabletStudentInfo(
        TabletStudentId id,
        string displayName,
        string givenName,
        string factionName,
        int legacyFactionFrame,
        int attackTypeFrame,
        int roleFrame,
        string thumbnailPath,
        bool hasSimpleLayeredPortrait = false,
        string portraitBasePath = "",
        string portraitFacePath = "",
        string portraitHaloPath = "",
        int baseFrameCount = 1,
        int faceFrameCount = 1,
        Microsoft.Xna.Framework.Vector2? baseOffset = null,
        Microsoft.Xna.Framework.Vector2? baseOrigin = null,
        Microsoft.Xna.Framework.Vector2? headOffset = null,
        Microsoft.Xna.Framework.Vector2? faceOrigin = null,
        Microsoft.Xna.Framework.Vector2? haloOffset = null)
    {
        Id = id;
        DisplayName = displayName;
        GivenName = givenName;
        FactionName = factionName;
        LegacyFactionFrame = legacyFactionFrame;
        AttackTypeFrame = attackTypeFrame;
        RoleFrame = roleFrame;
        ThumbnailPath = thumbnailPath;
        HasSimpleLayeredPortrait = hasSimpleLayeredPortrait;
        PortraitBasePath = portraitBasePath;
        PortraitFacePath = portraitFacePath;
        PortraitHaloPath = portraitHaloPath;
        BaseFrameCount = baseFrameCount;
        FaceFrameCount = faceFrameCount;
        BaseOffset = baseOffset ?? Microsoft.Xna.Framework.Vector2.Zero;
        BaseOrigin = baseOrigin ?? Microsoft.Xna.Framework.Vector2.Zero;
        HeadOffset = headOffset ?? Microsoft.Xna.Framework.Vector2.Zero;
        FaceOrigin = faceOrigin ?? Microsoft.Xna.Framework.Vector2.Zero;
        HaloOffset = haloOffset ?? Microsoft.Xna.Framework.Vector2.Zero;
    }
}

public static class TabletStudentCatalog
{
    public static readonly IReadOnlyList<TabletStudentInfo> Students = new TabletStudentInfo[]
    {
        // Attack frames: 0 Generic, 1 Explosive, 2 Piercing, 3 Mystic, 4 Sonic, 5 Siege.
        // Role frames:   0 None, 1 Tank, 2 Dealer, 3 Healer, 4 Support, 5 Tactical.
        // Legacy faction frame numbers are used only to select the supplied tile-background art.
        new(
            TabletStudentId.Yuuka, "Yuuka Hayase", "Yuuka", "Millennium", 7, 1, 1,
            "KivotosMod/Assets/Tablet/Students/Yuuka/Thumbnail",
            true,
            "KivotosMod/Assets/Tablet/Students/Yuuka/Portrait/Base",
            "KivotosMod/Assets/Tablet/Students/Yuuka/Portrait/Face",
            "KivotosMod/Assets/Tablet/Students/Yuuka/Portrait/Halo",
            2, 9,
            new Microsoft.Xna.Framework.Vector2(0f, -263f),
            new Microsoft.Xna.Framework.Vector2(122f, 203f),
            new Microsoft.Xna.Framework.Vector2(0f, -99f),
            new Microsoft.Xna.Framework.Vector2(34f, 54f),
            new Microsoft.Xna.Framework.Vector2(-16f, -99f)),

        new(
            TabletStudentId.Hoshino, "Hoshino Takanashi", "Hoshino", "Abydos", 5, 1, 1,
            "KivotosMod/Assets/Tablet/Students/Hoshino/Thumbnail",
            true,
            "KivotosMod/Assets/Tablet/Students/Hoshino/Portrait/Base",
            "KivotosMod/Assets/Tablet/Students/Hoshino/Portrait/Face",
            "KivotosMod/Assets/Tablet/Students/Hoshino/Portrait/Halo",
            2, 9,
            new Microsoft.Xna.Framework.Vector2(0f, -274f),
            new Microsoft.Xna.Framework.Vector2(128f, 178f),
            new Microsoft.Xna.Framework.Vector2(0f, -64f),
            new Microsoft.Xna.Framework.Vector2(38f, 50f),
            new Microsoft.Xna.Framework.Vector2(-5f, -103f)),

        new(
            TabletStudentId.Shiroko, "Shiroko Sunaookami", "Shiroko", "Abydos", 5, 1, 2,
            "KivotosMod/Assets/Tablet/Students/Shiroko/Thumbnail",
            true,
            "KivotosMod/Assets/Tablet/Students/Shiroko/Portrait/Base",
            "KivotosMod/Assets/Tablet/Students/Shiroko/Portrait/Face",
            "KivotosMod/Assets/Tablet/Students/Shiroko/Portrait/Halo",
            2, 9,
            new Microsoft.Xna.Framework.Vector2(0f, -274f),
            new Microsoft.Xna.Framework.Vector2(102f, 204f),
            new Microsoft.Xna.Framework.Vector2(0f, -86f),
            new Microsoft.Xna.Framework.Vector2(34f, 52f),
            new Microsoft.Xna.Framework.Vector2(0f, -97f)),

        new(
            TabletStudentId.Hina, "Hina Sorasaki", "Hina", "Gehenna", 7, 1, 2,
            "KivotosMod/Assets/Tablet/Students/Yuuka/Thumbnail",
            true,
            "KivotosMod/Assets/Tablet/Students/Yuuka/Portrait/Base",
            "KivotosMod/Assets/Tablet/Students/Yuuka/Portrait/Face",
            "KivotosMod/Assets/Tablet/Students/Yuuka/Portrait/Halo",
            2, 9,
            new Microsoft.Xna.Framework.Vector2(0f, -263f),
            new Microsoft.Xna.Framework.Vector2(122f, 203f),
            new Microsoft.Xna.Framework.Vector2(0f, -99f),
            new Microsoft.Xna.Framework.Vector2(34f, 54f),
            new Microsoft.Xna.Framework.Vector2(-16f, -99f)),

        // Only player-format artwork was supplied for Noa. The Formation card
        // displays a sprite-derived thumbnail until full portrait assets arrive.
        new(TabletStudentId.Noa, "诺亚", "诺亚", "Millennium", 7, 3, 4,
            "KivotosMod/Assets/Tablet/Students/Noa/Thumbnail"),

        new(TabletStudentId.Ibuki, "伊吹", "伊吹", "Gehenna", 7, 1, 4,
            "KivotosMod/Assets/Tablet/Students/Ibuki/Thumbnail"),
        new(TabletStudentId.Iroha, "伊吕波", "伊吕波", "Gehenna", 7, 1, 4,
            "KivotosMod/Assets/Tablet/Students/Iroha/Thumbnail"),
        new(TabletStudentId.Hikari, "小光", "小光", "Highlander", 4, 1, 2,
            "KivotosMod/Assets/Tablet/Students/Hikari/Thumbnail"),
        new(TabletStudentId.Mika, "未花", "未花", "Trinity", 8, 2, 2,
            "KivotosMod/Assets/Tablet/Students/Mika/Thumbnail"),

        new(TabletStudentId.Haruka, "春香", "春香", "Gehenna", 7, 1, 1,
            "KivotosMod/Assets/Tablet/Students/Haruka/Thumbnail"),
        new(TabletStudentId.Kayoko, "佳代子", "佳代子", "Gehenna", 7, 1, 4,
            "KivotosMod/Assets/Tablet/Students/Kayoko/Thumbnail"),
        new(TabletStudentId.Mutsuki, "睦月", "睦月", "Gehenna", 7, 1, 2,
            "KivotosMod/Assets/Tablet/Students/Mutsuki/Thumbnail"),
        new(TabletStudentId.Nozomi, "小望", "小望", "Highlander", 4, 1, 2,
            "KivotosMod/Assets/Tablet/Students/Nozomi/Thumbnail"),
        new(TabletStudentId.Nagisa, "渚", "渚", "Trinity", 8, 0, 0,
            "KivotosMod/Assets/Tablet/Students/Nagisa/Thumbnail"),
        new(TabletStudentId.Seia, "圣娅", "圣娅", "Trinity", 8, 0, 0,
            "KivotosMod/Assets/Tablet/Students/Seia/Thumbnail"),

        new(TabletStudentId.Mika2x2, "未花 · 2×2", "未花 2×2", "Trinity", 7, 0, 0,
            "KivotosMod/Assets/Tablet/Students/Mika/Thumbnail2x2"),
        new(TabletStudentId.Yuuka2x2, "优香 · 2×2", "优香 2×2", "Millennium", 7, 0, 0,
            "KivotosMod/Assets/Tablet/Students/Yuuka/Thumbnail2x2"),
        new(TabletStudentId.Ibuki2x2, "伊吹 · 2×2", "伊吹 2×2", "Gehenna", 7, 0, 0,
            "KivotosMod/Assets/Tablet/Students/Ibuki/Thumbnail2x2"),
        new(TabletStudentId.Iroha2x2, "伊吕波 · 2×2", "伊吕波 2×2", "Gehenna", 7, 0, 0,
            "KivotosMod/Assets/Tablet/Students/Iroha/Thumbnail2x2"),
        new(TabletStudentId.Hikari2x2, "小光 · 2×2", "小光 2×2", "Highlander", 7, 0, 0,
            "KivotosMod/Assets/Tablet/Students/Hikari/Thumbnail2x2"),

        new(TabletStudentId.Himari2x2, "日鞠 · 2×2", "日鞠 2×2", "Millennium", 7, 0, 0, "KivotosMod/Content/Items/Vanity/NpcSpritePack/PackHimari2x2Thumbnail"),
        new(TabletStudentId.Ako2x2, "亚子 · 2×2", "亚子 2×2", "Gehenna", 7, 0, 0, "KivotosMod/Content/Items/Vanity/NpcSpritePack/PackAko2x2Thumbnail"),
        new(TabletStudentId.Mutsuki2x2, "睦月 · 2×2", "睦月 2×2", "Gehenna", 7, 0, 0, "KivotosMod/Content/Items/Vanity/NpcSpritePack/PackMutsuki2x2Thumbnail"),
        new(TabletStudentId.GehennaStudent2x2, "格黑娜学生 · 2×2", "格黑娜学生 2×2", "Gehenna", 7, 0, 0, "KivotosMod/Content/Items/Vanity/NpcSpritePack/PackGehennaStudent2x2Thumbnail"),
        new(TabletStudentId.Haruka2x2, "春香 · 2×2", "春香 2×2", "Gehenna", 7, 0, 0, "KivotosMod/Content/Items/Vanity/NpcSpritePack/PackHaruka2x2Thumbnail"),
        new(TabletStudentId.Nonomi2x2, "野乃美 · 2×2", "野乃美 2×2", "Abydos", 7, 0, 0, "KivotosMod/Content/Items/Vanity/NpcSpritePack/PackNonomi2x2Thumbnail"),
        new(TabletStudentId.Serika2x2, "芹香 · 2×2", "芹香 2×2", "Abydos", 7, 0, 0, "KivotosMod/Content/Items/Vanity/NpcSpritePack/PackSerika2x2Thumbnail"),
        new(TabletStudentId.Koyuki2x2, "小雪 · 2×2", "小雪 2×2", "Millennium", 7, 0, 0, "KivotosMod/Content/Items/Vanity/NpcSpritePack/PackKoyuki2x2Thumbnail"),
        new(TabletStudentId.Kayoko2x2, "佳代子 · 2×2", "佳代子 2×2", "Gehenna", 7, 0, 0, "KivotosMod/Content/Items/Vanity/NpcSpritePack/PackKayoko2x2Thumbnail"),
        new(TabletStudentId.Aru2x2, "阿露 · 2×2", "阿露 2×2", "Gehenna", 7, 0, 0, "KivotosMod/Content/Items/Vanity/NpcSpritePack/PackAru2x2Thumbnail"),
        new(TabletStudentId.Megu2x2, "惠 · 2×2", "惠 2×2", "Gehenna", 7, 0, 0, "KivotosMod/Content/Items/Vanity/NpcSpritePack/PackMegu2x2Thumbnail"),
        new(TabletStudentId.Hina2x2, "日奈 · 2×2", "日奈 2×2", "Gehenna", 7, 0, 0, "KivotosMod/Content/Items/Vanity/NpcSpritePack/PackHina2x2Thumbnail"),
        new(TabletStudentId.Shiroko2x2, "白子 · 2×2", "白子 2×2", "Abydos", 7, 0, 0, "KivotosMod/Content/Items/Vanity/NpcSpritePack/PackShiroko2x2Thumbnail"),
        new(TabletStudentId.Nozomi2x2, "小望 · 2×2", "小望 2×2", "Highlander", 7, 0, 0, "KivotosMod/Content/Items/Vanity/NpcSpritePack/PackNozomi2x2Thumbnail"),
        new(TabletStudentId.Hoshino2x2, "星野 · 2×2", "星野 2×2", "Abydos", 7, 0, 0, "KivotosMod/Content/Items/Vanity/NpcSpritePack/PackHoshino2x2Thumbnail"),
        new(TabletStudentId.Noa2x2, "诺亚 · 2×2", "诺亚 2×2", "Millennium", 7, 0, 0, "KivotosMod/Content/Items/Vanity/NpcSpritePack/PackNoa2x2Thumbnail"),
        new(TabletStudentId.Akari2x2, "明里 · 2×2", "明里 2×2", "Gehenna", 7, 0, 0, "KivotosMod/Content/Items/Vanity/NpcSpritePack/PackAkari2x2Thumbnail"),
        new(TabletStudentId.MeguSauna2x2, "惠·桑拿 · 2×2", "惠·桑拿 2×2", "Gehenna", 7, 0, 0, "KivotosMod/Content/Items/Vanity/NpcSpritePack/PackMeguSauna2x2Thumbnail"),
        new(TabletStudentId.Arona, "Arona", "Arona", "Schale", 3, 0, 0,
            "KivotosMod/Assets/Tablet/Students/Arona/Thumbnail"),
        new(TabletStudentId.Sora, "Sora", "Sora", "Angel 24", 4, 0, 0,
            "KivotosMod/Assets/Tablet/Students/Sora/Thumbnail"),
        new(TabletStudentId.Plana, "Plana", "Plana", "Schale", 3, 0, 0,
            "KivotosMod/Assets/Tablet/Students/Plana/Thumbnail"),
    };

    // Unit Formation is a player-appearance picker, not the HOME companion roster.
    // Entries with playable player-format sprites appear here; companion portraits
    // are a separate concern and do not imply a playable outfit.
    public static readonly IReadOnlyList<TabletStudentInfo> FormationStudents = new TabletStudentInfo[]
    {
        Get(TabletStudentId.Yuuka),
        Get(TabletStudentId.Noa),
        Get(TabletStudentId.Hina),
        Get(TabletStudentId.Shiroko),
        Get(TabletStudentId.Hoshino),
        Get(TabletStudentId.Ibuki),
        Get(TabletStudentId.Iroha),
        Get(TabletStudentId.Hikari),
        Get(TabletStudentId.Mika),
        Get(TabletStudentId.Haruka),
        Get(TabletStudentId.Kayoko),
        Get(TabletStudentId.Mutsuki),
        Get(TabletStudentId.Nozomi),
        Get(TabletStudentId.Nagisa),
        Get(TabletStudentId.Seia),
        Get(TabletStudentId.Mika2x2),
        Get(TabletStudentId.Yuuka2x2),
        Get(TabletStudentId.Ibuki2x2),
        Get(TabletStudentId.Iroha2x2),
        Get(TabletStudentId.Hikari2x2),
        Get(TabletStudentId.Himari2x2),
        Get(TabletStudentId.Ako2x2),
        Get(TabletStudentId.Mutsuki2x2),
        Get(TabletStudentId.GehennaStudent2x2),
        Get(TabletStudentId.Haruka2x2),
        Get(TabletStudentId.Nonomi2x2),
        Get(TabletStudentId.Serika2x2),
        Get(TabletStudentId.Koyuki2x2),
        Get(TabletStudentId.Kayoko2x2),
        Get(TabletStudentId.Aru2x2),
        Get(TabletStudentId.Megu2x2),
        Get(TabletStudentId.Hina2x2),
        Get(TabletStudentId.Shiroko2x2),
        Get(TabletStudentId.Nozomi2x2),
        Get(TabletStudentId.Hoshino2x2),
        Get(TabletStudentId.Noa2x2),
        Get(TabletStudentId.Akari2x2),
        Get(TabletStudentId.MeguSauna2x2),


    };

    // HOME only cycles entries that currently have a proper full portrait renderer.
    public static readonly IReadOnlyList<TabletStudentInfo> CompanionStudents = new TabletStudentInfo[]
    {
        Get(TabletStudentId.Arona),
        Get(TabletStudentId.Plana),
        Get(TabletStudentId.Sora),
        Get(TabletStudentId.Yuuka),
        Get(TabletStudentId.Shiroko),
        Get(TabletStudentId.Hoshino),
    };

    public static int CompanionIndexOf(TabletStudentId id)
    {
        for (int i = 0; i < CompanionStudents.Count; i++)
            if (CompanionStudents[i].Id == id)
                return i;
        return 0;
    }

    public static int FormationIndexOf(TabletStudentId id)
    {
        for (int i = 0; i < FormationStudents.Count; i++)
            if (FormationStudents[i].Id == id)
                return i;
        return 0;
    }

    public static TabletStudentInfo Get(TabletStudentId id)
    {
        foreach (TabletStudentInfo student in Students)
            if (student.Id == id)
                return student;

        return Students[0];
    }

    public static int IndexOf(TabletStudentId id)
    {
        for (int i = 0; i < Students.Count; i++)
            if (Students[i].Id == id)
                return i;
        return 0;
    }
}
