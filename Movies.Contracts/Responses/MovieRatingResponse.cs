namespace Movies.Contracts.Responses;

public class MovieRatingResponse
{
    public required Guid MovieId { get; init; }
    public required string Slug { get; set; }
    public required int Rating { get; init; }
}