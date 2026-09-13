using DiaryHelper.ViewModels;

namespace DiaryHelper.Views;

public partial class WelcomeSettingsPage : ContentPage
{
    private readonly WelcomeSettingsViewModel _viewModel;

    public WelcomeSettingsPage(WelcomeSettingsViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.InitializeAsync();
    }

    protected override async void OnDisappearing()
    {
        base.OnDisappearing();
        await _viewModel.AutoSaveAsync();
    }
}
