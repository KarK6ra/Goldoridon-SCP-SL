using Exiled.API.Enums;
using Exiled.API.Features;
using Exiled.Events.EventArgs.Map;
using Exiled.Events.EventArgs.Player;
using Exiled.Events.EventArgs.Scp914;
using EyeCatcherPlugin.Roles;
using LightContainmentZoneDecontamination;
using MEC;
using PlayerRoles;
using PlayerStatsSystem;
using Scp914;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using static EyeCatcherPlugin.Methods;
using static RoundSummary;
using PlayerEvent = Exiled.Events.Handlers.Player;
using ServerEvent = Exiled.Events.Handlers.Server;


namespace EyeCatcherPlugin
{
    public class EventHandlers
    {
        public HashSet<Player> DirtyPlayers { get; } = new HashSet<Player>();
        private bool _isOusSpawnedThisRound = false;
        public static HashSet<ushort> RottedPlayers { get; } = new HashSet<ushort>();

        private CoroutineHandle _scp035AuraHandle;
        private CoroutineHandle _hoursTrackerHandle;
        private CoroutineHandle _cmdListenerHandle;
        public static Dictionary<Player, float> MeleeCooldowns { get; } = new Dictionary<Player, float>();


        private static readonly HashSet<ItemType> HeavyRifles = new HashSet<ItemType>
{
    ItemType.GunE11SR,    ItemType.GunShotgun,    ItemType.GunAK,    ItemType.GunLogicer,    ItemType.GunFRMG0,    ItemType.SCP1509
};


        private static readonly HashSet<ItemType> MachineGuns = new HashSet<ItemType>
{
    ItemType.GunLogicer,
    ItemType.GunFRMG0
};

        public static Dictionary<ushort, CoroutineHandle> BleedingPlayers { get; } = new Dictionary<ushort, CoroutineHandle>();
        public static HashSet<ushort> UpgradedOnFine { get; } = new HashSet<ushort>();
        public static Dictionary<ushort, CoroutineHandle> VeryFineTimers { get; } = new Dictionary<ushort, CoroutineHandle>();
        public static HashSet<ushort> Scp079ExtractedSerials { get; } = new HashSet<ushort>();
        public static HashSet<Player> PlayersWith079Data { get; } = new HashSet<Player>();
        public static readonly Dictionary<Player, CoroutineHandle> Active079Extractions = new Dictionary<Player, CoroutineHandle>();
        private CoroutineHandle _roundTimerHandle;
        public static int RoundNumber { get; set; } = 0;
        public static HashSet<string> OnlineVerifiedAdmins { get; } = new HashSet<string>();



        public void Subscribe()
        {
            ServerEvent.WaitingForPlayers += OnWaitingForPlayers;

            PlayerEvent.Verified += OnVerified;
            PlayerEvent.Spawned += OnSpawned;

            PlayerEvent.SearchingPickup += OnSearchingPickup;
            PlayerEvent.ItemAdded += OnItemAdded;
            Exiled.Events.Handlers.Scp914.UpgradingPlayer += OnUpgradingPlayer;
            Exiled.Events.Handlers.Warhead.Detonated += OnWarheadDetonated;
            PlayerEvent.TriggeringTesla += OnTriggeringTesla;
            PlayerEvent.UsedItem += OnUsedItem;
            Exiled.Events.Handlers.Server.RoundEnded += OnRoundEnded;

            PlayerEvent.ChangingSpectatedPlayer += OnChangingSpectatedPlayer;
            Exiled.Events.Handlers.Player.ChangingItem += OnChangingItem;
            PlayerEvent.Escaping += OnEscaping;
            Exiled.Events.Handlers.Player.Hurting += OnHurting;
            Exiled.Events.Handlers.Scp049.Attacking += On049Attacking;
            Exiled.Events.Handlers.Scp049.FinishingRecall += On049FinishingRecall;
            ServerEvent.RoundStarted += OnRoundStarted;
            PlayerEvent.UsedItem += OnUsedItem;
            Exiled.Events.Handlers.Player.InteractingDoor += OnInteractingDoor;

            Exiled.Events.Handlers.Scp079.Pinging += OnPinging;
            Exiled.Events.Handlers.Player.ChangingRole += OnChangingRole;
            Exiled.Events.Handlers.Player.Dying += OnDying;
            Exiled.Events.Handlers.Player.Left += OnLeft;
            PlayerEvent.DroppingItem += OnDroppingItem079;
            PlayerEvent.ItemAdded += OnItemAdded079;

            _scp035AuraHandle = Timing.RunCoroutine(Scp035AuraTick());
        }

        public void Unsubscribe()
        {
            ServerEvent.WaitingForPlayers -= OnWaitingForPlayers;

            PlayerEvent.Verified -= OnVerified;
            PlayerEvent.Spawned -= OnSpawned;
            Exiled.Events.Handlers.Player.Hurting -= OnHurting;
            Exiled.Events.Handlers.Player.ChangingItem -= OnChangingItem;
            PlayerEvent.ChangingSpectatedPlayer -= OnChangingSpectatedPlayer;
            Exiled.Events.Handlers.Scp049.Attacking -= On049Attacking;
            PlayerEvent.UsedItem -= OnUsedItem;
            Exiled.Events.Handlers.Scp049.FinishingRecall -= On049FinishingRecall;
            PlayerEvent.Escaping -= OnEscaping;
            Exiled.Events.Handlers.Player.InteractingDoor -= OnInteractingDoor;

            PlayerEvent.SearchingPickup -= OnSearchingPickup;
            Exiled.Events.Handlers.Warhead.Detonated -= OnWarheadDetonated;
            PlayerEvent.ItemAdded -= OnItemAdded;
            Exiled.Events.Handlers.Scp914.UpgradingPlayer -= OnUpgradingPlayer;
            PlayerEvent.TriggeringTesla -= OnTriggeringTesla;
            Exiled.Events.Handlers.Server.RoundEnded -= OnRoundEnded;
            Timing.KillCoroutines(_roundTimerHandle);


            foreach (var handle in BleedingPlayers.Values) Timing.KillCoroutines(handle);
            BleedingPlayers.Clear();

            Exiled.Events.Handlers.Player.ChangingRole -= OnChangingRole;
            Exiled.Events.Handlers.Player.Dying -= OnDying;
            Exiled.Events.Handlers.Player.Left -= OnLeft;
            Exiled.Events.Handlers.Scp079.Pinging -= OnPinging;
            ServerEvent.RoundStarted -= OnRoundStarted;
            PlayerEvent.DroppingItem -= OnDroppingItem079;
            PlayerEvent.ItemAdded -= OnItemAdded079;
            MEC.Timing.KillCoroutines(_hoursTrackerHandle);
            MEC.Timing.KillCoroutines(_cmdListenerHandle);


            Timing.KillCoroutines(_scp035AuraHandle);

        }

