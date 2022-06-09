using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Windows.WebCam;
using SimpleJSON;

public class WT_RoomDetector : MonoBehaviour
{
    [SerializeField]
    PhotoCapture photoCaptureObject;

    [SerializeField]
    Resolution cameraResolution;
    
    [SerializeField]
    GameObject existingGameObject;
    
    [SerializeField]
    CameraParameters cameraParameters;
    
    [SerializeField]
    private Camera renderCam;

    [SerializeField]
    GameObject m_Canvas = null;

    [SerializeField]
    Renderer m_CanvasRenderer = null;

    [SerializeField]
    WebCamTexture m_webcam;

    [SerializeField]
    List<string> existingPaintings = new List<string> ();

    public void Start()
    {
        renderCam = Camera.main;
        foreach (Resolution res in PhotoCapture.SupportedResolutions)
        {
            Debug.Log( string.Format("resolution:{0}x{1}", res.width, res.height));
        }
        cameraResolution = PhotoCapture.SupportedResolutions.OrderByDescending((res) => res.width * res.height).First();
        //#if UNITY_EDITOR
        // cameraResolution = PhotoCapture.SupportedResolutions.OrderByDescending((res) => res.width * res.height).First();
        //#endif

        m_webcam = new WebCamTexture(cameraResolution.width, cameraResolution.height);
        m_webcam.Play();
        StartCoroutine(TakePhoto());
        /*        cameraParameters = new CameraParameters(WebCamMode.PhotoMode);
                cameraParameters.hologramOpacity = 0.0f;
                cameraParameters.cameraResolutionWidth = cameraResolution.width;
                cameraParameters.cameraResolutionHeight = cameraResolution.height;
                cameraParameters.pixelFormat = CapturePixelFormat.BGRA32;

                Debug.Log("Calling createAsync");

                PhotoCapture.CreateAsync(false, delegate (PhotoCapture captureObject) {
                    Debug.Log("Called createAsync");
                    photoCaptureObject = captureObject;

                    // Activate the camera
                    photoCaptureObject.StartPhotoModeAsync(cameraParameters, delegate (PhotoCapture.PhotoCaptureResult result) {
                        StartCoroutine(TakePhoto());
                    });
                });
        */
    }


