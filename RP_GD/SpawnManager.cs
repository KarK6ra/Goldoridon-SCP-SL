using Exiled.API.Enums;
using Exiled.API.Features;
using Exiled.CustomRoles.API.Features;
using Exiled.Events.EventArgs.Server;
using EyeCatcherPlugin.Roles;
using MEC;
using PlayerRoles;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using EyeCatcherPlugin.Roles;

namespace EyeCatcherPlugin.CustomSpawn
{
    public static class SpawnManager
    {
        private static bool _isProcessing = false;
        private static bool _forceStartRequested = false;
        private static bool _scp181AssignedThisRound = false;
        private static CoroutineHandle _statusMonitorHandle;

        public static void Register()
        {
            RolePool.Build();
            PlayerDataStore.Init();

            Exiled.Events.Handlers.Server.WaitingForPlayers += OnWaitingForPlayers;
            Exiled.Events.Handlers.Server.RoundStarted += OnRoundStarted;
            Exiled.Events.Handlers.Server.RestartingRound += OnRestartingRound;
            Exiled.Events.Handlers.Player.Verified += OnPlayerVerified;
        }

        public static void Unregister()
        {
            Exiled.Events.Handlers.Server.WaitingForPlayers -= OnWaitingForPlayers;
            Exiled.Events.Handlers.Server.RoundStarted -= OnRoundStarted;
            Exiled.Events.Handlers.Server.RestartingRound -= OnRestartingRound;
            Exiled.Events.Handlers.Player.Verified -= OnPlayerVerified;
        }

        private static CoroutineHandle _botStatusUpdateHandle;

        private static void OnWaitingForPlayers()
        {
            Round.IsLocked = true;
            Round.IsLobbyLocked = true;
            _forceStartRequested = false;
            _scp181AssignedThisRound = false;
            _isProcessing = false;

            Log.Info("[SpawnManager] WaitingForPlayers → RoundLock + LobbyLock включены. Порог: " + RolePool.MinimumThreshold);

            Timing.RunCoroutine(MonitorLobby());

            Timing.KillCoroutines(_statusMonitorHandle);
            _statusMonitorHandle = Timing.RunCoroutine(UpdateServerStatusForBot());
        }

        private static IEnumerator<float> UpdateServerStatusForBot()
        {
            string sharedDir = Path.Combine(Paths.Configs, "EyeCatcherPlugin", "Shared");
            Directory.CreateDirectory(sharedDir);
            string statusPath = Path.Combine(sharedDir, "status.json");

            while (true)
            {
                try
                {
                    int currentPlayers = Player.List.Count(p => !p.IsNPC && p.IsConnected);

                    int maxPlayers = ((CustomNetworkManager)Mirror.NetworkManager.singleton).MaxPlayers;

                    var statusPayload = new
                    {
                        IsLobby = Round.IsLobby,
                        PlayersInLobby = currentPlayers,
                        MaxPlayers = maxPlayers,
                        MinimumThreshold = RolePool.MinimumThreshold,
                        RoundStarted = Round.InProgress,
                        UpdatedAt = DateTime.UtcNow
                    };

                    string json = Newtonsoft.Json.JsonConvert.SerializeObject(statusPayload, Newtonsoft.Json.Formatting.Indented);
                    File.WriteAllText(statusPath, json);
                }
                catch (Exception ex)
                {
                    Log.Error("[SpawnManager] Не удалось записать status.json для бота: " + ex.Message);
                }

                yield return Timing.WaitForSeconds(5f);
            }
        }

        public static void SendRoundStateToBot(string state, string message)
        {
            try
            {
                string cmdDir = Path.Combine(Paths.Configs, "EyeCatcherPlugin", "Shared", "Commands");
                Directory.CreateDirectory(cmdDir);
                string id = DateTime.UtcNow.Ticks.ToString();
                var payload = new
                {
                    Command = "round_notification",
                    Args = new Dictionary<string, string> { { "state", state }, { "message", message } }
                };
                File.WriteAllText(Path.Combine(cmdDir, "round_" + id + ".json"), Newtonsoft.Json.JsonConvert.SerializeObject(payload));
            }
            catch { }
        }


