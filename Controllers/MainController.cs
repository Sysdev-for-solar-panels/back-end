using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Text.Json;

namespace back_end.Controllers;

[ApiController]
[Route("api/")]
public class LoginController : ControllerBase
{
    private readonly ILogger<LoginController> _logger;

    public LoginController(ILogger<LoginController> logger)
    {
        _logger = logger;
    }

    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<ActionResult> Login([FromBody] User user)
    {
        if (user.Password is null && user.Email is null) 
        {
            var response = new 
            {
                error = "Add email or password"
            };
            
            return NotFound(JsonSerializer.Serialize(response));
        }
        else
        {
            (string,string)? signInUser = await new DBController().Login(user.Email!,user.Password!);
            if (signInUser is null)
            {
                var response = new 
                {
                    error = "Wrong email or password!"
                };
                
                return NotFound(JsonSerializer.Serialize(response));
            }
            else
            {
                await HttpContext.SignInAsync(new ClaimsPrincipal(new[]
                {
                    new ClaimsIdentity(new List<Claim>()
                    {
                        new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()),
                        new Claim(ClaimTypes.Role, signInUser.Value.Item2)
                    },
                    CookieAuthenticationDefaults.AuthenticationScheme)
                }));
                
                var response = new 
                {
                    name = signInUser.Value.Item1,
                    role = signInUser.Value.Item2
                };
                return Ok(JsonSerializer.Serialize(response));
            }
        }
    }

    [Authorize(Roles = "admin")]
    [HttpPost("registration")]
    public async Task<ActionResult> Registration([FromBody] Registration user)
    {
        string resultValue = "Unsuccesful registration";
        
        bool result = await new DBController().Registration(user.Name!,user.Email!,user.Password!,user.Role!);
        if (result)
        {
            resultValue = "Registration completed.";
            var obj = new
            {
                Value = resultValue
            };

            return Ok(JsonSerializer.Serialize(resultValue));
        }
        else 
        {
            var obj = new
            {
                Value = resultValue
            };
            
            return BadRequest(JsonSerializer.Serialize(obj));
        }
    }

    [HttpDelete("delete")]
    public async Task<ActionResult> Delete([FromBody] User user)
    {
        var result = await new DBController().Delete(user.Email!,user.Password!) switch {
            DBController.Result.Ok => Ok(JsonSerializer.Serialize(new {Message =  "Succesfully deleted"})),
            DBController.Result.NoRecordAffected => BadRequest(JsonSerializer.Serialize(new {Message =  "Wrong email or password"})),
            DBController.Result.DbException => StatusCode( 500,JsonSerializer.Serialize(new {Message =  "Internal error"})),
            _ => null
        };
        
        if (result is null)
        {
            return StatusCode(500, JsonSerializer.Serialize(new { Message = "Internal Error." }));
        }
        else 
        {
            return result;
        }
    }

    [AllowAnonymous]
    [HttpGet("logout")]
    public async Task<ActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return Ok(JsonSerializer.Serialize(new {Message = "Succesfuly loged out"}));
    }

    [Authorize(Roles = "raktarvezeto")]
    [HttpPost("set-price")]
    public async Task<ActionResult> SetPrice([FromBody] Price price)
    {
        if (price.ID == 0 || price.Value == 0)
        {
            Console.Error.WriteLine("result");
            return BadRequest(JsonSerializer.Serialize(new {Message = "Missing data"}));
        }
        else 
        {
            var result = await new DBController().ChangePrice(price.ID,price.Value) switch {
                DBController.Result.Ok => Ok(JsonSerializer.Serialize(new {Message =  "Succesfully changed the price"})),
                DBController.Result.DbException => StatusCode(500,JsonSerializer.Serialize(new {Message =  "Bad request"})),
                _  => StatusCode(500,JsonSerializer.Serialize(new {Message = "Bad request"}))
            };
            return result;
        }
    }

    [Authorize(Roles = "raktarvezeto")]
    [HttpPost("add-component")]
    public async Task<ActionResult> AddComponent([FromBody] Component component)
    {
        if (component.Name is null || component.Price == 0 || component.MaxQuantity == 0)
        {
            return BadRequest(JsonSerializer.Serialize(new {Message = "Missing data"}));
        }
        else 
        {
            var result = await new DBController().AddComponent(component.Name, component.Price!, component.MaxQuantity!) switch {
                DBController.Result.Ok => Ok(JsonSerializer.Serialize(new {Message =  "Succesfully added new component"})),
                DBController.Result.DbException => StatusCode(500,JsonSerializer.Serialize(new {Message =  "Bad request"})),
                _  => StatusCode(500,JsonSerializer.Serialize(new {Message = "Bad request"}))
            };

            return result;
        }
    }

    [Authorize(Roles = "raktarvezeto,szakember")]
    [HttpGet("list-components")]
    public async Task<ActionResult> ListComponent()
    {
      List<Component> component  = await new DBController().ListComponents();
      if (component.Count == 0)
      {
        return BadRequest(JsonSerializer.Serialize(new {Message = "There is no component"}));
      }
      else
      {
        return Ok(JsonSerializer.Serialize(component));
      }
    }

    [Authorize(Roles = "raktarvezeto")]
    [HttpGet("list-stack")]
    public async Task<ActionResult> ListStack()
    {
        List<StackItem> stackItems =  await new DBController().ListStack();

        if (stackItems.Count == 0)
        {
            return BadRequest(JsonSerializer.Serialize(new {Message = "There is no stack item"}));
        }
        else 
        {
            return Ok(JsonSerializer.Serialize(stackItems));
        }

    }

    [HttpPost("update-component")]
    [Authorize(Roles = "raktarvezeto")]
    public async Task<ActionResult> UpdateComponent([FromBody] Component comp)
    {
        var result = await new DBController().UpdateComponent(comp.ID, comp.Quantity) switch {
                DBController.Result.Ok => Ok(JsonSerializer.Serialize(new {Message =  "Succesfully updated new component"})),
                DBController.Result.DbException => StatusCode(500,JsonSerializer.Serialize(new {Message =  "Bad request"})),
                _  => StatusCode(500,JsonSerializer.Serialize(new {Message = "Bad request"}))
            };
        return result;
    }

    [HttpPost("join-stack")]
    [Authorize(Roles = "raktarvezeto")]
    public async Task<ActionResult> JoinStack([FromBody] StackItem stackItem)
    {
        var result = await new DBController().JoinStack(stackItem) switch {
                DBController.Result.Ok => Ok(JsonSerializer.Serialize(new {Message =  "Succesfully joined a new component"})),
                DBController.Result.DbException => StatusCode(500,JsonSerializer.Serialize(new {Message =  "Internal error"})),
                _  => StatusCode(500,JsonSerializer.Serialize(new {Message = "Internal error"}))
            };
        return result;
    }

    [HttpPost("create-project")]
    [Authorize(Roles = "szakember")]
    public async Task<ActionResult> CreateProject([FromBody] Project project)
    {
        var result = await new DBController().AddNewProject(project.name!,project.description!,project.status!,project.user_id, project.Location!) switch {
                DBController.Result.Ok => Ok(JsonSerializer.Serialize(new {Message =  "Succesfully created the project"})),
                DBController.Result.DbException => StatusCode(500,JsonSerializer.Serialize(new {Message =  "Internal error"})),
                _  => StatusCode(500,JsonSerializer.Serialize(new {Message = "Internal error"}))
            };
        
        return result;
    }

    [HttpGet("list-project")]
    [Authorize(Roles = "szakember,raktaros")]
    public async Task<ActionResult> ListProject()
    {
        var result = await new DBController().ListProjects();
        if (result.Count == 0)
        {
            return StatusCode(500,JsonSerializer.Serialize(new {Message = "Internal error"}));
        }
        else
        {
            return Ok(JsonSerializer.Serialize(result));
        }
    }

    [HttpPost("set-project-components")]
    [Authorize(Roles = "szakember")]
    public async Task<ActionResult> SetProjectComponents([FromBody] ProjectComponents projectComponents)
    {
        var result = await new DBController().SetProjectComponents(projectComponents.ProjectId!,projectComponents.ComponentId!) switch {
                DBController.Result.Ok => Ok(JsonSerializer.Serialize(new {Message =  "Succesfully filled up project components"})),
                DBController.Result.DbException => StatusCode(500,JsonSerializer.Serialize(new {Message =  "Internal error"})),
                _  => StatusCode(500,JsonSerializer.Serialize(new {Message = "Internal error"}))
            };
        
        return result;
    }
        [HttpPost("set-project-to-draft")]
    [Authorize(Roles = "szakember")]
    public async Task<ActionResult> SetProjectToDraft([FromBody] Drafter drafter)
    {
        var result = await new DBController().SetProjectToDraft(drafter.id!) switch {
                DBController.Result.Ok => Ok(JsonSerializer.Serialize(new {Message =  "Sikeres projekt státusz módosítás!"})),
                DBController.Result.DbException => StatusCode(500,JsonSerializer.Serialize(new {Message =  "Internal error"})),
                _  => StatusCode(500,JsonSerializer.Serialize(new {Message = "Internal error"}))
            };
        
        return result;
    }

    [HttpPost("add-time-and-price")]
    [Authorize(Roles = "szakember")]
    public async Task<IActionResult> AddTimeAndPrice([FromBody] TimeAndPrice timeAndPrice)
    {
        var result = await new DBController().AddTimeAndPrice(timeAndPrice.ProjectName!, timeAndPrice.Time!, timeAndPrice.Price!) switch
        {
            DBController.Result.Ok => Ok(JsonSerializer.Serialize(new { Message = "Succesfully filled up project time and price" })),
            DBController.Result.DbException => StatusCode(500, JsonSerializer.Serialize(new { Message = "Internal error" })),
            _ => StatusCode(500, JsonSerializer.Serialize(new { Message = "Internal error" }))
        };

        return result;
}

 [HttpGet("price-calculate")]
    [Authorize(Roles = "szakember")]
    public async Task<IActionResult> PriceCalculate([FromBody] PriceCalculate getprice)
    {
        var result = await new DBController().GetPriceCalculate(getprice.state!,getprice.id);
        if (result.Count == 0)
        {
            return StatusCode(500,JsonSerializer.Serialize(new {Message = "Internal error"}));
        }
        else
        {
            return Ok(JsonSerializer.Serialize(result));
        }
    }


    [HttpPost("project-close_fail")]
    [Authorize(Roles = "szakember")]
    public async Task<IActionResult> ProjectStatus([FromBody] ProjectStat projectStat)
{
    var result = await new DBController().SetProjectStatus(projectStat.ProjectName!, projectStat.status!) switch
    {
        DBController.Result.Ok => Ok(JsonSerializer.Serialize(new { Message = "Successfully filled up project status" })),
        DBController.Result.DbException => StatusCode(500, JsonSerializer.Serialize(new { Message = "Internal error" })),
        _ => StatusCode(500, JsonSerializer.Serialize(new { Message = "Internal error" }))
    };

    return result;
}

    [HttpGet("missing-component")]
    [Authorize(Roles = "raktarvezeto")]
    public async Task<ActionResult> missingComponent() 
    {
        List<MissingComponent> result = await new DBController().MissingComponent();

        if (result.Count == 0)
        {
            return Ok(JsonSerializer.Serialize(new {Message =  "There is no missing component!"}));
        }
        else 
        {
            return Ok(JsonSerializer.Serialize(result));
        }
    }

    [HttpGet("reserved-missing-component")]
    [Authorize(Roles = "raktarvezeto")]
    public async Task<ActionResult> ReservedMissingComponent() 
    {
        List<ReservedMissingComponent> result = await new DBController().ReservedMissingComponent();

        if (result.Count == 0)
        {
            return Ok(JsonSerializer.Serialize(new {Message =  "There is no missing component!"}));
        }
        else 
        {
            return Ok(JsonSerializer.Serialize(result));
        }
    }

    [HttpPost("change-project-status")]
    [Authorize(Roles = "raktaros")]
    public async Task<ActionResult> ChangeStatus([FromBody] ChangeStatus newStatus)
    {
        var result = await new DBController().ChangeProjectStatus(newStatus.ID) switch {
            DBController.Result.Ok => Ok(JsonSerializer.Serialize(new {Message =  "Succesfully changed the status"})),
            DBController.Result.DbException => StatusCode(500,JsonSerializer.Serialize(new {Message =  "There is no project like that"})),
            _  => StatusCode(500,JsonSerializer.Serialize(new {Message = "Internal error"}))
        };

        return result;
    }

    [HttpPost("set-project-status")]
    [Authorize(Roles = "szakember")]
    public async Task<ActionResult> SetProjectStatus([FromBody] SetStatus newStatus)
    {
        var result = await new DBController().ChangeProjectStatus(newStatus.ID) switch {
            DBController.Result.Ok => Ok(JsonSerializer.Serialize(new {Message =  "Succesfully changed the status"})),
            DBController.Result.DbException => StatusCode(500,JsonSerializer.Serialize(new {Message =  "There is no project like that"})),
            _  => StatusCode(500,JsonSerializer.Serialize(new {Message = "Internal error"}))
        };

        return result;
    }

    [HttpPost("component-location")]
    [Authorize(Roles = "raktaros")]
    public async Task<ActionResult> ComponentLocation([FromBody] ProjectID project)
    {
        var result = await new DBController().ComponentLocations(project.ID);

        if (result.Count == 0) 
        {
            return Ok(JsonSerializer.Serialize(new {Message =  "There is no component"}));
        }
        else
        {
            return Ok(JsonSerializer.Serialize(result));
        }
    }

    
}
