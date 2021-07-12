using System;
using System.Collections;
using System.Text;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.WebRTC;

public class MetaRealPeer : MonoBehaviour
{
   

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
        public IceMessage(string sdpType, string sdpContent)
        {
            type = sdpType;
            sdp = sdpContent;
        }

        public string type = "unknown";
        public string sdp;
    }

    public WebSocketConnection websocket;
    public Button connectWebsocketButton;
    public Button connectWebRTCButton;
    public Button connectVoiceButton;

    public MyEncoder encoder;
    public MyDecoder decoder;

    private bool isConnected;
    public TMP_Text statusText;
    public TMP_Text infoText;
    public byte[] buffer;

    private RTCPeerConnection localPeer;
    private RTCDataChannel sendChannel, receiveChannel;
    //private List<RTCRtpSender> localSenders;
    //private MediaStream audioStream;

    private DelegateOnIceConnectionChange localPeerOnIceConnectionChange;
    private DelegateOnIceCandidate localPeerOnIceCandidate;
    private DelegateOnNegotiationNeeded localPeerOnNegotiationNeeded;

    private DelegateOnMessage onDataChannelMessage;
    private DelegateOnOpen onSendChannelOpen;
    private DelegateOnOpen onReceiveChannelOpen;
    private DelegateOnClose onSendChannelClose;
    private DelegateOnClose onReceiveChannelClose;
    private DelegateOnDataChannel onDataChannel;
    private DelegateOnMessage onReceiveChannelMessage;
    private DelegateOnDataChannel onReceiveDataChannel;

    public List<string> iceCandidates = new List<string>();
    public IceList iceList;

    private RTCOfferOptions _offerOptions = new RTCOfferOptions
    {
        iceRestart = false,
        offerToReceiveAudio = true,
        offerToReceiveVideo = false
    };

    private RTCAnswerOptions _answerOptions = new RTCAnswerOptions
    {
        iceRestart = false,

    };

    [Serializable]
    public class IceCandidate
    {
        public IceCandidate(string candidate, string sdpMid, int? sdpMLineIndex)
        {
            this.candidate = candidate;
            this.sdpMid = sdpMid;
            this.sdpMLineIndex = sdpMLineIndex;
        }

        public string candidate = "unknown";
        public string sdpMid;
        public int? sdpMLineIndex;
    }

    [Serializable]
    public class IceList
    {
        public List<IceCandidate> iceList;
    }

    void OnEnable()
    {
        encoder.OnEncoded += EncodeAndSendBytes;
    }

    void OnDisable()
    {
        encoder.OnEncoded -= EncodeAndSendBytes;
    }

    private void Awake()
    {
        // Initialize WebRTC
        WebRTC.Initialize();
    }

    private void OnDestroy()
    {
        WebRTC.Dispose();
    }

    // Start is called before the first frame update
    void Start()
    {
        Debug.Log("starting this script");
        iceList.iceList = new List<IceCandidate>();
        //localSenders = new List<RTCRtpSender>();

        localPeerOnIceConnectionChange = state => { OnIceConnectionChange(state); };
        localPeerOnIceCandidate = candidate => { UpdateIceCandidates(candidate); };

        onReceiveChannelMessage = bytes => {
            Decode(bytes);
        };
        onDataChannel = channel =>
        {
            Debug.Log("Receiving");
            receiveChannel = channel;
            receiveChannel.OnMessage = onDataChannelMessage;
            receiveChannel.OnOpen = onReceiveChannelOpen;
            receiveChannel.OnClose = onReceiveChannelClose;
        };

        onDataChannelMessage = bytes => { Debug.Log(System.Text.Encoding.UTF8.GetString(bytes)); };
        onSendChannelOpen = () => { Debug.Log("SendChannel Opened"); };
        onSendChannelOpen = () => { Debug.Log("SendChannel Opened"); };
        onReceiveChannelOpen = () => { Debug.Log("ReceiveChannel Opened"); };
        onReceiveChannelClose = () => { Debug.Log("ReceiveChannel Closed"); };
        //localPeerOnNegotiationNeeded = () => { StartCoroutine(PeerOnNegotiationNeeded()); };
        // connectWebRTCButton.onClick.AddListener(() => { StartCoroutine(CreateOffer()); });
        //connectVoiceButton.onClick.AddListener(() => { StartVoice(); });
        
    }

    void Decode(byte[] bytes)
    {
        Debug.Log("decoding");
        int size = sizeof(int);
        byte[] encodedLengthBytes = bytes.Take(size).ToArray();
        byte[] encodedAudioBytes = bytes.Skip(sizeof(int)).Take(bytes.Length - sizeof(int)).ToArray();
        int length = DecodeLength(encodedLengthBytes);

        decoder.Decode(encodedAudioBytes, length);
    }

    int DecodeLength(byte[] bytes)
    {
        int result = BitConverter.ToInt32(bytes, 0);
        infoText.text = Encoding.UTF8.GetString(bytes);
        return result;
    }

    byte[] EncodeLength(byte[] bytes, int length)
    {
        int[] lengthArr = new int[] { length };
        byte[] result = new byte[lengthArr.Length * sizeof(int)];
        Buffer.BlockCopy(lengthArr, 0, result, 0, result.Length);
        return AddByteToArray(bytes, result);
    }

    public byte[] AddByteToArray(byte[] bArray, byte[] newBytes)
    {
        byte[] newArray = new byte[bArray.Length + newBytes.Length];
        bArray.CopyTo(newArray, newBytes.Length);
        newBytes.CopyTo(newArray, 0);
        return newArray;
    }

    void EncodeAndSendBytes(byte[] data, int length)
    {
        if (isConnected)
        {
            Debug.Log("sending" + data.Length + "  "+length);
            //buffer = EncodeLength(data, length);
            //sendChannel.Send(buffer);
            sendChannel.Send(data);
        }
    }

    public void ConnectToRTC(){
        StartCoroutine(CreateOffer());
    }

    public void StartVoice()
    {
        isConnected = true;
    }

    public IEnumerator CreateOffer()
    {
        Debug.Log("Creating Offer");
        var configuration = GetSelectedSdpSemantics();
        localPeer = new RTCPeerConnection(ref configuration);

        localPeer.OnIceCandidate = localPeerOnIceCandidate;
        localPeer.OnIceConnectionChange = localPeerOnIceConnectionChange;
        localPeer.OnNegotiationNeeded = localPeerOnNegotiationNeeded;

        sendChannel = localPeer.CreateDataChannel("data");
        sendChannel.OnOpen = onSendChannelOpen;
        sendChannel.OnClose = onSendChannelClose;

        localPeer.OnDataChannel = onDataChannel;

        var op = localPeer.CreateOffer(ref _offerOptions);
        yield return op;

        if (!op.IsError)
        {
            yield return StartCoroutine(OnCreateOfferSuccess(op.Desc));
        }
        else
        {
            Debug.Log("ERROR: Creating offer failed " + op.Error);
        }
        
    }

    IEnumerator OnCreateOfferSuccess(RTCSessionDescription desc)
    {
        websocket.SendSDPMessage("offer", desc.sdp);
        var op = localPeer.SetLocalDescription(ref desc);
        yield return op;

        if (!op.IsError)
        {
            Debug.Log("SetLocalDescription complete");
        }
        else
        {
            var error = op.Error;
            Debug.Log("ERROR: SetLocalDescription error - " + error.message);
        }
    }

    public IEnumerator CreateAnswer()
    {
        var op3 = localPeer.CreateAnswer(ref _answerOptions);
        yield return op3;
        if (!op3.IsError)
        {
            yield return OnCreateAnswerSuccess(op3.Desc);
        }
        else
        {
            Debug.Log("ERROR: Creating answer failed " + op3.Error.message);
        }
    }

    public IEnumerator AddOffer(string sdp)
    {
        var configuration = GetSelectedSdpSemantics();
        localPeer = new RTCPeerConnection(ref configuration);

        localPeer.OnIceCandidate = localPeerOnIceCandidate;
        localPeer.OnIceConnectionChange = localPeerOnIceConnectionChange;
        localPeer.OnNegotiationNeeded = localPeerOnNegotiationNeeded;

        sendChannel = localPeer.CreateDataChannel("data");
        sendChannel.OnOpen = onSendChannelOpen;
        localPeer.OnDataChannel = onDataChannel;

        // Create offer description from offer text field
        RTCSessionDescription desc = new RTCSessionDescription();
        desc.type = RTCSdpType.Offer;
        desc.sdp = sdp;

        var op2 = localPeer.SetRemoteDescription(ref desc);
        yield return op2;

        var op3 = localPeer.CreateAnswer(ref _answerOptions);
        yield return op3;

        if (!op3.IsError)
        {
            yield return OnCreateAnswerSuccess(op3.Desc);
        }
        else
        {
            Debug.Log("ERROR: Adding offer failed " + op3.Error.message);
        }
    }

    public void StartAddingAnswer(string sdp)
    {
        StartCoroutine(AddAnswer(sdp));
    }

    public IEnumerator AddAnswer(string sdp)
    {
        Debug.Log("added answer");
        RTCSessionDescription desc = new RTCSessionDescription();
        desc.type = RTCSdpType.Answer;
        desc.sdp = sdp;
        var op2 = localPeer.SetRemoteDescription(ref desc);
        yield return op2;
    }



    public void AddIceCandidateList(string iceJson)
    {
        IceList recievedIces = JsonUtility.FromJson<IceList>(iceJson);
        Debug.Log("added ices:" + recievedIces.iceList.Count);

        foreach (IceCandidate i in recievedIces.iceList)
        {
            RTCIceCandidateInit ic = new RTCIceCandidateInit();
            ic.candidate = i.candidate;
            ic.sdpMid = i.sdpMid;
            ic.sdpMLineIndex = i.sdpMLineIndex;
            RTCIceCandidate c = new RTCIceCandidate(ic);
            localPeer.AddIceCandidate(c);
        }
    }

    public void AddIceCandidate(string candidate, string sdpMid, int? sdpMLineIndex)
    {
        RTCIceCandidateInit ic = new RTCIceCandidateInit();
        ic.candidate = candidate;
        ic.sdpMid = sdpMid;
        ic.sdpMLineIndex = sdpMLineIndex;
        RTCIceCandidate c = new RTCIceCandidate(ic);
        localPeer.AddIceCandidate(c);
        
    }

    public void SendMessage()
    {
        Debug.Log("Sending voice ");
        sendChannel.Send(buffer);
    }

    private static RTCConfiguration GetSelectedSdpSemantics()
    {
        RTCConfiguration config = default;
        config.iceServers = new[]
        {
            //new RTCIceServer { urls = new[] { "stun:stun.l.google.com:19302" } }
            new RTCIceServer { urls = new[] { "stun:turn.webis.de:3478" } }
        };

        return config;
    }

    void OnIceConnectionChange(RTCIceConnectionState state)
    {
        switch (state)
        {
            case RTCIceConnectionState.New:
                Debug.Log("IceConnectionState: New");
                statusText.text = "New";
                break;
            case RTCIceConnectionState.Checking:
                Debug.Log("IceConnectionState: Checking");
                statusText.text = "Checking";

                foreach (IceCandidate ic in iceList.iceList)
                {
                    websocket.SendICEMessage(ic.candidate, ic.sdpMid, ic.sdpMLineIndex, "");
                }
                break;
            case RTCIceConnectionState.Closed:
                Debug.Log("IceConnectionState: Closed");
                break;
            case RTCIceConnectionState.Completed:
                Debug.Log("IceConnectionState: Completed");
                statusText.text = "Completed";
                break;
            case RTCIceConnectionState.Connected:
                Debug.Log("IceConnectionState: Connected");
                statusText.text = "Connected";

                break;
            case RTCIceConnectionState.Disconnected:
                Debug.Log("IceConnectionState: Disconnected");
                statusText.text = "Disconnected";
                break;
            case RTCIceConnectionState.Failed:
                Debug.Log("IceConnectionState: Failed");
                statusText.text = "Failed";
                break;
            case RTCIceConnectionState.Max:
                Debug.Log("IceConnectionState: Max");
                break;
            default:
                break;
        }
    }

    

    IEnumerator OnCreateAnswerSuccess(RTCSessionDescription desc)
    {
        var op = localPeer.SetLocalDescription(ref desc);
        yield return op;

        if (!op.IsError)
        {
            Debug.Log($"SetLocalDescription complete");
        }
        else
        {
            var error = op.Error;
            Debug.Log("ERROR: SetLocalDescription error - " + error.message);
        }
    }


    public void UpdateIceCandidates(RTCIceCandidate candidate)
    {
        string allCandidates = "";
        iceCandidates.Add(candidate.Candidate);
        IceCandidate ic = new IceCandidate(candidate.Candidate, candidate.SdpMid, candidate.SdpMLineIndex);
        iceList.iceList.Add(ic);
        //CandidatesInput.text = "";
        //foreach (string c in iceCandidates)
        //{
        //    allCandidates += "/" + c;
        //}
        //CandidatesInput.text = JsonUtility.ToJson(iceList);
    }

    // Update is called once per frame
    void Update()
    {

    }
    private void OnAudioFilterRead(float[] data, int channels)
    {
        Audio.Update(data, data.Length);
    }

}
