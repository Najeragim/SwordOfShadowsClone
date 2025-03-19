using UnityEngine;
using System.Collections;
using System.Collections.Generic;
public class PlayerMovement : MonoBehaviour
{
    [SerializeField] private float normalSpeed = 3f;
    [SerializeField] private float sprintSpeed = 5f;
    [SerializeField] private float chargedAttackSpeed = 1f;
    [SerializeField] private float chargedAttackCooldown = 3f;
    [SerializeField] private float chargedAttackDamageMultiplier = 2f;
    private bool isChargedAttacking = false;
    private bool canChargedAttack = true;
    private Coroutine chargedAttackCoroutine;
    private float speed;
    private bool isSprinting = false;
    private bool canRegenerateStamina = true;
    private Coroutine staminaDrainCoroutine;
    AudioManager audioManager;
    public PlayerValues playerValues;

    private Rigidbody2D playerRb;
    private Vector2 moveInput;
    private Vector2 lastMoveDirection = Vector2.down;
    private Animator playerAnimator;

    public StaminaBar staminaBar;
    private Coroutine regenStaminaCoroutine;

    private bool canHeal = true;
    private void Awake()
    {
        audioManager = GameObject.FindGameObjectWithTag("AudioManager").GetComponent<AudioManager>();
    }

    void Start()
    {
        playerRb = GetComponent<Rigidbody2D>();
        playerAnimator = GetComponent<Animator>();
        speed = normalSpeed;
        lastMoveDirection = Vector2.down;
    }

    void Update()
    {
        float moveX = Input.GetAxisRaw("Horizontal");
        float moveY = Input.GetAxisRaw("Vertical");
        moveInput = new Vector2(moveX, moveY).normalized;

        if (moveInput.sqrMagnitude > 0)
        {
            lastMoveDirection = moveInput;
        }

        playerAnimator.SetFloat("Horizontal", lastMoveDirection.x);
        playerAnimator.SetFloat("Vertical", lastMoveDirection.y);
        playerAnimator.SetFloat("Speed", moveInput.sqrMagnitude);

        DetectArcadeButtons();
    }
    private void FixedUpdate()
    {
        if (moveInput.sqrMagnitude > 0 && !playerAnimator.GetBool("IsProtecting"))
        {
            playerRb.MovePosition(playerRb.position + moveInput * speed * Time.fixedDeltaTime);

            if (speed == normalSpeed)
            {
                if (audioManager.SFXSource.clip != audioManager.player_walk || !audioManager.SFXSource.isPlaying)
                {
                    audioManager.PlaySFXLoop(audioManager.player_walk);
                }
            }
            else if (speed == sprintSpeed)
            {
                if (audioManager.SFXSource.clip != audioManager.player_run || !audioManager.SFXSource.isPlaying)
                {
                    audioManager.PlaySFXLoop(audioManager.player_run);
                }
            }
        }
        else
        {
            audioManager.StopSFX();
        }

        if (moveInput.sqrMagnitude == 0)
        {
            playerRb.linearVelocity = Vector2.zero;
        }
    }

    private void DetectArcadeButtons()
    {
        // Ataque (Joystick: Button 0, Teclado: Tecla Espacio)
        if (Input.GetKeyDown(KeyCode.JoystickButton0) || Input.GetKeyDown(KeyCode.Space))
        {
            Attack();
        }

        // Proteger (Joystick: Button 1, Teclado: Tecla K)
        if (Input.GetKeyDown(KeyCode.JoystickButton1) || Input.GetKeyDown(KeyCode.LeftShift))
        {
            StartProtecting();
        }
        if (Input.GetKeyUp(KeyCode.JoystickButton1) || Input.GetKeyUp(KeyCode.LeftShift))
        {
            StopProtecting();
        }

        // Sprint (Joystick: Button 2, Teclado: Tecla L)
        if (Input.GetKeyDown(KeyCode.JoystickButton2) || Input.GetKeyDown(KeyCode.LeftControl))
        {
            StartSprint();
        }
        if (Input.GetKeyUp(KeyCode.JoystickButton2) || Input.GetKeyUp(KeyCode.LeftControl))
        {
            StopSprint();
        }

        // Curar (Joystick: Button 3, Teclado: Tecla H)
        if ((Input.GetKeyDown(KeyCode.JoystickButton3) || Input.GetKeyDown(KeyCode.H)) && canHeal)
        {
            StartCoroutine(HealOverTime(3, 3f));
        }

        // Golpe cargado (Joystick: Button 4, Teclado: Tecla C)
        if (Input.GetKeyDown(KeyCode.JoystickButton4) || Input.GetKeyDown(KeyCode.C))
        {
            if (!playerAnimator.GetBool("IsProtecting"))
            {
                StartChargedAttack();
            }
            else
            {
                Debug.Log("No se puede realizar un ataque cargado mientras se está protegiendo.");
            }
        }

        // Esquivar (Joystick: Button 5, Teclado: Tecla Z)
        if (Input.GetKeyDown(KeyCode.JoystickButton5) || Input.GetKeyDown(KeyCode.Z))
        {
            Debug.Log("Esquivar");
        }

        if (Input.GetKeyDown(KeyCode.JoystickButton6) || Input.GetKeyDown(KeyCode.Q))
        {
            Debug.Log("Cambio de arma");
        }

        if (Input.GetKeyDown(KeyCode.JoystickButton7) || Input.GetKeyDown(KeyCode.E))
        {
            Debug.Log("Interactuar con el entorno");
        }
    }

