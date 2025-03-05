﻿using Movies.Application.Models;

namespace Movies.Application.Services;

public interface IRatingService
{
    Task<bool> RateMovieAsync(Guid movieId, int rating, Guid userId, CancellationToken cancellationToken = default);
    Task<bool> DeleteRatingAsync(Guid movieId, Guid userId, CancellationToken cancellationToken = default);

    public Task<IEnumerable<MovieRating>> GetRatingsForUserAsync(Guid userId,
        CancellationToken cancellationToken = default);
}