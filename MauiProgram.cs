using DiaryHelper.Services.Implementations;
using DiaryHelper.Services.Interfaces;
using DiaryHelper.ViewModels;
using DiaryHelper.Views;
using Microsoft.Extensions.Logging;

namespace DiaryHelper;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		var builder = MauiApp.CreateBuilder();
		builder
			.UseMauiApp<App>()
			.ConfigureFonts(fonts =>
			{
				fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
				fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
			});

#if DEBUG
		builder.Logging.AddDebug();
#endif

		// Infrastructure
		builder.Services.AddSingleton(new HttpClient());

		// Application Services
		builder.Services.AddSingleton<ISettingsService, SettingsService>();
		builder.Services.AddSingleton<IDatabaseService, DatabaseService>();
		builder.Services.AddSingleton<ITranslateService, GoogleTranslateService>();
		builder.Services.AddSingleton<IMistralService, MistralService>();

		// ViewModels
		builder.Services.AddTransient<WelcomeSettingsViewModel>();
		builder.Services.AddTransient<DiaryEntryViewModel>();
		builder.Services.AddTransient<HistoryViewModel>();

		// Views
		builder.Services.AddTransient<WelcomeSettingsPage>();
		builder.Services.AddTransient<DiaryEntryPage>();
		builder.Services.AddTransient<HistoryPage>();

		return builder.Build();
	}
}
