using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class bcdStatusTextScript : MonoBehaviour
{
    public DiverController diverController;

    public GameObject BCD_Status;

    public Text statusText;

    public Slider statusSlider;
    public Gradient background;
    public Gradient fill;

    public float sliderMax = 1f;
    public float sliderMin = -1f;

    public float normBuoyancy = 60;

    private void Start()
    {
        statusSlider.maxValue = sliderMax;
        statusSlider.minValue = sliderMin;
    }

    // Update is called once per frame
    void Update()
    {
        statusText.text = (diverController.buoyancy / normBuoyancy).ToString("F1");

        statusSlider.value = diverController.buoyancy / normBuoyancy;

        Debug.Log(diverController.buoyancy / normBuoyancy);
    }
}
