using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WebcamTexture : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        foreach(var i in WebCamTexture.devices)
        {
            print(i);
        }
        var webcam = new WebCamTexture();
        GetComponent<Renderer>().material.mainTexture = webcam;
        print(webcam.deviceName);
        webcam.Play();
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
