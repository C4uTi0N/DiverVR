using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class QuestionManager : MonoBehaviour
{
    [Header("Prefabs")]
    public GameObject QuestionUiBackground;
    public GameObject QuestionUiQuestion;

    [Header("Event Channel")]
    public QuestionEventChannelSO questionEventChannel = default;

    private GameObject background;

    private void OnEnable()
    {
        Debug.Log("QM Enabled");
        questionEventChannel.OnAskQuesiton += ShowQuestion;
    }

    private void OnDisable()
    {
        questionEventChannel.OnAskQuesiton -= ShowQuestion;
    }

    public void ShowQuestion(QuestionScriptableObject question, UnityAction<bool> callback)
    {
        background = Instantiate(QuestionUiBackground, new Vector3(0, 0, 0), Quaternion.identity);

        background.transform.Find("Question").GetComponent<TextMeshProUGUI>().text = question.Question;

        float offset = -50f;

        int index = 0;
        foreach (string answers in question.Answers)
        {
            int i = index;

            GameObject q = Instantiate(QuestionUiQuestion, new Vector3(0, offset, 0), Quaternion.identity);
            q.transform.SetParent(background.transform.Find("Background").transform, false); // get the first child of the canvas prefab which should be the background

            q.GetComponentInChildren<TextMeshProUGUI>().text = answers;
            q.GetComponentInChildren<Button>().onClick.AddListener(delegate { OnClick(question, i, callback); });


            offset -= 30f;
            index++;
        }

        Time.timeScale = 0;

    }


    public void OnClick(QuestionScriptableObject question, int answerIndex, UnityAction<bool> callback)
    {
        Destroy(background);
        Time.timeScale = 1;
        callback(question.CheckAnswer(answerIndex));

    }
}