        private void OnInteractingDoor(InteractingDoorEventArgs ev)
        {
            if (ev.Player == null || ev.Door == null) return;

            if (ev.Door.Type == DoorType.Scp049Gate ||
    ev.Door.Type == DoorType.Scp173Gate ||
    ev.Door.Type == DoorType.Scp173NewGate)
            {
                if (ev.Player.CurrentItem == null || !ev.Player.CurrentItem.Type.ToString().Contains("Keycard"))
                {
                    ev.IsAllowed = false;
                }
                return;
            }

            if (ev.Door.Type == DoorType.PrisonDoor || ev.Door.Type == DoorType.GR18Gate || ev.Door.Type == DoorType.GR18Inner || ev.Door.Name.Contains("GR18") ||
ev.Door.Name.Contains("PRISON") ||
(ev.Door.Room != null && (ev.Door.Room.Type == RoomType.LczClassDSpawn ||
                  ev.Door.Room.Type == RoomType.LczGlassBox)))
            {
                if (ev.Player.CurrentItem == null || !ev.Player.CurrentItem.Type.ToString().Contains("Keycard"))
                {
                    ev.IsAllowed = false;
                }
            }
        }

        private void OnDroppingItem079(DroppingItemEventArgs ev)
        {
            if (ev.Player == null || ev.Item == null) return;
            if (!Scp079ExtractedSerials.Contains(ev.Item.Serial)) return;

            bool stillHas = false;
            foreach (var item in ev.Player.Items)
            {
                if (item.Serial != ev.Item.Serial && Scp079ExtractedSerials.Contains(item.Serial))
                {
                    stillHas = true;
                    break;
                }
            }

            if (!stillHas)
                PlayersWith079Data.Remove(ev.Player);
        }

        private void OnItemAdded079(ItemAddedEventArgs ev)
        {
            if (ev.Player == null || ev.Item == null) return;
            if (!Scp079ExtractedSerials.Contains(ev.Item.Serial)) return;

            PlayersWith079Data.Add(ev.Player);
        }

        private void OnUsedItem(UsedItemEventArgs ev)
        {
            if (ev.Player == null) return;

            if (ev.Item.Type == ItemType.Medkit || ev.Item.Type == ItemType.SCP500)
            {
                ushort playerId = (ushort)ev.Player.Id;
                if (BleedingPlayers.TryGetValue(playerId, out CoroutineHandle handle))
                {
                    Timing.KillCoroutines(handle);
                    BleedingPlayers.Remove(playerId);
                    ev.Player.ShowHint("<color=green>Кровотечение успешно остановлено!</color>", 4f);
                }
            }
        }

        private void OnWarheadDetonated()
        {
            Room surfaceNukeRoom = Room.List.FirstOrDefault(r => r.Type == RoomType.Surface);
            Vector3 escapePos = surfaceNukeRoom != null ? surfaceNukeRoom.Position + Vector3.up * 1.5f : new Vector3(175f, 13.4f, -40f);

            foreach (Player p in Player.List.Where(p => p.Role.Side == Side.Scp && p.IsAlive).ToList())
            {
                if (p.Role.Type == RoleTypeId.Scp106)
                {
                    p.Position = escapePos;
                    p.ShowHint("<color=orange>Вы ушли в подпространство во время взрыва и спаслись на поверхности!</color>", 5f);
                    continue;
                }
                if (p.Role.Type == RoleTypeId.Scp096 && p.Role is Exiled.API.Features.Roles.Scp096Role r096 && r096.Targets.Count > 0)
                {
                    p.Position = escapePos;
                    p.ShowHint("<color=red>Ярость позволила вам пережить взрыв! Вы перенесены на поверхность.</color>", 5f);
                    continue;
                }
                p.Kill("Погиб при взрыве Альфа-Боеголовки");
            }
        }

        private void OnRoundStarted()
        {
            if (DecontaminationController.Singleton != null)
            {
                DecontaminationController.Singleton.RoundStartTime = double.MaxValue;
            }
            MEC.Timing.KillCoroutines(_hoursTrackerHandle);
            _hoursTrackerHandle = MEC.Timing.RunCoroutine(TrackBranchHoursTick());

            RoundNumber++;
            CustomSpawn.SpawnManager.SendRoundStateToBot("started", $"Раунд номер {RoundNumber} начался!");

            Timing.KillCoroutines(_roundTimerHandle);
            _roundTimerHandle = Timing.RunCoroutine(AutoRoundEnderTimer());
        }

        private IEnumerator<float> AutoRoundEnderTimer()
        {
            yield return Timing.WaitForSeconds(3600f);
            if (Round.InProgress)
            {
                if (OnlineVerifiedAdmins.Count == 0)
                {
                    Log.Info("[Временной Лимит] Прошел 1 час. Верифицированные администраторы отсутствуют в игре. Раунд завершается в ничью.");
                    Round.EndRound(true);
                }
                else
                {
                    Log.Info($"[Временной Лимит] Прошел 1 час. На сервере есть админы ({OnlineVerifiedAdmins.Count} онлайн). Раунд продолжается.");
                }
            }
        }

        private void OnRoundEnded(Exiled.Events.EventArgs.Server.RoundEndedEventArgs ev)
        {
            Timing.KillCoroutines(_roundTimerHandle);

            string winner = ev.LeadingTeam.ToString();

            if ((int)ev.LeadingTeam == 0 || winner.Equals("Draw", StringComparison.OrdinalIgnoreCase))
            {
                winner = "Ничья";
            }
            else if (winner.Equals("Anomalies", StringComparison.OrdinalIgnoreCase) || winner.Equals("Scp", StringComparison.OrdinalIgnoreCase))
            {
                winner = "SCP";
            }
            else if (winner.Equals("FacilityForces", StringComparison.OrdinalIgnoreCase) || winner.Equals("Mtf", StringComparison.OrdinalIgnoreCase))
            {
                winner = "МОГ/СБ";
            }
            else if (winner.Equals("ChaosInsurgency", StringComparison.OrdinalIgnoreCase))
            {
                winner = "Повстанцев Хаоса";
            }

            CustomSpawn.SpawnManager.SendRoundStateToBot("ended", $"Раунд номер {RoundNumber} закончен. Победа присвоена: \"Победа {winner}\"");
        }



        public static HashSet<Player> Cuffed049s { get; } = new HashSet<Player>();


