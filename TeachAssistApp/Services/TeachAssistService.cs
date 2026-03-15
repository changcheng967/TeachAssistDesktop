using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using HtmlAgilityPack;
using TeachAssistApp.Models;

namespace TeachAssistApp.Services;

public class TeachAssistService : ITeachAssistService
{
    private readonly HttpClient _httpClient;
    private readonly HttpClientHandler _httpClientHandler;
    private string? _username;
    private string? _password;
    private List<Course>? _cachedCourses;
    private string? _lastError;

    public bool IsLoggedIn { get; private set; }
    public string? LastError => _lastError;

    // Enable demo mode for testing without real credentials
    public static bool UseDemoMode { get; set; } = false;

    public TeachAssistService()
    {
        _httpClientHandler = new HttpClientHandler
        {
            CookieContainer = new CookieContainer(),
            UseCookies = true,
            AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate,
            AllowAutoRedirect = true,
            MaxAutomaticRedirections = 10
        };
        _httpClient = new HttpClient(_httpClientHandler);
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
        _httpClient.DefaultRequestHeaders.Add("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,*/*;q=0.8");
        _httpClient.DefaultRequestHeaders.Add("Accept-Language", "en-US,en;q=0.9");
        _httpClient.Timeout = TimeSpan.FromSeconds(30);
    }

    public async Task<bool> LoginAsync(string username, string password)
    {
        _username = username;
        _password = password;
        _lastError = null;

        // Demo mode for testing
        if (UseDemoMode || username.Equals("demo", StringComparison.OrdinalIgnoreCase))
        {
            System.Diagnostics.Debug.WriteLine("Using demo mode with mock data");
            _cachedCourses = GetMockCourses();
            IsLoggedIn = true;
            return true;
        }

        // Direct login to TeachAssist root API (FAST - no 3rd party timeout)
        try
        {
            System.Diagnostics.Debug.WriteLine("Logging in to TeachAssist...");
            return await TryDirectLoginAsync(username, password);
        }
        catch (Exception ex)
        {
            _lastError = $"Direct login failed: {ex.Message}";
            System.Diagnostics.Debug.WriteLine($"Direct login exception: {ex}");
            return false;
        }
    }

