using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace MDDBooster.Builders
{
    public class GraphQLBuilder
    {
        private readonly IModelMeta[] models;
        private readonly Settings.Settings settings;

        private static readonly Dictionary<Type, string> mapGraphTypes = new()
        {
            //{ typeof(int), "IdGraphType" },
            { typeof(Guid), "GuidGraphType" },
            { typeof(string), "StringGraphType" },
            { typeof(int), "IntGraphType" },
            { typeof(BigInteger), "BigIntGraphType" },
            { typeof(bool), "BooleanGraphType" },
            { typeof(Byte), "ByteGraphType" },
            //{ typeof(DateOnly), "DateGraphType" },
            { typeof(DateOnly), "DateOnlyGraphType" },
            { typeof(DateTime), "DateTimeGraphType" },
            { typeof(DateTimeOffset), "DateTimeOffsetGraphType" },
            { typeof(TimeOnly), "TimeOnlyGraphType" },
            //{ typeof(TimeSpan), "TimeSpanMillisecondsGraphType" },
            { typeof(TimeSpan), "TimeSpanSecondsGraphType" },
            { typeof(Decimal), "DecimalGraphType" },
            //{ typeof(int), "EnumerationGraphType" },
            { typeof(float), "FloatGraphType" },
            { typeof(long), "LongGraphType" },
            { typeof(uint), "UIntGraphType" },
            { typeof(ulong), "ULongGraphType" },
            { typeof(ushort), "UShortGraphType" },
            { typeof(Uri), "UriGraphType" }
        };

        public GraphQLBuilder(IModelMeta[] models, Settings.Settings settings)
        {
            this.models = models;
            this.settings = settings;
        }

        public void Build()
        {
            BuildGqlModels();
            BuildGqlAppFiles();
        }

        private void BuildGqlModels()
        {
            var basePath = Utils.ResolvePath(settings.BasePath, settings.ModelProject.Path, "Gql_");
            Utils.ResetDirectory(basePath);

            var tables = models.OfType<TableMeta>();
            var modelNS = settings.ModelProject.Namespace;

            foreach(var table in tables)
            {
                var name = table.Name;
                var pName = table.Name.ToPlural();

                var sbPropertyLines = new StringBuilder();
                foreach (var column in table.FullColumns)
                {
                    var sysType = column.GetSystemType();
                    if (sysType == typeof(Guid) || sysType == typeof(string))
                    {
                        var typeName = column.GetSystemTypeAlias();
                        var propertyName = column.Name.ToPlural();
                        sbPropertyLines.AppendLine($"\t\tpublic {typeName}[]? {propertyName} {{ get; set; }}");
                    }
                }

                var code = $@"// # {Constants.NO_NOT_EDIT_MESSAGE}
#pragma warning disable CS8618, IDE1006

using Iyu.Data;
using {modelNS}.Entity;

namespace {modelNS}.Gql
{{
    public class {name}SearchRequest : IGqlSearchRequest<{name}>
    {{
        public Guid _key {{ get; set; }}
        public string[] Columns {{ get; set; }}
    }}

    public class {pName}SearchRequest : PageRequestBase, IGqlSearchRequest<{name}>
    {{
{sbPropertyLines}
    }}
}}

#pragma warning restore CS8618, IDE1006";
                var fileName = $"{table.Name}SearchRequest.cs";
                var path = Path.Combine(basePath, fileName);
                AppFunctions.WriteFile(path, code);
            }
        }

        private void BuildGqlAppFiles()
        {
            var basePath = Utils.ResolvePath(settings.BasePath, settings.ServerProject!.Path, "Gql_");
            Utils.ResetDirectory(basePath);

            BuildAppGqlQuery(basePath);
            BuildAppGqlSchema(basePath);
            BuildAppGqlValidationRule(basePath);

            foreach (var table in models.OfType<TableMeta>())
            {
                BuildSchemaFiles(basePath, table);
            }
        }

        private void BuildAppGqlQuery(string basePath)
        {
            var tableNames = models.OfType<TableMeta>().Select(p => p.Name);

            var lines = tableNames.Select(name => $"Register(provider.GetService<{name}Query>()!);");
            var linesText = string.Join($"{Constants.NewLine}\t\t\t", lines);

            var ns = settings.ServerProject!.Namespace;
            var code = $@"// # {Constants.NO_NOT_EDIT_MESSAGE}
using GraphQL.Types;
using Iyu.Server.OData.Services;
using Microsoft.Extensions.DependencyInjection;
using {ns}.Gql.Schemas;

namespace {ns}.Gql
{{
    public partial class AppGqlQuery : ObjectGraphType<object>
    {{
        public AppGqlQuery(IServiceProvider provider)
        {{
            Name = ""Query"";

            {linesText}
        }}

        private void Register(IGqlQuery queryType)
        {{
            queryType.DefineQuery(this);
        }}
    }}
}}";
            var fileName = $"AppGqlQuery.cs";
            var path = Path.Combine(basePath, fileName);
            AppFunctions.WriteFile(path, code);
        }

        private void BuildAppGqlSchema(string basePath)
        {
            var ns = settings.ServerProject!.Namespace;
            var code = $@"// # {Constants.NO_NOT_EDIT_MESSAGE}
using GraphQL.Instrumentation;
using GraphQL.Types;

namespace {ns}.Gql
{{
    public partial class AppGqlSchema : Schema
    {{
        public AppGqlSchema(IServiceProvider provider) : base(provider)
        {{
            Query = provider.GetService(typeof(AppGqlQuery)) is AppGqlQuery query 
                ? query 
                : throw new InvalidOperationException();

            //Mutation = (AppGqlMutation)provider.GetService(typeof(AppGqlMutation)) ?? throw new InvalidOperationException();

            FieldMiddleware.Use(new InstrumentFieldsMiddleware());
        }}
    }}
}}";
            var fileName = $"AppGqlSchema.cs";
            var path = Path.Combine(basePath, fileName);
            AppFunctions.WriteFile(path, code);
        }

        private void BuildAppGqlValidationRule(string basePath)
        {
            var ns = settings.ServerProject!.Namespace;
            var code = $@"// # {Constants.NO_NOT_EDIT_MESSAGE}
using Iyu.Server.OData.Gql;

namespace {ns}.Gql
{{
    public partial class AppGqlValidationRule : GqlValidationRule
    {{
    }}
}}";
            var fileName = $"AppGqlValidationRule.cs";
            var path = Path.Combine(basePath, fileName);
            AppFunctions.WriteFile(path, code);
        }

        private void BuildSchemaFiles(string basePath, TableMeta table)
        {
            var path = System.IO.Path.Combine(basePath, $"Gql{table.Name}");
            Utils.ResetDirectory(path);

            BuildSchemaFieldType(path, table);
            BuildSchemaGraphType(path, table);
            BuildSchemaQuery(path, table);
            BuildSchemaRepository(path, table);
        }

        private void BuildSchemaFieldType(string basePath, TableMeta table)
        {
            var modelNS = settings.ModelProject.Namespace;
            var serverNS = settings.ServerProject!.Namespace;
            var entityName = table.Name;
            var entityNames = table.Name.ToPlural();

            var sbPropertyLines = new StringBuilder();
            foreach (var column in table.FullColumns)
            {
                var sysType = column.GetSystemType();
                if ((sysType == typeof(Guid) || sysType == typeof(string)) && TryGetGraphType(column, out var gType))
                {
                    var line = $@"
                new QueryArgument<ListGraphType<{gType}>>
                {{
                    Name = nameof({entityNames}SearchRequest.{column.Name.ToPlural()}),
                }},";

                    sbPropertyLines.Append(line);
                }
            }


            var code = $@"// # {Constants.NO_NOT_EDIT_MESSAGE}
using GraphQL.Types;
using Iyu;
using {modelNS}.Entity;
using {modelNS}.Gql;

namespace {serverNS}.Gql.Schemas
{{
    public class {entityName}FieldType : FieldType
    {{
        public {entityName}FieldType()
        {{
            this.Name = nameof({entityName});

            this.Type = typeof({entityName}GraphType);

            this.Arguments = new QueryArguments(new List<QueryArgument>
            {{
                new QueryArgument<NonNullGraphType<IdGraphType>>
                {{
                    Name = nameof({entityName}._key)
                }}
            }});

            this.Resolver = IoC.Resolve<{entityName}Repository>().GetResolverFindOne();
        }}
    }}

    public class {entityNames}FieldType : FieldType
    {{
        public {entityNames}FieldType()
        {{
            this.Name = ""{entityNames}"";

            this.Type = typeof(ListGraphType<{entityName}GraphType>);

            this.Arguments = new QueryArguments(new List<QueryArgument>
            {{{sbPropertyLines}
                new QueryArgument<IntGraphType>
                {{
                    Name = nameof({entityNames}SearchRequest.Page)
                }},
                new QueryArgument<IntGraphType>
                {{
                    Name = nameof({entityNames}SearchRequest.PageSize)
                }},
            }});

            this.Resolver = IoC.Resolve<{entityName}Repository>().GetResolverFind();
        }}
    }}
}}";
            var fileName = $"{table.Name}FieldType.cs";
            var path = Path.Combine(basePath, fileName);
            AppFunctions.WriteFile(path, code);
        }

        private static bool TryGetGraphType(ColumnMeta column, out string graphType)
        {
            if (column.PK)
            {
                graphType = "IdGraphType";
                return true;
            }
            else if (mapGraphTypes.TryGetValue(column.GetSystemType(), out graphType))
            {
                return true;
            }
            else
                return false;
        }

        private void BuildSchemaGraphType(string basePath, TableMeta table)
        {
            var modelNS = settings.ModelProject.Namespace;
            var serverNS = settings.ServerProject!.Namespace;
            var entityName = table.Name;

            var fieldLines = new List<string>();
            foreach (var column in table.FullColumns)
            {
                var cName = column.Name;
                if (TryGetGraphType(column, out var gTypeName))
                {
                    string line;
                    if (column.IsNotNull())
                    {
                        line = $"AddField(new FieldType() {{ Name = nameof({entityName}.{cName}), Type = typeof(NonNullGraphType<{gTypeName}>) }});";
                    }
                    else
                    {
                        line = $"AddField(new FieldType() {{ Name = nameof({entityName}.{cName}), Type = typeof({gTypeName}) }});";
                    }
                    fieldLines.Add(line);
                }
            }

            foreach (var child in table.GetChildren())
            {
                var name = child.Name.ToPlural();
                var line = $"AddField(new {name}FieldType() {{ Resolver = repository.GetResolver{name}() }});";
                fieldLines.Add(line);
            }

            var fieldLinesText = string.Join($"{Constants.NewLine}\t\t\t", fieldLines);

            var code = $@"// # {Constants.NO_NOT_EDIT_MESSAGE}
using GraphQL.Types;
using {modelNS}.Entity;

namespace {serverNS}.Gql.Schemas
{{
    public class {entityName}GraphType: ObjectGraphType<{entityName}>
    {{
        public {entityName}GraphType({entityName}Repository repository)
        {{
            Name = nameof({entityName});

            {fieldLinesText}
        }}
    }}
}}";
            var fileName = $"{table.Name}GraphType.cs";
            var path = Path.Combine(basePath, fileName);
            AppFunctions.WriteFile(path, code);
        }

        private void BuildSchemaQuery(string basePath, TableMeta table)
        {
            //var modelNS = settings.ModelProject.Namespace;
            var serverNS = settings.ServerProject!.Namespace;
            var entityName = table.Name;
            var entityNames = table.Name.ToPlural();

            var code = $@"// # {Constants.NO_NOT_EDIT_MESSAGE}
using GraphQL.Types;
using Iyu.Server.OData.Services;

namespace {serverNS}.Gql.Schemas
{{
    public class {entityName}Query : IGqlQuery
    {{
        public void DefineQuery(IObjectGraphType query)
        {{
            query.AddField(new {entityName}FieldType());
            query.AddField(new {entityNames}FieldType());
        }}
    }}
}}";
            var fileName = $"{table.Name}Query.cs";
            var path = Path.Combine(basePath, fileName);
            AppFunctions.WriteFile(path, code);
        }

        private void BuildSchemaRepository(string basePath, TableMeta table)
        {
            var modelNS = settings.ModelProject.Namespace;
            var serverNS = settings.ServerProject!.Namespace;
            var entityName = table.Name;
            var entityNames = table.Name.ToPlural();

            var childResolverLinesSB = new StringBuilder();
            foreach (var child in table.GetChildren())
            {
                var childName = child.Name;
                var childNames = child.Name.ToPlural();
                var line = $@"
        internal IFieldResolver GetResolver{childNames}()
        {{
            var r = new FuncFieldResolver<{entityName}, IEnumerable<{childName}>> (context =>
            {{
                var request = new {childNames}SearchRequest()
                {{
                    //{entityName}_keys = new Guid[] {{ context.Source._key }},
                    Columns = context.GetSelectColumns(),
                    Page = context.HasArgument(""Page"") ? context.GetArgument<int>(""Page"") : null,
                    PageSize = context.HasArgument(""PageSize"") ? context.GetArgument<int>(""PageSize"") : null,
                }};
                var query = resolver.FindAsync(request);
                return new ValueTask<IEnumerable<{childName}>>(query)!;
            }});

            return r;
        }}";
                childResolverLinesSB.AppendLine(line);
            }

            var code = $@"// # {Constants.NO_NOT_EDIT_MESSAGE}
using GraphQL;
using GraphQL.Resolvers;
using Iyu.Server.OData.Services;
using {modelNS}.Entity;
using {modelNS}.Gql;
using SqlKata.Execution;

namespace {serverNS}.Gql.Schemas
{{
    public class {entityName}Repository: IGqlRepository
    {{
        private readonly IGqlResolver resolver;
        
        public {entityName}Repository(IGqlResolver resolver)
        {{
            this.resolver = resolver;

            resolver.AddHandler(typeof({entityName}SearchRequest), this);
            resolver.AddHandler(typeof({entityNames}SearchRequest), this);
        }}

        public Task<{entityName}?> FindOneAsync({entityName}SearchRequest request)
        {{
            var query = resolver.ResolveQueryFactory()
                .Query(nameof({entityName}))
                .When(request.Columns.AnyItem(), q => q.Select(request.Columns))
                .Where(q => q.Where(""_key"", request._key))
                .FirstOrDefaultAsync<{entityName}?>();

            return query;
        }}

        public Task<IEnumerable<{entityName}>> FindAsync({entityNames}SearchRequest request)
        {{
            var query = resolver.ResolveQueryFactory()
                .Query(nameof({entityName}))
                .When(request.Columns != null && request.Columns.AnyItem(), q => q.Select(request.Columns))
                .When(request._keys != null && request._keys.AnyItem(), q => q.WhereIn(nameof({entityName}._key), request._keys))
                .ForPage(request.Page ?? request.DefaultPage, request.PageSize ?? request.DefaultPageSize)
                .GetAsync<{entityName}>();

            return query;
        }}

        internal IFieldResolver? GetResolverFindOne()
        {{
            var r = new FuncFieldResolver<{entityName}, {entityName}>(context =>
            {{
                var request = new {entityName}SearchRequest()
                {{
                    _key = context.GetArgument<Guid>(nameof({entityName}SearchRequest._key)),
                    Columns = context.GetSelectColumns()
                }};
                var query = resolver.FindOneAsync(request);
                return new ValueTask<{entityName}?>(query);
            }});

            return r;
        }}

        internal IFieldResolver? GetResolverFind()
        {{
            var r = new FuncFieldResolver<{entityName}, IEnumerable<{entityName}>>(context =>
            {{
                var request = new {entityNames}SearchRequest()
                {{
                    _keys = context.GetArgument<Guid[]?>(nameof({entityNames}SearchRequest._keys)),
                    Columns = context.GetSelectColumns(),
                    Page = context.HasArgument(""Page"") ? context.GetArgument<int>(""Page"") : null,
                    PageSize = context.HasArgument(""PageSize"") ? context.GetArgument<int>(""PageSize"") : null,
                }};
                var query = resolver.FindAsync(request);
                return new ValueTask<IEnumerable<{entityName}>>(query)!;
            }});

            return r;
        }}
        {childResolverLinesSB}
    }}
}}";
            var fileName = $"{table.Name}Repository.cs";
            var path = Path.Combine(basePath, fileName);
            AppFunctions.WriteFile(path, code);
        }
    }
}
