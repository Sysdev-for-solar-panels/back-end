using Npgsql;

class DBController
{
    public enum Result {Ok,NoRecordAffected,DbException};
    private Npgsql.NpgsqlDataSource dataSource;

    public DBController()
    {
        DotNetEnv.Env.Load();
        string host = Environment.GetEnvironmentVariable("HOST")!;
        string port = Environment.GetEnvironmentVariable("PORT")!;
        string uesrname = Environment.GetEnvironmentVariable("USERNAME")!;
        string password = Environment.GetEnvironmentVariable("PASSWORD")!;
        string database = Environment.GetEnvironmentVariable("DATABASE")!;
        dataSource = NpgsqlDataSource.Create($"Host={host};Port={port};Username={uesrname};Password={password};Database={database}");
    }

    public async Task<string?> Login(string email, string password)
    {
        await using var cmd = new NpgsqlCommand(
            @"select * from users 
                where email = @p1 and password = @p2"
        ,dataSource.OpenConnection())
        {
            Parameters =
            {
                new("p1", email),
                new("p2", password),   
            }
        };
        var reader = await cmd.ExecuteReaderAsync();
        if (reader.HasRows)
        {
            reader.Read();
            return reader.GetString(1);
        }
        else 
        {
            return null;
        }
    }

    public async Task<bool> Registration(string name,string email,string password,string role)
    {
        await using var cmd = new NpgsqlCommand(
            @"INSERT INTO users (name,email,password,role) 
                VALUES (@p1, @p2, @p3, @p4)"
        ,dataSource.OpenConnection())
        {
            Parameters =
            {
                new("p1", name),
                new("p2", email),
                new("p3", password),
                new("p4", role)
                
            }
        };

        try
        {
            await cmd.ExecuteNonQueryAsync();
            return true;
        }
        catch (System.Data.Common.DbException)
        {
            return false;
        }
    }

    public async Task<Result> Delete(string email, string password)
    {
        await using var cmd = new NpgsqlCommand(@"
        DELETE FROM users WHERE email = @p1 and password = @p2",dataSource.OpenConnection())
        {
            Parameters =
            {
                new("p1", email),
                new("p2", password),
            }
        };

        try
        {
            int effectedRows = await cmd.ExecuteNonQueryAsync();
            if(effectedRows > 0)
            {
                return Result.Ok;
            }
            else
            {
                return Result.NoRecordAffected;
            }
        }
        catch (System.Data.Common.DbException)
        {
            return Result.DbException;
        }
    }
}