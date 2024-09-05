using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

public class CPHInline
{
    public bool Execute()
    {
        // Отримуємо поточний час
        DateTime now = DateTime.Now;
        // Отримуємо час останнього виконання скрипта
        DateTime lastRunTime = CPH.GetGlobalVar<DateTime>("lastRunTime", false);
        // Перевірка на null (перше виконання)
        if (lastRunTime == default(DateTime) || (now - lastRunTime).TotalMinutes >= 30) // за замовчуванням таймер на нову перевірку глядачів 30 хвилин
        {
            // Оновлюємо час останнього виконання скрипта
            CPH.SetGlobalVar("lastRunTime", now, false);
            // Отримуємо список всіх присутніх користувачів
            CPH.TryGetArg("users", out List<Dictionary<string, object>> users);
            List<Dictionary<string, object>> currentViewers = new List<Dictionary<string, object>>();
            foreach (var user in users)
            {
                string currentUserName = user["userName"].ToString();
                CheckUserAsync(currentUserName).Wait(); // Виконання асинхронної функції синхронно
            }

            // Отримуємо поточний список глядачів із глобальної змінної
            var existingViewers = CPH.GetGlobalVar<List<Dictionary<string, object>>>("currentViewers") ?? new List<Dictionary<string, object>>();
            // Додаємо нових глядачів до списку
            existingViewers.AddRange(currentViewers);
            // Оновлюємо глобальну змінну зі списком глядачів
            CPH.SetGlobalVar("currentViewers", existingViewers, false);
            return true;
        }
        return false;
    }

    private async Task CheckUserAsync(string userName)
    {
        string url = $"https://artemiano.top/api/twitch/{userName}";
        string response;

        using (HttpClient client = new HttpClient())
        {
            // Додаємо заголовок User-Agent
            client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/85.0.4183.102 Safari/537.36");

            // Виконання асинхронного HTTP-запиту
            response = await client.GetStringAsync(url); 
        }

        if (response.Contains("Виникла помилка на сайті, пов'язана з API Twitch :("))
        {
            CPH.LogError($"API Twitch повернув помилку для {userName}.");
            return;
        }

        // Парсимо кількість "болотномовних" з відповіді
        int swampSpeakersCount = ParseCount(response, "болотномовних:");

        // Перевіряємо кількість і приймаємо рішення
        if (swampSpeakersCount > 10) // Замініть "10" на потрібну кількість для моментального бану
        {
            // Блокування користувача, якщо кількість "болотномовних" більше 10 або вказаноною вами величини
            CPH.TwitchBanUser(userName, "Забагато фоловів у країні 404"); // бан юзера який не пройшов перевірку та причина бану
        }
        else if (swampSpeakersCount > 0)
        {
            // Відправляємо повідомлення в чат, якщо кількість "болотномовних" більше 0 для попередження стрімера
            CPH.SendMessage($"{userName} має {swampSpeakersCount} {GetFollowSuffix(swampSpeakersCount)} в країні 404"); // приклад повідомлення: "Someuser має 5 фоловів в країні 404"
        }
    }

    private int ParseCount(string response, string key)
    {
        int count = 0;
        // Знаходимо індекс ключа в рядку
        int keyIndex = response.IndexOf(key);
        if (keyIndex != -1)
        {
            // Витягуємо частину рядка після ключа
            string substring = response.Substring(keyIndex + key.Length).Trim();
            // Витягуємо число до наступної крапки
            int endIndex = substring.IndexOf('.');
            if (endIndex != -1)
            {
                // Парсимо отримане число
                int.TryParse(substring.Substring(0, endIndex), out count);
            }
        }

        return count;
    }


    // Метод для отримання правильної форми слова "фолов" в залежності від кількості
    private string GetFollowSuffix(int number)
    {
        if (number % 100 >= 11 && number % 100 <= 19)
        {
            return "фоловів";
        }

        switch (number % 10)
        {
            case 1:
                return "фолов";
            case 2:
            case 3:
            case 4:
                return "фолови";
            default:
                return "фоловів";
        }
    }
}