using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace redmond_dining_bot
{
    public class dining
    {
        //This will ultimatley pull from dining API
        public string getDiningHall(string meal)
        {
            if (meal == "pizza")
            {
                return "Dining Hall 1";
            }
            else if (meal == "tacos")
            {
                return "Dining Hall 9";
            }
            else if (meal == "cake")
            {
                return "Dining Hall 3";
            }
            else
            {
                return "Not on the menu today";
            }          
        }
    }
}