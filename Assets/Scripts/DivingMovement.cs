using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.InputSystem;


[RequireComponent(typeof(Rigidbody))]

public class DivingMovement : MonoBehaviour
{
    // Environment variables
    private const float g = 9.82f;                                          // Gravitational constant
    private float _waterSurface;                                            // y-height of the water surface
    [SerializeField, Range(-5, 40)] float waterTemp = 15f;
    [SerializeField, Range(1000f, 1040f)] float waterDensity = 1025f;       // Kg/m3
    [SerializeField, Range(850, 1100)] float atmosphericPressue = 1013f;    // Air pressure in hPa (same as millibar)

    // BCD variables
    float BCD_OldBoyleVol = 0f;     // l, Previous calculated volume of BCD air.
    float BCD_SurfAirVol = 0f;      // l, Equivalent surface volume of air in BCD.
    float BCD_VolStep = 0.005f;     // l, Air added to BCD per step (fixed update).

    // Swimming
    float swimForce = 2f;           // Force of strokes
    float minForce = 0f;            // Min force for a stroke to be registered (default 0.25)
    float minStrokeInterval = 0f;   // Min time between strokes (default 0.25)
    float _strokeCooldown;          // Timer which is reset to 0 at each stroke
    float submergence = 0;          // How much of the diver is submerged (a bit fictive as of now, it's relative to the VR-headset position)

    // Breath
    int breathsPerMin = 6;
    float breathCooldown = 60f;     // 60s / x breaths per min.
    float breathTimer = 0;

    // Gas & descending, ascending
    float gasStartMass;
    float gasRemainingMass;
    float safetyEndPress = 35f;     // bar, Minimum pressure to surface with (equivalent to 500 PSI).
    float ascentMaxMtrPerMin = 10f; // m, Maximum meters/min when ascending.
    float netBuoyancy = 0f;

    // User data variables
    bool diveGoingOn = false;         // Has the dive begun?
    float userDataCooldown = 1f;    // s, Update frequecy of user data.
    float userDataUpdTimer = 0;
    //
    float time;             // hh:mm:ss, current local time
    float diveTime = 0;     // s, Time elapsed since the dive began.
    float depth;            // m, current diver depth
    float tankPress;        // bar, current pressure in tank
    float timeAtDepth;      // s, The time left at current depth, (with time to surface subtracted).
                            /* Note: the above is not NDL (No Decompression Limit)*/
    //
    string timeStr;
    string diveTimeStr    = "00h00m00s";
    string depthStr;
    string tankPressStr;
    string timeAtDepthStr = "00h00m00s";

    // Diver, user input variables
    [SerializeField, Range(100, 250)] float diverHeight = 180;        // cm
    [SerializeField, Range(20, 200)] float diverWeight = 80;          // kg
    [SerializeField, Range(10, 20)] float RMV = 15f;                  // Respiratory minute volume
    [SerializeField, Range(1, 10)] float suitThickness = 5;           // mm
    enum tankMaterials {Aluminium, Steel };
    [SerializeField] tankMaterials tankMaterial = tankMaterials.Steel;  // Aluminium or Steel
    private int matValue = 1;
    [SerializeField, Range(3, 30)] float tankEmptyWeight = 17f;         // kg
    [SerializeField, Range(100, 500)] float tankStartPress = 300;         // bar
    [SerializeField, Range(3, 20)] float tankCapacity = 10f;            // liter
    [SerializeField, Range(1f, 50f)] float leadWeights = 4f;            // kg
    [SerializeField, Range(1f, 6f)] float BCD_weight = 3.5f;            // kg
    [SerializeField, Range(5, 40)] float BCD_Capacity = 15f;            // liter

    // Unity Game object refs.
    [SerializeField] private GameObject testCube;
    [SerializeField] private Transform userStartPos;    // Spawnpos for the player
    [SerializeField] GameObject _XR_Origin;
    [SerializeField] private GameObject _waterBody;     // Gameobject holding our water
    [SerializeField] private GameObject _VR_Camera;
    Transform _trackRef;                                // Tracking controllers relative to this transform (_XR_origin)
    Rigidbody _rb;                                      // Note: divers rb is offset to (0,1,0)


