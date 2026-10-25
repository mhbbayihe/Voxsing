using VoxSign.Api.Data;
using VoxSign.Shared;

namespace VoxSign.Api.Services;

public static class Mapping
{
    public const string SignImagesPath = "/media/signs";

    /// <summary>URL absolue du dossier d'images, d'après la requête en cours.</summary>
    public static string ImageBaseUrl(this HttpRequest request) => $"{request.Scheme}://{request.Host}{request.PathBase}{SignImagesPath}";

    public static SignDto ToDto(this Sign sign, string imageBaseUrl) => new(
        sign.Id,
        sign.Word,
        sign.CategoryKey,
        sign.Category?.Label ?? sign.CategoryKey,
        sign.ImageKey,
        $"{imageBaseUrl}/{sign.ImageKey}.jpg",
        sign.Usage,
        sign.UsageDescription,
        sign.Example,
        sign.Keywords);

    public static PhraseDto ToDto(this Phrase phrase) => new(
        phrase.Id,
        phrase.Text,
        phrase.ThemeKey,
        phrase.Theme?.Label ?? phrase.ThemeKey,
        phrase.Intent,
        phrase.Context,
        IsCustom: phrase.OwnerId is not null);

    public static UserDto ToDto(this User user) => new(
        user.Id,
        user.FirstName,
        user.Email,
        user.SignLanguage,
        new SpeechPreferencesDto(user.SpeechLocale, user.SpeechPitch, user.SpeechVolume));
}
