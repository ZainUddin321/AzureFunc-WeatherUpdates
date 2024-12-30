using System;
using System.Text;
using System.Net;
using System.Net.Mail;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;


namespace WeatherUpdate.Function
{
    public class WeatherUpdate
    {
        private readonly ILogger _logger;

        public WeatherUpdate(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<WeatherUpdate>();
        }

        [Function("WeatherUpdate")]
        public async Task Run([TimerTrigger("0 0 4 * * *")] TimerInfo myTimer)
        {
            _logger.LogInformation($"C# Timer trigger function executed at: {DateTime.Now}");
            string response = await GetWeatherUpdate("Karachi");
            if (myTimer.ScheduleStatus is not null)
            {
                _logger.LogInformation($"{response}");
                _logger.LogInformation($"Next timer schedule at: {myTimer.ScheduleStatus.Next}");
            }
        }

        private static async Task<string> GetWeatherUpdate(string city){
            string apiKey = Environment.GetEnvironmentVariable("APIKEY");
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

                    string htmlTemplate = GetHtmlTemplate("EmailTemplate.html");
                    string emailBody = htmlTemplate
                    .Replace("{Description}", description)
                    .Replace("{Temperature}", weatherData["main"]["temp"].ToString())
                    .Replace("{FeelsLike}", weatherData["main"]["feels_like"].ToString())
                    .Replace("{Humidity}", weatherData["main"]["humidity"].ToString());

                    // Display results
                    StringBuilder result = new StringBuilder();
                    result.AppendLine($"Weather in {city}:");
                    result.AppendLine($"- Description: {description}");
                    result.AppendLine($"- Temperature: {temperature}°C");
                    result.AppendLine($"- Feels Like: {feelsLike}°C");
                    result.AppendLine($"- Humidity: {humidity}%");
                    // _logger.LogInformation("C# HTTP trigger function processed a request.")
                    // Send email
                    await SendEmail(city, emailBody);
                    return result.ToString();
                }
                catch (Exception ex)
                {
                    return $"Error: {ex.Message}";
                }
            }
        }
        private static async Task SendEmail(string city, string emailBody)
        {
            string smtpServer = "smtp.gmail.com";  // For Gmail
            int smtpPort = 587; // For TLS
            string smtpUser = Environment.GetEnvironmentVariable("EMAIL");  // Your email address
            string smtpPassword = "smyigvotweimbibd";  // Use App Password if 2FA is enabled
            string toEmail = Environment.GetEnvironmentVariable("EMAIL");

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
                    Body = emailBody,
                    IsBodyHtml = true,
                };

                // Send the email asynchronously
                await smtpClient.SendMailAsync(message);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error sending email: {ex.Message}");
            }
        }

        public static string GetHtmlTemplate(string templateFileName)
        {
            // Combine the root directory with the relative path to the file
            string rootPath = Environment.GetEnvironmentVariable("ProjectRoot")
                            ?? Path.Combine(Environment.GetEnvironmentVariable("HOME"), "site");

            string templatePath = Path.Combine(rootPath, "wwwroot/templates", templateFileName);
            // Read the file content
            return File.ReadAllText(templatePath);
        }
    }
}
