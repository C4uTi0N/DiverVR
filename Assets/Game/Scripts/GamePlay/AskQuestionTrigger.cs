using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
[RequireComponent(typeof(BoxCollider))]
public class AskQuestionTrigger : Trigger
{
    [Header("Question SO")]
    public QuestionSO Question;

    [Header("Event Channel")]
    public QuestionEventChannelSO askQuestionEventChannel;

    [Header("Event Channel")]
    public NotificationEventChannelSO notificationEventChannel;

    [Header("Correct Answer")]
    public NotificationSO correctAnswer;
    public NotificationSO wrongAnswer;


    protected override void OnEnter()
    {
        askQuestionEventChannel.RaiseEvent(Question, CorrectAnswer);
    }

    public void CorrectAnswer(bool answer)
    {
        if (answer)
        {
            notificationEventChannel.RaiseEvent(correctAnswer);
            return;
        }

        notificationEventChannel.RaiseEvent(wrongAnswer, OnEnter);

    }

}
