namespace Gu.Analyzers.Test.Helpers
{
    using System.Linq;
    using System.Threading;
    using Microsoft.CodeAnalysis.CSharp;
    using NUnit.Framework;

    internal partial class AssignedValueWalkerTests
    {
        internal class Constructors
        {
            [TestCase("var temp1 = this.value;", "1")]
            [TestCase("var temp2 = this.value;", "1, 2")]
            [TestCase("var temp3 = this.value;", "1, 2")]
            [TestCase("var temp4 = this.value;", "1, 2, 3")]
            [TestCase("var temp5 = this.value;", "1, 2, 3, 4")]
            [TestCase("var temp6 = this.value;", "1, 2, 3, 4, 5, arg")]
            [TestCase("var temp7 = this.value;", "1, 2, 3, 4, 5, arg")]
            [TestCase("var temp8 = this.value;", "1, 2, 3, 4, 5, arg")]
            public void FieldChainedCtor(string code, string expected)
            {
                var syntaxTree = CSharpSyntaxTree.ParseText(@"
internal class Foo
{
    public int value = 1;

    internal Foo()
    {
        var temp1 = this.value;
        this.value = 2;
        var temp2 = this.value;
    }

    internal Foo(string text)
        : this()
    {
        var temp3 = this.value;
        this.value = 3;
        var temp4 = this.value;
        this.value = 4;
        var temp5 = this.value;
    }

    internal void Bar(int arg)
    {
        var temp6 = this.value;
        this.value = 5;
        var temp7 = this.value;
        this.value = arg;
        var temp8 = this.value;
    }
}");
                var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.All);
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var value = syntaxTree.EqualsValueClause(code).Value;
                using (var pooled = AssignedValueWalker.Create(value, semanticModel, CancellationToken.None))
                {
                    var actual = string.Join(", ", pooled.Item.Select(x => x.Value));
                    Assert.AreEqual(expected, actual);
                }
            }

            [TestCase("var temp1 = this.value;", "1")]
            [TestCase("var temp2 = this.value;", "1, 2")]
            [TestCase("var temp3 = this.value;", "1, 2")]
            [TestCase("var temp4 = this.value;", "1, 2, 3")]
            [TestCase("var temp5 = this.value;", "1, 2, 3, 4")]
            [TestCase("var temp6 = this.value;", "1, 2, 3, 4, 5, arg")]
            [TestCase("var temp7 = this.value;", "1, 2, 3, 4, 5, arg")]
            [TestCase("var temp8 = this.value;", "1, 2, 3, 4, 5, arg")]
            public void FieldChainedCtorGenericClass(string code, string expected)
            {
                var syntaxTree = CSharpSyntaxTree.ParseText(@"
internal class Foo<T>
{
    public int value = 1;

    internal Foo()
    {
        var temp1 = this.value;
        this.value = 2;
        var temp2 = this.value;
    }

    internal Foo(string text)
        : this()
    {
        var temp3 = this.value;
        this.value = 3;
        var temp4 = this.value;
        this.value = 4;
        var temp5 = this.value;
    }

    internal void Bar(int arg)
    {
        var temp6 = this.value;
        this.value = 5;
        var temp7 = this.value;
        this.value = arg;
        var temp8 = this.value;
    }
}");
                var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.All);
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var value = syntaxTree.EqualsValueClause(code).Value;
                using (var pooled = AssignedValueWalker.Create(value, semanticModel, CancellationToken.None))
                {
                    var actual = string.Join(", ", pooled.Item.Select(x => x.Value));
                    Assert.AreEqual(expected, actual);
                }
            }

            [TestCase("var temp1 = this.value;", "1")]
            [TestCase("var temp2 = this.value;", "1, initArg, 3, initArg, 5, arg")]
            [TestCase("var temp3 = this.value;", "1, initArg, 3, initArg, 5, arg")]
            [TestCase("var temp4 = this.value;", "1, initArg")]
            [TestCase("var temp5 = this.value;", "1, initArg, 3")]
            [TestCase("var temp6 = this.value;", "1, initArg, 3, initArg")]
            [TestCase("var temp7 = this.value;", "1, initArg, 3, initArg, 5, arg")]
            [TestCase("var temp8 = this.value;", "1, initArg, 3, initArg, 5, arg")]
            [TestCase("var temp9 = this.value;", "1, initArg, 3, initArg, 5, arg")]
            public void FieldCtorCallingInitializeMethod(string code, string expected)
            {
                var syntaxTree = CSharpSyntaxTree.ParseText(@"
internal class Foo
{
    public int value = 1;

    internal Foo()
    {
        var temp1 = this.value;
        this.Initialize(2);
        var temp4 = this.value;
        this.value = 3;
        var temp5 = this.value;
        this.Initialize(4);
        var temp6 = this.value;
    }

    internal void Bar(int arg)
    {
        var temp7 = this.value;
        this.value = 5;
        var temp8 = this.value;
        this.value = arg;
        var temp9 = this.value;
    }

    private void Initialize(int initArg)
    {
        var temp2 = this.value;
        this.value = initArg;
        var temp3 = this.value;
    }
}");
                var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.All);
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var value = syntaxTree.EqualsValueClause(code).Value;
                using (var pooled = AssignedValueWalker.Create(value, semanticModel, CancellationToken.None))
                {
                    var actual = string.Join(", ", pooled.Item.Select(x => x.Value));
                    Assert.AreEqual(expected, actual);
                }
            }