    // UI-Refs, Environment variables
    // =======================================================
    [SerializeField] private TextMeshProUGUI waterTemp_Val;
    [SerializeField] private Slider waterTemp_Slider;
    [SerializeField] private TextMeshProUGUI waterDensity_Val;
    [SerializeField] private Slider waterDensity_Slider;
    [SerializeField] private TextMeshProUGUI atmosphericPressure_Val;
    [SerializeField] private Slider atmosphericPressure_Slider;
    // UI-Refs, Diver, user input variables
    // =======================================================
    [SerializeField] private TextMeshProUGUI diverHeight_Val;
    [SerializeField] private Slider diverHeight_Slider;
    [SerializeField] private TextMeshProUGUI diverWeight_Val;
    [SerializeField] private Slider diverWeight_Slider;
    [SerializeField] private TextMeshProUGUI RMV_Val;
    [SerializeField] private Slider RMV_Slider;
    [SerializeField] private TextMeshProUGUI suitThickness_Val;
    [SerializeField] private Slider suitThickness_Slider;

    [SerializeField] private TMP_Dropdown tankMaterials_Drop;
    [SerializeField] private TextMeshProUGUI tankEmptWeight_Val;
    [SerializeField] private Slider tankEmptWeight_Slider;
    [SerializeField] private TextMeshProUGUI tankStartPress_Val;
    [SerializeField] private Slider tankStartPress_Slider;
    [SerializeField] private TextMeshProUGUI tankCapacity_Val;
    [SerializeField] private Slider tankCapacity_Slider;
    [SerializeField] private TextMeshProUGUI leadWeights_Val;
    [SerializeField] private Slider leadWeights_Slider;
    [SerializeField] private TextMeshProUGUI BCD_Weight_Val;
    [SerializeField] private Slider BCD_Weight_Slider;
    [SerializeField] private TextMeshProUGUI BCD_Capacity_Val;
    [SerializeField] private Slider BCD_Capacity_Slider;

    [SerializeField] private Button defaultsButton;
    [SerializeField] private Toggle showTestDataToggle;
    // =======================================================


    // Test data UI-panel refs.
    // =======================================================
    [SerializeField] private GameObject testDataGO;
    [SerializeField] private TextMeshProUGUI localTime_Val;
    [SerializeField] private TextMeshProUGUI diveDuration_Val;
    [SerializeField] private TextMeshProUGUI tankPressure_Val;
    [SerializeField] private TextMeshProUGUI timeAtDepth_Val;
    [SerializeField] private TextMeshProUGUI depth_Val;
    [SerializeField] private TextMeshProUGUI netBuoyancy_Val;
    // =======================================================

    
    // Oculus controller refs.
    // =======================================================
    [SerializeField] InputActionReference leftContRef;      // Ref. to left grip button
    [SerializeField] InputActionReference leftContVel;      // Velocity of left controller
    [SerializeField] InputActionReference leftThumbStick;   // Forward on left thumbstick
    [SerializeField] InputActionReference leftContPos;      // Position of left controller
    [SerializeField] InputActionReference leftContX;        // X button of left controller
    [SerializeField] InputActionReference leftContY;        // Y button of left controller

    [SerializeField] InputActionReference rightContRef;     // Ref. to right grip button
    [SerializeField] InputActionReference rightContVel;     // Velocity of right controller
    [SerializeField] InputActionReference rightContPos;     // Position of right controller
    [SerializeField] InputActionReference rightContA;       // A button of right controller
    [SerializeField] InputActionReference rightContB;       // B button of right controller
    // =======================================================



    private void Awake()
    {
        _trackRef = _XR_Origin.transform;
        _rb = GetComponent<Rigidbody>();
        _rb.constraints = RigidbodyConstraints.FreezeRotation;
        _waterSurface = _waterBody.transform.position.y - 1f;
        _rb.mass = diverWeight;

        // Initial values.
        breathCooldown /= breathsPerMin;
        InitDive();
    }


