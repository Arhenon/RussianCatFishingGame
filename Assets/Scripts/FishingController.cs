using UnityEngine;
using UnityEngine.UI;
using TMPro;
public class FishingController : MonoBehaviour
{
    [Header("Связи")]
    public Transform rodPivot;
    public Transform rodTip;
    public GameObject hookPrefab;
    public Transform playerTransform;
    public CameraFollow cameraScript;

    public Slider powerSlider;
    public Slider tensionSlider;
    public Image tensionFill;
    public TMP_Text moneyText;
    public TMP_Text infoText;

    [Header("Настройки Мира")]
    public float waterLevelY = -1.5f;

    [Header("Настройки Удочки")]
    public float restAngle = 0f;
    public float chargeAngle = 65f;
    public float castForceMultiplier = 18f;
    public float rodReturnSpeed = 5f;

    [Header("Характеристики")]
    public float maxRodLength = 30f;

    // !!! ИЗМЕНЕНИЕ 1: Теперь это просто скорость движения (м/с)
    public float reelMoveSpeed = 5f;    // Скорость подмотки (визуальная)

    public float maxCastPower = 25f;
    public float maxTension = 100f;
    public float tensionRecovery = 15f;
    public float catchDistance = 2f;

    // !!! ИЗМЕНЕНИЕ 2: Множитель урона леске
    public float tensionDamageMultiplier = 5.0f; // Увеличь это, чтобы рвалось быстрее

    // Внутренние
    private GameObject currentHook;
    private Rigidbody2D hookRb;
    private LineRenderer hookLine;
    private Hook hookScript;

    private float currentCastPower = 0f;
    private float currentTension = 0f;
    private int money = 0;
    private bool chargingUp = true;

    private float fixedMaxLength = 0f;   // Стена
    private float ratchetLength = 0f;    // Трещотка

    private bool isInWater = false;

    private enum State { Idle, Charging, Fishing }
    private State state = State.Idle;

    void Start()
    {
        powerSlider.gameObject.SetActive(false);
        tensionSlider.gameObject.SetActive(false);
        infoText.text = "Зажми ПРОБЕЛ";
        if (rodPivot != null) rodPivot.localRotation = Quaternion.Euler(0, 0, restAngle);
    }

    void Update()
    {
        switch (state)
        {
            case State.Idle:
                if (Input.GetKeyDown(KeyCode.Space))
                {
                    state = State.Charging;
                    currentCastPower = 0f;
                    powerSlider.gameObject.SetActive(true);
                    infoText.text = "Сила...";
                    if (cameraScript != null) { cameraScript.target = rodTip; cameraScript.smoothSpeed = 5f; }
                }
                break;

            case State.Charging:
                HandleCharging();
                if (Input.GetKeyUp(KeyCode.Space)) PerformCast();
                break;

            case State.Fishing:
                HandleFishing();
                break;
        }
    }

    void HandleCharging()
    {
        float step = maxCastPower * 1.5f * Time.deltaTime;
        if (chargingUp)
        {
            currentCastPower += step;
            if (currentCastPower >= maxCastPower) { currentCastPower = maxCastPower; chargingUp = false; }
        }
        else
        {
            currentCastPower -= step;
            if (currentCastPower <= 0f) { currentCastPower = 0f; chargingUp = true; }
        }
        powerSlider.value = currentCastPower / maxCastPower;

        float progress = currentCastPower / maxCastPower;
        if (rodPivot != null)
            rodPivot.localRotation = Quaternion.Euler(0, 0, Mathf.Lerp(restAngle, chargeAngle, progress));
    }

    void PerformCast()
    {
        state = State.Fishing;
        powerSlider.gameObject.SetActive(false);
        tensionSlider.gameObject.SetActive(true);
        infoText.text = "ЛКМ - Крутить";
        isInWater = false;

        float powerPercent = currentCastPower / maxCastPower;
        fixedMaxLength = Mathf.Lerp(5f, maxRodLength, powerPercent);
        ratchetLength = fixedMaxLength;

        currentHook = Instantiate(hookPrefab, rodTip.position, Quaternion.identity);
        hookRb = currentHook.GetComponent<Rigidbody2D>();
        hookLine = currentHook.GetComponent<LineRenderer>();
        hookScript = currentHook.GetComponent<Hook>();
        hookLine.positionCount = 2;

        hookRb.linearDamping = 0.1f;
        hookRb.gravityScale = 1.0f;

        if (cameraScript != null) cameraScript.target = currentHook.transform;

        Vector2 throwDir = new Vector2(1f, 0.6f).normalized;
        if (currentCastPower > 1f)
            hookRb.AddForce(throwDir * currentCastPower * castForceMultiplier, ForceMode2D.Impulse);
        else
            hookRb.AddForce(Vector2.right * 2f, ForceMode2D.Impulse);
    }

