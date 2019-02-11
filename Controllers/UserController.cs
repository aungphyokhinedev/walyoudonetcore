using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

[Route("api/[controller]")]
[ApiController]
public class UserController : ControllerBase
{
    private readonly IUserRepository _userRepo;
    private readonly ICredentialRepository _credRepo;

    public UserController(IUserRepository userRepo,ICredentialRepository credRepo)
    {
        _userRepo = userRepo;
        _credRepo = credRepo;
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<LoginUser>> GetByID(int id)
    {
        return await _userRepo.GetByIDAsync(id);
    }


    [HttpGet("dob/{dateOfBirth}")]
   // [Route("dob/{dateOfBirth}")]
    public async Task<ActionResult<List<LoginUser>>> GetByDateOfBirth(DateTime dateOfBirth)
    {
        return await _userRepo.GetByDateOfBirthAsync(dateOfBirth);
    }

    [HttpPost("register")]
   // [Route("dob/{dateOfBirth}")]
    public async Task<ActionResult<Result>> AddUser(UserCredentials cred)
    { Console.WriteLine("register");
        return await _credRepo.AddUserAsync(cred);
    }

     [HttpPost("login")]
    public async Task<ActionResult<Result>> Login(LoginCredentials cred)
    {
        Console.WriteLine("Login");
        return await _credRepo.LoginAsync(cred);
    }
}