using System.Reflection;

namespace MDDBooster
{
    public static class Utils
    {
        internal static string? ResolvePath(string? basePath, string parameterPath, string? fileName = null)
        {
            if (parameterPath == null) return null;
            if (Path.IsPathRooted(parameterPath)) return parameterPath;

            basePath ??= Assembly.GetExecutingAssembly().Location;
            var cd = new DirectoryInfo(basePath);
            var splits = parameterPath.Replace(@"\", "/").Split("/");

            var paths = new List<string>();
            foreach (var name in splits)
            {
                if (name.Equals(".."))
                    cd = cd!.Parent;

                else if (name.Equals("."))
                    continue;

                else
                    paths.Add(name);
            }

            paths.Insert(0, cd.ToString());
            var dir = new DirectoryInfo(Path.Combine(paths.ToArray()));
            if (fileName == null)
                return dir.ToString();

            else
                return System.IO.Path.Combine(dir.ToString(), fileName);
        }

        internal static bool IsInterfaceName(string name)
        {
            return name.StartsWith("I") && Char.IsUpper(name[1]);
        }

        internal static bool IsAbstract(string headline)
        {
            return headline.Contains("@abstract");
        }

        internal static string GetNameWithoutKey(string name)
        {
            return name.EndsWith("_id") ? name.Left("_id")
                : name.EndsWith("_key") ? name.Left("_key")
                : name.EndsWith("Id") ? name.Left("Id")
                : name.EndsWith("Key") ? name.Left("Key")
                : throw new Exception("Rule위배: FK는 _id, _key, Id, Key로 끝나야 합니다.");
        }
    }
}