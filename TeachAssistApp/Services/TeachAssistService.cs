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
            // First, get the login page to establish session and get any CSRF tokens
            var getPageResponse = await _httpClient.GetAsync("https://ta.yrdsb.ca/yrdsb/");
            var getPageContent = await getPageResponse.Content.ReadAsStringAsync();
            System.Diagnostics.Debug.WriteLine($"Got login page, status: {getPageResponse.StatusCode}, length: {getPageContent.Length}");

            // Extract any hidden fields or tokens if needed
            var csrfToken = ExtractCsrfToken(getPageContent);
            System.Diagnostics.Debug.WriteLine($"CSRF Token found: {!string.IsNullOrEmpty(csrfToken)}");

            // Now post the login form
            var formData = new List<KeyValuePair<string, string>>
            {
                new("username", username),
                new("password", password)
            };

            if (!string.IsNullOrEmpty(csrfToken))
            {
                formData.Add(new("csrf_token", csrfToken));
            }

            var content = new FormUrlEncodedContent(formData);
            var postResponse = await _httpClient.PostAsync("https://ta.yrdsb.ca/yrdsb/index.php", content);

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

            // Check if login was successful
            // Check for error messages
            if (responseContent.Contains("Invalid") || responseContent.Contains("failed") ||
                responseContent.Contains("incorrect") || responseContent.Contains("not found"))
            {
                _lastError = "Invalid username or password";
                IsLoggedIn = false;
                return false;
            }

            // SUCCESS - The course data is in the login response itself!
            // Parse the login response to get courses
            var courses = ParseStudentPage(responseContent);

            if (courses.Count > 0)
            {
                IsLoggedIn = true;
                _cachedCourses = courses;
                System.Diagnostics.Debug.WriteLine($"Successfully parsed {_cachedCourses.Count} courses from login response");

                // Extract student_id for fetching all reports
                var studentIdMatch = Regex.Match(responseContent, @"student_id=(\d+)");
                var studentId = studentIdMatch.Success ? studentIdMatch.Groups[1].Value : null;

                // Try to fetch ALL available reports (including hidden ones!)
                await FetchAllReportsAsync(studentId);

                return true;
            }
            else
            {
                _lastError = "Login succeeded but no courses found in response";
                IsLoggedIn = false;
                return false;
            }
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

    private string? ExtractCsrfToken(string html)
    {
        try
        {
            var tokenMatch = Regex.Match(html, @"<input[^>]*name=""csrf_token""[^>]*value=""([^""]+)""", RegexOptions.IgnoreCase);
            if (tokenMatch.Success)
            {
                return tokenMatch.Groups[1].Value;
            }
        }
        catch { }
        return null;
    }

    private async Task FetchAllReportsAsync(string? studentId)
    {
        if (string.IsNullOrEmpty(studentId))
        {
            System.Diagnostics.Debug.WriteLine("Cannot fetch all reports - no student_id");
            return;
        }

        try
        {
            // Try to fetch the listReports.php page which might show ALL available reports
            var url = $"https://ta.yrdsb.ca/live/students/listReports.php?student_id={studentId}";
            System.Diagnostics.Debug.WriteLine($"Fetching all reports from: {url}");

            var response = await _httpClient.GetAsync(url);
            if (response.IsSuccessStatusCode)
            {
                var html = await response.Content.ReadAsStringAsync();
                System.Diagnostics.Debug.WriteLine($"listReports response length: {html.Length}");

                // Save to debug file
                try
                {
                    var debugPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "list_reports.html");
                    await File.WriteAllTextAsync(debugPath, html);
                    System.Diagnostics.Debug.WriteLine($"Saved list reports HTML to: {debugPath}");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Failed to save list reports HTML: {ex.Message}");
                }

                // Parse ALL links to find subject_ids for courses that don't have them
                var doc = new HtmlDocument();
                doc.LoadHtml(html);

                // Find all links that might contain subject_id
                var allLinks = doc.DocumentNode.SelectNodes("//a[@href]");
                if (allLinks != null)
                {
                    System.Diagnostics.Debug.WriteLine($"Found {allLinks.Count} links in listReports");

                    foreach (var link in allLinks)
                    {
                        var href = link.GetAttributeValue("href", "");
                        if (href.Contains("viewReport"))
                        {
                            // Extract subject_id and try to match to course codes
                            var subjectIdMatch = Regex.Match(href, @"subject_id=(\d+)");
                            if (subjectIdMatch.Success)
                            {
                                var subjectId = subjectIdMatch.Groups[1].Value;

                                // Try to find which course this belongs to by checking the link text
                                var linkText = link.InnerText.Trim();
                                System.Diagnostics.Debug.WriteLine($"Found report link: {linkText} -> subject_id={subjectId}");

                                // Try to match to existing courses by looking for course code in link text or nearby HTML
                                UpdateCourseWithSubjectId(link, subjectId, studentId);
                            }
                        }
                    }
                }
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"Failed to fetch listReports: {response.StatusCode}");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error fetching all reports: {ex.Message}");
        }
    }

    private void UpdateCourseWithSubjectId(HtmlNode linkNode, string subjectId, string studentId)
    {
        try
        {
            // Look at the parent tr/td to find the course code
            var parentRow = linkNode.Ancestors("tr").FirstOrDefault();
            if (parentRow != null)
            {
                var rowText = parentRow.InnerText;
                var rowHtml = parentRow.OuterHtml;

                // Try to extract course code from this row
                var codeMatch = Regex.Match(rowText, @"([A-Z]{3,5}\d[A-Z]?\d*-\d+)");
                if (codeMatch.Success)
                {
                    var courseCode = codeMatch.Groups[1].Value;
                    System.Diagnostics.Debug.WriteLine($"  Matched subject_id {subjectId} to course {courseCode}");

                    // Update the cached course with this subject_id
                    var course = _cachedCourses?.FirstOrDefault(c => c.Code == courseCode);
                    if (course != null && string.IsNullOrEmpty(course.SubjectId))
                    {
                        course.SubjectId = subjectId;
                        course.StudentId = studentId;
                        System.Diagnostics.Debug.WriteLine($"  ✅ Updated {courseCode} with subject_id={subjectId}");
                    }
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error updating course with subject_id: {ex.Message}");
        }
    }

    private List<Course> ParseStudentPage(string html)
    {
        var courses = new List<Course>();

        try
        {
            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            // Extract student_id from the page (appears in header and links)
            var studentIdMatch = Regex.Match(html, @"student_id=(\d+)");
            var studentId = studentIdMatch.Success ? studentIdMatch.Groups[1].Value : null;
            System.Diagnostics.Debug.WriteLine($"Found student_id: {studentId}");

            // Find all table rows that contain course data
            // TeachAssist has a specific structure with <tr> elements containing course info
            var allRows = doc.DocumentNode.SelectNodes("//tr");
            if (allRows == null) return courses;

            System.Diagnostics.Debug.WriteLine($"Found {allRows.Count} total table rows");

            foreach (var row in allRows)
            {
                try
                {
                    var cells = row.SelectNodes(".//td");
                    if (cells == null || cells.Count < 3) continue;

                    var rowText = row.InnerText;
                    var rowHtml = row.InnerHtml;

                    // Look for course code pattern - handle various formats:
                    // CGC1W1-1 (standard: XXX#X#-##)
                    // ESLEO1-2 (ESL format: XXXXX#-##)
                    // ENL1W1-16 (two digits after hyphen)
                    var codeMatch = Regex.Match(rowText, @"([A-Z]{3}\d[A-Z]\d-\d{1,2})");  // Standard format
                    if (!codeMatch.Success)
                    {
                        codeMatch = Regex.Match(rowText, @"([A-Z]{4,5}\d-\d{1,2})");  // ESL format (ESLEO, ESLCO, etc.)
                    }
                    if (!codeMatch.Success)
                    {
                        // Try without hyphen number
                        codeMatch = Regex.Match(rowText, @"([A-Z]{3}\d[A-Z]\d)");  // Standard without hyphen
                    }
                    if (!codeMatch.Success)
                    {
                        codeMatch = Regex.Match(rowText, @"([A-Z]{4,5}\d)");  // ESL without hyphen
                    }

                    if (!codeMatch.Success) continue;

                    var course = new Course
                    {
                        Code = codeMatch.Groups[1].Value
                    };

                    // Extract course name (everything after the code and colon)
                    // Handle cases where name might be missing (like MTH1W1-8 :)
                    var nameMatch = Regex.Match(rowText, @"[A-Z]{2,4}\d[A-Z][\d-]*\s*:\s*([^\n<]+?)\s*(?:Block:|<br>|$)");
                    if (nameMatch.Success)
                    {
                        var name = nameMatch.Groups[1].Value.Trim();
                        // Clean up extra whitespace
                        name = Regex.Replace(name, @"\s+", " ");
                        // Use CourseCodeParser to get decoded name, but keep original as fallback
                        course.Name = Helpers.CourseCodeParser.GetDisplayText(course.Code);
                    }
                    else
                    {
                        // Use CourseCodeParser to decode course code
                        course.Name = Helpers.CourseCodeParser.GetDisplayText(course.Code);
                    }

                    // Extract block and room
                    var blockMatch = Regex.Match(rowText, @"Block:\s*([P\d]+)");
                    if (blockMatch.Success)
                    {
                        var blockStr = blockMatch.Groups[1].Value;
                        // Parse P1, P2, etc. to numbers
                        var blockNumMatch = Regex.Match(blockStr, @"P(\d+)");
                        if (blockNumMatch.Success && int.TryParse(blockNumMatch.Groups[1].Value, out var blockNum))
                        {
                            course.Block = blockNum;
                        }
                    }

                    var roomMatch = Regex.Match(rowText, @"rm\.\s*([A-Z0-9]+)");
                    if (roomMatch.Success)
                    {
                        course.Room = roomMatch.Groups[1].Value;
                    }

                    // Extract mark - prioritize "current mark" over "MIDTERM MARK"
                    // Look for "current mark = XX.X%" first
                    var currentMarkMatch = Regex.Match(rowText, @"current mark\s*=\s*(\d+\.?\d*)\s*%");
                    if (!currentMarkMatch.Success)
                    {
                        // Try to find the current mark link
                        currentMarkMatch = Regex.Match(rowText, @"current mark\s*=\s*(\d+\.?\d*)");
                    }

                    if (!currentMarkMatch.Success)
                    {
                        // Look for MIDTERM MARK as fallback
                        currentMarkMatch = Regex.Match(rowText, @"MIDTERM MARK:\s*(\d+\.?\d*)%");
                    }

                    if (currentMarkMatch.Success && double.TryParse(currentMarkMatch.Groups[1].Value, out var mark))
                    {
                        course.OverallMark = mark;
                    }

                    // Extract subject_id from viewReport links (for fetching detailed assignments later)
                    var subjectIdMatch = Regex.Match(rowHtml, @"subject_id=(\d+)");
                    if (subjectIdMatch.Success)
                    {
                        course.SubjectId = subjectIdMatch.Groups[1].Value;

                        // Also extract student_id from the same link (might be different from global one)
                        var studentIdInLinkMatch = Regex.Match(rowHtml, @"student_id=(\d+)");
                        if (studentIdInLinkMatch.Success)
                        {
                            course.StudentId = studentIdInLinkMatch.Groups[1].Value;
                        }
                        else
                        {
                            course.StudentId = studentId;
                        }
                    }
                    else
                    {
                        // Debug: Save HTML for courses without subject_id
                        System.Diagnostics.Debug.WriteLine($"  No subject_id found for course: {course.Code}");
                        System.Diagnostics.Debug.WriteLine($"  Row HTML snippet: {rowHtml.Substring(0, Math.Min(300, rowHtml.Length))}");

                        // Try to find ANY hrefs or onclick handlers that might contain the subject_id
                        var allLinks = Regex.Matches(rowHtml, @"(href|onclick)=['""]([^'""]*)['""]", RegexOptions.IgnoreCase);
                        if (allLinks.Count > 0)
                        {
                            System.Diagnostics.Debug.WriteLine($"  Found {allLinks.Count} href/onclick attributes:");
                            foreach (Match match in allLinks)
                            {
                                System.Diagnostics.Debug.WriteLine($"    - {match.Groups[1].Value} = {match.Groups[2].Value}");
                            }
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine($"  No href/onclick found in row HTML");
                        }

                        // Check if there's a subject_id pattern anywhere in the row
                        var subjectIdAnywhere = Regex.Match(rowHtml, @"subject[_-]?id['""]?\s*[:=]\s*['""]?(\d+)", RegexOptions.IgnoreCase);
                        if (subjectIdAnywhere.Success)
                        {
                            System.Diagnostics.Debug.WriteLine($"  FOUND subject_id in unexpected format: {subjectIdAnywhere.Groups[1].Value}");
                            course.SubjectId = subjectIdAnywhere.Groups[1].Value;
                            course.StudentId = studentId;
                        }
                    }

                    // Only add if we have a valid code and it's not a lunch period
                    if (!course.Code.Contains("LUNCH") && !string.IsNullOrEmpty(course.Code))
                    {
                        System.Diagnostics.Debug.WriteLine($"Parsed course: {course.Code} - {course.Name} - Mark: {course.OverallMark} - SubjectId: {course.SubjectId}");
                        courses.Add(course);
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error parsing row: {ex.Message}");
                }
            }

            System.Diagnostics.Debug.WriteLine($"Total courses parsed: {courses.Count}");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Parse student page exception: {ex}");
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
            System.Diagnostics.Debug.WriteLine($"ParseCourseDetail: parsing HTML");
            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            // Detect CGC/Geography format first (has "By Overall Expectation" or Chart.js)
            var isCGCFormat = html.Contains("By Overall Expectation") ||
                             html.Contains("myChart") ||
                             html.Contains("Assessment Tasks") && html.Contains("Expectation");

            if (isCGCFormat)
            {
                System.Diagnostics.Debug.WriteLine($"  Detected CGC/Geography format");
                return ParseCGCCourseDetail(html, subjectId, studentId, doc);
            }

            // Use HtmlAgilityPack to find the h2 tag with course code
            var h2Nodes = doc.DocumentNode.SelectNodes("//h2");
            string? courseCode = null;

            if (h2Nodes != null)
            {
                System.Diagnostics.Debug.WriteLine($"  Found {h2Nodes.Count} h2 tags");

                foreach (var h2 in h2Nodes)
                {
                    var h2Text = h2.InnerText.Trim();
                    System.Diagnostics.Debug.WriteLine($"  h2 content: '{h2Text}'");

                    // Try to extract course code from h2 text
                    var codeMatch = Regex.Match(h2Text, @"([A-Z]{2,5}\d?[A-Z]*\d*-\d+)");
                    if (codeMatch.Success)
                    {
                        courseCode = codeMatch.Groups[1].Value;
                        System.Diagnostics.Debug.WriteLine($"  Extracted course code: {courseCode}");
                        break;
                    }
                }
            }

            if (string.IsNullOrEmpty(courseCode))
            {
                System.Diagnostics.Debug.WriteLine($"  No course code found in any h2 tag");
                // Show some HTML for debugging
                var bodyMatch = Regex.Match(html, @"<body[^>]*>(.*?)</body>", RegexOptions.Singleline | RegexOptions.IgnoreCase);
                if (bodyMatch.Success)
                {
                    var bodyContent = bodyMatch.Groups[1].Value;
                    var startIndex = Math.Max(0, bodyContent.IndexOf("align=\"center\"", StringComparison.OrdinalIgnoreCase) - 100);
                    System.Diagnostics.Debug.WriteLine($"  Body content around align center: {bodyContent.Substring(startIndex, Math.Min(500, bodyContent.Length - startIndex))}");
                }
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

            // Extract assignments from the main table
            // Try multiple selectors for different HTML formats
            var assignmentRows = doc.DocumentNode.SelectNodes("//tr[@rowspan='2']") ??
                                doc.DocumentNode.SelectNodes("//tr[td[@rowspan='2']]") ??
                                doc.DocumentNode.SelectNodes("//table[@border='1']//tr");

            if (assignmentRows != null)
            {
                System.Diagnostics.Debug.WriteLine($"  Found {assignmentRows.Count} potential assignment rows");

                foreach (var row in assignmentRows)
                {
                    try
                    {
                        var cells = row.SelectNodes(".//td");
                        if (cells == null || cells.Count < 2) continue;

                        // Get assignment name from first cell
                        var assignmentName = cells[0].InnerText.Trim();

                        // Skip non-assignment rows
                        if (string.IsNullOrEmpty(assignmentName) ||
                            assignmentName.Contains("Assignment") ||
                            assignmentName.Contains("Legend") ||
                            assignmentName.Contains("Category") ||
                            assignmentName.Length < 3) continue;

                        System.Diagnostics.Debug.WriteLine($"    Processing row: '{assignmentName}', cells: {cells.Count}");

                        // Get marks from each category cell (K/U, T, C, A)
                        for (int i = 1; i < cells.Count && i <= 4; i++)
                        {
                            var categoryCell = cells[i];
                            var innerTables = categoryCell.SelectNodes(".//table");

                            string cellText;
                            if (innerTables != null && innerTables.Count > 0)
                            {
                                // Get text from inner table
                                cellText = innerTables[0].InnerText;
                            }
                            else
                            {
                                // Get text directly from cell
                                cellText = categoryCell.InnerText.Trim();
                            }

                            // Skip empty cells
                            if (string.IsNullOrWhiteSpace(cellText) || cellText.Length < 5)
                                continue;

                            // Parse: "X / Y = Z% weight=W"
                            var markMatch = Regex.Match(cellText, @"([\d.]+)\s*/\s*([\d.]+)");
                            if (markMatch.Success)
                            {
                                var weightMatch = Regex.Match(cellText, @"weight=(\d+)");
                                var weight = weightMatch.Success && double.TryParse(weightMatch.Groups[1].Value, out var w) ? w : 0;

                                var assignment = new Assignment
                                {
                                    Name = assignmentName,
                                    MarkAchieved = double.TryParse(markMatch.Groups[1].Value, out var achieved) ? achieved : 0,
                                    MarkPossible = double.TryParse(markMatch.Groups[2].Value, out var possible) ? possible : 0,
                                    Weight = weight
                                };

                                // Determine category from column index
                                assignment.Category = i switch
                                {
                                    1 => "KU",   // Knowledge/Understanding (yellow)
                                    2 => "T",    // Thinking (green)
                                    3 => "C",    // Communication (purple)
                                    4 => "A",    // Application (orange)
                                    _ => "O"
                                };

                                course.Assignments.Add(assignment);
                                System.Diagnostics.Debug.WriteLine($"      Added: {assignment.Category} {assignment.MarkAchieved}/{assignment.MarkPossible} weight={assignment.Weight}");
                            }
                            else
                            {
                                System.Diagnostics.Debug.WriteLine($"      Cell {i} has content but no mark match: {cellText.Substring(0, Math.Min(40, cellText.Length))}");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"    Error parsing assignment row: {ex.Message}");
                    }
                }
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"  No assignment rows found");
            }

            // Extract weight table
            var weightTableRows = doc.DocumentNode.SelectNodes("//table[@border='1']//tr");
            if (weightTableRows != null)
            {
                System.Diagnostics.Debug.WriteLine($"  Found {weightTableRows.Count} weight table rows");

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
                            // Map category names to codes
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