        private void OnWaitingForPlayers()
        {
            _isOusSpawnedThisRound = false;

            DirtyPlayers.Clear();
            ClearAllCensors();
            UpgradedOnFine.Clear();
            Scp106Containment.ClearObjects();
            CageItem.Caged173s.Clear();
            foreach (var handle in VeryFineTimers.Values) Timing.KillCoroutines(handle);
            VeryFineTimers.Clear();
            CageItem.SpawnedCages.Clear();
            CageItem.DraggingPlayers.Clear();
            Scp035Armor.ClearMaskObjects();
            Scp181Role.ClearRoundData();
            Cuffed049s.Clear();
            foreach (var handle in BleedingPlayers.Values) Timing.KillCoroutines(handle);
            BleedingPlayers.Clear();
            CuffsItem.Cooldowns.Clear();
            CuffsItem.ActiveCuffing.Clear();
            KeyItem.Cooldowns.Clear();
            RottedPlayers.Clear();
            Scp079ExtractedSerials.Clear();
            PlayersWith079Data.Clear();
            foreach (var handle in Active079Extractions.Values)
                Timing.KillCoroutines(handle);
            Active079Extractions.Clear();
            KeyItem.ActiveUncuffing.Clear();
            MEC.Timing.KillCoroutines(_hoursTrackerHandle);
            MEC.Timing.KillCoroutines(_cmdListenerHandle);
            _cmdListenerHandle = MEC.Timing.RunCoroutine(ListenBotCommandsTick());
            Tranquilizer.ControlledDogs.Clear();
            Tranquilizer.Carrying.Clear();
            JointThickenerItem.BlockedSkeletons.Clear();
            try
            {
                UserSettings.ServerSpecific.ServerSpecificSettingsSync.DefinedSettings = new UserSettings.ServerSpecific.ServerSpecificSettingBase[]
{
    new UserSettings.ServerSpecific.SSKeybindSetting(1, "Взять/Отпустить клетку 173", KeyCode.G, true, false, "Позволяет подцепить клетку буксировки или бросить её"),
    new UserSettings.ServerSpecific.SSKeybindSetting(2, "Консервация SCP (Авто)", KeyCode.H, true, false, "Автоматически определяет тип SCP перед вами и изолирует его"),
    new UserSettings.ServerSpecific.SSKeybindSetting(3, "Взять карту (SCP-049)", KeyCode.J, true, false, "Позволяет Чумному Доктору подбирать ключ-карты с пола"),
    new UserSettings.ServerSpecific.SSKeybindSetting(4, "Обыскать игрока", KeyCode.K, true, false, "Запускает 3-секундный обыск карманов человека перед вами"),
    new UserSettings.ServerSpecific.SSKeybindSetting(5, "Применить медицину на цель", KeyCode.L, true, false, "Использовать медицинский предмет из ваших рук на другого выжившего"),
    new UserSettings.ServerSpecific.SSKeybindSetting(6, "Удар ближнего боя", KeyCode.F, true, false, "Нанести быстрый удар кулаком (-5 HP, кулдаун 3 секунды)")
};

                Log.Info("[Server-Specific UI] Вкладка меню из 6 кнопок успешно зарегистрирована в движке игры.");
            }
            catch (Exception ex)
            {
                Log.Error($"[Server-Specific UI] Ошибка инициализации DefinedSettings: {ex.Message}");
            }

        }

        private void OnChangingRole(ChangingRoleEventArgs ev)
        {
            if (ev.Player != null) FullResetPlayer(ev.Player);
        }

        private void OnDying(DyingEventArgs ev)
        {
            if (ev.Player != null) FullResetPlayer(ev.Player);
        }

        private void OnLeft(LeftEventArgs ev)
        {
            if (ev.Player != null) FullResetPlayer(ev.Player);
        }


        private void FullResetPlayer(Player player)
        {
            if (player == null) return;
            ushort playerId = (ushort)player.Id;

            if (player.CustomName != null)
                player.CustomName = null;


            if (RottedPlayers.Contains(playerId))
            {
                RottedPlayers.Remove(playerId);
            }

            if (BleedingPlayers.TryGetValue(playerId, out CoroutineHandle bleedHandle))
            {
                Timing.KillCoroutines(bleedHandle);
                BleedingPlayers.Remove(playerId);
            }

            if (VeryFineTimers.TryGetValue(playerId, out CoroutineHandle veryFineHandle))
            {
                Timing.KillCoroutines(veryFineHandle);
                VeryFineTimers.Remove(playerId);
            }

            Scp181Role.ActiveLuckyPlayers.Remove(playerId);


            if (CageItem.DraggingPlayers.TryGetValue(player, out Player scp173))
            {
                player.DisableEffect(EffectType.SinkHole);
                CageItem.DraggingPlayers.Remove(player);
            }

            if (CageItem.Caged173s.Contains(player))
            {
                CageItem.DestroyCage(player);
            }

            if (Cuffed049s.Contains(player))
            {
                Cuffed049s.Remove(player);
            }
            PlayersWith079Data.Remove(player);
            if (Active079Extractions.TryGetValue(player, out var extractHandle))
            {
                Timing.KillCoroutines(extractHandle);
                Active079Extractions.Remove(player);
            }
        }

        private void OnVerified(VerifiedEventArgs ev)
        {
            if (ev.Player == null)
                return;

            foreach (var censor in Scp96Censors.Values)
            {
                if (censor != null)
                    ev.Player.HideNetworkObject(censor);
            }
        }



        private void OnSearchingPickup(SearchingPickupEventArgs ev)
        {
            if (ev.Player == null || ev.Pickup == null) return;

            if (HeavyRifles.Contains(ev.Pickup.Type))
            {
                int count = ev.Player.Items.Count(i => HeavyRifles.Contains(i.Type));
                if (count >= 2)
                {
                    ev.IsAllowed = false;
                    ev.Player.ShowHint("<color=red>Вы не можете нести больше 2 единиц основного оружия!</color>", 3f);
                    return;
                }
            }

            if (MachineGuns.Contains(ev.Pickup.Type))
            {
                int mgCount = ev.Player.Items.Count(i => MachineGuns.Contains(i.Type));
                if (mgCount >= 1)
                {
                    ev.IsAllowed = false;
                    ev.Player.ShowHint("<color=red>Вы не можете нести больше 1 пулемета!</color>", 3f);
                    return;
                }
            }

            if (ev.Pickup.Type == ItemType.ArmorHeavy)
            {
                if (ev.Player.Role.Type == RoleTypeId.ClassD || ev.Player.Role.Type == RoleTypeId.Scientist)
                {
                    ev.IsAllowed = false;
                    ev.Player.ShowHint("<color=red>Гражданские классы слишком слабы для Тяжелой Брони!</color>", 3f);
                    return;
                }
            }
        }

        private void OnItemAdded(ItemAddedEventArgs ev)
        {
            if (ev.Player == null || ev.Item == null) return;

            if (ev.Item.Type == ItemType.MicroHID)
            {
                Timing.CallDelayed(0.1f, () =>
                {
                    if (ev.Player != null && ev.Player.Items.Contains(ev.Item) && ev.Player.CurrentItem != ev.Item)
                    {
                        ev.Player.CurrentItem = ev.Item;
                        ev.Player.ShowHint("<color=orange>МикроХИД слишком громоздкий, вы обязаны держать его в руках!</color>", 3f);
                    }
                });
            }
        }

