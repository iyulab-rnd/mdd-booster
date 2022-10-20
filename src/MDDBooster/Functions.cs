using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MDDBooster
{
    public static class Functions
    {
        internal static TableMeta FindTable(string name)
        {
            return Resolver.Models.OfType<TableMeta>().First(p => p.Name == name);
        }
    }
}
