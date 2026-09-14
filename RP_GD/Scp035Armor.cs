using System.Collections.Generic;
using System.Linq;
using Exiled.API.Enums;
using Exiled.API.Features;
using Exiled.API.Features.Attributes;
using Exiled.API.Features.Spawn;
using Exiled.API.Features.Toys;
using Exiled.CustomItems.API.Features;
using Exiled.Events.EventArgs.Player;
using MEC;
using PlayerRoles;
using UnityEngine;

namespace EyeCatcherPlugin
{
    public class Scp035Armor : CustomItem
    {
        public override uint Id { get; set; } = 1735;
        public override string Name { get; set; } = "Маска Одержимости SCP-035";
        public override string Description { get; set; } = "Зловещая театральная маска. Если поднять её и не выбросить за 5 секунд, она поглотит ваш разум!";
        public override ItemType Type { get; set; } = ItemType.ArmorLight; public override float Weight { get; set; } = 0.5f;
        public override SpawnProperties SpawnProperties { get; set; } = new SpawnProperties();

        public static Player Active035Player { get; private set; } = null;
        public static List<GameObject> SpawnedMaskParts { get; } = new List<GameObject>();
        private static readonly HashSet<ushort> TrackedPickupSerials = new HashSet<ushort>();

        public static HashSet<CoroutineHandle> ActiveMaskCoroutines { get; } = new HashSet<CoroutineHandle>();

        private static readonly Dictionary<Player, CoroutineHandle> WearTimers = new Dictionary<Player, CoroutineHandle>();

        protected override void SubscribeEvents()
        {
            Exiled.Events.Handlers.Player.ItemAdded += OnItemAdded;
            Exiled.Events.Handlers.Player.DroppingItem += OnDroppingMaskItem;
            Exiled.Events.Handlers.Player.Hurting += OnHurting35;
            Exiled.Events.Handlers.Server.RoundStarted += OnRoundStarted;
            Exiled.Events.Handlers.Player.Dying += OnDying35;

            Exiled.Events.Handlers.Player.ChangingRole += OnChangingRole35;
            Exiled.Events.Handlers.Player.Left += OnLeft35;
            base.SubscribeEvents();
        }

        protected override void UnsubscribeEvents()
        {
            Exiled.Events.Handlers.Player.ItemAdded -= OnItemAdded;
            Exiled.Events.Handlers.Player.DroppingItem -= OnDroppingMaskItem;
            Exiled.Events.Handlers.Player.Hurting -= OnHurting35;
            Exiled.Events.Handlers.Server.RoundStarted -= OnRoundStarted;
            Exiled.Events.Handlers.Player.Dying -= OnDying35;


            Exiled.Events.Handlers.Player.ChangingRole -= OnChangingRole35;
            Exiled.Events.Handlers.Player.Left -= OnLeft35;
            base.UnsubscribeEvents();
        }

        private void OnRoundStarted()
        {
            ClearMaskObjects();
            Active035Player = null;
            WearTimers.Clear();



            Timing.CallDelayed(5f, () =>
            {
                Room doctorRoom = Room.List.FirstOrDefault(r => r.Type == RoomType.Hcz049);
                if (doctorRoom != null)
                {
                    Vector3 spawnPos = doctorRoom.Transform.TransformPoint(new Vector3(0f, 1.0f, -3.5f));
                    var customPickup = Spawn(spawnPos);

                    if (customPickup != null && customPickup.GameObject != null)
                    {
                        Log.Info("[SCP-035] Маска-монетка успешно материализована на полу.");
                    }

                }
            });
        }


        private void OnItemAdded(ItemAddedEventArgs ev)
        {
            if (ev.Player != null && ev.Item != null && Check(ev.Item))
            {
                if (Active035Player != null) return;
                CoroutineHandle handle = Timing.RunCoroutine(TransformationTimer(ev.Player));
                WearTimers[ev.Player] = handle;
            }
        }

        private void OnDroppingMaskItem(DroppingItemEventArgs ev)
        {
            if (ev.Player == null || ev.Item == null) return;

            if (Check(ev.Item) && WearTimers.TryGetValue(ev.Player, out CoroutineHandle handle))
            {
                Timing.KillCoroutines(handle);
                WearTimers.Remove(ev.Player);
                ev.Player.ShowHint("<color=green>Вы вовремя выбросили маску... Её шепот прекратился.</color>", 3f);
                return;
            }

            if (Active035Player == ev.Player && Check(ev.Item))
            {
                ev.IsAllowed = false;
                ev.Player.ShowHint("<color=red>Маска срослась с вашим разумом и телом!</color>", 3f);
            }

        }


        private IEnumerator<float> TransformationTimer(Player player)
        {
            for (int i = 5; i > 0; i--)
            {
                player.ShowHint($"<color=red>Маска захватывает ваш разум! Сбросьте монетку! Отсчет: {i} сек.</color>", 1f);
                yield return Timing.WaitForSeconds(1f);
            }

            WearTimers.Remove(player);
            Active035Player = player;

            player.CustomName = "SCP-035";
            player.MaxHealth = 500f;
            player.Health = 500f;

            player.EnableEffect(EffectType.CardiacArrest, 99999f);
            player.ShowHint("<color=red><b>ВЫ СТАЛИ ОБЪЕКТОМ SCP-035!</b>\nВаше тело гниет. Маска материализовалась на вашем лице!</color>", 10f);



            while (player != null && player.IsAlive && Active035Player == player)
            {
                yield return Timing.WaitForSeconds(5f);
                if (player == null || !player.IsAlive) break;
                if (player.Health <= 2f)
                {
                    player.Kill("Маска полностью разъела носителя");
                    break;
                }
                player.Health -= 2f;
            }
        }
        public static void ClearMaskObjects()
        {
            foreach (var handle in ActiveMaskCoroutines)
                Timing.KillCoroutines(handle);
            ActiveMaskCoroutines.Clear();
            TrackedPickupSerials.Clear();

            foreach (var part in SpawnedMaskParts)
            {
                if (part != null) Mirror.NetworkServer.Destroy(part);
            }
            SpawnedMaskParts.Clear();
        }

        private void OnDying35(DyingEventArgs ev)
        {
            if (ev.Player == Active035Player) Reset035(ev.Player);
        }

        private void OnChangingRole35(ChangingRoleEventArgs ev)
        {
            if (ev.Player == null) return;

            if (WearTimers.TryGetValue(ev.Player, out CoroutineHandle handle))
            {
                Timing.KillCoroutines(handle);
                WearTimers.Remove(ev.Player);
            }

            if (Active035Player == ev.Player) Reset035(ev.Player);
        }

        private void OnLeft35(LeftEventArgs ev)
        {
            if (ev.Player == null) return;

            if (WearTimers.TryGetValue(ev.Player, out CoroutineHandle handle))
            {
                Timing.KillCoroutines(handle);
                WearTimers.Remove(ev.Player);
            }

            if (Active035Player == ev.Player) Reset035(ev.Player);
        }

        private void Reset035(Player player)
        {
            player.CustomName = null;
            Active035Player = null;
            ClearMaskObjects();
        }

        private void OnHurting35(HurtingEventArgs ev)
        {
            if (ev.Player == Active035Player && ev.DamageHandler.Type == DamageType.CardiacArrest)
            {
                ev.Amount = 0f;
                ev.IsAllowed = false;
            }
        }
    }
}

