using System;
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

    public class SdpMessage
    {
        public SdpMessage(string sdpType, string sdpContent)
        {
            type = sdpType;
            sdp = sdpContent;
        }

        public string type = "unknown";
        public string sdp;
    }

    public class IceMessage
    {

        public IceMessage(string candidate, string sdpMid, int? sdpMLineIndex, string fragment)
        {
            type = "icecandidate";
            this.candidate = new IceCandidate();
            this.candidate.candidate = candidate;
            this.candidate.sdpMid = sdpMid;
            this.candidate.sdpMLineIndex = sdpMLineIndex;
            this.candidate.usernameFragment = fragment;
        }

        public string type = "unknown";
        public IceCandidate candidate;
    }

    [Serializable]
    public class IceCandidate
    {
        public string candidate;
        public string sdpMid;
        public int? sdpMLineIndex;
        public string usernameFragment;
    }

    [Serializable]
    public class ClientId
    {
        public string type;
        public string clientId;
    }

    [Serializable]
    public class AlexaAction
    {
        public string type;
        public string action;
        public string skillId;
    }

    [Serializable]
    public class AlexaState
    {
        public string type;
        public string state;
    }

    //{"type":"alexa_action","action":"talk","skillId":"amzn1.ask.skill.dc017425-dbcb-4227-9369-a5a4789482e7"}

    private WebSocket websocket;
    public MetaRealPeer peer;
    public string address;
    public string cliendID;

    //{"type":"clientId","clientId":"amzn1.application-oa2-client.8b7bf426b4374296aafc671e653b2463"}

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
            Debug.Log("Websocket: Receiving answer: " + message);
            SdpMessage sdpJson = JsonUtility.FromJson<SdpMessage>(message);
            //Debug.Log("received: " + sdpJson.type + sdpJson.sdp);
            if (sdpJson.type.Equals("answer"))
            {
                Debug.Log("Add Answer");
                peer.StartAddingAnswer(sdpJson.sdp);
            }
            else if (sdpJson.type.Equals("icecandidate"))
            {
                Debug.Log("Add IceCandidate");
                IceMessage iceJson = JsonUtility.FromJson<IceMessage>(message);
                //Debug.Log("Candy" + iceJson.candidate.candidate + " ADADD" + iceJson.candidate.sdpMid + " ASDADD" + iceJson.candidate.sdpMLineIndex);
                if (iceJson.candidate.sdpMid == null)
                {
                    iceJson.candidate.sdpMid = "0";
                }
                if (iceJson.candidate.sdpMLineIndex == null)
                {
                    iceJson.candidate.sdpMLineIndex = 0;
                }

                peer.AddIceCandidate(iceJson.candidate.candidate, iceJson.candidate.sdpMid, iceJson.candidate.sdpMLineIndex);
            }
            else if (sdpJson.type.Equals("clientId"))
            {
                Debug.Log("Add IceCandidate");
                ClientId cId = JsonUtility.FromJson<ClientId>(message);
                cliendID = cId.clientId;
                peer.StartVoice();
            }
            else if (sdpJson.type.Equals("alexa_state"))
            {
                Debug.Log("Alexa State");
                AlexaState aS = JsonUtility.FromJson<AlexaState>(message);
                Debug.Log("Alexa State: " + aS.state);
                //peer.StartVoice();
                //"type":"alexa_state","state":"thinking"
            }

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

    public async void SendSDPMessage(string type, string sdp)
    {
        if (websocket.State == WebSocketState.Open)
        {
            SdpMessage sdpMsg = new SdpMessage(type, sdp);
            string json = JsonUtility.ToJson(sdpMsg);
            await websocket.SendText(json);
            Debug.Log("WS: Sent Message" + json);
        }
    }

    public async void SendICEMessage(string candidate, string sdpMid, int? sdpMLineIndex, string fragment)
    {
        Debug.Log("Sending ICE");
        if (websocket.State == WebSocketState.Open)
        {
            IceMessage iceMsg = new IceMessage(candidate, sdpMid, sdpMLineIndex, fragment);
            string json = JsonUtility.ToJson(iceMsg);
            await websocket.SendText(json);
            //Debug.Log("WS: Sent Message" + json);
        }
    }

    public async void SendActionMessage(string actionInfo)
    {
        if (websocket.State == WebSocketState.Open)
        {
            AlexaAction action = new AlexaAction();
            action.type = "alexa_action";
            action.action = actionInfo;
            action.skillId = "amzn1.ask.skill.dc017425-dbcb-4227-9369-a5a4789482e7";

            string json = JsonUtility.ToJson(action);
            await websocket.SendText(json);
            Debug.Log("WS: Sent Action" + json);
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
