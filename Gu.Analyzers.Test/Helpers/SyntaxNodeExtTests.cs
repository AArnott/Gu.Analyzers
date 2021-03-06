namespace Gu.Analyzers.Test.Helpers
{
    using Gu.Roslyn.Asserts;
    using Microsoft.CodeAnalysis.CSharp;

    using NUnit.Framework;

    internal class SyntaxNodeExtTests
    {
        internal class IsBeforeInScope
        {
            [TestCase("var temp = 1;", "temp = 2;", Result.Yes)]
            [TestCase("temp = 2;", "var temp = 1;", Result.No)]
            public void SameBlock(string firstStatement, string otherStatement, Result expected)
            {
                var syntaxTree = CSharpSyntaxTree.ParseText(@"
internal class Foo
{
    internal Foo()
    {
        var temp = 1;
        temp = 2;
    }
}");
                var first = syntaxTree.FindStatement(firstStatement);
                var other = syntaxTree.FindStatement(otherStatement);
                Assert.AreEqual(expected, first.IsExecutedBefore(other));
            }

            [TestCase("var temp = 1;", "temp = 2;", Result.Yes)]
            [TestCase("var temp = 1;", "temp = 3;", Result.Yes)]
            [TestCase("temp = 2;", "var temp = 1;", Result.No)]
            [TestCase("temp = 3;", "var temp = 1;", Result.No)]
            [TestCase("temp = 3;", "temp = 2;", Result.No)]
            [TestCase("temp = 2;", "temp = 3;", Result.No)]
            public void InsideIfBlock(string firstStatement, string otherStatement, Result expected)
            {
                var syntaxTree = CSharpSyntaxTree.ParseText(@"
namespace RoslynSandbox
{
    internal class Foo
    {
        internal Foo()
        {
            var temp = 1;
            if (Result.Yes)
            {
                temp = 2;
            }
            else
            {
                temp = 3;
            }
        }
    }
}");
                var first = syntaxTree.FindStatement(firstStatement);
                var other = syntaxTree.FindStatement(otherStatement);
                Assert.AreEqual(expected, first.IsExecutedBefore(other));
            }

            [TestCase("var temp = 1;", "temp = 2;", Result.Yes)]
            [TestCase("var temp = 1;", "temp = 3;", Result.Yes)]
            [TestCase("temp = 2;", "var temp = 1;", Result.No)]
            [TestCase("temp = 3;", "var temp = 1;", Result.No)]
            [TestCase("temp = 3;", "temp = 2;", Result.No)]
            [TestCase("temp = 2;", "temp = 3;", Result.No)]
            public void InsideIfBlockCurlyElse(string firstStatement, string otherStatement, Result expected)
            {
                var syntaxTree = CSharpSyntaxTree.ParseText(@"
namespace RoslynSandbox
{
    internal class Foo
    {
        internal Foo()
        {
            var temp = 1;
            if (true)
                temp = 2;
            else
            {
                temp = 3;
            }
        }
    }
}");
                var first = syntaxTree.FindStatement(firstStatement);
                var other = syntaxTree.FindStatement(otherStatement);
                Assert.AreEqual(expected, first.IsExecutedBefore(other));
            }

            [TestCase("var temp = 1;", "temp = 2;", Result.Yes)]
            [TestCase("var temp = 1;", "temp = 3;", Result.Yes)]
            [TestCase("var temp = 1;", "temp = 4;", Result.Yes)]
            [TestCase("temp = 2;", "temp = 4;", Result.Yes)]
            [TestCase("temp = 3;", "temp = 4;", Result.Yes)]
            [TestCase("temp = 2;", "var temp = 1;", Result.No)]
            [TestCase("temp = 3;", "var temp = 1;", Result.No)]
            [TestCase("temp = 3;", "temp = 2;", Result.No)]
            [TestCase("temp = 2;", "temp = 3;", Result.No)]
            public void InsideIfBlockNoCurlies(string firstStatement, string otherStatement, Result expected)
            {
                var syntaxTree = CSharpSyntaxTree.ParseText(@"
namespace RoslynSandbox
{
    internal class Foo
    {
        internal Foo()
        {
            var temp = 1;
            if (true)
                temp = 2;
            else
                temp = 3;
            temp = 4;
        }
    }
}");
                var first = syntaxTree.FindStatement(firstStatement);
                var other = syntaxTree.FindStatement(otherStatement);
                Assert.AreEqual(expected, first.IsExecutedBefore(other));
            }

            [TestCase("var temp = 1;", "temp = 4;", Result.Yes)]
            [TestCase("temp = 2;", "temp = 4;", Result.Yes)]
            [TestCase("temp = 3;", "temp = 4;", Result.Yes)]
            [TestCase("temp = 4;", "temp = 2;", Result.No)]
            [TestCase("temp = 4;", "temp = 3;", Result.No)]
            public void AfterIfBlock(string firstStatement, string otherStatement, Result expected)
            {
                var syntaxTree = CSharpSyntaxTree.ParseText(@"
namespace RoslynSandbox
{
    internal class Foo
    {
        internal Foo()
        {
            var temp = 1;
            if (Result.Yes)
            {
                temp = 2;
            }
            else
            {
                temp = 3;
            }

            temp = 4;
        }
    }
}");
                var first = syntaxTree.FindStatement(firstStatement);
                var other = syntaxTree.FindStatement(otherStatement);
                Assert.AreEqual(expected, first.IsExecutedBefore(other));
            }

            [Explicit("Not sure here.")]
            [TestCase("a = 1;", "a = 2;", Result.Yes)]
            [TestCase("a = 2;", "a = 3;", Result.Yes)]
            [TestCase("a = 3;", "a = 2;", Result.Yes)]
            public void Lambda(string firstStatement, string otherStatement, Result expected)
            {
                var syntaxTree = CSharpSyntaxTree.ParseText(@"
namespace RoslynSandbox
{
    using System;

    public class Foo
    {
        public Foo()
        {
            var a = 1;
            this.E += (_, __) => a = 3;
            this.E += (_, __) =>
            {
                a = 4;
                a = 5;
            };
            a = 2;
        }

        public event EventHandler E;
    }
}");
                var first = syntaxTree.FindStatement(firstStatement);
                var other = syntaxTree.FindStatement(otherStatement);
                Assert.AreEqual(expected, first.IsExecutedBefore(other));
            }
        }
    }
}
