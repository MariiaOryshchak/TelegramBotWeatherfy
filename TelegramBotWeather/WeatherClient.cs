using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Npgsql;
using System.Diagnostics.Metrics;
using TelegramBotWeather.Models;
using System.Threading;
using System.Collections.Generic;


namespace TelegramBotWeather
{
    public class Constants
    {
        
        public static string address = "https://localhost:7290";
        public static string Connect = "Host=localhost;Username=postgres;Password=123123;Database=postgres";

    }
    public class WeatherClient
    {
        private HttpClient _httpClient;
        private static string _address;

        public WeatherClient()
        {
            _address = Constants.address;
            _httpClient = new HttpClient();
            _httpClient.BaseAddress = new Uri(_address);

        }
        public async Task<CityWeather> GetCityWeatherAsync(string query)
        {
            var response = await _httpClient.GetAsync($"/CityWeather");
            response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<CityWeather>(content);
            result.Main.Temp -= 273; 

            return result;
        }


        //public async Task<CityWeather> PutWeather(string query)
        //{
        //    WeatherClient weatherClient = new WeatherClient();
        //    CityWeather cityWeather = await weatherClient.GetCityWeatherAsync(query);
        //    return cityWeather;
        //}
        public class DataBase
        {
            NpgsqlConnection connection = new NpgsqlConnection(Constants.Connect);


            public async Task InsertCityWeatherAsync(string query, CityWeather cityWeather)
            {
                var sql = "INSERT INTO public.\"CityWeather\"(\"CityName\", \"Temp\", \"Time\", \"Visibility\", \"SpeedWind\", \"GustWind\", \"Pressure\", \"Humidity\", \"Country\")"
                    + "VALUES (@CityName, @Temp, @Time, @Visibility, @SpeedWind, @GustWind, @Pressure, @Humidity, @Country)";

                NpgsqlCommand command = new NpgsqlCommand(sql, connection);
                command.Parameters.AddWithValue("CityName", query);
                command.Parameters.AddWithValue("Temp", cityWeather.Main.Temp);
                command.Parameters.AddWithValue("Time", DateTime.Now);
                command.Parameters.AddWithValue("Visibility", cityWeather.Visibility);
                command.Parameters.AddWithValue("SpeedWind", cityWeather.Wind.Speed);
                command.Parameters.AddWithValue("Humidity", cityWeather.Main.Humidity);
                command.Parameters.AddWithValue("Pressure", cityWeather.Main.Pressure);
                command.Parameters.AddWithValue("GustWind", cityWeather.Wind.Gust);
                command.Parameters.AddWithValue("Country", cityWeather.Sys.Country);

                await connection.OpenAsync();
                await command.ExecuteNonQueryAsync();
                await connection.CloseAsync();
            }