    private void Start()
    {
        GetSliderValues();
        SetUI_Values();
        SetListeners();
        TeleportToStartPos();
    }


    void TeleportToStartPos()
    {
        _rb.position = userStartPos.position;
        Vector3 pos1 = new Vector3(userStartPos.transform.position.x + 3, userStartPos.transform.position.y + 0.91f, userStartPos.transform.position.z);
        testCube.transform.position = pos1;
    }


    private void FixedUpdate()
    {
        netBuoyancy = ApplyGravity(_rb);        // Always apply gravity.

        // Updating timers.
        userDataUpdTimer += Time.deltaTime;
        if (diveGoingOn) {
            breathTimer += Time.deltaTime;
            diveTime += Time.deltaTime;
        }

        // Submergence controls the beginning of the dive.
        submergence = CalcSubmergence(_VR_Camera);    // Degree of submergence.
        if (submergence > 0) {
            diveGoingOn = true;        // Dummy variable (i.e could be set with UI button)
        }

        // Updating water drag and buoyancy if user is in the water.
        if (submergence > 0f)
        {
            Drag(_rb, submergence);
            netBuoyancy += BodyBuoyancy(_rb, submergence);
            netBuoyancy += SuitBuoyancy(submergence);
            netBuoyancy += BCD_Buoyancy(submergence);
            netBuoyancy += TankBuoyancy(submergence);
        }

        // Allowing swim movement if more than half submerged.
        if (submergence > 0.5f) SwimMovement();
        
        // Taking a breath.
        if (breathTimer > breathCooldown) {
            breathTimer = 0;
            TakeBreath();
        }

        // Updating data for UI
        if (userDataUpdTimer > userDataCooldown) {
            userDataUpdTimer = 0;

            // Calculating max stay-time at current depth.
            // ============================================
            // Subtracting safety-gas mass.
            float gasEndMass = GasDensity(safetyEndPress) * tankCapacity / 1000;   // kg. Tank gas mass, when tank pressure is at 'safetyEndPress'.
            float gasToUseMass = gasRemainingMass - gasEndMass;
            // Subtracting mass of gas used during ascent.
            gasToUseMass -= AscentGasMass();
            // Subtracting mass of gas used during 5min safety stop at 5m.
            gasToUseMass -= StopGasMass(5f, 5f);
            // We can now calculate remaining time at current depth.
            timeAtDepth = TimeAtDepth(gasToUseMass);
            // ============================================

            UpdateUserData();
        }
    }


    // Setting values for a new dive.
    private void InitDive()
    {
        breathTimer = 0;
        diveTime = 0;
        BCD_OldBoyleVol = 0f;           // Emptying the BCD
        tankPress = tankStartPress;     // Refilling the tank
        gasStartMass = GasDensity(tankPress) * tankCapacity / 1000f;
        gasRemainingMass = gasStartMass;
    }


    // Assigning variable values from UI-sliders.
    void GetSliderValues() {
        waterTemp = waterTemp_Slider.value;
        waterDensity = waterDensity_Slider.value;
        atmosphericPressue = atmosphericPressure_Slider.value;
        diverHeight = diverHeight_Slider.value;
        diverWeight = diverWeight_Slider.value;
        RMV = RMV_Slider.value;
        suitThickness = suitThickness_Slider.value;

        matValue = tankMaterials_Drop.value;
        if (matValue == 0) tankMaterial = tankMaterials.Aluminium;
        else if (matValue == 1) tankMaterial = tankMaterials.Steel;
        tankEmptyWeight = tankEmptWeight_Slider.value;
        tankStartPress = tankStartPress_Slider.value;
        tankCapacity = tankCapacity_Slider.value;
        leadWeights = leadWeights_Slider.value;
        BCD_weight = BCD_Weight_Slider.value;
        BCD_Capacity = BCD_Capacity_Slider.value;
    }


