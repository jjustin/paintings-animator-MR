using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Windows.WebCam;
using SimpleJSON;

public class BU_RoomDetector : MonoBehaviour
{

    [SerializeField]
    PhotoCapture photoCaptureObject;

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
    Texture2D tex;

    [SerializeField]
    List<string> existingPaintings = new List<string>();

    public void Start()
    {
        renderCam = Camera.main;
        Resolution cameraResolution = PhotoCapture.SupportedResolutions.OrderByDescending((res) => res.width * res.height).First();
        cameraResolution.width = 1280;
        cameraResolution.height = 720;
        //#if UNITY_EDITOR
        // cameraResolution = PhotoCapture.SupportedResolutions.OrderByDescending((res) => res.width * res.height).First();
        //#endif

        tex = new Texture2D(cameraResolution.width, cameraResolution.height);

        Debug.Log("Calling createAsync");

        PhotoCapture.CreateAsync(false, delegate (PhotoCapture captureObject) {
            Debug.Log("Called createAsync");
            photoCaptureObject = captureObject;

            cameraParameters = new CameraParameters(WebCamMode.PhotoMode);
            cameraParameters.hologramOpacity = 0.0f;
            cameraParameters.cameraResolutionWidth = cameraResolution.width;
            cameraParameters.cameraResolutionHeight = cameraResolution.height;
            cameraParameters.pixelFormat = CapturePixelFormat.BGRA32;

            // Activate the camera
            photoCaptureObject.StartPhotoModeAsync(cameraParameters, delegate (PhotoCapture.PhotoCaptureResult result) {
                TakePhoto();
            });
        });
    }


    // Use this for initialization
    void TakePhoto()
    {
        Debug.LogFormat("Take Photo called");
        photoCaptureObject.TakePhotoAsync(OnCapturedPhoto);
    }

    void OnCapturedPhoto(PhotoCapture.PhotoCaptureResult result, PhotoCaptureFrame photoCaptureFrame)
    {
        Debug.Log("OnCapturedPhoto called");
        StartCoroutine(HandleCaturedPhoto(result, photoCaptureFrame));
    }

    IEnumerator HandleCaturedPhoto(PhotoCapture.PhotoCaptureResult result, PhotoCaptureFrame photoCaptureFrame)
    {
        if (!result.success)
        {
            Debug.LogErrorFormat("Photo caputre unsuccessful");
            yield break;
        }

        // Copy the raw image data into the target texture
        Debug.Log("Calling UploadImageDataToTexture");
        photoCaptureFrame.UploadImageDataToTexture(tex);


        /*        List<byte> bytes = new List<byte>();
                photoCaptureFrame.CopyRawImageDataIntoBuffer(bytes);
#if UNITY_EDITOR
        Debug.Log("Calling UploadImageDataToTexture");
        photoCaptureFrame.UploadImageDataToTexture(tex);
#endif
        */


        yield return null;

        Debug.Log("Detect started");
        //byte[] bytes = tex.EncodeToJPG(); //Can also encode to jpg, just make sure to change the file extensions down below
        var form = new WWWForm();
        byte[] bytes = tex.EncodeToPNG();
        form.AddBinaryData("file", bytes, "detect.jpg", "image/jpeg"); ;

        yield return null;

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
                yield return new WaitForSeconds(2);
                TakePhoto();
                yield break;
            }

            Debug.Log("No errors from server");

            json = www.downloadHandler.text;
        }

        JSONNode parsed = JSON.Parse(json);


        if (m_Canvas == null)
        {
            m_Canvas = GameObject.CreatePrimitive(PrimitiveType.Quad);
            m_Canvas.name = "PhotoCaptureCanvas";
            m_CanvasRenderer = m_Canvas.GetComponent<Renderer>() as Renderer;
        }

        Matrix4x4 cameraToWorldMatrix; Matrix4x4 worldToCameraMatrix;
        if (!photoCaptureFrame.TryGetCameraToWorldMatrix(out cameraToWorldMatrix))
        {
            Debug.LogWarning("CameraToWorld matrix not returned, using render cam's matrix");
            cameraToWorldMatrix = renderCam.cameraToWorldMatrix;
            worldToCameraMatrix = renderCam.worldToCameraMatrix;
        }
        else
        {
            worldToCameraMatrix = cameraToWorldMatrix.inverse;
        }

        Matrix4x4 projectionMatrix;
        if (!photoCaptureFrame.TryGetProjectionMatrix(out projectionMatrix))
        {
            Debug.LogWarning("ProjectionMatrix matrix not returned, using render cam's matrix");
            projectionMatrix = renderCam.projectionMatrix;
        }

        m_CanvasRenderer.sharedMaterial.SetTexture("_MainTex", tex);
        m_CanvasRenderer.sharedMaterial.SetMatrix("_WorldToCameraMatrix", worldToCameraMatrix);
        m_CanvasRenderer.sharedMaterial.SetMatrix("_CameraProjectionMatrix", projectionMatrix);
        m_CanvasRenderer.sharedMaterial.SetFloat("_VignetteScale", 1.0f);

        // Position the canvas object slightly in front
        // of the real world web camera.
        Vector3 position = cameraToWorldMatrix.GetColumn(3) - cameraToWorldMatrix.GetColumn(2);

        // Rotate the canvas object so that it faces the user.
        Quaternion rotation = Quaternion.LookRotation(-cameraToWorldMatrix.GetColumn(2), cameraToWorldMatrix.GetColumn(1));

        m_Canvas.transform.position = position;
        m_Canvas.transform.rotation = rotation;

        Vector3 cam_pos = cameraToWorldMatrix.GetColumn(3);

        foreach (JSONNode image in parsed.Values)
        {
            bool shouldRender = true;
            Vector3[] points = new Vector3[4];
            string name = image["name"].Value;
            JSONNode corners = image["corners"];

            Debug.Log("Got response with " + name);

            for (int i = 0; i < corners.Count; i++)
            {
                JSONNode corner = corners[i];
                // The bottom-left of the viewport is (0,0); the right-top is (1,1)
                var screen_point = new Vector3(
                    (corner[0].AsFloat),
                    (1 - corner[1].AsFloat),
                    renderCam.nearClipPlane);

                var world_2d = renderCam.ViewportToWorldPoint(screen_point);

                var direction = (world_2d - cam_pos).normalized;

                DrawLine(cam_pos, cam_pos + direction * 10, Color.red, 10);

                RaycastHit hit;
                if (!Physics.Raycast(world_2d, direction, out hit))
                {
                    Debug.LogWarningFormat("Image {0} detected, but ray did not hit - skipping", name);
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
        }

        yield return new WaitForSeconds(2);
        TakePhoto();
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

        painting.transform.position = origin + (normal * 0.015f);

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

    void DrawLine(Vector3 start, Vector3 end, Color color, float duration = 0.2f)
    {
        GameObject myLine = new GameObject();
        myLine.transform.position = start;
        myLine.AddComponent<LineRenderer>();
        LineRenderer lr = myLine.GetComponent<LineRenderer>();
        lr.material = new Material(Shader.Find("Mixed Reality Toolkit/Standard"));
        lr.startColor = lr.endColor = color;
        lr.startWidth = lr.endWidth = 0.01f;
        lr.SetPosition(0, start);
        lr.SetPosition(1, end);
        GameObject.Destroy(myLine, duration);
    }
}
