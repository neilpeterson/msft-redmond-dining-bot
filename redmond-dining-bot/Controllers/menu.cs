using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace cafemenu
{
    public class Rootobject
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public string CafeLogo { get; set; }
        public string CafeLogoUri { get; set; }
        public string LocationName { get; set; }
        public string BuildingFloor { get; set; }
        public bool IsFoodTruck { get; set; }
        public string PictureURL { get; set; }
        public string OperatingHours { get; set; }
        public Cafeitem[] CafeItems { get; set; }
        public bool IsMenuItemsAvailable { get; set; }
        public int Id { get; set; }
    }

    public class Cafeitem
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public string Prices { get; set; }
        public string[] WeekDays { get; set; }
        public int Id { get; set; }
    }
}