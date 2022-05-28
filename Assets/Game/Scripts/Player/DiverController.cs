using System;
using UnityEngine;
using TMPro;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;


[RequireComponent(typeof(Rigidbody))]
public class DiverController : MonoBehaviour
{
    public bool playerPaused = false;

    public DiveSettings diveSettings;

    private const float g = 9.82f;         // Gravitational constant
    private float _waterSurface;

    [Header("Swimming Variables")]
    public float swimForce = 1.5f;         // Force of strokes
    public float minForce = 0f;            // Min force for a stroke to be registered (default 0.25)
    public float minStrokeInterval = 0f;   // Min time between strokes (default 0.25)
    public float _strokeCooldown;          // Timer which is reset to 0 at each stroke
    public float submergence = 0;          // How much of the diver is submerged (a bit fictive as of now, it's relative to the VR-headset position)
    public float ascentRate;               // Current ascent rate of player


    [Header("BCD Variables")]
    public float BCD_Volume = 0f;          // l, Last calculated volume of the BCD.
    public float BCD_Surf_Vol = 0;         // l, Volume BCD will assume if brought to surface.     
    public float BCDVolStep = 0.030f;      // l, Air added to BCD per step (fixed update).

    #region Private Variables
    // Breath
    int breathsPerMin = 6;
    float breathCooldown = 60f;             // 60s / x breaths per min.
    float breathTimer = 0;

    // Gas & descending, ascending
    float gasStartMass;
    double gasRemainingMass;
    float safetyEndPress = 35f;             // bar, Minimum pressure to surface with (equivalent to 500 PSI).
    float ascentMaxMtrPerMin = 10f;         // m, Maximum meters/min when ascending.
    [HideInInspector]
    public float buoyancy = 0f;
    // BCD dump
    bool BCD_DumpInProgress = false;
    float BCD_DumpStep = 0;
    int BCD_DumpCounter = 0;

    // User data variables
    bool diveGoingOn = false;               // Has the dive begun?
    float userDataCooldown = 0.25f;         // s, Update frequecy of user data.
    float userDataUpdTimer = 0;

    // Diver values
    float previousDepth;                    // depth one second ago
    float time;                             // hh:mm:ss, current local time
    float diveTime;                         // s, Time elapsed since the dive began.
    [HideInInspector]
    public float depth;                            // m, current diver depth
    int maxDepth;                           // m, max depth reached
    [HideInInspector]
    public double tankPress;                        // bar, current pressure in tank
    float timeAtDepth;                      // s, The time left at current depth, (with time to surface subtracted).
    Vector3 diverOldVelocity;               // Velocity of diver, before the dive was paused
    #endregion                              // Note: the above is not NDL (No Decompression Limit)

    //
    public string timeStr;
    public string diveTimeStr = "00h00m";
    public string depthStr;
    public string tankPressStr;
    public string timeAtDepthStr = "00h00m";
    public string maxDepthStr;



    [Header("Object References")]
    public Transform _waterBody;                        // Gameobject holding our water
    [SerializeField] private Transform _XROrigin;       // Root Object of Player- or XR-Rig
    [SerializeField] private Transform _VRCamera;       // player Camera
    [SerializeField] private Transform _leftControllerTransform;
    private GameObject _locomotionSystemGO;
    private ActionBasedContinuousMoveProvider _actionBasedContinuousMoveProvider;
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
    [SerializeField] InputActionReference rightControllerThumbStickButton;
    #endregion


