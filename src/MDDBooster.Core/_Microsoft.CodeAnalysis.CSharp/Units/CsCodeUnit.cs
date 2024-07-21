using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis;
using System.Diagnostics;
using MDDBooster;

namespace Microsoft.CodeAnalysis.CSharp.Units
{
    public class CsCodeUnit
    {
        public CsCodeUnit(string code)
        {
            SyntaxTree tree = CSharpSyntaxTree.ParseText(code);
            var root = tree.GetCompilationUnitRoot();

            Parse(root, root.Members);
        }

        private void Parse(CSharpSyntaxNode _, SyntaxList<MemberDeclarationSyntax> members)
        {
            foreach (var member in members)
            {
                if (member is NamespaceDeclarationSyntax @namespace)
                {
                    Parse(@namespace, @namespace.Members);
                }
                else if (member is EnumDeclarationSyntax @enum)
                {
                    var enumUnit = new EnumUnit
                    {
                        Name = @enum.Identifier.ValueText,
                        Values = @enum.Members.Select(m => new EnumValueUnit { Name = m.Identifier.ValueText })
                    };
                    EnumUnits.Add(enumUnit);
                }
                else if (member is ClassDeclarationSyntax @class)
                {
                    var classUnit = new ClassUnit
                    {
                        Name = @class.Identifier.ValueText,
                        IsAbstract = @class.IsAbstract(),
                        Inherits = @class.GetIngeritNames(),
                        IsGeneric = @class.IsGeneric(),
                        GenericTypeNames = @class.GetGenericTypeNames(),
                        Properties = @class.Members.OfType<PropertyDeclarationSyntax>().Select(p =>
                            BuildProperty(p, p.ToString().Contains("required")))
                    };
                    if (classUnit.IsAbstract)
                    {
                        AbstractUnits.Add(classUnit);
                    }
                    else
                    {
                        ClassUnits.Add(classUnit);
                    }
                }
                else if (member is InterfaceDeclarationSyntax @interface)
                {
                    var interfaceUnit = new InterfaceUnit
                    {
                        Name = @interface.Identifier.ValueText,
                        Inherits = @interface.BaseList?.Types.Select(t => t.ToString()) ?? Enumerable.Empty<string>(),
                        Properties = @interface.Members.OfType<PropertyDeclarationSyntax>().Select(p =>
                            BuildProperty(p, p.ToString().Contains("required")))
                    };
                    InterfaceUnits.Add(interfaceUnit);
                }
                else if (member is FileScopedNamespaceDeclarationSyntax @fileScopedNamespace)
                {
                    Parse(@fileScopedNamespace, @fileScopedNamespace.Members);
                }
                else if (member is RecordDeclarationSyntax @record)
                {
                    var classUnit = new ClassUnit
                    {
                        Name = @record.Identifier.ValueText,
                        Properties = @record.ParameterList!.Parameters.OfType<ParameterSyntax>().Select(p =>
                        {
                            var property = SyntaxFactory.PropertyDeclaration(
                                p.Type!,
                                p.Identifier)
                                .WithAttributeLists(p.AttributeLists)
                                .WithModifiers(p.Modifiers);

                            return BuildProperty(property, p.ToString().Contains("required"));
                        })
                    };
                    if (classUnit.IsAbstract)
                    {
                        AbstractUnits.Add(classUnit);
                    }
                    else
                    {
                        ClassUnits.Add(classUnit);
                    }
                }
                else
                {
                    var name = member.GetType().FullName;
                    Debug.WriteLine(name);
                    Debugger.Break();
                }
            }
        }

        private PropertyUnit BuildProperty(PropertyDeclarationSyntax property, bool isRequired)
        {
            if (property.Type is IdentifierNameSyntax)
            {
                var enumName = property.Type.ToString();
                var enumUnit = EnumUnits.FirstOrDefault(p => p.Name == enumName);
                if (enumUnit == null)
                {
                    return new PropertyUnit
                    {
                        Name = property.Identifier.ValueText,
                        Type = property.Type.ToString().Replace("?", ""),
                        IsRequired = isRequired,
                        IsNullable = property.Type is NullableTypeSyntax,
                        Attributes = GetAttributes(property)
                    };
                }
                else
                {
                    return new PropertyUnit
                    {
                        Name = property.Identifier.ValueText,
                        Type = enumName,
                        IsRequired = isRequired,
                        IsNullable = property.Type is NullableTypeSyntax,
                        Attributes = GetAttributes(property)
                    };
                }
            }
            else
            {
                return new PropertyUnit
                {
                    Name = property.Identifier.ValueText,
                    Type = property.Type.ToString().Replace("?", ""),
                    IsRequired = isRequired,
                    IsNullable = property.Type is NullableTypeSyntax,
                    Attributes = GetAttributes(property)
                };
            }
        }

