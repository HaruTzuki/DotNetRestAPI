using Dapper;
using Movies.Application.Database;
using Movies.Application.Models;

namespace Movies.Application.Repositories;

public class RatingRepository : IRatingRepository
{
    private readonly IDbConnectionFactory _dbConnectionFactory;

    public RatingRepository(IDbConnectionFactory dbConnectionFactory)
    {
        _dbConnectionFactory = dbConnectionFactory;
    }


    public async Task<bool> RateMovieAsync(Guid movieId, int rating, Guid userId, CancellationToken cancellationToken = default)
    {
        using var connection = await _dbConnectionFactory.CreateConnectionAsync(cancellationToken);
        var result = await connection.ExecuteAsync(new CommandDefinition("""
                insert into ratings (movieid, rating, userid)
                values (@movieId, @rating, @userId)
                on conflict (movieid, userid) do update
                set rating = @rating;
            """, new { movieId, rating, userId }, cancellationToken: cancellationToken));
        
        return result > 0;
    }

    public async Task<float?> GetRatingAsync(Guid movieId, CancellationToken cancellationToken = default)
    {
        using var connection = await _dbConnectionFactory.CreateConnectionAsync(cancellationToken);
        return await connection.QuerySingleOrDefaultAsync<float?>(new CommandDefinition("""
                select round(avg(rating),1)
                from ratings
                where movieid = @movieId;
            """, new { movieId }, cancellationToken: cancellationToken));
    }

    public async Task<(float? Rating, int? UserRating)> GetRatingAsync(Guid movieId, Guid userId,
        CancellationToken cancellationToken = default)
    {
        using var connection = await _dbConnectionFactory.CreateConnectionAsync(cancellationToken);
        return await connection.QuerySingleOrDefaultAsync<(float?, int?)>(new CommandDefinition("""
                select round(avg(rating),1) as rating, 
                    (select rating
                     from ratings
                     where movieid = @movieId and userid = @userId
                     limit 1) as userrating
                from ratings
                where movieid = @movieId and userid = @userId;
            """, new { movieId, userId }, cancellationToken: cancellationToken));
    }

    public async Task<bool> DeleteRatingAsync(Guid movieId, Guid userId, CancellationToken cancellationToken = default)
    {
        using var connection = await _dbConnectionFactory.CreateConnectionAsync(cancellationToken);
        var result = await connection.ExecuteAsync(new CommandDefinition("""
                delete from ratings
                where movieid = @movieId and userid = @userId;
            """, new { movieId, userId }, cancellationToken: cancellationToken));
        
        return result > 0;
    }

    public async Task<IEnumerable<MovieRating>> GetRatingsForUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        using var connection = await _dbConnectionFactory.CreateConnectionAsync(cancellationToken);
        return await connection.QueryAsync<MovieRating>(new CommandDefinition("""
                select r.rating, r.movieid, m.slug
                from ratings r 
                    inner join movies m on r.movieid = m.id
                where r.userid = @userId;
            """, new { userId }, cancellationToken: cancellationToken));
    }
}