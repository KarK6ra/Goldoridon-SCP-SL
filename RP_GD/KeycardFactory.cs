using Exiled.API.Features.Items;
using Exiled.API.Features.Items.Keycards;
using Interactables.Interobjects.DoorUtils;
using InventorySystem.Items.Keycards;
using UnityEngine;

namespace EyeCatcherPlugin
{
    public static class KeycardFactory
    {
        public static void ApplyCustomSettings(this Keycard keycard, string itemName, Color mainColor, Color permissionsColor, DoorPermissionFlags flags)
        {
            if (keycard is CustomKeycardItem customCard)
            {
                customCard.ItemName = itemName;

                customCard.Color = mainColor;

                customCard.PermissionsColor = permissionsColor;

                Exiled.API.Enums.KeycardPermissions exiledPerms = (Exiled.API.Enums.KeycardPermissions)flags;

                keycard.Permissions = exiledPerms;
            }
        }
    }
}
