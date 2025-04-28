using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerManager : MonoBehaviour
{
    public float strafeVelocity;
    public Vector2 mouseSensitivity;
    public float lookDistance;
    public float minLookAltDeg;
    public float maxLookAltDeg;

    private GameObject playerAvatar;
    private GameObject playerCamera;
    private InputAction moveAction;
    private InputAction lookAction;

    private Vector2 inputLookAngleOffset = new Vector2(0, 0);
    private Vector2 lookAngleOffset = new Vector2(0, 0);

    void Start()
    {
        playerAvatar = GameObject.FindGameObjectWithTag("PlayerAvatar");
        playerCamera = GameObject.FindGameObjectWithTag("PlayerCamera");

        moveAction = InputSystem.actions.FindAction("Move");
        lookAction = InputSystem.actions.FindAction("Look");
    }

    void FixedUpdate()
    {
        UpdatePlayerPosition();
        UpdateCameraXformWithPlayerPosition();
        UpdatePlayerHeadingWithCameraXform();
    }

    void UpdatePlayerPosition()
    {
        var move = moveAction.ReadValue<Vector2>() * Time.fixedDeltaTime * strafeVelocity;
        playerAvatar.transform.position += move.x * playerAvatar.transform.right + move.y * playerAvatar.transform.forward;
    }

    void UpdateCameraXformWithPlayerPosition()
    {
        var look = lookAction.ReadValue<Vector2>() * Time.fixedDeltaTime * mouseSensitivity;
        inputLookAngleOffset += look;
        inputLookAngleOffset.y = Mathf.Clamp(inputLookAngleOffset.y, minLookAltDeg, maxLookAltDeg);

        lookAngleOffset = new Vector2(
            Mathf.LerpAngle(lookAngleOffset.x, inputLookAngleOffset.x, 0.1f),
            Mathf.LerpAngle(lookAngleOffset.y, inputLookAngleOffset.y, 0.1f)
        );

        var cameraPosRaycastDirection = Quaternion.Euler(new Vector3(-lookAngleOffset.y, lookAngleOffset.x, 0)) * (-Vector3.forward);

        var cameraLookatTarget = playerAvatar.transform.position + new Vector3(0, 1.5f, 0);

        if (Physics.Raycast(cameraLookatTarget, cameraPosRaycastDirection, out RaycastHit hit, lookDistance, ~LayerMask.GetMask("Ignore Raycast")))
        {
            // Obstruction: move camera closer
            playerCamera.transform.position = (hit.distance * 0.9f) * cameraPosRaycastDirection + playerAvatar.transform.position;
        }
        else
        {
            playerCamera.transform.position = cameraPosRaycastDirection * lookDistance + cameraLookatTarget;
        }
        playerCamera.transform.LookAt(cameraLookatTarget);
    }

    void UpdatePlayerHeadingWithCameraXform()
    {
        var cameraForward = playerCamera.transform.forward;
        cameraForward.y = 0; // Ignore vertical component
        cameraForward.Normalize();
        playerAvatar.transform.forward = cameraForward;
    }
}
