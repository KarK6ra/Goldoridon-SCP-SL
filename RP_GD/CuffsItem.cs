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
    public class CuffsItem : CustomItem
    {
        public override uint Id { get; set; } = 1732;
        public override string Name { get; set; } = "Кандалы SCP-049";
        public override string Description { get; set; } = "Выбросьте рацию перед SCP-049, чтобы начать связывание.";
        public override ItemType Type { get; set; } = ItemType.Radio;
        public override float Weight { get; set; } = 2f;
        public override SpawnProperties SpawnProperties { get; set; } = new SpawnProperties();

        public override Vector3 Scale { get; set; } = new Vector3(3f, 3f, 3f);

        public static Dictionary<Player, float> Cooldowns { get; } = new Dictionary<Player, float>();
        public static Dictionary<Player, CoroutineHandle> ActiveCuffing { get; } = new Dictionary<Player, CoroutineHandle>();

        protected override void SubscribeEvents()
        {
            Exiled.Events.Handlers.Player.DroppingItem += OnDroppingItem;
            base.SubscribeEvents();
        }

        protected override void UnsubscribeEvents()
        {
            Exiled.Events.Handlers.Player.DroppingItem -= OnDroppingItem;
            base.UnsubscribeEvents();
        }

        private void OnDroppingItem(DroppingItemEventArgs ev)
        {
            if (ev.Player == null || ev.Item == null || !Check(ev.Item)) return;

            if (Cooldowns.TryGetValue(ev.Player, out float cdTime) && Time.time < cdTime)
            {
                ev.IsAllowed = false;
                ev.Player.ShowHint($"Предмет на перезарядке! Осталось {Mathf.CeilToInt(cdTime - Time.time)} сек.", 2f);
                return;
            }

            if (Physics.SphereCast(ev.Player.CameraTransform.position, 0.4f, ev.Player.CameraTransform.forward, out RaycastHit hit, 4.5f))
            {
                Player target = Player.Get(hit.collider);
                if (target != null && target.Role.Type == RoleTypeId.Scp049)
                {
                    if (ActiveCuffing.ContainsKey(target) || KeyItem.ActiveUncuffing.ContainsKey(target))
                    {
                        ev.IsAllowed = false; ev.Player.ShowHint("Этого SCP-049 уже кто-то связывает или развязывает!", 3f);
                        return;
                    }

                    if (EventHandlers.Cuffed049s.Contains(target))
                    {
                        ev.IsAllowed = false; ev.Player.ShowHint("Этот SCP-049 уже закован в кандалы!", 3f);
                        return;
                    }

                    ev.IsAllowed = false;

                    CoroutineHandle handle = Timing.RunCoroutine(CuffingProcess(ev.Player, target, ev.Item));
                    ActiveCuffing.Add(target, handle);
                    return;
                }
            }

        }


        private IEnumerator<float> CuffingProcess(Player human, Player scp049, Exiled.API.Features.Items.Item item)
        {
            Vector3 startPos = scp049.Position;

            human.EnableEffect(EffectType.Ensnared);

            for (int i = 0; i < 10; i++)
            {
                if (human == null || scp049 == null || scp049.Role.Type != RoleTypeId.Scp049)
                {
                    BreakProcess(scp049, human, "Процесс прерван: цель потеряна.");
                    yield break;
                }

                if (Vector3.Distance(scp049.Position, startPos) > 0.1f)
                {
                    BreakProcess(scp049, human, "Связывание сорвано! SCP-049 пошевелился.");
                    Cooldowns[human] = Time.time + 10f;
                    yield break;
                }

                human.ShowHint($"Идёт связывание... ({10 - i} сек.)", 1f);
                scp049.ShowHint("<color=red>Вас пытаются связать! НЕ ДВИГАЙТЕСЬ, чтобы сорвать процесс!</color>", 1f);
                yield return Timing.WaitForSeconds(1f);
            }

            ActiveCuffing.Remove(scp049);

            if (human != null)
                human.DisableEffect(EffectType.Ensnared);

            scp049.Handcuff();
            EventHandlers.Cuffed049s.Add(scp049);

            human.RemoveItem(item);
            CustomItem.Get(1733).Give(human);


            human.ShowHint("<color=green>SCP-049 успешно закован в кандалы!</color>", 5f);
            scp049.ShowHint("<color=red>Вы закованы в кандалы! Вы больше не можете атаковать и воскрешать.</color>", 5f);
        }

        private void BreakProcess(Player scp049, Player human, string message)
        {
            if (human != null)
            {
                human.DisableEffect(EffectType.Ensnared);
                human.ShowHint($"<color=red>{message}</color>", 3f);
            }
            if (scp049 != null)
            {
                ActiveCuffing.Remove(scp049);
            }
        }
    }
}
