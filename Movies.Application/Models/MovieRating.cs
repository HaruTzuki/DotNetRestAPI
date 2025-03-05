namespace Movies.Application.Models;

public class MovieRating
{
    public required Guid MovieId { get; init; }
    public required string Slug { get; set; }
    public required int Rating { get; init; }
}