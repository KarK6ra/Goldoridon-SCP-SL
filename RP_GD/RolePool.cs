using System.Collections.Generic;
using EyeCatcherPlugin.Roles;
using PlayerRoles;

namespace EyeCatcherPlugin.CustomSpawn
{
    public enum SpawnRoleKind
    {
        Vanilla,
        Custom
    }

    public class SpawnRoleEntry
    {
        public string Id { get; set; }
        public string DisplayName { get; set; }
        public SpawnRoleKind Kind { get; set; }
        public RoleTypeId VanillaRole { get; set; }
        public uint CustomRoleId { get; set; }
        public int MaxCount { get; set; }
    }

    public static class RolePool
    {
        public static readonly List<SpawnRoleEntry> AllPlayableRoles = new List<SpawnRoleEntry>();

        public static void Build()
        {
            AllPlayableRoles.Clear();

            AllPlayableRoles.Add(new SpawnRoleEntry
            {
                Id = "ClassD",
                DisplayName = "Class-D",
                Kind = SpawnRoleKind.Vanilla,
                VanillaRole = RoleTypeId.ClassD,
                MaxCount = 99
            });

            AllPlayableRoles.Add(new SpawnRoleEntry
            {
                Id = "Scientist",
                DisplayName = "Scientist",
                Kind = SpawnRoleKind.Vanilla,
                VanillaRole = RoleTypeId.Scientist,
                MaxCount = 8
            });

            AllPlayableRoles.Add(new SpawnRoleEntry
            {
                Id = "FacilityGuard",
                DisplayName = "Facility Guard",
                Kind = SpawnRoleKind.Vanilla,
                VanillaRole = RoleTypeId.FacilityGuard,
                MaxCount = 8
            });

            AddCustom("CentralCommand", "Центральное Командование", 1901);
            AddCustom("SiteDirector", "Директор", 1902);
            AddCustom("EthicsInspector", "Инспектор Комитета по Этике", 1903);

            AddCustom("ResearchDirector", "Научный Руководитель", 1910);
            AddCustom("ProjectLead", "Руководитель Проекта", 1911);
            AddCustom("ZoneScientist", "Научный Сотрудник", 1912);
            AddCustom("JuniorScientist", "Младший Научный Сотрудник", 1913);

            AddCustom("SecurityChief", "Глава Службы Безопасности", 1920);
            AddCustom("SecurityLieutenant", "Лейтенант", 1921);
            AddCustom("Supervisor", "Супервайзер СБ", 1922);
            AddCustom("SecurityOfficer", "Офицер СБ", 1923);
            AddCustom("SecurityCadet", "Кадет СБ", 1924);

            AddCustom("ChiefPhysician", "Главный Врач", 1930);
            AddCustom("Surgeon", "Хирург", 1931);
            AddCustom("Therapist", "Терапевт", 1932);
            AddCustom("Orderly", "Медбрат", 1933);
            AddCustom("Intern", "Интерн", 1934);

            AddCustom("ChiefEngineer", "Главный Инженер", 1940);
            AddCustom("ZoneEngineer", "Инженер", 1941);
            AddCustom("LogisticsOfficer", "Логистический Офицер", 1942);
            AddCustom("MaintenanceTech", "Техник по Обслуживанию", 1943);
        }

        private static void AddCustom(string id, string name, uint roleId)
        {
            AllPlayableRoles.Add(new SpawnRoleEntry
            {
                Id = id,
                DisplayName = name,
                Kind = SpawnRoleKind.Custom,
                CustomRoleId = roleId,
                MaxCount = 1
            });
        }

        public static int MinimumThreshold
        {
            get { return 15; } 
        }
    }
}