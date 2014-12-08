using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System;
using System.Collections.Immutable;
using System.Linq;

namespace CodeCracker.Style
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class StringFormatAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "CC0048";
        internal const string Title = "Use string interpolation instead of String.Format";
        internal const string MessageFormat = "Use string interpolation";
        internal const string Category = SupportedCategories.Style;
        const string Description = "String interpolation allows for better reading of the resulting string when compared to String.Format. You should use String.Format only when another method is supplying the format string.";

        internal static DiagnosticDescriptor Rule = new DiagnosticDescriptor(
            DiagnosticId,
            Title,
            MessageFormat,
            Category,
            DiagnosticSeverity.Info,
            isEnabledByDefault: true,
            description: Description,
            helpLink: HelpLink.ForDiagnostic(DiagnosticId));

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(Rule); } }

        public override void Initialize(AnalysisContext context)
        {
            context.RegisterSyntaxNodeAction(Analyzer, SyntaxKind.InvocationExpression);
        }

        private void Analyzer(SyntaxNodeAnalysisContext context)
        {
            var invocationExpression = (InvocationExpressionSyntax)context.Node;
            var memberExpresion = invocationExpression.Expression as MemberAccessExpressionSyntax;
            if (memberExpresion?.Name?.ToString() != "Format") return;
            var memberSymbol = context.SemanticModel.GetSymbolInfo(memberExpresion).Symbol;
            if (!memberSymbol?.ToString()?.StartsWith("string.Format(string, ") ?? false) return;
            var argumentList = invocationExpression.ArgumentList as ArgumentListSyntax;
            if (argumentList?.Arguments.Count < 2) return;
            if (!argumentList.Arguments[0]?.Expression?.IsKind(SyntaxKind.StringLiteralExpression) ?? false) return;
            var formatLiteral = (LiteralExpressionSyntax)argumentList.Arguments[0].Expression;
            var format = (string)context.SemanticModel.GetConstantValue(formatLiteral).Value;
            var formatArgs = Enumerable.Range(1, argumentList.Arguments.Count - 1).Select(i => new object()).ToArray();
            try
            {
                string.Format(format, formatArgs);
            }
            catch (FormatException)
            {
                return;
            }
            var diag = Diagnostic.Create(Rule, invocationExpression.GetLocation());
            context.ReportDiagnostic(diag);
        }
    }
}