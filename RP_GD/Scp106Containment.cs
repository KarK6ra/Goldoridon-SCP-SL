using System;
using System.Collections.Generic;
using System.Linq;
using AdminToys;
using Exiled.API.Enums;
using Exiled.API.Features;
using Exiled.API.Features.Toys;
using MEC;
using Mirror;
using PlayerRoles;
using UnityEngine;

namespace EyeCatcherPlugin
{
    public static class Scp106Containment
    {
        public static Room TargetRoom { get; private set; }
        private static readonly List<GameObject> SpawnedToys = new List<GameObject>();

        private static GameObject _boothDoorObject = null;
        public static bool IsVictimSacrificed { get; private set; } = false;
        public static bool IsProtocolActive { get; private set; } = false;

        private static readonly Vector3 LocalTriggerPos = new Vector3(6.125f, 2.413f, -5.535f);
        private static readonly Vector3 InsideZoneSize = new Vector3(1.8f, 2.8f, 1.8f);

        private static float _buttonOnePressTime = 0f;
        private static float _buttonTwoPressTime = 0f;
        private static Player _buttonOneUser = null;
        private static Player _buttonTwoUser = null;

        private static Npc _audioSpeakerBot = null;
        private static SCPSLAudioApi.AudioCore.AudioPlayerBase _audioPlayer = null;

