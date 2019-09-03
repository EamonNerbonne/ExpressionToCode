﻿ExpressionToCode
================
ExpressionToCode generates valid, readable C# from an Expression Tree. (nuget: [ExpressionToCodeLib](http://nuget.org/packages/ExpressionToCodeLib/))
------


  An example:

```C#
  ExpressionToCode.ToCode(
    () => new[] { 1.0, 2.01, 3.5 }.SequenceEqual(new[] { 1.0, 2.01, 3.5 })
  )
== "() => new[] { 1.0, 2.01, 3.5 }.SequenceEqual(new[] { 1.0, 2.01, 3.5 })"
```

ExpressionToCode also provides something like Groovy's [Power Assert](http://dontmindthelanguage.wordpress.com/2009/12/11/groovy-1-7-power-assert/) which includes the code of the failing assertion's expression and the values of its subexpressions.  This functionality is particularly useful in a unit testing framework such as [NUnit](http://www.nunit.org/) or [xUnit.NET](http://xunit.github.io/).  When you execute the following (failing) assertion:

```C#
PAssert.That(()=>Enumerable.Range(0,1000).ToDictionary(i=>"n"+i)["n3"].ToString()==(3.5).ToString());
```

The assertion fails with the following message:

```
assertion failed

Enumerable.Range(0, 1000).ToDictionary(i => "n" + i)["n3"].ToString(CultureInfo.InvariantCulture) == 3.5.ToString(CultureInfo.InvariantCulture)
   →   false (caused assertion failure)

Enumerable.Range(0, 1000).ToDictionary(i => "n" + i)["n3"].ToString(CultureInfo.InvariantCulture)
     →   "3"

Enumerable.Range(0, 1000).ToDictionary(i => "n" + i)["n3"]
     →   3

Enumerable.Range(0, 1000).ToDictionary(i => "n" + i)
     →   new Dictionary<string, int> {
              ["n0"] = 0,
              ["n1"] = 1,
              ["n2"] = 2,
              ["n3"] = 3,
              ["n4"] = 4,
              ["n5"] = 5,
              ["n6"] = 6,
              ["n7"] = 7,
              ["n8"] = 8,
              ["n9"] = 9,
              ["n10"] = 10,
              ["n11"] = 11,
              ["n12"] = 12,
              ["n13"] = 13,
              ["n14"] = 14,
              ["n15"] = 15,
              ["n16"] = 16,
              ["n17"] = 17,
              ["n18"] = 18,
              ["n19"] = 19,
              ["n20"] = 20,
              ["n21"] = 21,
              ["n22"] = 22,
              ["n23"] = 23,
              ["n24"] = 24,
              ["n25"] = 25,
              ["n26"] = 26,
              ["n27"] = 27,
              ["n28"] = 28,
              ["n29"] = 29,
              ...
          }

Enumerable.Range(0, 1000)
     →   { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, ... }

3.5.ToString(CultureInfo.InvariantCulture)
     →   "3.5"

```

ExpressionToCode's output is configurable in various ways. For expressions with small values, a values-on-stalks rendering might instead be used:
```C#
var a = 2;
var b = 5;
ExpressionToCodeConfiguration.DefaultAssertionConfiguration.WithAnnotator(CodeAnnotators.ValuesOnStalksCodeAnnotator)
    .Assert(() => Math.Max(a, b) > new[] { 3, 8, 13, 4 }.Average() );
 ```
 
```
Math.Max(a, b) > new[] { 3, 8, 13, 4 }.Average()  :  assertion failed
      │  │  │       │                     │
      │  │  │       │                     7.0
      │  │  │       new[] { 3, 8, 13, 4 }
      │  │  5
      │  2
      5
```

Note that the default configuration for asserts (i.e. `PAssert.That`) limits the length of sequences and strings; the default configuration of code-generation does not.

ExpressionToCode was inspired by [Power Assert.NET](https://github.com/PowerAssert/PowerAssert.Net).  It differs from PowerAssert.NET by supporting a larger portion of the lambda syntax and that the generated C# is more frequently valid; the aim is to generate valid C# for *all* expression trees created from lambda's.  Currently supported:

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
 * Recognizes closed-over variables and prints something plausible, rather than the crypic compiler-generated names.
 * Omits most implicit casts (e.g. `object.Equals(3, 4)` instead of `object.Equals((object)3, (object)4)`) - user defined implicit cast operators are not elided ([#4](/../../issues/4))
 * Detects when type parameters to methods are superfluous ([#13](/../../issues/13)).

**Not implemented (yet?):**
 * Use LINQ query syntax where possible - issue [#6](/../../issues/6).
 * Explicitly cast otherwise inferable lambda when required due to ambiguous overloads - issue [#14](/../../issues/14).
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

A complete listing of the public api is [here](ExpressionToCodeTest/ApiStabilityTest.PublicApi.approved.txt)

Supported platforms

---

Requires .NET 4.5.2 or .net standard 1.6 (previous versions support older platforms)

---

If you have any questions, you can contact me via github or mail eamon at nerbonne dot org.

See the documentation above, then download from or [import using NuGet](http://nuget.org/packages/ExpressionToCodeLib/), or just checkout the source (license: Apache 2.0 or the MIT license, at your option)!  
