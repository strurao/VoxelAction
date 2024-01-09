using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CharacterControllerCS : MonoBehaviour { 
    // State
    public enum PlayerState
    {
        Idle,
        Walking,
        Running,
        Jumping
    }
    private PlayerState currentState;

    [SerializeField]
    private Transform characterBody;
    [SerializeField]
    private Transform cameraArm;
    [SerializeField]
    private float rotSensitivity = 2f; // 카메라 회전 감도

    // Jump
    private float lastJumpTime;
    private const float doubleJumpDelay = 0.5f;
    private int jumpCount = 0;

    public float speed; // 인스펙터에서 설정 가능하도록 public
    public float rotateSpeed;
    public float jumpPower; // 인스펙터에서 설정 가능하도록 public
    public GameObject[] weapons;
    public bool[] hasWeapons;
    public GameObject[] grenades;
    public int hasGrenades;
    public GameObject leftzet;
    public GameObject rightzet;

    // 소모품
    public int ammo;
    public int coin;
    public int health;
    // 소유 가능한 최대치
    public int maxAmmo;
    public int maxCoin;
    public int maxHealth;
    public int maxHasGrenades;

    float hAxis;
    float vAxis;

    bool runDown; // KEY: left shift
    bool jumpDown; // KEY: space
    bool dodgeDown; // KEY: left ctrl
    bool iteractionDown; // KEY: e
    bool swapDown1; // Weapon KEY: 1
    bool swapDown2; // Weapon KEY: 2
    bool swapDown3; // Weapon KEY: 3
    bool fireDown; // Attack KEY: (default) left mouse click

    bool isWalking;
    bool isJump;
    bool isDodge;
    bool isSwap; // 교체 시간차를 위한 플래그 로직
    bool isFireReady = true; // fireDelayTime 을 기다리면 공격 가능!
    Vector3 moveVec;
    Vector3 dodgeVec;

    Rigidbody rigidbody;
    Animator animator;

    GameObject nearObject;
    WeaponCS equippedWeapon;
    int equippedWeaponIndex = -1; // 인벤토리 초기화

    float fireDelayTime;

    // Initialization
    void Awake()
    {
        rigidbody = GetComponent<Rigidbody>();
        animator = GetComponentInChildren<Animator>();
    }

    // Update is called once per frame 입력 처리나 애니메이션 관련 코드 
    void Update()
    {
        GetInput();
        Turn();
        Dodge();
        Swap();
        Interaction();
        Jump();
        Attack();
        LookAround();
    }

    // 물리 연산과 관련된 코드
    private void FixedUpdate()
    {
        Move();
    }

    /** 입력 **/
    void GetInput()
    {
        hAxis = Input.GetAxisRaw("Horizontal");
        vAxis = Input.GetAxisRaw("Vertical");
        runDown = Input.GetButton("Run");
        jumpDown = Input.GetButtonDown("Jump");
        dodgeDown = Input.GetButtonDown("Dodge");
        iteractionDown = Input.GetButtonDown("Interaction");
        fireDown = Input.GetButtonDown("Fire1");
        swapDown1 = Input.GetButtonDown("Swap1");
        swapDown2 = Input.GetButtonDown("Swap2");
        swapDown3 = Input.GetButtonDown("Swap3");

    }

    /** 마우스 움직임에 따라 카메라 회전 **/
    private void LookAround()
    {
        Vector2 mouseDelta = new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y"));
        Vector3 camAngle = cameraArm.rotation.eulerAngles;
        float x = camAngle.x - mouseDelta.y;
        if (x < 180f)
        {
            x = Mathf.Clamp(x, -1f, 80f);
        }
        else
        {
            x = Mathf.Clamp(x, 335f, 361f);
        }
        cameraArm.rotation = Quaternion.Euler(x, camAngle.y + mouseDelta.x * rotSensitivity, camAngle.z); // 국내에서는 일치한다
        // cameraArm.rotation = Quaternion.Euler(camAngle.x + mouseDelta.y, camAngle.y + mouseDelta.x, camAngle.z); // 해외
    }

    /** 이동 **/
    void Move()
    {
        Vector2 moveInput = new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"));
        bool isWalking = moveInput.magnitude != 0;

        if (isWalking)
        {
            Vector3 lookForward = new Vector3(cameraArm.forward.x, 0f, cameraArm.forward.z).normalized;
            Vector3 lookRight = new Vector3(cameraArm.right.x, 0f, cameraArm.right.z).normalized;
            Vector3 moveDir = lookForward * moveInput.y + lookRight * moveInput.x;
            characterBody.forward = moveDir;
            transform.position += moveDir * speed * (runDown ? 2f : 1f) * Time.deltaTime; // 달리기 시 이동 속도 증가

            isWalking = moveDir != Vector3.zero;

            // 회피 중에는 움직임 벡터에서 회피방향 벡터로 바뀌도록 설정
            if (isDodge)
                moveDir = dodgeVec;

            // 무기 교체, 망치를 휘두르고 있는 중에는 정지
            if (isSwap || !isFireReady)
                moveDir = Vector3.zero;
        }
        animator.SetBool("isWalk", isWalking); // 비교 연산자 설정
        animator.SetBool("isRun", characterBody.forward != Vector3.zero && runDown);
    }

    // 회전 
    void Turn()
    {
        // 카메라의 방향을 기준으로 캐릭터가 회전하도록 설정
        Vector3 lookDirection = new Vector3(cameraArm.forward.x, 0f, cameraArm.forward.z).normalized;
        if (lookDirection != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(lookDirection);
            characterBody.rotation = Quaternion.Slerp(characterBody.rotation, targetRotation, Time.deltaTime * rotateSpeed);
        }
    }

    /** 점프, 더블 점프 **/
    void Jump()
    {
        bool isDoubleJump = (jumpDown && (Time.time - lastJumpTime < doubleJumpDelay));
        if ((jumpDown && !isJump && !isDodge && !isSwap) || isDoubleJump && jumpCount < 2)
        {
            leftzet.SetActive(true);
            rightzet.SetActive(true);
            Debug.Log("SetActive True");
            float jumpForce = jumpPower;

            if (isDoubleJump && jumpCount == 1)
            {
                jumpForce *= 1.5f; // 더블 점프 시에만 증가된 힘을 사용
            }

            rigidbody.velocity = Vector3.zero; // 속도 초기화
            rigidbody.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
            animator.SetBool("isJump", true);
            animator.SetTrigger("doJump");
            isJump = true;
            jumpCount++;
            lastJumpTime = Time.time;

        }
    }

    /** 회피 **/
    void Dodge()
    {
        if (dodgeDown && characterBody.forward != Vector3.zero && !isJump && !isDodge && !isSwap)
        {
            dodgeVec = characterBody.forward;
            speed *= 2; // 회피는 이동 속도의 2배
            animator.SetTrigger("doDodge");
            isDodge = true;

            Invoke("DodgeOut", 0.5f); // 시간차를 두어서 함수를 호출
        }
    }

    void DodgeOut()
    {
        speed *= 0.5f; // 회피 동작 후에 속도를 원래대로 돌립니다.
        isDodge = false;
    }

    void Swap()
    {
        if (swapDown1 && (!hasWeapons[0] || equippedWeaponIndex == 0))
            return;
        if (swapDown2 && (!hasWeapons[1] || equippedWeaponIndex == 1))
            return;
        if (swapDown3 && (!hasWeapons[2] || equippedWeaponIndex == 2))
            return;

        int weaponIndex = -1;
        if (swapDown1) weaponIndex = 0;
        if (swapDown2) weaponIndex = 1;
        if (swapDown3) weaponIndex = 2;

        if (swapDown1 || swapDown2 || swapDown3 && !isJump && !isDodge)
        {
            if (equippedWeapon != null)
                equippedWeapon.gameObject.SetActive(false);

            equippedWeaponIndex = weaponIndex;
            equippedWeapon = weapons[weaponIndex].GetComponent<WeaponCS>();
            equippedWeapon.gameObject.SetActive(true);

            animator.SetTrigger("doSwap");
            isSwap = true;
            Invoke("SwapOut", 0.4f);
        }
    }

    void SwapOut()
    {
        isSwap = false;
    }

    void Attack()
    {

        if (equippedWeapon == null) return;

        fireDelayTime += Time.deltaTime; // 공격딜레이에 시간을 더해주고 공격 가능 여부를 확인
        isFireReady = equippedWeapon.rate < fireDelayTime; // 공격속도보다 시간이 커지면, 공격 가능

        if (fireDown && isFireReady && !isDodge && !isSwap)
        {
            equippedWeapon.Use();
            animator.SetTrigger("doSwing");
            fireDelayTime = 0; // 공격했으니 초기화
            Debug.Log("Swing");
        }
    }

    /** 상호작용: E키 **/
    void Interaction()
    {
        if (iteractionDown && nearObject != null && !isJump && !isDodge)
        {
            if (nearObject.tag == "Weapon") // 무기 입수
            {
                ItemCS item = nearObject.GetComponent<ItemCS>();
                int weaponIndex = item.value;
                hasWeapons[weaponIndex] = true;

                Destroy(nearObject);
            }
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.tag == "Floor")
        {
            animator.SetBool("isJump", false);
            isJump = false;
            jumpCount = 0;

            leftzet.SetActive(false);
            rightzet.SetActive(false);
            Debug.Log("SetActive False!!!!!!!!!!");
        }
    }

    void OnTriggerEnter(Collider other)
    {
        // 아이템 입수
        if (other.tag == "Item")
        {
            ItemCS item = other.GetComponent<ItemCS>();
            switch (item.type)
            {
                case ItemCS.Type.Ammo:
                    ammo += item.value;
                    if (ammo > maxAmmo)
                        ammo = maxAmmo;
                    break;
                case ItemCS.Type.Coin:
                    coin += item.value;
                    if (coin > maxCoin)
                        coin = maxCoin;
                    break;
                case ItemCS.Type.Heart:
                    health += item.value;
                    if (health > maxHealth)
                        health = maxHealth;
                    break;
                case ItemCS.Type.Grenade:
                    grenades[hasGrenades].SetActive(true);
                    hasGrenades += item.value;
                    if (hasGrenades > maxHasGrenades)
                        hasGrenades = maxHasGrenades;
                    break;
            }
            Destroy(other.gameObject);
        }
    }

    void OnTriggerStay(Collider other)
    {
        // 무기 입수
        if (other.tag == "Weapon")
            nearObject = other.gameObject;
    }

    void OnTriggerExit(Collider other)
    {
        if (other.tag == "Weapon")
            nearObject = null;
    }
}
