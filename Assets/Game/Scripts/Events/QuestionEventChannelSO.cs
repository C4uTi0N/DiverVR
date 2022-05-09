using UnityEngine;
using UnityEngine.Events;

[CreateAssetMenu(menuName = "Scriptable Objects/Event Channels/Question Event Channel")]
public class QuestionEventChannelSO : ScriptableObject
{

    public UnityAction<QuestionScriptableObject, UnityAction<bool>> OnAskQuesiton;

    public void RaiseEvent(QuestionScriptableObject question, UnityAction<bool> callback)
    {
        if(OnAskQuesiton != null)
        {
            OnAskQuesiton.Invoke(question, callback);
        }
    }
 }
