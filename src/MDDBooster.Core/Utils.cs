using System.Diagnostics;
using System.Reflection;

namespace MDDBooster
{
    public static class Utils
    {
        public static string ResolvePath(string? basePath, params string[] subPaths)
        {
            basePath ??= Assembly.GetExecutingAssembly().Location;

            var fistPath = subPaths.FirstOrDefault();
            if (fistPath == null) return basePath;
            if (Path.IsPathRooted(fistPath))
            {
                basePath = fistPath;
            }

            var cd = new DirectoryInfo(basePath);
            var parameterPath = string.Join("/", subPaths);
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
            return dir.ToString();
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
                : name;
        }

        internal static void ResetDirectory(string path)
        {
            if (Directory.Exists(path)) Directory.Delete(path, true);
            Directory.CreateDirectory(path);
        }

        public static string GetVirtualOneName(TableMeta table, ColumnMeta column)
        {
            var name = GetNameWithoutKey(column.Name); 
            return table.Columns.Any(p => p.Name == name) ? name + "Item" : name;
        }

        internal static string GetVirtualManeName(TableMeta child)
        {
            var pName = child.Name.ToPlural();
            return pName;
        }
    }
}