using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PlayerSettingsManager : MonoBehaviour
{
    public DiveSettings diveSettings;
    public DiverController diverController;

    
    // UI-Refs, Diver
    // =======================================================
    [Header("Diver UI Elements")]
    [SerializeField] private TextMeshProUGUI diverHeightValue;
    [SerializeField] private Slider diverHeightSlider;
    [SerializeField] private TextMeshProUGUI diverWeightValue;
    [SerializeField] private Slider diverWeightSlider;
    [SerializeField] private TextMeshProUGUI RMVValue;
    [SerializeField] private Slider RMVSlider;


    private void Start()
    {
        GetSliderValues();
        SetUIValueues();
        SetListeners();
    }

    void GetSliderValues()
    {
        
        diveSettings.diverHeight = diverHeightSlider.value;
        diveSettings.diverWeight = diverWeightSlider.value;
        diveSettings.RMV = RMVSlider.value;
    }

    void SetUIValueues()
    {
        diverHeightValue.text = diveSettings.diverHeight.ToString();
        diverWeightValue.text = diveSettings.diverWeight.ToString();
        RMVValue.text = diveSettings.RMV.ToString();
    }

    void SetListeners()
    {
        diverHeightSlider.onValueChanged.AddListener((val) => {
            diveSettings.diverHeight = val;
            diverHeightValue.text = val.ToString();
        });
        diverWeightSlider.onValueChanged.AddListener((val) => {
            diveSettings.diverWeight = val;
            diverWeightValue.text = val.ToString();
        });
        RMVSlider.onValueChanged.AddListener((val) => {
            diveSettings.RMV = val;
            RMVValue.text = val.ToString();
        });
    }

    public void ApplyDefaultValues()
    {
        diveSettings.diverHeight = 180;             // cm
        diveSettings.diverWeight = 80;              // kg
        diveSettings.RMV = 15f;                     // Respiratory minute volume
    }
}
