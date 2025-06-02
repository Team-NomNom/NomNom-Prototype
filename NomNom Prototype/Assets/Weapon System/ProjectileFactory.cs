using System;
using System.Collections;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Assertions.Must;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// TO DO LIST

// - Implement bullet drop (still confused how we would do that here, that seems like logic of the projectile itself)
// - Implement recoil (hard to do this until this class is attached to the drive system)
// - Implement visual effects (e.g. muzzle flash)
// - Implement method for single-use projectile (e.g. melee weapon)
//      - this projectile should spawn as a child of the factory
//      - and only one should be spawned at a time

public class ProjectileFactory : MonoBehaviour
{
    [SerializeField]
    public GameObject projectile;

    [SerializeField]
    public bool movesIndependently = true;

    [SerializeField]
    public bool fullAuto = true;


    [SerializeField]
    public int numBullets = 1;

    [SerializeField]
    public float bulletSpreadAngle = 0.0f;

    [SerializeField]
    public float bulletsPerSecond = 1.0f;

    // Not exactly sure how this should be implemented
    [SerializeField]
    public float recoilFactor = 0.0f;

    [SerializeField]
    public float joystickDeadzone = 0.5f;

    [SerializeField]
    public Text debugScreenText;


    // THIS SHOULD BE TEMPORARY the input system should be defined statically in the master tank class
    private InputSystem_Actions input;

    // TEMPORARY This should also be passed down from the master tank class
    private PlayerInput playerInput;

    private Camera mainCamera;

    // Update these accordingly when the model for the turret changes
    // We could even make them serializable?
    [SerializeField]
    private Transform shaftTransform;
    [SerializeField]
    private float joystickAngleOffset = 90f;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // THIS SHOULD BE TEMPORARY the input system should be defined statically in the master tank class
        input = new InputSystem_Actions();

        // TEMPORARY This should also be passed down from the master tank class
        playerInput = GetComponent<PlayerInput>();

        try
        {
            mainCamera = GameObject.Find("Main Camera").GetComponent<Camera>();
        }
        catch
        {
            Debug.LogError("Couldn't find camera! Is the camera named something other than 'Main Camera'?");
        }


    }

    // Update is called once per frame
    void Update()
    {
        if (debugScreenText != null)
        {
            debugScreenText.text = $"# of bullets: {numBullets}\nBullet spread angle: {bulletSpreadAngle} degrees\nBullets per second: {bulletsPerSecond}\nMoves Independently? {movesIndependently}\nFull auto? {fullAuto}";
        }


        if (movesIndependently)
        {
            if (playerInput.currentControlScheme == "Keyboard&Mouse")
            {
                Ray ray = mainCamera.ScreenPointToRay(Mouse.current.position.ReadValue());
                if (Physics.Raycast(ray, out RaycastHit hit, 100f))
                {
                    Vector3 targetPos = hit.point;

                    Vector3 direction = targetPos - transform.position;
                    direction.y = 0f;

                    if (direction != Vector3.zero)
                    {
                        Quaternion targetRotation = Quaternion.LookRotation(direction);
                        transform.rotation = targetRotation;
                    }
                }
            }
            else if (playerInput.currentControlScheme == "Gamepad")
            {
                Vector2 rStickPos = Gamepad.current.rightStick.ReadValue();
                
                // Ensures deadzone is respected
                if (Math.Abs(rStickPos.x) <= joystickDeadzone && Math.Abs(rStickPos.y) <= joystickDeadzone) { return; }

                // Just wanted to say I REMEMBERED HOW TO DO THIS COMPLETELY MYSELF WITHOUT CHATGPT I FEEL AMAZING RN
                // This code is a little jank because again NO AI WAS INVOLVED THIS WAS ALL ME BABY
                double angle = 0;
    
                // Special cases; hard-coded
                if (rStickPos.x == 0)
                {
                    if (rStickPos.y == 1) angle = 90;
                    if (rStickPos.y == -1) angle = 270;
                }
                else
                {
                    // Gets inverse tangent of x/y values and converts value to degrees
                    angle = Math.Atan(rStickPos.y / rStickPos.x) * (180 / Math.PI);
                    // Since inverse tangent is limited from (-pi/2) to (pi/2)
                    // We must manually make angles in the 2nd/3rd quadrants opposites
                    if (rStickPos.x < 0) angle += 180;
                }
                // Apparently unity uses clockwise rotation
                // as if this engine wasn't confusing enough
                angle *= -1;

                // Sets rotation with a defined offset
                transform.rotation = Quaternion.Euler(0, (float) angle + joystickAngleOffset, 0);

            }
            else
            {
                Debug.LogError("Control scheme other than KBM/Controller is being used.");
            }
        }


    }

    private Coroutine bulletLoop;
    private bool canFire = true;
    private bool isFiring;
    // This method is public so that eventually, it can be controlled by the input system in the master tank class
    // For now, input is controlled by the Player Input component in the turret
    public void SpawnBullets(InputAction.CallbackContext context)
    {
        //Debug.Log($"Attempting loop with context as {context.phase} and bulletloop as {bulletLoop} and canFire as {canFire}");
        if (context.started && bulletLoop == null)
        {
            bulletLoop = StartCoroutine(SpawnBulletLoop());
        }
        else if (context.canceled)
        {
            isFiring = false;
            try { StopCoroutine(bulletLoop); } catch { }
            bulletLoop = null;
        }
    }


    // Debug variable used to ensure proper bullet timing
    //private float time;
    //private int bulletsFired = 0;

    private IEnumerator SpawnBulletLoop()
    {
        if (!canFire) { yield return new WaitUntil(() => canFire); }
        isFiring = true;
        while (isFiring)
        {
            // Debug logic used to ensure proper bullet timing
            //if (time == null) { time = Time.fixedTime; }
            //Debug.Log($"Spawning bullet # {bulletsFired}! {Time.fixedTime - time} seconds since last bullet.");
            //time = Time.fixedTime;
            //bulletsFired++;

            // Math that's above my pay grade but works
            float totalSpread = bulletSpreadAngle * (numBullets - 1);
            float startAngle = -totalSpread / 2f;

            for (int bulletCount = 0; bulletCount < numBullets; bulletCount++)
            {
                // Offsets bullet spawn position to match turret shaft endpoint
                Vector3 bulletPos = shaftTransform.position;

                // Calculates angle for this bullet using math from earlier
                float angle = startAngle + (bulletCount * bulletSpreadAngle);

                // Euler? I hardly know her!
                Quaternion baseRotation = shaftTransform.rotation;
                Quaternion spreadRotation = Quaternion.Euler(0f, angle, 0f);
                Quaternion bulletRot = baseRotation * spreadRotation;

                // Spawns bullet
                Instantiate(projectile, bulletPos, bulletRot);
            }

            // Waits for cooldown before continuing (this also protects the fire button from being spammed)
            yield return StartCoroutine(BulletCooldown());

            // "Full auto" means that fire will continue as long as the button is held
            // If full auto is off, then player will have to hit the fire button again for each projectile
            if (!fullAuto) { yield break; }
        }

    }

    private IEnumerator BulletCooldown()
    {
        canFire = false;
        // Waits for inverse of bullets per second (seconds per bullet)
        yield return new WaitForSeconds(1 / bulletsPerSecond);
        canFire = true;
    }

    private void Recoil()
    {
        // TODO: Make this do something
        // reminder: you want to use the recoilFactor variable
    }
}
