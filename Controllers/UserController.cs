using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

[Route("api/[controller]")]
[ApiController]
public class UserController : ControllerBase
{
    private readonly ICredentialRepository _credRepo;

    private readonly ILogger<UserController> _logger;


    public UserController(ICredentialRepository credRepo,ILogger<UserController> logger)
    {
        _credRepo = credRepo;
        _logger = logger;
    }





    [HttpPost("register")]
   // [Route("dob/{dateOfBirth}")]
    public async Task<ActionResult<Result>> AddUser(ModelUserCredentials cred)
    { 
        return await _credRepo.AddUserAsync(cred);
    }

    [HttpPost("tokenvalidate")]
   // [Route("dob/{dateOfBirth}")]
    public object TokenValidate(ModelTokenCredentials  Token)
    { 
        return  _credRepo.TokenValidate(Token.Token);
    }

    [HttpPost("emailverify")]
   // [Route("dob/{dateOfBirth}")]
    public async Task<ActionResult<Result>>  EmailVerify(ModelVerification  verification)
    { 
        return await  _credRepo.VerifyEmailAsync(verification);
    }

    [HttpPost("passwordchange")]
   // [Route("dob/{dateOfBirth}")]
    public async Task<ActionResult<Result>>  PasswordChange(ModelChangePassword  changePassword)
    { 
        return await  _credRepo.ChangePasswordAsync(changePassword);
    }

    [HttpPost("sendresetpasswordmail/{email}")]
   // [Route("dob/{dateOfBirth}")]
    public async Task<ActionResult<Result>>   SendResetPasswordMail(string email)
    { 
        return  await _credRepo.SendResetPasswordMailAsync(email);
    }

    [HttpPost("resetpassword")]
   // [Route("dob/{dateOfBirth}")]
    public async Task<ActionResult<Result>>   ResetPassword(ModelResetPassword resetpassword)
    { 
        return  await _credRepo.ResetPasswordAsync(resetpassword);
    }

    [HttpPost("login")]
    public async Task<ActionResult<Result>> Login(ModelLoginCredentials cred)
    {
        
        Console.WriteLine("Login");
        return await _credRepo.LoginAsync(cred);
    }
}