    // Setting UI-value-fields from variable values.
    void SetUI_Values() {
        waterTemp_Val.text = waterTemp.ToString();
        waterDensity_Val.text = waterDensity.ToString();
        atmosphericPressure_Val.text = atmosphericPressue.ToString();
        diverHeight_Val.text = diverHeight.ToString();
        diverWeight_Val.text = diverWeight.ToString();
        RMV_Val.text = RMV.ToString();
        suitThickness_Val.text = suitThickness.ToString();

        tankMaterials_Drop.value = matValue;
        tankEmptWeight_Val.text = tankEmptyWeight.ToString();
        tankStartPress_Val.text = tankStartPress.ToString();
        tankCapacity_Val.text = tankCapacity.ToString();
        leadWeights_Val.text = leadWeights.ToString();
        BCD_Weight_Val.text = BCD_weight.ToString();
        BCD_Capacity_Val.text = BCD_Capacity.ToString();
    }

    void SetListeners() {
        waterTemp_Slider.onValueChanged.AddListener((val) => {
            waterTemp = val;
            waterTemp_Val.text = val.ToString();
        });
        waterDensity_Slider.onValueChanged.AddListener((val) => {
            waterDensity = val;
            waterDensity_Val.text = val.ToString();
        });
        atmosphericPressure_Slider.onValueChanged.AddListener((val) => {
            atmosphericPressue = val;
            atmosphericPressure_Val.text = val.ToString();
        });
        diverHeight_Slider.onValueChanged.AddListener((val) => {
            diverHeight = val;
            diverHeight_Val.text = val.ToString();
        });
        diverWeight_Slider.onValueChanged.AddListener((val) => {
            diverWeight = val;
            diverWeight_Val.text = val.ToString();
        });
        RMV_Slider.onValueChanged.AddListener((val) => {
            RMV = val;
            RMV_Val.text = val.ToString();
        });
        suitThickness_Slider.onValueChanged.AddListener((val) => {
            suitThickness = val;
            suitThickness_Val.text = val.ToString();
        });
        tankMaterials_Drop.onValueChanged.AddListener((val) => {
            matValue = val;
            if (matValue == 0) tankMaterial = tankMaterials.Aluminium;
            else if (matValue == 1) tankMaterial = tankMaterials.Steel;
        });
        tankEmptWeight_Slider.onValueChanged.AddListener((val) => {
            tankEmptyWeight = val;
            tankEmptWeight_Val.text = val.ToString();
        });
        tankStartPress_Slider.onValueChanged.AddListener((val) => {
            tankStartPress = val;
            InitDive(); // Have to init to recalculate gasRemainingMass.
            tankStartPress_Val.text = val.ToString();
        });
        tankCapacity_Slider.onValueChanged.AddListener((val) => {
            tankCapacity = val;
            tankCapacity_Val.text = val.ToString();
        });
        leadWeights_Slider.onValueChanged.AddListener((val) => {
            leadWeights = val;
            leadWeights_Val.text = val.ToString();
        });
        BCD_Weight_Slider.onValueChanged.AddListener((val) => {
            BCD_weight = val;
            BCD_Weight_Val.text = val.ToString();
        });
        BCD_Capacity_Slider.onValueChanged.AddListener((val) => {
            BCD_Capacity = val;
            BCD_Capacity_Val.text = val.ToString();
        });
        showTestDataToggle.onValueChanged.AddListener((val) => {
            testDataGO.SetActive(val);
        });
    }


    // Entry point for updating UI (panel on diving watch, etc.)
    // To be adjusted with data as needed.
    void UpdateUserData() {
        // Time
        timeStr = DateTime.Now.ToString("HH:mm:ss");
        localTime_Val.text = timeStr; 
        // Dive time
        TimeSpan ts1 = TimeSpan.FromSeconds(diveTime);
        diveTimeStr = ts1.ToString(@"hh\hmm\mss\s");
        diveDuration_Val.text = diveTimeStr;
        // Depth
        depthStr = Depth(_VR_Camera.transform.position.y).ToString("0.0") + " m";
        depth_Val.text = depthStr;
        // Tank pressure
        tankPressStr = tankPress.ToString("0.0") + " bar";
        tankPressure_Val.text = tankPressStr;
        // Time left at depth
        TimeSpan ts2 = TimeSpan.FromSeconds(timeAtDepth);
        timeAtDepthStr = ts2.ToString(@"hh\hmm\mss\s");
        timeAtDepth_Val.text = timeAtDepthStr;

        netBuoyancy_Val.text = netBuoyancy.ToString();

        /*
        Debug.Log("Time: " + timeStr);
        Debug.Log("Dive time: " + diveTimeStr);
        Debug.Log("Depth: " + depthStr);
        Debug.Log("Tank pressure: " + tankPressStr);
        Debug.Log("Time at depth: " + timeAtDepthStr);
        Debug.Log(" ");
        */
    }


