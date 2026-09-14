using System.Collections.Generic;
using Exiled.API.Enums;
using Exiled.API.Features;
using Exiled.API.Features.Spawn;
using Exiled.CustomItems.API.Features;
using Exiled.Events.EventArgs.Player;
using PlayerRoles;
using UnityEngine;

namespace EyeCatcherPlugin
{
    public class JointThickenerItem : CustomWeapon
    {
        public override uint Id { get; set; } = 1736;
        public override string Name { get; set; } = "Загуститель швов";
        public override string Description { get; set; } = "Оружие на базе разрушителя. Отменяет урон, но блокирует снятие маскировки у SCP-3114 на 5 минут.";
        public override ItemType Type { get; set; } = ItemType.ParticleDisruptor;
        public override float Weight { get; set; } = 4f;
        public override byte ClipSize { get; set; } = 1;
        public override float Damage { get; set; } = 0f;
        public override SpawnProperties SpawnProperties { get; set; } = new SpawnProperties();

        public static Dictionary<Player, float> BlockedSkeletons { get; } = new Dictionary<Player, float>();

        protected override void SubscribeEvents()
        {
            Exiled.Events.Handlers.Player.Hurting += OnHurting;
            Exiled.Events.Handlers.Scp3114.Revealing += OnRevealing3114;
            base.SubscribeEvents();
        }

        protected override void UnsubscribeEvents()
        {
            Exiled.Events.Handlers.Player.Hurting -= OnHurting;
            Exiled.Events.Handlers.Scp3114.Revealing -= OnRevealing3114;
            base.UnsubscribeEvents();
        }

        private void OnHurting(HurtingEventArgs ev)
        {
            if (ev.Attacker != null && ev.Attacker.CurrentItem != null && Check(ev.Attacker.CurrentItem))
            {
                ev.Amount = 0f;
                ev.IsAllowed = false;

                if (ev.Player != null && ev.Player.Role.Type == RoleTypeId.Scp3114)
                {
                    var scpRole = ev.Player.Role.Base as PlayerRoles.PlayableScps.Scp3114.Scp3114Role;
                    if (scpRole != null && scpRole.Disguised)
                    {
                        float lockDuration = Time.time + 300f; BlockedSkeletons[ev.Player] = lockDuration;

                        ev.Attacker.ShowHint("<color=green>Швы зафиксированы! SCP-3114 не сможет снять кожу в течение 5 минут!</color>", 5f);
                        ev.Player.ShowHint("<color=red>Ваши швы загустели! Вы не можете принудительно снять маскировку на 5 минут!</color>", 5f);
                    }
                }
            }
        }

        private void OnRevealing3114(Exiled.Events.EventArgs.Scp3114.RevealingEventArgs ev)
        {
            if (ev.Player != null && BlockedSkeletons.TryGetValue(ev.Player, out float endTime))
            {
                if (Time.time < endTime)
                {
                    ev.IsAllowed = false; ev.Player.ShowHint($"<color=red>Вы не можете снять маскировку! Швы заблокированы еще {Mathf.CeilToInt(endTime - Time.time)} сек.</color>", 2f);
                }
                else
                {
                    BlockedSkeletons.Remove(ev.Player);
                }
            }
        }
    }
}
