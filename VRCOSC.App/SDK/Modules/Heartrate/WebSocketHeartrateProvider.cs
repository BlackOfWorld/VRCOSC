﻿// Copyright (c) VolcanicArts. Licensed under the GPL-3.0 License.
// See the LICENSE file in the repository root for full license text.

using System;
using System.Threading.Tasks;
using Newtonsoft.Json;
using VRCOSC.App.SDK.Utils;

namespace VRCOSC.App.SDK.Modules.Heartrate;

/// <inheritdoc />
/// <summary>
/// Stub class that provides handling for a <see cref="WebSocketClient"/> and adds virtual methods for <see cref="WebSocketClient"/> management
/// </summary>
public abstract class WebSocketHeartrateProvider : HeartrateProvider
{
    public override bool IsConnected => client?.IsConnected ?? false;

    private WebSocketClient? client;

    protected virtual Uri? WebsocketUri => null;

    /// <inheritdoc />
    /// <summary>
    /// Initialises this <see cref="WebSocketHeartrateProvider"/> and connects to the websocket
    /// <exception cref="InvalidOperationException">If <see cref="Initialise"/> is called while this <see cref="WebSocketHeartrateProvider"/> is already initialised, or if <see cref="WebsocketUri"/> is null</exception>
    /// </summary>
    public override async Task Initialise()
    {
        await base.Initialise();

        if (client is not null) throw new InvalidOperationException("Call Teardown before re-initialising");
        if (WebsocketUri is null) throw new InvalidOperationException("WebsocketUri is null");

        client = new WebSocketClient(WebsocketUri.ToString(), 2000, 5);

        client.OnWsConnected += () =>
        {
            OnWebSocketConnected();
            OnConnected?.Invoke();
        };

        client.OnWsDisconnected += () =>
        {
            OnWebSocketDisconnected();
            OnDisconnected?.Invoke();
        };

        client.OnWsMessage += OnWebSocketMessage;

        await client.ConnectAsync();
    }

    /// <inheritdoc />
    /// <summary>
    /// Tears this <see cref="WebSocketHeartrateProvider"/> down and disconnects the <see cref="WebSocketClient"/> if it's connected
    /// </summary>
    public override async Task Teardown()
    {
        if (client is null || !client.IsConnected) return;

        await client.DisconnectAsync();

        client = null;
    }

    /// <summary>
    /// Sends an object by first serialising it using <see cref="JsonConvert"/>
    /// </summary>
    /// <param name="data">The data to serialise and then send</param>
    protected void SendDataAsJson(object data) => SendData(JsonConvert.SerializeObject(data));

    /// <summary>
    /// Sends a raw string
    /// </summary>
    /// <param name="data">The data to send</param>
    protected void SendData(string data)
    {
        if (!IsConnected) return;

        client?.SendAsync(data);
    }

    protected virtual void OnWebSocketConnected()
    {
    }

    protected virtual void OnWebSocketDisconnected()
    {
    }

    protected virtual void OnWebSocketMessage(string message)
    {
    }
}
