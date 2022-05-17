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
    [SerializeField] private TextMeshProUGUI MARValue;
    [SerializeField] private Slider MARSlider;


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
        diveSettings.maxAscentRate = MARSlider.value;
    }

    void SetUIValueues()
    {
        diverHeightValue.text = diveSettings.diverHeight.ToString();
        diverWeightValue.text = diveSettings.diverWeight.ToString();
        RMVValue.text = diveSettings.RMV.ToString();
        MARValue.text = diveSettings.maxAscentRate.ToString();
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
        }); MARSlider.onValueChanged.AddListener((val) => {
            diveSettings.maxAscentRate = val;
            MARValue.text = val.ToString();
        });
    }

    public void ApplyDefaultValues()
    {
        diveSettings.diverHeight = 180;             // cm
        diveSettings.diverWeight = 80;              // kg
        diveSettings.RMV = 15f;                     // Respiratory minute volume
        diveSettings.maxAscentRate = 10f;           // Max Ascent Rate
    }
}
