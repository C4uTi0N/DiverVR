using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Scriptable Objects/Notification")]
public class NotificationSO : ScriptableObject
{
    [SerializeField]
    public string Title;

    [TextArea]
    [SerializeField]
    public string Text;
}
