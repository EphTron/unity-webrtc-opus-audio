using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MyVisualizer : MonoBehaviour
{
    public MyRecorder recorder;

    void Update()
    {
        transform.localScale = new Vector3(1, recorder.GetRMS() * 100.0f, 1);
    }
}
