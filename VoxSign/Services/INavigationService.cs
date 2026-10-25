using VoxSign.Controls;

namespace VoxSign.Services;

public interface INavigationService
{
    Task GoToAsync(string route, IDictionary<string, object>? parameters = null);

    Task GoBackAsync();

    Task GoToTabAsync(NavTab tab);
}
