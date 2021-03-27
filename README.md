# Overview

SdlangSharp is a SIMD-accelerated library that brings fast, user-friendly access to reading and writing [SDLang](https://sdlang.org/) files.

SDLang is an ergonomic structured textual data language (similar to JSON and XML) that is designed more for humans than for computers, so is well suited
for user-written and user-read data/configuration files. It's syntax even allows it to be used as a pseudo programming language.

For formats that are purely for communication between computers, where the raw data isn't read or written by a user, SDLang isn't the greatest choice compared
to the likes of JSON and XML.

**Documentation still under construction, this is just the bare minimum right now. Library is early in development as well, so expect rough edges.**

1. [Overview](#overview)
1. [Features](#features)
1. [HOWTO](#howto)
    1. [Load a string into an AST](#load-a-string-into-an-ast)
    1. [Reading from an AST](#reading-from-an-ast)
    1. [Writing to an AST](#writing-to-an-ast)
    1. [Converting an AST into a string](#converting-an-ast-into-a-string)
1. [Usages of SDLang](#usages-of-sdlang)
1. [Limitations](#limitations)
1. [Performance](#performance)
1. [Safety](#safety)
1. [Contributing](#contributing)

# Features

* A fast, low-level SIMD-accelerated pull parser for code that **needs** to be fast.

* A medium-level push parser that still allows for a more manual style of parsing, but is easier to use than the pull parser.
  This is the best option for when the AST is a bit too heavy-weight.

* A high-level DOM/AST for maximum user-friendliness and comfort. Most projects will likely use this as the other options are for the rare
  project that needs to process huge amounts of SDL data. This option is obviously the slowest and most memory heavy, but that doesn't matter for most casual use-cases.

* Ability to export the AST into a user-readable string.

# HOWTO

## Load a string into an AST

Before we can do anything else, we need to be able to load in our data.

Doing this is as simple as `new SdlReader("code").ToAst()`:

```csharp
string input = File.ReadText("test.sdl"); // Or however you want to do this.
SdlTag rootTag = new SdlReader(input).ToAst();
```

## Reading from an AST

With an `SdlTag` in hand we now need to read data from it. Please see the [AST overview](#ast-overview) for an overview of all the functions available to you,
as for now we'll just use a handful.

```csharp
string sdl = @"
person ""Bradley Chatha"" age=21 {
    pet:dog ""Cooper"" cute=true
}

numbers {
    // Tags without a name are implicitly called 'content'
    1 1 1
    2 2 2
    3 3 3
}
";

SdlTag root = new SdlReader(sdl).ToAst();

// Multiple tags can have the same name, so GetChildrenCalled returns an IEnumerable.
SdlTag person = root.GetChildrenCalled("person").First(); // Or root.Children[0]
Console.WriteLine(
    "{0} is {1} years old",
    person.GetValueString(0),         // Get 0th value as a string
    person.GetAttributeInteger("age") // Get "age" as a long
);

// Or root.Children[0], or root.GetChildrenCalled("pet:dog"), or however really.
SdlTag pet = person.Children.Where(c => c.Namespace == "pet").First();
Console.WriteLine(
    "They have a pet {0} called {1} and {2}",
    pet.Name, // QualifiedName=pet:dog, Namespace=pet, Name=dog
    pet.GetValueString(0),
    pet.GetAttributeBoolean("cute") ? "he is cute" : "he is not cute?! You monster!"
);

// As mentioned in the SDL above, tags don't need to have names, and are called "content" by default.
SdlTag numbers = root.Children[1];
Console.WriteLine(
    "After crunching the numbers, I've come up with the answer: {0}",
    // Sum up all the numbers.
    numbers.Children
           .Where(c => c.Name == "content")
           .SelectMany(c => c.Values)
           .Select((SdlValue v) => v.Integer)
           .Aggregate((a,b) => a+b)
);
```

This produces the following output:

```
Bradley Chatha is 21 years old
They have a pet dog called Cooper and he is cute
After crunching the numbers, I've come up with the answer: 18
```

## Writing to an AST

The AST is as easy to manually create/edit as it is to read from, for example let's create the AST from
the previous section, but by hand:

```csharp
var root = new SdlTag("root");

var person = new SdlTag("person");
root.Children.Add(person);
person.Values.Add(new SdlValue("Bradley Chatha"));
person.Attributes.Add(new SdlAttribute("age", new SdlValue(21)));

var pet = new SdlTag("pet:dog");
person.Children.Add(pet);
pet.Values.Add(new SdlValue("Cooper"));
pet.Attributes.Add(new SdlAttribute("cute", SdlValue.True)); // or new SdlValue(true)

var numbers = new SdlTag("numbers")
{
    Children = new[]
    {
        new SdlTag("content"){ Values = new[]{ new SdlValue(1), new SdlValue(1), new SdlValue(1) } },
        new SdlTag("content"){ Values = new[]{ new SdlValue(2), new SdlValue(2), new SdlValue(2) } },
        new SdlTag("content"){ Values = new[]{ new SdlValue(3), new SdlValue(3), new SdlValue(3) } }
    }
};
root.Children.Add(numbers);
```

et voila.

# Converting an AST into a string

To do this, simply call the `.ToSdlString()` function. Let's use the AST we created above for example:

```csharp
var root = ...; // Code from the "Writing to an AST" section.

Console.WriteLine(root.ToSdlString());
```

And this produces the following output(strings export in WYSIWYG/backtick style for now):

```sdl
person `Bradley Chatha` age=21 {
    pet:dog `Cooper` cute=true
}
numbers {
    1 1 1
    2 2 2
    3 3 3
}
```


# Usages of SDLang

# Limitations

Currently the pull parser does not provide any debug information such as line and column due to it having quite a large performance impact on larger files.
However, considering SDLang is a human-focused language, I may just bite by pride and add it in (line numbers at the very least) despite it affecting performance in the <1% use case. It's especially annoying since SDLang allows you to escape raw new lines, and provides a WYSIWYG string type which preserves new lines, making it a bit
harder than it would've been otherwise, especially with SIMD stuff. (I also only measured this performance impact *before* adding in any SIMD stuff, and the pre-SIMD stuff  was already really slow due to my bad code, so I need to look at this again properly.)

Mostly due to laziness which I'll slyly write off as also being because of performance impact, the pull parser isn't 100% compliant and will allow things it technically shouldn't allow, but for the most part this is a non-issue honestly. If it allows something that is incorrect, still feel free the file an issue though, so it's documented somewhere for when/if I get round to fixing it.

Currently there is only an AVX2 acceleration in place, but I want to make an SSE one soon. If the user's computer is so old that it doesn't have either the AVX2 or (eventually) SSE instruction sets, then the library will use a slower fallback implementation. Again though, this is only really a concern for people who are using massive datasets.

# Performance

The preface is that this isn't a very "scientifically" benchmarked project so take this with a grain of salt, 
and that I'd also like to add in some graphs or something in the future, but for now here's the raw output of my last benchmark.

I'm not a very know-how person when it comes to optimisation, especially with languages like C#. This was also my first time using SIMD, but for the purposes that
I imagine SDLang is being used for, this library is plenty fast as-is.

The input data consists of some misc SDL data from the internet, which is then duplicated up to at least the **Megabytes** column in length before the test is ran.

The tests are:

* RawLogiclessParse: Tests how fast the pull parser parses the input data, nothing else beyond that.

* NullTokenPushing: Tests how fast the push parser can push all tokens into a token visitor that does nothing.

* NullTokenRawPushing: Same as `NullTokenPushing` except it uses a null token visitor that implements `ISdlRawTokenVisitor`, which stops the push parser from
  automatically converting values into an `SdlValue`, and instead places that burden onto the visitor itself.

* AstParse: Tests AST construction.

``` ini

BenchmarkDotNet=v0.12.1, OS=Windows 10.0.19041.867 (2004/?/20H1)
Intel Core i5-7600K CPU 3.80GHz (Kaby Lake), 1 CPU, 4 logical and 4 physical cores
.NET Core SDK=5.0.104
  [Host]     : .NET Core 3.1.13 (CoreCLR 4.700.21.11102, CoreFX 4.700.21.11602), X64 RyuJIT  [AttachedDebugger]
  DefaultJob : .NET Core 3.1.13 (CoreCLR 4.700.21.11102, CoreFX 4.700.21.11602), X64 RyuJIT


```
|              Method | Megabytes |         Mean |       Error |      StdDev |       Gen 0 |       Gen 1 |     Gen 2 |     Allocated |
|-------------------- |---------- |-------------:|------------:|------------:|------------:|------------:|----------:|--------------:|
|   **RawLogiclessParse** |         **1** |     **3.707 ms** |   **0.0082 ms** |   **0.0073 ms** |     **35.1563** |           **-** |         **-** |      **117.5 KB** |
|    NullTokenPushing |         1 |    10.533 ms |   0.0315 ms |   0.0295 ms |   4281.2500 |           - |         - |   13118.97 KB |
| NullTokenRawPushing |         1 |     6.605 ms |   0.0152 ms |   0.0142 ms |    773.4375 |           - |         - |    2373.61 KB |
|            AstParse |         1 |    58.738 ms |   0.5314 ms |   0.4710 ms |   3888.8889 |   1666.6667 |  666.6667 |   21306.79 KB |
|   **RawLogiclessParse** |        **10** |    **37.523 ms** |   **0.2036 ms** |   **0.1904 ms** |    **357.1429** |           **-** |         **-** |    **1176.25 KB** |
|    NullTokenPushing |        10 |   104.445 ms |   0.4488 ms |   0.4198 ms |  42600.0000 |           - |         - |  131328.74 KB |
| NullTokenRawPushing |        10 |    65.867 ms |   0.1236 ms |   0.1156 ms |   7750.0000 |           - |         - |   23760.34 KB |
|            AstParse |        10 |   579.102 ms |   3.0707 ms |   2.8723 ms |  34000.0000 |  11000.0000 |         - |  214821.94 KB |
|   **RawLogiclessParse** |        **25** |    **93.757 ms** |   **0.5492 ms** |   **0.5137 ms** |    **833.3333** |           **-** |         **-** |    **2940.84 KB** |
|    NullTokenPushing |        25 |   264.310 ms |   1.1890 ms |   1.1122 ms | 106000.0000 |           - |         - |  328320.88 KB |
| NullTokenRawPushing |        25 |   165.346 ms |   0.4432 ms |   0.4145 ms |  19250.0000 |           - |         - |    59400.9 KB |
|            AstParse |        25 | 1,607.097 ms |  14.7239 ms |  13.7727 ms |  87000.0000 |  30000.0000 | 1000.0000 |   535007.1 KB |
|   **RawLogiclessParse** |        **50** |   **197.801 ms** |   **1.2253 ms** |   **1.1462 ms** |   **1666.6667** |           **-** |         **-** |    **5881.88 KB** |
|    NullTokenPushing |        50 |   528.585 ms |   3.9946 ms |   3.7366 ms | 214000.0000 |           - |         - |  656712.73 KB |
| NullTokenRawPushing |        50 |   330.148 ms |   1.2207 ms |   1.1418 ms |  38000.0000 |           - |         - |  118813.97 KB |
|            AstParse |        50 | 3,539.433 ms |  19.2835 ms |  18.0378 ms | 175000.0000 |  60000.0000 | 3000.0000 | 1070122.75 KB |
|   **RawLogiclessParse** |        **75** |   **281.749 ms** |   **1.8673 ms** |   **1.7466 ms** |   **2500.0000** |           **-** |         **-** |    **8823.13 KB** |
|    NullTokenPushing |        75 |   790.808 ms |   3.5657 ms |   3.3354 ms | 320000.0000 |           - |         - |  985032.22 KB |
| NullTokenRawPushing |        75 |   494.901 ms |   1.4535 ms |   1.3596 ms |  57000.0000 |           - |         - |  178214.59 KB |
|            AstParse |        75 | 5,303.051 ms | 100.1587 ms |  93.6886 ms | 263000.0000 |  93000.0000 | 6000.0000 | 1613319.84 KB |
|   **RawLogiclessParse** |       **100** |   **369.545 ms** |   **2.1802 ms** |   **2.0393 ms** |   **3000.0000** |           **-** |         **-** |   **11763.75 KB** |
|    NullTokenPushing |       100 | 1,050.204 ms |   1.8269 ms |   1.7089 ms | 428000.0000 |           - |         - | 1313422.78 KB |
| NullTokenRawPushing |       100 |   663.975 ms |   1.7446 ms |   1.5466 ms |  76000.0000 |           - |         - |  237629.14 KB |
|            AstParse |       100 | 7,125.713 ms | 140.9261 ms | 363.7752 ms | 348000.0000 | 262000.0000 | 4000.0000 |  2140244.7 KB |

I'm not quite sure where exactly `NullTokenRawPushing` is allocating so much memory, as it should be roughly the same as `RawLogiclessParse` here, but I haven't looked into it
too much yet.

Furthermore, the following parts of the pull parser are SIMD-accelerated:

* Both styles of string.

  * Double quoted strings are currently iterated over twice - once to find the ending speech mark, then once more for some light validation.
    This is bad for small strings, but ends up being faster for larger ones. This is **really** bad for systems that don't have the SIMD intrinsics supported.

* Comments.

* Identifiers

* Base64 strings

I fear that numbers, datetimes, and timespans would actually perform slower on average when using SIMD, due to them not really being too many characters long,
especially for hand-made files, so I've left them out.

Strings, comments, and base64 strings can all be massive, so SIMD will generally speed things up here. Honestly this is where most of the time save has been
during my own tests.

Identifiers I've had mixed results with, since they're also generally quite short in characters the overhead of calling the SIMD stuff is relatively heavy.
This is combined with the fact that identifiers need to use the slower variation of the searcher function.

# Safety

This project contains two unsafe code segments: `SdlReader.ReadToEndOrChar` and `SdlReader.ReadToEndOrChars`.

They are unsafe due to usage of the `fixed` statement to load data into SIMD registers, and just general use of SIMD intrinsics.
These data loads should always be in bounds though, and are always assumed to be unaligned.

# Contributing

I'm perfectly accepting of anyone wanting to contribute to this library, just note that it might take me a while to respond.

Also note that I'm not too aware of the C# open-source scene, and am still pretty amateurish at C# itself, so excuse some oddities with the code!

And please, if you have an issue, *create a Github issue for me*. I can't fix or prioritise issues that I don't know exist.
I tend to not care about issues when **I** run across them, but when **someone else** runs into them, then it becomes a much higher priority for me to address it.

Finally, if you use this library in anyway feel free to request for me to add your project into an `Examples` section. I'd really love to see how others are using my code :)
