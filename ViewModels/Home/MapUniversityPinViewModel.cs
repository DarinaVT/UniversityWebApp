namespace UniWebApp.ViewModels.Home
{
    public class MapUniversityPinViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public string Country { get; set; } = null!;
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public decimal Rating { get; set; }
    }
}
