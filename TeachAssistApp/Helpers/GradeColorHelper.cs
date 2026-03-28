namespace TeachAssistApp.Helpers;

public static class GradeColorHelper
{
    public const string NA = "#FF30363D";
    public const string Tier95 = "#FF2EA043";  // A+ Forest Green
    public const string Tier90 = "#FF3FB950";  // A  Green
    public const string Tier85 = "#FF238636";  // A- Darker Green
    public const string Tier80 = "#FFD29922";  // B+ Gold
    public const string Tier75 = "#FF9A6700";  // B  Darker Gold
    public const string Tier70 = "#FFDB6D28";  // B- Orange
    public const string Tier65 = "#FFA57104";  // C+ Dark Orange
    public const string Tier60 = "#FFf85149";  // C  Red
    public const string Below60 = "#FFD73A49"; // Below C Darker Red

    public static string GetColor(double? mark)
    {
        if (mark == null) return NA;
        var m = mark.Value;
        if (m >= 95) return Tier95;
        if (m >= 90) return Tier90;
        if (m >= 85) return Tier85;
        if (m >= 80) return Tier80;
        if (m >= 75) return Tier75;
        if (m >= 70) return Tier70;
        if (m >= 65) return Tier65;
        if (m >= 60) return Tier60;
        return Below60;
    }
}
