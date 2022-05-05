using System;
using System.Collections;
using UnityEngine;
using TMPro;
using UnityEngine.InputSystem;


[RequireComponent(typeof(Rigidbody))]

public class DiverController : MonoBehaviour
{
    public DiveSettings diveSettings;

    private const float g = 9.82f;         // Gravitational constant
    private float _waterSurface;

    [Header("Swimming Variables")]
    public float swimForce = 2f;           // Force of strokes
    public float minForce = 0f;            // Min force for a stroke to be registered (default 0.25)
    public float minStrokeInterval = 0f;   // Min time between strokes (default 0.25)
    public float _strokeCooldown;          // Timer which is reset to 0 at each stroke
    public float submergence = 0;          // How much of the diver is submerged (a bit fictive as of now, it's relative to the VR-headset position)


    [Header("BCD Variables")]
    public float BCDOldBoyleVol = 0f;     // l, Previous calculated volume of BCD air.
    public float BCDSurfAirVol = 0f;      // l, Equivalent surface volume of air in BCD.
    public float BCDVolStep = 0.005f;     // l, Air added to BCD per step (fixed update).


    #region Private Variables
    // Breath
    int breathsPerMin = 6;
    float breathCooldown = 60f;             // 60s / x breaths per min.
    float breathTimer = 0;

    // Gas & descending, ascending
    float gasStartMass;
    float gasRemainingMass;
    float safetyEndPress = 35f;             // bar, Minimum pressure to surface with (equivalent to 500 PSI).
    float ascentMaxMtrPerMin = 10f;         // m, Maximum meters/min when ascending.
    float buoyancy = 0f;

    // User data variables
    bool diveGoingOn = false;               // Has the dive begun?
    float userDataCooldown = 0.25f;            // s, Update frequecy of user data.
    float userDataUpdTimer = 0;

    //
    float previousDepth;                    // depth one second ago
    float time;                             // hh:mm:ss, current local time
    float diveTime;                         // s, Time elapsed since the dive began.
    float depth;                            // m, current diver depth
    int maxDepth;                           // m, max depth reached
    float tankPress;                        // bar, current pressure in tank
    float timeAtDepth;                      // s, The time left at current depth, (with time to surface subtracted).
    #endregion                              // Note: the above is not NDL (No Decompression Limit)

    //
    public string timeStr;
    public string diveTimeStr = "00h00m";
    public string depthStr;
    public string tankPressStr;
    public string timeAtDepthStr = "00h00m";
    public string maxDepthStr;
    public float ascentRate;


    [Header("Object References")]
    public Transform _waterBody;                        // Gameobject holding our water
    [SerializeField] private Transform _XROrigin;       // Root Object of Player- or XR-Rig
    [SerializeField] private Transform _VRCamera;       // player Camera
    Rigidbody _rb;                                      // Note: divers rb is offset to (0,1,0)


    [Header("Test Data UI Elements")]
    [SerializeField] private TextMeshProUGUI localTimeValue;
    [SerializeField] private TextMeshProUGUI diveDurationValue;
    [SerializeField] private TextMeshProUGUI tankPressureValue;
    [SerializeField] private TextMeshProUGUI timeAtDepthValue;
    [SerializeField] private TextMeshProUGUI depthValue;
    [SerializeField] private TextMeshProUGUI netBuoyancyValue;
    [SerializeField] private TextMeshProUGUI BCD_AirVolValue;


    #region Controller Actions
    [Header("Left Controller Actions")]
    [SerializeField] InputActionReference leftControllerGripPress;
    [SerializeField] InputActionReference leftControllerVelocity;
    [SerializeField] InputActionReference leftControllerPosition;
    [SerializeField] InputActionReference leftControllerPrimaryButton;
    [SerializeField] InputActionReference leftControllerSecondaryButton;
    [SerializeField] InputActionReference leftControllerThumbstick;
    [Header("Right Controller Actions")]
    [SerializeField] InputActionReference rightControllerGripPress;
    [SerializeField] InputActionReference rightControllerVelocity;
    [SerializeField] InputActionReference rightControllerPosition;
    [SerializeField] InputActionReference rightControllerPrimaryButton;
    [SerializeField] InputActionReference rightControllerSecondaryButton;
    #endregion


    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _rb.constraints = RigidbodyConstraints.FreezeRotation;
        _rb.mass = diveSettings.diverWeight;
        _rb.useGravity = false;
        _waterSurface = _waterBody.position.y;

