using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace KivotosMod.Content.Tablet.Delivery;

public sealed class TabletDeliveryOrderSystem : ModSystem
{
    private sealed class DeliveryOrder
    {
        public int Owner;
        public Item Package;
        public int Cooldown;
        public bool InFlight;
        public Player Recipient;
        public Action OnDelivered;
    }

    private static readonly Dictionary<int, DeliveryOrder> Orders = new();
    private static int _nextOrderId = 1;

    public static int Enqueue(Player player, List<Item> contents, Action onDelivered = null)
    {
        if (Main.netMode == NetmodeID.MultiplayerClient)
            return -1;

        Item package = new();
        package.SetDefaults(ModContent.ItemType<DeliveryPackage>());
        package.stack = 1;
        if (package.ModItem is DeliveryPackage deliveryPackage)
            deliveryPackage.SetContents(contents);

        int id = _nextOrderId++;
        if (_nextOrderId <= 0)
            _nextOrderId = 1;

        global::KivotosMod.KivotosMod.Instance.Logger.Info($"[TabletDelivery] queued order={id} buyer={player.whoAmI} stacks={ContentCount(package)} mode={Main.netMode}");
        Orders[id] = new DeliveryOrder
        {
            Owner = player.whoAmI,
            Package = package,
            Cooldown = 30,
            InFlight = false,
            Recipient = player,
            OnDelivered = onDelivered,
        };
        return id;
    }

