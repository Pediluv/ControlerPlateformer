using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraFollow : MonoBehaviour
{

    public GameObject Perso;
    public float timeOffset;
    public Vector3 posOffset;

    private Vector3 velocity;

    void FixedUpdate()
    {
        transform.position = Vector3.SmoothDamp(transform.position, Perso.transform.position + posOffset, ref velocity, timeOffset);
    }
}