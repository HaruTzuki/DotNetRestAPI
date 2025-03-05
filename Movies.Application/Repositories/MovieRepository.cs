using System.Text;
using Dapper;
using Movies.Application.Database;
using Movies.Application.Models;

namespace Movies.Application.Repositories;

public class MovieRepository : IMovieRepository
{
    private readonly IDbConnectionFactory _dbConnectionFactory;

    public MovieRepository(IDbConnectionFactory dbConnectionFactory)
    {
        _dbConnectionFactory = dbConnectionFactory;
    }

    public async Task<bool> CreateAsync(Movie movie, CancellationToken cancellationToken = default)
    {
        using var connection = await _dbConnectionFactory.CreateConnectionAsync(cancellationToken);
        using var transaction = connection.BeginTransaction();

        var result = await connection.ExecuteAsync(new CommandDefinition("""
                                                                             INSERT INTO movies (id, slug, title, yearofrelease)
                                                                             VALUES (@Id, @Slug, @Title, @YearOfRelease);
                                                                         """, movie,
            cancellationToken: cancellationToken));

        if (result > 0)
        {
            foreach (var genre in movie.Genres)
            {
                await connection.ExecuteAsync(new CommandDefinition("""
                                                                    INSERT INTO genres (movieId, genre)
                                                                    VALUES (@MovieId, @Genre);
                                                                    """, new { MovieId = movie.Id, Genre = genre },
                    cancellationToken: cancellationToken));
            }
        }

        transaction.Commit();

        return result > 0;
    }

    public async Task<Movie?> GetByIdAsync(Guid id, Guid? userId = null, CancellationToken cancellationToken = default)
    {
        using var connection = await _dbConnectionFactory.CreateConnectionAsync(cancellationToken);
        var movie = await connection.QuerySingleOrDefaultAsync<Movie>(
            new CommandDefinition("""
                                  select m.*, round(avg(r.rating),1) as rating, myr.rating as userrating
                                  from movies m
                                  left join ratings r on m.id = r.movieid
                                  left join ratings myr on m.id = myr.movieid 
                                                               and myr.userid = @userId
                                  where id = @id
                                  group by id, userrating;
                                  """, new { id, userId }, cancellationToken: cancellationToken));

        if (movie is null)
        {
            return null;
        }

        var genres = await connection.QueryAsync<string>(
            new CommandDefinition("""
                                  select genre from genres where movieId = @id;
                                  """, new { id }, cancellationToken: cancellationToken));

        foreach (var genre in genres)
        {
            movie.Genres.Add(genre);
        }

        return movie;
    }

    public async Task<Movie?> GetBySlugAsync(string slug, Guid? userId = null,
        CancellationToken cancellationToken = default)
    {
        using var connection = await _dbConnectionFactory.CreateConnectionAsync(cancellationToken);
        var movie = await connection.QuerySingleOrDefaultAsync<Movie>(
            new CommandDefinition("""
                                  select m.*, round(avg(r.rating),1) as rating, myr.rating as userrating
                                  from movies m
                                  left join ratings r on m.id = r.movieid
                                  left join ratings myr on m.id = myr.movieid 
                                                               and myr.userid = @userId
                                  where slug = @slug
                                  group by id, userrating;
                                  """, new { slug, userId }, cancellationToken: cancellationToken));

        if (movie is null)
        {
            return null;
        }

        var genres = await connection.QueryAsync<string>(
            new CommandDefinition("""
                                  select genre from genres where movieId = @id;
                                  """, new { id = movie.Id }, cancellationToken: cancellationToken));

        foreach (var genre in genres)
        {
            movie.Genres.Add(genre);
        }

        return movie;
    }