    // Use this for initialization
    IEnumerator TakePhoto()
    {
        Debug.LogFormat("Take Photo called with resolution: {0}x{1}", cameraResolution.width, cameraResolution.height);

        // Copy the raw image data into the target texture
        Texture2D tex = new Texture2D(cameraResolution.width, cameraResolution.height);
        tex.SetPixels((m_webcam).GetPixels());
        tex.Apply();

        Resources.UnloadUnusedAssets();
        // photoCaptureFrame.UploadImageDataToTexture(tex);



        Debug.Log("Detect started");
        /*        byte[] bytes = tex.EncodeToPNG(); //Can also encode to jpg, just make sure to change the file extensions down below
                var form = new WWWForm();
                form.AddBinaryData("file", bytes, "detect.png", "image/png");

                string json;
                string url = string.Format("{0}/images/detect?skip_images={1}", GlobalVars.API_URL, string.Join(",", existingPaintings));
                using (UnityWebRequest www = UnityWebRequest.Post(url, form))
                {
                    Debug.Log("Detect on server started");
                    yield return www.SendWebRequest();
                    Debug.Log("Detect on server done");

                    if (www.result != UnityWebRequest.Result.Success)
                    {
                        Debug.Log(www.error);
                        yield break;
                    }

                    Debug.Log("No errors from server");

                    json = www.downloadHandler.text;
                }

                JSONNode parsed = JSON.Parse(json);

                float w = cameraResolution.width;
                float h = cameraResolution.height;
        */        /*        print(cameraResolution);
                        print(string.Format("render cam info: {0} x {1}", renderCam.pixelWidth, renderCam.pixelHeight)); */

        /*        Matrix4x4 cameraToWorldMatrix;
                if (!photoCaptureFrame.TryGetCameraToWorldMatrix(out cameraToWorldMatrix))
                {
                    Debug.LogWarning("CameraToWorld matrix not returned.");
                }
        */
        Matrix4x4 cameraToWorldMatrix = renderCam.cameraToWorldMatrix;
        Matrix4x4 worldToCameraMatrix = renderCam.worldToCameraMatrix; //cameraToWorldMatrix.inverse;
        Debug.Log(worldToCameraMatrix);

        Matrix4x4 projectionMatrix = renderCam.projectionMatrix;
        // photoCaptureFrame.TryGetProjectionMatrix(out projectionMatrix);

        if (m_Canvas == null)
        {
            m_Canvas = GameObject.CreatePrimitive(PrimitiveType.Quad);
            m_Canvas.name = "PhotoCaptureCanvas";
            m_CanvasRenderer = m_Canvas.GetComponent<Renderer>() as Renderer;
        }

        m_CanvasRenderer.sharedMaterial.SetTexture("_MainTex", tex);
        m_CanvasRenderer.sharedMaterial.SetMatrix("_WorldToCameraMatrix", worldToCameraMatrix);
        m_CanvasRenderer.sharedMaterial.SetMatrix("_CameraProjectionMatrix", projectionMatrix);
        m_CanvasRenderer.sharedMaterial.SetFloat("_VignetteScale", 1.0f);

        // Position the canvas object slightly in front
        // of the real world web camera.
        Vector3 position = cameraToWorldMatrix.GetColumn(3) - cameraToWorldMatrix.GetColumn(2);
        print(cameraToWorldMatrix.GetColumn(3));
        print(cameraToWorldMatrix.GetColumn(2));
        // Rotate the canvas object so that it faces the user.
        Quaternion rotation = Quaternion.LookRotation(-cameraToWorldMatrix.GetColumn(2), cameraToWorldMatrix.GetColumn(1));

        m_Canvas.transform.position = position;
        m_Canvas.transform.rotation = rotation;

        /*        foreach (JSONNode image in parsed.Values)
                {
                    bool shouldRender = true;
                    Vector3[] points = new Vector3[4];
                    string name = image["name"].Value;
                    JSONNode corners = image["corners"];

                    for (int i = 0; i < corners.Count; i++)
                    {
                        JSONNode corner = corners[i];
                        // The bottom-left of the screen is (0,0); the right-top is (pixelWidth,pixelHeight)
                        var screen_point = new Vector3(
                            (corner[0].AsFloat) ,
                            (h - corner[1].AsFloat) ,
                            renderCam.nearClipPlane);
                        var world_2d = renderCam.ViewportToScreenPoint(screen_point);

                        var direction = (world_2d - camera_position).normalized;

                        RaycastHit hit;
                        if (!Physics.Raycast(world_2d, direction, out hit))
                        {
                            shouldRender = false;
                            // Don't include image if spatial map is not yet available
                            break;
                        }
                        points[i] = hit.point;
                    }

                    if (shouldRender)
                    {
                        LoadPainting(name, points);
                    }
                }*/
        yield return new WaitForSeconds(2);
        StartCoroutine(TakePhoto());
    }

    void LoadPainting(string imageName, Vector3[] vertices)
    {
        // Create a GameObject to which the texture can be applied
        var prefab = Resources.Load("Prefabs/Painting");
        GameObject painting = Instantiate(prefab) as GameObject;

        var origin = vertices[0];
        for (int i = 0; i < vertices.Length; i++)
        {
            vertices[i] = vertices[i] - origin;
        }
        Vector3 normal = Vector3.Cross(vertices[1], vertices[3]).normalized;

        painting.transform.position = origin + (normal*0.015f);

        MeshRenderer meshRenderer = painting.AddComponent<MeshRenderer>();
        meshRenderer.sharedMaterial = new Material(Shader.Find("Standard"));
        MeshFilter meshFilter = painting.AddComponent<MeshFilter>();

        Mesh mesh = new Mesh();
        mesh.vertices = vertices;
        mesh.triangles = new int[6]{
                0, 1, 2,
                2, 1, 3
            };
        mesh.uv = new Vector2[4]
        {
              new Vector2(0, 1),
              new Vector2(1, 1),
              new Vector2(0, 0),
              new Vector2(1, 0) 
        };
        mesh.name = imageName;

        var selectorOrigin = (-vertices[1].normalized * 0.05f) + (normal.normalized * 0.1f);

        var selector = painting.transform.GetChild(0);
        selector.localPosition = selectorOrigin;

        meshFilter.mesh = mesh;

        painting.GetComponent<PaintingController>().SetPaintingName(imageName);

        existingPaintings.Add(imageName);

        Debug.Log("painting rendered");
    }
}
