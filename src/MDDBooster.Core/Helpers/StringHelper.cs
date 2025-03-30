using Humanizer;
using Pluralize.NET;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace MDDBooster.Helpers
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
            return value.Pascalize();
        }

        /// <summary>
        /// 주어진 문자열을 카멜 케이스로 변환합니다.
        /// </summary>
        /// <remarks>
        /// 이 메서드는 다음과 같은 규칙을 따릅니다:
        /// <list type="bullet">
        /// <item>첫 번째 문자는 항상 소문자로 변환됩니다.</item>
        /// <item>대문자로 이루어진 단어 (약어)는 모두 소문자로 변환됩니다. (예: "SID" -> "sid").</item>
        /// <item>연속된 대문자로 시작하는 단어는 모두 소문자로 변환됩니다 (예: "MLType" -> "mlType").</item>
        /// <item>대문자 다음에 소문자가 오는 경우, 대문자는 유지됩니다 (예: "UserID" -> "userId").</item>
        /// <item>언더스코어('_')는 제거되며, 그 다음 문자는 대문자로 변환됩니다.</item>
        /// </list>
        /// </remarks>
                        /// <example>
        /// 사용 예:
        /// <code>
        /// string result1 = "SID".ToCamel(); // 결과: "sid"
        /// string result2 = "UserID".ToCamel(); // 결과: "userId"
        /// string result3 = "MLType".ToCamel(); // 결과: "mlType"
        /// string result4 = "ABC_DEF".ToCamel(); // 결과: "abcDef"
        /// </code>
        /// </example>
        public static string ToCamel(this string value, bool removeLowdash = true)
        {
            if (string.IsNullOrEmpty(value))
                return value;

            var result = new StringBuilder();
            bool nextUpper = false;
            bool firstChar = true;

            for (int i = 0; i < value.Length; i++)
            {
                if (value[i] == '_')
                {
                    if (!removeLowdash)
                    {
                        result.Append('_');
                    }
                    nextUpper = removeLowdash;
                }
                else
                {
                    if (char.IsUpper(value[i]))
                    {
                        if (firstChar)
                        {
                            result.Append(char.ToLower(value[i]));
                            firstChar = false;
                        }
                        else if (i + 1 < value.Length && char.IsLower(value[i + 1]))
                        {
                            // 대문자 다음에 소문자가 오는 경우
                            result.Append(value[i]);
                        }
                        else
                        {
                            // 연속된 대문자의 경우
                            result.Append(char.ToLower(value[i]));
                        }
                    }
                    else
                    {
                        if (nextUpper)
                        {
                            result.Append(char.ToUpper(value[i]));
                            nextUpper = false;
                        }
                        else
                        {
                            result.Append(value[i]);
                        }
                        firstChar = false;
                    }
                }
            }
            return result.ToString();
        }

        public static string ToCamelWithoutUnderline(this string value)
        {
            return value.ToCamel(true);
        }

        public static string ToSnakeCase(this string value)
        {
            return value.Underscore();
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
        public static bool IsPlural(this string name) => new Pluralizer().IsPlural(name);
        /// <summary>
        /// 단수 이름이면 True
        /// </summary>
        public static bool IsSingular(this string name) => new Pluralizer().IsSingular(name);

    }
}