    public async Task<IEnumerable<Movie>> GetAllAsync(GetAllMoviesOptions options,
        CancellationToken cancellationToken = default)
    {
        using var connection = await _dbConnectionFactory.CreateConnectionAsync(cancellationToken);

        var orderClause = string.Empty;

        if (options.SortField is not null)
        {
            orderClause = $"""
                            , m.{options.SortField}
                            order by m.{options.SortField} {(options.SortOrder == SortOrder.Ascending ? "asc" : "desc")};""          
                           """;
        }

        var result = await connection.QueryAsync(
            new CommandDefinition($"""
                                   select m.*,
                                          string_agg(distinct g.genre, ',') as genres,
                                          round(avg(r.rating),1) as rating,
                                          myr.rating as userrating
                                   from movies m 
                                   left join genres g on m.id = g.movieid
                                   left join ratings r on m.id = r.movieid
                                   left join ratings myr on m.id = myr.movieid 
                                                                and myr.userid = @userId
                                   where (@title is null or m.title ilike '%' || @title || '%')
                                     and (@year is null or m.yearofrelease = @year)
                                   group by m.id, myr.rating {orderClause}
                                   limit @pageSize
                                   offset @pageOffset;
                                   """, new
            {
                userId = options.UserId,
                title = options.Title,
                year = options.YearOfRelease,
                pageSize = options.PageSize,
                pageOffset = (options.Page - 1) * options.PageSize
            }, cancellationToken: cancellationToken));


        return result.Select(x => new Movie
        {
            Id = x.id,
            Title = x.title,
            YearOfRelease = x.yearofrelease,
            Rating = (float?)x.rating,
            UserRating = (int?)x.userrating,
            Genres = Enumerable.ToList(x.genres.Split(","))
        });
    }

    public async Task<bool> UpdateAsync(Movie movie, CancellationToken cancellationToken = default)
    {
        using var connection = await _dbConnectionFactory.CreateConnectionAsync(cancellationToken);
        using var transaction = connection.BeginTransaction();

        await connection.ExecuteAsync(new CommandDefinition("""
                                                            delete from genres where movieid = @id
                                                            """, new { id = movie.Id },
            cancellationToken: cancellationToken));

        foreach (var genre in movie.Genres)
        {
            await connection.ExecuteAsync(new CommandDefinition("""
                                                                INSERT INTO genres (movieId, genre)
                                                                VALUES (@MovieId, @Genre);
                                                                """, new { MovieId = movie.Id, Genre = genre },
                cancellationToken: cancellationToken));
        }

        var result = await connection.ExecuteAsync(new CommandDefinition("""
                                                                         Update movies
                                                                         SET slug = @Slug,
                                                                             title = @Title,
                                                                             yearofrelease = @YearOfRelease
                                                                          WHERE id = @Id;   
                                                                         """, movie,
            cancellationToken: cancellationToken));

        transaction.Commit();
        return result > 0;
    }

    public async Task<bool> DeleteByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        using var connection = await _dbConnectionFactory.CreateConnectionAsync(cancellationToken);
        using var transaction = connection.BeginTransaction();

        var result = await connection.ExecuteAsync(new CommandDefinition("""
                                                                         DELETE FROM genres WHERE movieid = @id;
                                                                         """, new { id },
            cancellationToken: cancellationToken));

        if (result > 0)
        {
            result = await connection.ExecuteAsync(new CommandDefinition("""
                                                                         DELETE FROM movies WHERE id = @id;
                                                                         """, new { id },
                cancellationToken: cancellationToken));
        }

        transaction.Commit();

        return result > 0;
    }

    public async Task<bool> ExistsByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        using var connection = await _dbConnectionFactory.CreateConnectionAsync(cancellationToken);

        return await connection.ExecuteScalarAsync<bool>(new CommandDefinition("""
                                                                               select count(1) from movies where id = @id;
                                                                               """, new { id },
            cancellationToken: cancellationToken));
    }

    public async Task<int> GetCountAsync(string? title, int? yearOfRelease,
        CancellationToken cancellationToken = default)
    {
        using var connection = await _dbConnectionFactory.CreateConnectionAsync(cancellationToken);

        return await connection.QuerySingleAsync<int>(new CommandDefinition("""
        select count(id) from movies where (@title is null or title ilike '%' || @title || '%') and (@year is null or yearofrelease = @year);
        """, new {title, year = yearOfRelease}, cancellationToken: cancellationToken));
    }
}