using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.XR.OpenXR.Samples.ControllerSample;


public class ActionToHandAnim : ActionToControl
{
    [Tooltip("Animator on Hand")]
    [SerializeField] private Animator animator;

    [Tooltip("Name of animation paramater (case sensitive!)")]
    public string floatParameter;

    [Range(0f, 20f)]
    public float transitionSpeed = 20;
    private float currentValue;
    private float targetValue;

    private void Update()
    {
        if (currentValue != targetValue)
        {
            currentValue = Mathf.MoveTowards(currentValue, targetValue, transitionSpeed * Time.deltaTime);
            animator.SetFloat(floatParameter, currentValue);
        }
    }

    protected override void OnActionPerformed(InputAction.CallbackContext ctx) => UpdateValue(ctx);
    protected override void OnActionStarted(InputAction.CallbackContext ctx) => UpdateValue(ctx);
    protected override void OnActionCanceled(InputAction.CallbackContext ctx) => UpdateValue(ctx);

    private void UpdateValue(InputAction.CallbackContext ctx)
    {
        targetValue = ctx.ReadValue<float>();

    }
}
