// ReSharper disable once CheckNamespace
namespace BD.WTTS;

partial class Startup // OnStartup
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    static void InitVisualStudioAppCenterSDK()
    {

    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ShowSettingsModifiedRestartThisSoft()
    {
        if (Ioc.Get_Nullable<IToastIntercept>() is StartupToastIntercept intercept
            && !intercept.IsStartuped)
        {
            return;
        }
        Toast.Show(ToastIcon.Info, Strings.SettingsModifiedRestartThisSoft);
    }

    public virtual void InitSettingSubscribe()
    {
        var a = IApplication.Instance;

        UISettings.Theme.Subscribe(x => a.Theme = x);
        UISettings.Language.Subscribe(ResourceService.ChangeLanguage);

        GeneralSettings.GPU.Subscribe(x =>
        {
            //if (x.HasValue) // null 为默认值时不提示
            ShowSettingsModifiedRestartThisSoft();
        });
        GeneralSettings.PluginSafeMode.Subscribe(x =>
        {
            //if (x.HasValue) // null 为默认值时不提示
            ShowSettingsModifiedRestartThisSoft();
        });

#if (WINDOWS || MACCATALYST || MACOS || LINUX) && !(IOS || ANDROID)
        if (string.IsNullOrWhiteSpace(SteamSettings.SteamProgramPath.Value))
        {
            SteamSettings.SteamProgramPath.Default =
                Ioc.Get<ISteamService>().SteamProgramPath;
        }
#endif
    }

    public virtual void OnStartup()
    {
        StartupToastIntercept.OnStartuped();

#if STARTUP_WATCH_TRACE || DEBUG
        WatchTrace.Start();
#endif
        InitVisualStudioAppCenterSDK();
#if STARTUP_WATCH_TRACE || DEBUG
        WatchTrace.Record("VisualStudioAppCenter");
#endif

        if (IsMainProcess)
        {
            Task2.InBackground(async () =>
            {
                await ActiveUserRecordAsync(ActiveUserAnonymousStatisticType.OnStartup);
            });
            if (GeneralSettings.AutoCheckAppUpdate.Value)
            {
                Task2.InBackground(async () =>
                {
                    await IAppUpdateService.Instance
                        .CheckUpdateAsync(showIsExistUpdateFalse: false);
                });
            }
#if WINDOWS || LINUX || MACOS
            if (IsSteamRun)
            {
                try
                {
                    Steamworks.Dispatch.OnException = (e) =>
                    {
                        Console.Error.WriteLine(e.Message);
                        Console.Error.WriteLine(e.StackTrace);
                        Log.Error(nameof(Steamworks), e, "Steamworks.SteamClient OnException.");
                    };

                    // Init Client
                    Steamworks.SteamClient.Init(2425030);

                    if (Steamworks.SteamClient.IsValid)
                    {
                        Steamworks.SteamFriends.SetRichPresence("steam_display", "#Status_AtMainMenu");
                        //var r = Steamworks.SteamFriends.GetRichPresence("steam_display");
                    }
                }
                catch (Exception ex)
                {
                    Log.Error(nameof(Steamworks), ex, "Steamworks.SteamClient Init");
                }
            }
#endif 
        }
#if DEBUG
        DebugConsole.WriteInfo();
#endif

#if STARTUP_WATCH_TRACE || DEBUG
        WatchTrace.Stop();
#endif
    }

    protected abstract ActiveUserRecordDTO GetActiveUserRecord();

    [MethodImpl(MethodImplOptions.NoInlining)]
    async Task ActiveUserRecordAsync(ActiveUserAnonymousStatisticType type)
    {
        if (!IsMainProcess)
            return;

        try
        {
            var userService = UserService.Current;
            var isAuthenticated = userService.IsAuthenticated;
            var csc = IMicroServiceClient.Instance;
            if (isAuthenticated)
            {
                // 刷新用户信息
                var rspRUserInfo = await csc.Manage.RefreshUserInfo();
                if (rspRUserInfo.IsSuccess && rspRUserInfo.Content != null)
                {
                    await userService.SaveUserAsync(rspRUserInfo.Content);
                }
            }

            var request = GetActiveUserRecord();
            request.Type = type;
            request.IsAuthenticated = isAuthenticated;
            await csc.ActiveUser.Record(request);
        }
        catch (Exception ex)
        {
            GlobalExceptionHandler.Handler(ex, nameof(ActiveUserRecordAsync));
        }
    }
}