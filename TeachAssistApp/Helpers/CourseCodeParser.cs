using System.Collections.Generic;

namespace TeachAssistApp.Helpers
{
    public static class CourseCodeParser
    {
        private static readonly Dictionary<string, string> SubjectCodes = new()
        {
            {"ADA", "Drama"},
            {"AMU", "Music"},
            {"AVI", "Visual Arts"},
            {"BAF", "Financial Accounting"},
            {"BAT", "Accounting"},
            {"BBI", "Business"},
            {"CGC", "Geography of Canada"},
            {"CHC", "Canadian History"},
            {"CHV", "Civics"},
            {"CHW", "World History"},
            {"CIA", "Analyzing Current Issues"},
            {"CLN", "Law"},
            {"ENG", "English"},
            {"ENL", "English"},
            {"ESL", "English as a Second Language"},
            {"FSF", "Core French"},
            {"FIF", "French Immersion"},
            {"GLC", "Career Studies"},
            {"GLS", "Learning Strategies"},
            {"GPP", "Leadership"},
            {"HFA", "Food & Nutrition"},
            {"HFN", "Food & Nutrition"},
            {"HHS", "Human Services"},
            {"HIF", "Individual & Family"},
            {"HIP", "Psychology"},
            {"HRE", "Religion"},
            {"HSB", "Social Sciences"},
            {"HSP", "Anthropology & Sociology"},
            {"ICS", "Computer Science"},
            {"ICD", "Computer Science"},
            {"MCR", "Functions"},
            {"MCT", "Mathematics"},
            {"MCV", "Calculus & Vectors"},
            {"MDM", "Data Management"},
            {"MEL", "Mathematics for Work"},
            {"MFM", "Foundations of Mathematics"},
            {"MHF", "Advanced Functions"},
            {"MPM", "Principles of Mathematics"},
            {"MTH", "Mathematics"},
            {"NBE", "Indigenous Studies"},
            {"OLC", "Ontario Literacy Course"},
            {"PPL", "Healthy Active Living"},
            {"PAD", "Outdoor Activities"},
            {"PAF", "Personal Fitness"},
            {"PAI", "Physical Activities"},
            {"PSK", "Introductory Kinesiology"},
            {"SBI", "Biology"},
            {"SCH", "Chemistry"},
            {"SES", "Earth & Space Science"},
            {"SNC", "Science"},
            {"SPH", "Physics"},
            {"SVN", "Environmental Science"},
            {"TEJ", "Computer Engineering"},
            {"TDJ", "Technological Design"},
            {"TIK", "Computer Technology"},
            {"TGJ", "Communications Technology"},
            {"TMJ", "Manufacturing Technology"},
            {"TTJ", "Transportation Technology"},
            {"TWJ", "Construction Technology"}
        };

        private static readonly Dictionary<char, string> Pathways = new()
        {
            {'D', "Academic"},
            {'P', "Applied"},
            {'O', "Open"},
            {'U', "University"},
            {'M', "University/College"},
            {'C', "College"},
            {'E', "Workplace"},
            {'W', "Destreamed"},
            {'L', "Locally Developed"}
        };

        public static (string SubjectName, string GradeLevel, string Pathway) Parse(string courseCode)
        {
            if (string.IsNullOrEmpty(courseCode) || courseCode.Length < 5)
                return (courseCode ?? "", "", "");

            // Handle ESL courses specially (ESLAO, ESLBO, ESLCO, ESLDO, ESLEO)
            if (courseCode.StartsWith("ESL") && courseCode.Length >= 5)
            {
                char eslLevel = courseCode[3]; // A, B, C, D, or E
                return ("English as a Second Language", $"Level {eslLevel}", "Open");
            }

            // Extract parts: ABC#X# (e.g., MTH1W1-8)
            string subjectCode = courseCode.Length >= 3 ? courseCode.Substring(0, 3) : courseCode;

            // Grade number is 4th character
            string gradeLevel = "";
            if (courseCode.Length >= 4 && char.IsDigit(courseCode[3]))
            {
                int gradeNum = courseCode[3] - '0';
                gradeLevel = $"Grade {gradeNum + 8}"; // 1=Grade 9, 2=Grade 10, etc.
            }

            // Pathway is 5th character
            string pathway = "";
            if (courseCode.Length >= 5 && Pathways.ContainsKey(courseCode[4]))
            {
                pathway = Pathways[courseCode[4]];
            }

            // Look up subject name
            string subjectName = SubjectCodes.ContainsKey(subjectCode)
                ? SubjectCodes[subjectCode]
                : subjectCode;

            return (subjectName, gradeLevel, pathway);
        }

        public static string GetDisplayText(string courseCode)
        {
            var (subject, grade, pathway) = Parse(courseCode);

            // Special handling for ESL courses - shorter display
            if (courseCode.StartsWith("ESL") && courseCode.Length >= 5)
            {
                char eslLevel = courseCode[3]; // A, B, C, D, or E
                return $"ESL • Level {eslLevel}";
            }

            if (string.IsNullOrEmpty(grade) && string.IsNullOrEmpty(pathway))
                return subject;

            if (!string.IsNullOrEmpty(grade) && !string.IsNullOrEmpty(pathway))
                return $"{subject} • {grade} {pathway}";

            return $"{subject} • {grade}{pathway}";
        }
    }
}
