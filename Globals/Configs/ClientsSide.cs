using System.ComponentModel;
using Terraria.Localization;
using Terraria.ModLoader.Config;

namespace KivotosMod.Globals.Configs
{
    public enum SupernovaVoiceLanguage { None, Japanese, Chinese, Korean }

    public class KivotosClientConfig : ModConfig
    {
        public static KivotosClientConfig Instance;
        public override void OnLoaded()
        {
            Instance = this;
        }
        [Range(.3f, 20f)]
        [Increment(.01f)]
        [DefaultValue(1f)]
        public float StudentWeaponDamageMultiplier { get; set; } = 1f;

        [DefaultValue(SupernovaVoiceLanguage.Japanese)]
        public SupernovaVoiceLanguage SupernovaVoice { get; set; } = SupernovaVoiceLanguage.Japanese;

        public override ConfigScope Mode => ConfigScope.ClientSide;
        public override bool AcceptClientChanges(ModConfig pendingConfig, int whoAmI, ref NetworkText message) => false;

        [BackgroundColor(211, 211, 211, 192)]
        [DefaultValue(true)]
        public bool SpecialRarity { get; set; }

        [BackgroundColor(211, 211, 211, 192)]
        [Range(50, 30000)]
        [Increment(1)]
        [DefaultValue(10000)]
        public int MaxParticleCounts { get; set; }
        [BackgroundColor(211, 211, 211, 192)]
        [Range(0, 10f)]
        [DefaultValue(1f)]
        public float ScreenShakeStrength { get; set; }

        [BackgroundColor(211, 211, 211, 192)]
        [Range(0f, 1f)]
        [DefaultValue(1f)]
        public float ScreenDarkStrength { get; set; }
        [BackgroundColor(211, 211, 211, 192)]
        [Range(0f, 1f)]
        [DefaultValue(1f)]
        public float TextboxSize { get; set; }


    }
}
