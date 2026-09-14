using Exiled.API.Features;

namespace EyeCatcherPlugin
{
    public class RadioManager
    {
        public void Subscribe()
        {
            Exiled.Events.Handlers.Player.UsingRadioBattery += OnUsingRadioBattery;
        }

        public void Unsubscribe()
        {
            Exiled.Events.Handlers.Player.UsingRadioBattery -= OnUsingRadioBattery;
        }

        private void OnUsingRadioBattery(Exiled.Events.EventArgs.Player.UsingRadioBatteryEventArgs ev)
        {
            ev.IsAllowed = false;
        }
    }
}
