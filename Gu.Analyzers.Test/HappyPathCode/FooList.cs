// ReSharper disable UnusedMember.Global Used in HappyPathWithAll.PropertyChangedAnalyzersSln
// ReSharper disable NotAccessedField.Local
// ReSharper disable UnusedVariable
// ReSharper disable UnusedParameter.Local
// ReSharper disable RedundantAssignment
// ReSharper disable ArrangeAccessorOwnerBody
// ReSharper disable FunctionRecursiveOnAllPaths
// ReSharper disable UnusedParameter.Global
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable ArrangeThisQualifier
// ReSharper disable PossibleUnintendedReferenceComparison
// ReSharper disable RedundantCheckBeforeAssignment
// ReSharper disable UnusedMethodReturnValue.Global
// ReSharper disable CollectionNeverUpdated.Local
#pragma warning disable 1717
#pragma warning disable SA1101 // Prefix local calls with this
#pragma warning disable GU0011 // Don't ignore the return value.
#pragma warning disable GU0010 // Assigning same value.
#pragma warning disable IDE0009 // Member access should be qualified.
namespace Gu.Analyzers.Test.HappyPathCode
{
    using System.Collections;
    using System.Collections.Generic;

    internal class FooList<T> : IReadOnlyList<T>
    {
        private readonly List<T> inner = new List<T>();

        public int Count => this.inner.Count;

        public T this[int index] => this.inner[index];

        public IEnumerator<T> GetEnumerator()
        {
            return this.inner.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();
    }
}