    void HandleFishing()
    {
        if (rodPivot != null)
            rodPivot.localRotation = Quaternion.Lerp(rodPivot.localRotation, Quaternion.Euler(0, 0, restAngle), Time.deltaTime * rodReturnSpeed);

        if (currentHook == null) return;

        hookLine.SetPosition(0, rodTip.position);
        hookLine.SetPosition(1, currentHook.transform.position);

        Vector2 rodPos = rodTip.position;
        Vector2 hookPos = currentHook.transform.position;
        Vector2 rodToHookVector = hookPos - rodPos;
        float distance = rodToHookVector.magnitude;
        Vector2 dirToRod = -rodToHookVector.normalized;

        bool isReeling = Input.GetMouseButton(0);

        // 1. ВХОД В ВОДУ
        if (hookPos.y <= waterLevelY && !isInWater)
        {
            isInWater = true;
            hookRb.gravityScale = 0.5f;
            hookRb.linearVelocity *= 0.5f;
            if (cameraScript != null) cameraScript.smoothSpeed = 2f;
        }

        // 2. СИЛА РЫБЫ
        if (hookScript.caughtFish != null)
        {
            float fishForce = hookScript.caughtFish.struggleForce;
            hookRb.AddForce(Vector2.down * fishForce, ForceMode2D.Force);
            hookRb.AddForce(Vector2.right * Mathf.Sin(Time.time * 8f) * (fishForce * 0.4f), ForceMode2D.Force);
        }

        // 3. ПОДТЯЖКА (СИСТЕМА "ЛЕБЕДКА")
        if (isReeling && isInWater)
        {
            // !!! ГЛАВНОЕ ИЗМЕНЕНИЕ: Мы не толкаем (Force), мы задаем скорость (Velocity)
            // Это обеспечивает идеально плавное движение без рывков

            // Текущая скорость рыбы/гравитации + Наша скорость подтяжки
            Vector2 targetVelocity = dirToRod * reelMoveSpeed;

            // Lerp для мягкого старта, чтобы не было мгновенного телепорта скорости
            hookRb.linearVelocity = Vector2.Lerp(hookRb.linearVelocity, targetVelocity, Time.deltaTime * 10f);

            // Обновляем трещотку
            if (distance < ratchetLength) ratchetLength = distance;

            // Натяжение от работы мотора
            float mechTension = (hookScript.caughtFish != null) ? 20f : 5f;
            currentTension += mechTension * Time.deltaTime;
        }
        else
        {
            // Если не тянем - включаем Drag, чтобы крючок не падал слишком быстро
            hookRb.linearDamping = isInWater ? 1.0f : 0.1f;

            // Остывание
            if (distance < fixedMaxLength * 0.98f)
            {
                currentTension -= tensionRecovery * Time.deltaTime;
            }
        }

        // 4. ОГРАНИЧЕНИЯ
        float speedOut = Vector2.Dot(hookRb.linearVelocity, rodToHookVector.normalized);

        // А. Трещотка (держит, если нет рыбы)
        if (distance >= ratchetLength && distance < fixedMaxLength)
        {
            if (hookScript.caughtFish == null)
            {
                if (speedOut > 0) hookRb.linearVelocity -= rodToHookVector.normalized * speedOut;

                // Мягко держим позицию
                Vector2 holdPos = rodPos + rodToHookVector.normalized * ratchetLength;
                currentHook.transform.position = Vector3.Lerp(currentHook.transform.position, holdPos, Time.deltaTime * 5f);
            }
        }

        // Б. Стена (Финал)
        if (distance >= fixedMaxLength)
        {
            if (speedOut > 0) hookRb.linearVelocity -= rodToHookVector.normalized * speedOut;

            Vector2 limitPos = rodPos + rodToHookVector.normalized * fixedMaxLength;
            currentHook.transform.position = Vector3.Lerp(currentHook.transform.position, limitPos, Time.deltaTime * 10f);

            // !!! ИЗМЕНЕНИЕ: УРОН ЛЕСКЕ !!!
            if (hookScript.caughtFish != null)
            {
                // Формула: Сила рыбы * МНОЖИТЕЛЬ
                float damage = hookScript.caughtFish.struggleForce * tensionDamageMultiplier;
                currentTension += damage * Time.deltaTime;

                // Визуал
                currentHook.transform.position += (Vector3)Random.insideUnitCircle * 0.03f;
            }
        }

        // 5. МАГНИТ
        if (distance < catchDistance && isReeling)
        {
            currentHook.transform.position = Vector3.Lerp(currentHook.transform.position, rodPos, Time.deltaTime * 10f);
            if (distance < 0.5f)
            {
                if (hookScript.caughtFish != null) SellFish(hookScript.caughtFish);
                else EndFishing("Перезаброс...");
            }
        }

        // 6. UI
        currentTension = Mathf.Clamp(currentTension, 0, maxTension);
        tensionSlider.value = currentTension;
        if (tensionFill != null) tensionFill.color = Color.Lerp(Color.green, Color.red, currentTension / maxTension);

        if (currentTension >= maxTension) { EndFishing("ОБРЫВ! Снасть лопнула!"); return; }
    }

    void SellFish(FishAI fish)
    {
        money += (int)(fish.basePrice * fish.weight);
        moneyText.text = money + " RUB";
        EndFishing($"Пойман {fish.fishName}!");
        Destroy(fish.gameObject);
    }

    void EndFishing(string msg)
    {
        if (currentHook != null) Destroy(currentHook);
        state = State.Idle;
        infoText.text = msg + "\n(Пробел)";
        tensionSlider.gameObject.SetActive(false);
        currentTension = 0;
        if (cameraScript != null && playerTransform != null) cameraScript.target = playerTransform;
        if (rodPivot != null) rodPivot.localRotation = Quaternion.Euler(0, 0, restAngle);
    }
}