        public static void SpawnProtocolObjects()
        {
            ClearObjects();

            TargetRoom = Room.List.FirstOrDefault(r => r.Type == RoomType.Hcz106);
            if (TargetRoom == null)
            {
                Log.Error("[ОУС-106] Комната HCZ_106 не найдена на карте!");
                return;
            }

            IsVictimSacrificed = false;
            IsProtocolActive = false;

            Vector3 boothPos = TargetRoom.Transform.TransformPoint(LocalTriggerPos + new Vector3(0f, 0.55f, 0.6f));
            Quaternion boothRot = TargetRoom.Transform.rotation * Quaternion.Euler(3.525f, 1.386f + 180f, 0f);

            SpawnWall(boothPos + (boothRot * new Vector3(0f, 0f, 1.0f)), boothRot.eulerAngles, new Vector3(2.0f, 3.0f, 0.1f));
            SpawnWall(boothPos + (boothRot * new Vector3(-1.0f, 0f, 0f)), boothRot.eulerAngles, new Vector3(0.1f, 3.0f, 2.0f));
            SpawnWall(boothPos + (boothRot * new Vector3(1.0f, 0f, 0f)), boothRot.eulerAngles, new Vector3(0.1f, 3.0f, 2.0f));
            SpawnWall(boothPos + (boothRot * new Vector3(0f, -1.5f, 0f)), boothRot.eulerAngles, new Vector3(2.0f, 0.1f, 2.0f));
            SpawnWall(boothPos + (boothRot * new Vector3(0f, 1.5f, 0f)), boothRot.eulerAngles, new Vector3(2.0f, 0.1f, 2.0f));
            SpawnWall(boothPos + (boothRot * new Vector3(0f, 1.2f, -1.0f)), boothRot.eulerAngles, new Vector3(2.0f, 0.6f, 0.1f));

            Primitive doorPrim = Primitive.Create(PrimitiveType.Cube, boothPos + (boothRot * new Vector3(1.9f, 0f, -1.0f)), boothRot.eulerAngles, new Vector3(1.8f, 3.0f, 0.1f), true, new Color(0.1f, 0.1f, 0.1f));
            doorPrim.Collidable = true;
            doorPrim.MovementSmoothing = 0;
            _boothDoorObject = doorPrim.GameObject;
            SpawnedToys.Add(_boothDoorObject);

            Text textBooth = Text.Create(boothPos + (boothRot * new Vector3(0f, 1.2f, -1.07f)), boothRot, Vector3.one * 0.045f, "<b><size=55>КАМЕРА ВОССТАНОВЛЕНИЯ ОУС-106</size></b>", new Vector2(800f, 80f), null, true);
            SpawnedToys.Add(textBooth.GameObject);

            Vector3 consolePos = TargetRoom.Transform.TransformPoint(new Vector3(19.293f, 0.964f, 2.859f));
            Quaternion consoleRot = TargetRoom.Transform.rotation * Quaternion.Euler(5.034f, 133.855f, 0f);

            SpawnWall(consolePos + Vector3.down * 0.4f, consoleRot.eulerAngles, new Vector3(0.45f, 0.8f, 0.45f));
            SpawnPrimitive(PrimitiveType.Cylinder, consolePos, consoleRot.eulerAngles, new Vector3(0.35f, 0.05f, 0.35f), Color.gray);
            SpawnInteractable(consolePos + Vector3.up * 0.30f, consoleRot, new Vector3(1.2f, 1.2f, 1.2f), OnBoothConsoleInteracted);

            Text textConsole = Text.Create(consolePos + (consoleRot * new Vector3(0f, -0.95f, 0.8f)), consoleRot * Quaternion.Euler(90f, 0f, 0f), Vector3.one * 0.035f, "<b><size=40>ПРИЕМНИК КАРТЫ ОУС\n[ДОСТУП: СНС+]</size></b>", new Vector2(600f, 80f), null, true);
            SpawnedToys.Add(textConsole.GameObject);

            Vector3 btn1Pos = TargetRoom.Transform.TransformPoint(new Vector3(22.098f, 0.964f, 2.188f));
            Quaternion btn1Rot = TargetRoom.Transform.rotation * Quaternion.Euler(3.775f, 270.231f, 0f);

            SpawnWall(btn1Pos + Vector3.down * 0.4f, btn1Rot.eulerAngles, new Vector3(0.4f, 0.8f, 0.4f));
            SpawnPrimitive(PrimitiveType.Cylinder, btn1Pos, btn1Rot.eulerAngles, new Vector3(0.3f, 0.05f, 0.3f), Color.red);
            SpawnInteractable(btn1Pos + Vector3.up * 0.30f, btn1Rot, new Vector3(0.4f, 0.4f, 0.4f), (hub) => OnSafetyButtonInteracted(hub, 1));

            Text textBtn1 = Text.Create(btn1Pos + (btn1Rot * new Vector3(0f, -0.95f, 0.7f)), btn1Rot * Quaternion.Euler(90f, 0f, 0f), Vector3.one * 0.04f, "<b><size=45>ЗАТВОР АВТОМАТИКИ №1</size></b>", new Vector2(600f, 60f), null, true);
            SpawnedToys.Add(textBtn1.GameObject);

            Vector3 btn2Pos = TargetRoom.Transform.TransformPoint(new Vector3(22.105f, 0.964f, -21.203f));
            Quaternion btn2Rot = TargetRoom.Transform.rotation * Quaternion.Euler(4.279f, 268.594f, 0f);

            SpawnWall(btn2Pos + Vector3.down * 0.4f, btn2Rot.eulerAngles, new Vector3(0.4f, 0.8f, 0.4f));
            SpawnPrimitive(PrimitiveType.Cylinder, btn2Pos, btn2Rot.eulerAngles, new Vector3(0.3f, 0.05f, 0.3f), Color.red);
            SpawnInteractable(btn2Pos + Vector3.up * 0.30f, btn2Rot, new Vector3(0.4f, 0.4f, 0.4f), (hub) => OnSafetyButtonInteracted(hub, 2));

            Text textBtn2 = Text.Create(btn2Pos + (btn2Rot * new Vector3(0f, -0.95f, 0.7f)), btn2Rot * Quaternion.Euler(90f, 0f, 0f), Vector3.one * 0.04f, "<b><size=45>ЗАТВОР АВТОМАТИКИ №2</size></b>", new Vector2(600f, 60f), null, true);
            SpawnedToys.Add(textBtn2.GameObject);
        }

        private static void OnBoothConsoleInteracted(ReferenceHub hub)
        {
            Player player = Player.Get(hub);
            if (player == null) return;

            Log.Info($"[ОУС-106] Клик по пульту зафиксирован от игрока {player.Nickname}!");

            if (IsVictimSacrificed) return;

            bool scp106Exists = Player.List.Any(p => p.Role.Type == RoleTypeId.Scp106) || Npc.List.Any(n => n.Role.Type == RoleTypeId.Scp106);
            if (!scp106Exists)
            {
                player.ShowHint("<color=red>Объект SCP-106 отсутствует в комплексе.</color>", 3f);
                return;
            }

            if (player.CurrentItem == null ||
                (player.CurrentItem.Type != ItemType.KeycardResearchCoordinator &&
                 player.CurrentItem.Type != ItemType.KeycardFacilityManager &&
                 player.CurrentItem.Type != ItemType.KeycardContainmentEngineer &&
                 player.CurrentItem.Type != ItemType.KeycardMTFCaptain &&
                 player.CurrentItem.Type != ItemType.KeycardO5))
            {
                player.ShowHint("<color=red>Требуется уровень доступа СНС или выше.</color>", 3f);
                return;
            }

            Player victim = GetPlayerInBoothZone();
            if (victim == null)
            {
                player.ShowHint("<color=red>Камера декомпрессии пуста.</color>", 3f);
                return;
            }

            if (victim.Role.Type == RoleTypeId.Scp106 || victim.Role.Type == RoleTypeId.Spectator) return;

            IsVictimSacrificed = true;

            if (_boothDoorObject != null)
                Timing.RunCoroutine(AnimateDoorClose());

            victim.Kill("Был отдан в жертву лебединой песне");
            Map.ShowHint("<color=green>✓ Контур герметизирован. Активируйте затворы автоматики!</color>", 5f);
        }

