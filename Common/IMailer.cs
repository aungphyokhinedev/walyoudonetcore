using System.Threading.Tasks;

public interface IMailer
{
    void SendEmailAsync(EmailMessage message);
}