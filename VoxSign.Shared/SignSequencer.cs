using System.Text.RegularExpressions;

namespace VoxSign.Shared;

/// <summary>
/// Découpe une phrase en repères illustrés : recherche, de gauche à droite, l'expression connue la plus longue
/// (« s'il vous plaît » avant « plaît »), sans tenir compte de la casse, des accents, des apostrophes typographiques
/// ni du pluriel simple (« cafés »). Partagé par l'API et l'application (traduction hors ligne).
/// </summary>
public sealed partial class SignSequencer
{
    public readonly record struct Token(string Text, string? SignId);

    readonly List<(string[] Words, string SignId)> _expressions;

    /// <param name="lexicon">Repères et leurs mots-clés (le mot du repère est ajouté d'office).</param>
    public SignSequencer(IEnumerable<(string SignId, string Word, IEnumerable<string> Keywords)> lexicon)
    {
        _expressions = lexicon
            .SelectMany(entry => entry.Keywords.Append(entry.Word).Select(keyword => (Words: Words(keyword).Select(w => TextSearch.Fold(w.Value)).ToArray(), entry.SignId)))
            .Where(e => e.Words.Length > 0)
            .DistinctBy(e => (string.Join(' ', e.Words), e.SignId))
            .OrderByDescending(e => e.Words.Length)
            .ToList();
    }

    public static SignSequencer FromSigns(IEnumerable<SignDto> signs) =>
        new(signs.Select(s => (s.Id, s.Word, (IEnumerable<string>)s.Keywords)));

    /// <summary>Les mots de la phrase, chacun associé à un repère quand il en existe un.</summary>
    public IReadOnlyList<Token> Tokenize(string text)
    {
        var words = Words(text).ToList();
        var folded = words.Select(w => TextSearch.Fold(w.Value)).ToArray();
        var tokens = new List<Token>();

        for (var i = 0; i < words.Count;)
        {
            var match = _expressions.FirstOrDefault(e => Matches(folded, i, e.Words));
            if (match.Words is null)
            {
                tokens.Add(new Token(words[i].Value, null));
                i++;
                continue;
            }

            var first = words[i];
            var last = words[i + match.Words.Length - 1];
            tokens.Add(new Token(text.Substring(first.Index, last.Index + last.Length - first.Index), match.SignId));
            i += match.Words.Length;
        }

        return tokens;
    }

    /// <summary>Les repères de la phrase, dans l'ordre.</summary>
    public IReadOnlyList<string> Sequence(string text) =>
        Tokenize(text).Where(t => t.SignId is not null).Select(t => t.SignId!).ToList();

    static bool Matches(string[] words, int start, string[] expression)
    {
        if (start + expression.Length > words.Length)
            return false;

        for (var k = 0; k < expression.Length; k++)
        {
            var word = words[start + k];
            var expected = expression[k];
            // Pluriel simple : « cafés », « aides »
            if (word != expected && !(word.Length == expected.Length + 1 && word.StartsWith(expected, StringComparison.Ordinal) && word[^1] is 's' or 'x'))
                return false;
        }

        return true;
    }

    static IEnumerable<Match> Words(string text) => WordRegex().Matches(text);

    // Les apostrophes (droite ou typographique) séparent les mots : « s’il » → « s », « il »
    [GeneratedRegex(@"[\p{L}\p{N}]+")]
    private static partial Regex WordRegex();
}
