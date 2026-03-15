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

                    // Extract mark from anchor text
                    // Pattern 1: "current mark = 85%"
                    // Pattern 2: "Level 4" / "Level 4+" / "Level 4-"
                    double? mark = null;
                    var markMatch = Regex.Match(anchorText, @"current mark\s*=\s*(\d+\.?\d*)\s*%?");
                    if (markMatch.Success && double.TryParse(markMatch.Groups[1].Value, out var m))
                    {
                        mark = m;
                    }
                    else
                    {
                        // Try level-based marks: "Level 4", "Level 4+", "Level 4-"
                        var levelMatch = Regex.Match(anchorText, @"Level\s*(\d)([+-])?", RegexOptions.IgnoreCase);
                        if (levelMatch.Success && int.TryParse(levelMatch.Groups[1].Value, out var level))
                        {
                            mark = level switch
                            {
                                4 => 83.0,
                                3 => 73.0,
                                2 => 63.0,
                                1 => 53.0,
                                _ => (double?)null
                            };
                            if (mark.HasValue)
                            {
                                if (levelMatch.Groups[2].Value == "+") mark += 7;
                                else if (levelMatch.Groups[2].Value == "-") mark -= 5;
                            }
                        }
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
                        StudentId = studentIdMatch.Success ? studentIdMatch.Groups[1].Value : null,
                        ReportUrl = href // Store full resolved URL for fetching details
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
            // Look up cached course for the stored report URL
            var cachedCourse = _cachedCourses?.FirstOrDefault(c => c.SubjectId == subjectId);

            // Determine primary and fallback URLs
            string primaryUrl;
            string fallbackUrl;

            if (!string.IsNullOrEmpty(cachedCourse?.ReportUrl))
            {
                primaryUrl = cachedCourse.ReportUrl;
                // Construct fallback with the other report type
                fallbackUrl = primaryUrl.Contains("viewReportOE")
                    ? $"https://ta.yrdsb.ca/live/students/viewReport.php?subject_id={subjectId}&student_id={studentId}"
                    : $"https://ta.yrdsb.ca/live/students/viewReportOE.php?subject_id={subjectId}&student_id={studentId}";
            }
            else
            {
                // No stored URL — try OE first, then regular
                primaryUrl = $"https://ta.yrdsb.ca/live/students/viewReportOE.php?subject_id={subjectId}&student_id={studentId}";
                fallbackUrl = $"https://ta.yrdsb.ca/live/students/viewReport.php?subject_id={subjectId}&student_id={studentId}";
            }

            System.Diagnostics.Debug.WriteLine($"  Primary URL: {primaryUrl}");
            var response = await _httpClient.GetAsync(primaryUrl);
            System.Diagnostics.Debug.WriteLine($"  Response status: {response.StatusCode}");

            string html = string.Empty;
            bool needRetry = false;

            if (response.IsSuccessStatusCode)
            {
                html = await response.Content.ReadAsStringAsync();
                System.Diagnostics.Debug.WriteLine($"  HTML length: {html.Length}");

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
                System.Diagnostics.Debug.WriteLine($"  Trying fallback: {fallbackUrl}");
                response = await _httpClient.GetAsync(fallbackUrl);
                System.Diagnostics.Debug.WriteLine($"  Fallback response status: {response.StatusCode}");

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

    /// <summary>
    /// Dispatcher: detects the template type from HTML content and delegates to the appropriate parser.
    /// Template A = Standard category-based (bgcolor colors: KU/T/C/A/F/O)
    /// Template B = Overall Expectation / OE format (expectation codes: A1, B2, etc.)
    /// Template C = Fallback best-effort extraction
    /// </summary>
    private Course? ParseCourseDetail(string html, string subjectId, string studentId)
    {
        try
        {
            System.Diagnostics.Debug.WriteLine("ParseCourseDetail: detecting template type");

            // Template B signatures (check first — more specific)
            bool isOe = html.Contains("By Overall Expectation") ||
                        html.Contains("myChart") ||
                        (html.Contains("Assessment Tasks") && html.Contains("Expectation"));

            // Template A signatures — check bgcolors WITH and WITHOUT # prefix
            bool hasCategoryColors = (html.Contains("#ffffaa") || html.Contains("bgcolor=\"ffffaa\"") || html.Contains("bgcolor='ffffaa'")) ||
                                      (html.Contains("#c0fea4") || html.Contains("bgcolor=\"c0fea4\"") || html.Contains("bgcolor='c0fea4'")) ||
                                      (html.Contains("#afafff") || html.Contains("bgcolor=\"afafff\"") || html.Contains("bgcolor='afafff'")) ||
                                      (html.Contains("#ffd490") || html.Contains("bgcolor=\"ffd490\"") || html.Contains("bgcolor='ffd490'"));
            bool hasStandardTable = html.Contains("cellpadding=\"3\"") && html.Contains("cellspacing=\"0\"");

            if (isOe && !hasCategoryColors)
            {
                System.Diagnostics.Debug.WriteLine("  => Template B (Overall Expectation)");
                return ParseOECourseDetail(html, subjectId, studentId);
            }

            if (hasCategoryColors || hasStandardTable)
            {
                System.Diagnostics.Debug.WriteLine("  => Template A (Standard Category)");
                return ParseStandardCourseDetail(html, subjectId, studentId);
            }

            if (isOe)
            {
                System.Diagnostics.Debug.WriteLine("  => Template B (Overall Expectation, no category colors)");
                return ParseOECourseDetail(html, subjectId, studentId);
            }

            // Template C fallback
            System.Diagnostics.Debug.WriteLine("  => Template C (Fallback)");
            return ParseFallbackCourseDetail(html, subjectId, studentId);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"ParseCourseDetail exception: {ex}");
            return null;
        }
    }

    // =========================================================================
    // TEMPLATE A — Standard category-based (most common)
    // bgcolor colors: #ffffaa=KU, #c0fea4=T, #afafff=C, #ffd490=A,
    //                 #eeeeee=O, #dedede=F
    // Rows in rowspan="2" pairs; second row = feedback or more categories
    // Weight table: <table cellpadding="5"> with same bgcolor mapping
    // =========================================================================
    private Course? ParseStandardCourseDetail(string html, string subjectId, string studentId)
    {
        try
        {
            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            // Extract course code from h2 tag
            string? courseCode = ExtractCourseCode(doc);
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

            // Find the assignment table
            var assignTable = doc.DocumentNode.SelectSingleNode("//table[@border='1'][@cellpadding='3'][@cellspacing='0'][@width='100%']");
            assignTable ??= doc.DocumentNode.SelectSingleNode("//table[@border='1']");

            if (assignTable != null)
            {
                var rows = assignTable.SelectNodes(".//tr");
                if (rows != null)
                {
                    System.Diagnostics.Debug.WriteLine($"  Found {rows.Count} rows in assignment table");

                    for (int i = 0; i < rows.Count; i++)
                    {
                        try
                        {
                            var cells = rows[i].SelectNodes(".//td");
                            if (cells == null || cells.Count < 2) continue;

                            // First cell must have rowspan="2" to be an assignment name
                            if (cells[0].GetAttributeValue("rowspan", "1") != "2") continue;

                            var assignmentName = cells[0].InnerText.Trim();
                            if (string.IsNullOrWhiteSpace(assignmentName) ||
                                assignmentName.Length < 3 ||
                                assignmentName.Contains("Assignment", StringComparison.OrdinalIgnoreCase) ||
                                assignmentName.Contains("Legend", StringComparison.OrdinalIgnoreCase) ||
                                assignmentName.Contains("Category", StringComparison.OrdinalIgnoreCase))
                                continue;

                            System.Diagnostics.Debug.WriteLine($"    Processing: '{assignmentName}'");

                            // --- Extract feedback from second row of pair ---
                            string? feedback = null;
                            if (i + 1 < rows.Count)
                            {
                                var secondCells = rows[i + 1].SelectNodes(".//td");
                                if (secondCells != null && secondCells.Count > 0)
                                {
                                    // Check if second row has category-colored cells
                                    bool secondRowHasCategories = secondCells.Any(c =>
                                    {
                                        var bg = c.GetAttributeValue("bgcolor", "");
                                        return !string.IsNullOrEmpty(bg) && !string.IsNullOrEmpty(CategoryFromBgColor(bg));
                                    });

                                    if (!secondRowHasCategories)
                                    {
                                        // Pure feedback row — collect text from all cells
                                        var fbParts = secondCells.Select(c => c.InnerText.Trim())
                                                                 .Where(t => !string.IsNullOrWhiteSpace(t));
                                        feedback = string.Join(" ", fbParts);
                                        if (string.IsNullOrWhiteSpace(feedback)) feedback = null;
                                    }
                                }
                            }

                            // --- Parse category marks from first row ---
                            for (int ci = 1; ci < cells.Count; ci++)
                            {
                                ParseCategoryCell(cells[ci], assignmentName, feedback, course);
                            }

                            // --- If second row has category cells, parse those too ---
                            if (i + 1 < rows.Count)
                            {
                                var secondCells = rows[i + 1].SelectNodes(".//td");
                                if (secondCells != null)
                                {
                                    bool hasCats = secondCells.Any(c =>
                                    {
                                        var bg = c.GetAttributeValue("bgcolor", "");
                                        return !string.IsNullOrEmpty(bg) && !string.IsNullOrEmpty(CategoryFromBgColor(bg));
                                    });
                                    if (hasCats)
                                    {
                                        foreach (var catCell in secondCells)
                                        {
                                            ParseCategoryCell(catCell, assignmentName, feedback, course);
                                        }
                                    }
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"    Error parsing row: {ex.Message}");
                        }
                    }
                }
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("  No assignment table found");
            }

            // --- Weight table: <table cellpadding="5"> with bgcolor-based rows ---
            ExtractWeightTable(doc, course);

            // --- Overall mark ---
            ExtractOverallMark(html, course);

            System.Diagnostics.Debug.WriteLine($"Parsed Template A: {course.Code} - {course.Assignments.Count} assignments, Overall: {course.OverallMark}");
            return course;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"ParseStandardCourseDetail exception: {ex}");
            return null;
        }
    }

    // =========================================================================
    // TEMPLATE B — Overall Expectation / OE format
    // Organized by curriculum expectations (A1, A2, B1, C1, etc.)
    // Tables: Task Name | Expectation | Mark | Out Of | Weight
    // May have "Final Culminating Task" section
    // Overall mark may appear in Chart.js data or large font
    // =========================================================================
    private Course? ParseOECourseDetail(string html, string subjectId, string studentId)
    {
        try
        {
            // OE pages may not have an h2 with the course code — look up from cached courses
            var cachedCourse = _cachedCourses?.FirstOrDefault(c => c.SubjectId == subjectId);
            var courseCode = cachedCourse?.Code;

            if (string.IsNullOrEmpty(courseCode))
            {
                // Try h2 or regex fallback
                var doc = new HtmlDocument();
                doc.LoadHtml(html);
                courseCode = ExtractCourseCode(doc);
                if (string.IsNullOrEmpty(courseCode))
                {
                    var m = Regex.Match(html, @"([A-Z]{3,5}\d?[A-Z]*\d*-\d+)");
                    courseCode = m.Success ? m.Groups[1].Value : $"COURSE-{subjectId}";
                }
            }

            var course = new Course
            {
                Code = courseCode,
                SubjectId = subjectId,
                StudentId = studentId,
                Name = Helpers.CourseCodeParser.GetDisplayText(courseCode),
                IsCGCFormat = true
            };

            System.Diagnostics.Debug.WriteLine($"  OE Course code: {course.Code}");

            // --- Primary: Parse Chart.js bubble chart data (real OE format) ---
            ParseOEChartJsData(html, course);

            // --- Fallback: If no Chart.js data found, try traditional Assessment Tasks table ---
            if (course.Assignments.Count == 0)
            {
                System.Diagnostics.Debug.WriteLine("  No Chart.js data found, trying traditional OE table format");
                var doc = new HtmlDocument();
                doc.LoadHtml(html);
                ParseOETaskTable(html, doc, course, "Assessment Tasks");
                ParseOETaskTable(html, doc, course, "Final Culminating Task");

                if (course.Assignments.Count == 0)
                {
                    var allTables = doc.DocumentNode.SelectNodes("//table");
                    if (allTables != null)
                    {
                        foreach (var table in allTables)
                        {
                            ParseOETableRows(table, course, "F");
                        }
                    }
                }
            }

            // --- Overall mark ---
            // Try "Term Work {70} = Level 4" format first
            var termMatch = Regex.Match(html, @"Term Work\s*\{(\d+)\}\s*=\s*Level\s*(\d)([+-])?");
            if (termMatch.Success)
            {
                var level = int.TryParse(termMatch.Groups[2].Value, out var lv) ? lv : 0;
                course.OverallMark = LevelToPercentage(level);
                if (termMatch.Groups[3].Value == "+") course.OverallMark = (double)course.OverallMark + 7;
                else if (termMatch.Groups[3].Value == "-") course.OverallMark = (double)course.OverallMark - 5;
            }

            // Fallback: try other mark patterns
            if (course.OverallMark == null)
            {
                ExtractOverallMark(html, course);
            }

            System.Diagnostics.Debug.WriteLine($"Parsed Template B (OE): {course.Code} - {course.Assignments.Count} assignments, {course.AssignmentTrends.Count} trends, Overall: {course.OverallMark}");
            return course;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"ParseOECourseDetail exception: {ex}");
            return null;
        }
    }

    /// <summary>
    /// Parses Chart.js bubble chart data from OE pages.
    /// Each chart represents an expectation (A1, A2, B1, etc.) and contains
    /// datasets for Conversation/Observation/Product with assessment names and marks.
    /// </summary>
    private void ParseOEChartJsData(string html, Course course)
    {
        // Find each Chart.js instance using balanced-brace matching (regex fails due to nested });
        var chartPrefix = "new Chart(ctx, {";
        var charts = new List<(string Expectation, string Body)>();

        int searchPos = 0;
        while (searchPos < html.Length)
        {
            var idx = html.IndexOf(chartPrefix, searchPos);
            if (idx < 0) break;

            // Extract expectation code from the chart title near this block
            var beforeStart = Math.Max(0, idx - 500);
            var beforeChart = html.Substring(beforeStart, idx - beforeStart);
            var titleBefore = Regex.Match(beforeChart, @"text:\s*'[^']*([A-Z]\d)\.?\s*'");
            if (!titleBefore.Success)
                titleBefore = Regex.Match(beforeChart, @"text:\s*'-?\s*([A-Z]\d)\.?\s*'");
            var expectationHint = titleBefore.Success ? titleBefore.Groups[1].Value : "O";

            // Find balanced { } from the opening { of the config object
            var openBrace = html.IndexOf('{', idx);
            if (openBrace < 0) break;

            var depth = 0;
            var closeBrace = openBrace;
            for (int i = openBrace; i < html.Length; i++)
            {
                // Skip content inside strings
                if (html[i] == '\'' || html[i] == '"')
                {
                    var quote = html[i];
                    i++;
                    while (i < html.Length && html[i] != quote)
                    {
                        if (html[i] == '\\') i++;
                        i++;
                    }
                    continue;
                }
                // Skip // comments
                if (html[i] == '/' && i + 1 < html.Length && html[i + 1] == '/')
                {
                    while (i < html.Length && html[i] != '\n') i++;
                    continue;
                }

                if (html[i] == '{') depth++;
                else if (html[i] == '}') depth--;
                if (depth == 0) { closeBrace = i; break; }
            }

            if (depth == 0)
            {
                var chartBody = html.Substring(openBrace + 1, closeBrace - openBrace - 1);
                charts.Add((expectationHint, chartBody));
            }

            searchPos = closeBrace + 1;
        }

        System.Diagnostics.Debug.WriteLine($"  Found {charts.Count} Chart.js instances");

        foreach (var (expectationHint, chartBody) in charts)
        {
            try
            {
                // Extract expectation code from chart title inside body (more reliable)
                var titleMatch = Regex.Match(chartBody, @"text:\s*'[^']*([A-Z]\d)\.?'\s*");
                if (!titleMatch.Success)
                    titleMatch = Regex.Match(chartBody, @"text:\s*'-?\s*([A-Z]\d)\.?\s*'");
                var expectationCode = titleMatch.Success ? titleMatch.Groups[1].Value : expectationHint;

                // Find the datasets section and parse with balanced brackets
                var datasetsIdx = chartBody.IndexOf("datasets:");
                if (datasetsIdx < 0) continue;

                var afterDatasets = chartBody.Substring(datasetsIdx);
                var bracketStart = afterDatasets.IndexOf('[');
                if (bracketStart < 0) continue;

                // Match balanced brackets
                var depth = 0;
                var bracketEnd = bracketStart;
                for (int i = bracketStart; i < afterDatasets.Length; i++)
                {
                    if (afterDatasets[i] == '[') depth++;
                    else if (afterDatasets[i] == ']') depth--;
                    if (depth == 0) { bracketEnd = i; break; }
                }

                var datasetsContent = afterDatasets.Substring(bracketStart + 1, bracketEnd - bracketStart - 1);

                // Parse each dataset object: { labels: [...], label: 'Type', data: [...] }
                var pos = 0;
                while (pos < datasetsContent.Length)
                {
                    var objStart = datasetsContent.IndexOf('{', pos);
                    if (objStart < 0) break;

                    depth = 0;
                    var objEnd = objStart;
                    for (int i = objStart; i < datasetsContent.Length; i++)
                    {
                        if (datasetsContent[i] == '{') depth++;
                        else if (datasetsContent[i] == '}') depth--;
                        if (depth == 0) { objEnd = i; break; }
                    }

                    var datasetObj = datasetsContent.Substring(objStart, objEnd - objStart + 1);

                    // Extract assessment type: label: 'Product'
                    var typeMatch = Regex.Match(datasetObj, @"label:\s*['""]([^'""]+)['""]");
                    var assessmentType = typeMatch.Success ? typeMatch.Groups[1].Value : "Product";

                    // Extract assessment names: labels: ['Digital Logic Quiz', '...']
                    var labelsMatch = Regex.Match(datasetObj, @"labels:\s*\[([^\]]*)\]");
                    if (!labelsMatch.Success || string.IsNullOrWhiteSpace(labelsMatch.Groups[1].Value))
                    {
                        pos = objEnd + 1;
                        continue;
                    }

                    var names = Regex.Matches(labelsMatch.Groups[1].Value, @"['""]([^'""]+)['""]")
                                    .Cast<Match>().Select(m => m.Groups[1].Value).ToList();

                    // Extract data points: data: [{x:0, y:75, r:15}, ...]
                    var dataMatch = Regex.Match(datasetObj, @"data:\s*\[([^\]]*)\]");
                    if (!dataMatch.Success)
                    {
                        pos = objEnd + 1;
                        continue;
                    }

                    var dataPoints = Regex.Matches(dataMatch.Groups[1].Value, @"\{([^}]*)\}")
                                           .Cast<Match>().Select(m => m.Groups[1].Value).ToList();

                    // Match names to data points
                    for (int i = 0; i < names.Count && i < dataPoints.Count; i++)
                    {
                        var yMatch = Regex.Match(dataPoints[i], @"y:\s*([\d.]+)");
                        if (!yMatch.Success) continue;

                        var mark = double.TryParse(yMatch.Groups[1].Value, out var m) ? m : 0;

                        course.Assignments.Add(new Assignment
                        {
                            Name = names[i],
                            MarkAchieved = mark,
                            MarkPossible = 100, // Percentage-based
                            Category = expectationCode,
                            Feedback = assessmentType // Store type (Conversation/Observation/Product)
                        });

                        course.AssignmentTrends.Add(new AssignmentTrend
                        {
                            AssignmentName = names[i],
                            Mark = mark,
                            Expectation = expectationCode,
                            Type = assessmentType
                        });

                        System.Diagnostics.Debug.WriteLine($"    OE Chart: {names[i]} | {expectationCode} | {mark}% | type={assessmentType}");
                    }

                    pos = objEnd + 1;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"    Error parsing Chart.js block: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Converts Ontario level (1-4) to approximate percentage.
    /// </summary>
    private static double LevelToPercentage(int level)
    {
        return level switch
        {
            4 => 83.0,
            3 => 73.0,
            2 => 63.0,
            1 => 53.0,
            _ => 0.0
        };
    }

    // =========================================================================
    // TEMPLATE C — Fallback / best-effort
    // Scans for any mark-like patterns: "X / Y", "X%", "current mark = X%"
    // Flags course as PartiallyParsed
    // =========================================================================
    private Course? ParseFallbackCourseDetail(string html, string subjectId, string studentId)
    {
        try
        {
            System.Diagnostics.Debug.WriteLine("ParseFallbackCourseDetail: best-effort extraction");

            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            string? courseCode = ExtractCourseCode(doc);
            if (string.IsNullOrEmpty(courseCode))
            {
                var m = Regex.Match(html, @"([A-Z]{3,5}\d?[A-Z]*\d*-\d+)");
                courseCode = m.Success ? m.Groups[1].Value : $"COURSE-{subjectId}";
            }

            var course = new Course
            {
                Code = courseCode,
                SubjectId = subjectId,
                StudentId = studentId,
                Name = Helpers.CourseCodeParser.GetDisplayText(courseCode),
                PartiallyParsed = true
            };

            // Try to find any table with mark-like data
            var tables = doc.DocumentNode.SelectNodes("//table");
            if (tables != null)
            {
                foreach (var table in tables)
                {
                    var rows = table.SelectNodes(".//tr");
                    if (rows == null) continue;

                    foreach (var row in rows)
                    {
                        try
                        {
                            var cells = row.SelectNodes(".//td");
                            if (cells == null || cells.Count < 2) continue;

                            // Look for cells containing fraction patterns
                            for (int ci = 0; ci < cells.Count; ci++)
                            {
                                var cellText = cells[ci].InnerText.Trim();
                                var markMatch = Regex.Match(cellText, @"([\d.]+)\s*/\s*([\d.]+)");

                                if (markMatch.Success)
                                {
                                    var achieved = double.TryParse(markMatch.Groups[1].Value, out var a) ? a : 0;
                                    var possible = double.TryParse(markMatch.Groups[2].Value, out var p) ? p : 0;

                                    // Skip if denominator is 0 or the fraction doesn't look like a mark
                                    if (possible == 0) continue;
                                    if (achieved > possible) continue;

                                    // Try to get assignment name from nearby cells
                                    var name = ci > 0 ? cells[ci - 1].InnerText.Trim() : "";
                                    if (string.IsNullOrWhiteSpace(name) && ci + 1 < cells.Count)
                                        name = cells[ci + 1].InnerText.Trim();
                                    if (string.IsNullOrWhiteSpace(name))
                                        name = $"Item {course.Assignments.Count + 1}";

                                    // Try percentage in cell
                                    var pctMatch = Regex.Match(cellText, @"([\d.]+)%");
                                    var weight = 0.0;
                                    if (pctMatch.Success)
                                    {
                                        double.TryParse(pctMatch.Groups[1].Value, out weight);
                                    }

                                    course.Assignments.Add(new Assignment
                                    {
                                        Name = Regex.Replace(name, @"\s+", " "),
                                        MarkAchieved = achieved,
                                        MarkPossible = possible,
                                        Category = "O",
                                        Weight = weight > 0 ? weight : 0
                                    });
                                }
                            }
                        }
                        catch { }
                    }
                }
            }

            // Try to extract overall mark from any common pattern
            ExtractOverallMark(html, course);

            if (course.OverallMark == null)
            {
                var anyMark = Regex.Match(html, @"current mark\s*=\s*(\d+\.?\d*)\s*%");
                if (anyMark.Success && double.TryParse(anyMark.Groups[1].Value, out var m))
                    course.OverallMark = m;
            }

            System.Diagnostics.Debug.WriteLine($"Parsed Template C (Fallback): {course.Code} - {course.Assignments.Count} assignments (partial), Overall: {course.OverallMark}");
            return course;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"ParseFallbackCourseDetail exception: {ex}");
            return null;
        }
    }

    // =========================================================================
    // HELPER METHODS
    // =========================================================================

    /// <summary>
    /// Maps bgcolor attribute to category code.
    /// </summary>
    private static string CategoryFromBgColor(string? bgcolor)
    {
        if (string.IsNullOrEmpty(bgcolor)) return "";
        return bgcolor.TrimStart('#').ToLower() switch
        {
            "ffffaa" => "KU",  // Knowledge (yellow)
            "c0fea4" => "T",    // Thinking (green)
            "afafff" => "C",    // Communication (purple/blue)
            "ffd490" => "A",    // Application (orange)
            "eeeeee" => "O",    // Other (light grey)
            "dedede" => "F",    // Culminating/Final (darker grey)
            "cccccc" => "F",    // Culminating/Final (alternate grey)
            _ => ""
        };
    }

    /// <summary>
    /// Extracts course code from the first h2 tag that matches a course code pattern.
    /// </summary>
    private static string? ExtractCourseCode(HtmlDocument doc)
    {
        var h2Nodes = doc.DocumentNode.SelectNodes("//h2");
        if (h2Nodes == null) return null;

        foreach (var h2 in h2Nodes)
        {
            var m = Regex.Match(h2.InnerText.Trim(), @"([A-Z]{2,5}\d?[A-Z]*\d*-\d+)");
            if (m.Success) return m.Groups[1].Value;
        }
        return null;
    }

    /// <summary>
    /// Parses a single category cell (Template A) and adds an Assignment if valid.
    /// </summary>
    private void ParseCategoryCell(HtmlNode catCell, string assignmentName, string? feedback, Course course)
    {
        var bgcolor = catCell.GetAttributeValue("bgcolor", "");
        var cellText = catCell.InnerText.Trim();

        if (string.IsNullOrWhiteSpace(cellText) || string.IsNullOrEmpty(bgcolor)) return;

        var category = CategoryFromBgColor(bgcolor);
        if (string.IsNullOrEmpty(category)) return;

        if (cellText.Contains("no mark", StringComparison.OrdinalIgnoreCase)) return;

        // Parse "X / Y = Z%" or "X / Y" or "X / Y = Z% weight=W"
        var markMatch = Regex.Match(cellText, @"([\d.]+)\s*/\s*([\d.]+)");
        if (!markMatch.Success) return;

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
            Weight = weight,
            Feedback = feedback
        });

        System.Diagnostics.Debug.WriteLine($"      {category}: {achieved}/{possible} weight={weight}");
    }

    /// <summary>
    /// Extracts the weight table from a Template A page.
    /// Looks for &lt;table cellpadding="5"&gt; with bgcolor-based category rows.
    /// Falls back to text-based category names.
    /// </summary>
    private void ExtractWeightTable(HtmlDocument doc, Course course)
    {
        // Primary: <table cellpadding="5"> (some templates)
        var weightTable = doc.DocumentNode.SelectSingleNode("//table[@cellpadding='5']") ??
                           doc.DocumentNode.SelectSingleNode("//table[@cellpadding=\"5\"]");

        // Fallback: find weight table by content — has "Category" and "Weighting" headers
        weightTable ??= FindWeightTableFallback(doc);

        if (weightTable == null)
        {
            System.Diagnostics.Debug.WriteLine("  No weight table found");
            return;
        }

        var rows = weightTable.SelectNodes(".//tr");
        if (rows == null) return;

        System.Diagnostics.Debug.WriteLine($"  Found {rows.Count} weight table rows");

        foreach (var row in rows)
        {
            try
            {
                var cells = row.SelectNodes(".//td");
                if (cells == null || cells.Count < 2) continue;

                // Try bgcolor-based category detection first
                var bgColor = cells[0].GetAttributeValue("bgcolor", "");
                var category = CategoryFromBgColor(bgColor);

                // Fall back to text-based category names
                if (string.IsNullOrEmpty(category))
                {
                    var catName = cells[0].InnerText.Trim();
                    category = catName switch
                    {
                        "Knowledge/Understanding" => "KU",
                        "Knowledge" => "KU",
                        "Thinking" => "T",
                        "Communication" => "C",
                        "Application" => "A",
                        "Other" => "O",
                        "Final/Culminating" => "F",
                        "Final" => "F",
                        "Culminating" => "F",
                        _ => ""
                    };
                }

                if (string.IsNullOrEmpty(category)) continue;

                // First numeric cell after the category label is the weight
                for (int ci = 1; ci < cells.Count; ci++)
                {
                    var weightStr = cells[ci].InnerText.Trim().TrimEnd('%');
                    if (double.TryParse(weightStr, out var weight) && weight > 0 && weight <= 100)
                    {
                        course.WeightTable.SetWeight(category, weight);
                        System.Diagnostics.Debug.WriteLine($"    Weight: {category} = {weight}%");
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"    Error parsing weight row: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Fallback weight table finder — looks for any table containing percentage values
    /// alongside known category names.
    /// </summary>
    private static HtmlNode? FindWeightTableFallback(HtmlDocument doc)
    {
        var allTables = doc.DocumentNode.SelectNodes("//table");
        if (allTables == null) return null;

        foreach (var table in allTables)
        {
            var text = table.InnerText;
            // Weight tables typically mention at least one category and have percentages
            bool hasCategory = text.Contains("Knowledge") || text.Contains("Thinking") ||
                               text.Contains("Communication") || text.Contains("Application") ||
                               text.Contains("Final") || text.Contains("Culminating") ||
                               text.Contains("Other");
            bool hasPercentage = Regex.IsMatch(text, @"\d+\s*%");

            if (hasCategory && hasPercentage)
                return table;
        }
        return null;
    }

    /// <summary>
    /// Extracts the overall mark from common patterns (64pt font, bold percentage, etc.)
    /// </summary>
    private void ExtractOverallMark(string html, Course course)
    {
        // Pattern 1: font-size:64pt
        var m = Regex.Match(html, @"font-size:64pt[^>]*>\s*([\d.]+)%");
        if (m.Success && double.TryParse(m.Groups[1].Value, out var v))
        {
            course.OverallMark = v;
            return;
        }

        // Pattern 2: large bold percentage near "overall" or "mark"
        m = Regex.Match(html, @"(?:overall|final|course)\s*(?:mark)?\s*:?\s*<b>\s*(\d{1,3}\.?\d*)\s*%</b>", RegexOptions.IgnoreCase);
        if (m.Success && double.TryParse(m.Groups[1].Value, out v))
        {
            course.OverallMark = v;
            return;
        }

        // Pattern 3: any standalone "X%" in a large font or bold context
        m = Regex.Match(html, @"<font\s+size=['""]?[45678]['""]?[^>]*>\s*(\d{1,3}\.?\d*)\s*%", RegexOptions.IgnoreCase);
        if (m.Success && double.TryParse(m.Groups[1].Value, out v))
        {
            course.OverallMark = v;
        }
    }

    /// <summary>
    /// Parses an OE/expectations task table identified by its h2 section heading.
    /// Handles both HtmlAgilityPack parsed tables and raw regex fallback.
    /// </summary>
    private void ParseOETaskTable(string html, HtmlDocument doc, Course course, string sectionHeading)
    {
        // Try to find the section by h2
        var h2Nodes = doc.DocumentNode.SelectNodes("//h2");
        HtmlNode? sectionTable = null;

        if (h2Nodes != null)
        {
            foreach (var h2 in h2Nodes)
            {
                if (h2.InnerText.Trim().Equals(sectionHeading, StringComparison.OrdinalIgnoreCase) ||
                    h2.InnerText.Trim().Contains(sectionHeading, StringComparison.OrdinalIgnoreCase))
                {
                    // Find the next table sibling after this h2
                    sectionTable = h2.SelectSingleNode("following-sibling::table") ??
                                   h2.ParentNode?.SelectNodes(".//table")?.FirstOrDefault();
                    break;
                }
            }
        }

        if (sectionTable != null)
        {
            ParseOETableRows(sectionTable, course,
                sectionHeading.Contains("Final") ? "F" : "");
        }
        else
        {
            // Regex fallback: find the section in raw HTML
            var sectionMatch = Regex.Match(html, $@"<{Regex.Escape(sectionHeading)}>(.*?)(?=<h[23]|$)", RegexOptions.Singleline | RegexOptions.IgnoreCase);
            if (!sectionMatch.Success)
            {
                sectionMatch = Regex.Match(html, $@"{Regex.Escape(sectionHeading)}</h2>(.*?)(?=<h[23]|</body|$)", RegexOptions.Singleline | RegexOptions.IgnoreCase);
            }
            if (sectionMatch.Success)
            {
                var tableMatch = Regex.Match(sectionMatch.Groups[1].Value, @"<table[^>]*>(.*?)</table>", RegexOptions.Singleline);
                if (tableMatch.Success)
                {
                    var tempDoc = new HtmlDocument();
                    tempDoc.LoadHtml($"<table>{tableMatch.Groups[1].Value}</table>");
                    ParseOETableRows(tempDoc.DocumentNode.SelectSingleNode("//table")!, course,
                        sectionHeading.Contains("Final") ? "F" : "");
                }
            }
        }
    }

    /// <summary>
    /// Parses rows from an OE table, handling various column layouts:
    /// Task Name | Expectation | Mark | Out Of | Weight
    /// or: Task Name | Expectation | Mark (e.g. "8/10") | Weight
    /// </summary>
    private void ParseOETableRows(HtmlNode table, Course course, string defaultCategory)
    {
        if (table == null) return;

        var rows = table.SelectNodes(".//tr");
        if (rows == null) return;

        System.Diagnostics.Debug.WriteLine($"  OE table: {rows.Count} rows");

        int dataRowCount = 0;
        foreach (var row in rows)
        {
            try
            {
                var cells = row.SelectNodes(".//td");
                if (cells == null) continue;

                // Skip header rows
                var firstCellText = cells[0].InnerText.Trim().ToLower();
                if (firstCellText == "task" || firstCellText == "task name" ||
                    firstCellText == "assignment" || firstCellText == "name" ||
                    firstCellText.Contains("expectation"))
                    continue;

                // Determine column layout by checking cell count and content
                string taskName = "";
                string expectation = "";
                double achieved = 0, possible = 0, weight = 0;

                if (cells.Count >= 5)
                {
                    // Layout: Task | Expectation | Mark | Out Of | Weight
                    taskName = cells[0].InnerText.Trim();
                    expectation = cells[1].InnerText.Trim();
                    double.TryParse(cells[2].InnerText.Trim(), out achieved);
                    double.TryParse(cells[3].InnerText.Trim(), out possible);
                    double.TryParse(cells[4].InnerText.Trim().TrimEnd('%'), out weight);
                }
                else if (cells.Count >= 4)
                {
                    // Layout: Task | Expectation | Mark (X/Y) | Weight
                    // or:    Task | Mark | Out Of | Weight
                    taskName = cells[0].InnerText.Trim();
                    var secondText = cells[1].InnerText.Trim();
                    var thirdText = cells[2].InnerText.Trim();

                    // Check if second cell is an expectation code (like A1, B2, C3)
                    if (Regex.IsMatch(secondText, @"^[A-Z]\d$"))
                    {
                        expectation = secondText;
                        var markParts = thirdText.Split('/');
                        double.TryParse(markParts[0].Trim(), out achieved);
                        possible = markParts.Length > 1 && double.TryParse(markParts[1].Trim(), out var p) ? p : achieved;
                        double.TryParse(cells[3].InnerText.Trim().TrimEnd('%'), out weight);
                    }
                    else
                    {
                        // Second cell is mark, third is out-of
                        double.TryParse(secondText, out achieved);
                        double.TryParse(thirdText, out possible);
                        double.TryParse(cells[3].InnerText.Trim().TrimEnd('%'), out weight);
                    }
                }
                else if (cells.Count >= 3)
                {
                    // Layout: Task | Mark (X/Y or just number) | Weight
                    taskName = cells[0].InnerText.Trim();
                    var markStr = cells[1].InnerText.Trim();
                    var markParts = markStr.Split('/');
                    double.TryParse(markParts[0].Trim(), out achieved);
                    possible = markParts.Length > 1 && double.TryParse(markParts[1].Trim(), out var p) ? p : achieved;
                    double.TryParse(cells[2].InnerText.Trim().TrimEnd('%'), out weight);
                }
                else if (cells.Count >= 2)
                {
                    // Minimal: Task | Mark (X/Y)
                    taskName = cells[0].InnerText.Trim();
                    var markStr = cells[1].InnerText.Trim();
                    var markMatch = Regex.Match(markStr, @"([\d.]+)\s*/\s*([\d.]+)");
                    if (markMatch.Success)
                    {
                        double.TryParse(markMatch.Groups[1].Value, out achieved);
                        double.TryParse(markMatch.Groups[2].Value, out possible);
                    }
                }

                // Clean up task name
                taskName = Regex.Replace(taskName, @"\s+", " ").Trim();
                if (string.IsNullOrWhiteSpace(taskName) || achieved == 0 && possible == 0)
                    continue;

                // If no expectation detected but default category provided, use it
                if (string.IsNullOrEmpty(expectation) && !string.IsNullOrEmpty(defaultCategory))
                    expectation = defaultCategory;

                // If still no category, use the expectation code or default
                var category = !string.IsNullOrEmpty(expectation) ? expectation : "O";

                var assignment = new Assignment
                {
                    Name = taskName,
                    MarkAchieved = achieved,
                    MarkPossible = possible > 0 ? possible : achieved,
                    Weight = weight,
                    Category = category
                };

                course.Assignments.Add(assignment);

                // Add trend data for expectation-based assignments
                if (Regex.IsMatch(category, @"^[A-Z]\d$"))
                {
                    var pct = possible > 0 ? (achieved / possible) * 100 : 0;
                    course.AssignmentTrends.Add(new AssignmentTrend
                    {
                        AssignmentName = taskName,
                        Mark = pct,
                        Weight = weight,
                        Expectation = category,
                        Type = "Product"
                    });
                }

                dataRowCount++;
                System.Diagnostics.Debug.WriteLine($"    OE: {taskName} | {category} | {achieved}/{possible} | w={weight}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"    OE row error: {ex.Message}");
            }
        }

        System.Diagnostics.Debug.WriteLine($"  OE table: extracted {dataRowCount} assignments");
    }
}
