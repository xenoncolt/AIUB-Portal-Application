// Services/AIUBPortalService.cs
using AIUB.Portal.Services.Http;
using AIUB.Portal.Services.Portal;
using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;

namespace AIUB.Portal.Services
{
    public class AIUBPortalService : IAIUBPortalService, IDisposable
    {
        private readonly HttpClientWrapper _httpClient;
        private readonly StudentService _studentService;

        public AIUBPortalService()
        {
            _httpClient = new HttpClientWrapper();
            var courseService = new CourseService(_httpClient);
            var curriculumService = new CurriculumService(_httpClient);
            var courseAnalysisService = new CourseAnalysisService();

            _studentService = new StudentService(_httpClient, courseService, curriculumService, courseAnalysisService);
        }

        public async Task<(bool success, string msg, Dictionary<string, object> result)> LoginAsync(string username, string password)
        {
            try
            {
                Debug.WriteLine($"Attempting login for user: {username}");
                const string url = "https://portal.aiub.edu";

                // Create form content for the login request - match exactly what's in main.py
                var formData = new Dictionary<string, string>
                {
                    { "UserName", username },
                    { "Password", password }
                };

                // Post login request
                Debug.WriteLine("Sending login request...");
                var response = await _httpClient.PostAsync(url, formData);
                Debug.WriteLine($"Login response status: {response.StatusCode}");

                // Get the response content for analysis
                var responseContent = await response.Content.ReadAsStringAsync();

                // Check if we need to handle captcha
                if (await _httpClient.CheckForCaptcha(responseContent))
                {
                    return (false, "Captcha required. Please login via the portal website first to solve the captcha.", null);
                }

                // Check if login successful by examining the URL
                if (!_httpClient.CheckIfLoginSuccessful(response.RequestMessage.RequestUri.ToString()))
                {
                    Debug.WriteLine($"Login failed. Response URL: {response.RequestMessage.RequestUri}");
                    return (false, "Invalid username or password", null);
                }

                // Check if evaluation pending
                if (_httpClient.CheckIfEvaluationPending(response.RequestMessage.RequestUri.ToString()))
                {
                    return (false, "TPE Evaluation pending on portal", null);
                }

                Debug.WriteLine("Login successful. Getting student page...");

                // Get the student page
                var studentPageResponse = await _httpClient.GetAsync("https://portal.aiub.edu/Student");
                var studentPageContent = await studentPageResponse.Content.ReadAsStringAsync();

                // Extract cookies for use in subsequent requests
                var cookies = _httpClient.GetCookies("portal.aiub.edu");

                // Process student data
                var result = await _studentService.GetStudentDataAsync(studentPageContent, cookies);
                return (true, "Login successful", result);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Exception during login: {ex.Message}");
                Debug.WriteLine($"Stack trace: {ex.StackTrace}");
                return (false, $"Error: {ex.Message}", null);
            }
        }

        public void Dispose()
        {
            _httpClient.Dispose();
        }
    }
}
