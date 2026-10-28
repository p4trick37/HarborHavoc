using JetBrains.Annotations;
using System.Collections;
using Unity.Jobs;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UIElements;

public class PlayerController : MonoBehaviour
{
    private ControllerInput controls;
    private Vector2 move;
    private Vector2 cursorMove;
    private bool triggerPressed;
    private bool triggerHeld;





    #region Controller
    [Header("Controller")]
    public static bool usingController;
    [SerializeField] private float controllerSensitivity;
    [SerializeField] private RectTransform cursorImageTransform;
    #endregion
    #region Current Weapon
    [Header("Current Weapon")]
    public bool usingSemi;
    public bool usingAuto;
    public bool usingShotgun;
    private int currentWeapon;
    private bool[] shootCurrentWeapon = new bool[3];
    public static bool upgradedToAuto = false;
    #endregion
    #region Weapon OverHeating
    [Header("Weapons Overheating")]
    //The max that the gun can be heated to before it is considered overheated
    public static float maxSemiUse;
    public static float maxAutoUse;
    public static float maxShotgunUse;
    [Space(15)]

    //The time that it takes for the heat to lower before over heated
    public static float heatDrainSemi;
    public static float heatDrainAuto;
    public static float heatDrainShotgun;
    [Space(15)]

    //The heat that gets added when taking a shot
    public static float heatAddSemi;
    public static float heatAddAuto;
    public static float heatAddShotgun;

    [Space(15)]
    //The time that it takes for the weapon to be done heating
    public static float heatDelaySemi;
    public static float heatDelayAuto;
    public static float heatDelayShotgun;

    //The current heat of the weapon
    public float heatSemi = 0;
    public float heatAuto = 0;
    public float heatShotgun = 0;

    //Is the weapon over heated
    private bool isSemiHeated = false;
    private bool isAutoHeated = false;
    private bool isShotgunHeated = false;

    private float semiCount = 0;
    private float autoCount = 0;
    private float shotgunCount = 0;

    #endregion
    #region Player Movement and Rotation
    [Header("Player Movement and Rotation")]
    public static float movementSpeed;
    public static float rotationSpeed;
    #endregion
    #region Bullet Speed and Spread
    [Header("Bullet Speed and Spread")]
    public static float bulletSpeed;
    public static float bulletSpread;
    #endregion
    #region Firing Delays
    [Header("Delays for Firing Weapons")]
    public static float semiAutoDelay;
    public static float autoDelay;
    public static float shotgunDelay;
    #endregion
    #region Object References
    [Header("Object References")]
    [SerializeField] private GameObject turretWeapon;
    [SerializeField] private Transform turretLocation;
    [SerializeField] private GameObject bulletPrefab;

    public UpgradeManager upgradeManager;
    [SerializeField] private GameObject upgradeManagerInstance;
    #endregion,
    #region Hit Recovery
    // --- NEW: hit-recovery + speed cap ---
    [Header("Hit Recovery & Physics Limits")]
    [SerializeField] private float normalDrag = 0.5f;     // your usual drag
    [SerializeField] private float hitSlowdown = 4f;      // temporary higher drag after being hit
    [SerializeField] private float hitSlowdownTime = 0.4f;
    [SerializeField] private float maxSpeed = 10f;        // absolute velocity clamp

    private Rigidbody2D rb;
    private Coroutine hitRecoverCR;
    #endregion
    #region Current Weapon Delays and ability to shoot
    private float semiAutoTimerDelay;
    private float autoTimerDelay;
    private float shotgunTimerDelay;
    private bool canSemiShoot;
    private bool canAutoShoot;
    private bool canShotgunShoot;
    #endregion


    //private float oldTurretRotationValue;
    private static bool firstSceneLoaded = false;
    [SerializeField] private Water water;
    private bool playerInFog;

    public bool playerDead = false;


