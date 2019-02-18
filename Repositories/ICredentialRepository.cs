using System.Threading.Tasks;

public interface ICredentialRepository
{
    Task<Result> AddUserAsync(ModelUserCredentials credential);
    Task<Result> ChangePasswordAsync(ModelChangePassword changePassword);
    Task<Result> SendResetPasswordMailAsync(string email);

    Task<Result> ResetPasswordAsync(ModelResetPassword resetPassword);

    Task<Result> VerifyEmailAsync(ModelVerification modelVerification);
    Task<Result> LoginAsync(ModelLoginCredentials credential);
    Result TokenValidate(string token);
}
