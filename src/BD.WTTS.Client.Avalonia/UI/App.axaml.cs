using AngleSharp.Dom;
using BD.WTTS.Client.Resources;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using SkiaSharp;
using System.Drawing.Drawing2D;

namespace BD.WTTS.UI;

public sealed partial class App : Application
{
    const string TAG = "AvaApp";

    /// <summary>
    /// 获取当前主窗口
    /// </summary>
    public Window? MainWindow { get; internal set; }

    public static event InitializeHandler? InitializeMainWindow;

    public delegate Window InitializeHandler(object sender);

    /// <summary>
    /// 获取任意第一个窗口，优先返回主窗口
    /// </summary>
    /// <returns></returns>
    public Window? GetFirstOrDefaultWindow()
    {
        var window = MainWindow;
        if (window == null)
        {
            if (ApplicationLifetime is ClassicDesktopStyleApplicationLifetime classicDesktopStyleApplicationLifetime)
            {
                window = classicDesktopStyleApplicationLifetime.Windows.FirstOrDefault(x => x != null);
            }
        }
        return window;
    }

    public override void Initialize()
    {
        try
        {
            AvaloniaXamlLoader.Load(this);
        }
        catch (Exception ex)
        {
            Startup.GlobalExceptionHandler.Handler(ex, "load App.Xaml fail.");
        }
    }

    public override void RegisterServices()
    {
        //if (!Design.IsDesignMode)
        //{
        //    AvaloniaLocator.CurrentMutable
        //        .Bind<IFontManagerImpl>().ToConstant(Ioc.Get<IFontManagerImpl>());
        //}

        base.RegisterServices();
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            MainWindow = InitializeMainWindow?.Invoke(this);
            desktop.MainWindow = MainWindow ??= new MainWindow();
        }
        else if (ApplicationLifetime is ISingleViewApplicationLifetime singleView)
        {
            singleView.MainView = new MainView();
        }
        //SetThemeNotChangeValue(UISettings.Theme.Value);
        //SetThemeAccent(UISettings.UseSystemThemeAccent.Value ? bool.TrueString : UISettings.ThemeAccent.Value);
        base.OnFrameworkInitializationCompleted();
    }

    //#if WINDOWS
    //    /// <inheritdoc cref="IPlatformService.SetDesktopBackgroundToWindow(nint, int, int)"/>
    //    public void SetDesktopBackgroundWindow()
    //    {
    //        //try
    //        //{
    //        //    if (MainWindow is MainWindow window)
    //        //    {
    //        //        IPlatformService.Instance.SetDesktopBackgroundToWindow(
    //        //            window.BackHandle,
    //        //            Convert.ToInt32(window.Width),
    //        //            Convert.ToInt32(window.Height));
    //        //    }
    //        //}
    //        //catch (Exception ex)
    //        //{
    //        //    Log.Error(TAG, ex, "SetDesktopBackgroundToWindow fail.");
    //        //}
    //    }
    //#endif

    /// <summary>
    /// 设置当前打开窗口的 AvaloniaWindow 背景透明材质
    /// </summary>
    /// <param name="level"></param>
    public void SetAllWindowransparencyMateria(WindowBackgroundMaterial level)
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            foreach (var window in desktop.Windows)
            {
                window.TransparencyLevelHint = new WindowTransparencyLevel[] { level.ToWindowTransparencyLevel() };

                //if (window.TransparencyLevelHint == WindowTransparencyLevel.Transparent ||
                //    window.TransparencyLevelHint == WindowTransparencyLevel.Blur)
                //{
                //    ((IPseudoClasses)window.Classes).Set(":transparent", true);
                //}
                //else
                //{
                //    ((IPseudoClasses)window.Classes).Set(":transparent", false);
                //}
            }
        }
    }

    public bool Shutdown(int exitCode = 0)
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            MainThread2.BeginInvokeOnMainThread(() =>
            {
                desktop.Shutdown(exitCode);
            });
            return true;
        }
        return false;
    }

    public void InitSettingSubscribe()
    {
        GeneralSettings.TrayIcon.Subscribe(x => InitTrayIcon());
        UISettings.UseSystemThemeAccent.Subscribe(useSystemThemeAccent
            => SetThemeAccent(useSystemThemeAccent ? bool.TrueString : UISettings.ThemeAccent.Value));
        UISettings.ThemeAccent.Subscribe(color
            => SetThemeAccent(UISettings.UseSystemThemeAccent.Value ? bool.TrueString : UISettings.ThemeAccent.Value),
            !UISettings.UseSystemThemeAccent.Value);

        GeneralSettings.AutoRunOnStartup.Subscribe(IApplication.SetBootAutoStart);

        UISettings.WindowBackgroundMaterial.Subscribe(SetAllWindowransparencyMateria, false);

        //#if WINDOWS
        //        UISettings.EnableDesktopBackground.Subscribe(x =>
        //        {
        //            if (x)
        //            {
        //                //var t = (WindowTransparencyLevel)UISettings.WindowBackgroundMateria.Value;
        //                //if (t == WindowTransparencyLevel.None ||
        //                //    t == WindowTransparencyLevel.Mica)
        //                //{
        //                //    UISettings.EnableDesktopBackground.Value = false;
        //                //    Toast.Show(string.Format(AppResources.Settings_UI_EnableDesktopBackground_Error_, t));
        //                //    return;
        //                //}
        //                SetDesktopBackgroundWindow();
        //            }
        //        }, false);
        //#endif
    }

    static FontFamily GetDefaultFontFamily()
    {
        try
        {
            //var fontFamily = IPlatformService.Instance.GetDefaultFontFamily();
            return FontFamily.Parse(DefaultFontFamilyName);
        }
        catch
        {
        }
        return FontFamily.Default;
    }

    public const string DefaultFontFamilyName = "avares://BD.WTTS.Client.Avalonia/UI/Assets/Fonts/HarmonyOS_Sans_SC_Regular.ttf#HarmonyOS Sans SC";

    static readonly Lazy<FontFamily> _DefaultFontFamily = new(GetDefaultFontFamily);

    public static FontFamily DefaultFontFamily => _DefaultFontFamily.Value;

    public void OpenBrowserCommand(object url)
    {
        try
        {
            if (url == null || string.IsNullOrEmpty(url.ToString()))
            {
                Toast.Show(ToastIcon.Warning, "打开链接失败");
                return;
            }
            Browser2.Open(url.ToString());
        }
        catch (Exception ex)
        {
            Toast.LogAndShowT(ex);
        }
    }

    public async void CopyToClipboardCommand(object text)
    {
        try
        {
            if (text == null || string.IsNullOrEmpty(text.ToString()))
            {
                Toast.Show(ToastIcon.Warning, "复制内容失败");
                return;
            }
            await Clipboard2.SetTextAsync(text.ToString());
            Toast.Show(ToastIcon.Success, Strings.CopyToClipboard);
        }
        catch (Exception ex)
        {
            Toast.LogAndShowT(ex);
        }
    }
}
