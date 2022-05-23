using UnityEngine;
using UnityEngine.UI;

public class TankPressure : MonoBehaviour
{
    public Slider tankSlider;
    public DiveSettings settings;
    public DiverController diverController;

    public void Start()
    {


        tankSlider.maxValue = settings.tankStartPress;
        tankSlider.minValue = 0;
    }

    public void Update()
    {
        //Debug.Log(bcd.value);
        tankSlider.value = (float)diverController.tankPress;
    }
}