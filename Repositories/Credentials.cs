using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Data.SqlClient;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;




public class CredentialRepository : ICredentialRepository
{
    private readonly IConfiguration _config;
    private readonly ILogger<CredentialRepository> _logger;
    private readonly IMailer _mailer;
    private const string ValidAudience = "YOUR_AUDIENCE_VALUE_HERE";
    private const string ValidIssuer = "self";
    public CredentialRepository(IConfiguration config, ILogger<CredentialRepository> logger, IMailer mailer)
    {
        _config = config;
        _logger = logger;
        _mailer = mailer;
    }

    public string PasswordResetURL
    {
        get
        {
            return _config.GetSection("PasswordResetURL").Value;
        }
    }

    public string JWTSecret
    {
        get
        {
            return _config.GetSection("JWTSecret").Value;
        }
    }

    public int TokenLifeMinutes
    {
        get
        {
            return Convert.ToInt32(_config.GetSection("TokenLifeMinutes").Value);
        }
    }
    public int LoginAttempt
    {
        get
        {
            return Convert.ToInt32(_config.GetSection("LoginAttempt").Value);
        }
    }

    public IDbConnection Connection
    {
        get
        {
            return new SqlConnection(_config.GetConnectionString("AppConnectionString"));
        }
    }

    public Result TokenValidate(string token)
    {
        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();

            SecurityKey key = new SymmetricSecurityKey(System.Text.Encoding.ASCII.GetBytes(JWTSecret));
            var validationParameters = new TokenValidationParameters()
            {
                IssuerSigningKey = key,
                ValidAudience = ValidAudience,
                ValidIssuer = ValidIssuer,
            };

            SecurityToken validatedToken;
            var principal = tokenHandler.ValidateToken(token, validationParameters, out validatedToken);
            _logger.LogDebug(principal.Claims.Where(c => c.Type == "Email").FirstOrDefault().Value);
            if (principal != null)
            {
                return new Result
                {
                    StatusCode = ResultCodes.Success,
                    Description = "Token validation success"
                };
            }
            else
            {
                return new Result
                {
                    StatusCode = ResultCodes.AuthFail,
                    Description = "Token validation fails"
                };
            }

        }
        catch (Exception ex)
        {
            _logger.LogError(ex.StackTrace);
            return new Result
            {
                StatusCode = ResultCodes.AuthFail,
                Description = "Token validation fails"
            };
        }
    }
    public async Task<Result> LoginAsync(ModelLoginCredentials credential)
    {
        Result result = new Result();
        using (IDbConnection conn = Connection)
        {
            try
            {
                string sQuery = @"SELECT * FROM UserCredentials WHERE Email = @Email";
                conn.Open();
                var match = await conn.QueryAsync<ModelUserCredentials>(sQuery, new
                {
                    Email = credential.Email
                });
                var currentuser = match.FirstOrDefault();

                // user does not exit 
                if (currentuser == null)
                {
                    result.StatusCode = ResultCodes.AuthFail;
                    result.Description = "User Name or Password does not match";
                    return result;
                }

                // checking account status
                var checkresult = checkAccountStatus(currentuser);
                if (checkresult.StatusCode != ResultCodes.Success)
                {
                    return checkresult;
                }

                var validate = Hash.Validate(credential.Password, currentuser.Salt, currentuser.Password);

                //checking username pwd
                if (validate)
                {
                    // authentication successful so generate jwt token
                    var tokenHandler = new JwtSecurityTokenHandler();
                    var key = Encoding.ASCII.GetBytes(JWTSecret);
                    var tokenDescriptor = new SecurityTokenDescriptor
                    {
                        Subject = new ClaimsIdentity(new Claim[]
                        {
                            new Claim(ClaimTypes.Name, currentuser.UserID.ToString()),
                            new Claim("Email",currentuser.Email ),
                            new Claim("UserType",currentuser.UserType.ToString() )
                        }),
                        Audience = ValidAudience,
                        Issuer = ValidIssuer,
                        Expires = DateTime.UtcNow.AddMinutes(TokenLifeMinutes),
                        SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
                    };
                    // change login status
                    setLoginStatus(currentuser.UserID, 0);

                    var token = tokenHandler.CreateToken(tokenDescriptor);
                    result.Data = tokenHandler.WriteToken(token);
                    return result;
                }
                else
                {
                    setLoginStatus(currentuser.UserID, currentuser.LoginAttempt + 1);
                    result.StatusCode = ResultCodes.AuthFail;
                    result.Description = "User Name or Password does not match";
                    return result;
                }

            }
            catch (Exception ex)
            {
                result.StatusCode = ResultCodes.Error;
                result.Description = ex.Message;
                return result;
            }
        }
    }

    private Result checkAccountStatus(ModelUserCredentials credentials)
    {
        var result = new Result();
        if (credentials.Locked)
        {
            result.StatusCode = ResultCodes.AuthFail;
            result.Description = "Too many login fail, account is locked";
        }
        if (!credentials.Verified)
        {
            result.StatusCode = ResultCodes.AuthFail;
            result.Description = "Your email need to verfy first";
        }
        if (credentials.Disabled)
        {
            result.StatusCode = ResultCodes.AuthFail;
            result.Description = "Your account is disabled";
        }
        return result;
    }
    private void setLoginStatus(int userID, int loginAttempt)
    {
        _logger.LogDebug(userID + "?" + loginAttempt);
        using (IDbConnection conn = Connection)
        {
            var shouldLock = loginAttempt > LoginAttempt;

            string sQuery = @"UPDATE UserCredentials SET 
                LoginAttempt = @LoginAttempt,
                LastLogin = @LastLogin,
                Locked = @Locked
                WHERE UserID = @UserID;";
            conn.Open();
            conn.ExecuteAsync(sQuery, new { LastLogin = DateTime.Now, LoginAttempt = loginAttempt, Locked = shouldLock, UserID = userID });
        }
    }
    public async Task<Result> AddUserAsync(ModelUserCredentials credential)
    {
        Result result = new Result();
        //checking passwords 
        if (credential.Password != credential.ConfirmPassword)
        {
            result.StatusCode = ResultCodes.DataError;
            result.Description = "Password does not match";
            return result;
        }
        // hashing password
        string salt = Salt.Create();
        credential.Password = Hash.Create(credential.Password, salt);
        Random generator = new Random();
        credential.VerificationCode = generator.Next(0, 999999).ToString("D6");
        using (IDbConnection conn = Connection)
        {
            try
            {
                string sQuery = @"SELECT * FROM UserCredentials WHERE Email = @Email";
                conn.Open();
                var match = await conn.QueryAsync<ModelUserCredentials>(sQuery, new
                {
                    Email = credential.Email
                });
                var currentuser = match.Count();
                if (currentuser > 0)
                {
                    result.StatusCode = ResultCodes.DataError;
                    result.Description = "Email is already used";
                    return result;
                }


                string currenttime = DateTime.Now.ToString();
                sQuery = @"INSERT INTO UserCredentials (
                Email,
                MobileNo, 
                Password, 
                Salt,
                Token,
                LoginAttempt,
                UserType,
                Verified,
                VerificationCode,
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
                0,
                @VerificationCode,
                @CreateDate,
                @LastLogin, 
                @LastPasswordChange);
                SELECT CAST(SCOPE_IDENTITY() as int)";
                var id = await conn.QueryAsync<int>(sQuery, new
                {
                    Email = credential.Email,
                    MobileNo = credential.MobileNo,
                    Password = credential.Password,
                    Salt = salt,
                    VerificationCode = credential.VerificationCode,
                    CreateDate = currenttime,
                    LastLogin = currenttime,
                    LastPasswordChange = currenttime
                });


                // return id is 0, insertion fail
                if (id.Single() > 0)
                {

                    var message = new EmailMessage
                    {
                        ReceiverEmail = credential.Email,
                        Subject = "Email Verification",
                        Body = "To verify email, please use this code " + credential.VerificationCode
                    };
                    _mailer.SendEmailAsync(message);
                    Console.WriteLine(id.Single());
                    return result;
                }
                else
                {
                    result.StatusCode = ResultCodes.DBError;
                    result.Description = "Cannot insert user";
                    return result;
                }

            }
            catch (Exception ex)
            {
                result.StatusCode = ResultCodes.Error;
                result.Description = ex.Message;
                return result;
            }
        }
    }

    public async Task<Result> ChangePasswordAsync(ModelChangePassword changePassword)
    {
        // check password and confirm password
        if (changePassword.NewPassword != changePassword.ConfirmPassword)
        {
            return new Result
            {
                StatusCode = ResultCodes.DataError,
                Description = "Password does not match"
            };
        }

        var result = await LoginAsync(new ModelLoginCredentials
        {
            Email = changePassword.Email,
            Password = changePassword.CurrentPassword
        });
        /// authentication passed
        if (result.StatusCode == ResultCodes.Success)
        {
            using (IDbConnection conn = Connection)
            {
                try
                {
                    string sQuery = @"UPDATE UserCredentials SET 
                    Password = @Password,
                    Salt = @Salt,
                    LastPasswordChange = @LastPasswordChange
                    WHERE Email = @Email;";
                    string currenttime = DateTime.Now.ToString();
                    string salt = Salt.Create();
                    changePassword.NewPassword = Hash.Create(changePassword.NewPassword, salt);
                    conn.Open();
                    int effectedrows = await conn.ExecuteAsync(sQuery, new { Password = changePassword.NewPassword, Salt = salt, LastPasswordChange = currenttime, Email = changePassword.Email });

                    if (effectedrows > 0)
                    {
                        return new Result
                        {
                            StatusCode = ResultCodes.Success,
                            Description = "Password has been changed"
                        };
                    }
                    else
                    {
                        return new Result
                        {
                            StatusCode = ResultCodes.DBError,
                            Description = "Password  changing fail"
                        };
                    }

                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex.StackTrace);

                    return new Result
                    {
                        StatusCode = ResultCodes.Error,
                        Description = "Error, Password  changing fail"
                    };
                }
            }
        }
        else
        {
            return new Result
            {
                StatusCode = ResultCodes.AuthFail,
                Description = "Old password is not correct"
            };
        }

    }

    public async Task<Result> SendResetPasswordMailAsync(string email)
    {
        using (IDbConnection conn = Connection)
        {
            try
            {
                string sQuery = @"UPDATE UserCredentials SET 
                    ResetToken = @ResetToken
                    WHERE Email = @Email;";
                conn.Open();
                var resettoken = Guid.NewGuid().ToString();
                int effectedrows = await conn.ExecuteAsync(sQuery, new { ResetToken = resettoken, Email = email });

                if (effectedrows > 0)
                {

                    var message = new EmailMessage
                    {
                        ReceiverEmail = email,
                        Subject = "Reseting password",
                        Body = "to reset password, click the below link " + PasswordResetURL + "?token=" + resettoken + "&&email=" + email
                    };
                    _mailer.SendEmailAsync(message);

                    return new Result
                    {
                        StatusCode = ResultCodes.Success,
                        Description = "Password reset mail has been sent"
                    };
                }
                else
                {
                    return new Result
                    {
                        StatusCode = ResultCodes.DBError,
                        Description = "Reset mail cannot be sent"
                    };
                }

            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex.StackTrace);

                return new Result
                {
                    StatusCode = ResultCodes.Error,
                    Description = "Error, Reset mail cannot be sent"
                };
            }
        }
    }

    public async Task<Result> VerifyEmailAsync(ModelVerification modelVerification)
    {

        using (IDbConnection conn = Connection)
        {
            try
            {
                _logger.LogDebug(modelVerification.Code + ".." + modelVerification.Email);
                string sQuery = @"UPDATE UserCredentials SET 
                    Verified = 1
                    WHERE Email = @Email AND 
                    VerificationCode = @VerificationCode;";
                conn.Open();
                int effectedrows = await conn.ExecuteAsync(sQuery, new { Email = modelVerification.Email, VerificationCode = modelVerification.Code });
                _logger.LogDebug(effectedrows + " ef");
                if (effectedrows > 0)
                {
                    return new Result
                    {
                        StatusCode = ResultCodes.Success,
                        Description = "Email has been verified"
                    };
                }
                else
                {
                    return new Result
                    {
                        StatusCode = ResultCodes.AuthFail,
                        Description = "Verification fails"
                    };
                }

            }
            catch (Exception ex)
            {
                return new Result
                {
                    StatusCode = ResultCodes.Error,
                    Description = "Verification fail"
                };
            }
        }

    }

    public async Task<Result> ResetPasswordAsync(ModelResetPassword resetPassword)
    {
        // check password and confirm password
        if (resetPassword.NewPassword != resetPassword.ConfirmPassword)
        {
            return new Result
            {
                StatusCode = ResultCodes.DataError,
                Description = "Password does not match"
            };
        }

        using (IDbConnection conn = Connection)
        {
            try
            {
                string sQuery = @"UPDATE UserCredentials SET 
                    Password = @Password,
                    Salt = @Salt,
                    ResetToken = @NewResetToken,
                    LastPasswordChange = @LastPasswordChange
                    WHERE Email = @Email AND
                    ResetToken = @ResetToken;";
                var newresettoken = Guid.NewGuid().ToString();
                string currenttime = DateTime.Now.ToString();
                string salt = Salt.Create();
                resetPassword.NewPassword = Hash.Create(resetPassword.NewPassword, salt);
                conn.Open();
                int effectedrows = await conn.ExecuteAsync(sQuery, new
                {
                    Password = resetPassword.NewPassword,
                    Salt = salt,
                    LastPasswordChange = currenttime,
                    NewResetToken = newresettoken,
                    ResetToken = resetPassword.ResetToken,
                    Email = resetPassword.Email
                });

                if (effectedrows > 0)
                {
                    return new Result
                    {
                        StatusCode = ResultCodes.Success,
                        Description = "Password has been changed"
                    };
                }
                else
                {
                    return new Result
                    {
                        StatusCode = ResultCodes.DBError,
                        Description = "Reseting Password fail"
                    };
                }

            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex.StackTrace);

                return new Result
                {
                    StatusCode = ResultCodes.Error,
                    Description = "Error, Reseting Password fail"
                };
            }
        }
        
    }
}
