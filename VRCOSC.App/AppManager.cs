// Copyright (c) VolcanicArts. Licensed under the GPL-3.0 License.
// See the LICENSE file in the repository root for full license text.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using NAudio.CoreAudioApi;
using Valve.VR;
using VRCOSC.App.Audio;
using VRCOSC.App.Audio.Whisper;
using VRCOSC.App.ChatBox;
using VRCOSC.App.Modules;
using VRCOSC.App.OSC;
using VRCOSC.App.OSC.VRChat;
using VRCOSC.App.Profiles;
using VRCOSC.App.Router;
using VRCOSC.App.SDK.Handlers;
using VRCOSC.App.SDK.OVR;
using VRCOSC.App.SDK.OVR.Metadata;
using VRCOSC.App.SDK.Parameters;
using VRCOSC.App.SDK.VRChat;
using VRCOSC.App.Settings;
using VRCOSC.App.Themes;
using VRCOSC.App.Updater;
using VRCOSC.App.Utils;
using VRCOSC.App.VRChatAPI;
using Module = VRCOSC.App.SDK.Modules.Module;

namespace VRCOSC.App;

public class AppManager
{
#if DEBUG
    public const string APP_NAME = "VRCOSC-V2-Dev";
#else
    public const string APP_NAME = "VRCOSC-V2";
#endif

    private static Version assemblyVersion => Assembly.GetEntryAssembly()?.GetName().Version ?? new Version();
    public static string Version => $"{assemblyVersion.Major}.{assemblyVersion.Minor}.{assemblyVersion.Build}";

    private static AppManager? instance;
    internal static AppManager GetInstance() => instance ??= new AppManager();

    public readonly Storage Storage = new NativeStorage($"{Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)}/{APP_NAME}");

    public Observable<AppManagerState> State = new(AppManagerState.Stopped);

    public VelopackUpdater VelopackUpdater = null!;
    public ConnectionManager ConnectionManager = null!;
    public VRChatOscClient VRChatOscClient = null!;
    public VRChatClient VRChatClient = null!;
    public VRChatAPIClient VRChatAPIClient = null!;
    public OVRClient OVRClient = null!;

    public WhisperSpeechEngine SpeechEngine = null!;

    private Repeater? updateTask;
    private Repeater vrchatCheckTask = null!;
    private Repeater openvrCheckTask = null!;
    private Repeater openvrUpdateTask = null!;

    private readonly Queue<VRChatOscMessage> oscMessageQueue = new();
    private readonly object oscMessageQueueLock = new();
    private readonly AudioEndpointNotificationClient audioNotificationClient = new();

    public AppManager()
    {
        State.Subscribe(newState => Logger.Log("AppManager changed state to " + newState));
    }

    public void Initialise()
    {
        ChangeTheme(SettingsManager.GetInstance().GetValue<Theme>(VRCOSCSetting.Theme));

        //VelopackUpdater = new VelopackUpdater();
        ConnectionManager = new ConnectionManager();
        VRChatOscClient = new VRChatOscClient();
        VRChatClient = new VRChatClient(VRChatOscClient);
        VRChatAPIClient = new VRChatAPIClient();
        OVRClient = new OVRClient();
        ChatBoxWorldBlacklist.Init();

        SpeechEngine = new WhisperSpeechEngine();

        SpeechEngine.OnResult += result => ModuleManager.GetInstance().GetRunningModulesOfType<ISpeechHandler>().ForEach(module =>
        {
            try
            {
                module.OnSpeechResult(result);
            }
            catch (Exception e)
            {
                ExceptionHandler.Handle(e, $"{((Module)module).FullID} experienced an issue calling OnResult");
            }
        });

        audioNotificationClient.DeviceChanged += (flow, role, deviceId) => Application.Current.Dispatcher.Invoke(() =>
        {
            try
            {
                return;

                if (!SettingsManager.GetInstance().GetValue<bool>(VRCOSCSetting.AutoSwitchMicrophone)) return;
                if (flow != DataFlow.Capture) return;
                if (role != Role.Multimedia) return;

                Logger.Log("Automatic microphone change detected. Switching to new microphone");
                // TODO: Figure out why this is erroring only when modules are running
                SettingsManager.GetInstance().GetObservable<string>(VRCOSCSetting.SelectedInputDeviceID).Value = deviceId;
            }
            catch (Exception e)
            {
                ExceptionHandler.Handle(e);
            }
        });

        var chosenInputDeviceSetting = SettingsManager.GetInstance().GetObservable<string>(VRCOSCSetting.SelectedInputDeviceID);

        if (string.IsNullOrEmpty(chosenInputDeviceSetting.Value))
        {
            chosenInputDeviceSetting.Value = WasapiCapture.GetDefaultCaptureDevice().ID;
        }

        AudioDeviceHelper.RegisterCallbackClient(audioNotificationClient);

        OVRClient.SetMetadata(new OVRMetadata
        {
            ApplicationType = EVRApplicationType.VRApplication_Background,
            ApplicationManifest = Storage.GetFullPath("openvr/app.vrmanifest"),
            ActionManifest = Storage.GetFullPath("openvr/action_manifest.json")
        });

        OVRClient.OnShutdown += () => Application.Current.Dispatcher.Invoke(() =>
        {
            if (SettingsManager.GetInstance().GetValue<bool>(VRCOSCSetting.OVRAutoClose))
            {
                Application.Current.Shutdown();
            }
        });

        OVRHelper.OnError += m => Logger.Log($"[OpenVR] {m}");
    }

