using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [Header("Velocidades")]
    public float walkSpeed   = 5f;
    public float runSpeed    = 9f;
    public float sneakSpeed  = 2f;

    [Header("Referencia a la camara")]
    public Transform cameraTransform;

    private CharacterController characterController;
    private float currentSpeed;

    void Start()
    {
        characterController = GetComponent<CharacterController>();

        if (cameraTransform == null)
            cameraTransform = Camera.main.transform;
    }

    void Update()
    {
        HandleSpeed();
        HandleMovement();
    }

    void HandleSpeed()
    {
        if (Input.GetKey(KeyCode.LeftShift))
            currentSpeed = runSpeed;
        else if (Input.GetKey(KeyCode.LeftControl))
            currentSpeed = sneakSpeed;
        else
            currentSpeed = walkSpeed;
    }

    void HandleMovement()
    {
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical   = Input.GetAxisRaw("Vertical");

        Vector3 camForward = cameraTransform.forward;
        Vector3 camRight   = cameraTransform.right;

        camForward.y = 0f;
        camRight.y   = 0f;

        camForward.Normalize();
        camRight.Normalize();

        Vector3 moveDirection = (camForward * vertical + camRight * horizontal);

        if (moveDirection.magnitude > 1f)
            moveDirection.Normalize();

        moveDirection.y = -2f;

        characterController.Move(moveDirection * currentSpeed * Time.deltaTime);

        Vector3 lookDirection = new Vector3(moveDirection.x, 0f, moveDirection.z);
        if (lookDirection != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(lookDirection);
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRotation,
                10f * Time.deltaTime
            );
        }
    }
}
