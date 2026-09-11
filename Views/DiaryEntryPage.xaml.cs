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

    private void OnRequestScrollToIndex(int index)
    {
        MainThread.BeginInvokeOnMainThread(async () =>
        {
            // Give UI layout a moment to render the newly added sentence card
            await Task.Delay(150);
            try
            {
                if (_viewModel.Sentences.Count > index && index >= 0)
                {
                    SentencesCollectionView.ScrollTo(index, position: ScrollToPosition.End, animate: true);
                }
            }
            catch
            {
                // ignore if view is detached
            }
        });
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        _viewModel.RequestScrollToIndex -= OnRequestScrollToIndex;
        _viewModel.RequestScrollToIndex += OnRequestScrollToIndex;
        await _viewModel.InitializeAsync();
    }

    protected override bool OnBackButtonPressed()
    {
        _viewModel.GoBackCommand.Execute(null);
        return true;
    }

    protected override async void OnDisappearing()
    {
        base.OnDisappearing();
        _viewModel.RequestScrollToIndex -= OnRequestScrollToIndex;
        await _viewModel.AutoSaveOnExitAsync();
    }
}