    void TakeBreath() {

        float bars = PressureAtDepth(_VR_Camera.transform.position.y) / 1000f;   // bar, Pressure at current depth.
        // Diver air volume per breath * bars. Gives equivalent surface volume, for 1 breath.
        float breathVol = (RMV / breathsPerMin) * bars;
        float breathGasMass = GasMassAtSurfacePress(breathVol);
        // Book keeping
        gasRemainingMass -= breathGasMass;
        tankPress = TankPressure(gasRemainingMass);

        /*
        Debug.Log("Current air density:" + GasDensity(atmosphericPressue/1000f));
        Debug.Log("Bars:" + bars + ", breath vol:" + breathVol + ", gasFillMass:" + gasStartMass + ", breathMass:" + breathGasMass);
        Debug.Log("Updated tank pressure:" + tankPress);
        */
    }


    // Returning mass of gas used during ascent.
    float AscentGasMass()
    {
        depth = Depth(_VR_Camera.transform.position.y);
        float avgSurfacingDepth = depth / 2f;                                           // m, The average depth during ascent.
        float swimMinsToSurface = depth / ascentMaxMtrPerMin;                           // minutes, Minimum time to surface, excluding safety stop.
        float breathCount = swimMinsToSurface * breathsPerMin;                          // Number of breaths to surface, excluding safety stop.
        float avgBars = PressureAtDepth(_VR_Camera.transform.position.y / 2f) / 1000f;  // bar, Pressure at average surfacing depth.
        float breathVol = (RMV / breathsPerMin) * avgBars;
        float ascentGasMass = GasMassAtSurfacePress(breathVol) * breathCount;           // kg, Total mass of ascent air.

        /*
            Debug.Log("Avg surf depth: " + avgSurfacingDepth);
            Debug.Log("Mins to surface: " + swimMinsToSurface);
            Debug.Log("Number of breaths to surface: " + breathCount);
            Debug.Log("Avg. pressure during ascent: " + avgBars);
            Debug.Log("One breath avg. vol: " + breathVol);
            Debug.Log("Mass of one average breath: " + GasMassAtSurfacePress(breathVol));
            Debug.Log("Ascent gas mass: " + ascentGasMass);         
        */
        return ascentGasMass;
    }


    // Returns mass of gas used during a stop.
    float StopGasMass(float stopDuration, float depth) {
        float breathCount = stopDuration * breathsPerMin;
        float breathVol = (RMV / breathsPerMin) * PressureAtDepth(_waterSurface - depth) / 1000f;
        float stopGasMass = GasMassAtSurfacePress(breathVol) * breathCount;
        return stopGasMass;
    }


    // Time left at current depth.
    float TimeAtDepth(float massOfGasToUse) {

        float bars = PressureAtDepth(_VR_Camera.transform.position.y) / 1000f;   // bar, Pressure at current depth.
        // Diver air volume per breath * bars. Gives equivalent surface volume, for 1 breath.
        float breathVol = (RMV / breathsPerMin) * bars;
        float breathGasMass = GasMassAtSurfacePress(breathVol);
        /*
        Debug.Log("Mass of gas to use: " + massOfGasToUse);
        Debug.Log("Volume of one breath: " + breathVol);
        Debug.Log("Mass of one breath: " + breathGasMass);
        */
        float breathCount = massOfGasToUse / breathGasMass;     // Number of breaths until diver has to begin ascent.
        float oneBreathTime = 60f / breathsPerMin;              // s, duration of 1 breath.
        timeAtDepth = breathCount * oneBreathTime;
        return timeAtDepth;    
    }