    public override void PostUpdatePlayers()
    {
        if (Main.netMode == NetmodeID.MultiplayerClient || Orders.Count == 0)
            return;

        List<int> ids = new(Orders.Keys);
        foreach (int id in ids)
        {
            if (!Orders.TryGetValue(id, out DeliveryOrder order))
                continue;
            if (order.InFlight && !HasLiveDrone(id, order.Owner))
            {
                // Some engine/mod removals deactivate projectiles without OnKill.
                // A paid order must not remain marked in-flight forever.
                order.InFlight = false;
                order.Cooldown = 90;
            }
            if (order.Cooldown > 0)
                order.Cooldown--;
        }

        for (int playerIndex = 0; playerIndex < Main.maxPlayers; playerIndex++)
        {
            Player player = Main.player[playerIndex];
            if (player == null || !player.active || player.dead || player.ghost)
                continue;

            int droneType = ModContent.ProjectileType<DeliveryDroneProjectile>();
            // Multiplayer server-owned drone projectiles are owner=255, so the vanilla
            // ownedProjectileCounts per player cannot determine active deliveries.
            bool hasDrone = false;
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile existing = Main.projectile[i];
                if (existing.active && existing.type == droneType && (int)existing.ai[2] == playerIndex)
                {
                    hasDrone = true;
                    break;
                }
            }
            if (hasDrone)
                continue;

            int chosenId = -1;
            foreach (KeyValuePair<int, DeliveryOrder> pair in Orders)
            {
                DeliveryOrder order = pair.Value;
                if (order.Owner != playerIndex || !ReferenceEquals(order.Recipient, player)
                    || order.Cooldown > 0 || order.InFlight)
                    continue;
                if (chosenId == -1 || pair.Key < chosenId)
                    chosenId = pair.Key;
            }

            if (chosenId < 0 || !Orders.TryGetValue(chosenId, out DeliveryOrder chosen))
                continue;

            Vector2 spawn = player.Center + new Vector2(0f, -Main.rand.Next(2000, 4001)).RotatedByRandom(MathHelper.ToRadians(75f));
            spawn.X = Math.Clamp(spawn.X, Main.leftWorld + 64f, Main.rightWorld - 64f);
            spawn.Y = Math.Clamp(spawn.Y, Main.topWorld + 64f, Main.bottomWorld - 64f);

            int projectileIndex = Projectile.NewProjectile(
                player.GetSource_Misc("KivotosTabletDroneDelivery"),
                spawn,
                Vector2.Zero,
                droneType,
                0,
                0f,
                Main.netMode == NetmodeID.SinglePlayer ? playerIndex : Main.maxPlayers, // local in SP; server-owned in MP
                chosenId,
                chosen.Package.type,
                playerIndex); // ai[2] identifies the recipient on every machine

            if (projectileIndex >= 0 && projectileIndex < Main.maxProjectiles
                && Main.projectile[projectileIndex].active)
            {
                global::KivotosMod.KivotosMod.Instance.Logger.Info($"[TabletDelivery] drone order={chosenId} projectile={projectileIndex} buyer={playerIndex}");
                // NewProjectile ignores ai arguments when owner != Main.myPlayer.
                // Set the authoritative delivery identity explicitly before sending it.
                Projectile drone = Main.projectile[projectileIndex];
                drone.ai[0] = chosenId;
                drone.ai[1] = chosen.Package.type;
                drone.ai[2] = playerIndex;
                chosen.InFlight = true;
                Main.projectile[projectileIndex].netUpdate = true;
                if (Main.netMode == NetmodeID.Server)
                    NetMessage.SendData(MessageID.SyncProjectile, -1, -1, null, projectileIndex);
            }
            else
            {
                chosen.Cooldown = 90; // retry instead of permanently losing the paid order
            }
        }
    }

    private static int ContentCount(Item item) => item.ModItem is DeliveryPackage package ? package.Contents.Count : 0;

    private static bool HasLiveDrone(int orderId, int recipient)
    {
        int type = ModContent.ProjectileType<DeliveryDroneProjectile>();
        for (int i = 0; i < Main.maxProjectiles; i++)
        {
            Projectile drone = Main.projectile[i];
            if (drone.active && drone.type == type && (int)drone.ai[0] == orderId && (int)drone.ai[2] == recipient)
                return true;
        }
        return false;
    }

    public static bool TryDropPackage(int orderId, Vector2 position)
    {
        if (Main.netMode == NetmodeID.MultiplayerClient)
            return false;
        if (!Orders.TryGetValue(orderId, out DeliveryOrder order))
            return false;
        if (order.Owner < 0 || order.Owner >= Main.maxPlayers)
            return false;

        Player player = Main.player[order.Owner];
        if (player == null || !ReferenceEquals(order.Recipient, player))
            return false;

        // NewItem returns 0 during world generation without creating an item.
        if (WorldGen.gen)
            return false;

        int worldItem = Item.NewItem(
            player.GetSource_Misc("KivotosTabletDroneDelivery"),
            position,
            order.Package);

        if (worldItem < 0 || worldItem >= Main.maxItems)
            return false; // leave the order queued for another attempt
        Item spawned = Main.item[worldItem];
        if (!spawned.active || spawned.type != order.Package.type || spawned.stack != 1
            || ContentCount(spawned) == 0 || ContentCount(spawned) != ContentCount(order.Package))
        {
            global::KivotosMod.KivotosMod.Instance.Logger.Warn($"[TabletDelivery] drop rejected order={orderId} slot={worldItem} active={spawned.active} type={spawned.type} contents={ContentCount(spawned)}");
            return false;
        }
        global::KivotosMod.KivotosMod.Instance.Logger.Info($"[TabletDelivery] dropped order={orderId} slot={worldItem} contents={ContentCount(spawned)} position={position} mode={Main.netMode}");

        Main.item[worldItem].velocity = Vector2.Zero;
        // DeliveryPackage stores its purchased items in ModItem.NetSend.
        // An explicit SyncItem is needed for late/update packets in multiplayer.
        if (Main.netMode == NetmodeID.Server)
            NetMessage.SendData(MessageID.SyncItem, -1, -1, null, worldItem);

        Orders.Remove(orderId);
        order.OnDelivered?.Invoke();
        return true;
    }

    public static void NotifyDroneEnded(int orderId, bool delivered)
    {
        if (Main.netMode == NetmodeID.MultiplayerClient)
            return;
        if (delivered)
            return;
        if (!Orders.TryGetValue(orderId, out DeliveryOrder order))
            return;

        order.InFlight = false;
        order.Cooldown = 90;
    }

    internal static void CancelMailOrder(int id, Player player)
    {
        if (Orders.TryGetValue(id,out var order) && ReferenceEquals(order.Recipient,player) && order.OnDelivered != null)
            Orders.Remove(id);
    }

    public override void OnWorldUnload()
    {
        Orders.Clear();
        _nextOrderId = 1;
    }

    public override void Unload()
    {
        Orders.Clear();
        _nextOrderId = 1;
    }
}