    public void InitialLoadComplete()
    {
        VRChatOscClient.Init(ConnectionManager);
        ConnectionManager.Init();

        vrchatCheckTask = new Repeater(checkForVRChatAutoStart);
        vrchatCheckTask.Start(TimeSpan.FromSeconds(2));

        openvrCheckTask = new Repeater(checkForOpenVR);
        openvrCheckTask.Start(TimeSpan.FromSeconds(2));

        openvrUpdateTask = new Repeater(() => OVRClient.Update());
        openvrUpdateTask.Start(TimeSpan.FromSeconds(1d / 60d));

        //if (VelopackUpdater.IsUpdateAvailable)
        //    VelopackUpdater.ShowUpdate();
    }

    private void update()
    {
        if (State.Value != AppManagerState.Started) return;

        lock (oscMessageQueueLock)
        {
            processOscMessageQueue();
        }
    }

    private void checkForOpenVR() => Task.Run(() =>
    {
        OVRClient.Init();
        OVRClient.SetAutoLaunch(SettingsManager.GetInstance().GetValue<bool>(VRCOSCSetting.OVRAutoOpen));

        RenderOptions.ProcessRenderMode = OVRClient.HasInitialised ? RenderMode.SoftwareOnly : RenderMode.Default;
    });

    private void checkForVRChatAutoStart()
    {
        if (!VRChatClient.HasOpenStateChanged(out var clientOpenState)) return;

        if (clientOpenState && State.Value == AppManagerState.Stopped && SettingsManager.GetInstance().GetValue<bool>(VRCOSCSetting.VRCAutoStart)) RequestStart();
        if (!clientOpenState && State.Value == AppManagerState.Started && SettingsManager.GetInstance().GetValue<bool>(VRCOSCSetting.VRCAutoStop)) _ = StopAsync();
    }

    #region OSC

    private void onParameterReceived(VRChatOscMessage message)
    {
        lock (oscMessageQueueLock)
        {
            oscMessageQueue.Enqueue(message);
        }
    }

    private void processOscMessageQueue()
    {
        while (oscMessageQueue.TryDequeue(out var message))
        {
            if (message.IsAvatarChangeEvent)
            {
                VRChatClient.HandleAvatarChange(message);

                if (ProfileManager.GetInstance().AvatarChange((string)message.ParameterValue)) continue;

                sendControlParameters();
            }

            if (message.IsAvatarParameter)
            {
                var wasPlayerUpdated = VRChatClient.Player.Update(message.ParameterName, message.ParameterValue);
                if (wasPlayerUpdated) ModuleManager.GetInstance().PlayerUpdate();

                if (message.ParameterName.StartsWith("VRCOSC/Controls")) handleControlParameter(new ReceivedParameter(message.ParameterName, message.ParameterValue));
            }

            ModuleManager.GetInstance().ParameterReceived(message);
        }
    }

    private void handleControlParameter(ReceivedParameter parameter)
    {
        switch (parameter.Name)
        {
            case "VRCOSC/Controls/ChatBox/Enabled" when parameter.IsValueType<bool>():
                ChatBoxManager.GetInstance().SendEnabled = parameter.GetValue<bool>();
                break;
        }
    }

