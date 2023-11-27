using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MDDBooster
{
    public static class Resolver
    {
        public static Settings.Settings? Settings { get; private set; }
        public static IModelMeta[]? Models { get; private set; }

        public static void Init(Settings.Settings settings, IModelMeta[] models)
        {
            Models = models;
            Settings = settings;
        }
    }
}
