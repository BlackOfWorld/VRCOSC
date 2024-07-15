// Copyright (c) VolcanicArts. Licensed under the GPL-3.0 License.
// See the LICENSE file in the repository root for full license text.

using System;
using System.Net;
using System.Threading.Tasks;

namespace VRCOSC.App.OSC.Client;

public abstract class OscClient
{
    private readonly OscSender sender = new();
    private readonly OscReceiver receiver = new();

    public Action<OscMessage>? OnMessageSent;
    public Action<OscMessage>? OnMessageReceived;

    public Action<byte[]>? OnDataSent;
    public Action<byte[]>? OnDataReceived;

    protected OscClient()
    {
        receiver.OnRawDataReceived += data =>
        {
            OnDataReceived?.Invoke(data);

            var message = OscDecoder.Decode(data);
            if (message is null) return;

            OnMessageReceived?.Invoke(message);
        };
    }

    public void Initialise(IPEndPoint sendEndpoint, IPEndPoint receiveEndpoint)
    {
        sender.Initialise(sendEndpoint);
        receiver.Initialise(receiveEndpoint);
    }

    public void EnableSend() => sender.Enable();
    public void EnableReceive() => receiver.Enable();

    public void DisableSend() => sender.Disable();
    public Task DisableReceive() => receiver.Disable();

    public void SendValue(string address, object value) => SendValues(address, new[] { value });
    public void SendValues(string address, object[] values) => SendMessage(new OscMessage(address, values));

    public void SendMessage(OscMessage message)
    {
        Send(OscEncoder.Encode(message));
        OnMessageSent?.Invoke(message);
    }

    public void Send(byte[] data)
    {
        sender.Send(data);
        OnDataSent?.Invoke(data);
    }
}
