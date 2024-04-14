using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis;
using System.Diagnostics;

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
            foreach(var member in members)
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
                            new PropertyUnit
                            {
                                Name = p.Identifier.ValueText, 
                                Type = p.Type.ToString().Replace("?", ""),
                                IsRequired = p.ToString().Contains("required"),
                                IsNullable = p.Type is NullableTypeSyntax
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
                else if (member is InterfaceDeclarationSyntax @interface)
                {
                    var interfaceUnit = new InterfaceUnit
                    {
                        Name = @interface.Identifier.ValueText,
                        Properties = @interface.Members.OfType<PropertyDeclarationSyntax>().Select(p => 
                            BuildProperty(
                                type: p.Type, 
                                name: p.Identifier.ValueText, 
                                isRequired: p.ToString().Contains("required")))
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
                            BuildProperty(
                                type: p.Type!,
                                name: p.Identifier.ValueText,
                                isRequired: p.ToString().Contains("required")))
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

        private PropertyUnit BuildProperty(TypeSyntax type, string name, bool isRequired)
        {
            if (type is IdentifierNameSyntax)
            {
                var enumName = type.ToString();
                var enumUnit = EnumUnits.FirstOrDefault(p => p.Name == enumName);
                if (enumUnit == null)
                {
                    var unit = new PropertyUnit
                    {
                        Name = name,
                        Type = "string",
                        IsRequired = isRequired,
                        IsNullable = type is NullableTypeSyntax
                    };
                    return unit;
                }
                else
                {
                    return new PropertyUnit
                    {
                        Name = name,
                        Type = enumName,
                        IsRequired = isRequired,
                        IsNullable = type is NullableTypeSyntax
                    };
                }
            }
            else
            {
                var unit = new PropertyUnit
                {
                    Name = name,
                    Type = type!.ToString().Replace("?", ""),
                    IsRequired = isRequired,
                    IsNullable = type is NullableTypeSyntax
                };
                return unit;
            }
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
        IEnumerable<string> GetPropertyNames();
    }

    public abstract class ClassTypeUnitBase : IClassTypeUnit
    {
        public required string Name { get; set; }
        public required IEnumerable<PropertyUnit> Properties { get; set; }
        public IEnumerable<string> Inherits { get; set; } = [];
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
    }
}
