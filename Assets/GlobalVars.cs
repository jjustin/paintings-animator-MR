using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GlobalVars : MonoBehaviour
{
#if UNITY_EDITOR
    public static string API_URL = "http://localhost:5000";
#else
    public static string API_URL = "http://192.168.1.198:5000";
#endif
}
