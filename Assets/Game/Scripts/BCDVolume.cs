using UnityEngine;
using UnityEngine.UI;

public class BCDVolume : MonoBehaviour
{
    public Slider bcd;
    public DiveSettings settings;
    public DiverController diverController;

    public void Start()
    {
        bcd.maxValue = settings.BCD_Capacity;
        bcd.minValue = 0;
    }

    public void Update()
    {
        //Debug.Log(bcd.value);
        bcd.value = diverController.BCD_Volume;
    }
}