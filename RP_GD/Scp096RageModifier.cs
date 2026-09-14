using System.Collections.Generic;
using Exiled.API.Features;
using Exiled.Events.EventArgs.Scp096;
using MEC;
using PlayerRoles;
using PlayerRoles.PlayableScps.Scp096;

namespace EyeCatcherPlugin
{
    public class Scp096RageModifier
    {
        private static CoroutineHandle _rageMonitorHandle;
        private static bool _isInternal = false;

        public static void Init()
        {
            Exiled.Events.Handlers.Scp096.AddingTarget += OnAddingTarget;
            Exiled.Events.Handlers.Scp096.Enraging += OnEnraging;
            Exiled.Events.Handlers.Server.RoundStarted += OnRoundStarted;
        }

        public static void Uninit()
        {
            Exiled.Events.Handlers.Scp096.AddingTarget -= OnAddingTarget;
            Exiled.Events.Handlers.Scp096.Enraging -= OnEnraging;
            Exiled.Events.Handlers.Server.RoundStarted -= OnRoundStarted;
            Timing.KillCoroutines(_rageMonitorHandle);
        }

        private static void OnRoundStarted()
        {
            Timing.KillCoroutines(_rageMonitorHandle);
            _rageMonitorHandle = Timing.RunCoroutine(MonitorScp096Rage());
        }

        private static void OnAddingTarget(AddingTargetEventArgs ev)
        {
            if (_isInternal) return;
            if (ev.Player == null || ev.Target == null) return;

            if (Plugin.Instance?.GlassesItem?.ActiveGlassesPlayers.Contains(ev.Target) == true)
            {
                ev.IsAllowed = false;
                return;
            }

            if (!ev.IsAllowed) return;

            Exiled.API.Features.Roles.Scp096Role role = ev.Player.Role as Exiled.API.Features.Roles.Scp096Role;
            if (role == null) return;

            if (role.RageState == Scp096RageState.Docile || role.RageState == Scp096RageState.Calming)
            {
                _isInternal = true;

                role.AddTarget(ev.Target, true);
                role.Enrage(25f);

                _isInternal = false;
            }
        }

        private static void OnEnraging(EnragingEventArgs ev)
        {
            if (_isInternal)
            {
                ev.IsAllowed = true;
            }
        }

        private static IEnumerator<float> MonitorScp096Rage()
        {
            while (true)
            {
                yield return Timing.WaitForSeconds(1f);

                foreach (Player player in Player.List)
                {
                    if (player == null || player.Role.Type != RoleTypeId.Scp096)
                        continue;

                    Exiled.API.Features.Roles.Scp096Role role = player.Role as Exiled.API.Features.Roles.Scp096Role;
                    if (role == null) continue;

                    if (role.RageState == Scp096RageState.Enraged || role.RageState == Scp096RageState.Distressed)
                    {
                        if (role.Targets != null && role.Targets.Count > 0)
                        {
                            role.EnragedTimeLeft = 9999f;
                            if (role.TotalEnrageTime < 9999f)
                                role.TotalEnrageTime = 9999f;
                        }
                        else
                        {
                            role.Calm(true);
                        }
                    }
                }
            }
        }
    }
}