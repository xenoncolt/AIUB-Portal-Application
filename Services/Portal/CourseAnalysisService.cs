using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AIUB.Portal.Services.Portal
{
    public class CourseAnalysisService
    {
        public Dictionary<string, object> ProcessUnlockedCourses(
            Dictionary<string, object> courseMap,
            Dictionary<string, object> completedCourses,
            Dictionary<string, object> currentSemesterCourses,
            Dictionary<string, object> preRegisteredCourses)
        {
            var unlockedCourses = new Dictionary<string, object>();

            // Process courses that can be retaken 
            foreach (var entry in completedCourses)
            {
                var courseCode = entry.Key;
                if (entry.Value is Dictionary<string, string> courseInfo && courseInfo["grade"] == "D" && courseMap.ContainsKey(courseCode))
                {
                    var courseMapInfo = courseMap[courseCode] as Dictionary<string, object>;
                    if (courseMapInfo != null)
                    {
                        unlockedCourses[courseCode] = new Dictionary<string, object>
                        {
                            ["course_name"] = courseInfo["course_name"],
                            ["credit"] = courseMapInfo["credit"],
                            ["prerequisites"] = courseMapInfo["prerequisites"],
                            ["retake"] = true
                        };
                    }
                }
            }

            // Process courses that can be taken based on prerequisites
            foreach (var entry in courseMap)
            {
                var courseCode = entry.Key;
                var courseInfo = entry.Value as Dictionary<string, object>;

                if (courseInfo == null) continue;

                if (ShouldSkipCourse(courseCode, courseInfo, completedCourses, currentSemesterCourses, preRegisteredCourses, unlockedCourses)) continue;

                var prerequisites = courseInfo["prerequisites"] as List<string>;
                if (prerequisites != null) continue;

                if (prerequisites.Count == 0 || ArePrerequisitesMet(prerequisites, completedCourses, currentSemesterCourses))
                {
                    unlockedCourses[courseCode] = new Dictionary<string, object>
                    {
                        ["course_name"] = courseInfo["course_name"],
                        ["credit"] = courseInfo["credit"],
                        ["prerequisites"] = prerequisites,
                        ["retake"] = false
                    };
                }
            }

            return unlockedCourses;
        }

        private bool ShouldSkipCourse(string courseCode, Dictionary<string, object> courseInfo, Dictionary<string, object> completedCourses, Dictionary<string, object> currentSemesterCourses, Dictionary<string, object> preRegisteredCourses, Dictionary<string, object> unlockedCourses)
        {
            if (completedCourses.ContainsKey(courseCode))
            {
                if (completedCourses[courseCode] is Dictionary<string, string> completedCourse)
                {
                    completedCourse["credit"] = courseInfo["credit"].ToString();
                }
                return true;
            }

            if (courseCode.Contains('#') || courseCode.Contains('*')) return true;
            if (courseInfo["course_name"].ToString() == "INTERNSHIP") return true;
            if (unlockedCourses.ContainsKey(courseCode)) return true;

            if (currentSemesterCourses.ContainsKey(courseCode))
            {
                if (currentSemesterCourses[courseCode] is Dictionary<string, string> currentCourse && courseInfo["course_name"].ToString() == currentCourse["course_name"] && !new[] { "W", "I" }.Contains(currentCourse["grade"]))
                    return true;
            }

            if (preRegisteredCourses.ContainsKey(courseCode))
            {
                unlockedCourses[courseCode] = new Dictionary<string, object>
                {
                    ["course_name"] = courseInfo["course_name"],
                    ["credit"] = courseInfo["credit"],
                    ["prerequisites"] = courseInfo["prerequisites"],
                    ["retake"] = false
                };
                return true;
            }
            return false;
        }

        private bool ArePrerequisitesMet(List<string> prerequisites, Dictionary<string, object> completedCourses, Dictionary<string, object> currentSemesterCourses)
        {
            foreach (var prerequisite in prerequisites)
            {
                if (!completedCourses.ContainsKey(prerequisite) && !currentSemesterCourses.ContainsKey(prerequisite))
                {
                    return false;
                }
            }
            return true;
        }
    }
}
