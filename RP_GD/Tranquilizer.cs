using CommandSystem.Commands.RemoteAdmin.Dummies;
using Exiled.API.Features;
using Exiled.API.Features.Attributes;
using Exiled.API.Features.Spawn;
using Exiled.CustomItems.API.Features;
using Exiled.Events.EventArgs.Player;
using MEC;
using PlayerRoles;
using System.Collections.Generic;
using UnityEngine;

namespace EyeCatcherPlugin
{
    public class Tranquilizer : CustomWeapon
    {
        public override uint Id { get; set; } = 1731;
        public override string Name { get; set; } = "Tranquilizer COM-15";
        public override string Description { get; set; } = "Пистолет с сильным снотворным. Усыпляет разум SCP-939, подчиняя его вам.";
        public override ItemType Type { get; set; } = ItemType.GunCOM15;
        public override float Weight { get; set; } = 1.5f;
        public override byte ClipSize { get; set; } = 1;
        public override float Damage { get; set; } = 0f;

        public override SpawnProperties SpawnProperties { get; set; } = new SpawnProperties();

        public static Dictionary<Player, ZombieDogData> ControlledDogs { get; } = new Dictionary<Player, ZombieDogData>();

        public static Dictionary<Player, Player> Carrying { get; } = new Dictionary<Player, Player>();

        protected override void OnShot(ShotEventArgs ev)
        {
            if (ev.Target == null || ev.Player == null) return;

            if (ev.Target.IsHuman)
            {
                ev.Target.Kill("Умер от чрезвычайно высокой дозы транквилизатора");
                return;
            }

            if (ev.Target.Role.Type == RoleTypeId.Scp939)
            {
                Player dogPlayer = ev.Target;
                Vector3 currentPos = dogPlayer.Position;
                float currentHealth = dogPlayer.Health;
                dogPlayer.Kill("Твоё сознание угасло под действием транквилизатора...");

                Timing.CallDelayed(0.1f, () =>
{
    foreach (var ragdoll in Ragdoll.List)
    {
        if (ragdoll.Owner == dogPlayer && ragdoll.Role == RoleTypeId.Scp939)
        {
            Mirror.NetworkServer.Destroy(ragdoll.GameObject); break;
        }
    }
});

                Timing.CallDelayed(0.3f, () =>
{
    Npc dummyDog = Npc.Spawn("SCP-939 (Controlled)", RoleTypeId.Scp939, position: currentPos);

    if (dummyDog != null)
    {
        dummyDog.Health = currentHealth;
        Carrying.Add(ev.Player, dummyDog);

        CoroutineHandle AIHandle = Timing.RunCoroutine(RunDogAICoroutine(ev.Player, dummyDog));

        CoroutineHandle wakeHandle = Timing.RunCoroutine(WakeUpDogCoroutine(dogPlayer, dummyDog));

        ControlledDogs.Add(dummyDog, new ZombieDogData
        {
            RealPlayer = dogPlayer,
            WakeTimerHandle = wakeHandle,
            AICoroutineHandle = AIHandle
        });

        ev.Player.ShowHint("Вы подчинили разум SCP-939. Она послушно следует за вами.", 5f);
    }
});
            }
        }

        private IEnumerator<float> WakeUpDogCoroutine(Player realPlayer, Player dummyDog)
        {
            yield return Timing.WaitForSeconds(480f);
            if (ControlledDogs.ContainsKey(dummyDog))
            {
                ControlledDogs.Remove(dummyDog);

                foreach (var pair in new Dictionary<Player, Player>(Carrying))
                {
                    if (pair.Value == dummyDog)
                    {
                        RemoveDogControl(pair.Key);
                        break;
                    }
                }

                Vector3 wakePos = dummyDog.Position;
                float finalHealth = dummyDog.Health;
                dummyDog.Role.Set(RoleTypeId.Spectator);

                if (dummyDog is Npc npc)
                {
                    npc.Destroy();
                }



                if (realPlayer != null && realPlayer.Role.Type == RoleTypeId.Spectator)
                {
                    realPlayer.Role.Set(RoleTypeId.Scp939);
                    Timing.CallDelayed(0.5f, () =>
                    {
                        realPlayer.Position = wakePos;
                        realPlayer.Health = finalHealth; realPlayer.ShowHint("Действие транквилизатора закончилось! Вы вернули контроль над своим телом.", 5f);
                    });
                }
            }
        }

        public static void RemoveDogControl(Player leader)
        {
            if (!Carrying.TryGetValue(leader, out Player dummy)) return;
            Carrying.Remove(leader);

            if (dummy != null && ControlledDogs.TryGetValue(dummy, out var data))
            {
                Timing.KillCoroutines(data.AICoroutineHandle);
            }
        }

        private static IEnumerator<float> RunDogAICoroutine(Player leader, Npc dummy)
        {
            while (leader != null && dummy != null && dummy.GameObject != null)
            {
                float distance = Vector3.Distance(dummy.Position, leader.Position);

                if (distance > 1f)
                {
                    Vector3 direction = (leader.Position - dummy.Position).normalized;

                    dummy.Rotation = Quaternion.LookRotation(new Vector3(direction.x, 0, direction.z));
                    dummy.Position += new Vector3(direction.x, 0, direction.z) * 7f * Time.deltaTime;
                }


                yield return Timing.WaitForOneFrame;
            }
        }
    }
    public class ZombieDogData
    {
        public Player RealPlayer { get; set; }
        public CoroutineHandle WakeTimerHandle { get; set; }
        public CoroutineHandle AICoroutineHandle { get; set; }
    }
}
