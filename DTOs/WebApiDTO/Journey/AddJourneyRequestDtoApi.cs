
namespace DTO.WebApiDTO.Journey
{
    public class AddJourneyRequestDtoApi
    {
        public string StartingLocation { get; set; }
        public string ArrivalLocation { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime ArrivalTime { get; set; }
        public string TransportationType { get; set; }
        public decimal RouteDistanceKm { get; set; }
    }
}

