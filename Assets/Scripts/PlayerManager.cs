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
    public GameObject[] playerProjectileSpawnPoint;
    public GameObject playerProjectilePrefab;
    public float projectileForce = 10f;
    public float projectileCooldownSec = 0.5f;

    private GameObject playerAvatar;
    private GameObject playerCamera;
    private int numProjectilesFiredByPlayer = 0;
    private InputAction moveAction;
    private InputAction lookAction;
    private InputAction fireAction;
    private float accumulatedProjectileCooldownTime = 0f;

    private Vector2 inputLookAngleOffset = new Vector2(0, 0);
    private Vector2 lookAngleOffset = new Vector2(0, 0);

    void Start()
    {
        playerAvatar = GameObject.FindGameObjectWithTag("PlayerAvatar");
        playerCamera = GameObject.FindGameObjectWithTag("PlayerCamera");

        moveAction = InputSystem.actions.FindAction("Move");
        lookAction = InputSystem.actions.FindAction("Look");
        fireAction = InputSystem.actions.FindAction("Attack");
    }

    void FixedUpdate()
    {
        UpdatePlayerPosition();
        UpdateCameraXformWithPlayerPosition();
        UpdatePlayerHeadingWithCameraXform();
        HandleFireInput();
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

    void HandleFireInput()
    {
        accumulatedProjectileCooldownTime += Time.fixedDeltaTime;

        if (fireAction.ReadValue<float>() > 0.5f && accumulatedProjectileCooldownTime >= projectileCooldownSec)
        {            
            var spawner = playerProjectileSpawnPoint[numProjectilesFiredByPlayer % playerProjectileSpawnPoint.Length];
            var projectile = Instantiate(playerProjectilePrefab, spawner.transform);
            Destroy(projectile, 10f);

            projectile.GetComponent<Rigidbody>().AddForce(spawner.transform.forward * projectileForce, ForceMode.Impulse);

            accumulatedProjectileCooldownTime = 0.0f;
            numProjectilesFiredByPlayer++;
        }
    }
}
