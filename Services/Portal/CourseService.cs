using AIUB.Portal.Services.Http;
using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace AIUB.Portal.Services.Portal
{
    public class CourseService
    {
        private readonly HttpClientWrapper _httpClient;

        public CourseService(HttpClientWrapper httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<object[]> GetCompletedCoursesAsync(string currentSemester, Dictionary<string, string> cookies)
        {
            var url = "https://portal.aiub.edu/Student/GradeReport/ByCurriculum";
            var response = await _httpClient.GetAsync(url);
            var content = await response.Content.ReadAsStringAsync();

            var doc = new HtmlDocument();
            doc.LoadHtml(content);

            var rows = doc.DocumentNode.SelectNodes("//table[position()>1]/tr[position()>1][.//td[3][text()!='']]");

            var completedCourses = new Dictionary<string, object>();
            var currentSemesterCourses = new Dictionary<string, object>();
            var preRegisteredCourses = new Dictionary<string, object>();

            var validGrades = new[] { "A+", "A", "B+", "B", "C+", "C", "D+", "D", "F" };

            if (rows != null)
            {
                foreach (var row in rows)
                {
                    ProcessCourseRow(
                        row,
                        completedCourses,
                        currentSemesterCourses,
                        preRegisteredCourses,
                        validGrades,
                        currentSemester);
                }
            }

            return new object[] { completedCourses, currentSemesterCourses, preRegisteredCourses };
        }

        private void ProcessCourseRow(
            HtmlNode row,
            Dictionary<string, object> completedCourses,
            Dictionary<string, object> currentSemesterCourses,
            Dictionary<string, object> preRegisteredCourses,
            string[] validGrades,
            string currentSemester)
        {
            var cells = row.SelectNodes("td");
            if (cells == null || cells.Count < 3) return;

            var courseCode = cells[0].InnerText.Trim();
            var courseName = cells[1].InnerText.Trim();
            var resultsText = cells[2].InnerText.Trim();

            var matches = Regex.Matches(resultsText, @"\(([^)]+)\)\s*\[([^\]]+)\]");

            if (matches.Count > 0)
            {
                var lastMatch = matches[matches.Count - 1];
                var semester = lastMatch.Groups[1].Value.Trim();
                var grade = lastMatch.Groups[2].Value.Trim();

                if (grade == "-")
                {
                    HandleIncompleteGrade(
                        matches,
                        courseCode,
                        courseName,
                        semester,
                        currentSemester,
                        completedCourses,
                        currentSemesterCourses,
                        preRegisteredCourses,
                        validGrades);
                }
                else if (validGrades.Contains(grade))
                {
                    completedCourses[courseCode] = new Dictionary<string, object>
                    {
                        ["course_name"] = courseName,
                        ["grade"] = grade,
                        ["semester"] = semester
                    };
                }
            }
        }

        private void HandleIncompleteGrade(
            MatchCollection matches,
            string courseCode,
            string courseName,
            string semester,
            string currentSemester,
            Dictionary<string, object> completedCourses,
            Dictionary<string, object> currentSemesterCourses,
            Dictionary<string, object> preRegisteredCourses,
            string[] validGrades)
        {
            if (semester == currentSemester)
            {
                // Check for previous attempts with a valid grade
                for (int i = matches.Count - 2; i >= 0; i--)
                {
                    var prevMatch = matches[i];
                    var prevSemester = prevMatch.Groups[1].Value.Trim();
                    var prevGrade = prevMatch.Groups[2].Value.Trim();

                    if (validGrades.Contains(prevGrade))
                    {
                        completedCourses[courseCode] = new Dictionary<string, object>
                        {
                            ["course_name"] = courseName,
                            ["grade"] = prevGrade,
                            ["semester"] = prevSemester
                        };
                        break;
                    }
                }

                currentSemesterCourses[courseCode] = new Dictionary<string, object>
                {
                    ["course_name"] = courseName,
                    ["grade"] = "-"
                };
            }
            else
            {
                preRegisteredCourses[courseCode] = new Dictionary<string, object>
                {
                    ["course_name"] = courseName,
                    ["grade"] = "-"
                };
            }
        }

        public async Task<Dictionary<string, object>> ProcessSemestersAsync(
            List<KeyValuePair<string, string>> semesterOptions,
            Dictionary<string, string> cookies)
        {
            var result = new Dictionary<string, object>();

            foreach (var option in semesterOptions)
            {
                var semesterData = await GetSemesterDataAsync(option.Key, option.Value);
                result[option.Key] = semesterData;
            }

            return result;
        }

        public async Task<Dictionary<string, object>> GetSemesterDataAsync(
            string semesterName,
            string queryParam)
        {
            var coursesObj = new Dictionary<string, object>();

            try
            {
                var url = $"https://portal.aiub.edu/Student/Registration?q={queryParam}";
                var response = await _httpClient.GetAsync(url);
                var content = await response.Content.ReadAsStringAsync();

                var doc = new HtmlDocument();
                doc.LoadHtml(content);

                var tables = doc.DocumentNode.SelectNodes("//table");
                if (tables != null && tables.Count > 1)
                {
                    var courseElements = tables[1].SelectNodes(".//td[1]");
                    if (courseElements != null)
                    {
                        foreach (var course in courseElements)
                        {
                            if (string.IsNullOrWhiteSpace(course.InnerText))
                                continue;

                            var courseLink = course.SelectSingleNode(".//a");
                            if (courseLink == null)
                                continue;

                            var courseName = courseLink.InnerText;
                            var parsedCourse = ParseCourseDetails(courseName);

                            var creditCell = course.SelectSingleNode("following-sibling::td[1]");
                            int credit = 0;
                            if (creditCell != null)
                            {
                                var creditText = creditCell.InnerText.Trim();
                                var creditValues = creditText.Split('-')
                                    .Select(c => int.TryParse(c.Trim(), out var val) ? val : 0)
                                    .ToList();

                                credit = creditValues.Count > 0 ? creditValues.Max() : 0;
                            }

                            var timeSpans = course.SelectNodes(".//div/span");
                            if (timeSpans != null)
                            {
                                foreach (var span in timeSpans)
                                {
                                    if (!span.InnerText.Contains("Time"))
                                        continue;

                                    var parsedTime = ParseTime(span.InnerText);
                                    if (parsedTime != null)
                                    {
                                        var day = parsedTime["day"].ToString();
                                        var time = parsedTime["time"].ToString();

                                        if (!coursesObj.ContainsKey(day))
                                        {
                                            coursesObj[day] = new Dictionary<string, object>();
                                        }

                                        ((Dictionary<string, object>)coursesObj[day])[time] = new Dictionary<string, object>
                                        {
                                            ["course_name"] = parsedCourse["course_name"],
                                            ["class_id"] = parsedCourse["class_id"],
                                            ["credit"] = credit,
                                            ["section"] = parsedCourse["section"],
                                            ["type"] = parsedTime["type"],
                                            ["room"] = parsedTime["room"]
                                        };
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error processing semester {semesterName}: {ex.Message}");
            }

            return coursesObj;
        }

        private Dictionary<string, string> ParseCourseDetails(string course)
        {
            var match = Regex.Match(course, @"^(\d+)-(.+?)\s+\[([A-Z0-9]+)\](?:\s+\[([A-Z0-9]+)\])?$");

            if (match.Success)
            {
                var classId = match.Groups[1].Value;
                var courseName = System.Globalization.CultureInfo.CurrentCulture.TextInfo
                    .ToTitleCase(match.Groups[2].Value.ToLower());
                var section = match.Groups[4].Success ? match.Groups[4].Value : match.Groups[3].Value;

                return new Dictionary<string, string>
                {
                    ["class_id"] = classId,
                    ["course_name"] = courseName,
                    ["section"] = section
                };
            }

            return new Dictionary<string, string>
            {
                ["class_id"] = "",
                ["course_name"] = "",
                ["section"] = ""
            };
        }

        private Dictionary<string, object> ParseTime(string timeString)
        {
            try
            {
                var dayMap = new Dictionary<string, string>
                {
                    ["Sun"] = "Sunday",
                    ["Mon"] = "Monday",
                    ["Tue"] = "Tuesday",
                    ["Wed"] = "Wednesday",
                    ["Thu"] = "Thursday",
                    ["Fri"] = "Friday",
                    ["Sat"] = "Saturday"
                };

                var matches = Regex.Matches(timeString, @"\d{1,2}:\d{1,2}(?:\s?[ap]m|\s?[AP]M)?");
                if (matches.Count < 2) return null;

                var classTypeMatch = Regex.Match(timeString, @"\((.*?)\)");
                var dayMatch = Regex.Match(timeString, @"(Sun|Mon|Tue|Wed|Thu|Fri|Sat)");
                var roomMatch = Regex.Match(timeString, @"Room: (.*)");

                if (!classTypeMatch.Success || !dayMatch.Success || !roomMatch.Success)
                    return null;

                var classType = classTypeMatch.Groups[1].Value;
                var day = dayMap[dayMatch.Groups[1].Value];
                var room = roomMatch.Groups[1].Value;

                var startTime = matches[0].Value;
                var endTime = matches[1].Value;

                // Format times to be consistent
                var startTimeFormatted = FormatTime(startTime);
                var endTimeFormatted = FormatTime(endTime);

                var finalTime = $"{startTimeFormatted} - {endTimeFormatted}";

                return new Dictionary<string, object>
                {
                    ["type"] = classType,
                    ["time"] = finalTime,
                    ["day"] = day,
                    ["room"] = room
                };
            }
            catch
            {
                return null;
            }
        }

        private string FormatTime(string timeStr)
        {
            // If time already has am/pm, standardize the format
            var hasAmPm = timeStr.ToLower().Contains("am") || timeStr.ToLower().Contains("pm");

            if (hasAmPm)
            {
                var isPm = timeStr.ToLower().Contains("pm");
                var timePart = Regex.Match(timeStr, @"\d{1,2}:\d{1,2}").Value;
                var parts = timePart.Split(':');

                if (parts.Length == 2 && int.TryParse(parts[0], out var hour) && int.TryParse(parts[1], out var minute))
                {
                    return $"{hour}:{minute:D2} {(isPm ? "PM" : "AM")}";
                }
            }
            else
            {
                // Add AM/PM based on hour
                var parts = timeStr.Split(':');
                if (parts.Length == 2 && int.TryParse(parts[0], out var hour) && int.TryParse(parts[1], out var minute))
                {
                    var amPm = hour >= 12 ? "PM" : "AM";
                    hour = hour > 12 ? hour - 12 : (hour == 0 ? 12 : hour);
                    return $"{hour}:{minute:D2} {amPm}";
                }
            }

            return timeStr; // Return original if parsing fails
        }
    }
}
