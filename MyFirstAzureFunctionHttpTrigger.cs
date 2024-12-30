using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Net.Http;
using System.Net.Mail;
using Newtonsoft.Json.Linq;
using Microsoft.AspNetCore.Http.HttpResults;
using System.Text;

namespace Company.Function
{
    public class MyFirstAzureFunctionHttpTrigger
    {
        private readonly ILogger<MyFirstAzureFunctionHttpTrigger> _logger;

        public MyFirstAzureFunctionHttpTrigger(ILogger<MyFirstAzureFunctionHttpTrigger> logger)
        {
            _logger = logger;
        }

        [Function("MyFirstAzureFunctionHttpTrigger")]
        public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequest req)
        {
            string apiKey = Environment.GetEnvironmentVariable("APIKEY");
            string city = "Karachi";
            string url = Environment.GetEnvironmentVariable("APIURL");

            using (HttpClient client = new HttpClient())
            {
                try
                {
                    HttpResponseMessage response = await client.GetAsync(url);
                    response.EnsureSuccessStatusCode();

                    // Parse response
                    string responseBody = await response.Content.ReadAsStringAsync();
                    JObject weatherData = JObject.Parse(responseBody);

                    // Extract weather details
                    string description = weatherData["weather"][0]["description"].ToString();
                    double temperature = Convert.ToDouble(weatherData["main"]["temp"]);
                    double feelsLike = Convert.ToDouble(weatherData["main"]["feels_like"]);
                    double humidity = Convert.ToDouble(weatherData["main"]["humidity"]);

                    // Display results
                    StringBuilder result = new StringBuilder();
                    result.AppendLine($"Weather in {city}:");
                    result.AppendLine($"- Description: {description}");
                    result.AppendLine($"- Temperature: {temperature}°C");
                    result.AppendLine($"- Feels Like: {feelsLike}°C");
                    result.AppendLine($"- Humidity: {humidity}%");
                    // _logger.LogInformation("C# HTTP trigger function processed a request.")
                    // Send email
                    await SendEmail(city, result.ToString());
                    return new OkObjectResult(result.ToString());
                }
                catch (Exception ex)
                {
                    return new BadRequestObjectResult($"Error: {ex.Message}");
                }
            }
        }

        private static async Task SendEmail(string city, string weatherInfo)
        {
            string smtpServer = "smtp.gmail.com";  // For Gmail
            int smtpPort = 587; // For TLS
            string smtpUser = "csdsubmission1@gmail.com";  // Your email address
            string smtpPassword = "smyigvotweimbibd";  // Use App Password if 2FA is enabled
            string toEmail = "csdsubmission1@gmail.com";

            try
            {
                var smtpClient = new SmtpClient(smtpServer)
                {
                    Port = smtpPort,
                    Credentials = new NetworkCredential(smtpUser, smtpPassword),
                    EnableSsl = true,
                };

                var fromEmail = new MailAddress(smtpUser, "Weather App - AZ Function");
                var toEmailAddress = new MailAddress(toEmail);
                var message = new MailMessage(fromEmail, toEmailAddress)
                {
                    Subject = $"Weather Update for {city}",
                    Body = weatherInfo,
                    IsBodyHtml = false,
                };

                // Send the email asynchronously
                await smtpClient.SendMailAsync(message);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error sending email: {ex.Message}");
            }
        }
    }
}
