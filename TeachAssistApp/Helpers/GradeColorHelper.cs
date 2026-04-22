namespace TeachAssistApp.Helpers;

public static class GradeColorHelper
{
    public const string NA = "#FF8B949E";       // Medium gray — visible on both light and dark
    public const string Tier95 = "#FF1A7F37";   // A+ Rich Green
    public const string Tier90 = "#FF2DA44E";   // A  Green
    public const string Tier85 = "#FF238636";   // A- Forest Green
    public const string Tier80 = "#FFBF8700";   // B+ Amber Gold
    public const string Tier75 = "#FF9A6700";   // B  Dark Gold
    public const string Tier70 = "#FFBC4C00";   // B- Burnt Orange
    public const string Tier65 = "#FF954A00";   // C+ Deep Orange
    public const string Tier60 = "#FFCF222E";   // C  Red
    public const string Below60 = "#FFA40E26";  // Below C Dark Red

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
