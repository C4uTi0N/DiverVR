using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class QuestionManager : MonoBehaviour
{
    public Transform playerCamera;

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

    public void ShowQuestion(QuestionSO question, UnityAction<bool> callback)
    {
        background = Instantiate(QuestionUiBackground, playerCamera.position, Quaternion.Euler(0, playerCamera.eulerAngles.y, 0));

        background.transform.Find("Panel_Question").transform.Find("Background").transform.Find("Question").GetComponent<TextMeshProUGUI>().text = question.Question;

        float offset = -120f;

        int index = 0;
        foreach (string answers in question.Answers)
        {
            int i = index;

            GameObject q = Instantiate(QuestionUiQuestion, new Vector3(0, offset, 0), Quaternion.identity);
            // get the first child of the canvas prefab which should be the background
            q.transform.SetParent(background.transform.Find("Panel_Question").transform.Find("Background").transform, false);

            q.GetComponentInChildren<TextMeshProUGUI>().text = answers;
            q.GetComponentInChildren<Button>().onClick.AddListener(delegate { OnClick(question, i, callback); });


            offset -= 50f;
            index++;
        }

        //Time.timeScale = 0;
        GameObject.FindGameObjectWithTag("Player").GetComponent<DiverController>().playerPaused = true;

    }


    public void OnClick(QuestionSO question, int answerIndex, UnityAction<bool> callback)
    {
        Destroy(background);
        Time.timeScale = 1;
        callback(question.CheckAnswer(answerIndex));

    }
}