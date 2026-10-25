using VoxSign.Shared;

namespace VoxSign.Api.Tests;

public class SignSequencerTests
{
    static readonly SignSequencer Sequencer = new(SeedCatalog.Signs.Select(s => (s.Id, s.Word, (IEnumerable<string>)s.Keywords)));

    [Fact]
    public void Phrase_de_la_maquette_donne_trois_reperes_dans_l_ordre() =>
        Assert.Equal(["bonjour", "cafe", "svp"], Sequencer.Sequence("Bonjour, je voudrais un café, s’il vous plaît."));

    [Theory]
    [InlineData("BONJOUR", "bonjour")]
    [InlineData("deux cafes", "cafe")]
    [InlineData("s'il vous plait", "svp")]
    [InlineData("J’ai besoin d’aide.", "aide")]
    public void Ignore_casse_accents_apostrophes_et_pluriel(string text, string expected) =>
        Assert.Equal([expected], Sequencer.Sequence(text));

    [Fact]
    public void Expression_la_plus_longue_l_emporte_et_conserve_le_texte_d_origine()
    {
        var tokens = Sequencer.Tokenize("Merci, s’il vous plaît !");

        Assert.Equal(new SignSequencer.Token("Merci", "merci"), tokens[0]);
        Assert.Equal(new SignSequencer.Token("s’il vous plaît", "svp"), tokens[1]);
        Assert.Equal(2, tokens.Count);
    }

    [Fact]
    public void Les_mots_sans_repere_sont_conserves_sans_identifiant()
    {
        var tokens = Sequencer.Tokenize("Je voudrais du thé");

        Assert.All(tokens, t => Assert.Null(t.SignId));
        Assert.Equal(["Je", "voudrais", "du", "thé"], tokens.Select(t => t.Text));
    }
}
