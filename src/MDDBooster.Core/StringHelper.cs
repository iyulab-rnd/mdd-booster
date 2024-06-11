using Pluralize.NET;
using System.Globalization;
using System.Text.RegularExpressions;

namespace MDDBooster
{
    internal static class StringHelper
    {
        public static string? RegexReturn(this string input, string pattern, int index)
        {
            var m = Regex.Match(input, pattern);
            return m.Success ? m.Groups[index].Value : null;
        }

        /// <summary>
        /// search 이전의 값을 가져옵니다. search가 없으면 전체를 가져옵니다.
        /// </summary>
        public static string LeftOr(this string input, string search, bool last = false)
        {
            if (input == null) return string.Empty;

            var n = last
                ? input.LastIndexOf(search)
                : input.IndexOf(search);
            if (n < 0) return input;
            return input[..n];
        }

        /// <summary>
        /// search 이전의 값을 가져옵니다. search가 없으면 빈 문자열을 가져옵니다.
        /// </summary>
        public static string Left(this string input, string search, bool last = false)
        {
            if (input == null) return string.Empty;

            var n = last
                ? input.LastIndexOf(search)
                : input.IndexOf(search);
            if (n < 0) return string.Empty;
            return input[..n];
        }

        /// <summary>
        /// search 이후의 값을 가져옵니다. search가 없으면 빈 값을 가져옵니다.
        /// </summary>
        public static string Right(this string input, string search, bool include = false, bool lastTo = true)
        {
            var n = lastTo ? input.LastIndexOf(search) : input.IndexOf(search);
            if (n < 0)
                return string.Empty;

            else if (include)
                return input[n..];

            else
                return input[(n + search.Length)..];
        }

        /// <summary>
        /// search 이후의 값을 가져옵니다. search가 없으면 전체를 가져옵니다.
        /// </summary>
        public static string RightOr(this string input, string search, bool include = false, bool lastTo = true)
        {
            var n = lastTo ? input.LastIndexOf(search) : input.IndexOf(search);
            if (n < 0)
                return input;

            else if (include)
                return input[n..];

            else
                return input[(n + search.Length)..];
        }

        /// <summary>
        /// begin, end 사이의 값을 가져옵니다.
        /// begin이후 가장 빠른 end 를 찾습니다.
        /// </summary>
        public static string GetBetween(this string input, string begin, string end, bool last = false, bool include = false)
        {
            var first = last
                ? input.LastIndexOf(begin)
                : input.IndexOf(begin);

            if (first < 0) return string.Empty;

            var s = first >= 0 ? first + begin.Length : 0;
            var e = input.IndexOf(end, s);

            if (e < 0) return string.Empty;

            if (include)
            {
                s = first;
                e += end.Length;
            }

            var r = e > 0 ? input[s..e] : input[s..];
            return r;
        }

        /// <summary>
        /// begin, end 사이의 값을 가져옵니다.
        /// begin, end의 쌍을 고려합니다. 중첩된 괄호가 있을 때 가장 바깥쪽 괄호를 찾습니다.
        /// </summary>
        public static string GetBetweenBlock(this string input, string begin, string end)
        {
            var stack = new Stack<int>();
            int startIndex = -1;
            int endIndex = -1;

            for (int i = 0; i < input.Length; i++)
            {
                var subString = input[i..];
                if (subString.StartsWith(begin))
                {
                    stack.Push(i);
                }
                else if (subString.StartsWith(end))
                {
                    if (stack.Count == 0)
                    {
                        return null; // Unbalanced blocks
                    }

                    startIndex = stack.Pop();
                    endIndex = i;
                }

                if (stack.Count == 0 && startIndex != -1 && endIndex != -1)
                {
                    break; // We've found our outermost block
                }
            }

            if (startIndex == -1 || endIndex == -1)
            {
                return null; // No valid block found
            }

            return input.Substring(startIndex + begin.Length, endIndex - startIndex - begin.Length);
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
            var r = pluralizer.Pluralize(name);
            if (r == name)
                return name + "es";
            else
                return r;
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