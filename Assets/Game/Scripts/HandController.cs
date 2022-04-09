using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.InputSystem;


    [RequireComponent(typeof(ActionBasedController))]
    public class HandController : MonoBehaviour
    {
        ActionBasedController controller;
        Hand hand;

        // Start is called before the first frame update
        void Start()
        {
            controller = GetComponent<ActionBasedController>();
            hand = GetComponentInChildren<Hand>();
        }

        // Update is called once per frame
        void Update()
        {
            hand.SetTrigger(controller.selectAction.action.ReadValue<float>());
        }
    protected void OnActionPerformed(InputAction.CallbackContext ctx) => UpdateValue(ctx);

    private void UpdateValue(InputAction.CallbackContext ctx) => hand.SetGrip(ctx.ReadValue<float>());

}
