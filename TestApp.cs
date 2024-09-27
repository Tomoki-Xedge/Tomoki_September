using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Azure.Communication.Email;
using Newtonsoft.Json;
using Microsoft.Extensions.Logging; // ログのための名前空間を追加
using System;
using Azure;

namespace SendEmailFunction
{
    public static class EmailFunction
    {
        private static readonly string connectionString = "endpoint=https://m3-tomoki-communicationservice.japan.communication.azure.com;accesskey=8NPAXRYI3HShzyxDH3nnd7E2xyyyhtplWraXYqNDcm6LtT7jT7DjJQQJ99AIACULyCpqeKhZAAAAAZCSnBbz";
        private static readonly string senderAddress = "donotreply@bac48daa-0d0d-439c-8313-ba2e5215fcf3.azurecomm.net";

        [FunctionName("SendEmail")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = null)] HttpRequest req,
            ILogger log) // ロガーを追加
        {
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            var emailRequest = JsonConvert.DeserializeObject<EmailRequest>(requestBody);

            var emailClient = new EmailClient(connectionString);
            var recipientAddress = new EmailRecipients(new[] { new EmailAddress(emailRequest.Email) });
            var content = new EmailContent(emailRequest.Subject)
            {
                PlainText = emailRequest.Content
            };
            var emailMessage = new EmailMessage(senderAddress, recipientAddress, content);

            try
            {
                var emailSendOperation = await emailClient.SendAsync(WaitUntil.Completed, emailMessage);
                return new OkObjectResult(new { OperationId = emailSendOperation.Id });
            }
            catch (Exception ex) // 例外をキャッチ
            {
                log.LogError($"Error sending email: {ex.Message}"); // エラーメッセージをログに出力
                return new StatusCodeResult(StatusCodes.Status500InternalServerError);
            }
        }
    }

    public class EmailRequest
    {
        public string Email { get; set; }
        public string Subject { get; set; }
        public string Content { get; set; }
    }
}
