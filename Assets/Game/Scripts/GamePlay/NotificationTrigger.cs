using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NotificationTrigger : Trigger
{

    [Header("Event Channel")]
    public NotificationEventChannelSO NotificationEventChannel;
    public VoidEventChannel DestroyNotificationEventChannel;

    [Header("Notification")]
    public NotificationSO Notification;


    protected override void OnEnter()
    {
        NotificationEventChannel.RaiseEvent(Notification);
    }
}