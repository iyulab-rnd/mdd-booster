using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MDDBooster
{
    public static class AppFunctions
    {
        public static ILogger? Logger { get; set; }

        internal static void WriteFile(string path, string code)
        {
            Logger?.LogInformation("Write File: {path}", Path.GetFileName(path));

            var text = code.Replace("\t", "    ");
            File.WriteAllText(path, text);
        }
    }
}
