namespace VoxSign.Services.Api;

public static class ApiSettings
{
    const string BaseUrlKey = "api_base_url";

    /// <summary>
    /// Adresse de l'API. Par défaut, l'API de développement lancée sur le poste
    /// (l'émulateur Android joint le poste par 10.0.2.2). Modifiable sans recompiler via les préférences.
    /// </summary>
    public static string BaseUrl
    {
        get => Preferences.Default.Get(BaseUrlKey, Default);
        set => Preferences.Default.Set(BaseUrlKey, value);
    }

    static string Default => DeviceInfo.Platform == DevicePlatform.Android
        ? "http://10.0.2.2:5180/"
        : "http://localhost:5180/";
}
