using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Buoyancy : MonoBehaviour
{
    Rigidbody rb;
    [Header("Buoyancy settings")]
    [Range(0.01f, 3f)]
    public float bcd = 0.01f;
    public float depthBeforeSubmerged = 0.1f;
    private float liquidDensity = 0.99f;

    [Header("Calculated variables")]
    [SerializeField] private float finalBCD;
    [SerializeField] private float pressure;
    [SerializeField] private float depth;


    void Start()
    {
        rb = GetComponent<Rigidbody>();
    }


    void FixedUpdate()
    {
        depth = transform.position.y;
        
        if (depth < 0)
        {
            if (Input.GetKey(KeyCode.LeftShift))
            {
                if (bcd > 3)
                {
                    bcd = 3;
                    return;
                }
                bcd -= (0.001f / transform.position.y);
            }
            if (Input.GetKey(KeyCode.LeftControl))
            {
                if(bcd < 0.01f)
                {
                    bcd = 0.01f;
                    return;
                }
                bcd += (0.005f / transform.position.y);
            }
        }
        
        CalculatePressure();

        finalBCD = pressure * bcd;
        float bcdMultiplaier = Mathf.Clamp01(-transform.position.y / depthBeforeSubmerged) * finalBCD;
        rb.AddForce(new Vector3(0f, Mathf.Abs(Physics.gravity.y) * bcdMultiplaier, 0f), ForceMode.Acceleration);
    }

    void CalculatePressure()
    {
        pressure = Mathf.Abs(depth * liquidDensity);
    }
}