    private void StartSprint()
    {
        if (isChargedAttacking) return;
        if (playerValues.currentStamina > 0)
        {
            isSprinting = true;
            speed = sprintSpeed;
            canRegenerateStamina = false;

            if (regenStaminaCoroutine != null)
            {
                StopCoroutine(regenStaminaCoroutine);
                regenStaminaCoroutine = null;
            }

            if (staminaDrainCoroutine == null)
            {
                staminaDrainCoroutine = StartCoroutine(DrainStamina());
            }


        }
    }

    private void StopSprint()
    {
        isSprinting = false;
        speed = normalSpeed;
        if (staminaDrainCoroutine != null)
        {
            StopCoroutine(staminaDrainCoroutine);
            staminaDrainCoroutine = null;
        }

        if (regenStaminaCoroutine != null)
        {
            StopCoroutine(regenStaminaCoroutine);
        }

        regenStaminaCoroutine = StartCoroutine(WaitAndRegenerateStamina());
    }

    private IEnumerator DrainStamina()
    {
        while (isSprinting && playerValues.currentStamina > 0)
        {
            playerValues.LoseStamina(1);
            yield return new WaitForSeconds(1f);
        }
        StopSprint();
    }

    private IEnumerator WaitAndRegenerateStamina()
    {
        yield return new WaitForSeconds(2f);
        canRegenerateStamina = true;
        StartCoroutine(RegenerateStamina());

        if (regenStaminaCoroutine != null)
        {
            StopCoroutine(regenStaminaCoroutine);
        }

        regenStaminaCoroutine = StartCoroutine(RegenerateStamina());
    }

    private IEnumerator RegenerateStamina()
    {
        while (canRegenerateStamina && playerValues.currentStamina < playerValues.maxStamina)
        {
            playerValues.currentStamina++;
            staminaBar.SetStamina(playerValues.currentStamina);
            yield return new WaitForSeconds(4f);
        }

        regenStaminaCoroutine = null;
    }

    private IEnumerator HealOverTime(int healAmount, float duration)
    {
        canHeal = false;
        int healed = 0;

        while (healed < healAmount && playerValues.currentHealth < playerValues.maxHealth)
        {
            playerValues.currentHealth++;
            playerValues.healthBar.SetHealth(playerValues.currentHealth);
            yield return new WaitForSeconds(duration / healAmount);
            healed++;
        }

        yield return new WaitForSeconds(5f);
        canHeal = true;
    }

    private void Attack()
    {
        if (isChargedAttacking) return;
        if (!playerAnimator.GetBool("IsAttacking"))
        {
            playerAnimator.SetBool("IsAttacking", true);
            StartCoroutine(ResetAttackAnimation());

            float attackRange = 1.0f;
            float attackWidth = 1f;

            // Dirección de ataque basada en la última dirección de movimiento
            Vector2 attackDirection = moveInput == Vector2.zero ? lastMoveDirection : moveInput;
            Vector2 attackOrigin = (Vector2)transform.position + attackDirection * 0.5f;

            Debug.DrawRay(attackOrigin, attackDirection * attackRange, Color.red, 0.5f);

            Vector2 topLeft = attackOrigin + new Vector2(-attackWidth / 2, attackRange / 2);
            Vector2 bottomRight = attackOrigin + new Vector2(attackWidth / 2, -attackRange / 2);

            Debug.DrawLine(topLeft, new Vector2(topLeft.x, bottomRight.y), Color.green, 0.5f);
            Debug.DrawLine(topLeft, new Vector2(bottomRight.x, topLeft.y), Color.green, 0.5f);
            Debug.DrawLine(bottomRight, new Vector2(topLeft.x, bottomRight.y), Color.green, 0.5f);
            Debug.DrawLine(bottomRight, new Vector2(bottomRight.x, topLeft.y), Color.green, 0.5f);

            EnemyValues[] enemies = Object.FindObjectsByType<EnemyValues>(FindObjectsSortMode.None);
            foreach (EnemyValues enemy in enemies)
            {
                if (IsEnemyInAttackArea(enemy, topLeft, bottomRight))
                {
                    enemy.TakeDamage(1);
                }
            }
        }
    }
    private bool IsEnemyInAttackArea(EnemyValues enemy, Vector2 topLeft, Vector2 bottomRight)
    {
        if (enemy == null) return false;

        SpriteRenderer enemySprite = enemy.GetComponent<SpriteRenderer>();
        if (enemySprite == null) return false;

        Bounds enemyBounds = enemySprite.bounds;

        // Verifica si el centro del sprite está dentro del área de ataque
        return (enemyBounds.center.x >= topLeft.x && enemyBounds.center.x <= bottomRight.x) &&
               (enemyBounds.center.y >= bottomRight.y && enemyBounds.center.y <= topLeft.y);
    }


