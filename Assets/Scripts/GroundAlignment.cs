using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GroundAlignment : MonoBehaviour
{
    [SerializeField] private RaycastHit2D groundCheck;
    [SerializeField] private float raySize;
    [SerializeField] private Vector2 rayNorm;
    [SerializeField] private LayerMask layerMask;

    void Update()
    {
        groundCheck = Physics2D.Raycast(transform.position, -transform.up, raySize, layerMask);
        rayNorm = groundCheck.normal;

        transform.up = rayNorm;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(transform.position, groundCheck.point);
    }
}
