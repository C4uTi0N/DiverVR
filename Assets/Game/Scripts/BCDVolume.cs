using UnityEngine;
using UnityEngine.UI;

public class BCDVolume : MonoBehaviour
{
    public Slider bcd;

    public DiveSettings settings;
    public DiverController controller;

    public void Start()
    {
        //Debug.Log(settings.BCD_Capacity);

        bcd.maxValue = settings.BCD_Capacity;
        bcd.minValue = 0;
    }

    public void Update()
    {
        //Debug.Log(bcd.value);
        bcd.value = controller.BCDOldBoyleVol;
    }
}