    private IEnumerator ResetAttackAnimation()
    {
        yield return new WaitForSeconds(playerAnimator.GetCurrentAnimatorStateInfo(0).length);
        playerAnimator.SetBool("IsAttacking", false);
    }

    private void StartProtecting()
    {
        playerAnimator.SetBool("IsProtecting", true);
        speed = 0f;
        playerRb.linearVelocity = Vector2.zero; // Detener cualquier movimiento residual
        canRegenerateStamina = false;

        playerAnimator.SetFloat("Horizontal", 0f);
        playerAnimator.SetFloat("Vertical", 0f);
        playerAnimator.SetFloat("Speed", 0f);

        audioManager.StopSFX();
    }

    private void StopProtecting()
    {
        playerAnimator.SetBool("IsProtecting", false);
        speed = normalSpeed;
        canRegenerateStamina = true;
    }

    private void StartChargedAttack()
    {
        if (playerAnimator.GetBool("IsProtecting"))
        {
            Debug.Log("No se puede realizar un ataque cargado mientras se está protegiendo.");
            return;
        }
        if (!isChargedAttacking && canChargedAttack)
        {
            isChargedAttacking = true;
            canChargedAttack = false;
            playerAnimator.SetBool("IsChargedAttacking", true);

            // Detener cualquier movimiento actual
            moveInput = Vector2.zero;
            playerRb.linearVelocity = Vector2.zero;

            chargedAttackCoroutine = StartCoroutine(ChargedAttack());
        }
    }

    private IEnumerator ChargedAttack()
    {
        yield return new WaitForSeconds(0.5f);

        Vector2 attackDirection = lastMoveDirection;

        float attackDuration = 0.5f;
        float timer = 0f;

        while (timer < attackDuration)
        {
            Vector2 newPosition = playerRb.position + attackDirection * chargedAttackSpeed * Time.fixedDeltaTime;
            playerRb.MovePosition(newPosition);

            DetectEnemiesDuringChargedAttack(attackDirection);

            timer += Time.fixedDeltaTime;
            yield return new WaitForFixedUpdate();
        }

        // Detener el movimiento
        playerRb.linearVelocity = Vector2.zero;

        playerAnimator.SetBool("IsChargedAttacking", false);
        isChargedAttacking = false;
        damagedEnemies.Clear();

        StartCoroutine(ChargedAttackCooldown());
    }

    private HashSet<EnemyValues> damagedEnemies = new HashSet<EnemyValues>();
    private void DetectEnemiesDuringChargedAttack(Vector2 attackDirection)
    {
        float attackRange = 1.5f;
        float attackWidth = 1f;

        Vector2 attackOrigin = (Vector2)transform.position + attackDirection * 0.5f;

        Debug.DrawRay(attackOrigin, attackDirection * attackRange, Color.red, 0.5f);

        Vector2 topLeft = attackOrigin + new Vector2(-attackWidth / 2, attackRange / 2);
        Vector2 bottomRight = attackOrigin + new Vector2(attackWidth / 2, -attackRange / 2);

        Debug.DrawLine(topLeft, new Vector2(topLeft.x, bottomRight.y), Color.green, 0.5f);
        Debug.DrawLine(topLeft, new Vector2(bottomRight.x, topLeft.y), Color.green, 0.5f);
        Debug.DrawLine(bottomRight, new Vector2(topLeft.x, bottomRight.y), Color.green, 0.5f);
        Debug.DrawLine(bottomRight, new Vector2(bottomRight.x, topLeft.y), Color.green, 0.5f);

        EnemyValues[] enemies = Object.FindObjectsByType<EnemyValues>(FindObjectsSortMode.None);
        foreach (EnemyValues enemy in enemies)
        {
            // Verificar si el enemigo ya ha sido dañado durante este ataque cargado
            if (!damagedEnemies.Contains(enemy) && IsEnemyInAttackArea(enemy, topLeft, bottomRight))
            {
                // Aplicar daño multiplicado
                enemy.TakeDamage(Mathf.RoundToInt(1 * chargedAttackDamageMultiplier));

                // Marcar al enemigo como dañado
                damagedEnemies.Add(enemy);
            }
        }
    }

    private IEnumerator ChargedAttackCooldown()
    {
        yield return new WaitForSeconds(chargedAttackCooldown);
        canChargedAttack = true;
    }
}