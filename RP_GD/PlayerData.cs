using System;
using System.Collections.Generic;

namespace EyeCatcherPlugin.CustomSpawn
{
    [Serializable]
    public class PlayerData
    {
        public string SteamId { get; set; } = "";
        public string Nickname { get; set; } = "";
        public bool IsVerified { get; set; } = false;
        public int Level { get; set; } = 0;
        public float HoursPlayed { get; set; } = 0f;
        public bool ScpWhitelist { get; set; } = false;

        public bool AdminWhitelist { get; set; } = false;

        public Dictionary<string, float> BranchHours { get; set; } = new Dictionary<string, float>();

        public Dictionary<string, int> RolePriorities { get; set; } = new Dictionary<string, int>();

        public DateTime LastSeen { get; set; } = DateTime.UtcNow;

        public float GetBranchHours(string branchName)
        {
            if (BranchHours == null) BranchHours = new Dictionary<string, float>();
            return BranchHours.TryGetValue(branchName, out float value) ? value : 0f;
        }
        public bool WantAntagonist { get; set; } = false;
    }
}
