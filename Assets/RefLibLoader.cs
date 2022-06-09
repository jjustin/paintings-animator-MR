using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

public class RefLibLoader : MonoBehaviour
{

    public ARTrackedImageManager manager;

    // Start is called before the first frame update
    void Start()
    {

        var library = manager.CreateRuntimeLibrary();
        if (library is MutableRuntimeReferenceImageLibrary mutableLibrary)
        {
            var t = DownloadImage("http://localhost:5000/images/image7.jpg");
/*            mutableLibrary.ScheduleAddImageWithValidationJob(
                t.EncodeToPNG(),
                "my image",
                0.5f);
*/        }


    }

    // Update is called once per frame
    void Update()
    {
        
    }

    Texture2D DownloadImage(string MediaUrl)
    {
        UnityWebRequest request = UnityWebRequestTexture.GetTexture(MediaUrl);
        if (request.result != UnityWebRequest.Result.Success )
            Debug.Log(request.error);
        else
            return null;
        return ((DownloadHandlerTexture)request.downloadHandler).texture;
    }
}
