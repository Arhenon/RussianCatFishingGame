using UnityEngine;

public class FishAI : MonoBehaviour
{
    [Header("Данные Рыбы (Scriptable Object)")]
    public FishData data; // !!! ТЕПЕРЬ ВСЕ ДАННЫЕ БЕРУТСЯ ОТСЮДА !!![Header("Отступы от краев (Среда)")]
    public float topWaterOffset = 1.0f;
    public float bottomWaterOffset = 1.5f;

    [Header("Агро")]
    public float aggroRadius = 3f;
    public float biteSpeed = 4f;

    [Header("Поведение")]
    public FishBehavior behaviorType = FishBehavior.Patrol;
    public float patrolDistance = 5f;
    public float circleRadius = 2f;
    public bool spriteFacesRight = true;
    public float rotationSmoothness = 10f;

    public StruggleBehavior struggleType = StruggleBehavior.DiveToBottom;
    public float sweepDuration = 3f;
    public float struggleSpeedMultiplier = 1.5f;

    public enum FishBehavior { Stationary, Patrol, Circle, RandomWander }
    public enum StruggleBehavior { Circles, DiveToBottom, DashAndRest, LongSweeps }

    private Vector3 startPos;
    private float angle;
    private Vector3 targetWanderPos;
    private float wanderTimer;
    private Rigidbody2D rb;

    public bool isHooked = false;
    private Transform baitTarget;
    private bool isReturningHome = false;

    private Vector2 currentStruggleDir;
    private float currentStruggleMultiplier = 1f;
    private float struggleTimer;
    private int strugglePhase = 0;

    private Bounds waterBounds;
    private bool hasWaterBounds = false;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        startPos = transform.position;
        rb.gravityScale = 0f;
        rb.linearDamping = 2f;
        angle = Random.Range(0f, 360f);
        currentStruggleDir = Vector2.down;

        // Если забыл прикрепить данные, предупредим
        if (data == null) Debug.LogError("На рыбе не висит FishData!");

