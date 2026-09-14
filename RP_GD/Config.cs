using System.ComponentModel;

using Exiled.API.Interfaces;

using UnityEngine;

namespace EyeCatcherPlugin
{
    public class Config : IConfig
    {
        [Description("Включен ли плагин.")]
        public bool IsEnabled { get; set; } = true;

        [Description("Отладочные сообщения.")]
        public bool Debug { get; set; } = true;

        [Description("Размер черного куба-цензора.")]
        public Vector3 CubeScale { get; set; }
            = new Vector3(0.65f, 0.65f, 0.65f);

        [Description("Длина звукового сопровождения восстановления ОУС-106 (в секундах).")]
        public float Scp106ContainmentSongDuration { get; set; } = 30f;

        [Description("Имя аудиофайла (должен лежать в EXILED/Configs/Audio/).")]
        public string Scp106IntercomAudioName { get; set; } = "lebedinaya_pesnya.ogg";
    }
}