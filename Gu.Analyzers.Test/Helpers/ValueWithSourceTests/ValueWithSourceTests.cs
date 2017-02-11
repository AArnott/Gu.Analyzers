﻿namespace Gu.Analyzers.Test.Helpers
{
    using System.Linq;
    using System.Threading;

    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    using NUnit.Framework;

    public partial class ValueWithSourceTests
    {
        [TestCase("1", "1 Constant")]
        [TestCase(@"""1""", @"""1"" Constant")]
        [TestCase("new string('1', 1)", "new string('1', 1) Created")]
        [TestCase("new int[2]", "new int[2] Created")]
        [TestCase("new int[] { 1 , 2 , 3 }", "new int[] { 1 , 2 , 3 } Created")]
        [TestCase("new []{ 1 , 2 , 3 }", "new []{ 1 , 2 , 3 } Created")]
        [TestCase("{ 1 , 2 , 3 }", "{ 1 , 2 , 3 } Created")]
        public void SimpleAssign(string code, string expected)
        {
            var testCode = @"
internal class Foo
{
    internal void Bar()
    {
        var text = 1;
    }
}";
            testCode = testCode.AssertReplace("1", code);
            var syntaxTree = CSharpSyntaxTree.ParseText(testCode);
            var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.All);
            var semanticModel = compilation.GetSemanticModel(syntaxTree);
            var node = syntaxTree.EqualsValueClause($"var text = {code}").Value;
            using (var sources = VauleWithSource.GetRecursiveSources(node, semanticModel, CancellationToken.None))
            {
                var actual = string.Join(", ", sources.Item.Select(x => $"{x.Value} {x.Source}"));
                Assert.AreEqual(expected, actual);
            }
        }

        [TestCase("var temp1 = this.Id(1);", "this.Id(1) Calculated, arg Argument, 1 Constant")]
        [TestCase("var temp2 = this.Id(1);", "this.Id(1) Calculated, arg Argument, 1 Constant")]
        public void IdentityMethod(string code, string expected)
        {
            var testCode = @"
namespace RoslynSandBox
{
    using System;

    public class Foo
    {
        public Foo()
        {
            var temp1 = this.Id(1);
        }

        public int Id(int arg)
        {
            return arg;
        }

        public Bar()
        {
            var temp2 = this.Id(1);
        }
    }
}";
            var syntaxTree = CSharpSyntaxTree.ParseText(testCode);
            var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.All);
            var semanticModel = compilation.GetSemanticModel(syntaxTree);
            var node = syntaxTree.EqualsValueClause(code).Value;
            using (var sources = VauleWithSource.GetRecursiveSources(node, semanticModel, CancellationToken.None))
            {
                var actual = string.Join(", ", sources.Item.Select(x => $"{x.Value} {x.Source}"));
                Assert.AreEqual(expected, actual);
            }
        }

        [Test]
        public void StaticMethodReturningNew()
        {
            var syntaxTree = CSharpSyntaxTree.ParseText(@"
internal class Foo
{
    internal void Bar()
    {
        var text = Create();
    }

    internal static string Create()
    {
        return new string(' ', 1);
    }
}");
            var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.All);
            var semanticModel = compilation.GetSemanticModel(syntaxTree);
            var node = syntaxTree.GetRoot()
                                 .DescendantNodes()
                                 .OfType<EqualsValueClauseSyntax>()
                                 .First()
                                 .Value;
            using (var sources = VauleWithSource.GetRecursiveSources(node, semanticModel, CancellationToken.None))
            {
                var actual = string.Join(", ", sources.Item.Select(x => $"{x.Value} {x.Source}"));
                Assert.AreEqual("Create() Calculated, new string(' ', 1) Created", actual);
            }
        }

        [TestCase("internal static")]
        [TestCase("public")]
        [TestCase("private")]
        public void MethodReturningArg(string modifiers)
        {
            var testCode = @"
internal class Foo
{
    internal void Bar()
    {
        var value = Create(1);
    }

    internal static string Create(int value)
    {
        return value;
    }
}";
            testCode = testCode.AssertReplace("internal static", modifiers);
            var syntaxTree = CSharpSyntaxTree.ParseText(testCode);
            var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.All);
            var semanticModel = compilation.GetSemanticModel(syntaxTree);
            var node = syntaxTree.EqualsValueClause("var value = Create(1);").Value;
            using (var sources = VauleWithSource.GetRecursiveSources(node, semanticModel, CancellationToken.None))
            {
                var actual = string.Join(", ", sources.Item.Select(x => $"{x.Value} {x.Source}"));
                Assert.AreEqual("Create(1) Calculated, value Argument, 1 Constant", actual);
            }
        }

        [TestCase("internal static")]
        [TestCase("public")]
        [TestCase("private")]
        public void MethodChainReturningArg(string modifiers)
        {
            var testCode = @"
internal class Foo
{
    internal void Bar()
    {
        var value = Create1(1);
    }

    internal static string Create1(int value1)
    {
        return Create2(value1);
    }

    internal static string Create2(int value2)
    {
        return value2;
    }
}";
            testCode = testCode.AssertReplace("internal static", modifiers);
            var syntaxTree = CSharpSyntaxTree.ParseText(testCode);
            var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.All);
            var semanticModel = compilation.GetSemanticModel(syntaxTree);
            var node = syntaxTree.EqualsValueClause("var value = Create1(1);").Value;
            using (var sources = VauleWithSource.GetRecursiveSources(node, semanticModel, CancellationToken.None))
            {
                var actual = string.Join(", ", sources.Item.Select(x => $"{x.Value} {x.Source}"));
                Assert.AreEqual("Create1(1) Calculated, Create2(value1) Calculated, value2 Argument, value1 Argument, 1 Constant", actual);
            }
        }

        [Test]
        public void StaticMethodReturningNewInIfElse()
        {
            var syntaxTree = CSharpSyntaxTree.ParseText(@"
    internal class Foo
    {
        internal void Bar()
        {
            var text = Create(true);
        }

        internal static string Create(bool value)
        {
            if (value)
            {
                return new string('1', 1);
            }
            else
            {
                return new string('0', 1);
            }
        }
    }");
            var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.All);
            var semanticModel = compilation.GetSemanticModel(syntaxTree);
            var node = syntaxTree.GetRoot()
                                 .DescendantNodes()
                                 .OfType<EqualsValueClauseSyntax>()
                                 .First()
                                 .Value;
            using (var sources = VauleWithSource.GetRecursiveSources(node, semanticModel, CancellationToken.None))
            {
                var actual = string.Join(", ", sources.Item.Select(x => $"{x.Value} {x.Source}"));
                Assert.AreEqual("Create(true) Calculated, new string('1', 1) Created, new string('0', 1) Created", actual);
            }
        }

        [Test]
        public void StaticMethodReturningNewExpressionBody()
        {
            var syntaxTree = CSharpSyntaxTree.ParseText(@"
using System;
internal class Foo
{
    internal static async Task Bar()
    {
        var stream = Create();
    }

    internal static async IDisposable Create() => new Disposable();
}");
            var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.All);
            var semanticModel = compilation.GetSemanticModel(syntaxTree);
            var node = syntaxTree.GetRoot()
                                 .DescendantNodes()
                                 .OfType<EqualsValueClauseSyntax>()
                                 .First()
                                 .Value;
            using (var sources = VauleWithSource.GetRecursiveSources(node, semanticModel, CancellationToken.None))
            {
                var actual = string.Join(", ", sources.Item.Select(x => $"{x.Value} {x.Source}"));
                Assert.AreEqual("Create() Calculated, new Disposable() Created", actual);
            }
        }

        [Test]
        public void StaticMethodReturningFileOpenRead()
        {
            var syntaxTree = CSharpSyntaxTree.ParseText(@"
using System;
using System.IO;

public static class Foo
{
    public static long Bar()
    {
        var value = GetStream();
    }

    public static Stream GetStream()
    {
        return File.OpenRead(""A"");
    }
}");
            var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.All);
            var semanticModel = compilation.GetSemanticModel(syntaxTree);
            var node = syntaxTree.GetRoot()
                                 .DescendantNodes()
                                 .OfType<EqualsValueClauseSyntax>()
                                 .First()
                                 .Value;
            using (var sources = VauleWithSource.GetRecursiveSources(node, semanticModel, CancellationToken.None))
            {
                var actual = string.Join(", ", sources.Item.Select(x => $"{x.Value} {x.Source}"));
                Assert.AreEqual(@"GetStream() Calculated, File.OpenRead(""A"") External", actual);
            }
        }

        [Test]
        public void AssigningWithStaticField()
        {
            var syntaxTree = CSharpSyntaxTree.ParseText(@"
using System;
internal class Foo
{
    private static readonly int Cache = 1;
    internal void Bar()
    {
        var value = Cache;
    }
}");
            var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.All);
            var semanticModel = compilation.GetSemanticModel(syntaxTree);
            var node = syntaxTree.EqualsValueClause("var value = Cache;").Value;
            using (var sources = VauleWithSource.GetRecursiveSources(node, semanticModel, CancellationToken.None))
            {
                var actual = string.Join(", ", sources.Item.Select(x => $"{x.Value} {x.Source}"));
                Assert.AreEqual("Cache Cached", actual);
            }
        }

        [Test]
        public void AssigningWithStaticFieldIndexer()
        {
            var syntaxTree = CSharpSyntaxTree.ParseText(@"
using System;
internal class Foo
{
    private static readonly int[] Cache = { 1, 2, 3 };
    internal void Bar()
    {
        var value = Cache[1];
    }
}");
            var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.All);
            var semanticModel = compilation.GetSemanticModel(syntaxTree);
            var node = syntaxTree.EqualsValueClause("var value = Cache[1];").Value;
            using (var sources = VauleWithSource.GetRecursiveSources(node, semanticModel, CancellationToken.None))
            {
                var actual = string.Join(", ", sources.Item.Select(x => $"{x.Value} {x.Source}"));
                Assert.AreEqual("Cache[1] Cached", actual);
            }
        }

        [Test]
        public void AssigningWithStaticFieldConcurrentDictionaryGetOrAdd()
        {
            var syntaxTree = CSharpSyntaxTree.ParseText(@"
using System.Collections.Concurrent;
using System.IO;

public static class Foo
{
    private static readonly ConcurrentDictionary<int, Stream> StreamCache = new ConcurrentDictionary<int, Stream>();

    public static void Bar()
    {
        var stream = StreamCache.GetOrAdd(1, _ => File.OpenRead(""A""));
    }
}");
            var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.All);
            var semanticModel = compilation.GetSemanticModel(syntaxTree);
            var node = syntaxTree.EqualsValueClause("var stream = StreamCache.GetOrAdd(1, _ => File.OpenRead(\"A\"));").Value;
            using (var sources = VauleWithSource.GetRecursiveSources(node, semanticModel, CancellationToken.None))
            {
                var actual = string.Join(", ", sources.Item.Select(x => $"{x.Value} {x.Source}"));
                Assert.AreEqual(@"StreamCache.GetOrAdd(1, _ => File.OpenRead(""A"")) External, StreamCache Cached", actual);
            }
        }

        [Test]
        public void AssigningWithStaticFieldConcurrentDictionaryGetOrAddElvis()
        {
            var syntaxTree = CSharpSyntaxTree.ParseText(@"
using System.Collections.Concurrent;
using System.IO;

public static class Foo
{
    private static readonly ConcurrentDictionary<int, Stream> StreamCache = new ConcurrentDictionary<int, Stream>();

    public static void Bar()
    {
        var stream = StreamCache?.GetOrAdd(1, _ => File.OpenRead(""A""));
    }
}");
            var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.All);
            var semanticModel = compilation.GetSemanticModel(syntaxTree);
            var node = syntaxTree.EqualsValueClause("var stream = StreamCache?.GetOrAdd(1, _ => File.OpenRead(\"A\"));").Value;
            using (var sources = VauleWithSource.GetRecursiveSources(node, semanticModel, CancellationToken.None))
            {
                var actual = string.Join(", ", sources.Item.Select(x => $"{x.Value} {x.Source}"));
                Assert.AreEqual(@"StreamCache?.GetOrAdd(1, _ => File.OpenRead(""A"")) External, StreamCache Cached", actual);
            }
        }

        [Test]
        public void AssigningWithFieldConcurrentDictionaryGetOrAdd()
        {
            var syntaxTree = CSharpSyntaxTree.ParseText(@"
using System.Collections.Concurrent;
using System.IO;

public class Foo
{
    private readonly ConcurrentDictionary<int, Stream> Cache = new ConcurrentDictionary<int, Stream>();

    public void Bar()
    {
        var stream = Cache.GetOrAdd(1, _ => File.OpenRead(""A""));
    }
}");
            var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.All);
            var semanticModel = compilation.GetSemanticModel(syntaxTree);
            var node = syntaxTree.EqualsValueClause("var stream = Cache.GetOrAdd(1, _ => File.OpenRead(\"A\"));").Value;
            using (var sources = VauleWithSource.GetRecursiveSources(node, semanticModel, CancellationToken.None))
            {
                var actual = string.Join(", ", sources.Item.Select(x => $"{x.Value} {x.Source}"));
                Assert.AreEqual(@"Cache.GetOrAdd(1, _ => File.OpenRead(""A"")) External, Cache Member, new ConcurrentDictionary<int, Stream>() Created", actual);
            }
        }

        [Test]
        public void AssigningWithFieldConcurrentDictionaryGetOrAddElvis()
        {
            var syntaxTree = CSharpSyntaxTree.ParseText(@"
using System.Collections.Concurrent;
using System.IO;

public class Foo
{
    private readonly ConcurrentDictionary<int, Stream> Cache = new ConcurrentDictionary<int, Stream>();

    public void Bar()
    {
        var stream = Cache?.GetOrAdd(1, _ => File.OpenRead(""A""));
    }
}");
            var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.All);
            var semanticModel = compilation.GetSemanticModel(syntaxTree);
            var node = syntaxTree.EqualsValueClause("var stream = Cache?.GetOrAdd(1, _ => File.OpenRead(\"A\"));").Value;
            using (var sources = VauleWithSource.GetRecursiveSources(node, semanticModel, CancellationToken.None))
            {
                var actual = string.Join(", ", sources.Item.Select(x => $"{x.Value} {x.Source}"));
                Assert.AreEqual(@"Cache?.GetOrAdd(1, _ => File.OpenRead(""A"")) External, Cache Member, new ConcurrentDictionary<int, Stream>() Created", actual);
            }
        }

        [Test]
        public void AssigningWithFieldConcurrentDictionaryTryGetValue()
        {
            var syntaxTree = CSharpSyntaxTree.ParseText(@"
using System.Collections.Concurrent;
using System.IO;

public class Foo
{
    private readonly ConcurrentDictionary<int, Stream> Cache = new ConcurrentDictionary<int, Stream>();

    public void Bar()
    {
        Stream stream;
        Cache.TryGetValue(1, out stream);
        var temp = stream;
    }
}");
            var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.All);
            var semanticModel = compilation.GetSemanticModel(syntaxTree);
            var node = syntaxTree.EqualsValueClause("var temp = stream;").Value;
            using (var sources = VauleWithSource.GetRecursiveSources(node, semanticModel, CancellationToken.None))
            {
                var actual = string.Join(", ", sources.Item.Select(x => $"{x.Value} {x.Source}"));
                Assert.AreEqual(@"Cache.TryGetValue(1, out stream) Out, Cache Member, new ConcurrentDictionary<int, Stream>() Created", actual);
            }
        }

        [Test]
        public void AssigningWithFieldElvis()
        {
            var syntaxTree = CSharpSyntaxTree.ParseText(@"
namespace Gu.Reactive
{
    using System;
    using System.IO;
    using System.Reactive.Disposables;

    public abstract class Foo : IDisposable
    {
        private readonly SingleAssignmentDisposable disposable;

        protected Foo(SingleAssignmentDisposable disposable)
        {
            this.disposable = disposable;
        }

        public void Dispose()
        {
            var meh = disposable?.Disposable.Dispose();
        }
    }
}");
            var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.All);
            var semanticModel = compilation.GetSemanticModel(syntaxTree);
            var node = syntaxTree.EqualsValueClause("var meh = disposable?.Disposable.Dispose();").Value;
            using (var sources = VauleWithSource.GetRecursiveSources(node, semanticModel, CancellationToken.None))
            {
                var actual = string.Join(", ", sources.Item.Select(x => $"{x.Value} {x.Source}"));
                Assert.AreEqual(@"disposable?.Disposable.Dispose() External, disposable Member, disposable Injected", actual);
            }
        }

        [Test]
        public void MethodInjected()
        {
            var syntaxTree = CSharpSyntaxTree.ParseText(@"
internal class Foo
{
    internal static void Bar(int meh)
    {
        var value = meh;
    }
}");
            var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.All);
            var semanticModel = compilation.GetSemanticModel(syntaxTree);
            var node = syntaxTree.EqualsValueClause("var value = meh;").Value;
            using (var sources = VauleWithSource.GetRecursiveSources(node, semanticModel, CancellationToken.None))
            {
                var actual = string.Join(", ", sources.Item.Select(x => $"{x.Value} {x.Source}"));
                Assert.AreEqual("meh Injected", actual);
            }
        }

        [Test]
        public void MethodInjectedWithOptional()
        {
            var syntaxTree = CSharpSyntaxTree.ParseText(@"
internal class Foo
{
    internal void Meh()
    {
        Bar(1);
        Bar(2, ""abc"");
    }

    internal void Bar(int meh, string text = null)
    {
        var value = meh;
    }
}");
            var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.All);
            var semanticModel = compilation.GetSemanticModel(syntaxTree);
            var node = syntaxTree.EqualsValueClause("var value = meh;").Value;
            using (var sources = VauleWithSource.GetRecursiveSources(node, semanticModel, CancellationToken.None))
            {
                var actual = string.Join(", ", sources.Item.Select(x => $"{x.Value} {x.Source}"));
                Assert.AreEqual("meh PotentiallyInjected, 1 Constant, 2 Constant", actual);
            }
        }

        [Test]
        public void MethodInjectedWithOptionalAssigningOptional()
        {
            var syntaxTree = CSharpSyntaxTree.ParseText(@"
internal class Foo
{
    internal static void Meh()
    {
        Bar(1);
        Bar(2, ""abc"");
    }

    internal static void Bar(int meh, string text = null)
    {
        var value = text;
    }
}");
            var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.All);
            var semanticModel = compilation.GetSemanticModel(syntaxTree);
            var node = syntaxTree.EqualsValueClause("var value = text;").Value;
            using (var sources = VauleWithSource.GetRecursiveSources(node, semanticModel, CancellationToken.None))
            {
                var actual = string.Join(", ", sources.Item.Select(x => $"{x.Value} {x.Source}"));
                Assert.AreEqual(@"text PotentiallyInjected, null Constant, ""abc"" Constant", actual);
            }
        }

        [Test]
        public void VariableAssignedWithOutParameter()
        {
            var syntaxTree = CSharpSyntaxTree.ParseText(@"
internal class Foo
{
    internal void Bar()
    {
        int value;
        this.Assign(out value);
        var temp = value;
    }

    private void Assign(out int value)
    {
        value = 1;
    }
}");
            var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.All);
            var semanticModel = compilation.GetSemanticModel(syntaxTree);
            var node = syntaxTree.EqualsValueClause("var temp = value;").Value;
            using (var sources = VauleWithSource.GetRecursiveSources(node, semanticModel, CancellationToken.None))
            {
                var actual = string.Join(", ", sources.Item.Select(x => $"{x.Value} {x.Source}"));
                Assert.AreEqual("this.Assign(out value) Out, 1 Constant", actual);
            }
        }

        [Test]
        public void VariableAssignedWithOutParameterAssignedTwice()
        {
            var syntaxTree = CSharpSyntaxTree.ParseText(@"
internal class Foo
{
    internal void Bar()
    {
        int value;
        this.Assign(out value);
        var temp = value;
    }

    private void Assign(out int value)
    {
        value = 1;
        value = 2;
    }
}");
            var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.All);
            var semanticModel = compilation.GetSemanticModel(syntaxTree);
            var node = syntaxTree.EqualsValueClause("var temp = value;").Value;
            using (var sources = VauleWithSource.GetRecursiveSources(node, semanticModel, CancellationToken.None))
            {
                var actual = string.Join(", ", sources.Item.Select(x => $"{x.Value} {x.Source}"));
                Assert.AreEqual("this.Assign(out value) Out, 1 Constant, 2 Constant", actual);
            }
        }

        [Test]
        public void VariableAssignedWithChainedOutParameter()
        {
            var syntaxTree = CSharpSyntaxTree.ParseText(@"
internal class Foo
{
    internal void Bar()
    {
        int value;
        this.Assign1(out value);
        var temp = value;
        var meh = temp;
    }

    private void Assign1(out int value1)
    {
        this.Assign2(out value1);
    }

    private void Assign2(out int value2)
    {
        value2 = 1;
    }
}");
            var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.All);
            var semanticModel = compilation.GetSemanticModel(syntaxTree);
            var node = syntaxTree.EqualsValueClause("var meh = temp;").Value;
            using (var sources = VauleWithSource.GetRecursiveSources(node, semanticModel, CancellationToken.None))
            {
                var actual = string.Join(", ", sources.Item.Select(x => $"{x.Value} {x.Source}"));
                Assert.AreEqual("this.Assign1(out value) Out, this.Assign2(out value1) Out, 1 Constant", actual);
            }
        }

        [Test]
        public void VariableAssignedWithRefParameter()
        {
            var syntaxTree = CSharpSyntaxTree.ParseText(@"
internal class Foo
{
    private int field;

    internal void Bar()
    {
        int value;
        this.Assign(ref value);
        var temp = value;
        var meh = temp;
    }

    private void Assign(ref int value)
    {
        value = 1;
    }
}");
            var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.All);
            var semanticModel = compilation.GetSemanticModel(syntaxTree);
            var node = syntaxTree.EqualsValueClause("var temp = value;").Value;
            using (var sources = VauleWithSource.GetRecursiveSources(node, semanticModel, CancellationToken.None))
            {
                var actual = string.Join(", ", sources.Item.Select(x => $"{x.Value} {x.Source}"));
                Assert.AreEqual("this.Assign(ref value) Ref, 1 Constant", actual);
            }

            node = syntaxTree.EqualsValueClause("var meh = temp;").Value;
            using (var sources = VauleWithSource.GetRecursiveSources(node, semanticModel, CancellationToken.None))
            {
                var actual = string.Join(", ", sources.Item.Select(x => $"{x.Value} {x.Source}"));
                Assert.AreEqual("this.Assign(ref value) Ref, 1 Constant", actual);
            }
        }
    }
}