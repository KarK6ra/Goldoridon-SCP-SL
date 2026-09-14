using System;
using System.Collections.Generic;
using System.Linq;
using CommandSystem;
using Exiled.API.Enums;
using Exiled.API.Features;
using MEC;
using PlayerRoles;
using UnityEngine;

namespace EyeCatcherPlugin
{
    [CommandHandler(typeof(ClientCommandHandler))]
    public class Drag173Command : ICommand
    {
        public string Command => "drag173";
        public string[] Aliases => new string[] { };
        public string Description => "Начать транспортировку клетки с SCP-173";

        public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
        {
            Player player = Player.Get(sender);
            if (player == null || !player.IsHuman)
            {
                response = "Команда доступна только живым людям!";
                return false;
            }

            if (CageItem.DraggingPlayers.ContainsKey(player))
            {
                response = "Вы уже тащите клетку! Пропишите .release173, чтобы отпустить.";
                return false;
            }

            if (Physics.SphereCast(player.CameraTransform.position, 0.4f, player.CameraTransform.forward, out RaycastHit hit, 4.5f))
            {
                Player target173 = Player.Get(hit.collider);
                if (target173 != null && target173.Role.Type == RoleTypeId.Scp173 && CageItem.Caged173s.Contains(target173))
                {
                    if (CageItem.DraggingPlayers.ContainsValue(target173))
                    {
                        response = "Эту клетку уже тащит другой сотрудник!";
                        return false;
                    }

                    player.EnableEffect(EffectType.SinkHole);
                    player.CurrentItem = null;

                    CageItem.DraggingPlayers.Add(player, target173);
                    Timing.RunCoroutine(DragAI(player, target173));

                    response = "Вы подцепили клетку с SCP-173 на буксир. Она следует за вами.";
                    return true;
                }
            }

            response = "Подойдите ближе и посмотрите в упор на клетку с SCP-173!";
            return false;
        }

        public static IEnumerator<float> DragAI(Player leader, Player scp173)
        {
            while (leader != null && scp173 != null && CageItem.DraggingPlayers.TryGetValue(leader, out Player target) && target == scp173)
            {
                if (leader.CurrentItem != null)
                {
                    leader.CurrentItem = null;
                    leader.ShowHint("<color=red>Вы не можете держать предметы, пока тащите клетку!</color>", 2f);
                }

                Vector3 targetPos = leader.Position - (leader.Transform.forward * 1.5f);
                targetPos.y = leader.Position.y;
                float distance = Vector3.Distance(scp173.Position, targetPos);
                if (distance > 0.05f)
                {
                    if (scp173.GameObject.TryGetComponent(out Rigidbody rb))
                    {
                        rb.velocity = Vector3.zero;
                        rb.angularVelocity = Vector3.zero;
                    }

                    scp173.Position = Vector3.Lerp(scp173.Position, targetPos, 12f * Time.deltaTime);

                    scp173.Rotation = leader.Rotation;
                }

                yield return Timing.WaitForOneFrame;
            }
        }
    }

    [CommandHandler(typeof(ClientCommandHandler))]
    public class Release173Command : ICommand
    {
        public string Command => "release173";
        public string[] Aliases => new string[] { };
        public string Description => "Отпустить клетку с SCP-173";

        public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
        {
            Player player = Player.Get(sender);
            if (player == null)
            {
                response = "Игрок не найден.";
                return false;
            }

            if (CageItem.DraggingPlayers.TryGetValue(player, out Player scp173))
            {
                player.DisableEffect(EffectType.SinkHole);
                CageItem.DraggingPlayers.Remove(player);

                response = "Вы отпустили клетку буксировки.";
                return true;
            }

            response = "Вы никого не тащите.";
            return false;
        }
    }
}