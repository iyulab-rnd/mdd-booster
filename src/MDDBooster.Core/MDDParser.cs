using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MDDBooster
{
    public class MDDParser
    {
        public static IModelMeta[] Parse(string text)
        {
            var blocks = new List<string>();
            var sb = new StringBuilder();
            foreach (var line in text.Split(Constants.NewLine))
            {
                if (line.StartsWith("##"))
                {
                    if (sb.Length > 0) blocks.Add(sb.ToString());

                    sb.Clear();
                    sb.AppendLine(line);
                }
                else if (line.StartsWith('-'))
                {
                    sb.AppendLine(line);
                }
            }
            if (sb.Length > 0) blocks.Add(sb.ToString());

            var models = new List<IModelMeta>();
            foreach (var block in blocks)
            {
                var model = ModelMetaFactory.Create(block);
                if (model == null) continue;

                models.Add(model);
            }

            models.OfType<ModelMetaBase>().ToList().ForEach(p =>
            {
                var interfaces = new List<InterfaceMeta>();
                AbstractMeta? abstractMeta = null;

                foreach(var interfaceName in p.InterfaceNames)
                {
                    var m = models.FirstOrDefault(p => p.Name == interfaceName);

                    if (m is InterfaceMeta interfaceMeta)
                        interfaces.Add(interfaceMeta);
                    else
                        interfaces.Add(new InterfaceMeta(interfaceName, string.Empty, string.Empty));
                }

                if (string.IsNullOrEmpty(p.AbstractName) != true)
                {
                    var m = models.FirstOrDefault(m => m.Name == p.AbstractName);
                    if (m is AbstractMeta mAbs)
                        abstractMeta = mAbs;
                    else
                        throw new NotImplementedException(p.AbstractName);
                }

                p.Interfaces = [.. interfaces];
                p.Abstract = abstractMeta;
            });

            var defaults = models.OfType<ModelMetaBase>().FirstOrDefault(p => p.IsDefault());
            if (defaults != null)
            {
                foreach (var p in models.OfType<TableMeta>())
                {
                    if (defaults is InterfaceMeta m1)
                        p.Interfaces = [m1];

                    else if (defaults is AbstractMeta m2)
                        p.Abstract = m2;
                }
            }

            return [.. models];
        }
    }
}
