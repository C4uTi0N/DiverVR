using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

[RequireComponent(typeof(AudioSource))]
public class WatchUI : MonoBehaviour
{
    AudioSource alarm;

    [Header("Buttons")]
    public Button mODEButton;
    public Button sELECTButton;
    public Button uPButton;
    public Button dOWNButton;


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

    float maxAscentRate;
    float timer = 0;
    public float stopTime = 60;
    public float timeLimit = 5;
    bool alarmPlayed = false;


    private void Start()
    {
        alarm = GetComponent<AudioSource>();
        btnSetup();

        maxAscentRate = diveSettings.maxAscentRate / 60;
        ascentRateIndicatorSlider.maxValue = maxAscentRate;
        print("Max Acent Rate is: " + maxAscentRate);

    }


    private void Update()
    {
        AscentRateViolation();
    }


    void FixedUpdate()
    {
        UpdateUI();
        ToggleWaterContact();
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
        uPButton.onClick.AddListener(() => ToggleUP());
        dOWNButton.onClick.AddListener(() => ToggleDOWN());
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
        }
        else if (temperature.activeSelf)
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
            //print("Ascent Rate: " + diverController.ascentRate + " | Timer: " + timer);
            if (!alarmPlayed && timer >= timeLimit)
            {
                alarm.Play();
                alarmPlayed = true;
                print("Ascent rate violation!... Mandatory stop need");
                timer = 0;
            }
        }
        else if (diverController.ascentRate > 0.001)
        {
            timer = 0;
        }
        
        if (alarmPlayed)
        {
            if (diverController.ascentRate <= 0.01)
            {
                timer += Time.deltaTime;
                if (timer >= stopTime)
                {
                    alarmPlayed = false;
                    print("Mandatory stop completed");
                    timer = 0;
                }
            }
            if (diverController.ascentRate > 0.005)
            {
                if (!alarm.isPlaying) { alarm.PlayDelayed(0.15f); }
            }
        }
    }
}