        GameObject waterObj = GameObject.Find("Water");
        if (waterObj != null)
        {
            waterBounds = waterObj.GetComponent<SpriteRenderer>().bounds;
            hasWaterBounds = true;
        }
    }

    void Update()
    {
        if (data == null) return; // Защита от ошибок

        bool isAboveWater = hasWaterBounds && transform.position.y > waterBounds.max.y;

        if (isHooked)
        {
            if (isAboveWater)
            {
                float baseAngle = spriteFacesRight ? 90f : 0f;
                float flopAngle = baseAngle + Mathf.Sin(Time.time * 25f) * 15f;
                transform.localRotation = Quaternion.Euler(0, 0, flopAngle);
                currentStruggleMultiplier = 0f;
            }
            else
            {
                HandleStrugglePatterns();
                SmoothLookAt(currentStruggleDir);
            }
            return;
        }

        if (isAboveWater) { rb.gravityScale = 1f; return; }
        else rb.gravityScale = 0f;

        if (baitTarget != null)
        {
            isReturningHome = true;
            MoveTowardsBait();
        }
        else if (isReturningHome)
        {
            MoveTo(startPos, data.swimSpeed); // Используем data.swimSpeed
            if (Vector2.Distance(transform.position, startPos) < 0.5f) isReturningHome = false;
        }
        else
        {
            HandleIdleBehavior();
        }

        if (rb.linearVelocity.magnitude > 0.1f) SmoothLookAt(rb.linearVelocity);

        if (hasWaterBounds && !isAboveWater)
        {
            Vector3 pos = transform.position;
            bool hitWall = false;

            float minX = waterBounds.min.x + 0.5f;
            float maxX = waterBounds.max.x - 0.5f;
            float minYLimit = waterBounds.min.y + bottomWaterOffset;
            float maxYLimit = waterBounds.max.y - topWaterOffset;

            if (pos.x < minX) { pos.x = minX; hitWall = true; rb.linearVelocity = new Vector2(Mathf.Abs(rb.linearVelocity.x), rb.linearVelocity.y); }
            if (pos.x > maxX) { pos.x = maxX; hitWall = true; rb.linearVelocity = new Vector2(-Mathf.Abs(rb.linearVelocity.x), rb.linearVelocity.y); }
            if (pos.y < minYLimit) { pos.y = minYLimit; hitWall = true; rb.linearVelocity = new Vector2(rb.linearVelocity.x, Mathf.Abs(rb.linearVelocity.y)); }
            if (pos.y > maxYLimit) { pos.y = maxYLimit; hitWall = true; rb.linearVelocity = new Vector2(rb.linearVelocity.x, -Mathf.Abs(rb.linearVelocity.y)); }

            if (hitWall) transform.position = pos;
        }
    }

    void HandleStrugglePatterns()
    {
        float panicSpeed = data.swimSpeed * struggleSpeedMultiplier; // Используем data.swimSpeed

        switch (struggleType)
        {
            case StruggleBehavior.Circles:
                currentStruggleMultiplier = 1f;
                angle += panicSpeed * Time.deltaTime;
                currentStruggleDir = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)).normalized;
                currentStruggleDir.y -= 0.3f; currentStruggleDir.Normalize();
                break;
            case StruggleBehavior.DiveToBottom:
                currentStruggleMultiplier = 1f;
                float wiggle = Mathf.Sin(Time.time * panicSpeed * 5f) * 0.5f;
                currentStruggleDir = new Vector2(wiggle, -1f).normalized;
                break;
            case StruggleBehavior.DashAndRest:
                struggleTimer -= Time.deltaTime;
                if (strugglePhase == 0)
                {
                    currentStruggleMultiplier = 1.5f;
                    if (struggleTimer <= 0) { strugglePhase = 1; struggleTimer = 1f; currentStruggleMultiplier = 0f; }
                }
                else
                {
                    currentStruggleMultiplier = 0f;
                    if (struggleTimer <= 0) { strugglePhase = 0; struggleTimer = 2f; currentStruggleDir = new Vector2(Random.Range(-1f, 1f), Random.Range(-1f, 0f)).normalized; }
                }
                break;
            case StruggleBehavior.LongSweeps:
                struggleTimer -= Time.deltaTime;
                currentStruggleMultiplier = 1f;
                if (struggleTimer <= 0) { strugglePhase = 1 - strugglePhase; struggleTimer = sweepDuration; }
                if (strugglePhase == 0) currentStruggleDir = new Vector2(1f, -0.3f).normalized;
                else currentStruggleDir = new Vector2(-1f, -0.3f).normalized;
                break;
        }
    }

    public Vector2 GetCurrentStruggleForce() { return currentStruggleDir * (data.struggleForce * currentStruggleMultiplier); } // Используем data.struggleForce

    void SmoothLookAt(Vector2 direction)
    {
        if (direction.sqrMagnitude < 0.01f) return;
        float targetAngle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        if (!spriteFacesRight) targetAngle -= 90f;
        Quaternion targetRotation = Quaternion.AngleAxis(targetAngle, Vector3.forward);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * rotationSmoothness);
    }

    void HandleIdleBehavior()
    {
        switch (behaviorType)
        {
            case FishBehavior.Stationary: rb.linearVelocity = Vector2.Lerp(rb.linearVelocity, Vector2.zero, Time.deltaTime); break;
            case FishBehavior.Patrol: float xOffset = Mathf.Sin(Time.time * (data.swimSpeed / 2)) * patrolDistance; MoveTo(startPos + new Vector3(xOffset, 0, 0), data.swimSpeed); break;
            case FishBehavior.Circle: angle += data.swimSpeed * Time.deltaTime; MoveTo(new Vector3(startPos.x + Mathf.Cos(angle) * circleRadius, startPos.y + Mathf.Sin(angle) * circleRadius, 0), data.swimSpeed); break;
            case FishBehavior.RandomWander: wanderTimer -= Time.deltaTime; if (wanderTimer <= 0) { targetWanderPos = startPos + (Vector3)(Random.insideUnitCircle * patrolDistance); if (hasWaterBounds) { targetWanderPos.x = Mathf.Clamp(targetWanderPos.x, waterBounds.min.x + 0.5f, waterBounds.max.x - 0.5f); targetWanderPos.y = Mathf.Clamp(targetWanderPos.y, waterBounds.min.y + bottomWaterOffset, waterBounds.max.y - topWaterOffset); } wanderTimer = Random.Range(2f, 5f); } MoveTo(targetWanderPos, data.swimSpeed); break;
        }
    }

    public void CheckForBait(Transform hookTransform)
    {
        if (isHooked) return;
        if (hasWaterBounds && hookTransform.position.y > waterBounds.max.y) { baitTarget = null; return; }
        float dist = Vector2.Distance(transform.position, hookTransform.position);
        if (dist < aggroRadius) baitTarget = hookTransform; else baitTarget = null;
    }

    public void ClearBait() { baitTarget = null; }
    void MoveTowardsBait() { if (baitTarget != null) MoveTo(baitTarget.position, biteSpeed); }
    void MoveTo(Vector3 target, float speed) { Vector2 dir = (target - transform.position).normalized; rb.linearVelocity = dir * speed; }
}