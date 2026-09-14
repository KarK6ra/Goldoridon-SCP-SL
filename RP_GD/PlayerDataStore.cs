using Exiled.API.Features;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;

namespace EyeCatcherPlugin.CustomSpawn
{
    public static class PlayerDataStore
    {
        private static readonly string FolderPath = Path.Combine(Paths.Configs, "EyeCatcherPlugin", "Players");
        private static readonly Dictionary<string, PlayerData> Cache = new Dictionary<string, PlayerData>();

        public static void Init()
        {
            if (!Directory.Exists(FolderPath))
                Directory.CreateDirectory(FolderPath);

            Cache.Clear();
        }

        public static PlayerData Get(string steamId)
        {
            if (string.IsNullOrEmpty(steamId))
                return null;

            if (Cache.TryGetValue(steamId, out PlayerData cached))
                return cached;

            string path = Path.Combine(FolderPath, steamId + ".json");
            if (File.Exists(path))
            {
                try
                {
                    string json = File.ReadAllText(path);
                    PlayerData data = JsonConvert.DeserializeObject<PlayerData>(json);
                    if (data != null)
                    {
                        if (data.BranchHours == null) data.BranchHours = new Dictionary<string, float>();
                        Cache[steamId] = data;
                        return data;
                    }
                }
                catch (Exception ex)
                {
                    Log.Error("[PlayerDataStore] Ошибка чтения " + steamId + ": " + ex.Message);
                }
            }

            PlayerData fresh = new PlayerData
            {
                SteamId = steamId,
                IsVerified = false,
                Level = 0,
                HoursPlayed = 0f,
                ScpWhitelist = false,
                AdminWhitelist = false,
                BranchHours = new Dictionary<string, float>
                {
                    { "Science", 0f },
                    { "Medical", 0f },
                    { "Security", 0f },
                    { "Engineering", 0f }
                }
            };
            Cache[steamId] = fresh;
            Save(fresh);
            return fresh;
        }

        public static void Save(PlayerData data)
        {
            if (data == null || string.IsNullOrEmpty(data.SteamId))
                return;

            try
            {
                string path = Path.Combine(FolderPath, data.SteamId + ".json");
                string json = JsonConvert.SerializeObject(data, Formatting.Indented);
                File.WriteAllText(path, json);
                Cache[data.SteamId] = data;
            }
            catch (Exception ex)
            {
                Log.Error("[PlayerDataStore] Ошибка записи " + data.SteamId + ": " + ex.Message);
            }
        }

        public static void SaveAll()
        {
            foreach (var pair in Cache)
                Save(pair.Value);
        }
    }
}