        // Initial values.
        breathCooldown /= breathsPerMin;
        InitDive();
    }



    private void FixedUpdate()
    {
        buoyancy = ApplyGravity(_rb);        // Always apply gravity.

        // Submergence controls the beginning of the dive.
        submergence = PlayerSubmergence(_VRCamera.gameObject);    // Degree of submergence.

        if (submergence > 0)
        {
            diveGoingOn = true;                     // Dummy variable (i.e could be set with UI button)
            //Debug.Log("diveGoinOn = true");
        }
        else if (diveGoingOn)
        {
            diveGoingOn = false;
            //Debug.Log("diveGoinOn = false");
            InitDive();
        }

        // Updating timers.
        userDataUpdTimer += Time.deltaTime;
        if (diveGoingOn)
        {
            breathTimer += Time.deltaTime;
            diveTime += Time.deltaTime;
        }

        // Taking a breath (if the timer is running).
        if (breathTimer > breathCooldown)
        {
            breathTimer = 0;
            TakeBreath();
        }

        // Updating water drag and buoyancy if the dive has begun.
        if (diveGoingOn)
        {
            Drag(_rb, submergence);
            buoyancy += BodyBuoyancy(_rb, submergence);
            buoyancy += SuitBuoyancy(submergence);
            buoyancy += BCD_Buoyancy(submergence);
            buoyancy += TankBuoyancy(submergence);
            buoyancy += LeadBuoyancy(submergence);
        }

        // Allowing swim movement if more than half submerged.
        if (submergence > 0.5f) SwimMovement();

        // Updating data for UI
        if (userDataUpdTimer > userDataCooldown)
        {
            userDataUpdTimer = 0;

            // Calculating max stay-time at current depth.
            // ============================================
            // Subtracting safety-gas mass.
            float gasEndMass = GasDensity(safetyEndPress) * diveSettings.tankCapacity / 1000;   // kg. Tank gas mass, when tank pressure is at 'safetyEndPress'.
            float gasToUseMass = gasRemainingMass - gasEndMass;
            // Subtracting mass of gas used during ascent.
            gasToUseMass -= AscentGasMass();
            // Subtracting mass of gas used during 5min safety stop at 5m.
            gasToUseMass -= StopGasMass(5f, 5f);
            // We can now calculate remaining time at current depth.
            timeAtDepth = TimeAtDepth(gasToUseMass);
            // ============================================

            CalculateAscentRate(Depth(_VRCamera.position.y));
            UpdateUserData();
        }
    }


    // Setting values for a new dive.
    public void InitDive()
    {
        breathTimer = 0;
        diveTime = 0;
        BCDOldBoyleVol = 0f;           // Emptying the BCD
        BCDSurfAirVol = 0;
        RefillTank();
    }

    public void RefillTank()
    {
        tankPress = diveSettings.tankStartPress;     // Refilling the tank
        gasStartMass = GasDensity(tankPress) * diveSettings.tankCapacity / 1000f;
        gasRemainingMass = gasStartMass;
    }

    void CalculateAscentRate(float currentDepth)
    {
        previousDepth = currentDepth;
        ascentRate = (currentDepth - previousDepth) / 60;
        if (ascentRate < 0) ascentRate = 0;
    }

    // Entry point for updating UI (panel on diving watch, etc.)
    // To be adjusted with data as needed.
    void UpdateUserData()
    {
        // Time
        timeStr = DateTime.Now.ToString("HH:mm");
        if (localTimeValue != null) { localTimeValue.text = timeStr; }
        

        // Dive time (måske bare mm)
        TimeSpan ts1 = TimeSpan.FromSeconds(diveTime);
        diveTimeStr = ts1.ToString(@"mm");
        if (diveDurationValue != null) { diveDurationValue.text = diveTimeStr; }

        // Depth
        depthStr = Depth(_VRCamera.position.y).ToString("00.0");
        if (depthValue != null) { depthValue.text = depthStr; }

        // Tank pressure
        tankPressStr = tankPress.ToString("0.0") + " bar";
        if (tankPressureValue != null) { tankPressureValue.text = tankPressStr; }

        // Time left at depth (måske mm:ss)
        TimeSpan ts2 = TimeSpan.FromSeconds(timeAtDepth);
        timeAtDepthStr = ts2.ToString(@"mm\:ss");
        if (timeAtDepthValue != null) { timeAtDepthValue.text = timeAtDepthStr; }

        if (netBuoyancyValue != null)
            netBuoyancyValue.text = (buoyancy / g).ToString("0.00") + " kg";

        if (BCD_AirVolValue != null)
        BCD_AirVolValue.text = BCDOldBoyleVol.ToString("0.00") + " ltr";

        // Max depth dived
        if (maxDepth < depth)
        {
            maxDepth = Mathf.RoundToInt(depth);
        }
        maxDepthStr = maxDepth.ToString();



        /*
        Debug.Log("Time: " + timeStr);
        Debug.Log("Dive time: " + diveTimeStr);
        Debug.Log("Depth: " + depthStr);
        Debug.Log("Tank pressure: " + tankPressStr);
        Debug.Log("Time at depth: " + timeAtDepthStr);
        Debug.Log(" ");
        */
    }


    void TakeBreath()
    {

        float bars = PressureAtDepth(_VRCamera.position.y) / 1000f;   // bar, Pressure at current depth.
        // Diver air volume per breath * bars. Gives equivalent surface volume, for 1 breath.
        float breathVol = (diveSettings.RMV / breathsPerMin) * bars;
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
        depth = Depth(_VRCamera.position.y);
        float avgSurfacingDepth = depth / 2f;                                           // m, The average depth during ascent.
        float swimMinsToSurface = depth / ascentMaxMtrPerMin;                           // minutes, Minimum time to surface, excluding safety stop.
        float breathCount = swimMinsToSurface * breathsPerMin;                          // Number of breaths to surface, excluding safety stop.
        float avgBars = PressureAtDepth(_VRCamera.position.y / 2f) / 1000f;             // bar, Pressure at average surfacing depth.
        float breathVol = (diveSettings.RMV / breathsPerMin) * avgBars;
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
    float StopGasMass(float stopDuration, float depth)
    {
        float breathCount = stopDuration * breathsPerMin;
        float breathVol = (diveSettings.RMV / breathsPerMin) * PressureAtDepth(_waterSurface - depth) / 1000f;
        float stopGasMass = GasMassAtSurfacePress(breathVol) * breathCount;
        return stopGasMass;
    }


    // Time left at current depth.
    float TimeAtDepth(float massOfGasToUse)
    {

        float bars = PressureAtDepth(_VRCamera.position.y) / 1000f;   // bar, Pressure at current depth.
        // Diver air volume per breath * bars. Gives equivalent surface volume, for 1 breath.
        float breathVol = (diveSettings.RMV / breathsPerMin) * bars;
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
    float PlayerSubmergence(GameObject VR_Camera)
    {
        float top_yPos = VR_Camera.transform.position.y;
        float bottom_yPos = _rb.transform.position.y;
        float virtualBodyHeight = Mathf.Abs(bottom_yPos - top_yPos);

        if (top_yPos > _waterSurface + virtualBodyHeight) return 0f;
        else if (top_yPos <= _waterSurface) return 1f;
        else
        {
            float bodyPartInWater = Mathf.Abs(bottom_yPos - _waterSurface);
            return bodyPartInWater / virtualBodyHeight;
        }
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
        float diverSurface = 0.0235f * Mathf.Pow(diveSettings.diverHeight, 0.42246f) * Mathf.Pow(diveSettings.diverWeight, 0.51456f);

        float suitSurfaceVolume = diverSurface * diveSettings.suitThickness;      // Will be in liter
        float suitMass = _neopreneFoamDensity * suitSurfaceVolume;   // Will be in kg (reducing buoyancy by this, instead of increasing gravity force)

        float suitNewVolume = BoyleVolAtDepth(_VRCamera.gameObject, suitSurfaceVolume);
        float buoyancy = ((diveSettings.waterDensity / 1000f) * suitNewVolume - suitMass) * g * submergence;   // Force in Newton.

        Vector3 buoyancyForce = new Vector3(0f, buoyancy, 0f);       // Force vector.
        _rb.AddForce(buoyancyForce, ForceMode.Force);
        return buoyancy;
    }


    // Water 'net' lift of divers Buoyance Control Device (BCD).
    float BCD_Buoyancy(float submergence)
    {
        float bars = PressureAtDepth(_VRCamera.position.y) / 1000f;
        float BCD_newVolStep = BoyleNewVol(BCDVolStep, bars, diveSettings.atmosphericPressure / 1000f);
        float deltaMass = GasMassAtSurfacePress(BCD_newVolStep);

        if (BCDOldBoyleVol <= diveSettings.BCD_Capacity * 1000 - BCDVolStep && rightControllerSecondaryButton.action.IsPressed())
        {
            BCDSurfAirVol += BCD_newVolStep;
            gasRemainingMass -= deltaMass;
        }
        if (BCDOldBoyleVol >= BCDVolStep && rightControllerPrimaryButton.action.IsPressed())
        {
            BCDSurfAirVol -= BCD_newVolStep;
        }
        float BCD_BoyleVolume = BoyleVolAtDepth(_VRCamera.gameObject, BCDSurfAirVol);
        BCDOldBoyleVol = BCD_BoyleVolume;

        float buoyancy = ((diveSettings.waterDensity / 1000f) * BCD_BoyleVolume - diveSettings.BCD_weight) * g * submergence;

        Vector3 buoyancyForce = new Vector3(0f, buoyancy, 0f);
        _rb.AddForce(buoyancyForce, ForceMode.Force);
        return buoyancy;
    }


    // Water 'net' lift of divers tank.
    float TankBuoyancy(float submergence)
    {

        float tankWaterDisp = TankWaterDisp(diveSettings.tankCapacity);             // Volume of displaced water.
        float buoyancy = ((diveSettings.waterDensity / 1000f) * tankWaterDisp -   // Mass of displaced water.
                           (diveSettings.tankEmptyWeight + gasRemainingMass)        // Mass of tank and its remaining gas.
                         ) * g * submergence;                                       // = Buoyancy in Newton.

        Vector3 buoyancyForce = new Vector3(0f, buoyancy, 0f);                      // Force vector.
        _rb.AddForce(buoyancyForce, ForceMode.Force);                               // Applying force.
        return buoyancy;
    }


    // Water lift of lead
    float LeadBuoyancy(float submergence)
    {
        float leadDensity = 11.34f;                                                 // kg/liter
        float leadVolume = diveSettings.leadWeights / leadDensity;

        float buoyancy = ((diveSettings.waterDensity / 1000f) * leadVolume -       // Mass of displaced water.
                           diveSettings.leadWeights                                 // Mass of lead
                         ) * g * submergence;                                       // = Buoyancy in Newton.

        Vector3 buoyancyForce = new Vector3(0f, buoyancy, 0f);                      // Force vector.
        _rb.AddForce(buoyancyForce, ForceMode.Force);                               // Applying force.
        return buoyancy;
    }


    // Calculating drag coefficient by the formula:  d = 1/2 * d * v^2 * A * Cd
    void Drag(Rigidbody rb, float submergence)
    {
        float density = diveSettings.waterDensity;      // Kg/m3. Salt water is denser than fresh water.
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
        if (speedRatio > 0) rb.velocity *= speedRatio;
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
        const float MM_air = 29f;                       // Molar mass of atmospheric air.
        float P = BarToPascal(pressureInBar);           // Pressure in Pascal
        const float R = 8314.46f;                       // Ideal gas constant.
        float T = 273.15f + diveSettings.waterTemp;     // Water temperature in Kelvin
        float density = (MM_air * P) / (R * T);         // Formula derived from ideal gas equation
        return density;
    }


    // Using the equation for an ideal gas to calculate mass
    float GasMassAtSurfacePress(float literVolume)
    {
        float V = literVolume / 1000f;                      // m3
        float P = BarToPascal(diveSettings.atmosphericPressure / 1000f);    // Pascal
        const float MM_air = 29f;                           // Molar mass of atmospheric air.

        const float R = 8314.46f;                           // Ideal gas constant.
        float T = 273.15f + diveSettings.waterTemp;                      // Water temperature in Kelvin
        float mass = (P * V * MM_air) / (R * T);            // Formula derived from ideal gas equation
        return mass;
    }


    float TankPressure(float remainingGasMassInKg)
    {
        float m = remainingGasMassInKg;
        const float R = 8314.46f;                           // Ideal gas constant.
        float T = 273.15f + diveSettings.waterTemp;         // Kelvin, Water temperature.

        float V = diveSettings.tankCapacity / 1000f;        // m3, tank capatity.
        const float MM_air = 29f;                           // Molar mass of atmospheric air.

        float P = (m * R * T) / (V * MM_air);
        return PascalToBar(P);
    }


    // Returns the diving-tanks water displacement in liters
    float TankWaterDisp(float capacity)
    {
        float tankMatDens;
        if (diveSettings.tankMaterial == DiveSettings.tankMaterials.Aluminium) tankMatDens = 2.7f;    // kg/liter
        else if (diveSettings.tankMaterial == DiveSettings.tankMaterials.Steel) tankMatDens = 7.83f;  // kg/liter
        else tankMatDens = 7.83f;   // Falling back to steel.
        float matVol = diveSettings.tankEmptyWeight / tankMatDens;
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
        return surfaceVol * diveSettings.atmosphericPressure / PressureAtDepth(yPos);   // A new volume
    }


    // Returns water depth of the game object
    float Depth(float yPos)
    {

        /*
        // Diver is comming out of the water
        if (yPos > _waterSurface + diveSettings.diverHeight/100f && diveGoingOn)
        {
            diveGoingOn = false;
            InitDive();
        }
        */

        if (yPos >= _waterSurface) return 0;         // We don't want a depth above the water surface.
        return Mathf.Abs(_waterSurface - yPos);      // m, positive number.
    }



    // Pressure of water pillar.
    float PressureAtDepth(float yPos)
    {

        float pressure = Depth(yPos) * diveSettings.waterDensity * g;    // In pascal.
        pressure /= 100f;                                                // In hPa (same as mBar).
        pressure += diveSettings.atmosphericPressure;                    // In hPa (same as mBar).
        return pressure;
    }
    // =================================================================================
    // =================================================================================


    private void SwimMovement()
    {
        _strokeCooldown += Time.fixedDeltaTime;

        // Vertical movement (simulated use of legs) (with left controller y(up) and x(down) buttons)
        // ==========================================================================================
        if (leftControllerSecondaryButton.action.IsPressed())
        {
            Vector3 worldVel = _XROrigin.TransformDirection(Vector3.up);
            _rb.AddForce(worldVel * swimForce * 0.3f, ForceMode.Acceleration);
        }
        if (leftControllerPrimaryButton.action.IsPressed())
        {
            Vector3 worldVel = _XROrigin.TransformDirection(-1 * Vector3.up);
            _rb.AddForce(worldVel * swimForce * 0.3f, ForceMode.Acceleration);
        }

        // Movement (swimming) with hands; with left & right controllers movement.
        // Grip button has to be pressed when 'pulling' the controller.
        // =======================================================================
        if (_strokeCooldown > minStrokeInterval &&
            (leftControllerGripPress.action.IsPressed() || rightControllerGripPress.action.IsPressed())
           )
        {
            var leftHandVel = Vector3.zero;     // Velocity of left controller
            var rightHandVel = Vector3.zero;    // Velocity of right controller
            //var leftHandPos = Vector3.zero;     // Position of left controller
            //var rightHandPos = Vector3.zero;    // Position of right controller


            // If either grip button is pressed the controllers velocity is applied
            if (leftControllerGripPress.action.IsPressed())
            {
                leftHandVel = leftControllerVelocity.action.ReadValue<Vector3>();
                //leftHandPos = leftControllerPosition.action.ReadValue<Vector3>();
            }
            if (rightControllerGripPress.action.IsPressed())
            {
                rightHandVel = rightControllerVelocity.action.ReadValue<Vector3>();
                //rightHandPos = rightControllerPosition.action.ReadValue<Vector3>();
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
                Vector3 worldVel = _XROrigin.TransformDirection(handVel);    // Transforming the hands velocity from local space to world space
                //Vector3 worldPos = _XROrigin.TransformDirection(handPos);
                //Debug.Log("HandWorldPos: " + worldPos);
                //_rb.AddForceAtPosition(worldVel * swimForce, worldPos, ForceMode.Acceleration); // EXP
                _rb.AddForce(worldVel * swimForce, ForceMode.Acceleration); // Applying force to the diver
                _strokeCooldown = 0f;
            }
        }


    }

}