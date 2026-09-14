using CommandSystem;
using Exiled.API.Enums;
using Exiled.API.Features;
using MEC;
using PlayerRoles;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace EyeCatcherPlugin
{
    [CommandHandler(typeof(ClientCommandHandler))]
    public class ContainCommand : ICommand
    {
        public string Command => "contain";
        public string[] Aliases => new string[] { };
        public string Description => "Консервация объектов SCP. Использование: .contain <173/049/096/939>";

        public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
        {
            Player player = Player.Get(sender);
            if (player == null || !player.IsHuman)
            {
                response = "Эту команду могут вводить только выжившие люди!";
                return false;
            }

            if (arguments.Count < 1)
            {
                response = "Укажите подтип команды: .contain 173, .contain 049, .contain 096 или .contain 939";
                return false;
            }

            string subCommand = arguments.At(0).ToLower();

            if (subCommand == "173")
            {
                if (Physics.SphereCast(player.CameraTransform.position, 0.4f, player.CameraTransform.forward, out RaycastHit hit, 4.5f))
                {
                    Player target173 = Player.List.FirstOrDefault(p => p.Role.Type == RoleTypeId.Scp173 && Vector3.Distance(p.Position, hit.point) <= 2.5f);

                    if (target173 != null)
                    {
                        if (!CageItem.Caged173s.Contains(target173))
                        {
                            response = "Этот SCP-173 должен быть предварительно закован в клетку!";
                            return false;
                        }

                        if (target173.CurrentRoom == null || target173.CurrentRoom.Type != RoomType.Hcz049)
                        {
                            response = "Вы должны закатить клетку с SCP-173 внутрь её комплекса содержания (Hcz049)!";
                            return false;
                        }

                        foreach (var door in target173.CurrentRoom.Doors)
                        {
                            if (door.Name.Contains("173") || door.Type == DoorType.Scp173Gate)
                            {
                                door.IsOpen = false;
                                door.Lock(float.MaxValue, DoorLockType.AdminCommand);
                            }
                        }

                        Room targetRoom173 = Room.List.FirstOrDefault(r => r.Type == RoomType.Hcz049);
                        if (targetRoom173 != null) player.Position = targetRoom173.Position + Vector3.up * 1f;

                        CageItem.DestroyCage(target173);
                        target173.Role.Set(RoleTypeId.Spectator);
                        Exiled.API.Features.Cassie.Message("SCP 1 7 3 SUCCESSFULLY CONTAINED AND SECURED", false, true, true);

                        response = "SCP-173 успешно утилизирован и заперт в камере!";
                        return true;
                    }
                }
                response = "Подойдите ближе и посмотрите прямо на клетку с SCP-173!";
                return false;
            }

            if (subCommand == "3114")
            {
                if (Physics.SphereCast(player.CameraTransform.position, 0.4f, player.CameraTransform.forward, out RaycastHit hit, 4.5f))
                {
                    Player skeleton = Player.Get(hit.collider);
                    if (skeleton != null && skeleton.Role.Type == RoleTypeId.Scp3114)
                    {
                        if (skeleton.CurrentRoom == null || skeleton.CurrentRoom.Type != RoomType.Lcz173)
                        {
                            response = "Вы должны находиться строго внутри тестовой камеры содержания (Pt00)!";
                            return false;
                        }

                        bool isSticky = false;
                        if (JointThickenerItem.BlockedSkeletons.TryGetValue(skeleton, out float endTime))
                        {
                            if (UnityEngine.Time.time < endTime)
                            {
                                isSticky = true;
                            }
                            else
                            {
                                JointThickenerItem.BlockedSkeletons.Remove(skeleton);
                            }
                        }

                        if (!isSticky)
                        {
                            response = "SCP-3114 не находится в состоянии липкости! Сначала поразите его из Загустителя швов.";
                            return false;
                        }

                        JointThickenerItem.BlockedSkeletons.Remove(skeleton);
                        skeleton.Role.Set(RoleTypeId.Spectator);
                        Exiled.API.Features.Cassie.Message("SCP 3 1 1 4 SUCCESSFULLY CONTAINED AND SECURED", false, true, true);

                        response = "SCP-3114 успешно утилизирован в состоянии липкости швов и отправлен в наблюдатели!";
                        return true;
                    }
                }
                response = "Посмотрите в упор на SCP-3114!";
                return false;
            }



            if (subCommand == "049")
            {
                if (Physics.SphereCast(player.CameraTransform.position, 0.4f, player.CameraTransform.forward, out RaycastHit hit, 4.5f))
                {
                    Player target049 = Player.Get(hit.collider);
                    if (target049 != null && target049.Role.Type == RoleTypeId.Scp049)
                    {
                        if (!EventHandlers.Cuffed049s.Contains(target049))
                        {
                            response = "Этого SCP-049 сначала необходимо сковать кандалами!";
                            return false;
                        }

                        if (target049.CurrentRoom == null || target049.CurrentRoom.Type != RoomType.Hcz049)
                        {
                            response = "SCP-049 должен находиться строго внутри своего блока содержания (Hcz049)!";
                            return false;
                        }

                        foreach (var door in target049.CurrentRoom.Doors)
                        {
                            if (door.Name.Contains("049") || door.Type == DoorType.Scp049Gate)
                            {
                                door.IsOpen = false;
                                door.Lock(float.MaxValue, DoorLockType.AdminCommand);
                            }
                        }

                        Room targetRoom049 = Room.List.FirstOrDefault(r => r.Type == RoomType.Hcz049);
                        if (targetRoom049 != null) player.Position = targetRoom049.Position + Vector3.up * 1f;

                        EventHandlers.Cuffed049s.Remove(target049);
                        target049.Role.Set(RoleTypeId.Spectator);
                        Exiled.API.Features.Cassie.Message("SCP 0 4 9 SUCCESSFULLY CONTAINED AND SECURED", false, true, true);

                        response = "SCP-049 успешно возвращен под стражу и заблокирован!";
                        return true;
                    }
                }
                response = "Посмотрите прямо на связанного SCP-049!";
                return false;
            }

            if (subCommand == "096")
            {
                if (player.Role.Side != Side.Mtf && player.Role.Type != RoleTypeId.FacilityGuard)
                {
                    response = "Эту команду для SCP-096 могут использовать только Охрана или МОГ!";
                    return false;
                }

                if (Physics.SphereCast(player.CameraTransform.position, 0.4f, player.CameraTransform.forward, out RaycastHit hit, 4.5f))
                {
                    Player target = Player.Get(hit.collider);
                    if (target != null && target.Role.Type == RoleTypeId.Scp096)
                    {
                        if (target.CurrentRoom != null && target.CurrentRoom.Type == RoomType.Hcz096)
                        {
                            foreach (var door in target.CurrentRoom.Doors)
                            {
                                door.IsOpen = false;
                                door.Lock(float.MaxValue, DoorLockType.AdminCommand);
                            }

                            target.Role.Set(RoleTypeId.Spectator);
                            Exiled.API.Features.Cassie.Message("SCP 0 9 6 SUCCESSFULLY CONTAINED AND SECURED", false, true, true);

                            response = "SCP-096 успешно заблокирован в своей камере!";
                            return true;
                        }
                        else
                        {
                            response = "SCP-096 должен находиться строго внутри своей камеры содержания (Hcz096)!";
                            return false;
                        }
                    }
                }
                response = "Вы должны смотреть в упор на SCP-096!";
                return false;
            }

            if (subCommand == "939")
            {
                if (Physics.SphereCast(player.CameraTransform.position, 0.4f, player.CameraTransform.forward, out RaycastHit hit, 4.5f))
                {
                    Player dummyDog = Player.Get(hit.collider);
                    if (dummyDog != null && Tranquilizer.ControlledDogs.ContainsKey(dummyDog))
                    {
                        if (dummyDog.CurrentRoom != null && dummyDog.CurrentRoom.Type == RoomType.Hcz939)
                        {
                            var data = Tranquilizer.ControlledDogs[dummyDog];

                            MEC.Timing.KillCoroutines(data.WakeTimerHandle);
                            MEC.Timing.KillCoroutines(data.AICoroutineHandle);

                            foreach (var pair in new System.Collections.Generic.Dictionary<Player, Player>(Tranquilizer.Carrying))
                            {
                                if (pair.Value == dummyDog)
                                {
                                    Tranquilizer.RemoveDogControl(pair.Key);
                                    break;
                                }
                            }

                            Tranquilizer.ControlledDogs.Remove(dummyDog);
                            dummyDog.Role.Set(RoleTypeId.Spectator);
                            if (dummyDog is Npc npc) npc.Destroy();

                            Exiled.API.Features.Cassie.Message("SCP 9 3 9 SUCCESSFULLY CONTAINED AND SECURED", false, true, true);

                            response = "SCP-939 успешно деактивирована в зоне спавна!";
                            return true;
                        }
                        else
                        {
                            response = "Вы должны привести послушную SCP-939 в её родную комнату содержания (Hcz939)!";
                            return false;
                        }
                    }
                }
                response = "Вы должны смотреть на усыпленную (послушную) SCP-939!";
                return false;
            }

            response = "Неизвестный параметр. Используйте: .contain 173, .contain 049, .contain 096 или .contain 939";
            return false;
        }
        [CommandHandler(typeof(ClientCommandHandler))]
        public class DoctorGiveCardCommand : ICommand
        {
            public string Command => "givecard";
            public string[] Aliases => new string[] { };
            public string Description => "Забрать карточку с пола (Доступно только SCP-049)";

            public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
            {
                Player player = Player.Get(sender);
                if (player == null || player.Role.Type != RoleTypeId.Scp049)
                {
                    response = "Эту команду может использовать только Чумной Доктор!";
                    return false;
                }

                if (Physics.SphereCast(player.CameraTransform.position, 0.45f, player.CameraTransform.forward, out RaycastHit hit, 4.5f))
                {
                    Exiled.API.Features.Pickups.Pickup pickup = Exiled.API.Features.Pickups.Pickup.Get(hit.collider.gameObject);

                    if (pickup == null && hit.collider.transform.parent != null)
                        pickup = Exiled.API.Features.Pickups.Pickup.Get(hit.collider.transform.parent.gameObject);

                    if (pickup != null)
                    {
                        ItemType type = pickup.Type;

                        if (type.ToString().Contains("Keycard") && type != ItemType.KeycardChaosInsurgency)
                        {
                            pickup.Destroy();
                            var item = player.AddItem(type);
                            Timing.CallDelayed(0.1f, () =>
                            {
                                if (player != null && player.IsAlive)
                                    player.CurrentItem = item;
                            });

                            response = $"Вы успешно подобрали карту {type}!";
                            return true;
                        }
                    }
                }

                response = "Посмотрите прямо на лежащую ключ-карту (Устройства Повстанцев Хаоса брать нельзя)!";
                return false;
            }
        }


        [CommandHandler(typeof(ClientCommandHandler))]
        public class SearchCommand : ICommand
        {
            public string Command => "search";
            public string[] Aliases => new string[] { "обыск" };
            public string Description => "Обыскать игрока перед вами. У вас должно быть оружие, у цели — нет предметов в руках.";

            public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
            {
                Player player = Player.Get(sender);
                if (player == null || !player.IsHuman)
                {
                    response = "Команда доступна только живым людям!";
                    return false;
                }

                if (player.CurrentItem == null || !player.CurrentItem.IsWeapon)
                {
                    response = "Вы должны держать в руках оружие, чтобы провести обыск!";
                    return false;
                }

                if (Physics.SphereCast(player.CameraTransform.position, 0.4f, player.CameraTransform.forward, out RaycastHit hit, 3.5f))
                {
                    Player target = Player.Get(hit.collider);
                    if (target != null && target.IsHuman && target != player)
                    {
                        if (target.CurrentItem != null)
                        {
                            response = "Цель оказывает сопротивление или держит что-то в руках! Руки цели должны быть пусты.";
                            return false;
                        }

                        MEC.Timing.RunCoroutine(SearchProcess(player, target));
                        response = "Вы начали обыск... Не отводите взгляд от цели в течение 3 секунд.";
                        return true;
                    }
                }

                response = "Подойдите вплотную и посмотрите прямо на безоружного человека!";
                return false;
            }

            [CommandHandler(typeof(ClientCommandHandler))]
            public class AHelpCommand : ICommand
            {
                public string Command => "ahelp";
                public string[] Aliases => new string[] { "report", "жалоба" };
                public string Description => "Вызвать администрацию комплекса через Discord. Использование: .ahelp <причина>";

                public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
                {
                    Player player = Player.Get(sender);
                    if (player == null)
                    {
                        response = "Игрок не найден.";
                        return false;
                    }

                    if (arguments.Count < 1)
                    {
                        response = "Вы не указали причину вызова! Пример: .ahelp Игрок 173 забагал текстуру";
                        return false;
                    }

                    string reason = string.Join(" ", arguments);

                    System.IO.Directory.CreateDirectory(System.IO.Path.Combine(Paths.Configs, "EyeCatcherPlugin", "Shared", "Commands"));
                    string id = DateTime.UtcNow.Ticks.ToString();
                    var payload = new
                    {
                        Command = "ahelp_request",
                        Args = new Dictionary<string, string>
                {
                    { "player", player.Nickname },
                    { "steamid", player.UserId },
                    { "ip", player.IPAddress },
                    { "reason", reason }
                }
                    };

                    string path = System.IO.Path.Combine(Paths.Configs, "EyeCatcherPlugin", "Shared", "Commands", "ahelp_" + id + ".json");
                    System.IO.File.WriteAllText(path, Newtonsoft.Json.JsonConvert.SerializeObject(payload, Newtonsoft.Json.Formatting.Indented));

                    response = "<color=green>Ваш запрос успешно доставлен в Discord-терминал администрации комплекса!</color>";
                    return true;
                }
            }


            public static IEnumerator<float> SearchProcess(Player searcher, Player target)
            {
                searcher.ShowHint("<color=orange>Обыск начался, подождите 3 секунды...</color>", 3f);
                target.ShowHint("<color=red>Вас обыскивают под дулом оружия! Не двигайтесь.</color>", 3f);

                yield return Timing.WaitForSeconds(3f);

                if (searcher == null || target == null || !searcher.IsAlive || !target.IsAlive) yield break;

                if (searcher.CurrentItem == null || !searcher.CurrentItem.IsWeapon || target.CurrentItem != null || Vector3.Distance(searcher.Position, target.Position) > 4f)
                {
                    searcher.ShowHint("<color=red>Обыск сорван!</color>", 3f);
                    yield break;
                }

                System.Text.StringBuilder sb = new System.Text.StringBuilder();
                sb.AppendLine($"<color=yellow>=== СОДЕРЖИМОЕ КАРМАНОВ {target.Nickname} ===</color>");

                if (target.Items.Count == 0)
                {
                    sb.AppendLine("<color=white>Карманы пусты.</color>");
                }
                else
                {
                    foreach (var item in target.Items)
                    {
                        sb.AppendLine($"<color=cyan>• {item.Type}</color>");
                    }
                }

                searcher.ShowHint(sb.ToString(), 7f);
            }
        }
    }
}