using AIUB.Portal.Services.Http;
using HtmlAgilityPack;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace AIUB.Portal.Services.Portal
{
    public class StudentService
    {
        private readonly HttpClientWrapper _httpClient;
        private readonly CourseService _courseService;
        private readonly CurriculumService _curriculumService;
        private readonly CourseAnalysisService _courseAnalysisService;

        public StudentService(
            HttpClientWrapper httpClient,
            CourseService courseService,
            CurriculumService curriculumService,
            CourseAnalysisService courseAnalysisService)
        {
            _httpClient = httpClient;
            _courseService = courseService;
            _curriculumService = curriculumService;
            _courseAnalysisService = courseAnalysisService;
        }

        public async Task<Dictionary<string, object>> GetStudentDataAsync(
            string studentPageContent,
            Dictionary<string, string> cookies)
        {
            var doc = new HtmlDocument();
            doc.LoadHtml(studentPageContent);

            // Extract user name
            var userNode = doc.DocumentNode.SelectSingleNode("//a[@class='navbar-link']");
            var user = userNode?.InnerText.Trim() ?? "Unknown";

            // Process user name format
            if (user.Contains(','))
            {
                var parts = user.Split(',');
                user = $"{parts[1].Trim()} {parts[0].Trim()}";
            }
            user = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(user.ToLower());

            // Get Current Semester
            var semesterNode = doc.DocumentNode.SelectSingleNode("//select[@id='SemesterDropDown']/option[@selected='selected']");
            var currentSemester = semesterNode?.InnerText.Trim() ?? "Unknown";

            // Get semester options
            var semesterOptions = doc.DocumentNode.SelectNodes("//select[@id='SemesterDropDown']/option");
            var semesters = new List<KeyValuePair<string, string>>();

            if (semesterOptions != null)
            {
                foreach (var option in semesterOptions)
                {
                    var semesterName = option.InnerText.Trim();
                    var valueAttr = option.GetAttributeValue("value", "");
                    var queryParam = "";

                    var match = Regex.Match(valueAttr, @"q=(.+)");
                    if (match.Success)
                    {
                        queryParam = match.Groups[1].Value;
                        semesters.Add(new KeyValuePair<string, string>(semesterName, queryParam));
                    }
                }
            }

            // Start processing the student data
            var result = new Dictionary<string, object>
            {
                ["user"] = user,
                ["currentSemester"] = currentSemester
            };

            // Pass cookies to the services for their requests
            // Initiate tasks in parallel
            var curriculumTask = _curriculumService.GetCurriculumDataAsync(cookies);
            var coursesTask = _courseService.GetCompletedCoursesAsync(currentSemester, cookies);
            var semestersTask = _courseService.ProcessSemestersAsync(semesters, cookies);

            // Wait for tasks to complete
            await Task.WhenAll(curriculumTask, coursesTask, semestersTask);

            // Get results from tasks
            var curriculumData = curriculumTask.Result;
            var completedCoursesData = coursesTask.Result;
            var semesterClassRoutine = semestersTask.Result;

            // Add data to result
            result["semesterClassRoutine"] = semesterClassRoutine;
            result["curriculumncourses"] = curriculumData;
            result["completedCourses"] = completedCoursesData[0];
            result["currentSemesterCourses"] = completedCoursesData[1];
            result["preregisteredCourses"] = completedCoursesData[2];

            // Process unlocked courses
            var unlockedCourses = _courseAnalysisService.ProcessUnlockedCourses(
                curriculumData,
                (Dictionary<string, object>)completedCoursesData[0],
                (Dictionary<string, object>)completedCoursesData[1],
                (Dictionary<string, object>)completedCoursesData[2]
            );
            result["unlockedCourses"] = unlockedCourses;

            return result;
        }
    }
}
