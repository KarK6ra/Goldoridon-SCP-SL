using System;
using CommandSystem;
using Exiled.API.Features;
using Exiled.Permissions.Extensions;

namespace EyeCatcherPlugin.CustomSpawn
{
    [CommandHandler(typeof(RemoteAdminCommandHandler))]
    public class ForceStartCommand : ICommand
    {
        public string Command => "forcestartt";
        public string[] Aliases => new string[] { "fs", "startnow" };
        public string Description => "Принудительно запустить раунд, игнорируя минимальный порог игроков";

        public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
        {
            if (!sender.CheckPermission("eyecatcher.forcestart"))
            {
                response = "Нет прав (eyecatcher.forcestart)";
                return false;
            }

            if (!Round.IsLobby)
            {
                response = "Раунд уже идёт или не в лобби.";
                return false;
            }

            SpawnManager.ForceStart();
            response = "Принудительный старт раунда запрошен.";
            return true;
        }
    }
}