ExpressionToCode
================

Download via nuget: [ExpressionToCodeLib](http://nuget.org/packages/ExpressionToCodeLib/).

Generates valid, readable C# from an Expression Tree.  For example, this is true:

```C#
  ExpressionToCode.ToCode(
    () => new[] { 1.0, 2.01, 3.5 }.SequenceEqual(new[] { 1.0, 2.01, 3.5 })
  )
== "() => new[] { 1.0, 2.01, 3.5 }.SequenceEqual(new[] { 1.0, 2.01, 3.5 })"
```

ExpressionToCode also provides a clone of Groovy's [Power Assert](http://dontmindthelanguage.wordpress.com/2009/12/11/groovy-1-7-power-assert/) which includes the code of the failing assertion's expression and the values of its subexpressions.  This functionality is particularly useful in a unit testing framework such as [NUnit](http://www.nunit.org/) or [xUnit.NET](http://xunit.codeplex.com/).  When you execute the following (failing) assertion:

```C#
PAssert.That(()=>Enumerable.Range(0,1000).ToDictionary(i=>"n"+i)["n3"].ToString()==(3.5).ToString());
```

The assertion fails with the following message:

```
PAssert.That failed for:

Enumerable.Range(0, 1000).ToDictionary(i => "n" + (object)i)["n3"].ToString() == 3.5.ToString()
             |                 |                            |         |        |        |
             |                 |                            |         |        |        "3.5"
             |                 |                            |         |        false
             |                 |                            |         "3"
             |                 |                            3
             |                 {[n0, 0], [n1, 1], [n2, 2], [n3, 3], [n4, 4], [n5, 5], [n6, 6], [n7, 7], [n8, 8], [n9, 9], ...}
             {0, 1, 2, 3, 4, 5, 6, 7, 8, 9, ...}
```

ExpressionToCode was inspired by [Power Asssert.NET](http://powerassert.codeplex.com/).  It differs from PowerAssert.NET by supporting a larger portion of the lambda syntax and that the generated C# is more frequently valid; the aim is to generate valid C# for *all* expression trees created from lambda's.  Currently supported:

Expression tree support
---

 * Supports static field and property access
 * Supports more operators, e.g. logical and bitwise negation
 * Recognizes C# indexer use (e.g. `dict["mykey"]==3`), in addition to special cases for array indexers and string indexers
 * Adds parentheses where required by operator precedence and associativity (e.g. `() => x - (a - b) + x * (a + b)` is correctly regenerated)
 * Generates valid numeric and other constant literals including escapes and suffixes where required (e.g. `1m + (decimal)Math.Sqrt(1.41)`)
 * Supports C# syntactic sugar for object initializers, object member initializers, list initializers, extension methods, anonymous types (issues [#12](/../../issues/12), [#3](/../../issues/3)), etc
 * Uses the same spacing rules Visual Studio does by default
 * Supports nested Lambdas
 * Expands generic type instances and nullable types into normal C# (e.g. `Func<int, bool>` and `int?`)
 * Recognizes references to `this` and omits the keyword where possible ([#5](/../../issues/5))  

**Not implemented (yet?):**

 * Omit implicit casts (e.g. `object.Equals((object)3, (object)4)`) - issue [#4](/../../issues/4).
 * Use LINQ query syntax where possible - issue [#6](/../../issues/6).
 * Detect when type parameters to methods are superfluous - issue [#13](/../../issues/13).
 * Detect when nested lambda parameters require type annotation - issue [#14](/../../issues/14).
 * Warn when `==` differs from `.Equals` or `.SequenceEquals`, as Power Assert.NET does (issue [#2](/../../issues/2)).
 * See all [open issues](https://github.com/EamonNerbonne/ExpressionToCode/issues).

`ExpressionToCode` API 
-----

All classes live in the `ExpressionToCodeLib` namespace.

These are:
 * `PAssert` for making assertions in NUnit tests and elsewhere.
   * `PAssert.That` and `PAssert.IsTrue` are identical; both test the provided boolean expression and print a readable error message on failure
 * `ExpressionToCode` Renders a System.Linq.Expressions.Expression object to source code.
   * `ExpressionToCode.ToCode` (several overloads) simply renders the expression as source code.
   * `ExpressionToCode.AnnotatedToCode` (several overloads) renders the expression as source code, then annotates all subexpressions which are computable with their value using the stalk-like rendering as shown on the Project Home page.

Two public helper classes exist:

 * `PAssertFailedException` thrown on assertion failure.
 * `ObjectToCode` Renders .NET objects to code; a helper class.
   * `ObjectToCode.PlainObjectToCode` renders simple objects that can be parsed by the C# compiler.  This includes strings, chars, decimals, floats, doubles, all the integer types, booleans, enums, nulls, and default struct values.
   * `ObjectToCode.ComplexObjectToPseudoCode` renders as best it can anything thrown at it; but the resultant rendering is not necessarily compilable.  This is used to display the values of subexpressions.

Dependencies
---
Requires .NET 4.0 (.NET 3.5 could be supported by omitting support for newer expression types, this would require a few simple source changes).

---

If you have any questions, you can contact me via github or mail eamon at nerbonne dot org.

See the documentation below, then download from or [import using NuGet](http://nuget.org/packages/ExpressionToCodeLib/), or just checkout the source (license: Apache 2.0 or the MIT license, at your option)!  