        private static IEnumerator<float> AnimateDoorClose()
        {
            Vector3 boothPos = TargetRoom.Transform.TransformPoint(LocalTriggerPos + new Vector3(0f, 0.55f, 0.6f));
            Quaternion boothRot = TargetRoom.Transform.rotation * Quaternion.Euler(3.525f, 1.386f + 180f, 0f);

            Vector3 startLocalPos = new Vector3(1.9f, 0f, -1.0f);
            Vector3 targetLocalPos = new Vector3(0f, 0f, -1.0f);

            float elapsed = 0f;
            float duration = 0.8f;

            while (elapsed < duration && _boothDoorObject != null)
            {
                elapsed += Time.deltaTime;
                _boothDoorObject.transform.position = boothPos + (boothRot * Vector3.Lerp(startLocalPos, targetLocalPos, elapsed / duration));
                yield return Timing.WaitForOneFrame;
            }

            if (_boothDoorObject != null)
                _boothDoorObject.transform.position = boothPos + (boothRot * targetLocalPos);
        }

        private static void OnSafetyButtonInteracted(ReferenceHub hub, int buttonId)
        {
            Player player = Player.Get(hub);
            if (player == null || !IsVictimSacrificed || IsProtocolActive) return;

            Log.Info($"[ОУС-106] Клик по затвору №{buttonId} от игрока {player.Nickname}!");

            float currentTime = Time.time;

            if (buttonId == 1)
            {
                _buttonOnePressTime = currentTime;
                _buttonOneUser = player;
                player.ShowHint("<color=orange>● Затвор №1 взведен.</color>", 2f);
            }
            else
            {
                _buttonTwoPressTime = currentTime;
                _buttonTwoUser = player;
                player.ShowHint("<color=orange>● Затвор №2 взведен.</color>", 2f);
            }

            int realPlayerCount = Player.List.Count(p => !p.IsNPC);
            if (realPlayerCount <= 1)
            {
                if (_buttonOnePressTime > 0f && _buttonTwoPressTime > 0f && Mathf.Abs(_buttonOnePressTime - _buttonTwoPressTime) <= 10f)
                {
                    IsProtocolActive = true;
                    Timing.RunCoroutine(RunSongAndContainment());
                }
            }
            else
            {
                if (Mathf.Abs(_buttonOnePressTime - _buttonTwoPressTime) <= 0.4f)
                {
                    if (_buttonOneUser != null && _buttonTwoUser != null && _buttonOneUser != _buttonTwoUser)
                    {
                        IsProtocolActive = true;
                        Timing.RunCoroutine(RunSongAndContainment());
                    }
                }
            }
        }

