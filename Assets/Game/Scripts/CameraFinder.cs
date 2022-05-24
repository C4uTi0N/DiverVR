using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraFinder : MonoBehaviour
{
    Canvas thisCanvas;
    Camera playerCamera;


    // Start is called before the first frame update
    void Start()
    {
        thisCanvas = GetComponent<Canvas>();
        playerCamera = GameObject.Find("Camera").GetComponent<Camera>();
        thisCanvas.worldCamera = playerCamera;
    }
}