            [TestCase("var temp1 = this.Value;", "1")]
            [TestCase("var temp2 = this.Value;", "1, 2")]
            [TestCase("var temp3 = this.Value;", "1, 2")]
            [TestCase("var temp4 = this.Value;", "1, 2, 3")]
            [TestCase("var temp5 = this.Value;", "1, 2, 3, 4")]
            [TestCase("var temp6 = this.Value;", "1, 2, 3, 4, 5, arg")]
            [TestCase("var temp7 = this.Value;", "1, 2, 3, 4, 5, arg")]
            [TestCase("var temp8 = this.Value;", "1, 2, 3, 4, 5, arg")]
            [TestCase("var temp9 = this.Value;", "1, 2, 3, 4, 5, arg")]
            [TestCase("var temp10 = this.Value;", "1, 2, 3, 4, 5, arg")]
            public void AutoPropertyChainedCtor(string code, string expected)
            {
                var syntaxTree = CSharpSyntaxTree.ParseText(@"
internal class Foo
{
    internal Foo()
    {
        var temp1 = this.Value;
        this.Value = 2;
        var temp2 = this.Value;
    }

    internal Foo(string text)
        : this()
    {
        var temp3 = this.Value;
        this.Value = 3;
        var temp4 = this.Value;
        this.Value = 4;
        var temp5 = this.Value;
        this.Bar(5);
        var temp6 = this.Value;
        this.Bar(6);
        var temp7 = this.Value;
    }

    public int Value { get; set; } = 1;

    internal void Bar(int arg)
    {
        var temp8 = this.Value;
        this.Value = 7;
        var temp9 = this.Value;
        this.Value = arg;
        var temp10 = this.Value;
    }
}");
                var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.All);
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var value = syntaxTree.EqualsValueClause(code).Value;
                using (var pooled = AssignedValueWalker.Create(value, semanticModel, CancellationToken.None))
                {
                    var actual = string.Join(", ", pooled.Item.Select(x => x.Value));
                    Assert.AreEqual(expected, actual);
                }
            }

            [TestCase("var temp1 = this.value;", "1")]
            [TestCase("var temp2 = this.temp1;", "this.value")]
            [TestCase("var temp3 = this.value;", "1, 2")]
            [TestCase("var temp4 = this.temp1;", "this.value")]
            [TestCase("var temp5 = this.value;", "1, 2")]
            [TestCase("var temp6 = this.temp1;", "this.value")]
            public void FieldInitializedlWithLiteralAndAssignedInCtor(string code, string expected)
            {
                var syntaxTree = CSharpSyntaxTree.ParseText(@"
internal class Foo
{
    private readonly int value = 1;
    private readonly int temp1 = this.value;

    internal Foo()
    {
        var temp1 = this.value;
        var temp2 = this.temp1;
        this.value = 2;
        var temp3 = this.value;
        var temp4 = this.temp1;
    }

    internal void Bar()
    {
        var temp5 = this.value;
        var temp6 = this.temp1;
    }
}");
                var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.All);
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var value = syntaxTree.EqualsValueClause(code).Value;
                using (var pooled = AssignedValueWalker.Create(value, semanticModel, CancellationToken.None))
                {
                    var actual = string.Join(", ", pooled.Item.Select(x => x.Value));
                    Assert.AreEqual(expected, actual);
                }
            }

