namespace msftbot
{

    public class menudays
    {
        public string Name { get; set; }
        public Cafeitem[] CafeItems { get; set; }
        public int CafeId { get; set; }
        public string CafeName { get; set; }
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