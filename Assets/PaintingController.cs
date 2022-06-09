using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class PaintingController : MonoBehaviour
{
    [SerializeField]
    string emotion = "";
    public void SetEmotion(string emotion)
    {
        this.emotion = emotion;
        loadTexture();
    }

    [SerializeField]
    string paintingName = "";
    public void SetPaintingName(string paintingName)
    {
        this.paintingName = paintingName;
        print("pn: " + this.paintingName);
        loadTexture();
    }

    UnityEngine.Video.VideoPlayer videoPlayer;

    // Start is called before the first frame update
    void Start()
    {
    }

    void loadTexture()
    {
        print("called load texture");
        if (videoPlayer != null)
        {
            Destroy(videoPlayer);
        }
        print(string.Format("emotion: {0}", emotion));
        if (emotion == "")
        {
            StartCoroutine(setImageAsTexture());
            return;
        }


        // TODO: Update split to include all but last if names will contain more than one dot
        var url = string.Format("{0}/output/{1}_{2}.mp4", GlobalVars.API_URL, paintingName.Split('.')[0], emotion);
        
        videoPlayer = gameObject.AddComponent<UnityEngine.Video.VideoPlayer>();
        videoPlayer.url = url;
        videoPlayer.isLooping = true;
        videoPlayer.Play();
    }


    IEnumerator setImageAsTexture()
    {
        print("setting image as texture");

        string url = string.Format("{0}/images/{1}", GlobalVars.API_URL, paintingName);
        UnityWebRequest request = UnityWebRequestTexture.GetTexture(url);
        yield return request.SendWebRequest();
        Texture2D texture;
        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.Log(request.error);
            yield break;
        }
        else
        {
            texture = ((DownloadHandlerTexture)request.downloadHandler).texture;
        }

        Renderer renderer = gameObject.GetComponent<Renderer>() as Renderer;

        renderer.material.SetTexture("_MainTex", texture);
    }
}
