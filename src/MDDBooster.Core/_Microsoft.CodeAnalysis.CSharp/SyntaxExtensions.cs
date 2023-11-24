using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.CodeAnalysis.CSharp
{
    public static class SyntaxExtensions
    {
        public static bool IsAbstract(this ClassDeclarationSyntax @class)
        {
            var modifiers = @class.Modifiers;
            foreach (var modifier in modifiers)
            {
                if (modifier.IsKind(SyntaxKind.AbstractKeyword))
                {
                    return true;
                }
            }
            return false;
        }

        public static IEnumerable<string> GetIngeritNames(this ClassDeclarationSyntax @class)
        {
            var baseList = @class.BaseList;
            if (baseList == null)
            {
                return Enumerable.Empty<string>();
            }
            return baseList.Types.Select(t => t.Type.ToString());
        }

        public static bool IsGeneric(this ClassDeclarationSyntax @class)
        {
            var typeParameters = @class.TypeParameterList;
            if (typeParameters == null)
            {
                return false;
            }
            return typeParameters.Parameters.Count > 0;
        }

        public static IEnumerable<string> GetGenericTypeNames(this ClassDeclarationSyntax @class)
        {
            var typeParameters = @class.TypeParameterList;
            if (typeParameters == null)
            {
                return Enumerable.Empty<string>();
            }
            return typeParameters.Parameters.Select(p => p.Identifier.ValueText);
        }
    }
}
