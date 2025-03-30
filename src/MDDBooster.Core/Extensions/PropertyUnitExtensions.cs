using Microsoft.CodeAnalysis.CSharp.Units;

namespace MDDBooster.Extensions;

public static class PropertyUnitExtensions
{
    public static bool HasMaxLength(this PropertyUnit property, out int maxLength)
    {
        maxLength = 0;

        var maxLengthAttr = property.Attributes.FirstOrDefault(a =>
            a.Name == "MaxLength" || a.Name == "StringLength");

        if (maxLengthAttr != null)
        {
            // 인수가 비어있지 않으면 첫 번째 인수가 최대 길이
            if (maxLengthAttr.Arguments.Count > 0)
            {
                var lengthArg = maxLengthAttr.Arguments.FirstOrDefault();
                if (int.TryParse(lengthArg.Value, out maxLength))
                {
                    return true;
                }
            }
        }

        return false;
    }

    public static bool HasRange(this PropertyUnit property, out object? minValue, out object? maxValue)
    {
        minValue = null;
        maxValue = null;

        var rangeAttr = property.Attributes.FirstOrDefault(a => a.Name == "Range");

        if (rangeAttr != null && rangeAttr.Arguments.Count >= 2)
        {
            var args = rangeAttr.Arguments.Values.ToList();
            minValue = args[0].Trim('"');
            maxValue = args[1].Trim('"');
            return true;
        }

        return false;
    }

    public static bool HasRegularExpression(this PropertyUnit property, out string? pattern)
    {
        pattern = null;

        var regexAttr = property.Attributes.FirstOrDefault(a => a.Name == "RegularExpression");

        if (regexAttr != null && regexAttr.Arguments.Count > 0)
        {
            var patternArg = regexAttr.Arguments.FirstOrDefault();
            pattern = patternArg.Value?.Trim('"');
            return !string.IsNullOrEmpty(pattern);
        }

        return false;
    }

    public static bool HasFileAccepts(this PropertyUnit property, out string[] accepts)
    {
        accepts = Array.Empty<string>();

        // FileExtensions 어트리뷰트 확인
        var fileExtensionsAttr = property.Attributes.FirstOrDefault(a => a.Name == "FileExtensions");
        if (fileExtensionsAttr != null && fileExtensionsAttr.Arguments.ContainsKey("Extensions"))
        {
            var extensions = fileExtensionsAttr.Arguments["Extensions"].Trim('"').Split(',');
            accepts = extensions.Select(e => e.Trim().StartsWith(".") ? e.Trim() : "." + e.Trim()).ToArray();
            return accepts.Length > 0;
        }

        // 사용자 정의 File 어트리뷰트 확인
        var fileAttr = property.Attributes.FirstOrDefault(a => a.Name.Contains("File") && a.Arguments.ContainsKey("Accepts"));
        if (fileAttr != null)
        {
            var acceptsValue = fileAttr.Arguments["Accepts"];
            // 문자열에서 배열 형식 파싱 (예: "new[] { ".jpg", ".png" }")
            if (acceptsValue.Contains("{") && acceptsValue.Contains("}"))
            {
                var items = acceptsValue.Substring(
                    acceptsValue.IndexOf('{') + 1,
                    acceptsValue.LastIndexOf('}') - acceptsValue.IndexOf('{') - 1
                ).Split(',');

                accepts = items.Select(i => i.Trim().Trim('"', '\'')).ToArray();
                return accepts.Length > 0;
            }
        }

        // 기본 파일 확장자 설정 (특정 속성명에 따라)
        if (property.Type == "byte[]")
        {
            var name = property.Name.ToLower();
            if (name.Contains("image") || name.Contains("photo") || name.Contains("picture"))
            {
                accepts = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
                return true;
            }
            else if (name.Contains("document") || name.Contains("doc"))
            {
                accepts = new[] { ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".ppt", ".pptx" };
                return true;
            }
        }

        return false;
    }

    public static string GetSuggestedLabel(this PropertyUnit property)
    {
        // 속성 이름을 사람이 읽기 쉬운 형태로 변환
        var name = property.Name;

        // 속성 이름에서 앞의 밑줄 제거
        if (name.StartsWith("_"))
        {
            name = name.Substring(1);
        }

        // PascalCase를 공백으로 구분된 단어로 변환
        var result = System.Text.RegularExpressions.Regex.Replace(
            name,
            "([A-Z])",
            " $1"
        ).Trim();

        // 첫 글자를 대문자로 변환
        if (result.Length > 0)
        {
            result = char.ToUpper(result[0]) + result.Substring(1);
        }

        return result;
    }
}