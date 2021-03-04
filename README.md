# unity-webrtc-opus-audio
Simple workaround to send audio through Unity's [WebRTC plugin](https://github.com/Unity-Technologies/com.unity.webrtc) by encoding the Microphone input with [UnityOpus](https://github.com/TyounanMOTI/UnityOpus) and sending it as a byte[] through the DataChannel. I just modified the existing code from both plugins and merged them in a simple example. Credit goes to [Unity-Technologies](https://github.com/Unity-Technologies), [TyounanMOTI](https://github.com/TyounanMOTI) and [lstemple](https://github.com/lstemple) who pointed to UnityOpus.

# Setup
Install the WebRTC plugin by adding git package URL:
```com.unity.webrtc@2.3.3-preview```

# Tested
Tested with Unity 2020.2.3f DX11 (Windows10)
