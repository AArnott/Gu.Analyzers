namespace Gu.Analyzers
{
    internal static class KnownSymbol
    {
        internal static readonly QualifiedType Object = Create("System.Object");
        internal static readonly StringType String = new StringType();

        private static QualifiedType Create(string qualifiedName)
        {
            return new QualifiedType(qualifiedName);
        }
    }
}