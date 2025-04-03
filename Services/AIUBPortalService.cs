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

        public async Task<(bool success, string msg, Dictionary<string, object> result, string captchaImageUrl, string captchaId)> LoginAsync(string username, string password)
        {
            try
            {
                Debug.WriteLine($"Attempting login for user: {username}");
                const string url = "https://portal.aiub.edu";

                // Try to load cookies to see if we have a valid session
                await _httpClient.LoadCookiesFromStorage();

                // Create form content for the login request
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
                    var captchaDetails = await _httpClient.GetCaptchaDetails(responseContent);
                    if (captchaDetails.success)
                    {
                        return (false, "Captcha required", null, captchaDetails.captchaImageUrl, captchaDetails.captchaId);
                    }
                    return (false, "Captcha required but couldn't extract details", null, null, null);
                }

                // Check if login successful by examining the URL
                if (!_httpClient.CheckIfLoginSuccessful(response.RequestMessage.RequestUri.ToString()))
                {
                    Debug.WriteLine($"Login failed. Response URL: {response.RequestMessage.RequestUri}");
                    return (false, "Invalid username or password", null, null, null);
                }

                // Check if evaluation pending
                if (_httpClient.CheckIfEvaluationPending(response.RequestMessage.RequestUri.ToString()))
                {
                    return (false, "TPE Evaluation pending on portal", null, null, null);
                }

                Debug.WriteLine("Login successful. Getting student page...");

                // Get the student page
                var studentPageResponse = await _httpClient.GetAsync("https://portal.aiub.edu/Student");
                var studentPageContent = await studentPageResponse.Content.ReadAsStringAsync();

                // Extract cookies for use in subsequent requests
                var cookies = _httpClient.GetCookies("portal.aiub.edu");

                // Process student data
                var result = await _studentService.GetStudentDataAsync(studentPageContent, cookies);
                return (true, "Login successful", result, null, null);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Exception during login: {ex.Message}");
                Debug.WriteLine($"Stack trace: {ex.StackTrace}");
                return (false, $"Error: {ex.Message}", null, null, null);
            }
        }

        public async Task<(bool success, string msg, Dictionary<string, object> result)> SubmitCaptchaAsync(string username, string password, string captchaCode, string captchaId)
        {
            try
            {
                Debug.WriteLine($"Submitting captcha for user: {username}");
                const string url = "https://portal.aiub.edu";

                // Create form content for the login request with captcha
                var formData = new Dictionary<string, string>
                {
                    { "UserName", username },
                    { "Password", password },
                    { "CaptchaCode", captchaCode },
                    { "CaptchaId", captchaId }
                };

                // Post login request with captcha
                Debug.WriteLine("Sending login request with captcha...");
                var response = await _httpClient.PostAsync(url, formData);
                Debug.WriteLine($"Login response status: {response.StatusCode}");

                // Get the response content for analysis
                var responseContent = await response.Content.ReadAsStringAsync();

                // Check if login successful
                if (!_httpClient.CheckIfLoginSuccessful(response.RequestMessage.RequestUri.ToString()))
                {
                    Debug.WriteLine($"Login failed. Response URL: {response.RequestMessage.RequestUri}");
                    return (false, "Invalid username or password", null);
                }

                // Check if evaluation pending
                if (_httpClient.CheckIfEvaluationPending(response.RequestMessage.RequestUri.ToString())) return (false, "TPE Evaluation pending on portal", null);

                Debug.WriteLine("Login successfull. Getting student page...");

                // Get the student page
                var studentPageResponse = await _httpClient.GetAsync("https://portal.aiub.edu/Student");
                var studentPageContent = await studentPageResponse.Content.ReadAsStringAsync();

                var cookies = _httpClient.GetCookies("portal.aiub.edu");

                // save cookies for the future session
                _httpClient.SaveCookiesToStorage();

                // Process student data
                var result = await _studentService.GetStudentDataAsync(studentPageContent, cookies);
                return (true, "Login successful", result);
            } catch (Exception ex)
            {
                Debug.WriteLine($"Exception during captcha submission: {ex.Message}");
                Debug.WriteLine($"Stack trace: {ex.StackTrace}");
                return (false, $"Error: {ex.Message}", null);
            }
        }

        public async Task<bool> TryLoadSavedSession()
        {
            try
            {
                // try to load save cookies
                if (await _httpClient.LoadCookiesFromStorage())
                {
                    // Test if the session is still valid by making a request to the student page
                    var response = await _httpClient.GetAsync("https://portal.aiub.edu/Student");

                    // If we get redirected to the login page, the session is invalid
                    if (response.RequestMessage.RequestUri.ToString().Contains("portal.aiub.edu/Student"))
                    {
                        Debug.WriteLine("Session is still valid");
                        return true;
                    }

                    Debug.WriteLine("Session expired");
                }
                return false;
            } catch
            {
                Debug.WriteLine("Error loading saved session");
                return false;
            }
        }

        public void Dispose()
        {
            _httpClient.Dispose();
        }
    }
}
