﻿// Copyright (c) VolcanicArts. Licensed under the GPL-3.0 License.
// See the LICENSE file in the repository root for full license text.

using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using Valve.VR;
using VRCOSC.App.SDK.OVR.Device;
using VRCOSC.App.SDK.OVR.Metadata;

namespace VRCOSC.App.SDK.OVR;

public class OVRClient
{
    private static readonly string vrpath_file = Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "openvr", "openvrpaths.vrpath");
    private static readonly uint vrevent_t_size = (uint)Unsafe.SizeOf<VREvent_t>();

    internal Action? OnShutdown;

    public bool HasInitialised { get; private set; }

    public readonly OVRSystem System;
    public readonly OVRInput Input;
    internal OVRMetadata? Metadata;

    public HMD HMD => System.HMD;
    public Controller LeftController => System.LeftController;
    public Controller RightController => System.RightController;
    public IEnumerable<Tracker> Trackers => System.Trackers;

    internal OVRClient()
    {
        System = new OVRSystem();
        Input = new OVRInput(this);
    }

    internal void SetMetadata(OVRMetadata metadata)
    {
        Metadata = metadata;
    }

    internal void Init()
    {
        if (HasInitialised || Metadata is null) return;
        if (!File.Exists(vrpath_file)) return;

        if (!OVRHelper.InitialiseOpenVR(Metadata.ApplicationType)) return;

        OpenVR.Applications.AddApplicationManifest(Metadata.ApplicationManifest, false);
        System.Init();
        Input.Init();

        HasInitialised = true;
    }

    internal void Update()
    {
        if (!HasInitialised) return;

        pollEvents();

        if (!HasInitialised) return;

        System.Update();
        Input.Update();
    }

    private void pollEvents()
    {
        var evenT = new VREvent_t();

        while (OpenVR.System.PollNextEvent(ref evenT, vrevent_t_size))
        {
            var eventType = (EVREventType)evenT.eventType;

            switch (eventType)
            {
                case EVREventType.VREvent_Quit:
                    OpenVR.System.AcknowledgeQuit_Exiting();
                    shutdown();
                    return;
            }
        }
    }

    public void TriggerLeftControllerHaptic(float durationSeconds, float frequency, float amplitude) => OVRHelper.TriggerHaptic(Input.LeftControllerHapticActionHandle, System.LeftController.Id, durationSeconds, frequency, amplitude);
    public void TriggerRightControllerHaptic(float durationSeconds, float frequency, float amplitude) => OVRHelper.TriggerHaptic(Input.RightControllerHapticActionHandle, System.RightController.Id, durationSeconds, frequency, amplitude);

    /// <summary>
    /// Checks to see if the user is wearing their headset
    /// </summary>
    public bool IsUserPresent()
    {
        if (!HasInitialised || !HMD.IsConnected) return false;

        return OpenVR.System.GetTrackedDeviceActivityLevel(HMD.Id) == EDeviceActivityLevel.k_EDeviceActivityLevel_UserInteraction;
    }

    /// <summary>
    /// Checks to see if the dashboard is visible
    /// </summary>
    public bool IsDashboardVisible()
    {
        if (!HasInitialised) return false;

        return OpenVR.Overlay.IsDashboardVisible();
    }

    private void shutdown()
    {
        OpenVR.Shutdown();
        HasInitialised = false;
        OnShutdown?.Invoke();
    }

    internal void SetAutoLaunch(bool value)
    {
        if (!HasInitialised) return;

        OpenVR.Applications.SetApplicationAutoLaunch("volcanicarts.vrcosc", value);
    }
}
