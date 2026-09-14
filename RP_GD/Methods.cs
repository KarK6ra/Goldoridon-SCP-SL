using System.Collections.Generic;

using Exiled.API.Features;
using Exiled.API.Features.Roles;
using Exiled.API.Features.Toys;
using System.Collections.Generic;
using MEC;
using UnityEngine;

using MEC;

using Mirror;

using PlayerRoles;

using UnityEngine;

namespace EyeCatcherPlugin
{
    public static class Methods
    {
        public static Dictionary<Player, GameObject> Scp96Censors { get; }
            = new Dictionary<Player, GameObject>();

        public static Dictionary<Player, HashSet<CoroutineHandle>> Coroutines { get; }
            = new Dictionary<Player, HashSet<CoroutineHandle>>();

        private static readonly Dictionary<Player, CoroutineHandle> _censorAnimations =
    new Dictionary<Player, CoroutineHandle>();

        private static readonly Dictionary<Player, CoroutineHandle> _glitchAnimations =
            new Dictionary<Player, CoroutineHandle>();

        public static void AddCensor(Player player)
        {
            if (player == null)
                return;

            if (player.Role.Type != RoleTypeId.Scp096)
                return;

            Transform head;

            if (!TryGetScp096Head(player, out head))
            {
                Log.Error(
                    $"[EyeCatcher] Не удалось найти голову SCP-096: {player.Nickname}");

                return;
            }

            if (Scp96Censors.ContainsKey(player))
                RemoveCensor(player);

            Config config = Plugin.Instance.Config;

            Primitive censor = Primitive.Create(
                primitiveType: PrimitiveType.Cube,
                position: head.position,
                rotation: head.rotation.eulerAngles,
                scale: config.CubeScale,
                spawn: true,
                color: Color.black
            );

            censor.Collidable = false;
            censor.MovementSmoothing = 0;

            censor.Transform.SetParent(
                player.Transform,
                true);

            Scp96Censors.Add(
                player,
                censor.GameObject);

            if (!Coroutines.ContainsKey(player))
                Coroutines[player] = new HashSet<CoroutineHandle>();

            CoroutineHandle trackCoroutine = Timing.RunCoroutine(
    TrackHead(
        censor.Transform,
        head));

            Coroutines[player].Add(trackCoroutine);

            CoroutineHandle shakeCoroutine = Timing.RunCoroutine(
    AnimateCensor(
        player,
        censor.GameObject));

            Coroutines[player].Add(shakeCoroutine);

            CoroutineHandle glitchCoroutine = Timing.RunCoroutine(
    SpawnGlitchCubes(
        player,
        censor.GameObject));

            Coroutines[player].Add(glitchCoroutine);

            HideForNonGlassesPlayers(
                censor.GameObject);

            Log.Debug(
                $"[EyeCatcher] Created black censor for {player.Nickname}.");
        }

        private static IEnumerator<float> AnimateCensor(Player player, GameObject censor)
        {
            Vector3 originalPosition = censor.transform.localPosition;
            float time = 0f;

            while (censor != null && Scp96Censors.ContainsKey(player))
            {
                time += Time.deltaTime;

                float shakeX = Mathf.Sin(time * 31f) * 0.012f;
                float shakeY = Mathf.Sin(time * 37f) * 0.009f;
                float shakeZ = Mathf.Sin(time * 43f) * 0.006f;

                censor.transform.localPosition =
                    originalPosition + new Vector3(shakeX, shakeY, shakeZ);

                yield return Timing.WaitForOneFrame;
            }

            if (censor != null)
                censor.transform.localPosition = originalPosition;
        }

        private static IEnumerator<float> SpawnGlitchCubes(Player player, GameObject censor)
        {
            while (censor != null && Scp96Censors.ContainsKey(player))
            {
                yield return Timing.WaitForSeconds(UnityEngine.Random.Range(0.15f, 0.45f));

                if (censor == null || !Scp96Censors.ContainsKey(player))
                    yield break;

                CreateGlitchCube(player, censor);

                yield return Timing.WaitForSeconds(
                    UnityEngine.Random.Range(0.5f, 1.0f));
            }
        }

        private static void CreateGlitchCube(Player player, GameObject censor)
        {
            if (censor == null)
                return;

            Primitive glitch = Primitive.Create(
                primitiveType: PrimitiveType.Cube,
                position: censor.transform.position,
                rotation: censor.transform.rotation.eulerAngles,
                scale: new Vector3(0.05f, 0.05f, 0.05f),
                spawn: true,
                color: Color.white
            );

            if (glitch == null)
                return;

            GameObject glitchObject = glitch.GameObject;

            glitchObject.transform.SetParent(censor.transform, false);

            float size = UnityEngine.Random.Range(0.07f, 0.14f);

            glitchObject.transform.localScale = new Vector3(
                size,
                size,
                size);

            glitchObject.transform.localPosition = GetGlitchPosition();

            glitchObject.transform.localRotation = Quaternion.Euler(
                UnityEngine.Random.Range(-15f, 15f),
                UnityEngine.Random.Range(-15f, 15f),
                UnityEngine.Random.Range(-15f, 15f));

            glitch.Collidable = false;
            glitch.MovementSmoothing = 0;

            HideGlitchFromNonGlassesPlayers(glitchObject);

            Timing.RunCoroutine(
                DestroyGlitchCube(glitchObject));
        }