    private void sendControlParameters()
    {
        VRChatOscClient.SendValue($"{VRChatOscConstants.ADDRESS_AVATAR_PARAMETERS_PREFIX}VRCOSC/Controls/ChatBox/Enabled", ChatBoxManager.GetInstance().SendEnabled);
    }

    #endregion

    #region Start

    private CancellationTokenSource requestStartCancellationSource = null!;

    public async void ForceStart()
    {
        Logger.Log("Force starting");
        CancelStartRequest();
        initialiseOSCClient(IPAddress.Loopback, 9000, IPAddress.Loopback, 9001);
        await startAsync();
    }

    public void CancelStartRequest()
    {
        requestStartCancellationSource.Cancel();
        State.Value = AppManagerState.Stopped;
    }

    public async void RequestStart()
    {
        if (State.Value is AppManagerState.Waiting or AppManagerState.Starting or AppManagerState.Started) return;

        requestStartCancellationSource = new CancellationTokenSource();

        if (SettingsManager.GetInstance().GetValue<bool>(VRCOSCSetting.UseCustomEndpoints))
        {
            var outgoingEndpoint = SettingsManager.GetInstance().GetValue<string>(VRCOSCSetting.OutgoingEndpoint);
            var outgoingAddress = IPAddress.Parse(outgoingEndpoint.Split(':')[0]);
            var outgoingPort = int.Parse(outgoingEndpoint.Split(':')[1]);

            var incomingEndpoint = SettingsManager.GetInstance().GetValue<string>(VRCOSCSetting.IncomingEndpoint);
            var incomingAddress = IPAddress.Parse(incomingEndpoint.Split(':')[0]);
            var incomingPort = int.Parse(incomingEndpoint.Split(':')[1]);

            initialiseOSCClient(outgoingAddress, outgoingPort, incomingAddress, incomingPort);
            await startAsync();
            return;
        }

        State.Value = AppManagerState.Waiting;
        await Task.Run(waitForStart, requestStartCancellationSource.Token);
    }

    private async Task waitForStart()
    {
        Logger.Log("Waiting for starting conditions");

        var waitingCancellationSource = new CancellationTokenSource();

        await Task.WhenAny(new[]
        {
            Task.Run(() => waitForUnity(waitingCancellationSource), requestStartCancellationSource.Token),
            Task.Run(() => waitForVRChat(waitingCancellationSource), requestStartCancellationSource.Token)
        });

        waitingCancellationSource.Cancel();

        if (requestStartCancellationSource.IsCancellationRequested) return;

        if (isVRChatOpen())
        {
            Logger.Log("Found VRChat. Waiting for OSCQuery");
            ConnectionManager.Reset();
            await waitForOSCQuery();
            if (requestStartCancellationSource.IsCancellationRequested) return;

            initialiseOSCClient(IPAddress.Loopback, ConnectionManager.VRChatReceivePort!.Value, IPAddress.Loopback, ConnectionManager.VRCOSCReceivePort);
        }
        else
        {
            if (isUnityOpen())
            {
                Logger.Log("Found Unity");
                initialiseOSCClient(IPAddress.Loopback, 9000, IPAddress.Loopback, 9001);
            }
        }

        await startAsync();
    }

    private static bool isVRChatOpen() => Process.GetProcessesByName("vrchat").Any();
    private static bool isUnityOpen() => Process.GetProcessesByName("unity").Any();

    private async Task waitForUnity(CancellationTokenSource waitingSource)
    {
        while (!isUnityOpen() && !requestStartCancellationSource.IsCancellationRequested && !waitingSource.IsCancellationRequested)
        {
            await Task.Delay(500);
        }
    }

    private async Task waitForVRChat(CancellationTokenSource waitingSource)
    {
        while (!isVRChatOpen() && !requestStartCancellationSource.IsCancellationRequested && !waitingSource.IsCancellationRequested)
        {
            await Task.Delay(500);
        }
    }

    private async Task waitForOSCQuery()
    {
        while (!ConnectionManager.IsConnected && !requestStartCancellationSource.IsCancellationRequested)
        {
            await Task.Delay(500);
        }
    }

