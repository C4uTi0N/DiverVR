using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(BoxCollider))]

public class NotificationZone : Trigger
{

    [Header("Event Channel")]
    public NotificationEventChannelSO ShowNotificationEventChannel;
    public VoidEventChannel DestroyNotificationEventChannel;

    [Header("Notification")]
    public NotificationSO Notification;

    protected override void OnEnter()
    {
        ShowNotificationEventChannel.RaiseEvent(Notification);
    }

    protected override void OnExit()
    {
        DestroyNotificationEventChannel.RaiseEvent();
    }
}