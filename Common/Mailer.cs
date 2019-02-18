using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Gmail.v1;
using Google.Apis.Gmail.v1.Data;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using static Microsoft.AspNetCore.Hosting.Internal.HostingApplication;

public class Mailer : IMailer
{
    public EmailSettings _emailSettings { get; }
    private readonly ILogger<Mailer> _logger;
    private GmailService _gmail {get;}
    public Mailer(IOptions<EmailSettings> emailSettings, ILogger<Mailer> logger)
    {
        _emailSettings = emailSettings.Value;
        _logger = logger;

         UserCredential credential;

            using (var stream = new FileStream("credentials.json", FileMode.Open, FileAccess.Read))
            {
                // The file token.json stores the user's access and refresh tokens, and is created
                // automatically when the authorization flow completes for the first time.
                string credPath = "token.json";
                credential = GoogleWebAuthorizationBroker.AuthorizeAsync(
                    GoogleClientSecrets.Load(stream).Secrets,
                    Scopes,
                    "user",
                    CancellationToken.None,
                    new FileDataStore(credPath, true)).Result;
                Console.WriteLine("Credential file saved to: " + credPath);
            }

            // Create Gmail API service.
            _gmail = new GmailService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = credential,
                ApplicationName = _emailSettings.ApplicationName,
            });
    }
    public void SendEmailAsync(EmailMessage message)
    {

        Execute(message);
    }

    // If modifying these scopes, delete your previously saved credentials
    // at ~/.credentials/gmail-dotnet-quickstart.json
    static string[] Scopes = { GmailService.Scope.GmailSend };
    private static string Base64UrlEncode(string input)
    {
        var inputBytes = System.Text.Encoding.UTF8.GetBytes(input);
        // Special "url-safe" base64 encode.
        return Convert.ToBase64String(inputBytes)
          .Replace('+', '-')
          .Replace('/', '_')
          .Replace("=", "");
    }
    public void Execute(EmailMessage message)
    {
        try
        {
            System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
            var msg = new AE.Net.Mail.MailMessage
            {
                Subject = message.Subject,
                Body = message.Body,
                From = new MailAddress(_emailSettings.FromEmail)
            };
            msg.To.Add(new MailAddress(message.ReceiverEmail));
            msg.ReplyTo.Add(msg.From); // Bounces without this!!
            var msgStr = new StringWriter();
            msg.Save(msgStr);
            
            // Context is a separate bit of code that provides OAuth context;
            // your construction of GmailService will be different from mine.
            _logger.LogDebug(msgStr.ToString());
            var result = _gmail.Users.Messages.Send(new Message
            {
                Raw = Base64UrlEncode(msgStr.ToString())
            }, "me").Execute();
            
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex.StackTrace);
        }


    }
}
