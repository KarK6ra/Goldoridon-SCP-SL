using System;
using System.Collections.Generic;
using System.Linq;
using CommandSystem;
using Exiled.API.Features;
using EyeCatcherPlugin.CustomSpawn;
using Exiled.CustomRoles.API.Features;
using PlayerRoles;
using UnityEngine;

namespace EyeCatcherPlugin
{
    [CommandHandler(typeof(RemoteAdminCommandHandler))]
    public class SpawnSecurityWaveCommand : ICommand
    {
        public string Command => "spawn_sb_wave";
        public string[] Aliases => new string[] { "sbwave", "десантсб" };
        public string Description => "Вызвать тактическую волну СБ на поверхность Зоны из мертвых игроков.";

        public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
        {
            if (!Round.InProgress)
            {
                response = "Раунд еще не начался!";
                return false;
            }

            List<Player> spectators = Player.List.Where(p => p.Role.Type == RoleTypeId.Spectator && !p.IsNPC).ToList();

            if (spectators.Count == 0)
            {
                response = "В наблюдателях нет игроков для формирования отряда!";
                return false;
            }

            spectators = spectators.OrderBy(x => UnityEngine.Random.value).ToList();

            int chiefCount = 0;
            int lieutenantCount = 0;
            int supervisorCount = 0;
            int gruntsCount = 0; int totalSpawned = 0;

            Vector3 mtfSpawnPos = new Vector3(175f, 13.4f, -40f);

            foreach (Player player in spectators)
            {
                if (totalSpawned >= 13) break;
                PlayerData data = PlayerDataStore.Get(player.UserId ?? "");
                if (data == null || !data.IsVerified) continue;

                uint roleToGive = 0;

                if (chiefCount == 0 && data.GetBranchHours("Security") >= 18f)
                {
                    roleToGive = 1920; chiefCount++;
                }
                else if (lieutenantCount == 0 && data.GetBranchHours("Security") >= 12f)
                {
                    roleToGive = 1921; lieutenantCount++;
                }
                else if (supervisorCount == 0 && data.GetBranchHours("Security") >= 8f)
                {
                    roleToGive = 1922; supervisorCount++;
                }
                else if (gruntsCount < 10)
                {
                    if (data.GetBranchHours("Security") >= 2f)
                    {
                        roleToGive = 1923;
                    }
                    else if (data.HoursPlayed >= 1f)
                    {
                        roleToGive = 1924;
                    }
                    else
                    {
                        continue;
                    }
                    gruntsCount++;
                }

                if (roleToGive != 0)
                {
                    CustomRole role = CustomRole.Get(roleToGive);
                    if (role != null)
                    {
                        role.AddRole(player);
                        MEC.Timing.CallDelayed(0.2f, () =>
{
    if (player != null && player.IsAlive)
        player.Position = mtfSpawnPos + new Vector3(UnityEngine.Random.Range(-2f, 2f), 0f, UnityEngine.Random.Range(-2f, 2f));
});
                        totalSpawned++;
                    }
                }
            }

            if (totalSpawned > 0)
            {
                Exiled.API.Features.Cassie.Message("🚨 ATTENTION ALL PERSONNEL . SECURITY FORCE HAS ENTERED THE FACILITY OUTSIDE 🚨", false, true, true);

                response = $"Успешно мобилизован тактический отряд СБ! Спавн: {totalSpawned} сотрудников на улице.\n" +
                           $"(Глава СБ: {chiefCount}, Лейтенант: {lieutenantCount}, Супервайзер: {supervisorCount}, Рядовые: {gruntsCount})";
                return true;
            }

            response = "Ни один из мертвых игроков не подошел по квалификации часов для спавна в СБ!";
            return false;
        }
    }
}
