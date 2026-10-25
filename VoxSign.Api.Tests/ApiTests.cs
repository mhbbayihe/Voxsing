using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using VoxSign.Shared;

namespace VoxSign.Api.Tests;

public class ApiTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    // ---------- Compte ----------

    [Fact]
    public async Task Inscription_puis_connexion_donne_un_jeton_valide()
    {
        var client = factory.CreateClient();
        var email = $"{Guid.NewGuid():N}@Exemple.fr";

        var register = await client.PostAsJsonAsync("/api/auth/register", new RegisterRequest("Daniela", email, "voxsign-2026", "LSF"));
        Assert.Equal(HttpStatusCode.Created, register.StatusCode);

        var login = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest($"  {email.ToUpperInvariant()} ", "voxsign-2026"));
        login.EnsureSuccessStatusCode();
        var auth = await login.Content.ReadFromJsonAsync<AuthResponse>();
        Assert.Equal(email.ToLowerInvariant(), auth!.User.Email);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.Token);
        var me = await client.GetFromJsonAsync<UserDto>("/api/me");
        Assert.Equal("Daniela", me!.FirstName);
        Assert.Equal(SpeechPreferencesDto.Default, me.Speech);
    }

    [Fact]
    public async Task Inscription_refuse_doublon_et_donnees_invalides()
    {
        var client = factory.CreateClient();
        var email = $"{Guid.NewGuid():N}@exemple.fr";
        await client.PostAsJsonAsync("/api/auth/register", new RegisterRequest("A", email, "motdepasse", "LSF"));

        var duplicate = await client.PostAsJsonAsync("/api/auth/register", new RegisterRequest("B", email, "motdepasse", "LSF"));
        var invalid = await client.PostAsJsonAsync("/api/auth/register", new RegisterRequest("", "pas-un-email", "court", "XYZ"));

        Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode);
    }

    [Fact]
    public async Task Mauvais_mot_de_passe_renvoie_401()
    {
        var response = await factory.CreateClient().PostAsJsonAsync("/api/auth/login", new LoginRequest(SeedCatalog.DemoAccount.Email, "faux"));
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Preferences_vocales_sont_enregistrees_et_validees()
    {
        var client = await factory.CreateUserClientAsync();

        var ok = await client.PutAsJsonAsync("/api/me/preferences", new UpdatePreferencesRequest("LSFB", new SpeechPreferencesDto("fr-BE", 1.2f, 0.8f)));
        var invalid = await client.PutAsJsonAsync("/api/me/preferences", new UpdatePreferencesRequest(null, new SpeechPreferencesDto("fr-FR", 5f, 1f)));

        var user = await ok.Content.ReadFromJsonAsync<UserDto>();
        Assert.Equal("LSFB", user!.SignLanguage);
        Assert.Equal(new SpeechPreferencesDto("fr-BE", 1.2f, 0.8f), user.Speech);
        Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode);
    }

    // ---------- Saisie texte ----------

    [Fact]
    public async Task Traduction_renvoie_mots_et_sequence_de_reperes_illustres()
    {
        var response = await factory.CreateClient().PostAsJsonAsync("/api/translate", new TranslateRequest("Bonjour, je voudrais un café, s’il vous plaît."));
        var translation = await response.Content.ReadFromJsonAsync<TranslationDto>();

        Assert.Equal(["bonjour", "cafe", "svp"], translation!.Signs.Select(s => s.Id));
        Assert.EndsWith("/media/signs/cafe.jpg", translation.Signs[1].ImageUrl);
        Assert.Contains(translation.Tokens, t => t.Text == "voudrais" && t.SignId is null);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Traduction_refuse_un_texte_vide(string text)
    {
        var response = await factory.CreateClient().PostAsJsonAsync("/api/translate", new TranslateRequest(text));
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // ---------- Dictionnaire et phrases ----------

    [Fact]
    public async Task Dictionnaire_filtre_par_categorie_et_recherche_sans_accents()
    {
        var client = factory.CreateClient();

        var politesse = await client.GetFromJsonAsync<PagedResult<SignDto>>("/api/signs?category=politesse");
        var search = await client.GetFromJsonAsync<PagedResult<SignDto>>("/api/signs?search=cafe");
        var categories = await client.GetFromJsonAsync<List<CategoryDto>>("/api/categories");

        Assert.Equal(["Bonjour", "Merci", "S’il vous plaît"], politesse!.Items.Select(s => s.Word));
        Assert.Equal("Café", Assert.Single(search!.Items).Word);
        Assert.Equal(3, categories!.Single(c => c.Key == "politesse").SignCount);
    }

    [Fact]
    public async Task Image_d_un_repere_est_servie()
    {
        var response = await factory.CreateClient().GetAsync("/media/signs/merci.jpg");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("image/jpeg", response.Content.Headers.ContentType!.MediaType);
    }

    [Fact]
    public async Task Phrases_par_theme()
    {
        var phrases = await factory.CreateClient().GetFromJsonAsync<List<PhraseDto>>("/api/phrases?theme=cafe");
        var themes = await factory.CreateClient().GetFromJsonAsync<List<ThemeDto>>("/api/themes");

        Assert.Equal(8, phrases!.Count);
        Assert.Equal("Bonjour, je voudrais un café, s’il vous plaît.", phrases[0].Text);
        Assert.Equal(8, themes!.Single(t => t.Key == "cafe").PhraseCount);
    }

    [Fact]
    public async Task Phrase_personnelle_n_est_visible_que_de_son_auteur()
    {
        var author = await factory.CreateUserClientAsync();
        var other = await factory.CreateUserClientAsync();
        var id = Guid.NewGuid().ToString("N");

        var created = await author.PutAsJsonAsync($"/api/me/phrases/{id}", new CustomPhraseRequest("Je préfère le thé.", "cafe", null));
        var updated = await author.PutAsJsonAsync($"/api/me/phrases/{id}", new CustomPhraseRequest("Je préfère le thé vert.", "cafe", null));
        var stolen = await other.PutAsJsonAsync($"/api/me/phrases/{id}", new CustomPhraseRequest("Piratage", "cafe", null));

        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        Assert.Equal(HttpStatusCode.OK, updated.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, stolen.StatusCode);
        var mine = await author.GetFromJsonAsync<List<PhraseDto>>("/api/phrases?theme=cafe");
        var theirs = await other.GetFromJsonAsync<List<PhraseDto>>("/api/phrases?theme=cafe");
        Assert.Contains(mine!, p => p.Id == id && p.Text == "Je préfère le thé vert." && p.IsCustom);
        Assert.DoesNotContain(theirs!, p => p.Id == id);

        await author.DeleteAsync($"/api/me/phrases/{id}");
        Assert.DoesNotContain(await author.GetFromJsonAsync<List<PhraseDto>>("/api/phrases?theme=cafe") ?? [], p => p.Id == id);
    }

    // ---------- Favoris ----------

    [Fact]
    public async Task Favoris_sont_idempotents_pour_rejouer_les_changements_hors_ligne()
    {
        var client = await factory.CreateUserClientAsync();

        Assert.Equal(HttpStatusCode.NoContent, (await client.PutAsync("/api/me/favorites/signs/merci", null)).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await client.PutAsync("/api/me/favorites/signs/merci", null)).StatusCode);
        await client.PutAsync("/api/me/favorites/phrases/cafe-emporter", null);
        await client.DeleteAsync("/api/me/favorites/phrases/cafe-emporter");
        Assert.Equal(HttpStatusCode.NoContent, (await client.DeleteAsync("/api/me/favorites/phrases/cafe-emporter")).StatusCode);

        var favorites = await client.GetFromJsonAsync<FavoritesDto>("/api/me/favorites");
        Assert.Equal(["merci"], favorites!.SignIds);
        Assert.Empty(favorites.PhraseIds);
    }

    [Fact]
    public async Task Favoris_exigent_un_compte_et_un_repere_existant()
    {
        var anonymous = await factory.CreateClient().GetAsync("/api/me/favorites");
        var unknown = await (await factory.CreateUserClientAsync()).PutAsync("/api/me/favorites/signs/inconnu", null);

        Assert.Equal(HttpStatusCode.Unauthorized, anonymous.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, unknown.StatusCode);
    }

    [Fact]
    public async Task Compte_de_demonstration_a_ses_favoris()
    {
        var client = factory.CreateClient();
        var login = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest(SeedCatalog.DemoAccount.Email, SeedCatalog.DemoAccount.Password));
        var auth = await login.Content.ReadFromJsonAsync<AuthResponse>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth!.Token);

        var favorites = await client.GetFromJsonAsync<FavoritesDto>("/api/me/favorites");
        Assert.Equal(SeedCatalog.DemoAccount.FavoriteSignIds, favorites!.SignIds);
    }

    // ---------- Hors ligne ----------

    [Fact]
    public async Task Packs_listent_leur_contenu_et_leur_taille()
    {
        var packs = await factory.CreateClient().GetFromJsonAsync<List<PackSummaryDto>>("/api/packs");

        Assert.Equal(["essentiels", "quotidien", "sante"], packs!.Select(p => p.Id));
        var essentials = packs[0];
        Assert.Equal((4, 4), (essentials.SignCount, essentials.PhraseCount));
        Assert.True(essentials.SizeBytes > 200_000, "La taille doit inclure les images des repères.");
    }

    [Fact]
    public async Task Contenu_d_un_pack_porte_un_ETag_et_renvoie_304_s_il_est_a_jour()
    {
        var client = factory.CreateClient();
        var first = await client.GetAsync("/api/packs/sante");
        var content = await first.Content.ReadFromJsonAsync<PackContentDto>();

        var again = new HttpRequestMessage(HttpMethod.Get, "/api/packs/sante");
        again.Headers.IfNoneMatch.Add(first.Headers.ETag!);
        var second = await client.SendAsync(again);

        Assert.Equal(["aide", "svp", "merci"], content!.Signs.Select(s => s.Id));
        Assert.Equal(3, content.Phrases.Count);
        Assert.Equal(HttpStatusCode.NotModified, second.StatusCode);
    }
}
