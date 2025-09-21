namespace DTO.WebApiDTO.Journey
{
    public sealed class JourneyUnshareRequestDtoApi
    {
        public required List<Guid> UserIds { get; init; }
    }
}
