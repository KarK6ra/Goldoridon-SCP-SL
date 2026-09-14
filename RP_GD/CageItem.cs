using System.Collections.Generic;
using Exiled.API.Features;
using Exiled.API.Features.Spawn;
using Exiled.API.Features.Toys;
using Exiled.API.Enums;
using Exiled.CustomItems.API.Features;
using Exiled.Events.EventArgs.Player;
using MEC;
using PlayerRoles;
using UnityEngine;

namespace EyeCatcherPlugin
{
    public class CageItem : CustomItem
    {
        public override uint Id { get; set; } = 1734;
        public override string Name { get; set; } = "Сдерживающая Клетка SCP-173";
        public override string Description { get; set; } = "Используйте аптечку перед SCP-173, чтобы заковать объект в инженерную клетку.";
        public override ItemType Type { get; set; } = ItemType.Medkit;
        public override float Weight { get; set; } = 3f;
        public override SpawnProperties SpawnProperties { get; set; } = new SpawnProperties();

        public static HashSet<Player> Caged173s { get; } = new HashSet<Player>();
        public static Dictionary<Player, List<GameObject>> SpawnedCages { get; } = new Dictionary<Player, List<GameObject>>();
        public static Dictionary<Player, Player> DraggingPlayers { get; } = new Dictionary<Player, Player>();

        private static HashSet<Player> _activeCageBuilders { get; } = new HashSet<Player>();

        protected override void SubscribeEvents()
        {
            Exiled.Events.Handlers.Player.UsingItem += OnUsingItem;
            Exiled.Events.Handlers.Player.DroppingItem += OnDroppingItem;
            base.SubscribeEvents();
        }

        protected override void UnsubscribeEvents()
        {
            Exiled.Events.Handlers.Player.UsingItem -= OnUsingItem;
            Exiled.Events.Handlers.Player.DroppingItem -= OnDroppingItem;
            base.UnsubscribeEvents();
        }

        private void OnDroppingItem(DroppingItemEventArgs ev)
        {
            if (ev.Player != null && ev.Item != null && Check(ev.Item))
            {
                if (_activeCageBuilders.Contains(ev.Player))
                {
                    ev.IsAllowed = false;
                    ev.Player.ShowHint("<color=red>Нельзя выбросить инженерную аптечку во время развертывания клетки!</color>", 3f);
                }
            }
        }

        private void OnUsingItem(UsingItemEventArgs ev)
        {
            if (ev.Player == null || ev.Item == null || !Check(ev.Item)) return;

            ev.IsAllowed = false;

            if (Physics.SphereCast(ev.Player.CameraTransform.position, 0.4f, ev.Player.CameraTransform.forward, out RaycastHit hit, 4.5f))
            {
                Player target = Player.Get(hit.collider);
                if (target != null && target.Role.Type == RoleTypeId.Scp173)
                {
                    if (Caged173s.Contains(target))
                    {
                        ev.Player.ShowHint("<color=red>Этот SCP-173 уже находится в клетке!</color>", 3f);
                        return;
                    }

                    Timing.RunCoroutine(CageProcess(ev.Player, target, ev.Item));
                    return;
                }
            }

            ev.Player.ShowHint("<color=red>Вы должны смотреть прямо на SCP-173, чтобы использовать клетку!</color>", 3f);
        }

        private IEnumerator<float> CageProcess(Player human, Player scp173, Exiled.API.Features.Items.Item item)
        {
            human.EnableEffect(EffectType.Ensnared, 30f);
            _activeCageBuilders.Add(human);

            for (int i = 0; i < 30; i++)
            {
                if (human == null || scp173 == null || scp173.Role.Type != RoleTypeId.Scp173)
                {
                    if (human != null)
                    {
                        human.DisableEffect(EffectType.Ensnared);
                        _activeCageBuilders.Remove(human);
                    }
                    yield break;
                }

                human.ShowHint($"Установка инженерной клетки... Осталось {30 - i} сек.", 1f);
                yield return Timing.WaitForSeconds(1f);
            }

            if (human != null)
            {
                human.DisableEffect(EffectType.Ensnared);
                _activeCageBuilders.Remove(human);
            }

            if (scp173 == null || scp173.Role.Type != RoleTypeId.Scp173) yield break;

            Caged173s.Add(scp173);
            human.RemoveItem(item);

            List<GameObject> cageParts = new List<GameObject>();

            Vector3 basePos = scp173.Position + Vector3.down * 1.0f;

            cageParts.Add(CreateCagePart(PrimitiveType.Cylinder, basePos + new Vector3(0f, 0f, 0f), new Vector3(0f, 0f, 0f), new Vector3(2.2f, 0.08f, 2.2f), scp173));
            cageParts.Add(CreateCagePart(PrimitiveType.Cylinder, basePos + new Vector3(0f, 2.4f, 0f), new Vector3(0f, 0f, 0f), new Vector3(2.2f, 0.08f, 2.2f), scp173));

            float radius = 1.0f;
            for (int i = 0; i < 8; i++)
            {
                float angle = i * Mathf.PI / 4f;
                Vector3 offset = new Vector3(Mathf.Cos(angle) * radius, 1.2f, Mathf.Sin(angle) * radius);
                cageParts.Add(CreateCagePart(PrimitiveType.Cylinder, basePos + offset, new Vector3(0f, 0f, 0f), new Vector3(0.08f, 1.2f, 0.08f), scp173));
            }

            SpawnedCages.Add(scp173, cageParts);
            scp173.EnableEffect(EffectType.Ensnared);

            human.ShowHint("<color=green>✓ SCP-173 успешно изолирован в клетку примитивов!</color>", 5f);
            scp173.ShowHint("<color=red>Вы заперты в клетку! Ваши функции перемещения и атаки заблокированы.</color>", 5f);
        }

        private GameObject CreateCagePart(PrimitiveType type, Vector3 pos, Vector3 rot, Vector3 scale, Player parent)
        {
            Primitive prim = Primitive.Create(type, pos, rot, scale, true, Color.black);
            prim.Collidable = true;
            prim.MovementSmoothing = 0;
            prim.Transform.SetParent(parent.Transform, true);
            return prim.GameObject;
        }

        public static void DestroyCage(Player scp173)
        {
            if (scp173 == null) return;
            Caged173s.Remove(scp173);
            scp173.DisableEffect(EffectType.Ensnared);

            if (SpawnedCages.TryGetValue(scp173, out var parts))
            {
                foreach (var part in parts)
                {
                    if (part != null) Mirror.NetworkServer.Destroy(part);
                }
                SpawnedCages.Remove(scp173);
            }

            foreach (var pair in new Dictionary<Player, Player>(DraggingPlayers))
            {
                if (pair.Value == scp173)
                {
                    pair.Key.DisableEffect(EffectType.SinkHole);
                    DraggingPlayers.Remove(pair.Key);
                    break;
                }
            }
        }
    }
}