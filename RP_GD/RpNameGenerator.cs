using System;
using System.Text;

namespace EyeCatcherPlugin
{
    public static class RpNameGenerator
    {
        private static readonly string[] AcademicNames = {
            "Абрамов", "Артемьев", "Белов", "Бородин", "Васильев", "Вершинин", "Виноградов",
            "Воронов", "Голубев", "Громов", "Давыдов", "Данилов", "Ермаков", "Журавлев",
            "Зимин", "Ильин", "Калинин", "Киселев", "Ковалев", "Колесников", "Лазарев",
            "Лебедев", "Логинов", "Макаров", "Медведев", "Михайлов", "Морозов", "Назаров",
            "Некрасов", "Новиков", "Орлов", "Павлов", "Петров", "Поляков", "Романов",
            "Савельев", "Семенов", "Соколов", "Степанов", "Федоров", "Чернов"
        };

        private static readonly string[] GuardNames = {
            "Бойко", "Буров", "Волков", "Гончаров", "Денисов", "Дроздов", "Дубов", "Егоров",
            "Жуков", "Зайцев", "Иванов", "Кравцов", "Кузнецов", "Леонов", "Мартынов",
            "Одинцов", "Панин", "Рыбаков", "Сидоров", "Смирнов", "Соболев", "Тарасов",
            "Уваров", "Фролов", "Харитонов", "Царев", "Чехов", "Шаров", "Щербаков"
        };

        private static readonly string[] MtfCallsigns = {
            "Альфа", "Браво", "Вайпер", "Гранит", "Декстер", "Зенит", "Индиго", "Каппа",
            "Лансер", "Мираж", "Норд", "Омега", "Призма", "Раптор", "Сайфер", "Титан",
            "Фантом", "Хеликс", "Шэдоу", "Эхо", "Янтарь"
        };

        public static string GetClassDName()
        {
            StringBuilder sb = new StringBuilder(7);
            sb.Append("D-").Append(UnityEngine.Random.Range(1000, 10000));
            return sb.ToString();
        }

        public static string GetDirectorName()
        {
            int index = UnityEngine.Random.Range(0, AcademicNames.Length);
            StringBuilder sb = new StringBuilder(30);
            sb.Append("Д.Р. ").Append(AcademicNames[index]);
            return sb.ToString();
        }

        public static string GetGuardName()
        {
            int index = UnityEngine.Random.Range(0, GuardNames.Length);
            return GuardNames[index];
        }

        public static string GetStaffName(string roleName)
        {
            int index = UnityEngine.Random.Range(0, AcademicNames.Length);
            int cipher = UnityEngine.Random.Range(10, 100);
            StringBuilder sb = new StringBuilder(60);
            sb.Append(roleName).Append(' ').Append(AcademicNames[index]).Append(" (").Append(cipher).Append(')');
            return sb.ToString();
        }

        public static string GetMtfName(string rank)
        {
            int index = UnityEngine.Random.Range(0, MtfCallsigns.Length);
            int num = UnityEngine.Random.Range(1, 10);
            StringBuilder sb = new StringBuilder(50);
            sb.Append(rank).Append(" «").Append(MtfCallsigns[index]).Append("-").Append(num).Append("»");
            return sb.ToString();
        }
    }
}