        private static IEnumerator<float> MonitorLobby()
        {
            while (Round.IsLobby)
            {
                int players = Player.List.Count(p => !p.IsNPC && p.IsConnected);
                int threshold = RolePool.MinimumThreshold;

                if (_forceStartRequested || players >= threshold)
                {
                    Log.Info("[SpawnManager] Условие старта выполнено (игроков: " + players + ", порог: " + threshold + "). Запускаем раунд... (Раунд лок удержан)");
                    Round.IsLobbyLocked = false;
                    Round.Start();
                    yield break;
                }


                string msg = "\n\n\n\n\n\n<color=#FFAA00>Ожидание персонала...</color>\n" +
             "Игроков: <color=white>" + players + "</color> / <color=yellow>" + threshold + "</color>";
                foreach (Player p in Player.List)
                {
                    if (p != null && p.IsConnected)
                        p.ShowHint(msg, 1.2f);
                }

                yield return Timing.WaitForSeconds(1f);
            }
        }


        private static void OnRoundStarted()
        {
            if (_isProcessing) return;
            _isProcessing = true;

            Timing.CallDelayed(0.5f, () =>
            {
                try
                {
                    AssignAllRoles();
                }
                catch (Exception ex)
                {
                    Log.Error("[SpawnManager] Ошибка раздачи ролей: " + ex);
                }
                finally
                {
                    _isProcessing = false;
                }
            });
        }

        private static void OnRestartingRound()
        {
            _scp181AssignedThisRound = false;
            _forceStartRequested = false;
            PlayerDataStore.SaveAll();
        }

        private static void OnPlayerVerified(Exiled.Events.EventArgs.Player.VerifiedEventArgs ev)
        {
            if (ev.Player == null) return;
            string steamId = ev.Player.UserId;
            if (string.IsNullOrEmpty(steamId)) return;

            PlayerData data = PlayerDataStore.Get(steamId);
            data.Nickname = ev.Player.Nickname;
            data.LastSeen = DateTime.UtcNow;
            PlayerDataStore.Save(data);
        }

        public static void ForceStart()
        {
            _forceStartRequested = true;
            Log.Info("[SpawnManager] Запрошен принудительный старт раунда.");
        }

        private static void AssignAllRoles()
        {
            List<Player> players = Player.List.Where(p => !p.IsNPC && p.IsConnected).ToList();
            if (players.Count == 0) return;

            Dictionary<Player, PlayerData> playerDatas = new Dictionary<Player, PlayerData>();
            foreach (Player p in players)
            {
                string sid = p.UserId ?? "";
                playerDatas[p] = PlayerDataStore.Get(sid);
            }

            List<SpawnRoleEntry> pool = new List<SpawnRoleEntry>(RolePool.AllPlayableRoles);
            Dictionary<string, int> used = new Dictionary<string, int>();
            HashSet<Player> finalAssigned = new HashSet<Player>();

            List<Player> verifiedPlayers = players.Where(p => playerDatas[p].IsVerified && playerDatas[p].Level >= 1)
    .OrderByDescending(p => playerDatas[p].Level)
    .ThenBy(x => UnityEngine.Random.value).ToList();

            List<Player> unverifiedPlayers = players.Where(p => !verifiedPlayers.Contains(p))
                .OrderBy(x => UnityEngine.Random.value).ToList();

            foreach (Player player in verifiedPlayers)
            {
                PlayerData data = playerDatas[player];
                SpawnRoleEntry chosen = ChooseBestRole(player, data, pool, used);

                if (chosen != null && chosen.Id != "ClassD")
                {
                    if (chosen.Kind == SpawnRoleKind.Vanilla) AssignVanilla(player, chosen.VanillaRole);
                    else AssignCustom(player, chosen.CustomRoleId);

                    if (!used.ContainsKey(chosen.Id)) used[chosen.Id] = 0;
                    used[chosen.Id]++;
                    finalAssigned.Add(player);
                }
            }

            int totalStaffSpawned = used.Where(kv => kv.Key != "ClassD").Sum(kv => kv.Value);

            if (totalStaffSpawned < RolePool.MinimumThreshold && unverifiedPlayers.Count > 0)
            {
                List<SpawnRoleEntry> freeStaffRoles = pool.Where(r => r.Id != "ClassD" && (!used.ContainsKey(r.Id) || used[r.Id] < r.MaxCount)).ToList();

                foreach (Player player in new List<Player>(unverifiedPlayers))
                {
                    if (totalStaffSpawned >= RolePool.MinimumThreshold || freeStaffRoles.Count == 0) break;

                    SpawnRoleEntry fallbackStaff = freeStaffRoles
.Where(r => r.Id != "SiteDirector" && r.Id != "EthicsInspector" && r.Id != "CentralCommand")
.OrderBy(x => UnityEngine.Random.value).FirstOrDefault();

                    if (fallbackStaff != null)
                    {
                        if (fallbackStaff.Kind == SpawnRoleKind.Vanilla) AssignVanilla(player, fallbackStaff.VanillaRole);
                        else AssignCustom(player, fallbackStaff.CustomRoleId);

                        if (!used.ContainsKey(fallbackStaff.Id)) used[fallbackStaff.Id] = 0;
                        used[fallbackStaff.Id]++;

                        totalStaffSpawned++;
                        finalAssigned.Add(player);
                        unverifiedPlayers.Remove(player);

                        if (used[fallbackStaff.Id] >= fallbackStaff.MaxCount) freeStaffRoles.Remove(fallbackStaff);
                    }
                }
            }

            List<Player> remainingToD = players.Where(p => !finalAssigned.Contains(p)).ToList();
            foreach (Player player in remainingToD)
            {
                AssignVanilla(player, RoleTypeId.ClassD);
            }

            TryAssignScp181();
            TryAssignChaosSpies(players, playerDatas);
        }


