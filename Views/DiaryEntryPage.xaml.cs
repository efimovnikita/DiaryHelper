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
            // Restore full history view when a sentence is confirmed/added
            SetHistoryCollapsed(false);
            try
            {
                SentenceInputEditor.Unfocus();
            }
            catch { }

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

    private void OnInputEditorFocused(object? sender, FocusEventArgs e)
    {
        SetHistoryCollapsed(true);
    }

    private void OnInputEditorUnfocused(object? sender, FocusEventArgs e)
    {
        // When editor loses focus, if there is no draft text and no pending analysis, restore history view
        if (string.IsNullOrWhiteSpace(_viewModel.CurrentInput) && !_viewModel.HasPendingAnalysis)
        {
            SetHistoryCollapsed(false);
        }
    }

    private void OnToggleHistoryTapped(object? sender, TappedEventArgs e)
    {
        bool isCurrentlyCollapsed = HistoryRowDef.Height.Value == 0;
        SetHistoryCollapsed(!isCurrentlyCollapsed);
    }

    private void SetHistoryCollapsed(bool collapsed)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            if (collapsed)
            {
                HistoryRowDef.Height = new GridLength(0);
                SentencesCollectionView.IsVisible = false;
                HistoryToggleBar.IsVisible = _viewModel.Sentences.Count > 0;
                HistoryToggleIcon.Text = "▼";
            }
            else
            {
                HistoryRowDef.Height = new GridLength(1, GridUnitType.Star);
                SentencesCollectionView.IsVisible = true;
                HistoryToggleBar.IsVisible = false;
                HistoryToggleIcon.Text = "▲";
            }
        });
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        _viewModel.RequestScrollToIndex -= OnRequestScrollToIndex;
        _viewModel.RequestScrollToIndex += OnRequestScrollToIndex;
        await _viewModel.InitializeAsync();

        if (_viewModel.Sentences.Count > 0)
        {
            ScrollToBottom(animate: false);
        }
    }

    private void ScrollToBottom(bool animate = false)
    {
        MainThread.BeginInvokeOnMainThread(async () =>
        {
            // Give UI layout a moment to render items
            await Task.Delay(200);
            try
            {
                if (_viewModel.Sentences.Count > 0)
                {
                    SentencesCollectionView.ScrollTo(_viewModel.Sentences.Count - 1, position: ScrollToPosition.End, animate: animate);
                }
            }
            catch
            {
                // ignore if view is detached
            }
        });
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
