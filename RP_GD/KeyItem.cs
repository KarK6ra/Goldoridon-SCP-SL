using System.Collections.Generic;
using Exiled.API.Features;
using Exiled.API.Features.Attributes;
using Exiled.API.Features.Spawn;
using Exiled.API.Enums;
using Exiled.CustomItems.API.Features;
using Exiled.Events.EventArgs.Player;
using MEC;
using PlayerRoles;
using UnityEngine;

namespace EyeCatcherPlugin
{
    public class KeyItem : CustomItem
    {
        public override uint Id { get; set; } = 1733;
        public override string Name { get; set; } = "Ключ от кандалов";
        public override string Description { get; set; } = "Подбросьте монетку (нажмите осмотр/использовать), глядя на скованного SCP-049, чтобы освободить его.";
        public override ItemType Type { get; set; } = ItemType.Coin;
        public override float Weight { get; set; } = 0.5f;
        public override SpawnProperties SpawnProperties { get; set; } = new SpawnProperties();

        public override Vector3 Scale { get; set; } = new Vector3(3f, 3f, 3f);

        public static Dictionary<Player, float> Cooldowns { get; } = new Dictionary<Player, float>();
        public static Dictionary<Player, CoroutineHandle> ActiveUncuffing { get; } = new Dictionary<Player, CoroutineHandle>();

        private static HashSet<Player> _activeDroppingBlockers { get; } = new HashSet<Player>();

        protected override void SubscribeEvents()
        {
            Exiled.Events.Handlers.Player.FlippingCoin += OnFlippingCoin;
            Exiled.Events.Handlers.Player.DroppingItem += OnDroppingItem;
            base.SubscribeEvents();
        }

        protected override void UnsubscribeEvents()
        {
            Exiled.Events.Handlers.Player.FlippingCoin -= OnFlippingCoin;
            Exiled.Events.Handlers.Player.DroppingItem -= OnDroppingItem;
            base.UnsubscribeEvents();
        }

        private void OnDroppingItem(DroppingItemEventArgs ev)
        {
            if (ev.Player != null && ev.Item != null && Check(ev.Item))
            {
                if (_activeDroppingBlockers.Contains(ev.Player))
                {
                    ev.IsAllowed = false;
                    ev.Player.ShowHint("<color=red>Нельзя выбросить ключ во время развязывания!</color>", 2f);
                }
            }
        }

        private void OnFlippingCoin(FlippingCoinEventArgs ev)
        {
            if (ev.Player == null || ev.Item == null || !Check(ev.Item)) return;

            ev.IsAllowed = false;

            if (Cooldowns.TryGetValue(ev.Player, out float cdTime) && Time.time < cdTime)
            {
                ev.Player.ShowHint($"Ключ на перезарядке! Осталось {Mathf.CeilToInt(cdTime - Time.time)} сек.", 2f);
                return;
            }

            if (Physics.SphereCast(ev.Player.CameraTransform.position, 0.4f, ev.Player.CameraTransform.forward, out RaycastHit hit, 4.5f))
            {
                Player target = Player.Get(hit.collider);
                if (target != null && target.Role.Type == RoleTypeId.Scp049)
                {
                    if (!EventHandlers.Cuffed049s.Contains(target))
                    {
                        ev.Player.ShowHint("Этот SCP-049 не закован в кандалы!", 3f);
                        return;
                    }

                    if (ActiveUncuffing.ContainsKey(target) || CuffsItem.ActiveCuffing.ContainsKey(target))
                    {
                        ev.Player.ShowHint("Этого SCP-049 уже развязывают или связывают!", 3f);
                        return;
                    }

                    CoroutineHandle handle = Timing.RunCoroutine(UncuffingProcess(ev.Player, target, ev.Item));
                    ActiveUncuffing.Add(target, handle);
                    return;
                }
            }

            ev.Player.ShowHint("Вы должны смотреть на связанного SCP-049!", 3f);
        }

        private IEnumerator<float> UncuffingProcess(Player human, Player scp049, Exiled.API.Features.Items.Item item)
        {
            Vector3 startPos = scp049.Position;

            human.EnableEffect(EffectType.Ensnared);
            _activeDroppingBlockers.Add(human);

            for (int i = 0; i < 10; i++)
            {
                if (human == null || scp049 == null || scp049.Role.Type != RoleTypeId.Scp049)
                {
                    BreakProcess(scp049, human, "Процесс прерван: цель потеряна.");
                    yield break;
                }

                if (Vector3.Distance(scp049.Position, startPos) > 0.1f)
                {
                    BreakProcess(scp049, human, "Развязывание сорвано! SCP-049 пошевелился.");
                    Cooldowns[human] = Time.time + 10f;
                    yield break;
                }

                human.ShowHint($"Идёт развязывание... ({10 - i} сек.)", 1f);
                scp049.ShowHint("<color=orange>Вас пытаются развязать! НЕ ДВИГАЙТЕСЬ!</color>", 1f);
                yield return Timing.WaitForSeconds(1f);
            }

            ActiveUncuffing.Remove(scp049);

            if (human != null)
            {
                human.DisableEffect(EffectType.Ensnared);
                _activeDroppingBlockers.Remove(human);
            }

            scp049.RemoveHandcuffs();
            EventHandlers.Cuffed049s.Remove(scp049);

            human.RemoveItem(item);
            CustomItem.Get(1732).Give(human);

            human.ShowHint("<color=green>SCP-049 успешно освобожден!</color>", 5f);
            scp049.ShowHint("<color=green>С вас сняли кандалы! Вы снова можете атаковать.</color>", 5f);
        }

        private void BreakProcess(Player scp049, Player human, string message)
        {
            if (human != null)
            {
                human.DisableEffect(EffectType.Ensnared);
                _activeDroppingBlockers.Remove(human);
                human.ShowHint($"<color=red>{message}</color>", 3f);
            }
            if (scp049 != null)
            {
                ActiveUncuffing.Remove(scp049);
            }
        }
    }
}
