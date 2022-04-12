using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class DiveSettingsManager : MonoBehaviour
{
    public DiveSettings diveSettings;
    public DiverController diverController;

    #region Pre-define and Lock Values
    [Header("Lock Values (true = locked | false = unlocked)")]
    [SerializeField] private bool lockWaterTemperature = false;
    [SerializeField, Range(-5f, 40f)] private float predefinedWaterTemperature = 15f;
    [SerializeField] private bool lockWaterDesnity = false;
    [SerializeField, Range(1000f, 1040f)] private float predefinedWaterDesnity = 1025f;
    [SerializeField] private bool lockAtmosphericPressure = false;
    [SerializeField, Range(850f, 1100f)] private float predefinedAtmophericPressure = 1013f;
    [SerializeField] private bool lockTankMaterial = false;
    [SerializeField, Range(0, 1)] private int predefinedTankMaterial = 1;
    [SerializeField] private bool lockTankEmptyWeight = false;
    [SerializeField, Range(3f, 30f)] private float predefinedTankEmptyWeight = 17f;
    [SerializeField] private bool lockTankStartPressure = false;
    [SerializeField, Range(100f, 500f)] private float predefinedTankstartPressure = 300f;
    [SerializeField] private bool lockTankCapacity = false;
    [SerializeField, Range(3f, 20f)] private float predefinedTankCapacity = 10f;
    [SerializeField] private bool lockLeadWeights = false;
    [SerializeField, Range(1f, 50f)] private float predefinedLeadWeights = 4f;
    [SerializeField] private bool lockBCDWeight = false;
    [SerializeField, Range(1f, 6f)] private float predefinedBCDWeight = 3.5f;
    [SerializeField] private bool lockBCDCapacity = false;
    [SerializeField, Range(5f, 40f)] private float predefinedBCDCapacity = 15f;
    [SerializeField] private bool lockSuitThickness = false;
    [SerializeField, Range(1f, 10f)] private float predefinedSuitThickness = 5f;
    #endregion

    #region Environment variables
    [Header("Environment UI Elements")]
    [SerializeField] private TextMeshProUGUI waterTempValue;
    [SerializeField] private Slider waterTempSlider;
    [SerializeField] private TextMeshProUGUI waterDensityValue;
    [SerializeField] private Slider waterDensitySlider;
    [SerializeField] private TextMeshProUGUI atmosphericPressureValue;
    [SerializeField] private Slider atmosphericPressureSlider;
    #endregion

    #region Equipment variables
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
    #endregion


    private void Start()
    {
        LockValues();
        PredefineSliderValues();
        GetSliderValues();
        SetUIValueues();
        SetListeners();
    }

    void LockValues()
    {
        waterTempSlider.interactable = !lockWaterTemperature;
        waterDensitySlider.interactable = !lockWaterDesnity;
        atmosphericPressureSlider.interactable = !lockAtmosphericPressure;
        tankMaterialsDropdown.interactable = !lockTankMaterial;
        tankEmptyWeightSlider.interactable = !lockTankEmptyWeight;
        tankStartPressSlider.interactable = !lockTankStartPressure;
        tankCapacitySlider.interactable = !lockTankCapacity;
        leadWeightsSlider.interactable = !lockLeadWeights;
        BCDWeightSlider.interactable = !lockBCDWeight;
        BCDCapacitySlider.interactable = !lockBCDCapacity;
        suitThicknessSlider.interactable = !lockSuitThickness;
    }

    void PredefineSliderValues()
    {
        waterTempSlider.value = predefinedWaterTemperature;
        waterDensitySlider.value = predefinedWaterDesnity;
        atmosphericPressureSlider.value = predefinedAtmophericPressure;
        tankMaterialsDropdown.value = predefinedTankMaterial;
        tankEmptyWeightSlider.value = predefinedTankEmptyWeight;
        tankStartPressSlider.value = predefinedTankstartPressure;
        tankCapacitySlider.value = predefinedTankCapacity;
        leadWeightsSlider.value = predefinedLeadWeights;
        BCDWeightSlider.value = predefinedBCDWeight;
        BCDCapacitySlider.value = predefinedBCDCapacity;
        suitThicknessSlider.value = predefinedSuitThickness;
    }

    void GetSliderValues()
    {
        diveSettings.waterTemp = waterTempSlider.value;
        diveSettings.waterDensity = waterDensitySlider.value;
        diveSettings.atmosphericPressure = atmosphericPressureSlider.value;
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
            tankCapacityValue.text = val.ToString("0.0");
        });
        leadWeightsSlider.onValueChanged.AddListener((val) => {
            diveSettings.leadWeights = val;
            leadWeightsValue.text = val.ToString("0.0");
        });
        BCDWeightSlider.onValueChanged.AddListener((val) => {
            diveSettings.BCD_weight = val;
            BCDWeightValue.text = val.ToString("0.0");
        });
        BCDCapacitySlider.onValueChanged.AddListener((val) => {
            diveSettings.BCD_Capacity = val;
            BCDCapacityValue.text = val.ToString();
        });
        defaultsButton.onClick.AddListener(ApplyDefaultValues);
    }



    public void ApplyDefaultValues()
    {
        diveSettings._waterSurfaceOffset = 0f;          // y-height of the water surface
        if (!lockWaterTemperature)
            diveSettings.waterTemp = 15f;               // Celcius
        if (!lockWaterDesnity)
            diveSettings.waterDensity = 1025f;          // Kg/m3
        if (!lockAtmosphericPressure)
            diveSettings.atmosphericPressure = 1013f;   // Air pressure in hPa (same as millibar)

        if (!lockTankMaterial)
            diveSettings.matValue = 1;
        if (!lockTankEmptyWeight)
            diveSettings.tankEmptyWeight = 17f;         // kg
        if (!lockTankStartPressure)
            diveSettings.tankStartPress = 300;          // bar
        if (!lockTankCapacity)
            diveSettings.tankCapacity = 10f;            // liter
        if (!lockLeadWeights)
            diveSettings.leadWeights = 4f;              // kg
        if (!lockBCDWeight)
            diveSettings.BCD_weight = 3.5f;             // kg
        if (!lockBCDCapacity)
            diveSettings.BCD_Capacity = 15f;            // liter
        if (!lockSuitThickness)
            diveSettings.suitThickness = 5;             // mm
        SetUIValueues();
        PredefineSliderValues();
    }
}
