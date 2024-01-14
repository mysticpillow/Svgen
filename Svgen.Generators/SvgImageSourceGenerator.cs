using System;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace Svgen.Generators;

/// <summary>
/// Generator of lazy initialized SvgImage objects
/// </summary>
/// <remarks>
/// Implementation of the obsolete <see cref="ISourceGenerator"/>
/// </remarks>
[Obsolete(message: "Need to implement IIncrementalGenerator", error: false)]
[Generator]
public class SvgImageSourceGenerator : ISourceGenerator
{
    /// <summary>
    /// Attribute name of classes used for generation
    /// </summary>
    private const string LoadedAttributeFullName = "SvgenDemo.Resources.LoadedAttribute";

    /// <inheritdoc />
    public void Execute(GeneratorExecutionContext context)
    {
        var compilation = context.Compilation;

        var attributeSymbol = context.Compilation.GetTypeByMetadataName(LoadedAttributeFullName);

        if (attributeSymbol == null)
        {
            Debug.WriteLine("Attribute symbol not found");
            return;
        }

        var syntaxTreesWithClassesWithAttributes = context.Compilation.SyntaxTrees.Where(syntaxTree => syntaxTree.GetRoot().DescendantNodes().OfType<ClassDeclarationSyntax>()
            .Any(classDeclarationNode => classDeclarationNode.DescendantNodes().OfType<AttributeSyntax>().Any()));
        foreach (var syntaxTree in syntaxTreesWithClassesWithAttributes)
        {
            var semanticModel = compilation.GetSemanticModel(syntaxTree);
            foreach (var declaredClass in syntaxTree
                         .GetRoot()
                         .DescendantNodes()
                         .OfType<ClassDeclarationSyntax>()
                         .Where(classDeclarationNode => classDeclarationNode.DescendantNodes().OfType<AttributeSyntax>().Any()))
            {
                var hasSpecifiedAttribute = declaredClass.DescendantNodes().OfType<AttributeSyntax>()
                    .Any(attribute => attribute.DescendantTokens()
                        .Any(descendantToken => descendantToken.IsKind(SyntaxKind.IdentifierToken) &&
                                                semanticModel.GetTypeInfo(descendantToken.Parent).Type.Name == attributeSymbol.Name));

                if (!hasSpecifiedAttribute)
                {
                    continue;
                }

                var generatedClass = GenerateClass();
                var propertyDeclarations = declaredClass.DescendantNodes().OfType<PropertyDeclarationSyntax>();
                var propertySymbols = propertyDeclarations
                    .Select(propertyDeclaration => semanticModel.GetDeclaredSymbol(propertyDeclaration))
                    .Where(propertySymbol => propertySymbol.IsStatic && propertySymbol.GetMethod != null && propertySymbol.Kind == SymbolKind.Property);

                var generatedFields = new StringBuilder();
                var generatedProperties = new StringBuilder();
                foreach (var propertySymbol in propertySymbols)
                {
                    GenerateField(propertySymbol, generatedFields);
                    GenerateProperty(propertySymbol, generatedProperties);
                }

                generatedClass.Append(generatedFields);
                generatedClass.Append(generatedProperties);
                CloseClass(generatedClass);

                context.AddSource($"SvgResources.g.cs", SourceText.From(generatedClass.ToString(), Encoding.UTF8));
            }
        }
    }

    /// <inheritdoc />
    public void Initialize(GeneratorInitializationContext context)
    {
        //if (!Debugger.IsAttached)
        //{
        //    Debugger.Launch();
        //}
    }

    /// <summary>
    /// Add closing brace
    /// </summary>
    /// <param name="generatedClass"> Code of generated class </param>
    private void CloseClass(StringBuilder generatedClass)
    {
        generatedClass.Append(@"
}");
    }

    /// <summary>
    /// Generate the beginning of class code
    /// </summary>
    /// <returns> Code of generated class </returns>
    private StringBuilder GenerateClass()
    {
        var generatedClass = new StringBuilder();
        generatedClass.Append(@"
using System;
using Avalonia.Svg.Skia;

namespace SvgenDemo.Resources;

public static partial class SvgResources");
        generatedClass.Append(@"
{");
        return generatedClass;
    }

    /// <summary>
    /// Generate field
    /// </summary>
    /// <param name="property"> Property symbol </param>
    /// <param name="generatedFields"> Code of generated fields </param>
    private void GenerateField(ISymbol property, StringBuilder generatedFields)
    {
        generatedFields.Append($@"
private static SvgImage? _{property.MetadataName}Svg;");
    }

    /// <summary>
    /// Generate property
    /// </summary>
    /// <param name="property"> Property symbol </param>
    /// <param name="generatedProperties"> Code of generated properties </param>
    private void GenerateProperty(ISymbol property, StringBuilder generatedProperties)
    {
        var fieldName = $"_{property.MetadataName}Svg";
        generatedProperties.Append($@"
public static SvgImage {property.MetadataName}Svg
{{
    get
    {{
        {fieldName} ??= new() {{ Source = SvgSource.Load<SvgSource>({property.MetadataName}, new Uri({property.MetadataName})) }};
        return {fieldName};
    }}
}}
");
    }
}