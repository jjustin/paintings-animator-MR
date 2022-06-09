using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class test : MonoBehaviour
{
    public Camera cam;
    // Start is called before the first frame update
    void Start()
    {
        cam.CopyFrom(Camera.main);
        Debug.Log("test");
        StartCoroutine(X());
    }

    IEnumerator X()
    {
        Debug.Log(cam.cameraToWorldMatrix);
        Debug.Log(Camera.main.cameraToWorldMatrix);
        yield return new WaitForSeconds(2);
        Debug.Log(cam.cameraToWorldMatrix);
        Debug.Log(Camera.main.cameraToWorldMatrix);

    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
