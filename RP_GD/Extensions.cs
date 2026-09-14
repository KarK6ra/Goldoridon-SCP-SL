using Exiled.API.Features;

using Mirror;

using UnityEngine;

namespace EyeCatcherPlugin
{
    public static class Extensions
    {
        public static void ShowHidedNetworkObject(
            this Player player,
            GameObject networkedObject)
        {
            if (player == null ||
                player.Connection == null ||
                networkedObject == null)
                return;

            if (!networkedObject.TryGetComponent(
                    out NetworkIdentity identity))
            {
                Log.Warn(
                    $"[EyeCatcher] {networkedObject} does not have NetworkIdentity.");
                return;
            }

            Exiled.API.Extensions.MirrorExtensions.SendSpawnMessageMethodInfo.Invoke(
    null,
    new object[] { identity, player.Connection });
        }

        public static void HideNetworkObject(
            this Player player,
            GameObject networkedObject)
        {
            if (player == null ||
                player.Connection == null ||
                networkedObject == null)
                return;

            if (!networkedObject.TryGetComponent(
                    out NetworkIdentity identity))
            {
                Log.Warn(
                    $"[EyeCatcher] {networkedObject} does not have NetworkIdentity.");
                return;
            }

            player.Connection.Send(
                new ObjectHideMessage
                {
                    netId = identity.netId
                });
        }
    }
}