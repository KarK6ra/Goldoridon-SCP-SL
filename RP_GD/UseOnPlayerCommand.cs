using System;
using System.Collections.Generic;
using CommandSystem;
using Exiled.API.Features;
using MEC;
using PlayerRoles;
using UnityEngine;

namespace EyeCatcherPlugin
{
    [CommandHandler(typeof(ClientCommandHandler))]
    public class UseOnPlayerCommand : ICommand
    {
        public string Command => "use";
        public string[] Aliases => new string[] { "использовать" };
        public string Description => "Использовать аптечку/медицину из рук на игрока перед вами.";

        private static readonly HashSet<ItemType> MedicalWhiteList = new HashSet<ItemType>
        {
            ItemType.Medkit,
            ItemType.Painkillers,
            ItemType.SCP500,
            ItemType.Adrenaline
        };

        public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
        {
            Player player = Player.Get(sender);
            if (player == null || !player.IsHuman)
            {
                response = "Команда доступна только живым людям!";
                return false;
            }

            if (player.CurrentItem == null || !MedicalWhiteList.Contains(player.CurrentItem.Type))
            {
                response = "Вы должны держать в руках медицину (Аптечка, Обезболивающее, SCP-500, Адреналин)!";
                return false;
            }

            if (Physics.SphereCast(player.CameraTransform.position, 0.4f, player.CameraTransform.forward, out RaycastHit hit, 3.5f))
            {
                Player target = Player.Get(hit.collider);
                if (target != null && target.IsHuman && target != player)
                {
                    ItemType medicalType = player.CurrentItem.Type;

                    player.RemoveItem(player.CurrentItem);

                    Timing.CallDelayed(0.05f, () =>
{
    if (target != null && target.IsAlive)
    {
        var newItem = target.AddItem(medicalType);

        Timing.CallDelayed(0.05f, () =>
        {
            if (target != null && target.IsAlive)
            {
                target.CurrentItem = newItem;

                if (newItem is Exiled.API.Features.Items.Usable usableItem)
                {
                    usableItem.IsUsing = true;
                }
            }
        });
    }
});

                    response = "Вы применили медицинский предмет на человека перед вами!";
                    return true;
                }
            }

            response = "Подойдите ближе и посмотрите в упор на человека, чтобы вылечить его!";
            return false;
        }
    }
}
