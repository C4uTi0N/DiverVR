using UnityEngine;

[RequireComponent(typeof(Animator))]
public class Hand : MonoBehaviour
{
    private Animator animator;
    [SerializeField] private float gripTarget;
    [SerializeField] private float gripCurrent;
    [SerializeField] private float triggerTarget;
    [SerializeField] private float triggerCurrent;
    private string animatorGripParam = "Grip";
    private string animatorTriggerParam = "Trigger";

    [Range(0f, 100f)]
    public float transitionSpeed = 10;


    void Start()
    {
        animator = GetComponent<Animator>();
    }


    void Update()
    {
        AnimateHand();
    }

    public void SetGrip(float value)
    {
        gripTarget = value;
    }

    public void SetTrigger(float value)
    {
        triggerTarget = value;
    }

    void AnimateHand()
    {

        if (gripCurrent != gripTarget)
        {
            gripCurrent = Mathf.MoveTowards(gripCurrent, triggerTarget, transitionSpeed * Time.deltaTime);
            animator.SetFloat(animatorGripParam, gripCurrent);
        }
        
        if (triggerCurrent != triggerTarget)
        {
            triggerCurrent = Mathf.MoveTowards(triggerCurrent, triggerTarget, transitionSpeed * Time.deltaTime);
            animator.SetFloat(animatorTriggerParam, triggerCurrent);
        }

    }
}
