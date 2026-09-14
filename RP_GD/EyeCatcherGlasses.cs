using Exiled.API.Features;
using Exiled.API.Features.Items;
using Exiled.API.Features.Spawn;
using Exiled.CustomItems.API.Features;
using InventorySystem.Items.Usables.Scp1344;
using PlayerRoles.FirstPersonControl.Thirdperson.Subcontrollers.Wearables;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static EyeCatcherPlugin.Methods;

namespace EyeCatcherPlugin
{
    public class EyeCatcherGlasses : CustomGoggles
    {
        public HashSet<Player> ActiveGlassesPlayers { get; }
            = new HashSet<Player>();

        public override uint Id { get; set; } = 1730;

        public override float Weight { get; set; } = 1f;

        public override float WearingTime { get; set; } = 1f;

        public override float RemovingTime { get; set; } = 1f;

        public override string Name { get; set; }
            = "Anti-096";

        public override string Description { get; set; }
            = "Black glasses that censor SCP-096.";

        public override SpawnProperties SpawnProperties { get; set; }
            = new SpawnProperties();

        public override void Init()
        {
            base.Init();
        }

        public override void Destroy()
        {
            ActiveGlassesPlayers.Clear();

            base.Destroy();
        }

        protected override void OnWornGoggles(
    Player player,
    Scp1344 goggles)
        {
            if (player == null)
                return;

            if (Player.List.Any(p => p.Role is Exiled.API.Features.Roles.Scp096Role role096 && role096.Targets.Contains(player)))
            {
                player.ShowHint("<color=red><b>Поздно</b></color>", 3f);
                MEC.Timing.CallDelayed(0.1f, () => { if (player != null) player.RemoveItem(player.Items.FirstOrDefault(i => i.Serial == goggles.Serial)); this.Give(player); });
                return;
            }

            ActiveGlassesPlayers.Add(player);


            ObfuscateScp96s(player);

            foreach (Player spectator in player.CurrentSpectatingPlayers)
            {
                ObfuscateScp96s(spectator);

                Plugin.Instance.EventHandlers.DirtyPlayers.Add(
                    spectator);
            }

            Log.Debug(
                $"[EyeCatcher] {player.Nickname} activated glasses.");
        }

        protected override void OnRemovedGoggles(
            Player player,
            Scp1344 goggles)
        {
            if (player == null)
                return;

            ActiveGlassesPlayers.Remove(player);

            DeObfuscateScp96s(player);

            foreach (Player spectator in player.CurrentSpectatingPlayers)
            {
                DeObfuscateScp96s(spectator);

                Plugin.Instance.EventHandlers.DirtyPlayers.Remove(
                    spectator);
            }

            Log.Debug(
                $"[EyeCatcher] {player.Nickname} deactivated glasses.");
        }

        protected override void OnWaitingForPlayers()
        {
            ActiveGlassesPlayers.Clear();

            base.OnWaitingForPlayers();
        }

        protected override void SubscribeEvents()
        {
            Exiled.Events.Handlers.Scp096.AddingTarget +=
                OnAddingTarget;

            base.SubscribeEvents();
        }

        protected override void UnsubscribeEvents()
        {
            Exiled.Events.Handlers.Scp096.AddingTarget -=
                OnAddingTarget;

            base.UnsubscribeEvents();
        }

        private void OnAddingTarget(
            Exiled.Events.EventArgs.Scp096.AddingTargetEventArgs ev)
        {
            if (ev.Target == null)
                return;

            if (!ev.IsLooking)
                return;

            if (!ActiveGlassesPlayers.Contains(ev.Target))
                return;

            ev.IsAllowed = false;
        }
    }
}