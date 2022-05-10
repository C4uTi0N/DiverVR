using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class NotificationManager : MonoBehaviour
{
    [Header("Event Channel")]
    public NotificationEventChannelSO NotificationEventChannel;
    public VoidEventChannel DestroyNotificationEventChannel;

    [Header("UI")]
    public GameObject NoficationUI;

    private GameObject _notification;


    private void OnEnable()
    {
        NotificationEventChannel.OnShowNotification += ShowNotification;
        DestroyNotificationEventChannel.OnEventRaised += DestroyNotification;
    }

    private void OnDisable()
    {
        NotificationEventChannel.OnShowNotification -= ShowNotification;
        DestroyNotificationEventChannel.OnEventRaised -= DestroyNotification;
    }

    void ShowNotification(NotificationSO notification, UnityAction callback)
    {
        _notification = Instantiate(NoficationUI, new Vector3(0, 0, 0), Quaternion.identity);

        _notification.transform.GetChild(0).Find("Title").GetComponent<TextMeshProUGUI>().text = notification.Title;
        _notification.transform.GetChild(0).Find("Text").GetComponent<TextMeshProUGUI>().text = notification.Text;

        _notification.transform.GetChild(0).Find("Button").GetComponent<Button>().onClick.AddListener(() => DestroyNotification());

        if(callback != null)
        {
            _notification.transform.GetChild(0).Find("Button").GetComponent<Button>().onClick.AddListener(callback);
        }

        // Time.timeScale = 0;
        GameObject.FindGameObjectWithTag("Player").GetComponent<DiverController>().playerPaused = true;
    }

    void DestroyNotification()
    {
        if (_notification != null)
        {
            Destroy(_notification);
        }

        // Time.timeScale = 1;
        GameObject.FindGameObjectWithTag("Player").GetComponent<DiverController>().playerPaused = false;
    }
}