using System.Collections.Generic;
using Exiled.API.Enums;
using Exiled.API.Features;
using Exiled.Events.EventArgs.Player;
using PlayerRoles;
using UnityEngine;
using MEC;

namespace EyeCatcherPlugin
{
    public static class Scp181Role
    {
        public static HashSet<ushort> ActiveLuckyPlayers { get; } = new HashSet<ushort>();
        private static readonly HashSet<ushort> _recentlyRespawned = new HashSet<ushort>();

        public static void RegisterEvents()
        {
            Exiled.Events.Handlers.Player.Spawned += OnSpawned;
            Exiled.Events.Handlers.Player.TriggeringTesla += OnTriggeringTesla;
            Exiled.Events.Handlers.Player.Hurting += OnHurting;
            Exiled.Events.Handlers.Player.InteractingDoor += OnInteractingDoor;
            Exiled.Events.Handlers.Player.Dying += OnDying;
        }

        public static void UnregisterEvents()
        {
            Exiled.Events.Handlers.Player.Spawned -= OnSpawned;
            Exiled.Events.Handlers.Player.TriggeringTesla -= OnTriggeringTesla;
            Exiled.Events.Handlers.Player.Hurting -= OnHurting;
            Exiled.Events.Handlers.Player.InteractingDoor -= OnInteractingDoor;
            Exiled.Events.Handlers.Player.Dying -= OnDying;

            ClearRoundData();
        }

        public static void ClearRoundData()
        {
            ActiveLuckyPlayers.Clear();
            _recentlyRespawned.Clear();
        }

        private static void OnSpawned(SpawnedEventArgs ev)
        {
            if (ev.Player == null) return;
            ushort id = (ushort)ev.Player.Id;

            if (ev.Player.Role.Type == RoleTypeId.ClassD)
            {
                if (_recentlyRespawned.Contains(id))
                {
                    _recentlyRespawned.Remove(id);
                    ActiveLuckyPlayers.Add(id);
                    ev.Player.CustomName = "SCP-181";

                    ev.Player.ShowHint("<color=green><b>SCP-181 «Везунчик»</b></color>\nВы невероятным образом выжили!", 5f);
                    return;
                }

                if (Random.Range(0, 100) < 35)
                {
                    ActiveLuckyPlayers.Add(id);
                    ev.Player.CustomName = "SCP-181";

                    ev.Player.ShowHint("<color=yellow><b>Вы появились как SCP-181 «Везунчик»</b></color>", 5f);
                }
            }
            else
            {
                ActiveLuckyPlayers.Remove(id);
                if (ev.Player.CustomName == "SCP-181") ev.Player.CustomName = null;
            }
        }

        private static void OnTriggeringTesla(TriggeringTeslaEventArgs ev)
        {
            if (ev.Player == null || !ActiveLuckyPlayers.Contains((ushort)ev.Player.Id)) return;

            if (Random.Range(0, 100) >= 70)
            {
                ev.IsAllowed = false;
            }
        }

        private static void OnHurting(HurtingEventArgs ev)
        {
            if (ev.Player == null) return;
            ushort id = (ushort)ev.Player.Id;
            if (!ActiveLuckyPlayers.Contains(id)) return;

            if (Random.Range(0, 100) < 30)
            {
                ev.Amount = 0f;
                ev.IsAllowed = false; return;
            }

            if (Random.Range(0, 100) < 10)
            {
                if (!ev.Player.GetEffect(EffectType.Invisible).IsEnabled)
                {
                    ev.Player.EnableEffect(EffectType.Invisible, 30f);
                }
            }
        }

        private static void OnInteractingDoor(InteractingDoorEventArgs ev)
        {
            if (ev.Player == null || ev.Door == null) return;
            if (!ActiveLuckyPlayers.Contains((ushort)ev.Player.Id)) return;

            if (!ev.IsAllowed)
            {
                if (ev.Door.IsLocked) return;

                if (Random.Range(0, 100) < 50)
                {
                    ev.IsAllowed = true;
                }
            }
        }

        private static void OnDying(DyingEventArgs ev)
        {
            if (ev.Player == null) return;
            ushort id = (ushort)ev.Player.Id;

            if (ActiveLuckyPlayers.Contains(id))
            {
                ActiveLuckyPlayers.Remove(id);
                if (ev.Player.CustomName == "SCP-181") ev.Player.CustomName = null;

                if (Random.Range(0, 100) < 1)
                {
                    Vector3 deathPosition = ev.Player.Position;
                    _recentlyRespawned.Add(id);

                    Timing.CallDelayed(0.1f, () =>
                    {
                        if (ev.Player != null)
                        {
                            ev.Player.Role.Set(RoleTypeId.ClassD, SpawnReason.ForceClass);
                            Timing.CallDelayed(0.1f, () =>
                            {
                                ev.Player.Position = deathPosition;
                            });
                        }
                        else
                        {
                            _recentlyRespawned.Remove(id);
                        }
                    });
                }
            }
        }
    }
}