    // Doing gravity manually, as a force, since we want it to depend on mass.
    // This makes sense, then it is comparable to buoyancy,
    // which is also calculated using Newtons 2nd law and Archimedes principle
    float ApplyGravity(Rigidbody rb)
    {
        float force = -g * rb.mass;
        Vector3 gravityForce = new Vector3(0f, force, 0f);
        _rb.AddForce(gravityForce, ForceMode.Force);
        return force;
    }


    // Submergence is calculated relative to VR-Camera position (yPos).
    float CalcSubmergence(GameObject obj)
    {
        float faceAboveWater = 0.1f;
        float faceHeight = 0.3f;
        float yPos = obj.transform.position.y;

        if (yPos > _waterSurface + faceHeight + faceAboveWater + 0.1f) return 0f;
        else if (yPos >= _waterSurface + faceAboveWater &&
                 yPos <= _waterSurface + faceAboveWater + faceHeight)
            // Linear falloff of submergence y = ax + b
            return (-1f/faceHeight) * yPos + (1f + (_waterSurface + faceAboveWater) / faceHeight);
        else return 1f;
    }


    // Water lift of divers body.
    float BodyBuoyancy(Rigidbody rb, float submergence)
    {
        float m = rb.mass;
        float buoyancy = m * g * submergence;
        Vector3 buoyancyForce = new Vector3(0f, buoyancy, 0f);
        rb.AddForce(buoyancyForce, ForceMode.Force);
        return buoyancy;
    }


    // Water 'net' lift of divers (Neoprene) suit.
    float SuitBuoyancy(float submergence)
    {
        const float _neopreneFoamDensity = 0.1f;

        // The Gehan and George formula for body surface.
        float diverSurface = 0.0235f * Mathf.Pow(diverHeight, 0.42246f) * Mathf.Pow(diverWeight, 0.51456f);
        
        float suitSurfaceVolume = diverSurface * suitThickness;      // Will be in liter
        float suitMass = _neopreneFoamDensity * suitSurfaceVolume;   // Will be in kg (reducing buoyancy by this, instead of increasing gravity force)

        float suitNewVolume = BoyleVolAtDepth(_VR_Camera, suitSurfaceVolume);
        float buoyancy = ((waterDensity/1000f) * suitNewVolume - suitMass) * g * submergence;   // Force in Newton.

        Vector3 buoyancyForce = new Vector3(0f, buoyancy, 0f);                                  // Force vector.
        _rb.AddForce(buoyancyForce, ForceMode.Force);
        return buoyancy;
    }


    // Water 'net' lift of divers Buoyance Control Device (BCD).
    float BCD_Buoyancy(float submergence) {
        float bars = PressureAtDepth(_VR_Camera.transform.position.y) / 1000f;
        float BCD_newVolStep = BoyleNewVol(BCD_VolStep, bars, atmosphericPressue / 1000f);
        float deltaMass = GasMassAtSurfacePress(BCD_newVolStep);

        if (BCD_OldBoyleVol <= BCD_Capacity * 1000 - BCD_VolStep && rightContB.action.IsPressed())
        {
            BCD_SurfAirVol += BCD_newVolStep;
            gasRemainingMass -= deltaMass;
        }
        if (BCD_OldBoyleVol >= BCD_VolStep && rightContA.action.IsPressed())
        {
            BCD_SurfAirVol -= BCD_newVolStep;
        }
        float BCD_BoyleVolume = BoyleVolAtDepth(_VR_Camera, BCD_SurfAirVol);
        BCD_OldBoyleVol = BCD_BoyleVolume;

        float buoyancy = ((waterDensity/1000f) * BCD_BoyleVolume - BCD_weight) * g * submergence;

        Vector3 buoyancyForce = new Vector3(0f, buoyancy, 0f);
        _rb.AddForce(buoyancyForce, ForceMode.Force);
        return buoyancy;
    }


