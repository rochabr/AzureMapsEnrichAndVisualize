namespace Microsoft.AzureMaps
{
    public class Location
    {
        public int id { get; set; }
        public string street_number { get; set; }
        public string street_name { get; set; }
        public string details { get; set; }
        public string city { get; set; }
        public string province { get; set; }
        public string postal_code { get; set; }
        public string country_code { get; set; }
        public double? latitude { get; set; }
        public double? longitude { get; set; }
    }
}