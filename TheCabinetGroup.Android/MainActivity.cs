using System;

using Android.App;
using Android.Content.PM;

using Avalonia;
using Avalonia.Android;
using Avalonia.Media.Fonts;

namespace TheCabinetGroup.Android;

[Activity(
    Label = "The Cabinet Group",
    Theme = "@style/AppTheme",
    Icon = "@drawable/thecabinet",
    MainLauncher = true,
    ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.ScreenSize | ConfigChanges.UiMode)]
public class MainActivity : AvaloniaMainActivity<App>
{
    protected override AppBuilder CustomizeAppBuilder(AppBuilder builder)
    {
        return base.CustomizeAppBuilder(builder)
            .WithInterFont()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFontCollection(new EmbeddedFontCollection(
                    new Uri("fonts:"),
                    new Uri("avares://TheCabinetGroup/Assets/Fonts")));
            });
    }
}
