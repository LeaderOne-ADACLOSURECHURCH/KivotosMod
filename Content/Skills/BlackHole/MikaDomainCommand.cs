using KivotosMod.Content.Items.Tools;
using Terraria;
using Terraria.ModLoader;
namespace KivotosMod.Content.Skills.BlackHole;
public sealed class MikaDomainCommand : ModCommand
{
    public override string Command=>"mikadomain";
    public override CommandType Type=>CommandType.Chat;
    public override string Usage=>"/mikadomain [on|off|tools|tune|apple|peach|paint|stats|resonance] 或 /mikadomain demo [polar|remap|scope|open]";
    public override string Description=>"未花黑洞领域测试";
    public override void Action(CommandCaller caller,string input,string[] args)
    {
        string action=args.Length==0?"tools":args[0];
        if(action=="on"){if(!BlackHolePreviewSystem.Enabled || BlackHolePreviewSystem.PaintDemo || BlackHolePreviewSystem.CompositionDemo!=null)BlackHolePreviewSystem.Restart(false);return;}
        if(action=="paint"){BlackHolePreviewSystem.Restart(true);return;}
        if(action=="stats"){Main.NewText(MikaDomainStars.Status);return;}
        if(action=="tune"){MikaDomainTuning.Toggle();return;}
        if(action=="apple"){MikaDomainTuning.Toggle(apple:true);return;}
        if(action=="peach"){MikaShatterTuning.Toggle();return;}
        if(action=="resonance"){BlackHolePreviewSystem.PreviewResonance();return;}
        if(action=="demo" && args.Length==2)
        {
            string name=args[1] switch
            {
                "polar"=>"实现极坐标","remap"=>"时间重映射","scope" or "open"=>"预合成控制功能",_=>null
            };
            if(name!=null){BlackHolePreviewSystem.StartDemo(name,args[1]=="open");return;}
        }
        if(action=="off"){if(BlackHolePreviewSystem.Enabled)BlackHolePreviewSystem.Toggle();return;}
        if(action=="tools"){
            caller.Player.QuickSpawnItem(caller.Player.GetSource_GiftOrReward(),ModContent.ItemType<BlackHolePreviewItem>());
            caller.Player.QuickSpawnItem(caller.Player.GetSource_GiftOrReward(),ModContent.ItemType<MikaDomainScatterSword>());
            caller.Player.QuickSpawnItem(caller.Player.GetSource_GiftOrReward(),ModContent.ItemType<MikaDomainTuningFlower>());
            caller.Player.QuickSpawnItem(caller.Player.GetSource_GiftOrReward(),ModContent.ItemType<MikaDomainTuningApple>());
            caller.Player.QuickSpawnItem(caller.Player.GetSource_GiftOrReward(),ModContent.ItemType<MikaDomainCrystal>());
            caller.Player.QuickSpawnItem(caller.Player.GetSource_GiftOrReward(),ModContent.ItemType<MikaShatterPeach>());
            caller.Player.QuickSpawnItem(caller.Player.GetSource_GiftOrReward(),ModContent.ItemType<MikaOpeningGrenade>());return;
        }
        Main.NewText(Usage);
    }
}