        private List<AttributeUnit> GetAttributes(PropertyDeclarationSyntax property)
        {
            return property.AttributeLists
                .SelectMany(al => al.Attributes)
                .Select(a => new AttributeUnit
                {
                    Name = a.Name.ToString(),
                    Arguments = a.ArgumentList?.Arguments
                        .ToDictionary(
                            arg => arg.NameEquals?.Name.Identifier.Text ?? "",
                            arg => arg.Expression.ToString()
                        ) ?? new Dictionary<string, string>()
                })
                .ToList();
        }

        public List<EnumUnit> EnumUnits { get; } = [];
        public List<InterfaceUnit> InterfaceUnits { get; } = [];
        public List<ClassUnit> AbstractUnits { get; } = [];
        public List<ClassUnit> ClassUnits { get; } = [];
    }

    public class EnumUnit
    {
        public required string Name { get; set; }
        public required IEnumerable<EnumValueUnit> Values { get; set; }

        public IEnumerable<string> GetValueNames()
        {
            return Values.Select(p => p.Name);
        }
    }

    public class EnumValueUnit
    {
        public required string Name { get; set; }
    }

    public interface IClassTypeUnit
    {
        string Name { get; set; }
        IEnumerable<PropertyUnit> Properties { get; set; }
        IEnumerable<string> Inherits { get; set; }
        IEnumerable<string> GetInterfaces();
        IEnumerable<string> GetExtends();
        IEnumerable<string> GetPropertyNames();
    }

    public abstract class ClassTypeUnitBase : IClassTypeUnit
    {
        public required string Name { get; set; }
        public required IEnumerable<PropertyUnit> Properties { get; set; }
        public IEnumerable<string> Inherits { get; set; } = [];

        public IEnumerable<string> GetExtends()
        {
            return Inherits.Where(p => !Utils.IsInterfaceName(p));
        }

        public IEnumerable<string> GetInterfaces()
        {
            return Inherits.Where(Utils.IsInterfaceName);
        }

        public IEnumerable<string> GetPropertyNames()
        {
            return Properties.Select(p => p.Name);
        }
    }

    public class InterfaceUnit : ClassTypeUnitBase
    {
    }

    public class ClassUnit : ClassTypeUnitBase
    {
        public bool IsAbstract { get; set; }
        public bool IsGeneric { get; set; }
        public IEnumerable<string> GenericTypeNames { get; set; } = [];
    }

    public class PropertyUnit
    {
        public required string Name { get; set; }
        public required string Type { get; set; }
        public bool IsRequired { get; set; }
        public bool IsNullable { get; set; }
        public List<AttributeUnit> Attributes { get; set; } = new List<AttributeUnit>();

        public bool IsEnumerable => Type.Contains("IEnumerable<");
        public bool IsGeneric => Type.Contains('<');
        public string? GenericType => IsGeneric ? Type.Split('<')[1].Split('>')[0] : null;

        public string GetLabel()
        {
            if (Attributes.FirstOrDefault(p => p.Name == "Display") is AttributeUnit displayAttr)
            {
                var name = displayAttr.Arguments["Name"];
                // 양끝 따옴표를 제거합니다.
                return name.StartsWith('"') && name.EndsWith('"') ? name[1..^1] : name;
            }
            else
            {
                return Name;
            }
        }

        public string GetColumnTypeName()
        {
            if (Attributes.FirstOrDefault(p => p.Name == "Column") is AttributeUnit columnAttr)
            {
                return columnAttr.Arguments["TypeName"];
            }
            else
            {
                return Type;
            }
        }
    }

    public class AttributeUnit
    {
        public required string Name { get; set; }
        public Dictionary<string, string> Arguments { get; set; } = new Dictionary<string, string>();
    }
}