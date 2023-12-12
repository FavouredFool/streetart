using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

public class WallController : MonoBehaviour
{
    private void Start()
    {
        Texture2D texture = new Texture2D(Screen.width, Screen.height);
        GetComponent<Renderer>().material.mainTexture = texture;
    }
}