    private void Awake()
    {
        upgradeManager = FindFirstObjectByType<UpgradeManager>();
        if (upgradeManager != null)
        {
            if(firstSceneLoaded == false)
            {
                upgradeManager.StartValue();
            }
        }
        else
        {
            upgradeManager = Instantiate(upgradeManagerInstance).GetComponent<UpgradeManager>();
            upgradeManager.StartValue();
        }
        firstSceneLoaded = true;

        controls = new ControllerInput();

        controls.Player.Move.performed += ctx => move = ctx.ReadValue<Vector2>();
        controls.Player.Move.canceled += ctx => move = Vector2.zero;

        controls.Player.Look.performed += ctx => cursorMove = ctx.ReadValue<Vector2>();
        controls.Player.Look.canceled += ctx => cursorMove = Vector2.zero;

        controls.Player.Shoot.started += ctx =>
        {
            triggerPressed = true;
        };
        controls.Player.Shoot.performed += ctx =>
        {
            triggerHeld = true;
        };
        controls.Player.Shoot.canceled += ctx =>
        {
            triggerHeld = false;
        };
    }
    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.linearDamping = normalDrag;
            // Ensure we don't spin from impacts unless you want that:
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        }
        
        if(usingController == false)
        {
            cursorImageTransform.gameObject.SetActive(false);
        }
        else
        {
            cursorImageTransform.gameObject.SetActive(true);
        }
        //oldTurretRotationValue = 0;
    }

    private void Update()
    {
        ControlWeapon();
        #region Overheating
        //Semi overheating
        if (isSemiHeated == true)
        {
            semiCount += Time.deltaTime;
            if(semiCount >= heatDelaySemi)
            {
                isSemiHeated = false;
                heatSemi = 0;
                semiCount = 0;
            }
        }
        else
        {
            heatSemi -= heatDrainSemi * Time.deltaTime;
        }
        
        if(heatSemi < 0)
        {
            heatSemi = 0;
        }
        if(heatSemi > maxSemiUse)
        {
            isSemiHeated = true;
        }
        
        //Auto overheating
        if(isAutoHeated == true)
        {
            autoCount += Time.deltaTime;
            if(autoCount >= heatDelayAuto)
            {
                isAutoHeated = false;
                heatAuto = 0;
                autoCount = 0;
            }
        }
        else
        {
            heatAuto -= heatDrainAuto * Time.deltaTime;
        }

        if(heatAuto < 0)
        {
            heatAuto = 0;
        }
        if (heatAuto > maxAutoUse)
        {
            isAutoHeated = true;
        }
        //shotgun overheating
        if (isShotgunHeated == true)
        {
            shotgunCount += Time.deltaTime;
            if (shotgunCount >= heatDelayShotgun)
            {
                isShotgunHeated = false;
                heatShotgun = 0;
                shotgunCount = 0;
            }
        }
        else
        {
            heatShotgun -= heatDrainShotgun * Time.deltaTime;
        }

        if (heatShotgun < 0)
        {
            heatShotgun = 0;
        }
        if (heatShotgun > maxShotgunUse)
        {
            isShotgunHeated = true;
        }
        #endregion


        #region Shoot Delay
        //Setting the delays for each weapon after every bullet
        if (autoTimerDelay > 0) 
        { 
            autoTimerDelay -= Time.deltaTime; 
            canAutoShoot = false; 
        } 
        else 
        { 
            canAutoShoot = true; 
        }
        if (semiAutoTimerDelay > 0) 
        { 
            semiAutoTimerDelay -= Time.deltaTime; 
            canSemiShoot = false; } 
        else 
        { 
            canSemiShoot = true; 
        }
        if (shotgunTimerDelay > 0) 
        { 
            shotgunTimerDelay -= Time.deltaTime; 
            canShotgunShoot = false; } 
        else 
        { 
            canShotgunShoot = true; 
        }
        #endregion


        if(playerDead == true)
        {
            ControllerCursorPos(false);
            if (usingController == false)
            {
                PlayerKeyboardMovement(false);
                PlayerKeyboardRotation(false);
            }
            else
            {
                PlayerControllerMovement(false);
            }
            TurretMovement(false);
            InputForShooting(false);
        }
        else
        {
            ControllerCursorPos(true);
            if (usingController == false)
            {
                PlayerKeyboardMovement(true);
                PlayerKeyboardRotation(true);
            }
            else
            {
                PlayerControllerMovement(true);
            }
            TurretMovement(true);
            InputForShooting(true);
        }



        isPlayerInFog();

        

        Debug.Log(movementSpeed);
        
    }

    private void FixedUpdate()
    {
        //Shooting the bullet
        //Restarting the delay
        //Not allowing the player shoot another bullet


        if (usingSemi)
        {
            if (shootCurrentWeapon[currentWeapon] && isSemiHeated == false)
            {
                ShootSingleBullet();
                semiAutoTimerDelay = semiAutoDelay;
                shootCurrentWeapon[currentWeapon] = false;
            }
        }

        else if (usingAuto)
        {
            if (shootCurrentWeapon[currentWeapon] && canAutoShoot && isAutoHeated == false)
            {
                ShootSingleBullet();
                autoTimerDelay = autoDelay;
            }
        }
        else
        {
            if (shootCurrentWeapon[currentWeapon] && isShotgunHeated == false)
            {
                //Shoot3Bullets();
                shotgunTimerDelay = shotgunDelay;
                shootCurrentWeapon[currentWeapon] = false;
            }
        }

        // --- NEW: absolute speed clamp so pushes never drift forever ---
        if (rb && rb.linearVelocity.magnitude > maxSpeed)
            rb.linearVelocity = rb.linearVelocity.normalized * maxSpeed;
    }

    // --- NEW: call this when the player takes damage (from PlayerHealth) ---
    public void OnHitByEnemy()
    {
        if (hitRecoverCR != null) StopCoroutine(hitRecoverCR);
        hitRecoverCR = StartCoroutine(RecoverFromHit());
    }

    private IEnumerator RecoverFromHit()
    {
        if (rb)
        {
            float oldDrag = rb.linearDamping;
            rb.linearDamping = hitSlowdown;                  // temporarily add friction
            yield return new WaitForSeconds(hitSlowdownTime);
            rb.linearDamping = normalDrag;                   // restore normal movement feel
        }
        hitRecoverCR = null;
    }

    //Player Movement
    private void PlayerKeyboardMovement(bool shouldMove)
    {
        if (shouldMove)
        {   
            float movementInput = Input.GetAxisRaw("Vertical");
            transform.position += transform.up * movementInput * movementSpeed * Time.deltaTime;
        }
    }
    private void PlayerControllerMovement(bool shouldMove)
    {
        if (shouldMove)
        {
            Vector2 m = new Vector2(move.x, move.y);
            
            float angle = Mathf.Atan(m.y / m.x) * Mathf.Rad2Deg;
            if(float.IsNaN(angle))
            {
                angle = 0;
                transform.position += new Vector3(0, 0, 0);
            }
            else
            {
                if(m.x < 0)
                {
                    angle += 90;
                }
                else
                {
                    angle -= 90;
                }
                transform.rotation = Quaternion.Euler(0, 0, angle);
                Vector3 moveToward = new Vector3(Mathf.Cos((angle + 90) * Mathf.Deg2Rad), Mathf.Sin((angle + 90) * Mathf.Deg2Rad), 0);
                transform.position += moveToward * movementSpeed * Time.deltaTime;
            }
        }
    }
    //Player Rotation
    private void PlayerKeyboardRotation(bool shouldRotate)
    {
        if (shouldRotate)
        {
            transform.Rotate(0, 0, -(Input.GetAxisRaw("Horizontal") * rotationSpeed * Time.deltaTime * 30));
        }
    }

    //Movement of the turret with the mouse
    private void TurretMovement(bool shouldMove)
    {
        if (shouldMove)
        {
            turretLocation.transform.rotation = Quaternion.Euler(0, 0, GetRotationMouseTracker());
        }
    }
    private void InputForShooting(bool shouldShoot)
    {
        if(shouldShoot == true)
        {
            if (usingController == false)
            {

                if (usingSemi)
                {
                    //heatSemi = Heating(heatSemi, maxSemiUse, heatDrainSemi, heatDelaySemi);
                    if (Input.GetMouseButtonDown(0) && canSemiShoot)
                    {
                        shootCurrentWeapon[currentWeapon] = true;

                    }
                }
                else if (usingAuto)
                {
                    if (Input.GetMouseButton(0))
                    {
                        shootCurrentWeapon[currentWeapon] = true;

                    }
                    if (Input.GetMouseButtonUp(0))
                    {
                        shootCurrentWeapon[currentWeapon] = false;
                    }
                }
                else
                {
                    if (Input.GetMouseButtonDown(0) && canShotgunShoot)
                    {
                        shootCurrentWeapon[currentWeapon] = true;
                    }
                }
            }
            else
            {
                if (usingSemi)
                {
                    if (triggerPressed == true && canSemiShoot)
                    {
                        shootCurrentWeapon[currentWeapon] = true;
                        triggerPressed = false;
                    }
                }
                else if (usingAuto)
                {
                    if (triggerHeld == true)
                    {
                        shootCurrentWeapon[currentWeapon] = true;
                    }
                    if (triggerHeld == false)
                    {
                        shootCurrentWeapon[currentWeapon] = false;
                    }
                }
                else
                {
                    if (triggerPressed == true && canShotgunShoot)
                    {
                        shootCurrentWeapon[currentWeapon] = true;
                        triggerPressed = false;
                    }
                }
            }
        }       
    }

    private void ControlWeapon()
    {
        if(upgradedToAuto == true)
        {
            usingAuto = true;
            usingSemi = false;
            usingShotgun = false;
        }
        else
        {
            usingAuto = false;
            usingSemi = true;
            usingShotgun = false;
        }
    }



    //Shoots a bullet in the direction of the mouse
    private void ShootSingleBullet()
    {       
        GameObject bullet = Instantiate(bulletPrefab, turretLocation.position, Quaternion.identity);
        SpriteRenderer bulletSprite = bullet.GetComponentInChildren<SpriteRenderer>();
        if(UpgradeManager.shouldBulletColorChange == true)
        {
            bulletSprite.color = upgradeManager.upBulletColor;
        }
        float angle = GetRotationMouseTracker() + 90;
        Vector2 bulletDirection = new Vector2(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad));
        bullet.GetComponent<Rigidbody2D>().AddForce(bulletDirection * bulletSpeed, ForceMode2D.Impulse);
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayPlayerShoot();
        }

        if(usingSemi)
        {
            heatSemi += heatAddSemi;
        }
        else
        {
            heatAuto += heatAddAuto;
        }
    }
    /*
    private void Shoot3Bullets()
    {
        if(usingController == false)
        {
            GameObject bullet1 = Instantiate(bulletPrefab, turretLocation.position, Quaternion.identity);
            GameObject bullet2 = Instantiate(bulletPrefab, turretLocation.position, Quaternion.identity);
            GameObject bullet3 = Instantiate(bulletPrefab, turretLocation.position, Quaternion.identity);

            float baseAngle = GetRotationMouseTracker() + 90;
            float angle1 = baseAngle;
            float angle2 = baseAngle + bulletSpread;
            float angle3 = baseAngle - bulletSpread;

            Vector2 dir1 = new Vector2(Mathf.Cos(angle1 * Mathf.Deg2Rad), Mathf.Sin(angle1 * Mathf.Deg2Rad));
            Vector2 dir2 = new Vector2(Mathf.Cos(angle2 * Mathf.Deg2Rad), Mathf.Sin(angle2 * Mathf.Deg2Rad));
            Vector2 dir3 = new Vector2(Mathf.Cos(angle3 * Mathf.Deg2Rad), Mathf.Sin(angle3 * Mathf.Deg2Rad));

            bullet1.GetComponent<Rigidbody2D>().AddForce(dir1 * bulletSpeed, ForceMode2D.Impulse);
            bullet2.GetComponent<Rigidbody2D>().AddForce(dir2 * bulletSpeed, ForceMode2D.Impulse);
            bullet3.GetComponent<Rigidbody2D>().AddForce(dir3 * bulletSpeed, ForceMode2D.Impulse);
        }
        else
        {
            GameObject bullet1 = Instantiate(bulletPrefab, turretLocation.position, Quaternion.identity);
            GameObject bullet2 = Instantiate(bulletPrefab, turretLocation.position, Quaternion.identity);
            GameObject bullet3 = Instantiate(bulletPrefab, turretLocation.position, Quaternion.identity);

            //float baseAngle = NaFNumber(JoyStickAngle(RightJoyStickInput())) + 90;
            //float angle1 = baseAngle;
            //float angle2 = baseAngle + bulletSpread;
            //float angle3 = baseAngle - bulletSpread;

            //Vector2 dir1 = new Vector2(Mathf.Cos(angle1 * Mathf.Deg2Rad), Mathf.Sin(angle1 * Mathf.Deg2Rad));
            //Vector2 dir2 = new Vector2(Mathf.Cos(angle2 * Mathf.Deg2Rad), Mathf.Sin(angle2 * Mathf.Deg2Rad));
           //Vector2 dir3 = new Vector2(Mathf.Cos(angle3 * Mathf.Deg2Rad), Mathf.Sin(angle3 * Mathf.Deg2Rad));

            //bullet1.GetComponent<Rigidbody2D>().AddForce(dir1 * bulletSpeed, ForceMode2D.Impulse);
            //bullet2.GetComponent<Rigidbody2D>().AddForce(dir2 * bulletSpeed, ForceMode2D.Impulse);
            //bullet3.GetComponent<Rigidbody2D>().AddForce(dir3 * bulletSpeed, ForceMode2D.Impulse);
        }

        heatShotgun += heatAddShotgun;
        
    }
    */

    //Gathers the rotation angle of the player when using the mouse
    private float GetRotationMouseTracker()
    {
        float rotationAngleRad;
        float rotationAngleDeg;
        if(usingController == false)
        {
            rotationAngleRad = Mathf.Atan(MousePosition().y / MousePosition().x);
            rotationAngleDeg = Mathf.Rad2Deg * rotationAngleRad;

            if (MousePosition().x < 0) rotationAngleDeg += 90;
            else rotationAngleDeg -= 90;
        }
        else
        {
            rotationAngleRad = Mathf.Atan(CursorPositionController(cursorImageTransform).y / CursorPositionController(cursorImageTransform).x);
            rotationAngleDeg = Mathf.Rad2Deg * rotationAngleRad;

            if (CursorPositionController(cursorImageTransform).x < 0) rotationAngleDeg += 90;
            else rotationAngleDeg -= 90;
        }

        return rotationAngleDeg;
    }

    //Gathers the mouse position with its origin at the player on the camera
    private Vector2 MousePosition()
    {
        Vector2 centerOfScreen = new Vector2(Screen.width / 2, Screen.height / 2);
        Vector2 playerOrigen = turretLocation.position;
        Vector2 playerScreen = Camera.main.WorldToScreenPoint(playerOrigen);
        Vector2 mousePositionInput = Input.mousePosition;
        Vector2 mousePosition = mousePositionInput - playerScreen;
        return mousePosition;
    }
    private void ControllerCursorPos(bool shouldMove)
    {
        if(shouldMove == true)
        {
            cursorImageTransform.position += new Vector3(cursorMove.x, cursorMove.y, 0) * controllerSensitivity * Time.deltaTime;

            if (cursorImageTransform.anchoredPosition.x > 935)
            {
                cursorImageTransform.anchoredPosition = new Vector3(935, cursorImageTransform.anchoredPosition.y, cursorImageTransform.position.z);
            }
            if (cursorImageTransform.anchoredPosition.x < -935)
            {
                cursorImageTransform.anchoredPosition = new Vector3(-935, cursorImageTransform.anchoredPosition.y, cursorImageTransform.position.z);
            }
            if (cursorImageTransform.anchoredPosition.y > 515)
            {
                cursorImageTransform.anchoredPosition = new Vector3(cursorImageTransform.anchoredPosition.x, 515, cursorImageTransform.position.z);
            }
            if (cursorImageTransform.anchoredPosition.y < -515)
            {
                cursorImageTransform.anchoredPosition = new Vector3(cursorImageTransform.anchoredPosition.x, -515, cursorImageTransform.position.z);
            }
        }
        
    }
    /*
    private Vector2 RightJoyStickInput()
    {
        float x = Input.GetAxis("RightStickX");
        float y = Input.GetAxis("RightStickY");
        if(x < 0.1 && x > -0.1)
        {
            x = 0;
        }
        if(y < 0.1 && y > -0.1)
        {
            y = 0;
        }
        Vector2 input = new Vector2(x, y);
        return input;
    }
    private float JoyStickAngle(Vector2 input)
    {
        float angleRad = Mathf.Atan(input.y / input.x);
        float angleDeg = Mathf.Rad2Deg * angleRad;
        if(input.x < 0)
        {
            angleDeg += 90;
        }
        else
        {
            angleDeg -= 90;
        }
        return angleDeg;
    }

    private float NaFNumber(float number)
    {
        if (float.IsNaN(number) == true)
        {
            number = oldTurretRotationValue;
        }
        oldTurretRotationValue = number;
        return number;
    }

    */
    private void OnTriggerExit2D(Collider2D collision)
    {
        Water water = collision.GetComponent<Water>();
        if(water != null)
        {
            playerInFog = true;
        }
    }
    private void OnTriggerEnter2D(Collider2D collision)
    {
        Water water = collision.GetComponent<Water>();
        if(water != null)
        {
            playerInFog = false;
        }
    }
    private void isPlayerInFog()
    {
        if(playerInFog == true)
        {
            water.playerInFog = true;
        }
        else
        {
            water.playerInFog = false;
        }
    }

    private Vector2 CursorPositionController(RectTransform image)
    {
        Vector2 screenPos = RectTransformUtility.WorldToScreenPoint(Camera.main, image.position);
        Vector2 worldPos = Camera.main.ScreenToWorldPoint(screenPos);
        float positionX = worldPos.x - turretLocation.position.x;
        float positionY = worldPos.y - turretLocation.position.y;
        Vector2 position = new Vector2(positionX, positionY);
        return position;
    }

    private void OnEnable()
    {
        controls.Player.Enable();
    }
    private void OnDisable()
    {
        controls.Disable();
    }
}
