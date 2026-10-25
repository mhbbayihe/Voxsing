using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using VoxSign.Controls;
using VoxSign.Services;

namespace VoxSign.ViewModels;

public abstract partial class BaseViewModel(INavigationService navigation) : ObservableObject
{
    protected INavigationService Navigation { get; } = navigation;

    [ObservableProperty]
    bool isBusy;

    /// <summary>Chargement des données, à chaque affichage de l'écran (voir <see cref="App"/>).</summary>
    public virtual Task LoadAsync() => Task.CompletedTask;

    /// <summary>Appelé par l'application quand l'écran apparaît ; une erreur de chargement ne doit pas la faire tomber.</summary>
    public async void OnAppearing()
    {
        try
        {
            await LoadAsync();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Chargement de {GetType().Name} impossible : {ex}");
        }
    }

    [RelayCommand]
    Task GoBackAsync() => Navigation.GoBackAsync();

    [RelayCommand]
    Task NavigateTabAsync(NavTab tab) => Navigation.GoToTabAsync(tab);
}