        private static Vector3 GetGlitchPosition()
        {
            int side = UnityEngine.Random.Range(0, 6);

            float inside = UnityEngine.Random.Range(-0.18f, 0.18f);
            float surface = 0.48f;

            switch (side)
            {
                case 0:
                    return new Vector3(surface, inside, inside);

                case 1:
                    return new Vector3(-surface, inside, inside);

                case 2:
                    return new Vector3(inside, surface, inside);

                case 3:
                    return new Vector3(inside, -surface, inside);

                case 4:
                    return new Vector3(inside, inside, surface);

                default:
                    return new Vector3(inside, inside, -surface);
            }
        }

        private static IEnumerator<float> DestroyGlitchCube(GameObject glitch)
        {
            yield return Timing.WaitForSeconds(
                UnityEngine.Random.Range(0.5f, 1.0f));

            if (glitch != null)
                NetworkServer.Destroy(glitch);
        }

        private static void HideGlitchFromNonGlassesPlayers(GameObject glitch)
        {
            if (glitch == null)
                return;

            HashSet<Player> activePlayers =
                Plugin.Instance.GlassesItem.ActiveGlassesPlayers;

            foreach (Player target in Player.List)
            {
                if (target == null)
                    continue;

                if (activePlayers.Contains(target))
                    continue;

                target.HideNetworkObject(glitch);
            }
        }

        public static void RemoveCensor(Player player)
        {
            if (player == null)
                return;

            if (!Scp96Censors.TryGetValue(
                    player,
                    out GameObject censor))
                return;

            Scp96Censors.Remove(player);

            if (Coroutines.TryGetValue(
                    player,
                    out HashSet<CoroutineHandle> handles))
            {
                foreach (CoroutineHandle handle in handles)
                    Timing.KillCoroutines(handle);

                Coroutines.Remove(player);
            }

            if (censor != null)
                NetworkServer.Destroy(censor);

            Log.Debug(
                $"[EyeCatcher] Removed censor from {player.Nickname}.");
        }

        public static void ClearAllCensors()
        {
            foreach (HashSet<CoroutineHandle> handles in Coroutines.Values)
            {
                foreach (CoroutineHandle handle in handles)
                    Timing.KillCoroutines(handle);
            }

            Coroutines.Clear();

            foreach (GameObject censor in Scp96Censors.Values)
            {
                if (censor != null)
                    NetworkServer.Destroy(censor);
            }

            Scp96Censors.Clear();
        }

        private static void HideForNonGlassesPlayers(
            GameObject censor)
        {
            if (censor == null)
                return;

            HashSet<Player> activePlayers =
                Plugin.Instance.GlassesItem.ActiveGlassesPlayers;

            foreach (Player player in Player.List)
            {
                if (player == null)
                    continue;

                if (activePlayers.Contains(player))
                    continue;

                if (player.Role is SpectatorRole spectator)
                {
                    if (spectator.SpectatedPlayer != null &&
                        activePlayers.Contains(
                            spectator.SpectatedPlayer))
                    {
                        Plugin.Instance.EventHandlers.DirtyPlayers.Add(player);

                        continue;
                    }
                }

                player.HideNetworkObject(censor);
            }
        }

        public static void ObfuscateScp96s(Player player)
        {
            if (player == null)
                return;

            foreach (GameObject censor in Scp96Censors.Values)
            {
                if (censor != null)
                    player.ShowHidedNetworkObject(censor);
            }
        }

        public static void DeObfuscateScp96s(Player player)
        {
            if (player == null)
                return;

            foreach (GameObject censor in Scp96Censors.Values)
            {
                if (censor != null)
                    player.HideNetworkObject(censor);
            }
        }

        private static IEnumerator<float> TrackHead(
            Transform censor,
            Transform head)
        {
            while (censor != null && head != null)
            {
                censor.position = head.position;
                censor.rotation = head.rotation;

                yield return Timing.WaitForOneFrame;
            }
        }

        private static bool TryGetScp096Head(
            Player player,
            out Transform head)
        {
            head = null;

            if (player == null)
                return false;

            Scp096Role scp096Role = player.Role as Scp096Role;
            if (scp096Role == null)
                return false;

            head = scp096Role.HeadTransform;

            return head != null;
        }
    }
}