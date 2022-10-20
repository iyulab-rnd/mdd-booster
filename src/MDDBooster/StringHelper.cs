using Pluralize.NET;
using System.Globalization;
using System.Text.RegularExpressions;

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

        public static string GetBetween(this string input, string begin, string end, bool last = false)
        {
            var first = last
                ? input.LastIndexOf(begin)
                : input.IndexOf(begin);

            if (first < 0) return String.Empty;

            var s = first >= 0 ? first + begin.Length : 0;
            var e = input.IndexOf(end, s);

            var r = e > 0 ? input[s..e] : input[s..];
            return r;
        }

        public static string ToPascal(this string value)
        {
            TextInfo info = CultureInfo.CurrentCulture.TextInfo;
            return info.ToTitleCase(value).Replace(" ", string.Empty);
        }

        public static string ToCamel(this string value)
        {
            return char.ToLowerInvariant(value[0]) + value[1..];
        }

        /// <summary>
        /// 복수 이름을 가져옵니다.
        /// </summary>
        public static string ToPlural(this string name)
        {
            var pluralizer = new Pluralizer();
            return pluralizer.Pluralize(name);
        }

        /// <summary>
        /// 단수 이름을 가져옵니다.
        /// </summary>
        public static string ToSingular(this string name)
        {
            var pluralizer = new Pluralizer();
            return pluralizer.Singularize(name);
        }

        /// <summary>
        /// 복수 이름이면 True
        /// </summary>
        public static bool IsPlural(this string name) => (new Pluralizer()).IsPlural(name);
        /// <summary>
        /// 단수 이름이면 True
        /// </summary>
        public static bool IsSingular(this string name) => (new Pluralizer()).IsSingular(name);

    }
}