    // Water 'net' lift of divers tank.
    float TankBuoyancy(float submergence) {

        float tankWaterDisp = TankWaterDisp(tankCapacity);              // Volume of displaced water.
        float buoyancy = ( (waterDensity / 1000f) * tankWaterDisp  -    // Mass of displaced water.
                           (tankEmptyWeight + gasRemainingMass)         // Mass of tank and its remaining gas.
                         ) * g * submergence;                           // = Buoyancy in Newton.

        Vector3 buoyancyForce = new Vector3(0f, buoyancy, 0f);          // Force vector.
        _rb.AddForce(buoyancyForce, ForceMode.Force);                   // Applying force.
        return buoyancy;
    }


    // Calculating drag coefficient by the formula:  d = 1/2 * d * v^2 * A * Cd
    void Drag(Rigidbody rb, float submergence)
    {
        float density = waterDensity;                   // Kg/m3. Salt water is denser than fresh water.
        float speedSqr = rb.velocity.sqrMagnitude;      // speed squared, of object relative to medium.
        const float area = 0.25f;                       // m2, cross-sectional area of moving obj, orthogonal to direction of motion.
        const float dragCoeff = 0.7f;                   // Higher -> less aerodynamic
        float dragForce = 0.5f * density * speedSqr * area * dragCoeff;

        // Calculating impulse before and after applying drag.
        float deltaI = dragForce * 0.02f;   // Calculating drag for the duration of a fixed update.
        float speedBef = rb.velocity.magnitude;
        float Ibef = rb.mass * speedBef;
        float Iaft = Ibef - deltaI;

        float speedRatio = Iaft / Ibef;
        rb.velocity *= speedRatio;
    }


    // =================================================================================
    // Helper functions
    // =================================================================================
    float BarToPSI(float bar)
    {
        return bar * 14.50377f;
    }


    float PSItoBar(float PSI)
    {
        return PSI * 0.06895f;
    }


    float CuFtToLiter(float cuFt)
    {
        float liters = 0f;
        float oneFoot = 3.048f; // Deci meters.
        float oneCubicFootInLiters = Mathf.Pow(oneFoot, 3f);
        liters = cuFt * oneCubicFootInLiters;
        return liters;
    }


    float BarToPascal(float bar)
    {
        return bar * 100000f;
    }


    float PascalToBar(float pascal)
    {
        return pascal / 100000f;
    }


    // Using the equation for an ideal gas to calculate density.
    float GasDensity(float pressureInBar)
    {
        const float MM_air = 29f;               // Molar mass of atmospheric air.
        float P = BarToPascal(pressureInBar);   // Pressure in Pascal
        const float R = 8314.46f;               // Ideal gas constant.
        float T = 273.15f + waterTemp;          // Water temperature in Kelvin
        float density = (MM_air * P) / (R * T); // Formula derived from ideal gas equation
        return density;
    }


    // Using the equation for an ideal gas to calculate mass
    float GasMassAtSurfacePress(float literVolume)
    {
        float V = literVolume / 1000f;                      // m3
        float P = BarToPascal(atmosphericPressue/1000f);    // Pascal
        const float MM_air = 29f;                           // Molar mass of atmospheric air.

        const float R = 8314.46f;                           // Ideal gas constant.
        float T = 273.15f + waterTemp;                      // Water temperature in Kelvin
        float mass = (P * V * MM_air) / (R * T);            // Formula derived from ideal gas equation
        return mass;
    }


    float TankPressure(float remainingGasMassInKg) {
        float m = remainingGasMassInKg;
        const float R = 8314.46f;                           // Ideal gas constant.
        float T = 273.15f + waterTemp;                      // Kelvin, Water temperature.

        float V = tankCapacity / 1000f;                     // m3, tank capatity.
        const float MM_air = 29f;                           // Molar mass of atmospheric air.

        float P = (m * R * T) / (V * MM_air);
        return PascalToBar(P);
    }


