
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Extensions;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using Telegram.Bot.Polling;
using Npgsql;
using TelegramBotWeather.Models;
using static TelegramBotWeather.WeatherClient;
using static TelegramBotWeather.WeatherClient.DataBase;
using System.Net.Http;
using System.Net.Http.Json;
using System.Xml.Linq;
using System;
using System.Diagnostics.Metrics;
using static System.Net.Mime.MediaTypeNames;
using System.Threading;
using System.Collections.Generic;
using static TelegramBotWeather.Models.CityWeather;


namespace TelegramBotWeather
{
    public class TelegramWeatherBot
    {

        static TelegramBotClient botClient = new TelegramBotClient("5957782232:AAGGAg0s1RejPgJZrtZU4LzRHi8oyW-pL9o");

        CancellationToken cancellationToken = new CancellationToken();
        ReceiverOptions receiverOptions = new ReceiverOptions { AllowedUpdates = { } };

        public async Task Start()
        {
            botClient.StartReceiving(HandlerUpdateAsync, HandlerError, receiverOptions, cancellationToken);
            var botMe = await botClient.GetMeAsync();
            Console.WriteLine($"Бот {botMe.Username} почав працювати");

        }

        public Task HandlerError(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
        {
            var errorMessage = exception switch
            {
                ApiRequestException apiRequestException => $"В API телеграм-бота сталася помилка:\n {apiRequestException.ErrorCode}" +
                $"\n{apiRequestException.Message}",
                _ => exception.ToString()
            };
            return Task.CompletedTask;
        }

        public async Task HandlerUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
        {
            if (update.Type == UpdateType.Message && update?.Message?.Text != null)
            {
                await HandlerMessageAsync(botClient, update);
            }

        }


        private Dictionary<long, string> currentStage = new Dictionary<long, string>();

        public async Task HandlerMessageAsync(ITelegramBotClient botClient, Update update)
        {

            var message = update.Message;
            if (!currentStage.ContainsKey(message.Chat.Id))
            {
                currentStage.Add(message.Chat.Id, "home");
            }


            switch (currentStage[message.Chat.Id]) //чекає відповідь юзера
            {

                case "/addCity":
                    await GetWaetherByAddCity(message.Text);
                    break;
                default:

                    break;
            }
            switch (message.Text) //змінює вміст змінної (для всіх)
            {
                case "/addCity":
                    await botClient.SendTextMessageAsync(message.Chat.Id, "Введіть назву міста\n ");
                    currentStage[message.Chat.Id] = "/addCity";

                    break;

                case "/myCities":
                    await botClient.SendTextMessageAsync(message.Chat.Id, "Міста в моєму списку\n ");
                    currentStage[message.Chat.Id] = "/myCities";
                    break;
                case "/updateData":
                    await botClient.SendTextMessageAsync(message.Chat.Id, "Оновити дані погоди\n");
                    currentStage[message.Chat.Id] = "/updateData";
                    break;

                case "/deleteAllCities":
                    await botClient.SendTextMessageAsync(message.Chat.Id, "Видалити всі міста зі списку\n ");
                    currentStage[message.Chat.Id] = "/deleteAllCities";
                    break;
                case "/start":
                    currentStage[message.Chat.Id] = "/start";
                    break;
                case "/keyboard":
                    currentStage[message.Chat.Id] = "/keyboard";
                    break;

            }
            switch (currentStage[message.Chat.Id]) //не чекає відповіді
            {

                case "/start":
                    await botClient.SendTextMessageAsync(message.Chat.Id, "Ласкаво просимо до бота погоди. Виберіть " +
                        "команду щоб продовжити /keyboard");
                    break;
                case "/updateData":
                    //await UpdateCityWeather(message.Text);
                    break;
                case "/deleteAllSities":
                    //await DeleteReview(message.Text);
                    break;
                case "/myCities":
                    await SelectStatisticAsync(message.Text);
                    break;
                case "/keyboard":
                    ReplyKeyboardMarkup replyKeyboardMarkup = new ReplyKeyboardMarkup(
                        new[]
                        {
                 new KeyboardButton[] { "Введіть назву міста", "Оновити дані погоди" },
                 new KeyboardButton[] { "Міста в моєму списку", "Видалити всі міста зі списку" },
                         }
                    )
                    {
                        ResizeKeyboard = true
                    };
                    await botClient.SendTextMessageAsync(message.Chat.Id, "Виберіть пункт меню:", replyMarkup: replyKeyboardMarkup);
                    break;
                case "Введіть назву міста":
                    await botClient.SendTextMessageAsync(message.Chat.Id, "Виберіть цю команду /addCity");
                    break;

                case "Міста в моєму списку":
                    await botClient.SendTextMessageAsync(message.Chat.Id, "Виберіть цю команду /myCities");
                    break;
                case "Оновити дані погоди":
                    await botClient.SendTextMessageAsync(message.Chat.Id, "Виберіть цю команду /updateData");
                    break;
                case "Видалити всі міста зі списку":
                    //await DeleteReview(message.Text);
                    break;
            }
            async Task GetWaetherByAddCity(string query)
            {
                WeatherClient weatherClient = new WeatherClient();
                CityWeather cityWeather = await weatherClient.GetCityWeatherAsync(query);

                await botClient.SendTextMessageAsync(message.Chat.Id, $"Погода у місті: {query}\n" +
                    $"Час запиту: {DateTime.Now}\n" +
                    $"Температура: {cityWeather.Main.Temp} \n" +
                    $"Видимість: {cityWeather.Visibility} \n" +
                    $"Вологість: {cityWeather.Main.Humidity} \n" +
                    $"Швидкість вітру: {cityWeather.Wind.Speed}  \n" +
                    $"Пориви вітру: {cityWeather.Wind.Gust}  \n" +
                    $"Тиск: {cityWeather.Main.Pressure}  \n" +
                    $"Країна: {cityWeather.Sys.Country}");

                DataBase dataBase = new DataBase();
                await dataBase.InsertCityWeatherAsync(query, cityWeather);
                currentStage[message.Chat.Id] = "";
            }






            async Task SelectStatisticAsync(string? text)
            {
                DataBase dataBase = new DataBase();
                await dataBase.SelectStatisticAsync();
                currentStage[message.Chat.Id] = "";
            }
            



            return;
        }


    }
}



//async Task DeleteReview(string cityName)
//{
//    DataBase database = new DataBase();
//    await database.DeleteUsersReview(cityName);
//    currentStage[message.Chat.Id] = "";
//}


//async Task UpdateCityWeather(string cityName)
//{
//    var apiUrl = $"https://localhost:7290/CityWeather?CityName={Uri.EscapeDataString(cityName)}";
//    CityWeather cityWeather = new CityWeather();
//    WeatherClient weatherClient = new WeatherClient();
//    DataBase dataBase = new DataBase();
//    var updatedCityWeather = await weatherClient.GetCityWeatherAsync(cityName);
//    await dataBase.InsertCityWeatherAsync(updatedCityWeather, cityName);
//    currentStage[message.Chat.Id] = "";
//}


//async Task DoWeatherReviev(string cityName)
//{
//    WeatherClient weatherClient = new WeatherClient();
//    var resultreview = await weatherClient.PutWeather(cityName);

//    await botClient.SendTextMessageAsync(message.Chat.Id, $"Display Title: \n" + $"CityName: {cityName}\n");

//    DataBase dataBase = new DataBase();
//    await dataBase.InsertCityWeatherAsync(resultreview, cityName);

//    List<CityWeather> cityWeatherData = await dataBase.GetCityWeatherDataAsync();

//    foreach (CityWeather cityWeather in cityWeatherData)
//    {
//        string messages = $"Temperature: {cityWeather.Main.Temp}\n" +
//                          $"Time: {cityWeather.time}\n" +
//                          $"Visibility: {cityWeather.Visibility}\n" +
//                          $"SpeedWind: {cityWeather.Wind.Speed}\n" +
//                          $"GustWind: {cityWeather.Wind.Gust}\n" +
//                          $"Pressure: {cityWeather.Main.Pressure}\n" +
//                          $"Humidity: {cityWeather.Main.Humidity}\n" +
//                          $"Country: {cityWeather.Sys.Country}";

//        await botClient.SendTextMessageAsync(message.Chat.Id, messages);
//    }

//    currentStage[message.Chat.Id] = "";
//}
//async Task GetInformatoin()
//{
//    DataBase dataBase = new DataBase();
//    List<CityWeather> cityWeatherData = await dataBase.GetCityWeatherDataAsync();

//    foreach (CityWeather cityWeather in cityWeatherData)
//    {

//        string messages = $"Temperature: {cityWeather.Main.Temp}\n" +
//                        $"Time: {cityWeather.time}\n" +
//                        $"Visibility: {cityWeather.Visibility}\n" +
//                        $"SpeedWind: {cityWeather.Wind.Speed}\n" +
//                        $"GustWind: {cityWeather.Wind.Gust}\n" +
//                        $"Pressure: {cityWeather.Main.Pressure}\n" +
//                        $"Humidity: {cityWeather.Main.Humidity}\n" +
//                        $"Country: {cityWeather.Sys.Country}";

//        await botClient.SendTextMessageAsync(message.Chat.Id, messages);

//    }

//}