        private static IEnumerator<float> RunSongAndContainment()
        {
            string audioPath = System.IO.Path.Combine(Exiled.API.Features.Paths.Configs, "Audio", Plugin.Instance.Config.Scp106IntercomAudioName);

            if (System.IO.File.Exists(audioPath))
            {
                try
                {
                    _audioSpeakerBot = Npc.Spawn("ОУС-106 Аудиосистема", RoleTypeId.Tutorial, position: new Vector3(0f, -2000f, 0f));

                    if (_audioSpeakerBot != null)
                    {
                        _audioSpeakerBot.ReferenceHub.roleManager.ServerSetRole(RoleTypeId.Tutorial, RoleChangeReason.None);

                        _audioPlayer = SCPSLAudioApi.AudioCore.AudioPlayerBase.Get(_audioSpeakerBot.ReferenceHub);

                        if (_audioPlayer != null)
                        {
                            _audioPlayer.BroadcastChannel = VoiceChat.VoiceChatChannel.Intercom;
                            _audioPlayer.Loop = false;

                            _audioPlayer.Enqueue(audioPath, -1);
                            _audioPlayer.Play(0);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log.Error($"[ОУС-106 Audio] Ошибка инициализации SCPSLAudioApi: {ex.Message}");
                }
            }
            else
            {
                Log.Warn($"[ОУС-106 Audio] Аудиофайл не найден: {audioPath}. Протокол запустится без звука.");
            }

            float duration = Plugin.Instance.Config.Scp106ContainmentSongDuration;
            yield return Timing.WaitForSeconds(duration);

            StopAudioBot();

            foreach (Player scp in Player.List.Where(p => p.Role.Type == RoleTypeId.Scp106))
            {
                scp.Kill("ОУС восстановлены");
            }

            Exiled.API.Features.Cassie.Message("SCP 106 SUCCESSFULLY CONTAINED AND SECURED", false, true, true);
            Map.ShowHint("<color=green>✓ Протокол завершен. Объект SCP-106 успешно нейтрализован.</color>", 5f);
        }

        private static void StopAudioBot()
        {
            try
            {
                if (_audioPlayer != null)
                {
                    if (_audioPlayer.AudioToPlay != null)
                    {
                        _audioPlayer.AudioToPlay.Clear();
                    }
                    _audioPlayer = null;
                }

                if (_audioSpeakerBot != null)
                {
                    _audioSpeakerBot.Destroy();
                    _audioSpeakerBot = null;
                }
            }
            catch (Exception) { }
        }

        private static Player GetPlayerInBoothZone()
        {
            if (TargetRoom == null) return null;

            Vector3 targetBoothCenter = LocalTriggerPos + new Vector3(0f, 0.55f, 0.6f);
            Vector3 zoneSize = new Vector3(2.5f, 3.5f, 2.5f);

            foreach (Player p in Player.List.Concat(Npc.List))
            {
                if (p == null || p.GameObject == null) continue;

                Vector3 playerLocalPos = TargetRoom.Transform.InverseTransformPoint(p.Position);
                Vector3 relativeToBooth = playerLocalPos - targetBoothCenter;

                if (Mathf.Abs(relativeToBooth.x) <= zoneSize.x / 2f &&
                    Mathf.Abs(relativeToBooth.y) <= zoneSize.y / 2f &&
                    Mathf.Abs(relativeToBooth.z) <= zoneSize.z / 2f)
                {
                    return p;
                }
            }
            return null;
        }

        private static void SpawnWall(Vector3 pos, Vector3 rot, Vector3 scale) => SpawnPrimitive(PrimitiveType.Cube, pos, rot, scale, new Color(0.12f, 0.12f, 0.12f));

        private static void SpawnPrimitive(PrimitiveType type, Vector3 pos, Vector3 rot, Vector3 scale, Color color)
        {
            Primitive prim = Primitive.Create(type, pos, rot, scale, true, color);
            prim.Collidable = true;
            prim.MovementSmoothing = 0;
            SpawnedToys.Add(prim.GameObject);
        }

        private static void SpawnInteractable(Vector3 pos, Quaternion rot, Vector3 scale, Action<ReferenceHub> interactionAction)
        {
            GameObject prefab = NetworkManager.singleton.spawnPrefabs.Find(p => p.name == "InvisibleInteractableToy");
            if (prefab == null) return;

            GameObject toyObj = UnityEngine.Object.Instantiate(prefab, pos, rot);
            InvisibleInteractableToy interactableToy = toyObj.GetComponent<InvisibleInteractableToy>();

            if (interactableToy != null)
            {
                interactableToy.transform.localScale = scale;
                interactableToy.NetworkShape = InvisibleInteractableToy.ColliderShape.Box;

                interactableToy.NetworkInteractionDuration = 0f;
                interactableToy.NetworkIsLocked = false;

                interactableToy.OnInteracted += interactionAction;

                NetworkServer.Spawn(toyObj);
                SpawnedToys.Add(toyObj);
            }
        }

        public static void ClearObjects()
        {
            try
            {
                if (_audioPlayer != null)
                {
                    if (_audioPlayer.AudioToPlay != null)
                    {
                        _audioPlayer.AudioToPlay.Clear();
                    }
                    _audioPlayer = null;
                }

                if (_audioSpeakerBot != null)
                {
                    _audioSpeakerBot.Destroy();
                    _audioSpeakerBot = null;
                }
            }
            catch (Exception ex)
            {
                Log.Debug($"[ОУС-106 Audio] Игнорируемое исключение при очистке аудиосистемы: {ex.Message}");
            }

            foreach (var obj in SpawnedToys)
            {
                if (obj != null)
                    NetworkServer.Destroy(obj);
            }
            SpawnedToys.Clear();

            _boothDoorObject = null;
            _buttonOneUser = null;
            _buttonTwoUser = null;
            _buttonOnePressTime = 0f;
            _buttonTwoPressTime = 0f;
        }
    }
}