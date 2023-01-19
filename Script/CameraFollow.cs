using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    private Vector3 offset;
    [SerializeField] private Transform target;
    private float translateSpeed = 10;
    private float rotationSpeed = 10;
    
    private Vector3 camBehind = new Vector3(0,8,-14);
    private Vector3 camBehinindOffset = new Vector3(0,0,0);
    private Vector3 camLeftOffset = new Vector3(0,0,6);
    private Vector3 camLeft = new Vector3(-13,2,0);
    
    private Vector3 camOffset;
    
    private void Start()
    {
        offset = camBehind;
        camOffset = camBehinindOffset;
    }
    private void FixedUpdate()
    {
        SwitchCamera();
        HandleTranslation();
        HandleRotation();
    }

    void HandleTranslation()
    {
        var targetPosition = target.TransformPoint(offset) + camOffset;
        transform.position = Vector3.Lerp(transform.position, targetPosition, translateSpeed * Time.deltaTime);
    }

    void HandleRotation()
    {
        var direction = target.position + camOffset - transform.position;
        var rotation = Quaternion.LookRotation(direction);
        transform.rotation = Quaternion.Lerp(transform.rotation, rotation, rotationSpeed *Time.deltaTime);
    }

    void SwitchCamera()
    {
        if(Input.GetKey(KeyCode.Alpha1))
        {
            offset = camBehind;
            camOffset = camBehinindOffset;
        }
        if(Input.GetKey(KeyCode.Alpha2))
        {
            offset = camLeft;
            camOffset = camLeftOffset;
        }

    }
}
