using System;

namespace TelegramBotWeather
{
    class Program
    {
        static void Main()
        {
            TelegramWeatherBot bot = new TelegramWeatherBot();
            bot.Start().Wait();

            Console.WriteLine("Press any key to exit");
            Console.ReadKey();
        }
    }
}