    // Returns the diving-tanks water displacement in liters
    float TankWaterDisp(float capacity) {
        float tankMatDens;
        if (tankMaterial == tankMaterials.Aluminium) tankMatDens = 2.7f;    // kg/liter
        else if (tankMaterial == tankMaterials.Steel) tankMatDens = 7.83f;  // kg/liter
        else tankMatDens = 7.83f;   // Falling back to steel.
        float matVol = tankEmptyWeight / tankMatDens;
        float totVol = matVol + capacity;
        return totVol;
    }


    // Using Boyles equation p1*v1 = p2*v2
    float BoyleNewPress(float oldPress, float oldVol, float newVol)
    {
        return oldPress * (oldVol / newVol);    // A new pressure
    }

    float BoyleNewVol(float oldVol, float oldPress, float newPress)
    {
        return oldVol * (oldPress / newPress);    // A new volume
    }


    float BoyleVolAtDepth(GameObject GO, float surfaceVol)
    {
        float yPos = GO.transform.position.y;
        return surfaceVol * atmosphericPressue / PressureAtDepth(yPos);   // A new volume
    }


    // Returns water depth of the game object
    float Depth(float yPos) {
        // Diver is comming out of the water
        if (yPos > 0.1f && diveGoingOn) {
            diveGoingOn = false;
            InitDive();
        }
        // Dive is going on
        if (yPos >= _waterSurface) return 0;    // We don't want a depth above the water surface.
        return Mathf.Abs(yPos) + _waterSurface;                 // m, positive number.
    }


    // Pressure of water pillar.
    float PressureAtDepth(float yPos) {

        float pressure = Depth(yPos) * waterDensity * g;    // In pascal.
        pressure /= 100f;                                   // In hPa (same as mBar).
        pressure += atmosphericPressue;                     // In hPa (same as mBar).
        return pressure;
    }
    // =================================================================================
    // =================================================================================


    private void SwimMovement()
    {
        _strokeCooldown += Time.fixedDeltaTime;

        // Diver can swim if these conditions are met:
        if (_strokeCooldown > minStrokeInterval &&
            (leftContRef.action.IsPressed() || rightContRef.action.IsPressed())
           )
        {
            var leftHandVel = Vector3.zero;     // Velocity of left controller
            var rightHandVel = Vector3.zero;    // Velocity of right controller
            //var leftHandPos = Vector3.zero;     // Position of left controller
            //var rightHandPos = Vector3.zero;    // Position of right controller


            // If either grip button is pressed the controllers velocity is applied
            if (leftContRef.action.IsPressed())
            {
                leftHandVel = leftContVel.action.ReadValue<Vector3>();
                //leftHandPos = leftContPos.action.ReadValue<Vector3>();
            }
            if (rightContRef.action.IsPressed())
            {
                rightHandVel = rightContVel.action.ReadValue<Vector3>();
                //rightHandPos = rightContPos.action.ReadValue<Vector3>();
            }

            Vector3 handVel = leftHandVel + rightHandVel;   //  Combined velocity vector of both hands
            //Vector3 handPos = new Vector3( (leftHandPos.x + rightHandPos.x)/2, (leftHandPos.y + rightHandPos.y) / 2, (leftHandPos.z + rightHandPos.z)/ 2);
            handVel *= -1;  // * -1 because we are swimming in the opposite direction of the hand movement

            //Debug.Log("BodyPos: " + _rb.transform.position);
            //Debug.Log("HandPos: " + handPos);

            // Comparing velocity magnitude (squared (less processing)) with minimum force
            if (handVel.sqrMagnitude > minForce * minForce)
            {
                // Vector3 direction = _rb.transform.position - transform.position; //EXP #######################################
                Vector3 worldVel = _trackRef.TransformDirection(handVel);    // Transforming the hands velocity from local space to world space
                //Vector3 worldPos = _trackRef.TransformDirection(handPos);
                //Debug.Log("HandWorldPos: " + worldPos);
                //_rb.AddForceAtPosition(worldVel * swimForce, worldPos, ForceMode.Acceleration); // EXP
                _rb.AddForce(worldVel * swimForce, ForceMode.Acceleration); // Applying force to the diver
                _strokeCooldown = 0f;
            }
        }


    }

}
