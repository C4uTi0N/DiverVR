using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

[CreateAssetMenu(menuName = "Scriptable Objects/Event Channels/Void Event Channel")]

public class VoidEventChannel : ScriptableObject
{
   
    public UnityAction OnEventRaised;

    public void RaiseEvent()
    {
        if (OnEventRaised != null)
        {
            OnEventRaised.Invoke();
        }
    }
}