            public async Task DeleteUsersReview(string cityName)
            {
                NpgsqlConnection connection = new NpgsqlConnection(Constants.Connect);
                await connection.OpenAsync();
                var sql = "DELETE FROM public.\"CityWeather\"where \"CityName\" = @CityName";
                NpgsqlCommand command = new NpgsqlCommand(sql, connection);
                command.Parameters.AddWithValue("CityName", cityName);
                await command.ExecuteNonQueryAsync();
                await connection.CloseAsync();
            }
            public async Task UpdateCityWeather(CityWeather cityWeather, string cityName)
            {
                NpgsqlConnection connection = new NpgsqlConnection(Constants.Connect);
                await connection.OpenAsync();

                var sql = "UPDATE public.\"CityWeather\" SET \"Temp\" = @Temp, \"Time\" = @Time, \"SpeedWind\" = @SpeedWind, \"GustWind\" = @GustWind, \"Pressure\" = @Pressure, \"Humidity\" = @Humidity, \"Country\" = @Country WHERE \"CityName\" = @CityName";
                NpgsqlCommand command = new NpgsqlCommand(sql, connection);
                command.Parameters.AddWithValue("@CityName", cityName);
                command.Parameters.AddWithValue("Temp", cityWeather.Main.Temp);
                command.Parameters.AddWithValue("Time", DateTime.Now);
                command.Parameters.AddWithValue("Visibility", cityWeather.Visibility);
                command.Parameters.AddWithValue("SpeedWind", cityWeather.Wind.Speed);
                command.Parameters.AddWithValue("Humidity", cityWeather.Main.Humidity);
                command.Parameters.AddWithValue("Pressure", cityWeather.Main.Pressure);
                command.Parameters.AddWithValue("GustWind", cityWeather.Wind.Gust);
                command.Parameters.AddWithValue("Country", cityWeather.Sys.Country);

                await command.ExecuteNonQueryAsync();
                await connection.CloseAsync();
            }
            public async Task<List<CityMain>> SelectStatisticAsync()
            {
                NpgsqlConnection connection = new NpgsqlConnection(Constants.Connect);
                List<CityMain> cityMains = new List<CityMain>();
                await connection.OpenAsync();
                var sql = "select \"CityName\", \"Temp\", \"Time\", \"SpeedWind\", \"GustWind\", \"Pressure\", \"Humidity\", \"Country\" from public.\"CityWeather\"";
                NpgsqlCommand command = new NpgsqlCommand(sql, connection);
                NpgsqlDataReader npgsqlDataReader = await command.ExecuteReaderAsync();
                while (await npgsqlDataReader.ReadAsync())
                {
                    cityMains.Add(new CityMain(npgsqlDataReader.GetString(0), npgsqlDataReader.GetDouble(1), npgsqlDataReader.GetDateTime(2), npgsqlDataReader.GetDouble(3), npgsqlDataReader.GetDouble(4), npgsqlDataReader.GetDouble(5), npgsqlDataReader.GetDouble(6), npgsqlDataReader.GetString(7)));
                }
                await connection.CloseAsync();
                return cityMains;
            }
            
            public async Task UpdateUsersReview(CityWeather cityWeather, string cityName)
            {
                DataBase database = new DataBase();
                await database.UpdateCityWeather(cityWeather, cityName);

            }


        }
    }
}

//public async Task<List<CityWeather>> GetCityWeatherDataAsync()
//{
//    List<CityWeather> cityWeatherData = new List<CityWeather>();

//    using (NpgsqlConnection connection = new NpgsqlConnection(Constants.Connect))
//    {
//        await connection.OpenAsync();

//        string sql = "SELECT * FROM public.\"CityWeather\"";

//        using (NpgsqlCommand command = new NpgsqlCommand(sql, connection))
//        {
//            using (NpgsqlDataReader reader = await command.ExecuteReaderAsync())
//            {
//                while (await reader.ReadAsync())
//                {
//                    CityWeather cityWeather = new CityWeather();
//                    //cityWeather.time = reader.GetDateTime(reader.GetOrdinal("Time"));
//                    string cityName = reader.GetString(reader.GetOrdinal("CityName"));
//                    cityWeather.Main.Temp = reader.GetDouble(reader.GetOrdinal("Temp"));
//                    cityWeather.Visibility = reader.GetInt32(reader.GetOrdinal("Visibility"));
//                    cityWeather.Wind.Speed = reader.GetDouble(reader.GetOrdinal("SpeedWind"));
//                    cityWeather.Wind.Gust = reader.GetDouble(reader.GetOrdinal("GustWind"));
//                    cityWeather.Main.Pressure = reader.GetDouble(reader.GetOrdinal("Pressure"));
//                    cityWeather.Main.Humidity = reader.GetDouble(reader.GetOrdinal("Humidity"));
//                    cityWeather.Sys.Country = reader.GetString(reader.GetOrdinal("Country"));

//                    cityWeatherData.Add(cityWeather);
//                }
//            }
//        }

//        connection.Close();
//    }

//    return cityWeatherData;
//}
//public async Task<CityWeather> PutWeather(string query, double temp, DateTime dateTime, int visibility, double speedWind, double gustWind, double pressure, double humidity, string country)
//{
//    CityWeather cityWeather = new CityWeather();
//    string Query = query;
//    cityWeather.Main.Temp = temp;
//    DateTime DateTime = dateTime;
//    cityWeather.Visibility = visibility;
//    cityWeather.Wind.Speed = speedWind;
//    cityWeather.Wind.Gust = gustWind;
//    cityWeather.Main.Pressure = pressure;
//    cityWeather.Main.Humidity = humidity;
//    cityWeather.Sys.Country = country;
//    return cityWeather;
//}