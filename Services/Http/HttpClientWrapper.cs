using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace AIUB.Portal.Services.Http
{
    public class HttpClientWrapper : IDisposable
    {
        private readonly HttpClient _httpClient;
        private readonly CookieContainer _cookieContainer;
        private readonly HttpClientHandler _httpClientHandler;

        public HttpClientWrapper()
        {
            _cookieContainer = new CookieContainer();
            _httpClientHandler = new HttpClientHandler
            {
                UseCookies = true,
                CookieContainer = _cookieContainer,
                AllowAutoRedirect = true, // Allow auto redirect to follow the login flow
            };

            _httpClient = new HttpClient(_httpClientHandler);

            // Add standard browser headers
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
            _httpClient.DefaultRequestHeaders.Add("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,*/*;q=0.8");
            _httpClient.DefaultRequestHeaders.Add("Accept-Language", "en-US,en;q=0.9");
        }

        public async Task<HttpResponseMessage> PostAsync(string url, Dictionary<string, string> formData)
        {
            var content = new FormUrlEncodedContent(formData);
            var request = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = content
            };

            // Add referer header
            request.Headers.Add("Referer", url);

            var response = await _httpClient.SendAsync(request);
            return response;
        }

        public async Task<HttpResponseMessage> GetAsync(string url)
        {
            return await _httpClient.GetAsync(url);
        }

        public Dictionary<string, string> GetCookies(string domain)
        {
            var cookies = _cookieContainer.GetCookies(new Uri($"https://{domain}"));
            var cookieDict = new Dictionary<string, string>();

            foreach (Cookie cookie in cookies)
            {
                cookieDict[cookie.Name] = cookie.Value;
            }

            return cookieDict;
        }

        public void Dispose()
        {
            _httpClient.Dispose();
            _httpClientHandler.Dispose();
        }

        public bool CheckIfLoginSuccessful(string responseUrl)
        {
            return responseUrl.Contains("https://portal.aiub.edu/Student");
        }

        public bool CheckIfEvaluationPending(string responseUrl)
        {
            return responseUrl.Contains("Student/Tpe/Start");
        }

        public async Task<bool> CheckForCaptcha(string responseContent)
        {
            var doc = new HtmlDocument();
            doc.LoadHtml(responseContent);
            var captchaElement = doc.DocumentNode.SelectSingleNode("//div[@id='captcha']");

            if (captchaElement != null)
            {
                var style = captchaElement.GetAttributeValue("style", "");
                return !style.Contains("display: none");
            }

            return false;
        }
    }
}
