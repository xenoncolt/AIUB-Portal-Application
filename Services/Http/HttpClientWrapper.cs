using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace AIUB.Portal.Services.Http
{
    public class CookieData
    {
        public string Name { get; set; }
        public string Value { get; set; }
        public string Domain { get; set; }
        public string Path { get; set; }
        public DateTime Expires { get; set; }
        public bool HttpOnly { get; set; }
        public bool Secure { get; set; }
    }
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

        public async Task<(bool success, string captchaImageUrl, string captchaId)> GetCaptchaDetails(string responseContent)
        {
            var doc = new HtmlDocument();
            doc.LoadHtml(responseContent);

            var captchaElement = doc.DocumentNode.SelectSingleNode("//div[@id='captcha']");
            if (captchaElement != null)
            {
                var captchaImg = captchaElement.SelectSingleNode(".//img");
                var captchaIdInput = doc.DocumentNode.SelectSingleNode("//input[@name='CaptchaId']");
                string captchaId = captchaIdInput.GetAttributeValue("value", string.Empty);

                if (captchaImg != null)
                {
                    var captchaImgSrc = captchaImg.GetAttributeValue("src", "");
                    // Full url for the captcha image (not sure gonna work or not)
                    if (captchaImgSrc.StartsWith('/'))
                    {
                        captchaImgSrc = "https://portal.aiub.edu" + captchaImgSrc;
                    }

                    return (true, captchaImgSrc, captchaId);
                }
            }

            return (false, string.Empty, string.Empty);
        }

        // For cookie persistence
        public void SaveCookiesToStorage()
        {
            try
            {
                var cookiesJson = SerializeCookies();
                if (!string.IsNullOrEmpty(cookiesJson))
                {
                    SecureStorage.SetAsync("aiub_cookies", cookiesJson);
                }
            } catch (Exception ex)
            {
                Debug.WriteLine($"Error saving cookies: {ex.Message}");
            }
        }

        private string SerializeCookies()
        {
            try
            {
                List<CookieData> cookies = new List<CookieData>();
                var domainCookies = _cookieContainer.GetCookies(new Uri("https://portal.aiub.edu"));

                foreach (Cookie cookie in domainCookies)
                {
                    cookies.Add(new CookieData
                    {
                        Name = cookie.Name,
                        Value = cookie.Value,
                        Domain = cookie.Domain,
                        Path = cookie.Path,
                        Expires = cookie.Expires,
                        HttpOnly = cookie.HttpOnly,
                        Secure = cookie.Secure
                    });
                }

                return JsonSerializer.Serialize(cookies);
            } catch (Exception ex)
            {
                Debug.WriteLine($"Error serializing cookies: {ex.Message}");
                return string.Empty;
            }
        }

        // Load Cookies
        public async Task<bool> LoadCookiesFromStorage()
        {
            try
            {
                var cookiesJson = await SecureStorage.GetAsync("aiub_cookies");
                if (!string.IsNullOrEmpty(cookiesJson))
                {
                    return DeserializeCookies(cookiesJson);
                }
            } catch (Exception ex)
            {
                Debug.WriteLine($"Error loading cookies: {ex.Message}");
            }
            return false;
        }

        private bool DeserializeCookies(string cookiesJson)
        {
            try
            {
                var cookies = JsonSerializer.Deserialize<List<CookieData>>(cookiesJson);
                if (cookies != null || cookies?.Count == 0) return false;

                foreach (var cookiesData in cookies)
                {
                    var cookie = new Cookie(cookiesData.Name, cookiesData.Value, cookiesData.Path, cookiesData.Domain)
                    {
                        Expires = cookiesData.Expires,
                        HttpOnly = cookiesData.HttpOnly,
                        Secure = cookiesData.Secure
                    };

                    _cookieContainer.Add(cookie);
                }
                return true;
            } catch (Exception ex)
            {
                Debug.WriteLine($"Error deserializing cookies: {ex.Message}");
                return false;
            }
        }
    }
}
