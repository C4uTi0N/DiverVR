using UnityEngine;


[RequireComponent(typeof(BoxCollider))]
public abstract class Trigger : MonoBehaviour
{
    [Header("Trigger")]
    public bool Repeatable;
    public string ColliderTag = "Player";

    protected bool _triggeredOnEnter = false;
    protected bool _triggeredOnExit = false;

    protected virtual void OnEnter() { }
    protected virtual void OnExit() { }

    protected virtual void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(ColliderTag) && (Repeatable || !_triggeredOnEnter))
        {
            OnEnter();
            _triggeredOnEnter = true;
        }
    }

    protected virtual void OnTriggerExit(Collider other)
    {
        if (other.CompareTag(ColliderTag) && (Repeatable || !_triggeredOnExit))
        {
            OnExit();
            _triggeredOnExit = true;
        }
    }


    protected void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        BoxCollider boxCollider = GetComponent<BoxCollider>();
        Gizmos.DrawWireCube(boxCollider.transform.position, boxCollider.size);
    }
}