using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Globalization;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using KivotosMod.Content.Tablet.Delivery;

namespace KivotosMod.Content.Tablet.Mail;

public sealed class TabletMailEntry
{
    public string Id = "";
    public int Template;
    public bool Read, Favorite, Claimed, Pending;
    public readonly List<MailGift> Gifts = new();
    public int OrderId = -1; // transient: rebuilt from Pending after loading
    public long ReceivedUtc;
}

public sealed class TabletMailPlayer : ModPlayer
{
    public readonly List<TabletMailEntry> Letters = new();
    public long PlayedTicks;
    public int LastDailyStamp;
    public bool Ready;
    public bool HasUnread => Letters.Any(e => !e.Read);
    public bool HasStory(int id) => Letters.Any(e => e.Id == "story:" + id);
    public bool StoryEligible => HasStory(0) && HasStory(1) && PlayedTicks > 86400;
    public IEnumerable<TabletMailEntry> Sorted => Letters.Select((entry,index)=>(entry,index))
        .OrderBy(pair=>pair.entry.Claimed).ThenByDescending(pair=>pair.entry.Favorite)
        .ThenByDescending(pair=>pair.index).Select(pair=>pair.entry);
    public override void Initialize() { Letters.Clear(); Ready = false; }
    public override void OnEnterWorld()
    {
        Ready = true;
        PlayedTicks = Math.Max(PlayedTicks, (long)((Main.ActivePlayerFileData?.GetPlayTime().TotalSeconds ?? 0) * 60));
        foreach (var entry in Letters) entry.OrderId = -1;
        ExpireDaily(DateTimeOffset.UtcNow);
        if (Main.netMode == NetmodeID.MultiplayerClient) SendSnapshot(0);
    }
    public override void PostUpdate() { if (Ready && PlayedTicks < long.MaxValue) PlayedTicks++; }
    public bool Receive(string id, int template, List<MailGift> gifts)
    {
        if (Letters.Any(e => e.Id == id)) return false;
        var entry = new TabletMailEntry { Id = id, Template = template, ReceivedUtc = DateTimeOffset.UtcNow.ToUnixTimeSeconds() };
        entry.Gifts.AddRange(gifts); Letters.Add(entry);
        SendSnapshot();
        return true;
    }
    public void Action(string id, byte action)
    {
        if (ExpireDaily(DateTimeOffset.UtcNow)) SendSnapshot();
        var entry = Letters.FirstOrDefault(e => e.Id == id);
        if (entry == null) return;
        if (action == 1) entry.Read = true;
        else if (action == 2) entry.Favorite = !entry.Favorite;
        else if (action == 3 && !entry.Claimed && !Player.dead && !Player.ghost)
        {
            entry.Read = entry.Claimed = entry.Pending = true;
            QueuePending(entry);
        }
        SendSnapshot();
    }
    public void QueuePending(TabletMailEntry entry)
    {
        if (!entry.Pending || entry.OrderId >= 0 || !Player.active || Player.dead) return;
        var contents = entry.Gifts.Select(g => new Item(g.Type,g.Stack)).ToList();
        entry.OrderId = TabletDeliveryOrderSystem.Enqueue(Player, contents, () =>
        {
            entry.Pending = false; entry.OrderId = -1;
            ExpireDaily(DateTimeOffset.UtcNow);
            SendSnapshot();
        });
    }
    internal bool ExpireDaily(DateTimeOffset now)
    {
        long cutoff = now.AddDays(-2).ToUnixTimeSeconds();
        return Letters.RemoveAll(entry => entry.Template is >=100 and <=102 && !entry.Pending &&
            DailyReceived(entry, now) <= cutoff) > 0;
    }
    private static long DailyReceived(TabletMailEntry entry, DateTimeOffset now)
    {
        if (entry.ReceivedUtc > 0) return entry.ReceivedUtc;
        // Existing daily IDs encode the start of their reward day (04:00 UTC+8).
        if (entry.Id.StartsWith("daily:", StringComparison.Ordinal) &&
            DateTime.TryParseExact(entry.Id.AsSpan(6), "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
            entry.ReceivedUtc = new DateTimeOffset(date.AddHours(4), TimeSpan.FromHours(8)).ToUnixTimeSeconds();
        else
            entry.ReceivedUtc = now.ToUnixTimeSeconds();
        return entry.ReceivedUtc;
    }
    public void ResetMailbox()
    {
        foreach (var entry in Letters) if (entry.OrderId >= 0) TabletDeliveryOrderSystem.CancelMailOrder(entry.OrderId, Player);
        Letters.Clear(); SendSnapshot();
    }
    public override void PlayerDisconnect()
    {
        foreach (var entry in Letters) if (entry.OrderId >= 0)
        { TabletDeliveryOrderSystem.CancelMailOrder(entry.OrderId, Player); entry.OrderId = -1; }
        Ready = false;
    }
    public override void SaveData(TagCompound tag)
    {
        tag["PlayedTicks"] = PlayedTicks; tag["LastDailyStamp"] = LastDailyStamp;
        tag["Letters"] = Letters.Select(e => new TagCompound {
            ["Id"]=e.Id,["Template"]=e.Template,["Read"]=e.Read,["Favorite"]=e.Favorite,["Claimed"]=e.Claimed,["Pending"]=e.Pending,["ReceivedUtc"]=e.ReceivedUtc,
            ["Gifts"]=e.Gifts.Select(g => ItemIO.Save(new Item(g.Type,g.Stack))).ToList()
        }).ToList();
    }
    public override void LoadData(TagCompound tag)
    {
        Letters.Clear(); PlayedTicks=Math.Max(0,tag.GetLong("PlayedTicks")); LastDailyStamp=tag.GetInt("LastDailyStamp");
        foreach (var data in tag.GetList<TagCompound>("Letters"))
        {
            var entry = new TabletMailEntry {Id=data.GetString("Id"),Template=data.GetInt("Template"),Read=data.GetBool("Read"),Favorite=data.GetBool("Favorite"),Claimed=data.GetBool("Claimed"),Pending=data.GetBool("Pending"),ReceivedUtc=data.GetLong("ReceivedUtc")};
            foreach(var gift in data.GetList<TagCompound>("Gifts")) { var item=ItemIO.Load(gift);if(!item.IsAir)entry.Gifts.Add(new(item.type,item.stack)); }
            if (!Letters.Any(e => e.Id == entry.Id)) Letters.Add(entry);
        }
    }
    internal void Write(BinaryWriter writer)
    {
        writer.Write(PlayedTicks);writer.Write(LastDailyStamp);writer.Write((ushort)Math.Min(4096,Letters.Count));
        foreach(var e in Letters.Take(4096))
        {
            writer.Write(e.Id);writer.Write(e.Template);writer.Write(e.Read);writer.Write(e.Favorite);writer.Write(e.Claimed);writer.Write(e.Pending);writer.Write(e.ReceivedUtc);
            writer.Write((byte)e.Gifts.Count);foreach(var gift in e.Gifts){writer.Write(gift.Type);writer.Write(gift.Stack);}
        }
    }
    internal void Read(BinaryReader reader)
    {
        PlayedTicks=Math.Max(0,reader.ReadInt64());LastDailyStamp=reader.ReadInt32();int count=reader.ReadUInt16();
        if(count>4096)throw new IOException("Mailbox too large");
        Letters.Clear();
        for(int i=0;i<count;i++)
        {
            var e=new TabletMailEntry {Id=reader.ReadString(),Template=reader.ReadInt32(),Read=reader.ReadBoolean(),Favorite=reader.ReadBoolean(),Claimed=reader.ReadBoolean(),Pending=reader.ReadBoolean(),ReceivedUtc=reader.ReadInt64()};
            int gifts=reader.ReadByte();if(gifts>32)throw new IOException("Too many mail gifts");
            for(int j=0;j<gifts;j++){int type=reader.ReadInt32(),stack=reader.ReadInt32();if(type>0&&type<ItemLoader.ItemCount&&stack>0&&stack<=9999)e.Gifts.Add(new(type,stack));}
            if((e.Template>=0&&e.Template<11||e.Template>=100&&e.Template<=102)&&!Letters.Any(x=>x.Id==e.Id)) Letters.Add(e);
        }
        Ready=true;
    }
    public void SendSnapshot(byte bootstrap = 1)
    {
        if(Main.netMode==NetmodeID.SinglePlayer)return;
        if(Main.netMode==NetmodeID.MultiplayerClient&&bootstrap!=0)return;
        var packet=global::KivotosMod.KivotosMod.Instance.GetPacket();packet.Write((byte)13);packet.Write(bootstrap);Write(packet);
        packet.Send(Main.netMode==NetmodeID.Server?Player.whoAmI:-1);
    }
    public static void Request(string id,byte action)
    {
        if(Main.netMode!=NetmodeID.MultiplayerClient){Main.LocalPlayer.GetModPlayer<TabletMailPlayer>().Action(id,action);return;}
        var packet=global::KivotosMod.KivotosMod.Instance.GetPacket();packet.Write((byte)14);packet.Write(action);packet.Write(id);packet.Send();
    }
    internal static void HandleSnapshot(BinaryReader reader,int sender)
    {
        byte bootstrap=reader.ReadByte();
        if(Main.netMode==NetmodeID.Server)
        {
            if(bootstrap!=0||sender<0||sender>=Main.maxPlayers)return;
            var state=Main.player[sender].GetModPlayer<TabletMailPlayer>();if(state.Ready)return;
            state.Read(reader);state.ExpireDaily(DateTimeOffset.UtcNow);state.SendSnapshot();
        }
        else if(Main.netMode==NetmodeID.MultiplayerClient&&bootstrap==1) Main.LocalPlayer.GetModPlayer<TabletMailPlayer>().Read(reader);
    }
    internal static void HandleAction(BinaryReader reader,int sender)
    {
        byte action=reader.ReadByte();string id=reader.ReadString();
        if(Main.netMode!=NetmodeID.Server||sender<0||sender>=Main.maxPlayers)return;
        var player=Main.player[sender];var state=player.GetModPlayer<TabletMailPlayer>();
        if(!player.active||!state.Ready)return;
        if(action==4){if(player.HasItem(ModContent.ItemType<MailResetter>()))state.ResetMailbox();return;}
        if(action>=1&&action<=3)state.Action(id,action);
    }
}
