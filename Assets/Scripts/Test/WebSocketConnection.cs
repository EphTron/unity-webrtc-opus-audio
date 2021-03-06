using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NativeWebSocket;

public class WebSocketConnection : MonoBehaviour
{
    public enum JsonType
    {
        SDP,
        ICE
    }

    private class SdpMessage
    {
        public SdpMessage(string sdpType, string sdpContent)
        {
            type = sdpType;
            sdp = sdpContent;
        }

        public string type = "unknown";
        public string sdp;
    }

    private WebSocket websocket;
    public string address;

    // Start is called before the first frame update
    void Start()
    {
        string wsAddress = address;

        websocket = new WebSocket(wsAddress);

        websocket.OnOpen += () =>
        {
            Debug.Log("WS: Connection open!");
        };

        websocket.OnError += (e) =>
        {
            Debug.Log("WS: Error!" + e);
        };

        websocket.OnClose += (e) =>
        {
            Debug.Log("WS: Connection closed!");
        };

        websocket.OnMessage += (bytes) =>
        {
            var message = System.Text.Encoding.UTF8.GetString(bytes);
            Debug.Log("WS: OnMessage!" + message);
        };
    }

    // Update is called once per frame
    void Update()
    {
#if !UNITY_WEBGL || UNITY_EDITOR
        websocket.DispatchMessageQueue();
#endif
    }

    public async void Connect()
    {
        // waiting for messages
        await websocket.Connect();
    }

    public async void Close()
    {
        // waiting for messages
        await websocket.Close();
    }

    public async void Receive()
    {
        // waiting for messages
        await websocket.Receive();
    }

    public async void SendMessage(string msg)
    {
        if (websocket.State == WebSocketState.Open)
        {
            await websocket.SendText(msg);
            Debug.Log("WS: Sent Message" + msg);
        }
    }

    public async void SendJson(string msg, JsonType jsonType)
    {
        string json = "";
        if (jsonType == JsonType.SDP)
        {
            SdpMessage offer = new SdpMessage("offer", msg);
            json = JsonUtility.ToJson(offer, true);
        } else if (jsonType == JsonType.ICE)
        {
            json = "ICE NOT IMPLEMENTED";
        }


        if (websocket.State == WebSocketState.Open)
        {
            await websocket.SendText(json);
            Debug.Log("WS: Sent Offer " + json);
        }
        
    }

    private async void OnApplicationQuit()
    {
        await websocket.Close();
    }

}