        private void OnChangingItem(ChangingItemEventArgs ev)
        {
            if (ev.Player == null) return;

            if (ev.Item != null && RottedPlayers.Contains((ushort)ev.Player.Id))
            {
                ev.IsAllowed = false;
                ev.Player.ShowHint("<color=red>Прикосновение Чумного Доктора иссушило ваши руки! Вы не можете держать предметы.</color>", 3f);
                return;
            }

            if (ev.Item?.Type != ItemType.MicroHID)
            {
                var microHid = ev.Player.Items.FirstOrDefault(i => i.Type == ItemType.MicroHID);
                if (microHid != null)
                {
                    ev.IsAllowed = false;
                    Timing.CallDelayed(0.05f, () => ev.Player.CurrentItem = microHid);
                    ev.Player.ShowHint("<color=orange>Вы не можете убрать МикроХИД из рук, пока он находится в инвентаре!</color>", 2f);
                }
            }
        }

        private void OnUpgradingPlayer(UpgradingPlayerEventArgs ev)
        {
            if (ev.Player == null) return;

            if (ev.Player.Role.Side == Side.Scp || ev.Player == Scp035Armor.Active035Player)
                return;

            ev.IsAllowed = false;
            ushort playerId = (ushort)ev.Player.Id;

            switch (ev.KnobSetting)
            {
                case Scp914KnobSetting.Rough:
                    ev.Player.Kill("Разорван в клочья режимом Rough");
                    break;

                case Scp914KnobSetting.Coarse:
                    ev.Player.Health = 1f;
                    break;

                case Scp914KnobSetting.OneToOne:
                    var targetPair = Player.List.FirstOrDefault(p => p != ev.Player && p.Role.Type == ev.Player.Role.Type);
                    if (targetPair != null)
                    {
                        Vector3 tempPos = ev.Player.Position;
                        ev.Player.Position = targetPair.Position;
                        targetPair.Position = tempPos;
                    }
                    break;

                case Scp914KnobSetting.Fine:
                    if (UpgradedOnFine.Contains(playerId))
                    {
                        ev.Player.Kill("Перегрузка клеток при повторном улучшении Fine");
                        break;
                    }

                    if (UnityEngine.Random.Range(0, 100) < 10)
                    {
                        ev.Player.Role.Set(RoleTypeId.Scp0492, SpawnReason.ForceClass);
                    }
                    else
                    {
                        ev.Player.MaxHealth = 150f;
                        ev.Player.Health = 150f;
                        UpgradedOnFine.Add(playerId);
                    }
                    break;

                case Scp914KnobSetting.VeryFine:
                    ev.Player.MaxHealth = 3000f;
                    ev.Player.Health = 3000f;
                    ev.Player.EnableEffect(EffectType.MovementBoost, 255, 30f);

                    if (VeryFineTimers.TryGetValue(playerId, out CoroutineHandle oldVeryFine))
                        Timing.KillCoroutines(oldVeryFine);

                    CoroutineHandle veryFineHandle = Timing.RunCoroutine(VeryFineExpiryCoroutine(ev.Player, playerId));
                    VeryFineTimers[playerId] = veryFineHandle;
                    break;
            }
        }

        private IEnumerator<float> VeryFineExpiryCoroutine(Player player, ushort id)
        {
            yield return Timing.WaitForSeconds(30f);
            if (player != null && player.IsAlive && VeryFineTimers.ContainsKey(id))
            {
                player.EnableEffect(EffectType.Decontaminating, 9999f);
            }
            VeryFineTimers.Remove(id);
        }

        private void OnTriggeringTesla(TriggeringTeslaEventArgs ev)
        {
            if (ev.Player == null) return;

            RoleTypeId role = ev.Player.Role.Type;
            if (role == RoleTypeId.ClassD || role == RoleTypeId.Scientist || role == RoleTypeId.FacilityGuard || ev.Player.Role.Side == Side.Mtf)
            {
                ev.IsAllowed = false;
            }
        }

        private void OnSpawned(SpawnedEventArgs ev)
        {
            if (ev.Player == null) return;

            if (ev.Player.Role.Type == RoleTypeId.Scp173)
            {
                Timing.CallDelayed(0.1f, () =>
                {
                    if (ev.Player != null && ev.Player.Role.Type == RoleTypeId.Scp173)
                    {
                        ev.Player.MaxHealth = 7000f;
                        ev.Player.Health = 7000f;
                    }
                });
            }

            if (ev.Player.IsHuman && !ZoneRoleManager.IsZoneStaff(ev.Player))
            {
                switch (ev.Player.Role.Type)
                {
                    case RoleTypeId.ClassD:
                        if (!Scp181Role.ActiveLuckyPlayers.Contains((ushort)ev.Player.Id))
                        {
                            ev.Player.CustomName = RpNameGenerator.GetClassDName();
                        }
                        break;

                    case RoleTypeId.FacilityGuard:
                        ev.Player.CustomName = "Охранник " + RpNameGenerator.GetGuardName();
                        break;

                    case RoleTypeId.Scientist:
                        ev.Player.CustomName = RpNameGenerator.GetStaffName("Н.С.");
                        break;

                    case RoleTypeId.NtfCaptain:
                        ev.Player.CustomName = RpNameGenerator.GetMtfName("Капитан МОГ");
                        break;
                    case RoleTypeId.NtfSergeant:
                        ev.Player.CustomName = RpNameGenerator.GetMtfName("Сержант МОГ");
                        break;
                    case RoleTypeId.NtfPrivate:
                        ev.Player.CustomName = RpNameGenerator.GetMtfName("Рядовой МОГ");
                        break;
                    case RoleTypeId.NtfSpecialist:
                        ev.Player.CustomName = RpNameGenerator.GetMtfName("Спец. МОГ");
                        break;

                    case RoleTypeId.ChaosConscript:
                    case RoleTypeId.ChaosRifleman:
                    case RoleTypeId.ChaosMarauder:
                    case RoleTypeId.ChaosRepressor:
                        ev.Player.CustomName = $"Агент ПХ «Штурм-{UnityEngine.Random.Range(100, 1000)}»";
                        break;
                }
            }



            if (ev.Player.Role.Type == RoleTypeId.Scp079 && ev.Player.Role is Exiled.API.Features.Roles.Scp079Role scp079)
            {
                scp079.Level = 5;
            }

            if (!_isOusSpawnedThisRound && ev.Player.Role.Type != RoleTypeId.Spectator && ev.Player.Role.Type != RoleTypeId.None)
            {
                _isOusSpawnedThisRound = true;
                MEC.Timing.CallDelayed(1.5f, () =>
                {
                    Log.Info("[ОУС-106] Сетевая инфраструктура стабильна. Запуск автоматической сборки ОУС-106...");
                    Scp106Containment.SpawnProtocolObjects();
                });
            }


            if (ev.Player.Role.Type == RoleTypeId.Scp3114)
            {
                Timing.CallDelayed(0.1f, () =>
                {
                    if (ev.Player != null && ev.Player.Role.Type == RoleTypeId.Scp3114)
                    {
                        ev.Player.MaxHealth = 3500f;
                        ev.Player.Health = 3500f;
                        ev.Player.ShowHint("<color=red><b>Вы пробудились!</b>\nВаше максимальное здоровье увеличено до 3500 ХП.</color>", 5f);
                    }
                });
            }

            if (ev.OldRole == RoleTypeId.Scp096 && ev.Player.Role.Type != RoleTypeId.Scp096)
            {
                RemoveCensor(ev.Player);
                return;
            }

            if (ev.Player.Role.Type == RoleTypeId.Scp096)
            {
                Timing.CallDelayed(0.5f, () =>
                {
                    if (ev.Player != null && ev.Player.Role.Type == RoleTypeId.Scp096)
                    {
                        AddCensor(ev.Player);
                    }
                });
            }
        }

