using Exiled.API.Enums;
using Exiled.API.Features;
using Exiled.API.Features.Items;
using Exiled.API.Features.Items.Keycards;
using Exiled.API.Interfaces.Keycards;
using Exiled.CustomRoles.API;
using Exiled.CustomRoles.API.Features;
using Exiled.Events.EventArgs.Server;
using Interactables.Interobjects.DoorUtils;
using MEC;
using PlayerRoles;
using Respawning;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace EyeCatcherPlugin.Roles
{
    public static class ZoneRoleManager
    {
        private static readonly List<CustomRole> AllRoles = new List<CustomRole>(20);
        private static bool _wavesBlocked;


        public static void RegisterAll()
        {
            if (AllRoles.Count > 0)
                return;

            AllRoles.Add(new CentralCommand());
            AllRoles.Add(new SiteDirector());
            AllRoles.Add(new EthicsInspector());

            AllRoles.Add(new ResearchDirector());
            AllRoles.Add(new ProjectLead());
            AllRoles.Add(new ZoneScientist());
            AllRoles.Add(new JuniorScientist());

            AllRoles.Add(new SecurityChief());
            AllRoles.Add(new SecurityLieutenant());
            AllRoles.Add(new Supervisor());
            AllRoles.Add(new SecurityOfficer());
            AllRoles.Add(new SecurityCadet());

            AllRoles.Add(new ChiefPhysician());
            AllRoles.Add(new Surgeon());
            AllRoles.Add(new Therapist());
            AllRoles.Add(new Orderly());
            AllRoles.Add(new Intern());

            AllRoles.Add(new ChiefEngineer());
            AllRoles.Add(new ZoneEngineer());
            AllRoles.Add(new LogisticsOfficer());
            AllRoles.Add(new MaintenanceTech());

            for (int i = 0; i < AllRoles.Count; i++)
                AllRoles[i].Register();

            if (!_wavesBlocked)
            {
                Exiled.Events.Handlers.Server.RespawningTeam += OnRespawningTeam;
                _wavesBlocked = true;
            }

            Log.Info("[ZoneRoles] Зарегистрировано " + AllRoles.Count + " кастомных ролей.");
        }

        public static void UnregisterAll()
        {
            for (int i = 0; i < AllRoles.Count; i++)
                AllRoles[i].Unregister();

            AllRoles.Clear();

            if (_wavesBlocked)
            {
                Exiled.Events.Handlers.Server.RespawningTeam -= OnRespawningTeam;
                _wavesBlocked = false;
            }
        }

        private static void OnRespawningTeam(RespawningTeamEventArgs ev)
        {
            ev.IsAllowed = false;
        }

        public static bool IsZoneStaff(Player player)
        {
            if (player == null)
                return false;

            for (int i = 0; i < AllRoles.Count; i++)
            {
                if (AllRoles[i].Check(player))
                    return true;
            }

            return false;
        }
    }

    public abstract class ZoneRoleBase : CustomRole
    {
        public abstract string BadgeColor { get; }
        public abstract DoorPermissionFlags CardPermissions { get; }
        public abstract Color CardMainColor { get; }
        public abstract Color CardPermColor { get; }

        public virtual ItemType CardBaseType
        {
            get { return ItemType.KeycardCustomSite02; }
        }


        public virtual string CardDisplayName
        {
            get { return Name; }
        }


        public override string CustomInfo
        {
            get => Name;
            set { }
        }

        public override int MaxHealth { get; set; } = 100;
        public override float SpawnChance { get; set; } = 0f;
        public override bool IgnoreSpawnSystem { get; set; } = true;
        public override bool KeepInventoryOnSpawn { get; set; } = false;
        public override bool RemovalKillsPlayer { get; set; } = false;

        public override List<string> Inventory { get; set; } = new List<string>();

        public override Dictionary<AmmoType, ushort> Ammo { get; set; } = new Dictionary<AmmoType, ushort>();

        protected override void RoleAdded(Player player)
        {
            if (player == null)
                return;

            player.CustomInfo = CustomInfo;

            Timing.CallDelayed(0.3f, () =>
{
    if (player == null || !player.IsAlive || !Check(player))
        return;

    if (this is CentralCommand)
        player.CustomName = "Оперуполномоченный ЦК";
    else if (this is SiteDirector)
        player.CustomName = RpNameGenerator.GetDirectorName();
    else if (this is EthicsInspector)
        player.CustomName = RpNameGenerator.GetStaffName("Инспектор КЭ");
    else
    {
        player.CustomName = RpNameGenerator.GetStaffName(Name);
    }

    GivePersonalKeycard(player);
});
        }
        protected override void RoleRemoved(Player player)
        {
            if (player != null)
                player.CustomInfo = string.Empty;
        }

        private void GivePersonalKeycard(Player player)
        {
            if (player == null || player.Inventory == null)
                return;

            if (CardPermissions == DoorPermissionFlags.None)
                return;

            var cardsToRemove = player.Items.Where(i => i.IsKeycard).ToList();
            foreach (var card in cardsToRemove)
                player.RemoveItem(card);

            Timing.CallDelayed(0.1f, () =>
            {
                if (player == null || !player.IsAlive) return;

                Exiled.API.Features.Items.Item item = player.AddItem(CardBaseType);

                if (item is Exiled.API.Features.Items.Keycards.CustomKeycardItem customCard)
                {
                    customCard.Permissions = (Exiled.API.Enums.KeycardPermissions)CardPermissions;
                    customCard.PermissionsColor = CardPermColor;
                    customCard.Color = CardMainColor;
                    customCard.ItemName = CardDisplayName;
                    if (customCard is Exiled.API.Interfaces.Keycards.INameTagKeycard nameTagKeycard)
                    {
                        nameTagKeycard.NameTag = player.CustomName;
                    }

                    if (customCard is Exiled.API.Interfaces.Keycards.ILabelKeycard labelKeycard)
                    {
                        labelKeycard.Label = CardDisplayName;
                        labelKeycard.LabelColor = CardPermColor;
                    }

                    customCard.Resync();
                }
                else
                {
                    Log.Warn($"[ZoneRoles] Предмет {item?.Type} не является CustomKeycardItem!");
                }
            });
        }
    }

    public sealed class CentralCommand : ZoneRoleBase
    {
        public override uint Id { get; set; } = 1901;
        public override ItemType CardBaseType => ItemType.KeycardCustomManagement;
        public override string Name { get; set; } = "Центральное Командование";
        public override string Description { get; set; } = "Высшее руководство Зоны 19-02.";
        public override RoleTypeId Role { get; set; } = RoleTypeId.Scientist;

        public override string BadgeColor { get { return "#FFD700"; } }

        public override DoorPermissionFlags CardPermissions
        {
            get
            {
                return DoorPermissionFlags.Checkpoints | DoorPermissionFlags.ExitGates |
                       DoorPermissionFlags.Intercom | DoorPermissionFlags.AlphaWarhead |
                       DoorPermissionFlags.ContainmentLevelOne | DoorPermissionFlags.ContainmentLevelTwo |
                       DoorPermissionFlags.ContainmentLevelThree | DoorPermissionFlags.ArmoryLevelOne |
                       DoorPermissionFlags.ArmoryLevelTwo | DoorPermissionFlags.ArmoryLevelThree;
            }
        }

        public override Color CardMainColor { get { return new Color(1f, 0.84f, 0f); } }
        public override Color CardPermColor { get { return Color.white; } }


        public override List<string> Inventory { get; set; } = new List<string>
        {


            "GunCOM18",


            "Radio",
            "Flashlight"
        };

        public override Dictionary<AmmoType, ushort> Ammo { get; set; } = new Dictionary<AmmoType, ushort>
        {
            { AmmoType.Nato556, 120 },
            { AmmoType.Nato9, 60 }
        };
    }

    public sealed class SiteDirector : ZoneRoleBase
    {
        public override uint Id { get; set; } = 1902;
        public override ItemType CardBaseType => ItemType.KeycardCustomManagement;
        public override string Name { get; set; } = "Директор";
        public override string Description { get; set; } = "Директор объекта.";
        public override RoleTypeId Role { get; set; } = RoleTypeId.Scientist;

        public override string BadgeColor { get { return "#FF4500"; } }

        public override DoorPermissionFlags CardPermissions
        {
            get
            {
                return DoorPermissionFlags.Checkpoints | DoorPermissionFlags.ExitGates |
                       DoorPermissionFlags.Intercom | DoorPermissionFlags.AlphaWarhead |
                       DoorPermissionFlags.ContainmentLevelOne | DoorPermissionFlags.ContainmentLevelTwo |
                       DoorPermissionFlags.ContainmentLevelThree | DoorPermissionFlags.ArmoryLevelTwo;
            }
        }

        public override Color CardMainColor { get { return new Color(1f, 0.27f, 0f); } }
        public override Color CardPermColor { get { return Color.yellow; } }

        public override List<string> Inventory { get; set; } = new List<string>
        {



            "Medkit",


            "Radio",
            "Flashlight"
        };

        public override Dictionary<AmmoType, ushort> Ammo { get; set; } = new Dictionary<AmmoType, ushort>
        {
            { AmmoType.Nato9, 1 },
            { AmmoType.Nato556, 1 }
        };
    }

    public sealed class EthicsInspector : ZoneRoleBase
    {
        public override uint Id { get; set; } = 1903;
        public override ItemType CardBaseType => ItemType.KeycardCustomManagement;
        public override string Name { get; set; } = "Инспектор Комитета по Этике";
        public override string Description { get; set; } = "Представитель Комитета по Этике.";
        public override RoleTypeId Role { get; set; } = RoleTypeId.Scientist;

        public override string BadgeColor { get { return "#DA70D6"; } }

        public override DoorPermissionFlags CardPermissions
        {
            get
            {
                return DoorPermissionFlags.Checkpoints | DoorPermissionFlags.ExitGates |
                       DoorPermissionFlags.Intercom | DoorPermissionFlags.ContainmentLevelTwo;
            }
        }

        public override Color CardMainColor { get { return new Color(0.85f, 0.44f, 0.84f); } }
        public override Color CardPermColor { get { return Color.magenta; } }


        public override List<string> Inventory { get; set; } = new List<string>
        {


            "Medkit",
            "Painkillers",
            "Radio",
            "Flashlight"
        };

        public override Dictionary<AmmoType, ushort> Ammo { get; set; } = new Dictionary<AmmoType, ushort>
        {
            { AmmoType.Nato9, 40 }
        };
    }

    public sealed class ResearchDirector : ZoneRoleBase
    {
        public override uint Id { get; set; } = 1910;
        public override string Name { get; set; } = "Научный Руководитель";
        public override string Description { get; set; } = "Руководит всеми научными проектами.";
        public override RoleTypeId Role { get; set; } = RoleTypeId.Scientist;

        public override string BadgeColor { get { return "#00BFFF"; } }

        public override DoorPermissionFlags CardPermissions
        {
            get
            {
                return DoorPermissionFlags.Checkpoints | DoorPermissionFlags.ExitGates |
                       DoorPermissionFlags.Intercom | DoorPermissionFlags.ContainmentLevelOne |
                       DoorPermissionFlags.ContainmentLevelTwo | DoorPermissionFlags.ContainmentLevelThree;
            }
        }

        public override Color CardMainColor { get { return new Color(0f, 0.75f, 1f); } }
        public override Color CardPermColor { get { return Color.cyan; } }


        public override List<string> Inventory { get; set; } = new List<string>
        {


            "Medkit",

            "Adrenaline",
            "Radio",
            "Flashlight"
        };

        public override Dictionary<AmmoType, ushort> Ammo { get; set; } = new Dictionary<AmmoType, ushort>
        {
            { AmmoType.Nato9, 1 }
        };
    }

    public sealed class ProjectLead : ZoneRoleBase
    {
        public override uint Id { get; set; } = 1911;
        public override string Name { get; set; } = "Руководитель Проекта";
        public override string Description { get; set; } = "Ответственный за конкретный проект.";
        public override RoleTypeId Role { get; set; } = RoleTypeId.Scientist;

        public override string BadgeColor { get { return "#1E90FF"; } }

        public override DoorPermissionFlags CardPermissions
        {
            get
            {
                return DoorPermissionFlags.Checkpoints | DoorPermissionFlags.ContainmentLevelOne |
                       DoorPermissionFlags.ContainmentLevelTwo;
            }
        }

        public override Color CardMainColor { get { return new Color(0.12f, 0.56f, 1f); } }
        public override Color CardPermColor { get { return Color.blue; } }


        public override List<string> Inventory { get; set; } = new List<string>
        {

            "Medkit",
            "Painkillers",
            "Radio",
            "Flashlight",
            "Загуститель швов"
        };


        public override Dictionary<AmmoType, ushort> Ammo { get; set; } = new Dictionary<AmmoType, ushort>
        {
            { AmmoType.Nato9, 30 }
        };
    }

    public sealed class ZoneScientist : ZoneRoleBase
    {
        public override uint Id { get; set; } = 1912;
        public override string Name { get; set; } = "Научный Сотрудник";
        public override string Description { get; set; } = "Обычный научный персонал.";
        public override RoleTypeId Role { get; set; } = RoleTypeId.Scientist;

        public override string BadgeColor { get { return "#87CEEB"; } }

        public override DoorPermissionFlags CardPermissions
        {
            get { return DoorPermissionFlags.Checkpoints | DoorPermissionFlags.ContainmentLevelOne; }
        }

        public override Color CardMainColor { get { return new Color(0.53f, 0.81f, 0.92f); } }
        public override Color CardPermColor { get { return Color.white; } }


        public override List<string> Inventory { get; set; } = new List<string>
        {
            "Medkit",
            "Painkillers",
            "Radio",

        };

        public override Dictionary<AmmoType, ushort> Ammo { get; set; } = new Dictionary<AmmoType, ushort>();
    }

    public sealed class JuniorScientist : ZoneRoleBase
    {
        public override uint Id { get; set; } = 1913;
        public override string Name { get; set; } = "Младший Научный Сотрудник";
        public override string Description { get; set; } = "Стажёр научного отдела.";
        public override RoleTypeId Role { get; set; } = RoleTypeId.Scientist;

        public override string BadgeColor { get { return "#ADD8E6"; } }

        public override DoorPermissionFlags CardPermissions
        {
            get { return DoorPermissionFlags.ContainmentLevelOne; }
        }

        public override Color CardMainColor { get { return new Color(0.68f, 0.85f, 0.90f); } }
        public override Color CardPermColor { get { return Color.gray; } }


        public override List<string> Inventory { get; set; } = new List<string>
        {
            "Painkillers",
            "Flashlight"
        };

        public override Dictionary<AmmoType, ushort> Ammo { get; set; } = new Dictionary<AmmoType, ushort>();
    }

    public sealed class SecurityChief : ZoneRoleBase
    {
        public override uint Id { get; set; } = 1920;
        public override ItemType CardBaseType => ItemType.KeycardCustomTaskForce;
        public override string Name { get; set; } = "Глава Службы Безопасности";
        public override string Description { get; set; } = "Командует всей СБ.";
        public override RoleTypeId Role { get; set; } = RoleTypeId.NtfCaptain;

        public override string BadgeColor { get { return "#DC143C"; } }

        public override DoorPermissionFlags CardPermissions
        {
            get
            {
                return DoorPermissionFlags.Checkpoints | DoorPermissionFlags.ExitGates |
                       DoorPermissionFlags.Intercom | DoorPermissionFlags.ArmoryLevelOne |
                       DoorPermissionFlags.ArmoryLevelTwo | DoorPermissionFlags.ArmoryLevelThree |
                       DoorPermissionFlags.ContainmentLevelTwo;
            }
        }

        public override Color CardMainColor { get { return new Color(0.86f, 0.08f, 0.24f); } }
        public override Color CardPermColor { get { return Color.red; } }


        public override List<string> Inventory { get; set; } = new List<string>
        {
            "ArmorHeavy",
            "GunFRMG0",
            "GrenadeHE",

            "Medkit",
            "Adrenaline",
            "Radio",
            "Сдерживающая Клетка SCP-173"
        };

        public override Dictionary<AmmoType, ushort> Ammo { get; set; } = new Dictionary<AmmoType, ushort>
        {
            { AmmoType.Nato556, 150 },
            { AmmoType.Nato9, 60 }
        };
    }

    public sealed class SecurityLieutenant : ZoneRoleBase
    {
        public override uint Id { get; set; } = 1921;
        public override ItemType CardBaseType => ItemType.KeycardCustomTaskForce;
        public override string Name { get; set; } = "Лейтенант";
        public override string Description { get; set; } = "Офицер среднего звена СБ.";
        public override RoleTypeId Role { get; set; } = RoleTypeId.NtfSpecialist;

        public override string BadgeColor { get { return "#B22222"; } }

        public override DoorPermissionFlags CardPermissions
        {
            get
            {
                return DoorPermissionFlags.Checkpoints | DoorPermissionFlags.ExitGates |
                       DoorPermissionFlags.ArmoryLevelOne | DoorPermissionFlags.ArmoryLevelTwo;
            }
        }

        public override Color CardMainColor { get { return new Color(0.70f, 0.13f, 0.13f); } }
        public override Color CardPermColor { get { return Color.red; } }


        public override List<string> Inventory { get; set; } = new List<string>
        {
            "ArmorCombat",
            "GunCrossvec",


            "Medkit",
            "Adrenaline",
            "Radio",

            "Tranquilizer COM-15",
            "Anti-096"
        };

        public override Dictionary<AmmoType, ushort> Ammo { get; set; } = new Dictionary<AmmoType, ushort>
        {
            { AmmoType.Nato9, 120 },
            { AmmoType.Nato556, 40 }
        };
    }

    public sealed class Supervisor : ZoneRoleBase
    {
        public override uint Id { get; set; } = 1922;
        public override ItemType CardBaseType => ItemType.KeycardCustomTaskForce;
        public override string Name { get; set; } = "Супервайзер СБ"; public override string Description { get; set; } = "Опытный сотрудник СБ с кандалами.";
        public override RoleTypeId Role { get; set; } = RoleTypeId.NtfPrivate;

        public override string BadgeColor { get { return "#CD5C5C"; } }

        public override DoorPermissionFlags CardPermissions
        {
            get { return DoorPermissionFlags.Checkpoints | DoorPermissionFlags.ArmoryLevelOne; }
        }

        public override Color CardMainColor { get { return new Color(0.80f, 0.36f, 0.36f); } }
        public override Color CardPermColor { get { return Color.white; } }

        public override List<string> Inventory { get; set; } = new List<string>
        {
            "ArmorCombat",
            "GunFSP9",

            "Medkit",
            "Painkillers",
            "Radio",
            "Flashlight",
            "Кандалы SCP-049"
        };

        public override Dictionary<AmmoType, ushort> Ammo { get; set; } = new Dictionary<AmmoType, ushort>
        {
            { AmmoType.Nato9, 90 }
        };
    }


    public sealed class SecurityOfficer : ZoneRoleBase
    {
        public override uint Id { get; set; } = 1923;
        public override ItemType CardBaseType => ItemType.KeycardCustomMetalCase;
        public override string Name { get; set; } = "Офицер СБ";
        public override string Description { get; set; } = "Обычный сотрудник службы безопасности.";
        public override RoleTypeId Role { get; set; } = RoleTypeId.NtfPrivate;

        public override string BadgeColor { get { return "#F08080"; } }

        public override DoorPermissionFlags CardPermissions
        {
            get { return DoorPermissionFlags.Checkpoints; }
        }

        public override Color CardMainColor { get { return new Color(0.94f, 0.50f, 0.50f); } }
        public override Color CardPermColor { get { return Color.white; } }


        public override List<string> Inventory { get; set; } = new List<string>
        {
            "ArmorLight",
            "GunFSP9",
            "GunCOM15",
            "Medkit",
            "Radio",
            "Flashlight"
        };

        public override Dictionary<AmmoType, ushort> Ammo { get; set; } = new Dictionary<AmmoType, ushort>
        {
            { AmmoType.Nato9, 60 }
        };
    }

    public sealed class SecurityCadet : ZoneRoleBase
    {
        public override uint Id { get; set; } = 1924;
        public override ItemType CardBaseType => ItemType.KeycardCustomMetalCase;
        public override string Name { get; set; } = "Кадет СБ";
        public override string Description { get; set; } = "Новичок службы безопасности.";
        public override RoleTypeId Role { get; set; } = RoleTypeId.NtfSergeant;

        public override string BadgeColor { get { return "#FFA07A"; } }

        public override DoorPermissionFlags CardPermissions
        {
            get
            {
                return DoorPermissionFlags.Checkpoints |
                       DoorPermissionFlags.ArmoryLevelOne |
                       DoorPermissionFlags.ArmoryLevelTwo;
            }
        }


        public override Color CardMainColor { get { return new Color(1f, 0.63f, 0.48f); } }
        public override Color CardPermColor { get { return Color.gray; } }


        public override List<string> Inventory { get; set; } = new List<string>
        {
            "ArmorLight",
            "GunCOM15",
            "Painkillers",
            "Radio",
            "Flashlight"
        };

        public override Dictionary<AmmoType, ushort> Ammo { get; set; } = new Dictionary<AmmoType, ushort>
        {
            { AmmoType.Nato9, 30 }
        };
    }

    public sealed class ChiefPhysician : ZoneRoleBase
    {
        public override uint Id { get; set; } = 1930;
        public override string Name { get; set; } = "Главный Врач";
        public override string Description { get; set; } = "Руководитель медицинского отдела.";
        public override RoleTypeId Role { get; set; } = RoleTypeId.Scientist;

        public override string BadgeColor { get { return "#32CD32"; } }

        public override DoorPermissionFlags CardPermissions
        {
            get
            {
                return DoorPermissionFlags.Checkpoints | DoorPermissionFlags.ContainmentLevelOne |
                       DoorPermissionFlags.Intercom;
            }
        }

        public override Color CardMainColor { get { return new Color(0.20f, 0.80f, 0.20f); } }
        public override Color CardPermColor { get { return Color.green; } }


        public override List<string> Inventory { get; set; } = new List<string>
        {

            "Medkit",
            "Medkit",
            "Adrenaline",
            "Painkillers",
            "Radio",
            "Flashlight"
        };

        public override Dictionary<AmmoType, ushort> Ammo { get; set; } = new Dictionary<AmmoType, ushort>
        {
            { AmmoType.Nato9, 30 }
        };
    }

    public sealed class Surgeon : ZoneRoleBase
    {
        public override uint Id { get; set; } = 1931;
        public override string Name { get; set; } = "Хирург";
        public override string Description { get; set; } = "Оперирующий врач.";
        public override RoleTypeId Role { get; set; } = RoleTypeId.Scientist;

        public override string BadgeColor { get { return "#3CB371"; } }

        public override DoorPermissionFlags CardPermissions
        {
            get { return DoorPermissionFlags.Checkpoints | DoorPermissionFlags.ContainmentLevelOne; }
        }

        public override Color CardMainColor { get { return new Color(0.24f, 0.70f, 0.44f); } }
        public override Color CardPermColor { get { return Color.green; } }


        public override List<string> Inventory { get; set; } = new List<string>
        {
            "Medkit",
            "Medkit",

            "Adrenaline",
            "Painkillers",
            "Radio",

        };

        public override Dictionary<AmmoType, ushort> Ammo { get; set; } = new Dictionary<AmmoType, ushort>();
    }

    public sealed class Therapist : ZoneRoleBase
    {
        public override uint Id { get; set; } = 1932;
        public override string Name { get; set; } = "Терапевт";
        public override string Description { get; set; } = "Лечащий врач.";
        public override RoleTypeId Role { get; set; } = RoleTypeId.Scientist;

        public override string BadgeColor { get { return "#66CDAA"; } }

        public override DoorPermissionFlags CardPermissions
        {
            get { return DoorPermissionFlags.ContainmentLevelOne; }
        }

        public override Color CardMainColor { get { return new Color(0.40f, 0.80f, 0.67f); } }
        public override Color CardPermColor { get { return Color.white; } }


        public override List<string> Inventory { get; set; } = new List<string>
        {
            "Medkit",
            "Painkillers",
            "Adrenaline",
            "Radio",

        };

        public override Dictionary<AmmoType, ushort> Ammo { get; set; } = new Dictionary<AmmoType, ushort>();
    }

    public sealed class Orderly : ZoneRoleBase
    {
        public override uint Id { get; set; } = 1933;
        public override string Name { get; set; } = "Медбрат";
        public override string Description { get; set; } = "Младший медицинский персонал.";
        public override RoleTypeId Role { get; set; } = RoleTypeId.Scientist;

        public override string BadgeColor { get { return "#98FB98"; } }

        public override DoorPermissionFlags CardPermissions
        {
            get { return DoorPermissionFlags.None; }
        }

        public override Color CardMainColor { get { return new Color(0.60f, 0.98f, 0.60f); } }
        public override Color CardPermColor { get { return Color.gray; } }


        public override List<string> Inventory { get; set; } = new List<string>
        {
            "Medkit",
            "Painkillers",
            "Flashlight"
        };

        public override Dictionary<AmmoType, ushort> Ammo { get; set; } = new Dictionary<AmmoType, ushort>();
    }

    public sealed class Intern : ZoneRoleBase
    {
        public override uint Id { get; set; } = 1934;
        public override string Name { get; set; } = "Интерн";
        public override string Description { get; set; } = "Стажёр медицинского отдела.";
        public override RoleTypeId Role { get; set; } = RoleTypeId.Scientist;

        public override string BadgeColor { get { return "#F0FFF0"; } }

        public override DoorPermissionFlags CardPermissions
        {
            get { return DoorPermissionFlags.None; }
        }

        public override Color CardMainColor { get { return new Color(0.94f, 1f, 0.94f); } }
        public override Color CardPermColor { get { return Color.gray; } }


        public override List<string> Inventory { get; set; } = new List<string>
        {
            "Painkillers",
            "Flashlight",
            "Загуститель швов"
        };


        public override Dictionary<AmmoType, ushort> Ammo { get; set; } = new Dictionary<AmmoType, ushort>();
    }

    public sealed class ChiefEngineer : ZoneRoleBase
    {
        public override uint Id { get; set; } = 1940;
        public override string Name { get; set; } = "Главный Инженер";
        public override string Description { get; set; } = "Руководитель инженерного отдела.";
        public override RoleTypeId Role { get; set; } = RoleTypeId.FacilityGuard;

        public override string BadgeColor { get { return "#FF8C00"; } }

        public override bool KeepPositionOnSpawn { get; set; } = false;

        protected override void RoleAdded(Player player)
        {
            base.RoleAdded(player);

            Timing.CallDelayed(0.45f, () =>
            {
                if (player == null || !player.IsAlive) return;

                var hczRooms = Room.List
                    .Where(r => r.Zone == ZoneType.HeavyContainment && r.Type != RoomType.HczNuke)
                    .ToList();

                if (hczRooms.Count == 0) return;

                Room room = hczRooms[UnityEngine.Random.Range(0, hczRooms.Count)];
                player.Position = room.Position + Vector3.up * 1.3f;
            });
        }

        public override DoorPermissionFlags CardPermissions
        {
            get
            {
                return DoorPermissionFlags.Checkpoints | DoorPermissionFlags.ExitGates |
                       DoorPermissionFlags.ContainmentLevelOne | DoorPermissionFlags.ContainmentLevelTwo |
                       DoorPermissionFlags.ArmoryLevelOne;
            }
        }

        public override Color CardMainColor { get { return new Color(1f, 0.55f, 0f); } }
        public override Color CardPermColor { get { return Color.yellow; } }


        public override List<string> Inventory { get; set; } = new List<string>
        {

            "Medkit",
            "Adrenaline",
            "Radio",
            "Flashlight",
            "KeycardJanitor"         };

        public override Dictionary<AmmoType, ushort> Ammo { get; set; } = new Dictionary<AmmoType, ushort>
        {
            { AmmoType.Nato9, 50 }
        };
    }

    public sealed class ZoneEngineer : ZoneRoleBase
    {
        public override uint Id { get; set; } = 1941;
        public override string Name { get; set; } = "Инженер";
        public override string Description { get; set; } = "Инженер комплекса.";
        public override RoleTypeId Role { get; set; } = RoleTypeId.FacilityGuard;

        public override string BadgeColor { get { return "#FFA500"; } }

        public override bool KeepPositionOnSpawn { get; set; } = false;

        protected override void RoleAdded(Player player)
        {
            base.RoleAdded(player);

            Timing.CallDelayed(0.45f, () =>
            {
                if (player == null || !player.IsAlive) return;

                var hczRooms = Room.List
                    .Where(r => r.Zone == ZoneType.HeavyContainment && r.Type != RoomType.HczNuke)
                    .ToList();

                if (hczRooms.Count == 0) return;

                Room room = hczRooms[UnityEngine.Random.Range(0, hczRooms.Count)];
                player.Position = room.Position + Vector3.up * 1.3f;
            });
        }

        public override DoorPermissionFlags CardPermissions
        {
            get { return DoorPermissionFlags.Checkpoints | DoorPermissionFlags.ContainmentLevelOne; }
        }

        public override Color CardMainColor { get { return new Color(1f, 0.65f, 0f); } }
        public override Color CardPermColor { get { return Color.yellow; } }


        public override List<string> Inventory { get; set; } = new List<string>
        {

            "Medkit",
            "Radio",
            "Flashlight"
        };

        public override Dictionary<AmmoType, ushort> Ammo { get; set; } = new Dictionary<AmmoType, ushort>
        {
            { AmmoType.Nato9, 30 }
        };
    }

    public sealed class LogisticsOfficer : ZoneRoleBase
    {
        public override uint Id { get; set; } = 1942;
        public override string Name { get; set; } = "Логистический Офицер";
        public override string Description { get; set; } = "Отвечает за снабжение и логистику.";
        public override RoleTypeId Role { get; set; } = RoleTypeId.FacilityGuard;

        public override string BadgeColor { get { return "#FFD700"; } }

        public override bool KeepPositionOnSpawn { get; set; } = false;

        protected override void RoleAdded(Player player)
        {
            base.RoleAdded(player);

            Timing.CallDelayed(0.45f, () =>
            {
                if (player == null || !player.IsAlive) return;

                var hczRooms = Room.List
                    .Where(r => r.Zone == ZoneType.HeavyContainment && r.Type != RoomType.HczNuke)
                    .ToList();

                if (hczRooms.Count == 0) return;

                Room room = hczRooms[UnityEngine.Random.Range(0, hczRooms.Count)];
                player.Position = room.Position + Vector3.up * 1.3f;
            });
        }

        public override DoorPermissionFlags CardPermissions
        {
            get { return DoorPermissionFlags.Checkpoints | DoorPermissionFlags.ExitGates; }
        }

        public override Color CardMainColor { get { return new Color(1f, 0.84f, 0f); } }
        public override Color CardPermColor { get { return Color.white; } }


        public override List<string> Inventory { get; set; } = new List<string>
        {

            "Medkit",
            "Radio",
            "Flashlight"
        };

        public override Dictionary<AmmoType, ushort> Ammo { get; set; } = new Dictionary<AmmoType, ushort>
        {
            { AmmoType.Nato9, 30 }
        };
    }

    public sealed class MaintenanceTech : ZoneRoleBase
    {
        public override uint Id { get; set; } = 1943;
        public override string Name { get; set; } = "Техник по Обслуживанию";
        public override string Description { get; set; } = "Технический персонал.";
        public override RoleTypeId Role { get; set; } = RoleTypeId.FacilityGuard;

        public override string BadgeColor { get { return "#DAA520"; } }

        public override bool KeepPositionOnSpawn { get; set; } = false;

        protected override void RoleAdded(Player player)
        {
            base.RoleAdded(player);

            Timing.CallDelayed(0.45f, () =>
            {
                if (player == null || !player.IsAlive) return;

                var hczRooms = Room.List
                    .Where(r => r.Zone == ZoneType.HeavyContainment && r.Type != RoomType.HczNuke)
                    .ToList();

                if (hczRooms.Count == 0) return;

                Room room = hczRooms[UnityEngine.Random.Range(0, hczRooms.Count)];
                player.Position = room.Position + Vector3.up * 1.3f;
            });
        }

        public override DoorPermissionFlags CardPermissions
        {
            get { return DoorPermissionFlags.None; }
        }

        public override Color CardMainColor { get { return new Color(0.85f, 0.65f, 0.13f); } }
        public override Color CardPermColor { get { return Color.gray; } }


        public override List<string> Inventory { get; set; } = new List<string>
        {
            "Painkillers",
            "Flashlight",
            "KeycardJanitor"
        };

        public override Dictionary<AmmoType, ushort> Ammo { get; set; } = new Dictionary<AmmoType, ushort>();
    }
}
