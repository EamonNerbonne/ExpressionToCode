using System;
using System.Linq;
using ExpressionToCodeLib.Internal;

namespace ExpressionToCodeLib
{
    public static class ObjectStringify
    {
        public static readonly IObjectStringifier Default = new ObjectStringifyImpl();
        public static readonly IObjectStringifier WithFullTypeNames = new ObjectStringifyImpl(true);
    }
}