        private IEnumerator<float> TrackBranchHoursTick()
        {
            while (Round.InProgress)
            {
                yield return MEC.Timing.WaitForSeconds(60f);

                foreach (Player player in Player.List.Where(p => p.IsHuman && p.IsAlive && !p.IsNPC))
                {
                    string steamId = player.UserId;
                    if (string.IsNullOrEmpty(steamId)) continue;

                    CustomSpawn.PlayerData data = CustomSpawn.PlayerDataStore.Get(steamId);
                    if (data == null || !data.IsVerified) continue;

                    string currentRoleName = player.CustomInfo;
                    if (string.IsNullOrEmpty(currentRoleName)) currentRoleName = player.Role.Type.ToString();

                    string branch = "Universal";
                    switch (currentRoleName)
                    {
                        case "JuniorScientist":
                        case "ZoneScientist":
                        case "ProjectLead":
                        case "ResearchDirector":
                        case "Scientist":
                            branch = "Science";
                            break;

                        case "Intern":
                        case "Orderly":
                        case "Therapist":
                        case "Surgeon":
                        case "ChiefPhysician":
                            branch = "Medical";
                            break;

                        case "SecurityCadet":
                        case "SecurityOfficer":
                        case "SeniorOfficer":
                        case "SecurityLieutenant":
                        case "SecurityChief":
                        case "FacilityGuard":
                            branch = "Security";
                            break;

                        case "MaintenanceTech":
                        case "LogisticsOfficer":
                        case "ZoneEngineer":
                        case "ChiefEngineer":
                            branch = "Engineering";
                            break;
                    }

                    float addition = 1f / 60f;

                    if (branch != "Universal")
                    {
                        if (data.BranchHours == null) data.BranchHours = new Dictionary<string, float>();
                        if (!data.BranchHours.ContainsKey(branch)) data.BranchHours[branch] = 0f;
                        data.BranchHours[branch] += addition;
                    }

                    data.HoursPlayed += addition;

                    int calculatedLevel = (int)(data.HoursPlayed / 3f);
                    if (calculatedLevel != data.Level)
                    {
                        data.Level = calculatedLevel;
                        player.ShowHint("<color=#FFD700>🎉 Ваш уровень повышен до **" + data.Level + " lvl**!</color>", 6f);
                    }

                    CustomSpawn.PlayerDataStore.Save(data);
                }
            }
        }

        private IEnumerator<float> ListenBotCommandsTick()
        {
            string cmdDir = Path.Combine(Paths.Configs, "EyeCatcherPlugin", "Shared", "Commands");
            if (!Directory.Exists(cmdDir)) Directory.CreateDirectory(cmdDir);

            while (true)
            {
                yield return MEC.Timing.WaitForSeconds(2f);
                try
                {
                    string[] files = Directory.GetFiles(cmdDir, "*.json");
                    foreach (string file in files)
                    {
                        string json = File.ReadAllText(file);
                        BotCommandPacket packet = Newtonsoft.Json.JsonConvert.DeserializeObject<BotCommandPacket>(json);

                        if (packet != null)
                        {
                            if (packet.Command == "setadminwhitelist")
                            {
                                string sid = (packet.Args != null && packet.Args.ContainsKey("steamid")) ? packet.Args["steamid"] : "";
                                string valStr = (packet.Args != null && packet.Args.ContainsKey("value")) ? packet.Args["value"] : "false";

                                if (!string.IsNullOrEmpty(sid))
                                {
                                    CustomSpawn.PlayerData data = CustomSpawn.PlayerDataStore.Get(sid);
                                    data.AdminWhitelist = (valStr == "true");
                                    CustomSpawn.PlayerDataStore.Save(data);
                                }
                            }
                            else if (packet.Command == "sync_online_admins")
                            {
                                string adminsString = (packet.Args != null && packet.Args.ContainsKey("steamids")) ? packet.Args["steamids"] : "";
                                OnlineVerifiedAdmins.Clear();

                                if (!string.IsNullOrEmpty(adminsString))
                                {
                                    string[] splitAdmins = adminsString.Split(',');
                                    foreach (string adminSteamId in splitAdmins)
                                    {
                                        if (Player.List.Any(p => p.UserId == adminSteamId && p.IsConnected))
                                        {
                                            OnlineVerifiedAdmins.Add(adminSteamId);
                                        }
                                    }
                                }
                            }
                            else if (packet.Command == "toggle_antagonist")
                            {
                                string sid = (packet.Args != null && packet.Args.ContainsKey("steamid")) ? packet.Args["steamid"] : "";
                                string stateStr = (packet.Args != null && packet.Args.ContainsKey("state")) ? packet.Args["state"] : "false";

                                if (!string.IsNullOrEmpty(sid))
                                {
                                    CustomSpawn.PlayerData data = CustomSpawn.PlayerDataStore.Get(sid);
                                    data.WantAntagonist = (stateStr == "true");
                                    CustomSpawn.PlayerDataStore.Save(data);
                                }
                            }
                            else if (packet.Command == "force_end_round")
                            {
                                string winnerType = (packet.Args != null && packet.Args.ContainsKey("winner")) ? packet.Args["winner"] : "draw";
                                Log.Info($"[Администрация Дискорд] Получена команда экстренного завершения раунда. Исход: {winnerType}");

                                if (RoundSummary.singleton != null)
                                {
                                    foreach (Player p in Player.List)
                                    {
                                        if (p == null || !p.IsAlive || p.IsNPC) continue;

                                        if (winnerType == "scp" && p.Role.Side != Side.Scp)
                                        {
                                            p.Kill("Раунд принудительно завершен Администрацией");
                                        }
                                        else if (winnerType == "mtf" && p.Role.Side != Side.Mtf && p.Role.Type != RoleTypeId.Scientist)
                                        {
                                            p.Kill("Раунд принудительно завершен Администрацией");
                                        }
                                        else if (winnerType == "chaos" && p.Role.Side != Side.ChaosInsurgency && p.Role.Type != RoleTypeId.ClassD)
                                        {
                                            p.Kill("Раунд принудительно завершен Администрацией");
                                        }
                                        else if (winnerType == "draw")
                                        {
                                            p.Kill("Раунд принудительно завершен Администрацией");
                                        }
                                    }

                                    RoundSummary.singleton.ForceEnd();
                                }
                                else
                                {
                                    Round.EndRound(true);
                                }
                            }
                        }
                        File.Delete(file);
                    }
                }
                catch { }
            }
        }


