using UnityEngine;
using UnityEngine.UI;

public class UI_Buoyancy : MonoBehaviour
{
    public Slider buoPosSlider;
    public Slider buoNegSlider;
    public DiveSettings settings;
    public DiverController diverController;
    public float extremum = 100f;
    public float neutralTreshold = 1f;

    public void Start() {
        // Positive
        buoPosSlider.maxValue = extremum;
        buoPosSlider.minValue = 0;
        // Negative
        buoNegSlider.minValue = 0;
        buoNegSlider.maxValue = extremum;
    }

    public void FixedUpdate()
    {
        float buoyancy = diverController.buoyancy;

        if (buoyancy < neutralTreshold && buoyancy > -neutralTreshold) {
            buoPosSlider.value = 0;
            buoNegSlider.value = 0;
        }
        else if (buoyancy > 0) {
            buoNegSlider.value = 0;
            if (buoyancy > extremum) buoyancy = extremum - 0.001f;
            buoPosSlider.value = buoyancy % extremum;
        }
        else {
            buoPosSlider.value = 0;
            if (buoyancy < -extremum) buoyancy = -extremum + 0.001f;
            buoNegSlider.value = (-buoyancy) % extremum;
        }
    }

}