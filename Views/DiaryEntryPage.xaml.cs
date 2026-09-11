using DiaryHelper.ViewModels;

namespace DiaryHelper.Views;

public partial class DiaryEntryPage : ContentPage
{
    private readonly DiaryEntryViewModel _viewModel;

    public DiaryEntryPage(DiaryEntryViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.InitializeAsync();
    }
}