    private async Task startAsync()
    {
        State.Value = AppManagerState.Starting;

        VRChatLogReader.Start();
        RouterManager.GetInstance().Start();
        VRChatOscClient.EnableSend();
        ChatBoxManager.GetInstance().Start();
        await ModuleManager.GetInstance().StartAsync();

        updateTask = new Repeater(update);
        updateTask.Start(TimeSpan.FromSeconds(1d / 60d));

        VRChatOscClient.OnParameterReceived += onParameterReceived;
        VRChatOscClient.EnableReceive();

        if (ModuleManager.GetInstance().GetRunningModulesOfType<ISpeechHandler>().Any())
            SpeechEngine.Initialise();

        State.Value = AppManagerState.Started;

        sendControlParameters();
    }

    private void initialiseOSCClient(IPAddress sendAddress, int sendPort, IPAddress receiveAddress, int receivePort)
    {
        try
        {
            var sendEndpoint = new IPEndPoint(sendAddress, sendPort);
            var receiveEndpoint = new IPEndPoint(receiveAddress, receivePort);

            Logger.Log($"Initialising OSC with send {sendEndpoint} and receive {receiveEndpoint}");

            VRChatOscClient.Initialise(sendEndpoint, receiveEndpoint);
        }
        catch (Exception e)
        {
            ExceptionHandler.Handle(e, $"{nameof(AppManager)} experienced an exception", true);
        }
    }

    #endregion

    #region Restart

    public async Task RestartAsync()
    {
        await StopAsync();
        await Task.Delay(200);
        RequestStart();
    }

    #endregion

    #region Stop

    public async Task StopAsync()
    {
        if (State.Value is AppManagerState.Stopping or AppManagerState.Stopped) return;

        State.Value = AppManagerState.Stopping;

        SpeechEngine.Teardown();

        VRChatLogReader.Stop();
        await VRChatOscClient.DisableReceive();
        VRChatOscClient.OnParameterReceived -= onParameterReceived;

        if (updateTask is not null)
            await updateTask.StopAsync();

        await ModuleManager.GetInstance().StopAsync();
        ChatBoxManager.GetInstance().Stop();
        VRChatClient.Teardown();
        VRChatOscClient.DisableSend();
        RouterManager.GetInstance().Stop();

        lock (oscMessageQueueLock)
        {
            oscMessageQueue.Clear();
        }

        State.Value = AppManagerState.Stopped;
    }

    #endregion

    #region Profiles

    public async void ChangeProfile(Profile newProfile)
    {
        var currentProfile = ProfileManager.GetInstance().ActiveProfile.Value;
        if (currentProfile == newProfile) return;

        Debug.Assert(currentProfile is not null);
        Debug.Assert(newProfile is not null);

        Logger.Log($"Changing profile from {currentProfile.Name.Value} ({currentProfile.ID}) to {newProfile.Name.Value} ({newProfile.ID})");

        foreach (Window window in Application.Current.Windows)
        {
            if (window != MainWindow.GetInstance()) window.Close();
        }

        var beforeState = State.Value;

        if (State.Value == AppManagerState.Started)
        {
            await StopAsync();
        }

        ChatBoxManager.GetInstance().Unload();
        ModuleManager.GetInstance().UnloadAllModules();

        ProfileManager.GetInstance().ActiveProfile.Value = newProfile;

        ModuleManager.GetInstance().LoadAllModules();
        ChatBoxManager.GetInstance().Load();
        RouterManager.GetInstance().Load();

        if (beforeState == AppManagerState.Started)
        {
            await Task.Delay(100);
            await startAsync();
        }
    }

    #endregion

    #region Themes

    public void ChangeTheme(Theme theme)
    {
        var mergedDictionaries = Application.Current.Resources.MergedDictionaries;
        mergedDictionaries.Clear();

        switch (theme)
        {
            case Theme.Dark:
                mergedDictionaries.Add(new ResourceDictionary { Source = new Uri("pack://application:,,,/VRCOSC.App;component/Themes/Dark.xaml") });
                break;

            case Theme.Light:
                mergedDictionaries.Add(new ResourceDictionary { Source = new Uri("pack://application:,,,/VRCOSC.App;component/Themes/Light.xaml") });
                break;

            default:
                throw new ArgumentOutOfRangeException(nameof(theme), theme, null);
        }
    }

    #endregion
}

public enum AppManagerState
{
    Waiting,
    Starting,
    Started,
    Stopping,
    Stopped
}
