using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class DiveSettingsManager : MonoBehaviour
{
    public DiveSettings diveSettings;
    public DiverController diverController;

    // UI-Refs, Environment variables
    // =======================================================
    [Header("Environment UI Elements")]
    [SerializeField] private TextMeshProUGUI waterTempValue;
    [SerializeField] private Slider waterTempSlider;
    [SerializeField] private TextMeshProUGUI waterDensityValue;
    [SerializeField] private Slider waterDensitySlider;
    [SerializeField] private TextMeshProUGUI atmosphericPressureValue;
    [SerializeField] private Slider atmosphericPressureSlider;
    // =======================================================


    // UI-Refs, Diver, user input variables
    // =======================================================
    [Header("Diver UI Elements")]
    [SerializeField] private TextMeshProUGUI diverHeightValue;
    [SerializeField] private Slider diverHeightSlider;
    [SerializeField] private TextMeshProUGUI diverWeightValue;
    [SerializeField] private Slider diverWeightSlider;
    [SerializeField] private TextMeshProUGUI RMVValue;
    [SerializeField] private Slider RMVSlider;
    [SerializeField] private TextMeshProUGUI suitThicknessValue;
    [SerializeField] private Slider suitThicknessSlider;
    [SerializeField] private TextMeshProUGUI leadWeightsValue;
    [SerializeField] private Slider leadWeightsSlider;

    [Header("Air Tank UI Elements")]
    [SerializeField] private TMP_Dropdown tankMaterialsDropdown;
    [SerializeField] private TextMeshProUGUI tankEmptyWeightValue;
    [SerializeField] private Slider tankEmptyWeightSlider;
    [SerializeField] private TextMeshProUGUI tankStartPressValue;
    [SerializeField] private Slider tankStartPressSlider;
    [SerializeField] private TextMeshProUGUI tankCapacityValue;
    [SerializeField] private Slider tankCapacitySlider;

    [Header("BCD UI Elements")]
    [SerializeField] private TextMeshProUGUI BCDWeightValue;
    [SerializeField] private Slider BCDWeightSlider;
    [SerializeField] private TextMeshProUGUI BCDCapacityValue;
    [SerializeField] private Slider BCDCapacitySlider;

    [Header("Reset UI Elements")]
    [SerializeField] private Button defaultsButton;


    private void Start()
    {
        GetSliderValues();
        SetUIValueues();
        SetListeners();
    }

    void GetSliderValues()
    {
        diveSettings.waterTemp = waterTempSlider.value;
        diveSettings.waterDensity = waterDensitySlider.value;
        diveSettings.atmosphericPressure = atmosphericPressureSlider.value;
        diveSettings.diverHeight = diverHeightSlider.value;
        diveSettings.diverWeight = diverWeightSlider.value;
        diveSettings.RMV = RMVSlider.value;
        diveSettings.suitThickness = suitThicknessSlider.value;

        diveSettings.matValue = tankMaterialsDropdown.value;
        if (diveSettings.matValue == 0) diveSettings.tankMaterial = DiveSettings.tankMaterials.Aluminium;
        else if (diveSettings.matValue == 1) diveSettings.tankMaterial = DiveSettings.tankMaterials.Steel;
        diveSettings.tankEmptyWeight = tankEmptyWeightSlider.value;
        diveSettings.tankStartPress = tankStartPressSlider.value;
        diveSettings.tankCapacity = tankCapacitySlider.value;
        diveSettings.leadWeights = leadWeightsSlider.value;
        diveSettings.BCD_weight = BCDWeightSlider.value;
        diveSettings.BCD_Capacity = BCDCapacitySlider.value;
    }

    void SetUIValueues()
    {
        waterTempValue.text = diveSettings.waterTemp.ToString();
        waterDensityValue.text = diveSettings.waterDensity.ToString();
        atmosphericPressureValue.text = diveSettings.atmosphericPressure.ToString();
        diverHeightValue.text = diveSettings.diverHeight.ToString();
        diverWeightValue.text = diveSettings.diverWeight.ToString();
        RMVValue.text = diveSettings.RMV.ToString();
        suitThicknessValue.text = diveSettings.suitThickness.ToString();

        tankMaterialsDropdown.value = diveSettings.matValue;
        tankEmptyWeightValue.text = diveSettings.tankEmptyWeight.ToString();
        tankStartPressValue.text = diveSettings.tankStartPress.ToString();
        tankCapacityValue.text = diveSettings.tankCapacity.ToString();
        leadWeightsValue.text = diveSettings.leadWeights.ToString();
        BCDWeightValue.text = diveSettings.BCD_weight.ToString();
        BCDCapacityValue.text = diveSettings.BCD_Capacity.ToString();
    }

    void SetListeners()
    {
        waterTempSlider.onValueChanged.AddListener((val) => {
            diveSettings.waterTemp = val;
            waterTempValue.text = val.ToString();
        });
        waterDensitySlider.onValueChanged.AddListener((val) => {
            diveSettings.waterDensity = val;
            waterDensityValue.text = val.ToString();
        });
        atmosphericPressureSlider.onValueChanged.AddListener((val) => {
            diveSettings.atmosphericPressure = val;
            atmosphericPressureValue.text = val.ToString();
        });
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
        suitThicknessSlider.onValueChanged.AddListener((val) => {
            diveSettings.suitThickness = val;
            suitThicknessValue.text = val.ToString();
        });
        tankMaterialsDropdown.onValueChanged.AddListener((val) => {
            diveSettings.matValue = val;
            if (diveSettings.matValue == 0) diveSettings.tankMaterial = DiveSettings.tankMaterials.Aluminium;
            else if (diveSettings.matValue == 1) diveSettings.tankMaterial = DiveSettings.tankMaterials.Steel;
        });
        tankEmptyWeightSlider.onValueChanged.AddListener((val) => {
            diveSettings.tankEmptyWeight = val;
            tankEmptyWeightValue.text = val.ToString();
        });
        tankStartPressSlider.onValueChanged.AddListener((val) => {
            diveSettings.tankStartPress = val;
            diverController.RefillTank(); // Have to recalculate gasRemainingMass.
            tankStartPressValue.text = val.ToString();
        });
        tankCapacitySlider.onValueChanged.AddListener((val) => {
            diveSettings.tankCapacity = val;
            tankCapacityValue.text = val.ToString();
        });
        leadWeightsSlider.onValueChanged.AddListener((val) => {
            diveSettings.leadWeights = val;
            leadWeightsValue.text = val.ToString();
        });
        BCDWeightSlider.onValueChanged.AddListener((val) => {
            diveSettings.BCD_weight = val;
            BCDWeightValue.text = val.ToString();
        });
        BCDCapacitySlider.onValueChanged.AddListener((val) => {
            diveSettings.BCD_Capacity = val;
            BCDCapacityValue.text = val.ToString();
        });
        defaultsButton.onClick.AddListener(() => ApplyDefaultValues());
    }

    public void ApplyDefaultValues()
    {
        diveSettings._waterSurface = 0f;            // y-height of the water surface
        diveSettings.waterTemp = 15f;               // Celcius
        diveSettings.waterDensity = 1025f;          // Kg/m3
        diveSettings.atmosphericPressure = 1013f;   // Air pressure in hPa (same as millibar)

        diveSettings.diverHeight = 180;             // cm
        diveSettings.diverWeight = 80;              // kg
        diveSettings.RMV = 15f;                     // Respiratory minute volume
        diveSettings.suitThickness = 5;             // mm

        diveSettings.matValue = 1;

        diveSettings.tankEmptyWeight = 17f;         // kg
        diveSettings.tankStartPress = 300;          // bar
        diveSettings.tankCapacity = 10f;            // liter
        diveSettings.leadWeights = 4f;              // kg
        diveSettings.BCD_weight = 3.5f;             // kg
        diveSettings.BCD_Capacity = 15f;            // liter
    }
}
