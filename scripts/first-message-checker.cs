using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

public class CPHInline
{
    public bool Execute()
    {
        // Отримуємо ім'я користувача з аргументів
        CPH.TryGetArg("userName", out string userName);
        string url = $"https://artemiano.top/api/twitch/{userName}";
        string response;

        // Виконуємо асинхронний метод для отримання даних
        Task.Run(async () =>
        {
            using (HttpClient client = new HttpClient())
            {
                // Додаємо заголовок User-Agent
                client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/85.0.4183.102 Safari/537.36");

                try
                {
                    // Надсилаємо GET-запит до API
                    HttpResponseMessage httpResponse = await client.GetAsync(url);

                    if (httpResponse.IsSuccessStatusCode)
                    {
                        response = await httpResponse.Content.ReadAsStringAsync();
                    }
                    else
                    {
                        CPH.LogError($"Помилка при отриманні даних: {httpResponse.StatusCode}");
                        return false;
                    }
                }
                catch (HttpRequestException e)
                {
                    CPH.LogError($"Запит завершився з помилкою: {e.Message}");
                    return false;
                }
            }

            // Перевіряємо, чи повернуто помилку з API
            if (response.Contains("Виникла помилка на сайті, пов'язана з API Twitch :("))
            {
                CPH.LogError("API Twitch повернув помилку.");
                return false;
            }

            // Парсимо кількість "болотномовних" з відповіді
            int swampSpeakersCount = ParseCount(response, "болотномовних:");

            // Перевіряємо кількість і приймаємо рішення
            if (swampSpeakersCount > 10) // Замініть "10" на потрібну кількість для моментального бану
            {
                // Блокування користувача, якщо кількість "болотномовних" більше 10 або вказаноною вами величини
                CPH.TwitchBanUser(userName, "Забагато фоловів у країні 404");
            }
            else if (swampSpeakersCount > 0) // Замініть "0" на потрібну кількість
            {
                // Відправляємо повідомлення в чат, якщо кількість "болотномовних" більше 0 для попередження стрімера
                CPH.SendMessage($"{userName} має {swampSpeakersCount} {GetFollowSuffix(swampSpeakersCount)} в країні 404");
            }

            return true;
        }).GetAwaiter().GetResult(); // Викликаємо асинхронний метод та очікуємо його завершення

        return true;
    }

    // Метод для парсингу кількості з відповіді
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