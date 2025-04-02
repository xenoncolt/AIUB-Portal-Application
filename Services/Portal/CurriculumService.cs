using AIUB.Portal.Services.Http;
using HtmlAgilityPack;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AIUB.Portal.Services.Portal
{
    public class CurriculumService
    {
        private readonly HttpClientWrapper _httpClient;

        public CurriculumService(HttpClientWrapper httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<Dictionary<string, object>> GetCurriculumDataAsync(Dictionary<string, string> cookies)
        {
            var curriculumUrl = "https://portal.aiub.edu/Student/Curriculum";
            var response = await _httpClient.GetAsync(curriculumUrl);
            var content = await response.Content.ReadAsStringAsync();

            var doc = new HtmlDocument();
            doc.LoadHtml(content);

            var curriculumNodes = doc.DocumentNode.SelectNodes("//*[@curriculumid]");
            var courseMap = new Dictionary<string, object>();

            if (curriculumNodes != null)
            {
                foreach (var node in curriculumNodes)
                {
                    var curriculumId = node.GetAttributeValue("curriculumid", "");
                    if (!string.IsNullOrEmpty(curriculumId))
                    {
                        var courses = await ProcessCurriculumAsync(curriculumId);
                        foreach (var course in courses)
                        {
                            courseMap[course.Key] = course.Value;
                        }
                    }
                }
            }

            return courseMap;
        }

        private async Task<Dictionary<string, object>> ProcessCurriculumAsync(string curriculumId)
        {
            var url = $"https://portal.aiub.edu/Common/Curriculum?ID={curriculumId}";
            var response = await _httpClient.GetAsync(url);
            var content = await response.Content.ReadAsStringAsync();

            var doc = new HtmlDocument();
            doc.LoadHtml(content);

            var courseRows = doc.DocumentNode.SelectNodes("//table[@class='table-bordered']/tr[position()>1]");
            var courses = new Dictionary<string, object>();

            if (courseRows != null)
            {
                foreach (var row in courseRows)
                {
                    var cells = row.SelectNodes("td");
                    if (cells != null && cells.Count >= 4)
                    {
                        var courseCode = cells[0].InnerText.Trim();
                        var courseName = cells[1].InnerText.Trim();
                        var creditText = cells[2].InnerText.Trim();

                        // Skip courses with * or # in their code or internship
                        if (courseCode.Contains('#') || courseCode.Contains('*') || courseName == "INTERNSHIP")
                            continue;

                        var credit = 0;
                        var creditValues = creditText.Split(' ')
                            .Where(c => int.TryParse(c, out _))
                            .Select(int.Parse)
                            .ToList();

                        if (creditValues.Any())
                        {
                            credit = creditValues.Max();
                        }

                        var prerequisites = new List<string>();
                        var prereqNodes = cells[3].SelectNodes(".//li");
                        if (prereqNodes != null)
                        {
                            foreach (var prereqNode in prereqNodes)
                            {
                                prerequisites.Add(prereqNode.InnerText.Trim());
                            }
                        }

                        courses[courseCode] = new Dictionary<string, object>
                        {
                            ["course_name"] = courseName,
                            ["credit"] = credit,
                            ["prerequisites"] = prerequisites
                        };
                    }
                }
            }

            return courses;
        }
    }
}
