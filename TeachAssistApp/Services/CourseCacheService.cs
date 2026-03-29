using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using TeachAssistApp.Models;

namespace TeachAssistApp.Services;

public interface ICourseCacheService
{
    Task SaveCoursesAsync(string username, List<Course> courses);
    Task<List<Course>?> LoadCoursesAsync(string username);
    Task SaveCourseDetailsAsync(string username, string subjectId, Course course);
    Task<Course?> LoadCourseDetailsAsync(string username, string subjectId);
    void ClearAll();
}

public class CourseCacheService : ICourseCacheService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = false,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly string _cacheDir;

    public CourseCacheService()
    {
        _cacheDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "TeachAssistApp", "cache");
        Directory.CreateDirectory(_cacheDir);
    }

    public async Task SaveCoursesAsync(string username, List<Course> courses)
    {
        if (string.IsNullOrEmpty(username)) return;

        var filePath = GetCoursesPath(username);
        var json = JsonSerializer.Serialize(courses, JsonOptions);
        await File.WriteAllTextAsync(filePath, json);
    }

    public async Task<List<Course>?> LoadCoursesAsync(string username)
    {
        if (string.IsNullOrEmpty(username)) return null;

        var filePath = GetCoursesPath(username);
        if (!File.Exists(filePath)) return null;

        try
        {
            var json = await File.ReadAllTextAsync(filePath);
            return JsonSerializer.Deserialize<List<Course>>(json, JsonOptions);
        }
        catch
        {
            return null;
        }
    }

    public async Task SaveCourseDetailsAsync(string username, string subjectId, Course course)
    {
        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(subjectId)) return;

        var filePath = GetDetailsPath(username, subjectId);
        var json = JsonSerializer.Serialize(course, JsonOptions);
        await File.WriteAllTextAsync(filePath, json);
    }

    public async Task<Course?> LoadCourseDetailsAsync(string username, string subjectId)
    {
        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(subjectId)) return null;

        var filePath = GetDetailsPath(username, subjectId);
        if (!File.Exists(filePath)) return null;

        try
        {
            var json = await File.ReadAllTextAsync(filePath);
            return JsonSerializer.Deserialize<Course>(json, JsonOptions);
        }
        catch
        {
            return null;
        }
    }

    public void ClearAll()
    {
        try
        {
            if (Directory.Exists(_cacheDir))
            {
                Directory.Delete(_cacheDir, recursive: true);
                Directory.CreateDirectory(_cacheDir);
            }
        }
        catch { }
    }

    private string GetCoursesPath(string username)
    {
        var safeName = string.Join("_", username.Split(Path.GetInvalidFileNameChars()));
        return Path.Combine(_cacheDir, $"{safeName}_courses.json");
    }

    private string GetDetailsPath(string username, string subjectId)
    {
        var safeName = string.Join("_", username.Split(Path.GetInvalidFileNameChars()));
        return Path.Combine(_cacheDir, $"{safeName}_{subjectId}_detail.json");
    }
}
