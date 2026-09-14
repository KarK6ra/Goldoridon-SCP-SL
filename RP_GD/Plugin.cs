using Exiled.API.Features;
using Exiled.CustomItems.API;
using EyeCatcherPlugin.CustomSpawn;
using EyeCatcherPlugin.Roles;
using System;

namespace EyeCatcherPlugin
{
    public class Plugin : Plugin<Config>
    {
        public static Plugin Instance { get; private set; }

        public EyeCatcherGlasses GlassesItem { get; private set; }

        public EventHandlers EventHandlers { get; private set; }

        public override string Name => "EyeCatcherGlasses";

        public override string Author => "AI Developer & User";

        public override Version Version => new Version(5, 0, 0);

        public override Version RequiredExiledVersion => new Version(9, 14, 2);

        public Tranquilizer TranqGun { get; private set; }
        public CuffsItem Cuffs { get; private set; }
        public KeyItem Key { get; private set; }
        public CageItem Cage { get; private set; }
        public Scp035Armor Scp035Mask { get; private set; }
        public RadioManager RadioMgr { get; private set; }
        public JointThickenerItem JointThickener { get; private set; }




        public override void OnEnabled()
        {
            Instance = this;

            GlassesItem = new EyeCatcherGlasses();
            Scp096RageModifier.Init();
            GlassesItem.Register();

            TranqGun = new Tranquilizer();
            TranqGun.Register();
            Scp035Mask = new Scp035Armor();
            RadioMgr = new RadioManager();
            JointThickener = new JointThickenerItem();
            JointThickener.Register();
            RadioMgr.Subscribe();
            Scp035Mask.Register();

            Cuffs = new CuffsItem();
            Cuffs.Register();
            Cage = new CageItem();
            ZoneRoleManager.RegisterAll();
            
            SpawnManager.Register();
            Cage.Register();

            Key = new KeyItem();
            Key.Register();
            Scp181Role.RegisterEvents();
            UserSettings.ServerSpecific.ServerSpecificSettingsSync.ServerOnSettingValueReceived += EventHandlers.OnSettingReceived;



            EventHandlers = new EventHandlers();
            EventHandlers.Subscribe();

            base.OnEnabled();

            Log.Info("[EyeCatcher] Плагин успешно включен.");
        }


        public override void OnDisabled()
        {
            EventHandlers?.Unsubscribe();

            GlassesItem?.Unregister();
            TranqGun?.Unregister();
            Cage?.Unregister();
            Cage = null;

            Cuffs?.Unregister();
            RadioMgr?.Unsubscribe();
            RadioMgr = null;
            JointThickener?.Unregister();
            JointThickener = null;
            JointThickenerItem.BlockedSkeletons.Clear();
            Key?.Unregister();
            Scp096RageModifier.Uninit();
            Scp181Role.UnregisterEvents();
            SpawnManager.Unregister();
            Scp106Containment.ClearObjects();
            ZoneRoleManager.UnregisterAll();
            UserSettings.ServerSpecific.ServerSpecificSettingsSync.ServerOnSettingValueReceived -= EventHandlers.OnSettingReceived;


            EventHandlers = null;
            GlassesItem = null;
            TranqGun = null;
            Cuffs = null;
            Key = null;

            Instance = null;

            base.OnDisabled();
        }

    }
}