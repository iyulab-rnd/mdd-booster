using MDDBooster.Models;
using System.Diagnostics;

namespace MDDBooster;

public static class Functions
{
    internal static TableMeta FindTable(string name)
    {
        Debug.WriteLine($"FindTable: {name}");
        return Resolver.Models?.OfType<TableMeta>().First(p => p.Name == name) ?? throw new Exception($"cannot find table - {name}");
    }

    internal static IEnumerable<TableMeta> FindChildren(TableMeta table)
    {
        return Resolver.Models?.OfType<TableMeta>()
            .Where(p => p != table
                && p.Columns.Any(n => n.FK && n.GetForeignKeyEntityName() == table.Name))
            ?? throw new Exception($"cannot find children - {table.Name}");
    }

    /// <summary>
    /// 주석과 코멘트를 제외한 라인을 가져옵니다.
    /// </summary>
    internal static string GetConentLine(string line)
    {
        return line.LeftOr("#").LeftOr("//").Trim();
    }

    internal static Task FileWriteAsync(string path, string contents)
    {
        return File.WriteAllTextAsync(path, contents.Trim());
    }

    internal static void FileWrite(string path, string contents)
    {
        File.WriteAllText(path, contents.Trim());
    }
}