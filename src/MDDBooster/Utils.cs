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
    }
}