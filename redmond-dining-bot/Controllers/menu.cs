using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace redmond_dining_menu
{
    public class menu
    {
        public string getMenu(string location)
        {
            if (location == "building 16")
            {
                return "tacos, pizza, fish, cake";
            }
            else if (location == "building 25")
            {
                return "salad, fruit, pho, sandwich";
            }
            else
            {
                return "Cannot find a menu for this location.";
            }
        }
    }
}