using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class WatchUI : MonoBehaviour
{
    [Header("Buttons")]
    public Button btnMODE;
    public Button btnSELECT;
    public Button btnUP;
    public Button btnDOWN;


    [Header("UI Gameobjects")]
    public GameObject waterContact;
    public GameObject temperature;
    public GameObject diveDuration;
    public GameObject clock;
    public GameObject maxDepth;


    [Header("UI References")]
    public TextMeshProUGUI currentDepthText;
    public TextMeshProUGUI temperatureText;
    public TextMeshProUGUI diveDurationText;
    public TextMeshProUGUI clockText;
    public TextMeshProUGUI maxDepthText;
    public TextMeshProUGUI timeAtDepthText;
    public Slider ascentRateIndicatorSlider;

    public DiverController diverController;
    public DiveSettings diveSettings;

    float maxAscentRate = 1.667f;           // max ascent rate of 10 meters pr minute
    float timeLimit = 5;
    float timer = 0;

    private void Start()
    {
        btnSetup();
    }


    private void Update()
    {
        //AscentRateViolation();
    }


    void FixedUpdate()
    {
        //UpdateUI();
        //ToggleWaterContact();
    }
    
    private void UpdateUI()
    {
        currentDepthText.text = diverController.depthStr;
        timeAtDepthText.text = diverController.timeAtDepthStr;
        ascentRateIndicatorSlider.value = diverController.ascentRate;

        if (temperature.activeSelf)
            temperatureText.text = diveSettings.waterTemp.ToString();

        if (diveDuration.activeSelf) 
            diveDurationText.text = diverController.diveTimeStr;

        if (clock.activeSelf)
            clockText.text = diverController.timeStr;

        if (maxDepth.activeSelf)
            maxDepthText.text = diverController.maxDepthStr;
    }

    void btnSetup()
    {
        btnUP.onClick.AddListener(() => ToggleUP());
        btnDOWN.onClick.AddListener(() => ToggleDOWN());
    }

    void ToggleDOWN()
    {
        if (clock.activeSelf) 
        { 
            clock.SetActive(false);
            maxDepth.SetActive(true);
        }
        else if (maxDepth.activeSelf)
        {
            maxDepth.SetActive(false);
            clock.SetActive(true);
        }
    }

    void ToggleUP()
    {
        if (diveDuration.activeSelf)
        {
            diveDuration.SetActive(false);
            temperature.SetActive(true);
        } else if (temperature.activeSelf)
        {
            temperature.SetActive(false);
            diveDuration.SetActive(true);
        }
    }

    void ToggleWaterContact()
    {
        if (transform.position.y < diverController._waterBody.position.y) { waterContact.SetActive(true); }
        else waterContact.SetActive(false);
    }

    void AscentRateViolation()
    {
        if (diverController.ascentRate > maxAscentRate)
        {
            timer += Time.deltaTime;
            if (timer >= timeLimit)
            {
                // Mandatory stop
                Debug.Log("Ascent rate violation!... Mandatory stop need");
            }
        }
        else { timer = 0; }
    }
}
