using System;
using System.Collections.Generic;

namespace MyWeatherApp_Deployed.Models
{
    public class WeatherHistoryModel
    {
        public string City { get; set; } = "";
        public DateTime Date { get; set; }

        public double AvgTempC {  get; set; }
        public double MaxTempC { get; set; }
        public double MinTempC { get; set; }

        public double AvgHumidity { get; set; }
        public double TotalPrecipMm { get; set; }

        public List<HourlyHistory> Hourly { get; set; } = new();

        public class HourlyHistory
        {
            public string Time { get; set; } = "";
            public double TempC { get; set; }
            public double Humidity { get; set; }
            public double PrecipMm { get; set; }

        }
    }
}