            [TestCase("var temp1 = this.value;", "1")]
            [TestCase("var temp2 = this.value;", "1, 2")]
            [TestCase("var temp3 = this.value;", "1, 2, 3")]
            [TestCase("var temp4 = this.value;", "1, 2, 3")]
            public void FieldAssignedWithOutParameter(string code, string expected)
            {
                var syntaxTree = CSharpSyntaxTree.ParseText(@"
namespace RoslynSandBox
{
    internal class Foo
    {
        private int value = 1;

        public Foo()
        {
            var temp1 = this.value;
            this.Assign(out this.value, 2);
            var temp2 = this.value;
        }

        internal void Bar()
        {
            var temp3 = this.value;
            this.Assign(out this.value, 3);
            var temp4 = this.value;
        }

        private void Assign(out int outValue, int arg)
        {
            outValue = arg;
        }
    }
}");
                var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.All);
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var value = syntaxTree.EqualsValueClause(code).Value;
                using (var pooled = AssignedValueWalker.Create(value, semanticModel, CancellationToken.None))
                {
                    var actual = string.Join(", ", pooled.Item.Select(x => x.Value));
                    Assert.AreEqual(expected, actual);
                }
            }

            [TestCase("var temp1 = this.value;", "1")]
            [TestCase("var temp2 = this.value;", "1, this.Assign(ref this.value)")]
            [TestCase("var temp3 = this.value;", "1, this.Assign(ref this.value), this.Assign(ref this.value)")]
            [TestCase("var temp4 = this.value;", "1, this.Assign(ref this.value), this.Assign(ref this.value)")]
            public void FieldAssignedWithRefParameter(string code, string expected)
            {
                var syntaxTree = CSharpSyntaxTree.ParseText(@"
namespace RoslynSandBox
{
    internal class Foo
    {
        private int value = 1;

        public Foo()
        {
            var temp1 = this.value;
            this.Assign(ref this.value);
            var temp2 = this.value;
        }

        internal void Bar()
        {
            var temp3 = this.value;
            this.Assign(ref this.value);
            var temp4 = this.value;
        }

        private void Assign(ref int refValue)
        {
            refValue = 2;
        }
    }
}");
                var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.All);
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var value = syntaxTree.EqualsValueClause(code).Value;
                using (var pooled = AssignedValueWalker.Create(value, semanticModel, CancellationToken.None))
                {
                    var actual = string.Join(", ", pooled.Item.Select(x => x.Value));
                    Assert.AreEqual(expected, actual);
                }
            }

            [TestCase("var temp1 = this.Value;", "1, 2, 3")]
            [TestCase("var temp2 = this.Value;", "1, 2, 3, 4")]
            public void InitializedInChainedWithLiteralGeneric(string code, string expected)
            {
                var syntaxTree = CSharpSyntaxTree.ParseText(@"
internal class Foo<T>
{
    internal Foo()
    {
        this.Value = 2;
    }

    internal Foo(string text)
        : this()
    {
        this.Value = 3;
        var temp1 = this.Value;
        this.Value = 4;
    }

    public int Value { get; set; } = 1;

    internal void Bar()
    {
        var temp2 = this.Value;
    }
}");
                var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.All);
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var value = syntaxTree.EqualsValueClause(code).Value;
                using (var pooled = AssignedValueWalker.Create(value, semanticModel, CancellationToken.None))
                {
                    var actual = string.Join(", ", pooled.Item.Select(x => x.Value));
                    Assert.AreEqual(expected, actual);
                }
            }

            [TestCase("var temp1 = this.value;", "1")]
            [TestCase("var temp2 = this.value;", "1, 2")]
            [TestCase("var temp3 = this.value;", "1, 2, 3, arg")]
            [TestCase("var temp4 = this.value;", "1, 2, 3, arg")]
            [TestCase("var temp4 = this.value;", "1, 2, 3, arg")]
            public void FieldImplicitBase(string code, object expected)
            {
                var syntaxTree = CSharpSyntaxTree.ParseText(@"
internal class FooBase
{
    protected readonly int value = 1;

    internal FooBase()
    {
        var temp1 = this.value;
        this.value = 2;
        var temp2 = this.value;
    }
}

internal class Foo : FooBase
{
    internal void Bar(int arg)
    {
        var temp3 = this.value;
        this.value = 3;
        var temp4 = this.value;
        this.value = arg;
        var temp5 = this.value;
    }
}");
                var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.All);
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var value = syntaxTree.EqualsValueClause(code).Value;
                using (var pooled = AssignedValueWalker.Create(value, semanticModel, CancellationToken.None))
                {
                    var actual = string.Join(", ", pooled.Item.Select(x => x.Value));
                    Assert.AreEqual(expected, actual);
                }
            }

