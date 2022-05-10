using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Question", menuName = "Scriptable Objects/Question")]
public class QuestionSO : ScriptableObject
{

    [SerializeField]
    public string Question;

    [SerializeField]
    public List<string> Answers;

    [SerializeField]
    public int CorrectAnswerIndex; 

    public bool CheckAnswer(int answer)
    {
        return CorrectAnswerIndex == answer; 
    }
}
