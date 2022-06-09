using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EmotionSelector : MonoBehaviour
{
    public string emotion;

    public void SetEmoiton()
    {
        var parent = gameObject.transform.parent;
        var painting = parent.parent.GetComponent<PaintingController>();
        if (emotion != null)
        {
           painting.SetEmotion(emotion);
        }
    }
}