        private static void TryAssignChaosSpies(List<Player> players, Dictionary<Player, PlayerData> playerDatas)
        {
            int totalPlayers = players.Count;
            int spiesToAssign = 0;

            if (totalPlayers >= 45) spiesToAssign = 3;
            else if (totalPlayers >= 30) spiesToAssign = 2;
            else if (totalPlayers >= 15) spiesToAssign = 1;

            if (spiesToAssign == 0) return;

            List<Player> candidates = players.Where(p => p.IsHuman && p.IsAlive && playerDatas.ContainsKey(p) && playerDatas[p].WantAntagonist).ToList();

            if (candidates.Count < spiesToAssign)
            {
                var extraCandidates = players.Where(p => p.IsHuman && p.IsAlive && !candidates.Contains(p) && (p.Role.Type == RoleTypeId.ClassD || p.Role.Type == RoleTypeId.Scientist)).ToList();
                candidates.AddRange(extraCandidates.OrderBy(x => UnityEngine.Random.value).Take(spiesToAssign - candidates.Count));
            }

            List<Player> chosenSpies = candidates.OrderBy(x => UnityEngine.Random.value).Take(spiesToAssign).ToList();

            foreach (Player spy in chosenSpies)
            {
                spy.ShowHint("\n\n\n<color=red><b>?? ВЫ ЗАВЕРБОВАНЫ СЕКТОРОМ ПХ ??</b></color>\nВаша цель — саботировать работу комплекса и помочь Повстанцам!", 10f);
                spy.Broadcast(8, "<color=#00FF00>Вы тайно работаете на Повстанцев Хаоса. Уничтожьте персонал Зоны!</color>");
                Log.Info($"[Антагонисты] Игрок {spy.Nickname} назначен Шпионом ПХ.");
            }
        }


