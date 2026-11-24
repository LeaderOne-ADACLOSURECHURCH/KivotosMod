using System;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace KivotosMod.Content.Tablet.Mail;

public sealed class TabletMailWorld : ModSystem
{
    public int Day;
    private bool _previousDaytime, _rolled, _sentToday, _forceNext;
    private int _planned = -1, _clock;
    private readonly List<int> _dispatches = new();
    public override void OnWorldLoad() { Day=0;_previousDaytime=Main.dayTime;_rolled=_sentToday=_forceNext=false;_planned=-1;_clock=0;_dispatches.Clear(); }
    public override void SaveWorldData(TagCompound tag)
    {
        tag["MailDay"]=Day;tag["MailRolled"]=_rolled;tag["MailSent"]=_sentToday;tag["MailForce"]=_forceNext;tag["MailPlan"]=_planned;
        tag["MailPreviousDaytime"]=_previousDaytime;tag["MailDispatches"]=_dispatches.ToList();
    }
    public override void LoadWorldData(TagCompound tag)
    {
        Day=tag.GetInt("MailDay");_rolled=tag.GetBool("MailRolled");_sentToday=tag.GetBool("MailSent");_forceNext=tag.GetBool("MailForce");
        _planned=tag.ContainsKey("MailPlan")?tag.GetInt("MailPlan"):-1;
        _previousDaytime=tag.ContainsKey("MailPreviousDaytime")?tag.GetBool("MailPreviousDaytime"):Main.dayTime;
        _dispatches.Clear();_dispatches.AddRange(tag.GetList<int>("MailDispatches"));
    }
    public static bool SpecialEvent => Main.invasionType != 0 || Main.slimeRain || Main.bloodMoon || Main.eclipse
        || Main.pumpkinMoon || Main.snowMoon || NPC.LunarApocalypseIsUp || Terraria.GameContent.Events.DD2Event.Ongoing;
    public static int RewardStamp(DateTimeOffset now)
    {
        var date=now.ToOffset(TimeSpan.FromHours(8)).AddHours(-4);
        return date.Year*10000+date.Month*100+date.Day;
    }
    public override void PostUpdateWorld()
    {
        if(Main.netMode==NetmodeID.MultiplayerClient)return;
        if(Main.dayTime&&!_previousDaytime)
        { Day++;_forceNext=!_sentToday;_sentToday=false;_rolled=false;_planned=-1; }
        _previousDaytime=Main.dayTime;
        if(++_clock%60!=0)return;
        var players=Main.player.Where(p=>p != null && p.active && p.GetModPlayer<TabletMailPlayer>().Ready).ToArray();
        int time=(int)Main.time+(Main.dayTime?0:54000);
        var now=DateTimeOffset.UtcNow;
        int stamp=RewardStamp(now);
        foreach(var player in players)
        {
            var state=player.GetModPlayer<TabletMailPlayer>();
            if(state.ExpireDaily(now))state.SendSnapshot();
            foreach(var entry in state.Letters) if(entry.Pending)state.QueuePending(entry);
            if(!SpecialEvent)
            {
                if(!state.HasStory(0))state.Receive("story:0",0,TabletMailCatalog.StoryGifts(0));
                else if(!state.HasStory(1)&&Main.dayTime&&time>=27000)
                    state.Receive("story:1",1,TabletMailCatalog.StoryGifts(1));
            }
            if(state.HasStory(1)&&state.LastDailyStamp<stamp)
            {
                state.LastDailyStamp=stamp;
                state.Receive("daily:"+stamp,100+Main.rand.Next(3),TabletMailCatalog.DailyGifts(TabletMailCatalog.Stage));
            }
        }
        if(SpecialEvent||_sentToday)return;
        var eligible=players.Select(p=>p.GetModPlayer<TabletMailPlayer>()).Where(p=>p.StoryEligible).ToArray();
        var unseen=Enumerable.Range(2,9).Where(id=>eligible.Any(p=>!p.HasStory(id))).ToArray();
        if(!_rolled&&unseen.Length>0)
        {
            _rolled=true;
            if(_forceNext||Main.rand.Next(2)==0)_planned=unseen[Main.rand.Next(unseen.Length)];
        }
        if(_planned<2||_planned>=TabletMailCatalog.Stories.Length||!eligible.Any(p=>!p.HasStory(_planned)))return;
        var mail=TabletMailCatalog.Stories[_planned];
        if(time<mail.Time||(mail.Windy&&(Main.raining||!Main.dayTime||Math.Abs(Main.windSpeedCurrent)<.4f)))return;
        bool sent=false;
        foreach(var state in eligible)sent|=state.Receive("story:"+_planned,_planned,TabletMailCatalog.StoryGifts(_planned));
        if(sent){_sentToday=true;_dispatches.Add(_planned);}
    }
}