    private void Awake()
    {
        _locomotionSystemGO = GameObject.FindGameObjectWithTag("Locomotion System");
        _actionBasedContinuousMoveProvider = _locomotionSystemGO.GetComponent<ActionBasedContinuousMoveProvider>();
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
        if (playerPaused)
        {
            if (_actionBasedContinuousMoveProvider.enabled == true)
            {
                _actionBasedContinuousMoveProvider.enabled = false;
                diverOldVelocity = _rb.velocity;
                _rb.velocity = Vector3.zero;
            }
        }
        else
        {
            if (_actionBasedContinuousMoveProvider.enabled == false)
            {
                _actionBasedContinuousMoveProvider.enabled = true;
                _rb.velocity = diverOldVelocity;
            }

            buoyancy = ApplyGravity(_rb);           // Always apply gravity.
            ascentRate = _rb.velocity.y;

            // Submergence controls the beginning of the dive.
            submergence = PlayerSubmergence(_VRCamera.gameObject);    // Degree of submergence.

            // Here the dive is started and stopped.
            if (submergence > 0)
            {
                diveGoingOn = true;
                if (_actionBasedContinuousMoveProvider.enableStrafe == true)
                    _actionBasedContinuousMoveProvider.enableStrafe = false;
            }
            else if (diveGoingOn)
            {
                diveGoingOn = false;
                _actionBasedContinuousMoveProvider.enableStrafe = true;
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
                if (BCD_DumpInProgress)
                {
                    if (BCD_DumpCounter < 50)
                    {
                        BCD_Surf_Vol -= BCD_DumpStep;
                        BCD_DumpCounter++;
                    }
                    else
                    {
                        BCD_DumpInProgress = false;
                    }
                }
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
                double gasEndMass = VanDerWall_Mass(BarToPascal(safetyEndPress),
                                                    CelciusToKelvin(diveSettings.waterTemp),
                                                    diveSettings.tankCapacity / 1000d);
                double gasToUseMass = gasRemainingMass - gasEndMass;
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
    }


    // Setting values for a new dive.
    public void InitDive()
    {
        breathTimer = 0;
        diveTime = 0;
        BCD_Volume = 0f;           // Emptying the BCD
        BCD_Surf_Vol = 0;          // Emptying the BCD
        RefillTank();
    }

    // Initializing the tank to start pressure.
    public void RefillTank()
    {
        tankPress = diveSettings.tankStartPress;     // Refilling the tank
        double gasStartMass = VanDerWall_Mass(BarToPascal(tankPress),
                                               CelciusToKelvin(diveSettings.waterTemp),
                                               diveSettings.tankCapacity / 1000d
                                             );
        gasRemainingMass = gasStartMass;
    }

    void CalculateAscentRate(float currentDepth)
    {
        float metersPerSec = (previousDepth - currentDepth) * (1f /userDataCooldown);   // meters per sec.
        ascentRate = metersPerSec;
        if (ascentRate < 0) ascentRate = 0;
        previousDepth = currentDepth;
    }

    // Entry point for updating UI (panel on diving watch, etc.)
    // To be adjusted with data as needed.
    void UpdateUserData()
    {
        // Time
        timeStr = DateTime.Now.ToString("HH:mm");
        if (localTimeValue != null) { localTimeValue.text = timeStr; }


        // Dive time (m�ske bare mm)
        TimeSpan ts1 = TimeSpan.FromSeconds(diveTime);
        diveTimeStr = ts1.ToString(@"mm");
        if (diveDurationValue != null) { diveDurationValue.text = diveTimeStr; }

        // Depth
        depthStr = Depth(_VRCamera.position.y).ToString("00.0");
        if (depthValue != null) { depthValue.text = depthStr; }

        // Tank pressure
        tankPressStr = tankPress.ToString("0.0") + " bar";
        if (tankPressureValue != null) { tankPressureValue.text = tankPressStr; }

        // Time left at depth (m�ske mmm:ss)
        int mins = (int)(timeAtDepth / 60);
        int secs = (int)timeAtDepth % 60;
        timeAtDepthStr = mins.ToString();
        if (timeAtDepthValue != null) { timeAtDepthValue.text = timeAtDepthStr; }

        if (netBuoyancyValue != null)
            netBuoyancyValue.text = (buoyancy / g).ToString("0.00") + " kg";

        if (BCD_AirVolValue != null)
            BCD_AirVolValue.text = BCD_Volume.ToString("0.00") + " ltr";

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


    // Updating the tank pressure for one breath.
    void TakeBreath()
    {
        float pascalPress = PressureAtDepth(_VRCamera.position.y);   // pascal, Pressure at current depth.
        double breathGasMass = VanDerWall_Mass(pascalPress,
                                                CelciusToKelvin(diveSettings.waterTemp),
                                                (diveSettings.RMV / breathsPerMin / 1000d)
                                              );
        // Book keeping
        if (gasRemainingMass > breathGasMass) gasRemainingMass -= breathGasMass;
        tankPress = VanDerWall_Press(gasRemainingMass,
                                     CelciusToKelvin(diveSettings.waterTemp),
                                     diveSettings.tankCapacity / 1000d
                                    );
        /*
        Debug.Log("BreathMass:" + breathGasMass);
        Debug.Log("Updated tank pressure:" + tankPress);
        */
    }


    // Returning mass of gas used during ascent.
    float AscentGasMass()
    {
        depth = Depth(_VRCamera.position.y);
        float avgSurfacingDepth = depth / 2f;                                               // m, The average depth during ascent.
        float swimMinsToSurface = depth / ascentMaxMtrPerMin;                               // minutes, Minimum time to surface, excluding safety stop.
        float breathCount = swimMinsToSurface * breathsPerMin;                              // Number of breaths to surface, excluding safety stop.
        float avgPascals = PressureAtDepth(avgSurfacingDepth);                              // pascal, Pressure at average surfacing depth.
        double totalBreathVol = (diveSettings.RMV / breathsPerMin) * breathCount / 1000d;   // m3, Total volume of air used during ascent.
        double ascentGasMass = 0;
        if (totalBreathVol > 0)
            ascentGasMass = VanDerWall_Mass(avgPascals,                                    // kg, Total mass of ascent air.
                                                CelciusToKelvin(diveSettings.waterTemp),
                                                totalBreathVol
                                            );
        /*
        Debug.Log("-------------");   
        Debug.Log("Avg surf depth: " + avgSurfacingDepth);
        Debug.Log("Mins to surface: " + swimMinsToSurface);
        Debug.Log("Number of breaths to surface: " + breathCount);
        Debug.Log("Avg. pressure during ascent: " + avgPascals);
        Debug.Log("One breath avg. vol: " + totalBreathVol);
        Debug.Log("Ascent gas mass: " + ascentGasMass);
        Debug.Log("-------------");
        */
        return (float)ascentGasMass;
    }


    // Returns mass of gas used during a stop.
    float StopGasMass(float stopDuration, float depth)
    {
        double pascals = PressureAtDepth(_waterSurface - depth);
        float breathCount = stopDuration * breathsPerMin;
        double totalBreathVol = (diveSettings.RMV / breathsPerMin) * breathCount / 1000d;
        double stopGasMass = VanDerWall_Mass(pascals,                             // kg, Total mass of stop air.
                                              CelciusToKelvin(diveSettings.waterTemp),
                                              totalBreathVol
                                             );
        return (float)stopGasMass;
    }


    // Time left at current depth.
    float TimeAtDepth(double massOfGasToUse)
    {
        double pascals = PressureAtDepth(_VRCamera.position.y);          // pascal, Pressure at current depth.
        double oneBreathVol = (diveSettings.RMV / breathsPerMin / 1000d);
        double oneBreathGasMass = VanDerWall_Mass(pascals,              // kg, mass of air used.
                                                   CelciusToKelvin(diveSettings.waterTemp),
                                                   oneBreathVol
                                                 );
        float breathCount = (float)(massOfGasToUse / oneBreathGasMass);  // Number of breaths until diver has to begin ascent.
        float oneBreathTime = 60f / breathsPerMin;                       // s, duration of 1 breath.
        timeAtDepth = breathCount * oneBreathTime;
        /*
        Debug.Log("------------------------------------------");
        Debug.Log("gasRemainingMass:" + gasRemainingMass);
        Debug.Log("Mass of gas to use: " + massOfGasToUse);
        Debug.Log("Volume of one breath: " + oneBreathVol);
        Debug.Log("Mass of one breath: " + oneBreathGasMass);
        Debug.Log("TimeATDepth: " + timeAtDepth);
        Debug.Log("------------------------------------------");
        */
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


    // Net. waterlift of divers Buoyance Control Device (BCD).
    float BCD_Buoyancy(float submergence)
    {
        if (!BCD_DumpInProgress && rightControllerThumbStickButton.action.IsPressed())
        {
            BCD_DumpInProgress = true;
            BCD_DumpStep = BCD_Surf_Vol / 50f;
            BCD_DumpCounter = 0;
        }

        double pascals = PressureAtDepth(_VRCamera.position.y);
        float BCD_StepSurfVol = BoyleNewVol(BCDVolStep, (float)pascals, diveSettings.atmosphericPressure * 100);
        if (!BCD_DumpInProgress)
        {
            if (BCD_Volume <= diveSettings.BCD_Capacity - BCDVolStep && rightControllerSecondaryButton.action.IsPressed())
            {
                BCD_Surf_Vol += BCD_StepSurfVol * 1.5f;
                double deltaMass = VanDerWall_Mass(pascals,
                                        CelciusToKelvin(diveSettings.waterTemp),
                                        BCDVolStep / 1000d
                                      );
                if (gasRemainingMass > deltaMass) gasRemainingMass -= deltaMass;
            }
            if (BCD_Volume >= BCDVolStep && rightControllerPrimaryButton.action.IsPressed())
            {
                BCD_Surf_Vol -= BCD_StepSurfVol * 2.5f;
            }
        }

        // Checking if BCD have expanded beyond capacity. In that case releasing surplus.
        if (BoyleVolAtDepth(_VRCamera.gameObject, BCD_Surf_Vol) > diveSettings.BCD_Capacity)
        {
            float toRelease = BoyleVolAtDepth(_VRCamera.gameObject, BCD_Surf_Vol) - diveSettings.BCD_Capacity;
            BCD_Surf_Vol -= BoyleNewVol(toRelease, (float)pascals, diveSettings.atmosphericPressure * 100);
        }
        // Updating the BCDs volume (depending on depth).
        BCD_Volume = BoyleVolAtDepth(_VRCamera.gameObject, BCD_Surf_Vol);

        float buoyancy = ((diveSettings.waterDensity / 1000f) * BCD_Volume - diveSettings.BCD_weight) * g * submergence;
        Vector3 buoyancyForce = new Vector3(0f, buoyancy, 0f);
        _rb.AddForce(buoyancyForce, ForceMode.Force);
        return buoyancy;
    }


    // Net. water lift of divers tank.
    float TankBuoyancy(float submergence)
    {

        float tankWaterDisp = TankWaterDisp(diveSettings.tankCapacity);             // Volume of displaced water.
        float buoyancy = (float)((diveSettings.waterDensity / 1000f) * tankWaterDisp -   // Mass of displaced water.
                           (diveSettings.tankEmptyWeight + gasRemainingMass)        // Mass of tank and its remaining gas.
                         ) * g * submergence;                                       // = Buoyancy in Newton.

        Vector3 buoyancyForce = new Vector3(0f, buoyancy, 0f);                      // Force vector.
        _rb.AddForce(buoyancyForce, ForceMode.Force);                               // Applying force.
        return buoyancy;
    }


    // Net. water lift of lead.
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
        // m2, cross-sectional area of moving obj, orthogonal to direction of motion.
        const float area = 0.25f;
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
    float CelciusToKelvin(float celcius)
    {
        return 273.15f + celcius;
    }

    double BarToPascal(double bar)
    {
        return bar * 100000f;
    }

    double PascalToBar(double pascal)
    {
        return pascal / 100000f;
    }


    // Using the Van Der Wall equation.
    double VanDerWall_Mass(double P, double T, double V)
    {
        // M, A, B for atmospheric air.
        const double M = 0.0289647d;   //  kg/mol,     Molar mass.         
        const double A = 0.1358d;      //  J/(mol*K),  Attraction between particles.
        const double B = 0.0000364d;   //  m3/mol,     Volume excluded by a mole of particles.
        const double R = 8.3144621d;   //              Gas constant.    

        // When the 'Van Der Wall' equation is put in the form of a 3rd deg. polynomium,
        // with n (number of moles) as the unknown,
        // then a, b ,c, d signifies the coefficients of the polynomium.
        double a = -(A * B) / Math.Pow(V, 2d);
        double b = A / V;
        double c = -(B * P + R * T);
        double d = P * V;

        // USING THE QUBIC FORMULA:
        // -----------------------------------------------------------------------------------
        // Simplifying by creating three expressions which is repetivive in the qubic formula.
        double exp1 = (-Math.Pow(b, 3d) / (27d * Math.Pow(a, 3d)) +
                        (b * c) / (6d * Math.Pow(a, 2d)) -
                        d / (2d * a)
                      );
        double exp2 = (c / (3d * a) -
                       Math.Pow(b, 2d) / (9d * Math.Pow(a, 2d))
                      );
        double exp3 = b / (3d * a);

        // Deriving the root (n = number of moles) of the polynomium, using the qubic formula.
        double n = CubicRoot(exp1 +
                             Math.Sqrt(Math.Pow(exp1, 2d) +
                                        Math.Pow(exp2, 3d)
                                      )
                            ) +

                   CubicRoot(exp1 -
                             Math.Sqrt(Math.Pow(exp1, 2d) +
                                        Math.Pow(exp2, 3d)
                                      )
                            ) -
                   exp3;
        // -----------------------------------------------------------------------------------

        // Now we can calculate the gas mass.
        double gasMass = n * M;
        return gasMass;
    }


    double VanDerWall_Press(double m, double T, double V)
    {
        // M, A, B for atmospheric air.
        const double M = 0.0289647d;   //  kg/mol,     Molar mass.         
        const double A = 0.1358d;      //  J/(mol*K),  Attraction between particles.
        const double B = 0.0000364d;   //  m3/mol,     Volume excluded by a mole of particles.
        const double R = 8.3144621d;   //              Gas constant.    
        double n = m / M;
        double gasPress = (n * R * T) / (V - n * B) - A * Math.Pow(n, 2d) / Math.Pow(V, 2d);
        return PascalToBar(gasPress);
    }


    // c# does not support cubic root of neative numbers, this funtion does.
    private double CubicRoot(double x)
    {
        if (x < 0)
            return -Math.Pow(-x, 1d / 3d);
        else
            return Math.Pow(x, 1d / 3d);
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
    float BoyleNewVol(float oldVol, float oldPress, float newPress)
    {
        return oldVol * (oldPress / newPress);    // A new volume
    }


    // Volume compared to surface.
    float BoyleVolAtDepth(GameObject GO, float surfaceVol)
    {
        float yPos = GO.transform.position.y;
        return surfaceVol * (100 * diveSettings.atmosphericPressure) / PressureAtDepth(yPos);   // A new volume
    }


    // Returns water depth of the game object
    float Depth(float yPos)
    {
        if (yPos >= _waterSurface) return 0;         // We don't want a depth above the water surface.
        return Mathf.Abs(_waterSurface - yPos);      // m, positive number.
    }


    // Pressure of water pillar.
    float PressureAtDepth(float yPos)
    {
        float pressure = Depth(yPos) * diveSettings.waterDensity * g;    // In pascal.
        pressure /= 100f;                                                // In hPa (same as mBar).
        pressure += diveSettings.atmosphericPressure;                    // In hPa (same as mBar).
        pressure *= 100f;                                                // In pascal.       
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
            _rb.AddForce(worldVel * swimForce * 0.5f, ForceMode.Acceleration);
        }
        if (leftControllerPrimaryButton.action.IsPressed())
        {
            Vector3 worldVel = _XROrigin.TransformDirection(-1 * Vector3.up);
            _rb.AddForce(worldVel * swimForce * 0.5f, ForceMode.Acceleration);
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

            // If either grip button is pressed the controllers velocity is applied
            if (leftControllerGripPress.action.IsPressed())
            {
                leftHandVel = leftControllerVelocity.action.ReadValue<Vector3>();
            }
            if (rightControllerGripPress.action.IsPressed())
            {
                rightHandVel = rightControllerVelocity.action.ReadValue<Vector3>();
            }

            Vector3 handVel = leftHandVel + rightHandVel;   //  Combined velocity vector of both hands
            handVel *= -1;  // * -1 because we are swimming in the opposite direction of the hand movement

            // Comparing velocity magnitude (squared (less processing)) with minimum force
            if (handVel.sqrMagnitude > minForce * minForce)
            {
                // Transforming the hands velocity from local space to world space
                Vector3 worldVel = _XROrigin.TransformDirection(handVel);
                // Applying force to the diver
                _rb.AddForce(worldVel * swimForce, ForceMode.Acceleration);
                _strokeCooldown = 0f;
            }
        }
    }

}