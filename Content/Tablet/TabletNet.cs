using System.Collections.Generic;
using System.IO;
using System.Linq;
using Terraria.ModLoader.IO;
using Terraria;
using Terraria.ID;
using KivotosMod.Content.Skills.BlackHole;

namespace KivotosMod.Content.Tablet;

internal enum TabletNetMessage : byte
{
    CheckoutRequest = 1,
    CheckoutResult = 2,
    AppearanceSync = 3,
    UpgradeRequest = 4,
    UpgradeResult = 5,
    ResetWeaponRequest = 6,
    BoostWeaponRequest = 7,
    InventoryResult = 8,
    PiggyBankState = 9,
    HurtVoice = 10,
    ShieldRequest = 11,
    ShieldState = 12,
    MailSnapshot = 13,
    MailAction = 14,
    MikaExAction = MikaExPlayer.ActionMessage,
    MikaExState = MikaExPlayer.StateMessage,
}

public static class TabletNet
{
    internal static void SendAppearance(int playerIndex, bool hasSelection, TabletStudentId student,
        int toWho = -1, int fromWho = -1)
    {
        if (Main.netMode == NetmodeID.SinglePlayer)
            return;

        var packet = global::KivotosMod.KivotosMod.Instance.GetPacket();
        packet.Write((byte)TabletNetMessage.AppearanceSync);
        packet.Write((byte)playerIndex);
        packet.Write(hasSelection);
        packet.Write((byte)student);
        packet.Send(toWho, fromWho);
    }

    internal static void SendUpgradeRequest(int slot, int type, int level)
    {
        SendPiggyBank();
        var packet = global::KivotosMod.KivotosMod.Instance.GetPacket();
        packet.Write((byte)TabletNetMessage.UpgradeRequest);
        packet.Write((byte)slot);
        packet.Write(type);
        packet.Write((byte)level);
        packet.Send();
    }

    internal static void SendResetRequest(int slot, int type, int level)
    {
        var packet = global::KivotosMod.KivotosMod.Instance.GetPacket();
        packet.Write((byte)TabletNetMessage.ResetWeaponRequest);
        packet.Write((byte)slot);
        packet.Write(type);
        packet.Write((byte)level);
        packet.Send();
    }

    internal static void SendBoostRequest(int slot, int type, int level)
    {
        var packet = global::KivotosMod.KivotosMod.Instance.GetPacket();
        packet.Write((byte)TabletNetMessage.BoostWeaponRequest);
        packet.Write((byte)slot);
        packet.Write(type);
        packet.Write((byte)level);
        packet.Send();
    }

    public static void SendCheckoutRequest(IReadOnlyList<TabletCartLine> lines)
    {
        SendPiggyBank();
        if (lines == null)
            return;

        var packet = global::KivotosMod.KivotosMod.Instance.GetPacket();
        packet.Write((byte)TabletNetMessage.CheckoutRequest);
        packet.Write((byte)System.Math.Min(lines.Count, TabletCheckoutService.MaxLines));
        for (int i = 0; i < lines.Count && i < TabletCheckoutService.MaxLines; i++)
        {
            packet.Write(lines[i].ItemType);
            packet.Write(lines[i].Quantity);
        }
        packet.Send();
    }

    private static void SendCheckoutResult(int toClient, bool success, string message)
    {
        var packet = global::KivotosMod.KivotosMod.Instance.GetPacket();
        packet.Write((byte)TabletNetMessage.CheckoutResult);
        packet.Write(success);
        packet.Write(message ?? string.Empty);
        packet.Send(toClient);
    }

    private static void SendPiggyBank()
    {
        var packet = global::KivotosMod.KivotosMod.Instance.GetPacket();
        packet.Write((byte)TabletNetMessage.PiggyBankState);
        foreach (Item item in Main.LocalPlayer.bank.item) ItemIO.Send(item, packet, writeStack: true);
        packet.Send();
    }

    private static byte[][] InventorySnapshot(Player player)
    {
        return TabletWallet.Storages(player).SelectMany(storage => storage).Select(item =>
        {
            using var stream = new MemoryStream();
            using var writer = new BinaryWriter(stream);
            ItemIO.Send(item, writer, writeStack: true);
            return stream.ToArray();
        }).ToArray();
    }

