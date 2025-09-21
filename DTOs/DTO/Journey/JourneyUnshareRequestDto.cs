namespace DTO.DTO.Journey
{
    public sealed class JourneyUnshareRequestDto
    {
        public required List<Guid> UserIds { get; init; }
    }
}
