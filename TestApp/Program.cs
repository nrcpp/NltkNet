using NltkNet;
using System.Linq;

namespace TestApp
{
    internal static class Program
    {
        private static void Main(string[] args)
        {
            Nltk.Init();
            if (args.Contains("--self-test"))
            {
                RuntimeTests.Run();
                return;
            }

            const string text = "WASHINGTON -- In the wake of a string of abuses by New York police officers in the 1990s, Loretta E. Lynch, " +
                                "the top federal prosecutor in Brooklyn, spoke forcefully about the pain of a broken trust that African-Americans " +
                                "felt and said the responsibility for repairing generations of miscommunication and mistrust fell to law enforcement.";

            using var tokens = Nltk.Tokenize.WordTokenize(text);
            using var taggedWords = Nltk.PosTag(tokens.AsNet);
            using PythonValue entities = Nltk.NeChunk(taggedWords);

            BuiltIns.Print($"NER output for text: '{text}'");
            BuiltIns.Print(entities);
        }
    }
}
