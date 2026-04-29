using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class FishingController : MonoBehaviour
{
    [Header("Связи")]
    public CatalogManager catalog;
    public Transform rodPivot;
    public Transform rodTip;
    public GameObject hookPrefab;
    public Transform playerTransform;
    public CameraFollow cameraScript; [Header("Текущая Экипировка")]
    public RodData currentRod; // Файл с настройками удочки
    public SpriteRenderer rodVisualRenderer; // Картинка удочки в руках

    public Slider powerSlider;
    public Slider tensionSlider;
    public Image tensionFill;
    public TMP_Text moneyText;
    public TMP_Text infoText;

    [Header("Глобальные Настройки (Общие для всех удочек)")]
    public float restAngle = 0f;
    public float chargeAngle = 65f;
    public float castForceMultiplier = 5f;
    public float rodReturnSpeed = 5f;
    public float emptyReelSpeed = 10f; // Скорость сматывания пустого крючка
    public float hookWaterGravity = 0.8f;
    public float maxTension = 100f;    // Предел прочности лески всегда 100%
    public float tensionRecovery = 40f;
    public float catchDistance = 1.5f;



    private GameObject currentHook;
    private Rigidbody2D hookRb;
    private LineRenderer hookLine;
    private Hook hookScript;

    private float currentCastPower = 0f;
    private float currentTension = 0f;

    public int money = 0;
    private bool chargingUp = true;

    private float currentReeledLength = 0f;
    private bool hasSplashed = false;

    private Bounds waterBounds;
    private bool hasWaterBounds = false;
    private float waterLevelY = -1.5f;

    private enum State { Idle, Charging, Fishing }
    private State state = State.Idle;

    void Start()
    {
        if (powerSlider) powerSlider.gameObject.SetActive(false);
        if (tensionSlider) tensionSlider.gameObject.SetActive(false);
        if (infoText) infoText.text = "Зажми ПРОБЕЛ";
        if (rodPivot != null) rodPivot.localRotation = Quaternion.Euler(0, 0, restAngle);

        // Надеваем удочку при старте
        if (currentRod != null) EquipRod(currentRod);
        if (cameraScript != null && playerTransform != null)
        {
            cameraScript.target = playerTransform;
        }
        else Debug.LogError("ОШИБКА: В GameManager не вставлен файл удочки (Current Rod)!");

        GameObject waterObj = GameObject.Find("Water");
        if (waterObj != null)
        {
            waterBounds = waterObj.GetComponent<SpriteRenderer>().bounds;
            hasWaterBounds = true;
            waterLevelY = waterBounds.max.y;
        }
    }

    void Update()
    {
        // Защита от зависаний, если удочка не надета
        if (currentRod == null) return;

        switch (state)
        {
            case State.Idle:
                if (Input.GetKeyDown(KeyCode.Space))
                {
                    state = State.Charging;
                    currentCastPower = 0f;
                    if (powerSlider) powerSlider.gameObject.SetActive(true);
                    if (infoText) infoText.text = "Сила...";
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

    // Метод для смены удочки в магазине
    public void EquipRod(RodData newRod)
    {
        currentRod = newRod;
        // Проверяем, что ссылка на картинку в руках существует
        if (rodVisualRenderer != null && newRod.rodSprite != null)
        {
            rodVisualRenderer.sprite = newRod.rodSprite;
            Debug.Log("Спрайт успешно заменен на: " + newRod.rodName);
        }
        else
        {
            Debug.LogWarning("Не удалось заменить спрайт! Проверь ссылки в Инспекторе.");
        }
    }

    void HandleCharging()
    {
        // Берем maxCastPower из файла currentRod
        float step = currentRod.maxCastPower * 1.5f * Time.deltaTime;
        if (chargingUp)
        {
            currentCastPower += step;
            if (currentCastPower >= currentRod.maxCastPower) { currentCastPower = currentRod.maxCastPower; chargingUp = false; }
        }
        else
        {
            currentCastPower -= step;
            if (currentCastPower <= 0f) { currentCastPower = 0f; chargingUp = true; }
        }
        if (powerSlider) powerSlider.value = currentCastPower / currentRod.maxCastPower;

        float progress = currentCastPower / currentRod.maxCastPower;
        if (rodPivot != null) rodPivot.localRotation = Quaternion.Euler(0, 0, Mathf.Lerp(restAngle, chargeAngle, progress));
    }

    void PerformCast()
    {
        state = State.Fishing;
        if (powerSlider) powerSlider.gameObject.SetActive(false);
        if (tensionSlider) tensionSlider.gameObject.SetActive(true);
        if (infoText) infoText.text = "ЛКМ - Крутить";
        hasSplashed = false;

        // Берем длину лески из файла currentRod
        float powerPercent = currentCastPower / currentRod.maxCastPower;
        currentReeledLength = Mathf.Lerp(5f, currentRod.maxRodLength, powerPercent);

        currentHook = Instantiate(hookPrefab, rodTip.position, Quaternion.identity);
        hookRb = currentHook.GetComponent<Rigidbody2D>();
        hookLine = currentHook.GetComponent<LineRenderer>();
        hookScript = currentHook.GetComponent<Hook>();
        hookLine.positionCount = 2;

        hookRb.linearDamping = 0.1f;
        hookRb.gravityScale = 1.0f;

        if (cameraScript != null) cameraScript.target = currentHook.transform;

        Vector2 throwDir = new Vector2(1f, 0.5f).normalized;
        if (currentCastPower > 1f) hookRb.AddForce(throwDir * currentCastPower * castForceMultiplier, ForceMode2D.Impulse);
        else hookRb.AddForce(Vector2.right * 2f, ForceMode2D.Impulse);
    }

    void HandleFishing()
    {
        if (rodPivot != null) rodPivot.localRotation = Quaternion.Lerp(rodPivot.localRotation, Quaternion.Euler(0, 0, restAngle), Time.deltaTime * rodReturnSpeed);
        if (currentHook == null) return;

        Vector3 currentPos = currentHook.transform.position;
        bool isReeling = Input.GetMouseButton(0);
        bool currentlyInWater = currentPos.y <= waterLevelY;

        if (hasWaterBounds && currentlyInWater)
        {
            bool hitBound = false;
            float currentBottomOffset = (hookScript.caughtFish != null) ? hookScript.caughtFish.bottomWaterOffset : 0.3f;

            if (currentPos.y < waterBounds.min.y + currentBottomOffset) { currentPos.y = waterBounds.min.y + currentBottomOffset; if (hookRb.linearVelocity.y < 0) hookRb.linearVelocity = new Vector2(hookRb.linearVelocity.x, 0); hitBound = true; }
            if (currentPos.x < waterBounds.min.x + 0.3f) { currentPos.x = waterBounds.min.x + 0.3f; if (hookRb.linearVelocity.x < 0) hookRb.linearVelocity = new Vector2(0, hookRb.linearVelocity.y); hitBound = true; }
            if (currentPos.x > waterBounds.max.x - 0.3f) { currentPos.x = waterBounds.max.x - 0.3f; if (hookRb.linearVelocity.x > 0) hookRb.linearVelocity = new Vector2(0, hookRb.linearVelocity.y); hitBound = true; }
            if (hitBound) currentHook.transform.position = currentPos;
        }

        hookLine.SetPosition(0, rodTip.position);
        hookLine.SetPosition(1, currentHook.transform.position);

        Vector2 rodPos = rodTip.position;
        Vector2 hookPos = currentHook.transform.position;
        Vector2 rodToHookVector = hookPos - rodPos;
        float distance = rodToHookVector.magnitude;
        Vector2 dirToRod = -rodToHookVector.normalized;


        if (currentlyInWater && !hasSplashed)
        {
            hasSplashed = true;
            hookRb.linearVelocity *= 0.3f;
            if (cameraScript != null) cameraScript.smoothSpeed = 2f;
        }

        if (currentlyInWater) { hookRb.gravityScale = hookWaterGravity; hookRb.linearDamping = isReeling ? 4f : 1.0f; }
        else { hookRb.gravityScale = 1.0f; hookRb.linearDamping = 0.5f; }

        if (hookScript.caughtFish != null && currentlyInWater)
        {
            hookRb.AddForce(hookScript.caughtFish.GetCurrentStruggleForce(), ForceMode2D.Force);
        }

        if (isReeling)
        {
            if (hookScript.caughtFish == null)
            {
                hookRb.linearDamping = 5f;
                currentReeledLength -= emptyReelSpeed * Time.deltaTime;
                if (currentReeledLength < 0.5f) currentReeledLength = 0.5f;

                if (distance > currentReeledLength)
                {
                    currentHook.transform.position = Vector3.Lerp(currentHook.transform.position, rodPos + rodToHookVector.normalized * currentReeledLength, Time.deltaTime * 15f);
                    float spd = Vector2.Dot(hookRb.linearVelocity, rodToHookVector.normalized);
                    if (spd > 0) hookRb.linearVelocity -= rodToHookVector.normalized * spd;
                }
            }
            else
            {
                // Берем силу тяги удочки из файла currentRod!
                float finalReelForce = currentRod.fightingReelForce * currentRod.rodStrength;
                hookRb.AddForce(dirToRod * finalReelForce, ForceMode2D.Force);

                if (distance < currentReeledLength) currentReeledLength = distance;

                // Берем прочность удочки из файла currentRod!
                float tensionDamage = (hookScript.caughtFish.data.struggleForce / currentRod.rodStrength);
                currentTension += tensionDamage * Time.deltaTime;
            }
        }
        else
        {
            if (distance > currentReeledLength) currentReeledLength = distance;
            currentTension -= tensionRecovery * Time.deltaTime;
        }

        float maxAllowedSpeed = (hookScript.caughtFish != null) ? 6f : 15f;
        if (hookRb.linearVelocity.magnitude > maxAllowedSpeed)
        {
            hookRb.linearVelocity = hookRb.linearVelocity.normalized * maxAllowedSpeed;
        }

        if (distance >= currentReeledLength && hookScript.caughtFish == null && currentlyInWater)
        {
            float speedOut = Vector2.Dot(hookRb.linearVelocity, rodToHookVector.normalized);
            if (speedOut > 0) hookRb.linearVelocity -= rodToHookVector.normalized * speedOut;
            currentHook.transform.position = rodPos + rodToHookVector.normalized * currentReeledLength;
        }

        // МАГНИТ
        if (distance < catchDistance && isReeling)
        {
            currentHook.transform.position = Vector3.Lerp(currentHook.transform.position, rodPos, Time.deltaTime * 10f);
            if (distance < 0.5f)
            {
                if (hookScript.caughtFish != null) SellFish(hookScript.caughtFish);
                else EndFishing("Перезаброс...");
            }
        }

        currentTension = Mathf.Clamp(currentTension, 0, maxTension);
        if (tensionSlider) tensionSlider.value = currentTension;
        if (tensionFill != null) tensionFill.color = Color.Lerp(Color.green, Color.red, currentTension / maxTension);

        if (currentTension >= maxTension) { EndFishing("ОБРЫВ! Перегрев снасти!"); return; }
    }

    void SellFish(FishAI fish)
    {
        money += fish.data.price;
        if (moneyText) moneyText.text = money + " RUB";

        if (catalog != null) catalog.UnlockFish(fish.data.fishName);

        EndFishing($"Пойман {fish.data.fishName}!");
        Destroy(fish.gameObject);
    }

    void EndFishing(string msg) 
    { 
        if (currentHook != null) Destroy(currentHook); 
        state = State.Idle; if (infoText) infoText.text = msg + "\n(Пробел)"; 
        if (tensionSlider) tensionSlider.gameObject.SetActive(false); 
        currentTension = 0; if (cameraScript != null && playerTransform != null) cameraScript.target = playerTransform; 
    }
}