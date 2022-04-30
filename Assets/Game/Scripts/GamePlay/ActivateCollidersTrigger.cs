using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ActivateCollidersTrigger : Trigger
{

    public List<GameObject> ActivateColliders;
    public List<GameObject> DeactivateColliders;


    protected override void OnEnter()
    {
        ActivateColliders.ForEach(c => c.SetActive(true));
        DeactivateColliders.ForEach(c => c.SetActive(false));
    }

}
