using Npgsql;
using back_end;

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

    public async Task<(string,string)?> Login(string email, string password)
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
            return (reader.GetString(1),reader.GetString(4));
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

    public async Task<Result> ChangePrice(int id, int price)
    {
        await using var cmd = new NpgsqlCommand(
            @"UPDATE components SET price = @p2 WHERE id = @p1",dataSource.OpenConnection())
        {
            Parameters =
            {
                new("p1", id),
                new("p2", price),
            }
        };

        try
        {
            await cmd.ExecuteNonQueryAsync();
            return Result.Ok;
        }
        catch (System.Data.Common.DbException err)
        {
            Console.Error.WriteLine(err);
            return Result.DbException;
        }
    }

    public async Task<Result> AddComponent(string name,int price,int maxQuantity)
    {
        await using var cmd = new NpgsqlCommand(
            @"INSERT INTO components (name,price,max_quantity) 
                VALUES (@p1, @p2, @p3)",dataSource.OpenConnection())
        {
            Parameters =
            {
                new("p1", name),
                new("p2", price),
                new("p3", maxQuantity),    
            }
        };

        try
        {
            await cmd.ExecuteNonQueryAsync();
            return Result.Ok;
        }
        catch (System.Data.Common.DbException err)
        {
            Console.Error.WriteLine(err);
            return Result.DbException;
        }
    }

    public async Task<List<Component>> ListComponents()
    {
        List<Component> results = new List<Component>();
        await using var cmd = new NpgsqlCommand(
            @"select * from components", dataSource.OpenConnection());

        var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            //id,name,price
            results.Add(
            new Component
                {
                    ID = reader.GetInt32(0),
                    Name = reader.GetString(1), 
                    Price = reader.GetInt32(2)
                }
            );
        }
        return results;
    }

    public async Task<List<StackItem>> ListStack()
    {
        List<StackItem> stackItems = new List<StackItem>();
        await using var cmd = new NpgsqlCommand(
            @"select stack.id,stack.component_id,components.name,components.quantity,components.max_quantity
                from stack
                join components on stack.component_id = components.id", dataSource.OpenConnection());
        var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            stackItems.Add(
            new StackItem
            {
                StackId = reader.GetInt32(0),
                ComponentId = reader.GetValue(1) as int?,
                ComponentName = reader.GetString(2),
                ComponentQuantity = reader.GetValue(3) as int?,
                ComponentMaxQuantity = reader.GetValue(4) as int?
            });
        }
        return stackItems;
    }

    public async Task<Result> UpdateComponent(int id, int newQuantity)
    {
        await using var cmd = new NpgsqlCommand(
            @"update components set quantity = @p1 where id = @p2", dataSource.OpenConnection())
            {
                Parameters =
                {
                    new("p2", id),
                    new("p1", newQuantity),
                }
            };
        try
        {
            await cmd.ExecuteNonQueryAsync();
            return Result.Ok;
        }
        catch (System.Data.Common.DbException err)
        {
            Console.Error.WriteLine(err);
            return Result.DbException;
        }
    }
}