        public class BotCommandPacket
        {
            public string Command { get; set; }
            public Dictionary<string, string> Args { get; set; }
        }


        private void OnEscaping(EscapingEventArgs ev)
        {
            if (ev.Player == null || ev.Player.Role.Side != Side.Scp) return;

            if (PlayersWith079Data.Contains(ev.Player))
            {
                foreach (var item in ev.Player.Items.ToList())
                    ev.Player.RemoveItem(item);

                ev.IsAllowed = false;
                PlayersWith079Data.Remove(ev.Player);

                ev.Player.Role.Set(RoleTypeId.Spectator);
                Map.ShowHint($"<color=#FF00FF>⚠️ {ev.Player.Nickname} успешно вынес данные SCP-079 из комплекса!</color>", 6f);
                Exiled.API.Features.Cassie.Message("SCP 0 7 9 DATA SUCCESSFULLY EXTRACTED BY HOSTILE PERSONNEL", false, true, true);
                return;
            }

            RoleTypeId role = ev.Player.Role.Type;

            if (role == RoleTypeId.Scp106 || role == RoleTypeId.Scp096)
            {
                ev.IsAllowed = false;
                ev.Player.ShowHint("<color=red>Этот объект SCP слишком нестабилен или огромен, чтобы незаметно покинуть комплекс!</color>", 4f);
                return;
            }

            if (role == RoleTypeId.Scp049 && Cuffed049s.Contains(ev.Player))
            {
                ev.IsAllowed = false;
                ev.Player.ShowHint("<color=red>Вы скованы кандалами! Вы не можете протиснуться в зону эвакуации.</color>", 4f);
                return;
            }

            if (role == RoleTypeId.Scp939 && Tranquilizer.ControlledDogs.ContainsKey(ev.Player))
            {
                ev.IsAllowed = false;
                ev.Player.ShowHint("<color=red>Разум под действием транквилизатора блокирует волю к побегу!</color>", 4f);
                return;
            }

            if (role == RoleTypeId.Scp173 && CageItem.Caged173s.Contains(ev.Player))
            {
                ev.IsAllowed = false;
                ev.Player.ShowHint("<color=red>Клетка примитивов заблокировала ваши гидравлические приводы!</color>", 4f);
                return;
            }

            ev.IsAllowed = false;
            string scpNumber = role == RoleTypeId.Scp3114 ? "3 1 1 4" : role.ToString().Replace("Scp", "");
            Exiled.API.Features.Cassie.Message($"ATTENTION ALL PERSONNEL OBJECT SCP {scpNumber} HAS ESCAPED THE FACILITY", false, true, true);


            ev.Player.Role.Set(RoleTypeId.Spectator);

            Map.ShowHint($"<color=red>⚠️ Объект SCP-{scpNumber} успешно совершил побег из комплекса!</color>", 5f);
        }

        private void OnHurting(HurtingEventArgs ev)
        {
            if (ev.Player == null) return;

            if (ev.Attacker != null && ev.Attacker.Role.Type == RoleTypeId.Scp173 && CageItem.Caged173s.Contains(ev.Attacker))
            {
                ev.Amount = 0f;
                ev.IsAllowed = false;
                ev.Attacker.ShowHint("<color=red>Вы заперты в клетке и не можете наносить урон!</color>", 2f);
                return;
            }

            RoleTypeId role = ev.Player.Role.Type;

            if (role == RoleTypeId.Scp173)
            {
                if (ev.DamageHandler.Type == DamageType.MicroHid)
                {
                    ev.Amount = 0f;
                    ev.IsAllowed = false;
                }
                return;
            }

            if ((role == RoleTypeId.Scp106 || role == RoleTypeId.Scp096) && ev.Amount < 9999f)
            {
                ev.Amount = 0f;
                ev.IsAllowed = false;
                return;
            }

            if (ev.Attacker != null && ev.Attacker.Role.Type == RoleTypeId.Scp049)
            {
                RottedPlayers.Add((ushort)ev.Player.Id);
            }

            if (ev.Attacker != null && ev.DamageHandler.Base is FirearmDamageHandler && ev.Player.IsHuman)
            {
                ushort playerId = (ushort)ev.Player.Id;
                if (!BleedingPlayers.ContainsKey(playerId))
                {
                    CoroutineHandle handle = Timing.RunCoroutine(BleedingCoroutine(ev.Player, playerId));
                    BleedingPlayers.Add(playerId, handle);
                }
            }
        }

        private IEnumerator<float> BleedingCoroutine(Player player, ushort id)
        {
            yield return Timing.WaitForSeconds(10f);

            while (player != null && player.IsAlive && BleedingPlayers.ContainsKey(id))
            {
                player.Hurt(3f, DamageType.Asphyxiation);
                player.ShowHint("<color=red>У вас открылось кровотечение! Используйте Аптечку или SCP-500.</color>", 2f);

                yield return Timing.WaitForSeconds(7f);
            }
        }

        private void OnPinging(Exiled.Events.EventArgs.Scp079.PingingEventArgs ev)
        {
            ev.IsAllowed = false;
        }

        private IEnumerator<float> Scp035AuraTick()
        {
            while (true)
            {
                yield return Timing.WaitForSeconds(2f);

                if (Scp035Armor.Active035Player == null || !Scp035Armor.Active035Player.IsAlive)
                    continue;

                Player msk = Scp035Armor.Active035Player;
                Vector3 mskPos = msk.Position;

                foreach (Player p in Player.List)
                {
                    if (p == null || p == msk || !p.IsHuman || p.Role.Type == RoleTypeId.Tutorial)
                        continue;

                    if (Vector3.SqrMagnitude(p.Position - mskPos) <= 9f)
                    {
                        p.Hurt(1f, DamageType.Asphyxiation);
                    }
                }
            }
        }

        private void On049Attacking(Exiled.Events.EventArgs.Scp049.AttackingEventArgs ev)
        {
            if (ev.Player != null && Cuffed049s.Contains(ev.Player))
            {
                ev.IsAllowed = false;
                ev.Player.ShowHint("<color=red>Вы связаны и не можете атаковать!</color>", 2f);
            }
        }

        private void On049FinishingRecall(Exiled.Events.EventArgs.Scp049.FinishingRecallEventArgs ev)
        {
            if (ev.Player != null && Cuffed049s.Contains(ev.Player))
            {
                ev.IsAllowed = false;
                ev.Player.ShowHint("<color=red>Кандалы блокируют ваши лечебные способности!</color>", 2f);
            }
        }