    private static void SendInventoryResult(int client, byte[][] before)
    {
        byte[][] after = InventorySnapshot(Main.player[client]);
        var changed = Enumerable.Range(0, after.Length).Where(i => !before[i].SequenceEqual(after[i])).ToArray();
        var packet = global::KivotosMod.KivotosMod.Instance.GetPacket();
        packet.Write((byte)TabletNetMessage.InventoryResult);
        packet.Write((byte)changed.Length);
        foreach (int slot in changed)
        {
            packet.Write((byte)slot);
            packet.Write(after[slot]);
        }
        packet.Send(client);
    }

    internal static void SendHurtVoice(int player, TabletStudentId student, int variant)
    {
        var packet = global::KivotosMod.KivotosMod.Instance.GetPacket();
        packet.Write((byte)TabletNetMessage.HurtVoice);
        packet.Write((byte)player);
        packet.Write((byte)student);
        packet.Write((byte)variant);
        packet.Send();
    }

    public static void HandlePacket(BinaryReader reader, int whoAmI)
    {
        TabletNetMessage message = (TabletNetMessage)reader.ReadByte();
        switch (message)
        {
            case TabletNetMessage.MikaExAction:
                MikaExPlayer.ReceiveAction(reader, whoAmI);
                break;
            case TabletNetMessage.MikaExState:
                MikaExDomain.ReceiveState(reader);
                break;
            case TabletNetMessage.MailSnapshot:
                global::KivotosMod.Content.Tablet.Mail.TabletMailPlayer.HandleSnapshot(reader, whoAmI);
                break;
            case TabletNetMessage.MailAction:
                global::KivotosMod.Content.Tablet.Mail.TabletMailPlayer.HandleAction(reader, whoAmI);
                break;
            case TabletNetMessage.ShieldRequest:
                global::KivotosMod.Content.Skills.WeaponShieldPlayer.HandleRequest(reader, whoAmI);
                break;
            case TabletNetMessage.ShieldState:
                global::KivotosMod.Content.Skills.WeaponShieldPlayer.ReceiveState(reader);
                break;
            case TabletNetMessage.PiggyBankState:
                if (Main.netMode != NetmodeID.Server || whoAmI < 0 || whoAmI >= Main.maxPlayers) return;
                for (int i = 0; i < Main.player[whoAmI].bank.item.Length; i++)
                    Main.player[whoAmI].bank.item[i] = ItemIO.Receive(reader, readStack: true);
                break;
            case TabletNetMessage.InventoryResult:
                if (Main.netMode != NetmodeID.MultiplayerClient) return;
                int changedCount = reader.ReadByte();
                Item[][] storages = TabletWallet.Storages(Main.LocalPlayer);
                for (int i = 0; i < changedCount; i++)
                {
                    int slot = reader.ReadByte();
                    Item item = ItemIO.Receive(reader, readStack: true);
                    if (slot < storages[0].Length) storages[0][slot] = item;
                    else if (slot - storages[0].Length < storages[1].Length) storages[1][slot - storages[0].Length] = item;
                }
                break;
            case TabletNetMessage.HurtVoice:
            {
                int playerIndex = reader.ReadByte();
                TabletStudentId student = (TabletStudentId)reader.ReadByte();
                int variant = reader.ReadByte();
                if (Main.netMode == NetmodeID.Server) playerIndex = whoAmI;
                if (playerIndex < 0 || playerIndex >= Main.maxPlayers || !Main.player[playerIndex].active
                    || !global::KivotosMod.Content.Items.Vanity.StudentHurtVoicePlayer.ValidVoice(student, variant)) return;
                if (Main.netMode == NetmodeID.Server)
                {
                    var packet = global::KivotosMod.KivotosMod.Instance.GetPacket();
                    packet.Write((byte)TabletNetMessage.HurtVoice);
                    packet.Write((byte)playerIndex); packet.Write((byte)student); packet.Write((byte)variant);
                    packet.Send(-1, whoAmI);
                }
                else if (playerIndex != Main.myPlayer)
                    global::KivotosMod.Content.Items.Vanity.StudentHurtVoicePlayer.PlayRemote(Main.player[playerIndex], student, variant);
                break;
            }
            case TabletNetMessage.BoostWeaponRequest:
            {
                if (Main.netMode != NetmodeID.Server || whoAmI < 0 || whoAmI >= Main.maxPlayers) return;
                int slot = reader.ReadByte();
                int type = reader.ReadInt32();
                int level = reader.ReadByte();
                byte[][] before = InventorySnapshot(Main.player[whoAmI]);
                TabletWeaponBoost.TryBoost(Main.player[whoAmI], slot, type, level, out string result);
                SendInventoryResult(whoAmI, before);
                var response = global::KivotosMod.KivotosMod.Instance.GetPacket();
                response.Write((byte)TabletNetMessage.UpgradeResult);
                response.Write(result);
                response.Send(whoAmI);
                break;
            }
            case TabletNetMessage.ResetWeaponRequest:
            {
                if (Main.netMode != NetmodeID.Server || whoAmI < 0 || whoAmI >= Main.maxPlayers) return;
                int slot = reader.ReadByte();
                int type = reader.ReadInt32();
                int level = reader.ReadByte();
                byte[][] before = InventorySnapshot(Main.player[whoAmI]);
                TabletWeaponReset.TryReset(Main.player[whoAmI], slot, type, level, out string result);
                SendInventoryResult(whoAmI, before);
                var response = global::KivotosMod.KivotosMod.Instance.GetPacket();
                response.Write((byte)TabletNetMessage.UpgradeResult);
                response.Write(result);
                response.Send(whoAmI);
                break;
            }
            case TabletNetMessage.UpgradeRequest:
            {
                if (Main.netMode != NetmodeID.Server || whoAmI < 0 || whoAmI >= Main.maxPlayers) return;
                int slot = reader.ReadByte();
                int type = reader.ReadInt32();
                int level = reader.ReadByte();
                byte[][] before = InventorySnapshot(Main.player[whoAmI]);
                TabletWeaponUpgrade.TryUpgrade(Main.player[whoAmI], slot, type, level, out string result);
                SendInventoryResult(whoAmI, before);
                var response = global::KivotosMod.KivotosMod.Instance.GetPacket();
                response.Write((byte)TabletNetMessage.UpgradeResult);
                response.Write(result);
                response.Send(whoAmI);
                break;
            }
            case TabletNetMessage.UpgradeResult:
                if (Main.netMode == NetmodeID.MultiplayerClient) Main.NewText(reader.ReadString());
                break;
            case TabletNetMessage.AppearanceSync:
            {
                int playerIndex = reader.ReadByte();
                bool hasSelection = reader.ReadBoolean();
                TabletStudentId student = (TabletStudentId)reader.ReadByte();
                // A client can change only its own appearance. Relay the server's accepted state.
                if (Main.netMode == NetmodeID.Server)
                    playerIndex = whoAmI;
                if (playerIndex < 0 || playerIndex >= Main.maxPlayers
                    || !TabletStudentAppearancePlayer.IsFormationStudent(student))
                    return;

                Main.player[playerIndex].GetModPlayer<TabletStudentAppearancePlayer>()
                    .ReceiveSelection(hasSelection, student);
                if (Main.netMode == NetmodeID.Server)
                    SendAppearance(playerIndex, hasSelection, student, -1, whoAmI);
                break;
            }

            case TabletNetMessage.CheckoutRequest:
            {
                if (Main.netMode != NetmodeID.Server)
                    return;
                if (whoAmI < 0 || whoAmI >= Main.maxPlayers)
                    return;

                int count = reader.ReadByte();
                if (count <= 0 || count > TabletCheckoutService.MaxLines)
                {
                    SendCheckoutResult(whoAmI, false, "购物车数据无效。");
                    return;
                }

                List<TabletCartLine> lines = new(count);
                for (int i = 0; i < count; i++)
                    lines.Add(new TabletCartLine(reader.ReadInt32(), reader.ReadInt32()));

                Player player = Main.player[whoAmI];
                byte[][] before = InventorySnapshot(player);
                bool success = TabletCheckoutService.TryCheckoutAndQueue(player, lines, out string resultMessage);
                SendInventoryResult(whoAmI, before);
                SendCheckoutResult(whoAmI, success, resultMessage);
                break;
            }

            case TabletNetMessage.CheckoutResult:
            {
                if (Main.netMode == NetmodeID.Server)
                    return;
                bool success = reader.ReadBoolean();
                string resultMessage = reader.ReadString();
                TabletUiSystem.HandleCheckoutResult(success, resultMessage);
                break;
            }
        }
    }
}
