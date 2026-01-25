using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using HtmlAgilityPack;
using System.IO;

/// Simple tool to test TeachAssist login independently
class TeachAssistLoginTester
{
    static async Task Main(string[] args)
    {
        var username = "440003227";
        var password = "2tfqp4sp";

        Console.WriteLine("=== TeachAssist Login Tester ===");
        Console.WriteLine($"Username: {username}");
        Console.WriteLine($"Password: {password}");
        Console.WriteLine();

        var handler = new HttpClientHandler
        {
            CookieContainer = new CookieContainer(),
            UseCookies = true,
            AllowAutoRedirect = true
        };
        var client = new HttpClient(handler);
        client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");

        try
        {
            // Step 1: Get login page
            Console.WriteLine("Step 1: Getting login page...");
            var getResponse = await client.GetAsync("https://ta.yrdsb.ca/yrdsb/");
            var getContent = await getResponse.Content.ReadAsStringAsync();
            Console.WriteLine($"  Status: {getResponse.StatusCode}");
            Console.WriteLine($"  Content length: {getContent.Length}");
            Console.WriteLine();

            // Step 2: Post login
            Console.WriteLine("Step 2: Posting login...");
            var formData = new Dictionary<string, string>
            {
                {"username", username},
                {"password", password}
            };
            var formContent = new FormUrlEncodedContent(formData);
            var postResponse = await client.PostAsync("https://ta.yrdsb.ca/yrdsb/index.php", formContent);
            var postResponseContent = await postResponse.Content.ReadAsStringAsync();
            Console.WriteLine($"  Status: {postResponse.StatusCode}");
            Console.WriteLine($"  Content length: {postResponseContent.Length}");

            // Save full response to file for inspection
            var outputFile = "C:/Users/chang/Downloads/TA/TestLogin/login_response.html";
            await File.WriteAllTextAsync(outputFile, postResponseContent);
            Console.WriteLine($"  Saved full response to: {outputFile}");
            Console.WriteLine();

            // Look for course codes in the login response
            var courseMatches = Regex.Matches(postResponseContent, @"[A-Z]{3}\d[A-Z]\d");
            Console.WriteLine($"  Found {courseMatches.Count} course codes in login response:");
            foreach (Match match in courseMatches)
            {
                Console.WriteLine($"    - {match.Value}");
            }

            // Look for percentages
            var percentMatches = Regex.Matches(postResponseContent, @"(\d+\.?\d*)\s*%");
            Console.WriteLine();
            Console.WriteLine($"  Found {percentMatches.Count} grade percentages in login response:");
            foreach (Match match in percentMatches.Take(20))
            {
                Console.WriteLine($"    - {match.Value}");
            }

            // Look for tables
            var doc = new HtmlDocument();
            doc.LoadHtml(postResponseContent);
            var tables = doc.DocumentNode.SelectNodes("//table");
            Console.WriteLine();
            Console.WriteLine($"  Found {tables?.Count ?? 0} tables in login response");

            // Try to find all links
            var links = doc.DocumentNode.SelectNodes("//a[@href]");
            Console.WriteLine();
            Console.WriteLine($"  Found {links?.Count ?? 0} links in login response:");
            if (links != null)
            {
                var uniqueLinks = links.Take(20).Select(l => l.GetAttributeValue("href", "")).Distinct();
                foreach (var link in uniqueLinks)
                {
                    Console.WriteLine($"    - {link}");
                }
            }

            Console.WriteLine();
            if (courseMatches.Count > 0)
            {
                Console.WriteLine("  ✅ Course data is in the login response itself!");
            }
            else
            {
                Console.WriteLine("  ⚠️  No course codes found in login response");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Exception: {ex.Message}");
            Console.WriteLine($"   {ex.StackTrace}");
        }

        Console.WriteLine();
        Console.WriteLine("Done. Check the HTML file for more details.");
    }
}