        private void OnChangingSpectatedPlayer(ChangingSpectatedPlayerEventArgs ev)
        {
            if (ev.Player == null)
                return;

            if (ev.OldTarget != null &&
                Plugin.Instance.GlassesItem.ActiveGlassesPlayers.Contains(ev.OldTarget))
            {
                DeObfuscateScp96s(ev.Player);

                DirtyPlayers.Remove(ev.Player);
            }

            if (ev.NewTarget != null &&
                Plugin.Instance.GlassesItem.ActiveGlassesPlayers.Contains(ev.NewTarget))
            {
                ObfuscateScp96s(ev.Player);

                DirtyPlayers.Add(ev.Player);
            }
        }
        public static void OnSettingReceived(ReferenceHub hub, UserSettings.ServerSpecific.ServerSpecificSettingBase setting)
        {
            Player player = Player.Get(hub);
            if (player == null || !player.IsConnected) return;
            var keybind = setting as UserSettings.ServerSpecific.SSKeybindSetting;
            if (keybind == null)
                return;

            if (!keybind.SyncIsPressed)
                return;

            if (setting.SettingId == 1)
            {
                if (!player.IsHuman) return;

                if (CageItem.DraggingPlayers.TryGetValue(player, out Player scp173))
                {
                    player.DisableEffect(EffectType.SinkHole);
                    CageItem.DraggingPlayers.Remove(player);
                    player.ShowHint("<color=green>Вы отпустили клетку буксировки.</color>", 3f);
                }
                else
                {
                    if (Physics.SphereCast(player.CameraTransform.position, 0.4f, player.CameraTransform.forward, out RaycastHit hit, 4.5f))
                    {
                        Player target173 = Player.Get(hit.collider);
                        if (target173 != null && target173.Role.Type == RoleTypeId.Scp173 && CageItem.Caged173s.Contains(target173))
                        {
                            if (CageItem.DraggingPlayers.ContainsValue(target173))
                            {
                                player.ShowHint("<color=red>Эту клетку уже тащит другой сотрудник!</color>", 3f);
                                return;
                            }

                            player.EnableEffect(EffectType.SinkHole);
                            player.CurrentItem = null;
                            CageItem.DraggingPlayers.Add(player, target173);

                            MEC.Timing.RunCoroutine(Drag173Command.DragAI(player, target173));
                            player.ShowHint("<color=green>Вы подцепили клетку с SCP-173 на буксир. Она следует за вами.</color>", 3f);
                        }
                    }
                }
            }

            else if (setting.SettingId == 2)
            {
                if (!player.IsHuman) return;

                if (Physics.SphereCast(player.CameraTransform.position, 0.4f, player.CameraTransform.forward, out RaycastHit hit, 4.5f))
                {
                    Player targetScp = Player.Get(hit.collider);
                    if (targetScp == null || targetScp.Role.Side != Side.Scp)
                    {
                        targetScp = Player.List.FirstOrDefault(p => p.Role.Type == RoleTypeId.Scp173 && Vector3.Distance(p.Position, hit.point) <= 2.5f);
                    }

                    if (targetScp != null)
                    {
                        if (targetScp.Role.Type == RoleTypeId.Scp173)
                        {
                            if (!CageItem.Caged173s.Contains(targetScp))
                            {
                                player.ShowHint("<color=red>Этот SCP-173 должен быть предварительно закован в клетку!</color>", 3f);
                                return;
                            }
                            if (targetScp.CurrentRoom == null || targetScp.CurrentRoom.Type != RoomType.Hcz049)
                            {
                                player.ShowHint("<color=red>Вы должны закатить клетку с SCP-173 внутрь её комплекса содержания (Hcz049)!</color>", 3f);
                                return;
                            }
                            foreach (var door in targetScp.CurrentRoom.Doors)
                            {
                                if (door.Name.Contains("173") || door.Type == DoorType.Scp173Gate)
                                {
                                    door.IsOpen = false;
                                    door.Lock(float.MaxValue, DoorLockType.AdminCommand);
                                }
                            }
                            Room targetRoom173 = Room.List.FirstOrDefault(r => r.Type == RoomType.Hcz049);
                            if (targetRoom173 != null) player.Position = targetRoom173.Position + Vector3.up * 1f;

                            CageItem.DestroyCage(targetScp);
                            targetScp.Role.Set(RoleTypeId.Spectator);
                            Exiled.API.Features.Cassie.Message("SCP 1 7 3 SUCCESSFULLY CONTAINED AND SECURED", false, true, true);
                            player.ShowHint("<color=green>SCP-173 успешно утилизирован и заперт в камере!</color>", 5f);
                        }

                        else if (targetScp.Role.Type == RoleTypeId.Scp049)
                        {
                            if (!Cuffed049s.Contains(targetScp))
                            {
                                player.ShowHint("<color=red>Этого SCP-049 сначала необходимо сковать кандалами!</color>", 3f);
                                return;
                            }
                            if (targetScp.CurrentRoom == null || targetScp.CurrentRoom.Type != RoomType.Hcz049)
                            {
                                player.ShowHint("<color=red>SCP-049 должен находиться строго внутри своего блока содержания (Hcz049)!</color>", 3f);
                                return;
                            }
                            foreach (var door in targetScp.CurrentRoom.Doors)
                            {
                                if (door.Name.Contains("049") || door.Type == DoorType.Scp049Gate)
                                {
                                    door.IsOpen = false;
                                    door.Lock(float.MaxValue, DoorLockType.AdminCommand);
                                }
                            }
                            Room targetRoom049 = Room.List.FirstOrDefault(r => r.Type == RoomType.Hcz049);
                            if (targetRoom049 != null) player.Position = targetRoom049.Position + Vector3.up * 1f;

                            Cuffed049s.Remove(targetScp);
                            targetScp.Role.Set(RoleTypeId.Spectator);
                            Exiled.API.Features.Cassie.Message("SCP 0 4 9 SUCCESSFULLY CONTAINED AND SECURED", false, true, true);
                            player.ShowHint("<color=green>SCP-049 успешно возвращен под стражу и заблокирован!</color>", 5f);
                        }

                        else if (targetScp.Role.Type == RoleTypeId.Scp096)
                        {
                            if (player.Role.Side != Side.Mtf && player.Role.Type != RoleTypeId.FacilityGuard)
                            {
                                player.ShowHint("<color=red>Консервацию SCP-096 может проводить только Охрана или МОГ!</color>", 3f);
                                return;
                            }
                            if (targetScp.CurrentRoom != null && targetScp.CurrentRoom.Type == RoomType.Hcz096)
                            {
                                foreach (var door in targetScp.CurrentRoom.Doors)
                                {
                                    door.IsOpen = false;
                                    door.Lock(float.MaxValue, DoorLockType.AdminCommand);
                                }
                                targetScp.Role.Set(RoleTypeId.Spectator);
                                Exiled.API.Features.Cassie.Message("SCP 0 9 6 SUCCESSFULLY CONTAINED AND SECURED", false, true, true);
                                player.ShowHint("<color=green>SCP-096 успешно заблокирован в своей камере!</color>", 5f);
                            }
                            else
                            {
                                player.ShowHint("<color=red>SCP-096 должен находиться строго внутри своей камеры содержания (Hcz096)!</color>", 3f);
                            }
                        }

                        else if (targetScp.Role.Type == RoleTypeId.Scp939)
                        {
                            if (Tranquilizer.ControlledDogs.ContainsKey(targetScp))
                            {
                                if (targetScp.CurrentRoom != null && targetScp.CurrentRoom.Type == RoomType.Hcz939)
                                {
                                    var dogData = Tranquilizer.ControlledDogs[targetScp];
                                    Timing.KillCoroutines(dogData.WakeTimerHandle);
                                    Timing.KillCoroutines(dogData.AICoroutineHandle);

                                    foreach (var pair in new Dictionary<Player, Player>(Tranquilizer.Carrying))
                                    {
                                        if (pair.Value == targetScp)
                                        {
                                            Tranquilizer.RemoveDogControl(pair.Key);
                                            break;
                                        }
                                    }

                                    Tranquilizer.ControlledDogs.Remove(targetScp);
                                    targetScp.Role.Set(RoleTypeId.Spectator);
                                    if (targetScp is Npc npc) npc.Destroy();

                                    Exiled.API.Features.Cassie.Message("SCP 9 3 9 SUCCESSFULLY CONTAINED AND SECURED", false, true, true);
                                    player.ShowHint("<color=green>SCP-939 успешно деактивирована в зоне спавна!</color>", 5f);
                                }
                                else
                                {
                                    player.ShowHint("<color=red>Вы должны привести послушную SCP-939 в её родную комнату содержания (Hcz939)!</color>", 3f);
                                }
                            }
                        }

                        else if (targetScp.Role.Type == RoleTypeId.Scp3114)
                        {
                            if (targetScp.CurrentRoom == null || targetScp.CurrentRoom.Type != RoomType.Lcz173)
                            {
                                player.ShowHint("<color=red>Вы должны находиться строго внутри тестовой камеры содержания (Pt00)!</color>", 3f);
                                return;
                            }

                            bool isSticky = JointThickenerItem.BlockedSkeletons.TryGetValue(targetScp, out float endTime) && UnityEngine.Time.time < endTime;
                            if (!isSticky)
                            {
                                player.ShowHint("<color=red>SCP-3114 не в состоянии липкости! Сначала поразите его из Загустителя швов.</color>", 3f);
                                return;
                            }

                            JointThickenerItem.BlockedSkeletons.Remove(targetScp);
                            targetScp.Role.Set(RoleTypeId.Spectator);
                            Exiled.API.Features.Cassie.Message("SCP 3 1 1 4 SUCCESSFULLY CONTAINED AND SECURED", false, true, true);
                            player.ShowHint("<color=green>SCP-3114 успешно утилизирован!</color>", 5f);
                        }
                    }
                }
            }

            else if (setting.SettingId == 3)
            {
                if (player.Role.Type != RoleTypeId.Scp049) return;

                if (Physics.SphereCast(player.CameraTransform.position, 0.45f, player.CameraTransform.forward, out RaycastHit hit, 4.5f))
                {
                    Exiled.API.Features.Pickups.Pickup pickup = Exiled.API.Features.Pickups.Pickup.Get(hit.collider.gameObject);
                    if (pickup == null && hit.collider.transform.parent != null)
                        pickup = Exiled.API.Features.Pickups.Pickup.Get(hit.collider.transform.parent.gameObject);

                    if (pickup != null && pickup.Type.ToString().Contains("Keycard") && pickup.Type != ItemType.KeycardChaosInsurgency)
                    {
                        ItemType type = pickup.Type;
                        pickup.Destroy();
                        var item = player.AddItem(type);
                        Timing.CallDelayed(0.1f, () => { if (player != null && player.IsAlive) player.CurrentItem = item; });
                        player.ShowHint($"<color=green>Вы успешно подобрали карту {type}!</color>", 3f);
                    }
                }
            }

            else if (setting.SettingId == 4)
            {
                if (!player.IsHuman) return;
                if (player.CurrentItem == null || !player.CurrentItem.IsWeapon)
                {
                    player.ShowHint("<color=red>Вы должны держать в руках оружие, чтобы провести обыск!</color>", 3f);
                    return;
                }

                if (Physics.SphereCast(player.CameraTransform.position, 0.4f, player.CameraTransform.forward, out RaycastHit hit, 3.5f))
                {
                    Player target = Player.Get(hit.collider);
                    if (target != null && target.IsHuman && target != player)
                    {
                        if (target.CurrentItem != null)
                        {
                            player.ShowHint("<color=red>Цель оказывает сопротивление! Руки цели должны быть пусты.</color>", 3f);
                            return;
                        }

                        MEC.Timing.RunCoroutine(ContainCommand.SearchCommand.SearchProcess(player, target));
                        player.ShowHint("<color=orange>Вы начали обыск... Не отводите взгляд 3 секунды.</color>", 3f);
                    }
                }
            }

            else if (setting.SettingId == 5)
            {
                if (!player.IsHuman || player.CurrentItem == null) return;

                HashSet<ItemType> medicalList = new HashSet<ItemType> { ItemType.Medkit, ItemType.Painkillers, ItemType.SCP500, ItemType.Adrenaline };
                if (!medicalList.Contains(player.CurrentItem.Type))
                {
                    player.ShowHint("<color=red>Возьмите в руки медицину (Аптечка, Обезболивающее, SCP-500, Адреналин)!</color>", 3f);
                    return;
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
                                        if (newItem is Exiled.API.Features.Items.Usable usable) usable.IsUsing = true;
                                    }
                                });
                            }
                        });
                        player.ShowHint("<color=green>Вы применили медицину на человека перед вами!</color>", 3f);
                    }
                }
            }

            else if (setting.SettingId == 6)
            {
                if (!player.IsHuman || !player.IsAlive) return;

                if (MeleeCooldowns.TryGetValue(player, out float cdEndTime) && UnityEngine.Time.time < cdEndTime)
                {
                    player.ShowHint($"<color=red>Удар на перезарядке! Подождите {Mathf.CeilToInt(cdEndTime - UnityEngine.Time.time)} сек.</color>", 2f);
                    return;
                }

                if (Physics.SphereCast(player.CameraTransform.position, 0.5f, player.CameraTransform.forward, out RaycastHit hit, 2.0f))
                {
                    Player struckTarget = Player.Get(hit.collider);
                    if (struckTarget != null && struckTarget != player && struckTarget.IsAlive)
                    {
                        MeleeCooldowns[player] = UnityEngine.Time.time + 3f;

                        struckTarget.Hurt(5f, DamageType.Custom);
                        struckTarget.ShowHint("<color=red>Вас ударили кулаком! (-5 ХП)</color>", 2f);

                        player.ShowHitMarker();
                    }
                }
            }
        }
    }
}

