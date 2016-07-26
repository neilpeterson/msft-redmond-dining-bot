using msftbot.Support;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace msftbot
{
    public class FoodTruck
    {
        public int ID { get; set; }
        public int BuildingID { get; set; }
        public string Location { get; set; }
        public string WeekName { get; set; }
        public string DayName { get; set; }
        public string Foodtruckname { get; set; }
        public string FoodtruckDescription { get; set; }
        public string FoodtruckURL { get; set; }
        public string FoodtruckHours { get; set; }
        public string Name { get; set; }
        public string CafeLogoUri { get; set; }
        public bool IsFoodTruck { get; set; }
    }

    internal class FoodTruckActions
    {
        internal FoodTruckActions()
        { }

        internal async Task<string> GetAllFoodTruck()
        {
            // Get authentication token from authentication.cs
            Authentication auth = new Authentication();
            string authtoken = await auth.GetAuthHeader();

            // Get JSON – List of all Food Trucks
            HttpClient httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authtoken);
            HttpResponseMessage response = await httpClient.GetAsync("https://msrefdiningint.azurewebsites.net/api/v1/foodtrucks");
            response.EnsureSuccessStatusCode();
            string responseBody = await response.Content.ReadAsStringAsync();

            // Convert JSON to list
            List<FoodTruck> allFoodTruckList = JsonConvert.DeserializeObject<List<FoodTruck>>(responseBody);

            // String for output
            StringBuilder allFoodTruck = new StringBuilder();

            // get today for Linq query
            string day = DateTime.Now.DayOfWeek.ToString();

            var foodTruck =
                from n in allFoodTruckList
                where (n.DayName.Equals(day))
                select n;

            foreach (FoodTruck truck in foodTruck)
            {
                allFoodTruck.AppendFormat(Constants.foodTruckFormat, truck.Foodtruckname, truck.Location, Environment.NewLine);
            }

            return allFoodTruck.ToString();
        }
    }
}