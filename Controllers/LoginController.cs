using Microsoft.AspNetCore.Mvc;

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

    
    [HttpGet("login")]
    public async Task<ActionResult> Get(string email, string password)
    {
        string? name = await new DBController().Login(email,password);
        if (name is null)
        {
            return NotFound("Wrong email or password!");
        }
        else
        {
            return Ok(name);
        }
    }
    [HttpPost("registration")]
    public async Task<ActionResult> Post(string name,string email,string password,string role)
    {
        bool result = await new DBController().Registration(name,email,password,role);
        if (result)
        {
            return Ok("Registration completed.");
        }
        else 
        {
            return StatusCode(500, new { Message = "Internal Error." });
        }
    }

    [HttpDelete("delete")]
    public async Task<ActionResult> Delete(string email, string password)
    {
        var result = await new DBController().Delete(email,password) switch {
            DBController.Result.Ok => Ok("Succesfully deleted"),
            DBController.Result.NoRecordAffected => BadRequest("Wrong email or password"),
            DBController.Result.DbException => StatusCode(500, new { Message = "Internal Error." }),
            _ => null
        };
        
        if (result is null)
        {
            return StatusCode(500, new { Message = "Internal Error." });
        }
        else 
        {
            return result;
        }
    }
}
