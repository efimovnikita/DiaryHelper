using DiaryHelper.Views;

namespace DiaryHelper;

public partial class AppShell : Shell
{
	public AppShell()
	{
		InitializeComponent();

		Routing.RegisterRoute(nameof(DiaryEntryPage), typeof(DiaryEntryPage));
		Routing.RegisterRoute(nameof(HistoryPage), typeof(HistoryPage));
	}
}
