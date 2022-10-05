﻿using System.Text.RegularExpressions;

namespace MDDBooster
{
    public static class StringHelper
    {
        public static string? RegexReturn(this string input, string pattern, int index)
        {
            var m = Regex.Match(input, pattern);
            return m.Success ? m.Groups[index].Value : null;
        }

        public static string Left(this string input, string search, bool last = false)
        {
            if (input == null) return string.Empty;

            var n = last
                ? input.LastIndexOf(search)
                : input.IndexOf(search);
            if (n < 0) return input;
            return input[..n];
        }

        public static string RightFromFirst(this string input, string search)
        {
            if (input == null) return string.Empty;

            var n = input.IndexOf(search);
            if (n < 0) return input;
            return input[(n + search.Length)..];
        }

        public static string Right(this string input, string search, bool include = false)
        {
            var n = input.LastIndexOf(search);
            if (n < 0)
                return input;

            else if (include)
                return input[n..];

            else
                return input[(n + search.Length)..];
        }

        public static string GetBetween(this string input, string begin, string to, bool last = false)
        {
            var first = last
                ? input.LastIndexOf(begin)
                : input.IndexOf(begin);

            if (first < 0) return String.Empty;

            var start = first >= 0 ? first + begin.Length : 0;
            var end = input.IndexOf(to, start);

            var s = end > 0 ? input[start..end] : input[start..];
            return s;
        }
    }
}