    private async Task<bool> TryDirectLoginAsync(string username, string password)
    {
        try
        {
            // POST directly to the TeachAssist login URL
            // The response HTML contains the course list
            var formData = new List<KeyValuePair<string, string>>
            {
                new("username", username),
                new("password", password)
            };

            var content = new FormUrlEncodedContent(formData);
            var postResponse = await _httpClient.PostAsync("https://ta.yrdsb.ca/yrdsb/", content);

            var responseContent = await postResponse.Content.ReadAsStringAsync();
            System.Diagnostics.Debug.WriteLine($"Login POST response, status: {postResponse.StatusCode}, length: {responseContent.Length}");

            // Save full HTML to debug file
            try
            {
                var debugPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "login_response.html");
                await File.WriteAllTextAsync(debugPath, responseContent);
                System.Diagnostics.Debug.WriteLine($"Saved login HTML to: {debugPath}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to save debug HTML: {ex.Message}");
            }

            // Check for login failure indicators
            if (responseContent.Contains("Invalid") || responseContent.Contains("failed") ||
                responseContent.Contains("incorrect") || responseContent.Contains("not found"))
            {
                _lastError = "Invalid username or password";
                IsLoggedIn = false;
                return false;
            }

            // Parse courses from the response HTML (subject_id comes from hrefs in the course list)
            var courses = ParseStudentPage(responseContent);

            if (courses.Count > 0)
            {
                IsLoggedIn = true;
                _cachedCourses = courses;
                System.Diagnostics.Debug.WriteLine($"Successfully parsed {_cachedCourses.Count} courses from login response");
                return true;
            }

            _lastError = "Login succeeded but no courses found in response";
            IsLoggedIn = false;
            return false;
        }
        catch (HttpRequestException ex)
        {
            _lastError = $"Network error: {ex.Message}";
            System.Diagnostics.Debug.WriteLine($"HTTP request exception: {ex}");
            return false;
        }
        catch (TaskCanceledException)
        {
            _lastError = "Request timed out. Check your internet connection.";
            return false;
        }
    }

    private List<Course> ParseStudentPage(string html)
    {
        var courses = new List<Course>();

        try
        {
            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            // Find the course table with width="85%"
            var courseTable = doc.DocumentNode.SelectSingleNode("//table[@width='85%']");
            if (courseTable == null)
            {
                System.Diagnostics.Debug.WriteLine("ParseStudentPage: Could not find course table with width='85%'");
                return courses;
            }

            var rows = courseTable.SelectNodes(".//tr");
            if (rows == null || rows.Count < 2)
            {
                System.Diagnostics.Debug.WriteLine("ParseStudentPage: Course table has no data rows");
                return courses;
            }

            System.Diagnostics.Debug.WriteLine($"ParseStudentPage: Found {rows.Count - 1} data rows in course table");

            // Skip first row (header)
            for (int i = 1; i < rows.Count; i++)
            {
                try
                {
                    var row = rows[i];
                    var cells = row.SelectNodes(".//td");
                    if (cells == null || cells.Count < 3) continue;

                    // Cell 0: Course name/code (e.g., "ICS4U1-03 : Computer Science")
                    var cell0Text = cells[0].InnerText.Trim();

                    // Cell 2: Contains <a> tag with "current mark = XX%" and href to detailed report
                    var anchor = cells[2].SelectSingleNode(".//a");
                    if (anchor == null)
                    {
                        // Try cell 1 as fallback
                        if (cells.Count > 1)
                            anchor = cells[1].SelectSingleNode(".//a");
                    }
                    if (anchor == null) continue;

                    var anchorText = anchor.InnerText.Trim();
                    var href = anchor.GetAttributeValue("href", "");

                    // Extract mark from anchor text (e.g., "current mark = 85%")
                    var markMatch = Regex.Match(anchorText, @"current mark\s*=\s*(\d+\.?\d*)\s*%?");
                    double? mark = null;
                    if (markMatch.Success && double.TryParse(markMatch.Groups[1].Value, out var m))
                    {
                        mark = m;
                    }

                    // Fix href - prepend "live/students/" if not absolute and doesn't start with /
                    if (!href.StartsWith("http", StringComparison.OrdinalIgnoreCase))
                    {
                        if (!href.StartsWith("/"))
                        {
                            href = "live/students/" + href;
                        }
                        var baseUri = new Uri("https://ta.yrdsb.ca/");
                        href = new Uri(baseUri, href).ToString();
                    }

                    // Extract subject_id and student_id from href query parameters
                    var subjectIdMatch = Regex.Match(href, @"subject_id=(\d+)");
                    var studentIdMatch = Regex.Match(href, @"student_id=(\d+)");

                    // Extract course code from cell 0
                    var courseCodeMatch = Regex.Match(cell0Text, @"([A-Z]{3}\d[A-Z]\d-\d{1,2})"); // Standard: XXX#X#-##
                    if (!courseCodeMatch.Success)
                    {
                        courseCodeMatch = Regex.Match(cell0Text, @"([A-Z]{4,5}\d-\d{1,2})"); // ESL: XXXXX#-##
                    }
                    if (!courseCodeMatch.Success) continue;

                    var courseCode = courseCodeMatch.Groups[1].Value;

                    // Extract descriptive name (text after " : ")
                    var colonIdx = cell0Text.IndexOf(':');
                    var courseName = Helpers.CourseCodeParser.GetDisplayText(courseCode);
                    if (colonIdx >= 0)
                    {
                        var rawName = cell0Text.Substring(colonIdx + 1).Trim();
                        if (!string.IsNullOrWhiteSpace(rawName))
                        {
                            courseName = Regex.Replace(rawName, @"\s+", " ");
                        }
                    }

                    var course = new Course
                    {
                        Code = courseCode,
                        Name = courseName,
                        SubjectId = subjectIdMatch.Success ? subjectIdMatch.Groups[1].Value : null,
                        StudentId = studentIdMatch.Success ? studentIdMatch.Groups[1].Value : null
                    };
                    if (mark.HasValue)
                    {
                        course.OverallMark = mark.Value;
                    }

                    courses.Add(course);
                    System.Diagnostics.Debug.WriteLine($"Parsed course: {course.Code} - {course.Name} - Mark: {course.OverallMark} - SubjectId: {course.SubjectId}");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error parsing course row: {ex.Message}");
                }
            }

            System.Diagnostics.Debug.WriteLine($"Total courses parsed: {courses.Count}");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"ParseStudentPage exception: {ex}");
        }

        return courses;
    }

    private Course? ParseHtmlCourse(HtmlNode node)
    {
        try
        {
            var text = node.InnerText;
            var course = new Course();

            // Extract course code
            var codeMatch = Regex.Match(text, @"[A-Z]{3}\d[A-Z]\d");
            if (codeMatch.Success)
            {
                course.Code = codeMatch.Value;
            }

            // Extract percentage
            var percentMatch = Regex.Match(text, @"(\d+\.?\d*)\s*%");
            if (percentMatch.Success && double.TryParse(percentMatch.Groups[1].Value, out var mark))
            {
                course.OverallMark = mark;
            }

            // Extract room number
            var roomMatch = Regex.Match(text, @"Room\s*:?\s*(\d+)", RegexOptions.IgnoreCase);
            if (roomMatch.Success)
            {
                course.Room = roomMatch.Groups[1].Value;
            }

            if (!string.IsNullOrEmpty(course.Code))
            {
                return course;
            }
        }
        catch { }

        return null;
    }

    public async Task<List<Course>> GetCoursesAsync()
    {
        if (!IsLoggedIn)
        {
            _lastError = "Not logged in";
            throw new InvalidOperationException("Not logged in");
        }

        // Return cached courses from login
        if (_cachedCourses != null && _cachedCourses.Count > 0)
        {
            System.Diagnostics.Debug.WriteLine($"Returning {_cachedCourses.Count} cached courses");
            return await Task.FromResult(_cachedCourses);
        }

        _lastError = "No courses found. Try demo mode (username: demo) or login again.";
        return new List<Course>();
    }

    private List<Course> ParseApiCourses(string jsonContent)
    {
        try
        {
            using var jsonDoc = JsonDocument.Parse(jsonContent);
            var root = jsonDoc.RootElement;

            if (root.ValueKind == JsonValueKind.Array)
            {
                var courses = new List<Course>();
                foreach (var courseElement in root.EnumerateArray())
                {
                    courses.Add(ParseApiCourse(courseElement));
                }
                return courses;
            }
            else if (root.ValueKind == JsonValueKind.Object)
            {
                var coursesList = new List<Course>();
                JsonElement coursesToParse = default;

                if (root.TryGetProperty("courses", out var coursesProperty) && coursesProperty.ValueKind == JsonValueKind.Array)
                {
                    coursesToParse = coursesProperty;
                }
                else if (root.TryGetProperty("data", out var dataProperty) && dataProperty.ValueKind == JsonValueKind.Array)
                {
                    coursesToParse = dataProperty;
                }

                if (coursesToParse.ValueKind == JsonValueKind.Array)
                {
                    foreach (var courseElement in coursesToParse.EnumerateArray())
                    {
                        coursesList.Add(ParseApiCourse(courseElement));
                    }
                    return coursesList;
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Parse API courses exception: {ex}");
        }

        return new List<Course>();
    }

    private Course ParseApiCourse(JsonElement courseElement)
    {
        var course = new Course
        {
            Code = courseElement.GetProperty("code").GetString() ?? "",
            Name = courseElement.GetProperty("name").GetString() ?? "",
            Block = courseElement.TryGetProperty("block", out var b) ? b.GetInt32() : 1,
            Room = courseElement.TryGetProperty("room", out var r) ? r.GetString() ?? "" : "",
            StartTime = courseElement.TryGetProperty("start_time", out var st) ? st.GetString() : null,
            EndTime = courseElement.TryGetProperty("end_time", out var et) ? et.GetString() : null
        };

        if (courseElement.TryGetProperty("overall_mark", out var markElement))
        {
            if (markElement.ValueKind == JsonValueKind.Number)
            {
                course.OverallMark = markElement.GetDouble();
            }
            else if (markElement.ValueKind == JsonValueKind.String)
            {
                var markStr = markElement.GetString();
                if (double.TryParse(markStr, out var mark))
                {
                    course.OverallMark = mark;
                }
                else
                {
                    course.OverallMark = "N/A";
                }
            }
        }

        if (courseElement.TryGetProperty("assignments", out var assignmentsElement))
        {
            foreach (var assignmentElement in assignmentsElement.EnumerateArray())
            {
                course.Assignments.Add(ParseApiAssignment(assignmentElement));
            }
        }

        if (courseElement.TryGetProperty("weight_table", out var weightElement))
        {
            course.WeightTable = ParseApiWeightTable(weightElement);
        }

        System.Diagnostics.Debug.WriteLine($"Parsed course: {course.Code} - {course.Name} - Mark: {course.OverallMark}");
        return course;
    }

    private Assignment ParseApiAssignment(JsonElement assignmentElement)
    {
        var assignment = new Assignment
        {
            Name = assignmentElement.GetProperty("name").GetString() ?? "",
            Date = assignmentElement.TryGetProperty("date", out var d) ? d.GetString() : null,
            Category = assignmentElement.TryGetProperty("category", out var c) ? c.GetString() ?? "" : ""
        };

        if (assignmentElement.TryGetProperty("mark_achieved", out var achievedElement))
        {
            if (achievedElement.ValueKind == JsonValueKind.Number)
            {
                assignment.MarkAchieved = achievedElement.GetDouble();
            }
        }

        if (assignmentElement.TryGetProperty("mark_possible", out var possibleElement))
        {
            if (possibleElement.ValueKind == JsonValueKind.Number)
            {
                assignment.MarkPossible = possibleElement.GetDouble();
            }
        }

        if (assignmentElement.TryGetProperty("weight", out var weightElement))
        {
            if (weightElement.ValueKind == JsonValueKind.Number)
            {
                assignment.Weight = weightElement.GetDouble();
            }
        }

        if (assignmentElement.TryGetProperty("feedback", out var feedbackElement))
        {
            assignment.Feedback = feedbackElement.GetString();
        }

        return assignment;
    }

    private WeightTable ParseApiWeightTable(JsonElement weightElement)
    {
        var weightTable = new WeightTable();

        foreach (var property in weightElement.EnumerateObject())
        {
            if (property.Value.ValueKind == JsonValueKind.Number)
            {
                weightTable.SetWeight(property.Name, property.Value.GetDouble());
            }
        }

        return weightTable;
    }

    private List<Course> GetMockCourses()
    {
        return new List<Course>
        {
            new Course
            {
                Code = "ICS4U1-03",
                Name = Helpers.CourseCodeParser.GetDisplayText("ICS4U1-03"),
                Block = 1,
                Room = "234",
                StartTime = "2024-09-03",
                EndTime = "2025-01-27",
                OverallMark = 92,
                Assignments = new List<Assignment>
                {
                    new Assignment { Name = "Unit 1 Test", Date = "2024-09-20", MarkAchieved = 48, MarkPossible = 50, Category = "KU", Weight = 10 },
                    new Assignment { Name = "Programming Assignment 1", Date = "2024-10-15", MarkAchieved = 95, MarkPossible = 100, Category = "A", Weight = 15 },
                    new Assignment { Name = "Final Project", Date = "2025-01-15", MarkAchieved = 88, MarkPossible = 100, Category = "C", Weight = 20 },
                },
                WeightTable = new WeightTable
                {
                    Weights = new Dictionary<string, double>
                    {
                        { "KU", 20 },
                        { "T", 15 },
                        { "C", 25 },
                        { "A", 30 },
                        { "F", 10 }
                    }
                }
            },
            new Course
            {
                Code = "ENG4U1-01",
                Name = Helpers.CourseCodeParser.GetDisplayText("ENG4U1-01"),
                Block = 2,
                Room = "312",
                StartTime = "2024-09-03",
                EndTime = "2025-01-27",
                OverallMark = 87,
                Assignments = new List<Assignment>
                {
                    new Assignment { Name = "Essay 1", Date = "2024-10-01", MarkAchieved = 82, MarkPossible = 100, Category = "C", Weight = 10 },
                    new Assignment { Name = "Novel Study Test", Date = "2024-11-15", MarkAchieved = 90, MarkPossible = 100, Category = "KU", Weight = 15 },
                },
                WeightTable = new WeightTable
                {
                    Weights = new Dictionary<string, double>
                    {
                        { "KU", 25 },
                        { "T", 15 },
                        { "C", 30 },
                        { "A", 20 },
                        { "F", 10 }
                    }
                }
            },
            new Course
            {
                Code = "MHF4U1-02",
                Name = Helpers.CourseCodeParser.GetDisplayText("MHF4U1-02"),
                Block = 3,
                Room = "401",
                StartTime = "2024-09-03",
                EndTime = "2025-01-27",
                OverallMark = 78,
                Assignments = new List<Assignment>
                {
                    new Assignment { Name = "Chapter 1 Quiz", Date = "2024-09-25", MarkAchieved = 72, MarkPossible = 100, Category = "KU", Weight = 5 },
                    new Assignment { Name = "Midterm", Date = "2024-11-20", MarkAchieved = 80, MarkPossible = 100, Category = "T", Weight = 20 },
                },
                WeightTable = new WeightTable
                {
                    Weights = new Dictionary<string, double>
                    {
                        { "KU", 30 },
                        { "T", 20 },
                        { "C", 20 },
                        { "A", 20 },
                        { "F", 10 }
                    }
                }
            }
        };
    }

    public async Task LogoutAsync()
    {
        _cachedCourses = null;
        _username = null;
        _password = null;
        IsLoggedIn = false;
        _lastError = null;

        // Clear cookies and reset HTTP client
        try
        {
            _httpClientHandler.CookieContainer = new CookieContainer();

            // Cancel any pending requests
            if (_httpClient != null)
            {
                // Try to abort any pending requests
                try
                {
                    _httpClient.CancelPendingRequests();
                }
                catch { }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error during logout: {ex.Message}");
        }
    }

    public async Task<Course?> GetCourseDetailsAsync(string subjectId, string studentId)
    {
        System.Diagnostics.Debug.WriteLine($"GetCourseDetailsAsync called: subject_id={subjectId}, student_id={studentId}");

        // Debug: Show cookies
        var cookies = _httpClientHandler.CookieContainer.GetCookies(new Uri("https://ta.yrdsb.ca"));
        System.Diagnostics.Debug.WriteLine($"  Cookies being sent: {cookies.Count}");
        foreach (System.Net.Cookie cookie in cookies)
        {
            System.Diagnostics.Debug.WriteLine($"    {cookie.Name} = {cookie.Value}");
        }

        if (!IsLoggedIn)
        {
            _lastError = "Not logged in";
            System.Diagnostics.Debug.WriteLine($"  Not logged in");
            return null;
        }

        try
        {
            // Try viewReportOE.php first (most common)
            var url = $"https://ta.yrdsb.ca/live/students/viewReportOE.php?subject_id={subjectId}&student_id={studentId}";
            System.Diagnostics.Debug.WriteLine($"  Fetching: {url}");
            var response = await _httpClient.GetAsync(url);
            System.Diagnostics.Debug.WriteLine($"  Response status: {response.StatusCode}");

            string html = string.Empty;
            bool needRetry = false;

            if (response.IsSuccessStatusCode)
            {
                html = await response.Content.ReadAsStringAsync();
                System.Diagnostics.Debug.WriteLine($"  HTML length: {html.Length}");

                // If HTML is too small, it might be an error page - try the other URL
                if (html.Length < 5000)
                {
                    System.Diagnostics.Debug.WriteLine($"  HTML too small ({html.Length} bytes), likely an error page");
                    needRetry = true;
                }
            }
            else
            {
                needRetry = true;
            }

            if (needRetry)
            {
                // Try regular viewReport.php
                url = $"https://ta.yrdsb.ca/live/students/viewReport.php?subject_id={subjectId}&student_id={studentId}";
                System.Diagnostics.Debug.WriteLine($"  Trying regular version: {url}");
                response = await _httpClient.GetAsync(url);
                System.Diagnostics.Debug.WriteLine($"  Regular response status: {response.StatusCode}");

                if (response.IsSuccessStatusCode)
                {
                    html = await response.Content.ReadAsStringAsync();
                    System.Diagnostics.Debug.WriteLine($"  HTML length: {html.Length}");
                }
            }

            if (response.IsSuccessStatusCode && !string.IsNullOrEmpty(html))
            {
                var parsedCourse = ParseCourseDetail(html, subjectId, studentId);
                System.Diagnostics.Debug.WriteLine($"  Parsed course has {parsedCourse?.Assignments.Count ?? 0} assignments");
                return parsedCourse;
            }
            else
            {
                _lastError = $"Failed to fetch course details: {response.StatusCode}";
                return null;
            }
        }
        catch (Exception ex)
        {
            _lastError = $"Error fetching course details: {ex.Message}";
            System.Diagnostics.Debug.WriteLine($"GetCourseDetailsAsync exception: {ex}");
            return null;
        }
    }

    private Course? ParseCourseDetail(string html, string subjectId, string studentId)
    {
        try
        {
            System.Diagnostics.Debug.WriteLine("ParseCourseDetail: parsing HTML");
            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            // Detect CGC/Geography format first
            var isCGCFormat = html.Contains("By Overall Expectation") ||
                             html.Contains("myChart") ||
                             (html.Contains("Assessment Tasks") && html.Contains("Expectation"));

            if (isCGCFormat)
            {
                System.Diagnostics.Debug.WriteLine("  Detected CGC/Geography format");
                return ParseCGCCourseDetail(html, subjectId, studentId, doc);
            }

            // Extract course code from h2 tag
            var h2Nodes = doc.DocumentNode.SelectNodes("//h2");
            string? courseCode = null;

            if (h2Nodes != null)
            {
                foreach (var h2 in h2Nodes)
                {
                    var codeMatch = Regex.Match(h2.InnerText.Trim(), @"([A-Z]{2,5}\d?[A-Z]*\d*-\d+)");
                    if (codeMatch.Success)
                    {
                        courseCode = codeMatch.Groups[1].Value;
                        break;
                    }
                }
            }

            if (string.IsNullOrEmpty(courseCode))
            {
                System.Diagnostics.Debug.WriteLine("  No course code found in h2 tag");
                return null;
            }

            var course = new Course
            {
                Code = courseCode,
                SubjectId = subjectId,
                StudentId = studentId,
                Name = "Course" // Will be filled from cached course list
            };

            System.Diagnostics.Debug.WriteLine($"  Course code: {course.Code}");

            // Find the assignment table: <table border="1" cellpadding="3" cellspacing="0" width="100%">
            var assignTable = doc.DocumentNode.SelectSingleNode("//table[@border='1'][@cellpadding='3'][@cellspacing='0'][@width='100%']");
            if (assignTable == null)
            {
                // Fallback: try any table with border="1"
                assignTable = doc.DocumentNode.SelectSingleNode("//table[@border='1']");
            }

            if (assignTable == null)
            {
                System.Diagnostics.Debug.WriteLine("  No assignment table found");
                return course;
            }

            var rows = assignTable.SelectNodes(".//tr");
            if (rows == null)
            {
                System.Diagnostics.Debug.WriteLine("  No rows in assignment table");
                return course;
            }

            System.Diagnostics.Debug.WriteLine($"  Found {rows.Count} rows in assignment table");

            // Map bgcolor to category code
            static string CategoryFromBgColor(string? bgcolor)
            {
                if (string.IsNullOrEmpty(bgcolor)) return "";
                return bgcolor.TrimStart('#').ToLower() switch
                {
                    "ffffaa" => "KU",  // Knowledge (yellow)
                    "c0fea4" => "T",    // Thinking (green)
                    "afafff" => "C",    // Communication (purple/blue)
                    "ffd490" => "A",    // Application (orange)
                    "dedede" => "F",    // Culminating/Final (grey)
                    _ => ""
                };
            }

            // Process rows in pairs (rowspan="2")
            for (int i = 0; i < rows.Count; i++)
            {
                try
                {
                    var row = rows[i];
                    var cells = row.SelectNodes(".//td");
                    if (cells == null || cells.Count < 2) continue;

                    // Check if first cell has rowspan="2" (assignment name cell)
                    var firstCell = cells[0];
                    var rowspan = firstCell.GetAttributeValue("rowspan", "1");

                    if (rowspan != "2") continue;

                    var assignmentName = firstCell.InnerText.Trim();

                    // Skip header/legend rows
                    if (string.IsNullOrWhiteSpace(assignmentName) ||
                        assignmentName.Contains("Assignment", StringComparison.OrdinalIgnoreCase) ||
                        assignmentName.Contains("Legend", StringComparison.OrdinalIgnoreCase) ||
                        assignmentName.Contains("Category", StringComparison.OrdinalIgnoreCase) ||
                        assignmentName.Length < 3) continue;

                    System.Diagnostics.Debug.WriteLine($"    Processing: '{assignmentName}'");

                    // Process category cells from both rows of the pair
                    for (int ri = 0; ri <= 1 && (i + ri) < rows.Count; ri++)
                    {
                        var pairRow = rows[i + ri];
                        var pairCells = pairRow.SelectNodes(".//td");
                        if (pairCells == null) continue;

                        // First row: skip cell 0 (assignment name with rowspan)
                        // Second row: start from cell 0
                        int startCol = (ri == 0) ? 1 : 0;

                        for (int ci = startCol; ci < pairCells.Count; ci++)
                        {
                            var catCell = pairCells[ci];
                            var bgcolor = catCell.GetAttributeValue("bgcolor", "");
                            var cellText = catCell.InnerText.Trim();

                            if (string.IsNullOrWhiteSpace(cellText) || string.IsNullOrEmpty(bgcolor)) continue;

                            var category = CategoryFromBgColor(bgcolor);
                            if (string.IsNullOrEmpty(category)) continue;

                            // Skip "no mark" cells
                            if (cellText.Contains("no mark", StringComparison.OrdinalIgnoreCase)) continue;

                            // Parse "X / Y = Z%" or "X / Y"
                            var markMatch = Regex.Match(cellText, @"([\d.]+)\s*/\s*([\d.]+)");
                            if (markMatch.Success)
                            {
                                var achieved = double.TryParse(markMatch.Groups[1].Value, out var a) ? a : 0;
                                var possible = double.TryParse(markMatch.Groups[2].Value, out var p) ? p : 0;
                                var weightMatch = Regex.Match(cellText, @"weight=(\d+)");
                                var weight = weightMatch.Success && double.TryParse(weightMatch.Groups[1].Value, out var w) ? w : 0;

                                course.Assignments.Add(new Assignment
                                {
                                    Name = assignmentName,
                                    MarkAchieved = achieved,
                                    MarkPossible = possible,
                                    Category = category,
                                    Weight = weight
                                });

                                System.Diagnostics.Debug.WriteLine($"      {category}: {achieved}/{possible} weight={weight}");
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"    Error parsing assignment row: {ex.Message}");
                }
            }

            // Extract weight table from other tables
            var weightTableRows = doc.DocumentNode.SelectNodes("//table[@border='1']//tr");
            if (weightTableRows != null)
            {
                foreach (var row in weightTableRows)
                {
                    try
                    {
                        var cells = row.SelectNodes(".//td");
                        if (cells == null || cells.Count < 4) continue;

                        var categoryName = cells[0].InnerText.Trim();
                        var weightStr = cells[1].InnerText.Trim().TrimEnd('%');

                        if (double.TryParse(weightStr, out var weight) && weight > 0)
                        {
                            var categoryCode = categoryName switch
                            {
                                "Knowledge/Understanding" => "KU",
                                "Thinking" => "T",
                                "Communication" => "C",
                                "Application" => "A",
                                "Other" => "O",
                                "Final/Culminating" => "F",
                                _ => ""
                            };

                            if (!string.IsNullOrEmpty(categoryCode))
                            {
                                course.WeightTable.SetWeight(categoryCode, weight);
                                System.Diagnostics.Debug.WriteLine($"    Weight: {categoryCode} = {weight}%");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"    Error parsing weight row: {ex.Message}");
                    }
                }
            }

            // Extract overall mark
            var overallMarkMatch = Regex.Match(html, @"font-size:64pt[^>]*>\s*([\d.]+)%");
            if (overallMarkMatch.Success && double.TryParse(overallMarkMatch.Groups[1].Value, out var overallMark))
            {
                course.OverallMark = overallMark;
            }

            System.Diagnostics.Debug.WriteLine($"Parsed course detail: {course.Code} - {course.Assignments.Count} assignments, Overall: {course.OverallMark}");
            return course;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"ParseCourseDetail exception: {ex}");
            return null;
        }
    }

    private Course? ParseCGCCourseDetail(string html, string subjectId, string studentId, HtmlDocument doc)
    {
        try
        {
            System.Diagnostics.Debug.WriteLine($"ParseCGCCourseDetail: parsing CGC/Geography format");

            // Extract course code from HTML or use subjectId
            var courseCodeMatch = Regex.Match(html, @"CGC1W-?\d*");
            if (!courseCodeMatch.Success)
            {
                courseCodeMatch = Regex.Match(html, @"([A-Z]{3,5}\d?[A-Z]*\d*-\d+)");
            }

            var course = new Course
            {
                Code = courseCodeMatch.Success ? courseCodeMatch.Groups[1].Value : $"CGC1W-{studentId}",
                SubjectId = subjectId,
                StudentId = studentId,
                Name = Helpers.CourseCodeParser.GetDisplayText(courseCodeMatch.Success ? courseCodeMatch.Groups[1].Value : "CGC1W"),
                IsCGCFormat = true
            };

            // Parse Assessment Tasks table
            var assessmentTasksHeader = Regex.Match(html, @"<h2>Assessment Tasks</h2>");
            if (assessmentTasksHeader.Success)
            {
                var afterTasks = html.Substring(assessmentTasksHeader.Index);
                var tableMatch = Regex.Match(afterTasks, @"<table[^>]*>(.*?)</table>", RegexOptions.Singleline);
                if (tableMatch.Success)
                {
                    var taskRows = Regex.Matches(tableMatch.Groups[1].Value, @"<tr>(.*?)</tr>", RegexOptions.Singleline);
                    System.Diagnostics.Debug.WriteLine($"  Found {taskRows.Count} assessment task rows");

                    foreach (Match rowMatch in taskRows)
                    {
                        var rowHtml = rowMatch.Groups[1].Value;
                        var cells = Regex.Matches(rowHtml, @"<td[^>]*>(.*?)</td>", RegexOptions.Singleline);

                        if (cells.Count >= 5)
                        {
                            var taskName = Regex.Replace(cells[0].Groups[1].Value.Trim(), @"<[^>]+>", "").Trim();
                            var expectation = Regex.Replace(cells[1].Groups[1].Value.Trim(), @"<[^>]+>", "").Trim();
                            var markStr = Regex.Replace(cells[2].Groups[1].Value.Trim(), @"<[^>]+>", "").Trim();
                            var weightStr = Regex.Replace(cells[4].Groups[1].Value.Trim(), @"<[^>]+>", "").Trim();

                            // Parse mark (e.g., "10" or "10/10")
                            var markParts = markStr.Split('/');
                            if (markParts.Length >= 1 && double.TryParse(markParts[0].Trim(), out var mark))
                            {
                                var markPossible = markParts.Length >= 2 && double.TryParse(markParts[1].Trim(), out var possible) ? possible : mark;
                                var weight = double.TryParse(weightStr, out var w) ? w : 1;

                                var assignment = new Assignment
                                {
                                    Name = taskName,
                                    MarkAchieved = mark,
                                    MarkPossible = markPossible,
                                    Weight = weight,
                                    Category = expectation // Use expectation as category (A1, A2, B1, etc.)
                                };

                                course.Assignments.Add(assignment);

                                // Also add to trends
                                course.AssignmentTrends.Add(new AssignmentTrend
                                {
                                    AssignmentName = taskName,
                                    Mark = (mark / markPossible) * 100,
                                    Weight = weight,
                                    Expectation = expectation,
                                    Type = "Product"
                                });

                                System.Diagnostics.Debug.WriteLine($"    CGC Task: {taskName} | {expectation} | {mark}/{markPossible} | weight={weight}");
                            }
                        }
                    }
                }
            }

            // Parse Final Culminating Task
            var finalTaskHeader = Regex.Match(html, @"<h2>Final Culminating Task</h2>");
            if (finalTaskHeader.Success)
            {
                var afterFinal = html.Substring(finalTaskHeader.Index);
                var tableMatch = Regex.Match(afterFinal, @"<table[^>]*>(.*?)</table>", RegexOptions.Singleline);
                if (tableMatch.Success)
                {
                    var finalRows = Regex.Matches(tableMatch.Groups[1].Value, @"<tr>(.*?)</tr>", RegexOptions.Singleline);
                    foreach (Match rowMatch in finalRows)
                    {
                        var rowHtml = rowMatch.Groups[1].Value;
                        var cells = Regex.Matches(rowHtml, @"<td[^>]*>(.*?)</td>", RegexOptions.Singleline);

                        if (cells.Count >= 3)
                        {
                            var taskName = Regex.Replace(cells[0].Groups[1].Value.Trim(), @"<[^>]+>", "").Trim();
                            var markStr = Regex.Replace(cells[1].Groups[1].Value.Trim(), @"<[^>]+>", "").Trim();
                            var weightStr = Regex.Replace(cells[2].Groups[1].Value.Trim(), @"<[^>]+>", "").Trim();

                            // Parse mark (e.g., "100/100" or "49/50")
                            var markMatch = Regex.Match(markStr, @"([\d.]+)\s*/\s*([\d.]+)");
                            if (markMatch.Success)
                            {
                                var markAchieved = double.TryParse(markMatch.Groups[1].Value, out var ma) ? ma : 0;
                                var markPossible = double.TryParse(markMatch.Groups[2].Value, out var mp) ? mp : 0;
                                var weight = double.TryParse(weightStr, out var w) ? w : 1;

                                course.Assignments.Add(new Assignment
                                {
                                    Name = taskName,
                                    MarkAchieved = markAchieved,
                                    MarkPossible = markPossible,
                                    Weight = weight,
                                    Category = "F" // Final
                                });

                                System.Diagnostics.Debug.WriteLine($"    Final Task: {taskName} | {markAchieved}/{markPossible} | weight={weight}");
                            }
                        }
                    }
                }
            }

            System.Diagnostics.Debug.WriteLine($"Parsed CGC course: {course.Code} - {course.Assignments.Count} assignments, {course.AssignmentTrends.Count} trends");
            return course;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"ParseCGCCourseDetail exception: {ex}");
            return null;
        }
    }
}
