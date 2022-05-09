using UnityEngine;
using UnityEngine.Events;

[CreateAssetMenu(menuName = "Scriptable Objects/Event Channels/Notification Event Channel")]
public class NotificationEventChannelSO : ScriptableObject
{

    public UnityAction<NotificationSO, UnityAction> OnShowNotification;

    public void RaiseEvent(NotificationSO notification, UnityAction callback = null)
    {
        if (OnShowNotification != null)
        {
            OnShowNotification.Invoke(notification, callback);
        }
    }
}
