using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Windows.WebCam;
using Unity.Jobs;
using Unity.Collections;
using SimpleJSON;

struct PaintingDetection
{
    public Vector3 vertices;
    // public name;
    public Matrix4x4 cameraToWorldMatrix;
}

public class Jobs_RoomDetector : MonoBehaviour
{
    [SerializeField]
    private JobHandle job;

    private NativeArray<PaintingDetection> result;

    [SerializeField]
    List<string> existingPaintings = new List<string> ();

    public void Start()
    {
        StartJob();
    }

    public void StartJob()
    {
        Debug.Log("Starting Job");
        result = new NativeArray<PaintingDetection>(5, Allocator.Persistent);
        var photoTaker = new PhotoTakerJob();

        // photoTaker.existingPaintings = new NativeArray<char>(existingPaintings.ToArray(), Allocator.TempJob);
        photoTaker.result = result;

        job = photoTaker.Schedule();
    }

    public void Update()
    {
        job.Complete();
        if (job.IsCompleted)
        {
            Debug.Log("job done");
            result.Dispose();
            /*        foreach (JSONNode image in parsed.Values)
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

                        DrawLine(cam_pos, cam_pos + direction*10, Color.red, 10);

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
        */
        }

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
}
struct PhotoTakerJob : IJob 
{
    private int w, h;
    private Matrix4x4 viewToWorldMatrix;

    // public string[] existingPaintings;
    public NativeArray<PaintingDetection> result;

    public void Execute()
    {
        Debug.Log("called execute");
        w = 1280; h = 720;

        Debug.Log("Calling createAsync");

        PhotoTakerJob job = this;

        PhotoCapture.CreateAsync(false, delegate (PhotoCapture captureObject)
        {
            Debug.Log("Called createAsync");
            PhotoCapture photoCaptureObject = captureObject;

            CameraParameters cameraParameters = new CameraParameters(WebCamMode.PhotoMode);
            cameraParameters.hologramOpacity = 0.0f;
            cameraParameters.cameraResolutionWidth = job.w;
            cameraParameters.cameraResolutionHeight = job.h;
            cameraParameters.pixelFormat = CapturePixelFormat.BGRA32;
            Debug.LogFormat("job: {0}x{1}", job.w, job.h);
            // Activate the camera
            photoCaptureObject.StartPhotoModeAsync(cameraParameters, delegate (PhotoCapture.PhotoCaptureResult result)
            {
                photoCaptureObject.TakePhotoAsync(OnCapturedPhoto);
            });
        });
    }

    void OnCapturedPhoto(PhotoCapture.PhotoCaptureResult result, PhotoCaptureFrame photoCaptureFrame)
    {
        Debug.Log("OnCapturedPhoto called");
        if (!result.success)
        {
            Debug.LogErrorFormat("Photo caputre unsuccessful");
            return;
        }

        // Copy the raw image data into the target texture
        Debug.Log("Calling UploadImageDataToTexture");
        Texture2D tex = new Texture2D(w, h);
        photoCaptureFrame.UploadImageDataToTexture(tex);
/*        List<byte> bytes = new List<byte>();
        photoCaptureFrame.CopyRawImageDataIntoBuffer(bytes);
        Debug.LogFormat("Copied");
*/
        Debug.Log("Detect started");

        byte[] bytes = tex.EncodeToPNG(); //Can also encode to jpg, just make sure to change the file extensions down below
        var form = new WWWForm();
        form.AddBinaryData("file", bytes, "detect.png", "image/png");

        string json;
        string url = string.Format("{0}/images/detect?skip_images={1}", GlobalVars.API_URL, "");//string.Join(",", existingPaintings));
        using (UnityWebRequest www = UnityWebRequest.Post(url, form))
        {
            Debug.Log("Detect on server started");
            www.SendWebRequest();

            Debug.Log("Detect on server done");

            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.Log(www.error);
                return;
            }

            Debug.Log("No errors from server");

            json = www.downloadHandler.text;
        }

        JSONNode parsed = JSON.Parse(json);


        Matrix4x4 cameraToWorldMatrix; Matrix4x4 worldToCameraMatrix;
        if (!photoCaptureFrame.TryGetCameraToWorldMatrix(out cameraToWorldMatrix))
        {
            Debug.LogWarning("CameraToWorld matrix not returned, using render cam's matrix");
            // cameraToWorldMatrix = renderCam.cameraToWorldMatrix;
            // worldToCameraMatrix = renderCam.worldToCameraMatrix;
        }
        else
        {
            worldToCameraMatrix = cameraToWorldMatrix.inverse;
        }

        Matrix4x4 projectionMatrix;
        if (!photoCaptureFrame.TryGetProjectionMatrix(out projectionMatrix))
        {
            Debug.LogWarning("ProjectionMatrix matrix not returned, using render cam's matrix");
            // projectionMatrix = renderCam.projectionMatrix;
        }

        Vector3 cam_pos = cameraToWorldMatrix.GetColumn(3);

/*        foreach (JSONNode image in parsed.Values)
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

                DrawLine(cam_pos, cam_pos + direction*10, Color.red, 10);

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
*/
    }

}

