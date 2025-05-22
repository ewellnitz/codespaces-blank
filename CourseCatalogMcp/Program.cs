// See https://aka.ms/new-
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;

var builder = Host.CreateApplicationBuilder(args);
builder.Logging.AddConsole(options =>
{
    options.LogToStandardErrorThreshold = LogLevel.Trace;
});

builder.Services
    .AddMcpServer()
    .WithStdioServerTransport()
    .WithToolsFromAssembly()
    .WithResourcesFromAssembly()
    .WithPromptsFromAssembly();

await builder.Build().RunAsync();

#region Data Models

public class Course
{
    public string Id { get; set; }
    public string Title { get; set; }
    public string Description { get; set; }
    public List<string> Prerequisites { get; set; } = new();
}

public class StudentProfile
{
    public string StudentId { get; set; }
    public List<string> CompletedCourses { get; set; } = new();
    public List<string> CurrentEnrollments { get; set; } = new();
    public string AcademicStanding { get; set; } = "Good";
}

#endregion

#region Tools

[McpServerToolType]
public static class CourseTools
{
    private static readonly List<Course> Courses = new()
    {
        new Course { Id = "CS101", Title = "Intro to Computer Science", Description = "Basics of computer science." },
        new Course { Id = "CS201", Title = "Data Structures", Description = "In-depth look at data structures.", Prerequisites = new List<string> { "CS101" } },
        new Course { Id = "CS301", Title = "Algorithms", Description = "Advanced algorithms course.", Prerequisites = new List<string> { "CS201" } }
    };

    private static readonly Dictionary<string, StudentProfile> Students = new()
    {
        ["student123"] = new StudentProfile
        {
            StudentId = "student123",
            CompletedCourses = new List<string> { "CS101" },
            CurrentEnrollments = new List<string> { "CS201" }
        }
    };

    [McpServerTool, Description("Search for courses by keyword.")]
    public static IEnumerable<Course> SearchCourses(string keyword)
    {
        return Courses.Where(c => c.Title.Contains(keyword, StringComparison.OrdinalIgnoreCase));
    }

    [McpServerTool, Description("Get details of a specific course.")]
    public static Course GetCourseDetails(string courseId)
    {
        return Courses.FirstOrDefault(c => c.Id.Equals(courseId, StringComparison.OrdinalIgnoreCase));
    }

    [McpServerTool, Description("Check if a student meets prerequisites for a course.")]
    public static bool CheckPrerequisites(string studentId, string courseId)
    {
        if (!Students.TryGetValue(studentId, out var student))
            return false;

        var course = Courses.FirstOrDefault(c => c.Id.Equals(courseId, StringComparison.OrdinalIgnoreCase));
        if (course == null)
            return false;

        return course.Prerequisites.All(prereq => student.CompletedCourses.Contains(prereq));
    }

    [McpServerTool, Description("Register a student for a course.")]
    public static string RegisterStudent(string studentId, string courseId)
    {
        if (!Students.TryGetValue(studentId, out var student))
        {
            student = new StudentProfile { StudentId = studentId };
            Students[studentId] = student;
        }

        if (student.CurrentEnrollments.Contains(courseId))
            return "Student already enrolled in this course.";

        student.CurrentEnrollments.Add(courseId);
        return "Registration successful.";
    }

    [McpServerTool, Description("Drop a course for a student.")]
    public static string DropCourse(string studentId, string courseId)
    {
        if (Students.TryGetValue(studentId, out var student) && student.CurrentEnrollments.Contains(courseId))
        {
            student.CurrentEnrollments.Remove(courseId);
            return "Course dropped successfully.";
        }

        return "Student not enrolled in this course.";
    }
}

#endregion

#region Resources


[McpServerResource("courseCatalog/{courseId}")]

[McpServerResource("courseCatalog")]
public class CourseCatalogResource : IMcpServerResource
{
    public object Get()
    {
        return new List<Course>
        {
            new Course { Id = "CS101", Title = "Intro to Computer Science", Description = "Basics of computer science." },
            new Course { Id = "CS201", Title = "Data Structures", Description = "In-depth look at data structures.", Prerequisites = new List<string> { "CS101" } },
            new Course { Id = "CS301", Title = "Algorithms", Description = "Advanced algorithms course.", Prerequisites = new List<string> { "CS201" } }
        };
    }
}

[McpServerResource("studentProfile/{studentId}")]
public class StudentProfileResource : IMcpServerResource
{
    public object Get(string studentId)
    {
        // Mock data for demonstration
        return new StudentProfile
        {
            StudentId = studentId,
            CompletedCourses = new List<string> { "CS101" },
            CurrentEnrollments = new List<string> { "CS201" },
            AcademicStanding = "Good"
        };
    }
}

#endregion

#region Prompts

[McpServerPromptType]
public static class CoursePrompts
{
    [McpServerPrompt, Description("Suggest courses based on student's interests and history.")]
    public static string RecommendCourses(string subject, string courseList)
    {
        return $"Based on your interest in {subject} and your completed courses, here are some recommendations: {courseList}";
    }

    [McpServerPrompt, Description("Explain prerequisite eligibility.")]
    public static string ExplainPrerequisites(string eligibility, string course, string reason)
    {
        return $"You are {eligibility} for {course} because you have {reason}.";
    }

    [McpServerPrompt, Description("Confirm course registration.")]
    public static string RegistrationConfirmation(string course, string startDate)
    {
        return $"You have been successfully registered for {course}. Classes begin on {startDate}.";
    }
}

#endregion
