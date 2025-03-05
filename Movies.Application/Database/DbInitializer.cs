using Dapper;

namespace Movies.Application.Database;

public class DbInitializer
{
    private readonly IDbConnectionFactory _dbConnectionFactory;

    public DbInitializer(IDbConnectionFactory dbConnectionFactory)
    {
        _dbConnectionFactory = dbConnectionFactory;
    }

    public async Task InitializeAsync()
    {
        using var connection = await _dbConnectionFactory.CreateConnectionAsync();
        await connection.ExecuteAsync("""
                                          CREATE TABLE IF NOT EXISTS movies (
                                              id UUID primary key,
                                              slug TEXT not null,
                                              title TEXT not null,
                                              yearofrelease INTEGER not null);
                                      """);

        await connection.ExecuteAsync("""
                                          CREATE UNIQUE INDEX CONCURRENTLY IF NOT EXISTS idx_movies_slug ON Movies USING BTREE(slug);
                                      """);

        await connection.ExecuteAsync("""
                                          CREATE TABLE IF NOT EXISTS genres (
                                              movieId UUID references movies(id),
                                                genre TEXT not null);
                                      """);

        await connection.ExecuteAsync("""
                                      CREATE TABLE IF NOT EXISTS ratings (
                                          userid uuid,
                                          movieid uuid references movies(id),
                                          rating integer not null,
                                          primary key (userid, movieid)
                                      );
                                      """);
    }
}