        private static SpawnRoleEntry ChooseBestRole(Player player, PlayerData data, List<SpawnRoleEntry> pool, Dictionary<string, int> used)
        {
            if (data.RolePriorities != null && data.RolePriorities.Count > 0)
            {
                var sortedPriorities = data.RolePriorities.OrderByDescending(x => x.Value).ToList();

                foreach (var kv in sortedPriorities)
                {
                    string roleKey = kv.Key;
                    SpawnRoleEntry entry = pool.FirstOrDefault(r => r.Id == roleKey);
                    if (entry == null) continue;

                    int already = used.ContainsKey(entry.Id) ? used[entry.Id] : 0;
                    if (already >= entry.MaxCount) continue;

                    if (CheckRoleHoursRequirement(entry.Id, data))
                    {
                        return entry;
                    }
                }
            }

            List<SpawnRoleEntry> freeAndAllowed = new List<SpawnRoleEntry>();
            foreach (var r in pool)
            {
                int already = used.ContainsKey(r.Id) ? used[r.Id] : 0;
                if (already < r.MaxCount && CheckRoleHoursRequirement(r.Id, data))
                {
                    freeAndAllowed.Add(r);
                }
            }

            if (freeAndAllowed.Count == 0) return null;

            freeAndAllowed = freeAndAllowed.OrderBy(x => UnityEngine.Random.value).ToList();

            SpawnRoleEntry nonD = null;
            foreach (var r in freeAndAllowed)
            {
                if (r.Id != "ClassD")
                {
                    nonD = r;
                    break;
                }
            }

            return nonD != null ? nonD : freeAndAllowed[0];
        }

        private static bool CheckRoleHoursRequirement(string roleId, PlayerData data)
        {
            if (roleId == "ClassD") return true;

            if (Round.IsLobby || !Round.InProgress)
            {
                if (roleId == "SecurityChief" || roleId == "SecurityLieutenant" ||
                    roleId == "Supervisor" || roleId == "SecurityOfficer" || roleId == "SecurityCadet")
                {
                    return false;
                }
            }

            if (roleId == "SiteDirector" || roleId == "EthicsInspector" || roleId == "CentralCommand")
            {
                return data.AdminWhitelist;
            }

            if (roleId == "JuniorScientist") return data.HoursPlayed >= 1f;
            if (roleId == "ZoneScientist") return data.HoursPlayed >= 4f && data.GetBranchHours("Science") >= 2f;
            if (roleId == "ProjectLead") return data.GetBranchHours("Science") >= 10f;
            if (roleId == "ResearchDirector") return data.GetBranchHours("Science") >= 18f;
            if (roleId == "MaintenanceTech") return data.HoursPlayed >= 2f;
            if (roleId == "ChiefEngineer") return data.GetBranchHours("Engineering") >= 6f;

            if (roleId == "Intern") return data.HoursPlayed >= 1f;
            if (roleId == "Orderly") return data.GetBranchHours("Medical") >= 1f; if (roleId == "Therapist") return data.GetBranchHours("Medical") >= 4f; if (roleId == "ChiefPhysician") return data.GetBranchHours("Medical") >= 6f;

            if (roleId == "CentralCommand")
            {
                return data.GetBranchHours("Science") >= 2f &&
                       data.GetBranchHours("Security") >= 2f &&
                       data.GetBranchHours("Engineering") >= 2f &&
                       data.GetBranchHours("Medical") >= 4f;
            }

            return false;
        }



        private static void AssignVanilla(Player player, RoleTypeId role)
        {
            player.Role.Set(role, SpawnReason.ForceClass, RoleSpawnFlags.All);
        }

        private static void AssignCustom(Player player, uint customRoleId)
        {
            CustomRole role = CustomRole.Get(customRoleId);
            if (role != null)
            {
                role.AddRole(player);
            }
            else
            {
                Log.Warn("[SpawnManager] CustomRole id=" + customRoleId + " не найден, выдаём Scientist");
                AssignVanilla(player, RoleTypeId.Scientist);
            }
        }

        private static void TryAssignScp181()
        {
            if (_scp181AssignedThisRound) return;

            List<Player> classDs = Player.List
                .Where(p => p.Role.Type == RoleTypeId.ClassD && !p.IsNPC)
                .ToList();

            if (classDs.Count == 0) return;

            if (UnityEngine.Random.Range(0, 100) < 35)
            {
                Player lucky = classDs[UnityEngine.Random.Range(0, classDs.Count)];
                Scp181Role.ActiveLuckyPlayers.Add((ushort)lucky.Id);
                lucky.CustomName = "SCP-181";
                lucky.ShowHint("<color=yellow><b>Вы появились как SCP-181 «Везунчик»</b></color>", 5f);
                _scp181AssignedThisRound = true;
            }
        }
    }
}