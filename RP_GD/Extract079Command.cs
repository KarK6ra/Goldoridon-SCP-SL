using System;
using System.Collections.Generic;
using CommandSystem;
using Exiled.API.Enums;
using Exiled.API.Features;
using MEC;
using PlayerRoles;
using UnityEngine;

namespace EyeCatcherPlugin
{
    [CommandHandler(typeof(ClientCommandHandler))]
    public class Extract079Command : ICommand
    {
        public string Command => "extract079";
        public string[] Aliases => new string[] { "выкачать079", "extract79" };
        public string Description => "Выкачать данные SCP-079. Нужно стоять в комнате 079 и держать карту Повстанцев Хаоса.";

        public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
        {
            Player player = Player.Get(sender);
            if (player == null || !player.IsHuman)
            {
                response = "Команда доступна только живым людям!";
                return false;
            }

            if (player.Role.Type != RoleTypeId.ClassD && player.Role.Side != Side.ChaosInsurgency)
            {
                response = "Эту команду могут использовать только Class-D или Повстанцы Хаоса!";
                return false;
            }

            if (player.CurrentRoom == null || player.CurrentRoom.Type != RoomType.Hcz079)
            {
                response = "Вы должны находиться в комнате содержания SCP-079!";
                return false;
            }

            if (player.CurrentItem == null || player.CurrentItem.Type != ItemType.KeycardChaosInsurgency)
            {
                response = "Вы должны держать в руках карту Повстанцев Хаоса!";
                return false;
            }

            if (EventHandlers.Active079Extractions.ContainsKey(player))
            {
                response = "Вы уже выкачиваете данные!";
                return false;
            }

            CoroutineHandle handle = Timing.RunCoroutine(ExtractProcess(player, player.CurrentItem));
            EventHandlers.Active079Extractions[player] = handle;

            response = "Начата выкачка данных SCP-079. Не убирайте карту и не выходите из комнаты 10 секунд!";
            return true;
        }

        private IEnumerator<float> ExtractProcess(Player player, Exiled.API.Features.Items.Item card)
        {
            player.EnableEffect(EffectType.Ensnared, 12f);

            for (int i = 0; i < 10; i++)
            {
                if (player == null || !player.IsAlive ||
                    player.CurrentRoom == null || player.CurrentRoom.Type != RoomType.Hcz079 ||
                    player.CurrentItem == null || player.CurrentItem.Serial != card.Serial)
                {
                    BreakExtract(player, "Выкачка прервана: вы покинули комнату или убрали карту.");
                    yield break;
                }

                player.ShowHint($"<color=#00FFFF>Выкачка данных SCP-079... {10 - i} сек.</color>", 1.1f);
                yield return Timing.WaitForSeconds(1f);
            }

            EventHandlers.Active079Extractions.Remove(player);
            if (player != null)
                player.DisableEffect(EffectType.Ensnared);

            if (card != null && card.Serial != 0)
            {
                EventHandlers.Scp079ExtractedSerials.Add(card.Serial);
                EventHandlers.PlayersWith079Data.Add(player);
                player.ShowHint("<color=green>✓ Данные SCP-079 успешно загружены на карту Повстанцев Хаоса!</color>", 6f);
            }
        }

        private void BreakExtract(Player player, string message)
        {
            if (player != null)
            {
                player.DisableEffect(EffectType.Ensnared);
                player.ShowHint($"<color=red>{message}</color>", 3f);
                EventHandlers.Active079Extractions.Remove(player);
            }
        }
    }
}