            [TestCase("var temp1 = this.value;", "1")]
            [TestCase("var temp2 = this.value;", "1, 2")]
            [TestCase("var temp3 = this.value;", "1, 2")]
            [TestCase("var temp4 = this.value;", "1, 2, 3")]
            [TestCase("var temp5 = this.value;", "1, 2, 3, 4")]
            [TestCase("var temp6 = this.value;", "1, 2, 3, 4, 5, arg")]
            [TestCase("var temp7 = this.value;", "1, 2, 3, 4, 5, arg")]
            [TestCase("var temp7 = this.value;", "1, 2, 3, 4, 5, arg")]
            public void FieldImplicitBaseWhenSubclassHasCtor(string code, object expected)
            {
                var syntaxTree = CSharpSyntaxTree.ParseText(@"
internal class FooBase
{
    protected readonly int value = 1;

    internal FooBase()
    {
        var temp1 = this.value;
        this.value = 2;
        var temp2 = this.value;
    }
}

internal class Foo : FooBase
{
    internal Foo()
    {
        var temp3 = this.value;
        this.value = 3;
        var temp4 = this.value;
        this.value = 4;
        var temp5 = this.value;
    }

    internal void Bar(int arg)
    {
        var temp6 = this.value;
        this.value = 5;
        var temp7 = this.value;
        this.value = arg;
        var temp8 = this.value;
    }
}");
                var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.All);
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var value = syntaxTree.EqualsValueClause(code).Value;
                using (var pooled = AssignedValueWalker.Create(value, semanticModel, CancellationToken.None))
                {
                    var actual = string.Join(", ", pooled.Item.Select(x => x.Value));
                    Assert.AreEqual(expected, actual);
                }
            }

            [TestCase("var temp1 = this.value;", "1, 2, 3")]
            [TestCase("var temp2 = this.value;", "1, 2, 3, 4")]
            public void InitializedInBaseCtorWithLiteral(string code, string expected)
            {
                var syntaxTree = CSharpSyntaxTree.ParseText(@"
internal class FooBase
{
    protected readonly int value = 1;
    
    internal FooBase()
    {
        this.value = 2;
    }

    internal FooBase(int value)
    {
        this.value = value;
    }
}

internal class Foo : FooBase
{
    internal Foo()
    {
        this.value = 3;
        var temp1 = this.value;
        this.value = 4;
    }

    internal void Bar()
    {
        var temp2 = this.value;
    }
}");
                var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.All);
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var value = syntaxTree.EqualsValueClause(code).Value;
                using (var pooled = AssignedValueWalker.Create(value, semanticModel, CancellationToken.None))
                {
                    var actual = string.Join(", ", pooled.Item.Select(x => x.Value));
                    Assert.AreEqual(expected, actual);
                }
            }

            [TestCase("var temp1 = this.value;", "default(T)")]
            [TestCase("var temp2 = this.value;", "default(T)")]
            public void InitializedInBaseCtorWithDefaultGeneric(string code, string expected)
            {
                var syntaxTree = CSharpSyntaxTree.ParseText(@"
internal class FooBase<T>
{
    protected readonly T value;
    
    internal FooBase()
    {
        this.value = default(T);
    }

    internal FooBase(T value)
    {
        this.value = value;
    }
}

internal class Foo : FooBase<int>
{
    internal Foo()
    {
        var temp1 = this.value;
    }

    internal void Bar()
    {
        var temp2 = this.value;
    }
}");
                var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.All);
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var value = syntaxTree.EqualsValueClause(code).Value;
                using (var pooled = AssignedValueWalker.Create(value, semanticModel, CancellationToken.None))
                {
                    var actual = string.Join(", ", pooled.Item.Select(x => x.Value));
                    Assert.AreEqual(expected, actual);
                }
            }

            [TestCase("var temp1 = this.value;", "default(T)")]
            [TestCase("var temp2 = this.value;", "default(T)")]
            public void InitializedInBaseCtorWithDefaultGenericGeneric(string code, string expected)
            {
                var syntaxTree = CSharpSyntaxTree.ParseText(@"
internal class FooBase<T>
{
    protected readonly T value;
    
    internal FooBase()
    {
        this.value = default(T);
    }

    internal FooBase(T value)
    {
        this.value = value;
    }
}

internal class Foo<T> : FooBase<T>
{
    internal Foo()
    {
        var temp1 = this.value;
    }

    internal void Bar()
    {
        var temp2 = this.value;
    }
}");
                var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.All);
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var value = syntaxTree.EqualsValueClause(code).Value;
                using (var pooled = AssignedValueWalker.Create(value, semanticModel, CancellationToken.None))
                {
                    var actual = string.Join(", ", pooled.Item.Select(x => x.Value));
                    Assert.AreEqual(expected, actual);
                }
            }
        }
    }
}