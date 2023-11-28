using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CrossVectorDisplay : MonoBehaviour
{
    public Transform obj1;
    public Transform obj2;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        Debug.DrawRay(transform.position, Vector3.Cross(obj1.up,obj2.up), Color.red);
    }
}
