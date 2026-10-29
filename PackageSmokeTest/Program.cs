using NltkNet;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using TestApp;

if (args.Contains("--expect-missing-runtime"))
{
    try { Nltk.Init(); }
    catch (FileNotFoundException ex) when (ex.Message.Contains("NltkNet.Runtime."))
    {
        Console.WriteLine("PASS: missing runtime diagnostic.");
        return;
    }
    throw new InvalidOperationException("Expected a missing runtime error.");
}
if (args.Contains("--expect-integrity-error"))
{
    try { Nltk.Init(); }
    catch (InvalidDataException ex) when (ex.Message.Contains("SHA-256"))
    {
        Console.WriteLine("PASS: runtime integrity check.");
        return;
    }
    throw new InvalidOperationException("Expected a runtime integrity error.");
}
if (args.Contains("--parallel-init"))
    Parallel.For(0, 8, _ => Nltk.Init());
else
    Nltk.Init();
if (args.Contains("--runtime-info"))
{
    Console.WriteLine(Nltk.Py.Evaluate("__import__('sys').version"));
    Console.WriteLine(Nltk.Py.Evaluate("__import__('sys').getfilesystemencoding()"));
    Console.WriteLine(Nltk.Py.Evaluate("__import__('nltk').data.path"));
    Console.WriteLine(Nltk.Py.Evaluate("__import__('os').listdir(__import__('nltk').data.path[0])"));
}
if (args.Contains("--check-isolated-cache"))
{
    string prefix = Nltk.Py.Evaluate("__import__('sys').prefix");
    if (!prefix.Replace('\\', '/').StartsWith(Environment.GetEnvironmentVariable("NLTKNET_CACHE_DIRECTORY").Replace('\\', '/') + "/", StringComparison.Ordinal))
        throw new InvalidOperationException("The cold-cache test did not use the isolated cache: " + prefix);
}
RuntimeTests.Run(withData: args.Contains("--with-data"));
