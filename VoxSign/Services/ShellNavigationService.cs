using VoxSign.Controls;

namespace VoxSign.Services;

public class ShellNavigationService : INavigationService
{
    public Task GoToAsync(string route, IDictionary<string, object>? parameters = null) =>
        parameters is null
            ? Shell.Current.GoToAsync(route)
            : Shell.Current.GoToAsync(route, parameters);

    public Task GoBackAsync() => Shell.Current.GoToAsync("..");

    public Task GoToTabAsync(NavTab tab) => Shell.Current.GoToAsync(tab switch
    {
        NavTab.Dictionnaire => $"//{Routes.Dictionary}",
        NavTab.Favoris => $"//{Routes.Favorites}",
        NavTab.Echanges => $"//{Routes.Chat}",
        _ => $"//{Routes.Home}",
    });
}
