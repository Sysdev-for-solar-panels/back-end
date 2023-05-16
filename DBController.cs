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
                    Price = reader.GetInt32(2),
                    Status = reader.GetString(5),
                    Quantity = reader.GetInt32(6), 
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
                left join components on stack.component_id = components.id", dataSource.OpenConnection());
        var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            stackItems.Add(
            new StackItem
            {
                StackId = reader.GetInt32(0),
                ComponentId = (await reader.IsDBNullAsync(1)) ? null : reader.GetInt32(1),
                ComponentName = (await reader.IsDBNullAsync(2)) ? null : reader.GetString(2),
                ComponentQuantity = (await reader.IsDBNullAsync(3)) ? null : reader.GetInt32(3),
                ComponentMaxQuantity = (await reader.IsDBNullAsync(4)) ? null : reader.GetInt32(4)
                
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
        catch
        {
            return Result.DbException;
        }
    }
    // A1-5-ig 
    public async Task<Result> AddNewProject(string name,string description,string status, int user_id, string location)
    {
        await using var cmd = new NpgsqlCommand(
            @"INSERT INTO projects (name,description,status,user_id, location) 
                VALUES (@p1, @p2, @p3, @p4, @p5)",dataSource.OpenConnection())
        {
            Parameters =
            {
                new("p1", name),
                new("p2", description),
                new("p3", status),
                new("p4", user_id),
                new("p5", location)
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
    public async Task<List<Project>> ListProjects()
    {
        List<Project> results = new List<Project>();
        await using var cmd = new NpgsqlCommand(
            @"select * from projects", dataSource.OpenConnection());

        var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            //name,description,status,user_id
            results.Add(
            new Project
                {
                    ID = reader.GetInt32(0),
                    name = reader.GetString(1),
                    description = reader.GetString(2), 
                    status = reader.GetString(3),
                    user_id = reader.GetInt32(4),
                    Location = reader.GetString(5)
                }
            );
        }
        return results;
    }

    public async Task<Result> JoinStack(StackItem stackItem)
    {
        await using var copy = new NpgsqlCommand(
            @"insert into components (name,price,max_quantity,description,status,quantity)
            select name, price, max_quantity, description, status, 0
            from components
            where name = @p1
            RETURNING id", dataSource.OpenConnection())
            {
                Parameters = 
                {
                    new("p1", stackItem.ComponentName),
                }
            };
        try
        {
            var reader = await copy.ExecuteReaderAsync();
            reader.Read();
            int componentId = reader.GetInt32(0);
            await using var update = new NpgsqlCommand(
            @"UPDATE stack set component_id = @p1 WHERE id = @p2", dataSource.OpenConnection())
            {
                Parameters = 
                {
                    new("p1",componentId),
                    new("p2", stackItem.StackId),
                }
            };
            await update.ExecuteNonQueryAsync();
            return Result.Ok;
        }
        catch
        {
            return Result.DbException;
        }
    }

    public Result SetProjectComponents(int projectId, List<int> componentId)
    {
        try 
        {
            componentId.ForEach(async (id) => 
            {
                await using var command = dataSource.CreateCommand($"INSERT INTO project_components values({projectId},{id})");
                await using var reader = await command.ExecuteReaderAsync();
            });
            return Result.Ok;
        }
        catch
        {
            return Result.DbException;
        }
    }

    public async Task<EstimateProject> EstimateProject()
    {
        await using var cmd = new NpgsqlCommand(
            @"Select sum(c.price)*1.4 from reservations 
            reservations join projects p on p.id=r.project_id 
            join project_components pc on p.id=pc.project_id 
            join components c on pc.component_id=c.id", dataSource.OpenConnection());

        var reader = await cmd.ExecuteReaderAsync();
        reader.Read();
        return new EstimateProject
        {
            Price = reader.GetInt32(0)
        };
    }

    public async Task<List<MissingComponent>> MissingComponent()
    {
        List<MissingComponent> missingComponents = new List<MissingComponent>();
        await using var cmd = new NpgsqlCommand(
            @"SELECT p.name AS project_name, c.name AS component_name, c.quantity AS component_quantity, r.quantity AS reservation_quantity
            FROM projects p
            JOIN components c ON p.id = c.id
            JOIN reservations r ON c.id = r.id;", dataSource.OpenConnection());
        var reader = await cmd.ExecuteReaderAsync();
        while (reader.Read())
        {
            String projectName =  reader.GetString(0);
            String componentName = reader.GetString(1);
            int componentQuantiy = reader.GetInt32(2);
            int reservationQuantity = reader.GetInt32(3);

            if ((componentQuantiy - reservationQuantity) < 0)
            {
                missingComponents.Add(
                    new MissingComponent
                    {
                        ProjectName = projectName,
                        ComponentQuantity = componentQuantiy,
                        Name = componentName,
                        Reserved = reservationQuantity
                    }
                );
            }
        }
        return missingComponents;
    }

    public async Task<Result> ChangeProjectStatus(int id)
    {
        await using var cmd = new NpgsqlCommand(
            @"UPDATE projects SET status= 'InProgress' WHERE id = @p1", dataSource.OpenConnection())
            {
                Parameters =
                {
                    new("p1", id),
                }
            };

        try 
        {
            await cmd.ExecuteNonQueryAsync();
            return Result.Ok;
        }
        catch
        {
            return Result.DbException;
        }
    }
}