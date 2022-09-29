using System;
using System.Text.RegularExpressions;

namespace DebitExpress.VatRelief.Utils;

internal static class Extensions
{
    public static string CleanUp(this string str)
    {
        if (string.IsNullOrWhiteSpace(str)) return string.Empty;
        return Regex.Replace(str, "[,$`&\"']", string.Empty).NormalizeWhiteSpace().Trim().ToUpper();
    }

    public static string NormalizeWhiteSpace(this string input)
    {
        int len = input.Length,
            index = 0,
            i = 0;
        var src = input.ToCharArray();
        var skip = false;
        for (; i < len; i++)
        {
            var ch = src[i];
            switch (ch)
            {
                case '\u0020':
                case '\u00A0':
                case '\u1680':
                case '\u2000':
                case '\u2001':
                case '\u2002':
                case '\u2003':
                case '\u2004':
                case '\u2005':
                case '\u2006':
                case '\u2007':
                case '\u2008':
                case '\u2009':
                case '\u200A':
                case '\u202F':
                case '\u205F':
                case '\u3000':
                case '\u2028':
                case '\u2029':
                case '\u0009':
                case '\u000A':
                case '\u000B':
                case '\u000C':
                case '\u000D':
                case '\u0085':
                    if (skip) continue;
                    src[index++] = ch;
                    skip = true;
                    continue;
                default:
                    skip = false;
                    src[index++] = ch;
                    continue;
            }
        }

        return new string(src, 0, index);
    }

    public static string Strip(this string str)
    {
        if (string.IsNullOrWhiteSpace(str)) return string.Empty;

        var num = Regex.Replace(str, "[^0-9]", "");
        return num.PadRight(9, '0')[..9];
    }

    public static string ToValue(this decimal val)
    {
        var rounded = Math.Round(val, 2);
        return val % 1 == 0 ? $"{Math.Truncate(val)}" : $"{rounded}";
    }

    public static string Round(this decimal val) => $"{Math.Round(val, 2):#0.00}";

    public static decimal ToDecimal(this string str)
    {
        decimal.TryParse(str, out var result);
        return result;
    }

    public static bool IsTrue(this string? str)
    {
        if (string.IsNullOrEmpty(str)) return false;

        var upper = str.ToUpper();
        return upper is "YES" or "Y" or "TRUE" or "T" or "1";
    }

    public static bool IsValidTin(this string? str)
    {
        if (string.IsNullOrEmpty(str)) return false;

        return new Regex("^[0-9]\\d{2}-[0-9]\\d{2}-[0-9]\\d{2}-[0-9]\\d{2,4}$").IsMatch(str);
    }

    public static string QuarterRangeString(int startingMonth, int year)
    {
        var startingDate = GetEndOfMonth(year, startingMonth);
        var endingDate = GetEndOfMonth(year, startingMonth + 2);

        return $"{startingDate:MMddyyyy}-{endingDate:MMddyyyy}";
    }

    public static DateTime GetEndOfMonth(int year, int month)
    {
        const int lastMonth = 12;
        if (month <= lastMonth)
        {
            var endDay = DateTime.DaysInMonth(year, month);
            return new DateTime(year, month, endDay);
        }

        var eom = DateTime.DaysInMonth(year + 1, month - lastMonth);
        return new DateTime(year + 1, month - lastMonth, eom);
    }
}