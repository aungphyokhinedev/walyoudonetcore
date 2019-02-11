using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Extensions.Configuration;

public class LoginCredentials
{
    [Required]
    [StringLength(25)]
    [EmailAddress]
    public string Email { get; set; }
    [Required]
    [StringLength(25)]
    public string Password { get; set; }
}
public class UserCredentials
{
    public int UserID { get; set; }
    [Required]
    [StringLength(25)]
    [EmailAddress]
    public string Email { get; set; }
    [StringLength(25)]
    [Phone]
    public string MobileNo { get; set; }
    [Required]
    [StringLength(25)]
    public string Password { get; set; }
    [Required]
    [StringLength(25)]
    
    public string ConfirmPassword { get; set; }

    public string Salt { get; set; }
    public string Token { get; set; }
    public bool Locked { get; set; }
    public int LoginAttempt { get; set; }
    public bool Disabled { get; set; }
    public DateTime CreateDate  { get; set; }
    public DateTime CreatLastLoginDate  { get; set; }
    public DateTime LastPasswordChange  { get; set; }
}

public interface ICredentialRepository
{
    Task<Result> AddUserAsync(UserCredentials credential);
    Task<Result> LoginAsync(LoginCredentials credential);
}

public class CredentialRepository : ICredentialRepository
{
    private readonly IConfiguration _config;

    public CredentialRepository(IConfiguration config)
    {
        _config = config;
    }    

    public IDbConnection Connection
    {
        get
        {
            return new SqlConnection(_config.GetConnectionString("AppConnectionString"));
        }
    }
    public async Task<Result> LoginAsync(LoginCredentials credential)
    {
         Result result = new Result();
         using (IDbConnection conn = Connection)
        {
            try{
                 string sQuery = @"SELECT * FROM UserCredentials WHERE Email = @Email";
                 conn.Open();
                var match = await conn.QueryAsync<UserCredentials>(sQuery, new { 
                            Email = credential.Email
                            });
                var currentuser = match.FirstOrDefault();

                // user does not exit 
                if(currentuser == null) {
                    result.StatusCode = ResultCodes.AuthFail;
                    result.Description = "User Name or Password does not match";
                    return result;
                }
              
                var validate = Hash.Validate(credential.Password,currentuser.Salt,currentuser.Password);
                
                //checking username pwd
                if(validate) {
                    return result;
                }
                else{
                    result.StatusCode = ResultCodes.AuthFail;
                    result.Description = "User Name or Password does not match";
                    return result;
                }

            }catch(Exception ex){
                result.StatusCode = ResultCodes.Error;
                result.Description = ex.Message;
                return result;
            }
        }
    }
    public async Task<Result> AddUserAsync(UserCredentials credential)
    {
        Result result = new Result();
        //checking passwords 
        if(credential.Password != credential.ConfirmPassword){
                result.StatusCode = ResultCodes.DataError;
                result.Description = "Password does not match";
                return result;
        }
        // hashing password
        string salt = Salt.Create();
        credential.Password = Hash.Create(credential.Password,salt);

        using (IDbConnection conn = Connection)
        {
            try{
            string currenttime = DateTime.Now.ToString();

            string sQuery = @"INSERT INTO UserCredentials (
                Email,
                MobileNo, 
                Password, 
                Salt,
                Token,
                LoginAttempt,
                UserType,
                CreateDate,
                LastLogin, 
                LastPasswordChange) 
                VALUES ( 
                @Email,
                @MobileNo, 
                @Password, 
                @Salt,
                '',
                0,
                0,
                @CreateDate,
                @LastLogin, 
                @LastPasswordChange);
                SELECT CAST(SCOPE_IDENTITY() as int)";
            conn.Open();
            var id = await conn.QueryAsync<int>(sQuery, new { 
                Email = credential.Email,
                MobileNo = credential.MobileNo,
                Password = credential.Password,
                Salt = salt,
                CreateDate = currenttime,
                LastLogin = currenttime,
                LastPasswordChange = currenttime });


            // return id is 0, insertion fail
            if(id.Single() > 0) {
                Console.WriteLine(id.Single());
                return result;
            }
            else{
                result.StatusCode = ResultCodes.DBError;
                result.Description = "Cannot insert user";
                return result;
            }

            }catch(Exception ex){
                result.StatusCode = ResultCodes.Error;
                result.Description = ex.Message;
                return result;
            }
        }
    }

}
