using System;
using CommandSystem;
using Exiled.API.Features;

namespace EyeCatcherPlugin
{
    [CommandHandler(typeof(RemoteAdminCommandHandler))]
    public class ContainmentCommand : ICommand
    {
        public string Command => "spawn106ous";
        public string[] Aliases => new string[] { "sp106" };
        public string Description => "Принудительный ручной спавн объектов ОУС-106";

        public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
        {
            try
            {
                Scp106Containment.SpawnProtocolObjects();
                response = "Сборка ОУС-106 выполнена успешно! Проверьте комнату деда.";
                return true;
            }
            catch (Exception ex)
            {
                response = $"Сбой сборки: {ex.Message}";
                return false;
            }
        }
    }
}