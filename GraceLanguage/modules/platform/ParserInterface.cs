using System.Collections.Generic;
using System.Linq;
using System.IO;
using System;
using Grace.Parsing;
using Grace.Execution;
using Grace.Runtime;
using Grace.Utility;

[ModuleEntryPoint]
public class PrettyPrinterUtil : GraceObject
{
    DictionaryDataObject parseNodes = new DictionaryDataObject(ParseNodeMeta.GetPatternDict());

    public static GraceObject Instantiate(EvaluationContext ctx) { return new PrettyPrinterUtil(); }
    public PrettyPrinterUtil()
    {
        this.AddMethod("parseNodes", new DelegateMethod0(() =>
            parseNodes));
        this.AddMethod("parseFile(_)", new DelegateMethod1(filename =>
             new GraceObjectProxy(new Parser(File.OpenText(filename.FindNativeParent<GraceString>().Value).ReadToEnd()).Parse())));
        this.AddMethod("printStr(_)", new DelegateMethod1(str =>
             { Console.Write(str.FindNativeParent<GraceString>().Value); return Done; }));
        this.AddMethod("translateNewlines(_)", new DelegateMethod1(str =>
            GraceString.Create(str.FindNativeParent<GraceString>().Value.Replace("\u2028", Environment.NewLine))));
        this.AddMethod("argv", new DelegateMethod0(() =>
            GraceVariadicList.Of(UnusedArguments.UnusedArgs.Select(GraceString.Create))));
        this.AddMethod("rand(_)", new DelegateMethod1(seed =>
            new GraceRandom(seed)));
        this.AddMethod("newSet", new DelegateMethod0(() =>
            new GraceNumberSet()));
        this.AddMethod("asInt(_)", new DelegateMethod1(s =>
            GraceNumber.Create(Int32.Parse(s.FindNativeParent<GraceString>().Value))));
    }
}

class GraceRandom : GraceObject
{
    public GraceRandom(GraceObject seed) : base(createSharedMethods())
    {
        GraceNumber o = GraceNumberSet.AsNumber(seed);
        Int32 s = 0;
        if (o != null) s = o.GetInt();
        else
        {
            Grace.ErrorReporting.RaiseError(null, "R2001",
                new Dictionary<string, string>() {
                    { "method", "rand" },
                    { "index", "0" },
                    { "part", "rand" },
                    { "required", "Number" }
                },
                "ArgumentTypeError: rand requires a Number argument"
            );
        }
        this.generator = new Random(s);
    }
    Random generator;

    /// <summary>Generates a random number between 1 and other</summary>
    /// <param name="other">Argument to the method</param>
    public static GraceObject mNext(GraceRandom rand, GraceObject other)
    {
        GraceNumber o = GraceNumberSet.AsNumber(other);
        if (o != null)
            return GraceNumber.Create(rand.generator.Next(1, o.GetInt()));

        Grace.ErrorReporting.RaiseError(null, "R2001",
                new Dictionary<string, string>() {
                    { "method", "next" },
                    { "index", "1" },
                    { "part", "next" },
                    { "required", "Number" }
                },
                "ArgumentTypeError: next requires a Number argument"
        );
        return GraceNumber.Create(0);
    }

    // <summary>Generates a random boolean</summary>
    public static GraceObject mNextBool(GraceRandom rand) { return GraceBoolean.Create(rand.generator.NextDouble() >= 0.5); }

    private static Dictionary<string, Method> sharedMethods;
    private static Dictionary<string, Method> createSharedMethods()
    {
        if (sharedMethods != null)
            return sharedMethods;

        sharedMethods = new Dictionary<string, Method>
        {
            { "next(_)", new DelegateMethodTyped1<GraceRandom>(mNext) },
            { "nextBool", new DelegateMethodTyped0<GraceRandom>(mNextBool) }
        };
        return sharedMethods;
    }
}

/// <summary>A set of Grace Numbers</summary>
class GraceNumberSet : GraceObject
{
    public GraceNumberSet() : base(createSharedMethods()) { }
    HashSet<Rational> set = new HashSet<Rational>();

    public override String ToString()
    {
        return "{" + String.Join(", ", this.set.Select(r => r.ToString()).ToArray()) + "}";
    }
    /// <param name="other">Argument to the method</param>
    public static GraceObject mContains(GraceNumberSet set, GraceObject other)
    {
        GraceNumber o = AsNumber(other);
        if (o != null)
            return GraceBoolean.Create(set.set.Contains(o.Value));

        Grace.ErrorReporting.RaiseError(null, "R2001",
                new Dictionary<string, string>() {
                    { "method", "contains" },
                    { "index", "1" },
                    { "part", "contains" },
                    { "required", "Number" }
                },
                "ArgumentTypeError: contains requires a Number argument"
        );
        return GraceBoolean.Create(false);
    }

    public static GraceNumber AsNumber(GraceObject other)
    {
        var num = other as GraceNumber;
        var prox = other as GraceObjectProxy;

        return num != null ? num : prox != null ? prox.Object as GraceNumber : null;
    }

    /// <param name="other">Argument to the method</param>
    public static GraceObject mAdd(GraceNumberSet set, GraceObject other)
    {
        GraceNumber o = AsNumber(other);
        if (o != null) set.set.Add(o.Value);
        else Grace.ErrorReporting.RaiseError(null, "R2001",
                new Dictionary<string, string>() {
                    { "method", "add" },
                    { "index", "1" },
                    { "part", "add" },
                    { "required", "Number" }
                },
                "ArgumentTypeError: add requires a Number argument"
        );
        return GraceObject.Done;
    }

    private static Dictionary<string, Method> sharedMethods;
    private static Dictionary<string, Method> createSharedMethods()
    {
        if (sharedMethods != null)
            return sharedMethods;

        sharedMethods = new Dictionary<string, Method>
        {
            { "add(_)", new DelegateMethodTyped1<GraceNumberSet>(mAdd) },
            { "contains(_)", new DelegateMethodTyped1<GraceNumberSet>(mContains) },
        };
        return sharedMethods;
    }
}