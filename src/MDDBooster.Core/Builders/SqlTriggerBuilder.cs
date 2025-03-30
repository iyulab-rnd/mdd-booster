using MDDBooster.Models;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Net;
using System.Text;

namespace MDDBooster.Builders
{
    public class SqlTriggerBuilder : BuilderBase
    {
        private readonly TableMeta table;

        public SqlTriggerBuilder(TableMeta m) : base(m)
        {
            this.table = m;
        }

        internal void Build(string basePath)
        {
#if DEBUG
            if (this.meta.Name == "Plan")
            {
            }
#endif
            var children = Functions.FindChildren(this.table);
            if (children.Any() != true) return;

            var lines = new List<string>();
            foreach(var child in children)
            {
                var fkColumns = child.GetFkColumns();
                foreach(var fkColumn in fkColumns)
                {
                    var fktName = fkColumn.GetForeignKeyEntityName();
                    if (fktName != this.Name) continue;

                    if (fkColumn.IsNotNull())
                    {
                        lines.Add($"DELETE FROM [{child.Name}] WHERE [{fkColumn.Name}] IN (SELECT deleted.{this.meta.GetPKColumn().Name} FROM deleted)");
                    }
                    else
                    {
                        lines.Add($"UPDATE [{child.Name}] SET [{fkColumn.Name}] = NULL WHERE [{fkColumn.Name}] IN (SELECT deleted.{this.meta.GetPKColumn().Name} FROM deleted)");
                    }
                }
            }
            if (lines.Any() != true) return;

            var linesText = string.Join(Constants.NewLine, lines);

            var code = $@"-- # {Constants.NO_NOT_EDIT_MESSAGE}
CREATE TRIGGER {Name}Trigger
    ON [dbo].[{Name}]
    FOR DELETE
AS

{linesText}

GO";
            var text = code.Replace("\t", "    ");
            var path = Path.Combine(basePath, $"{Name}Trigger.sql");
            Functions.FileWrite(path, text);
        }
    }
}