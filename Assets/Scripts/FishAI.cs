using UnityEngine;

public class FishAI : MonoBehaviour
{
    [Header("Настройки")]
    public float speed = 2f;
    public float leftBorder = -8f;
    public float rightBorder = 8f;

    [Header("Характеристики")]
    public string fishName = "Карась";
    public float weight = 1.5f;
    public int basePrice = 50;

    // !!! НОВОЕ: Сила, с которой рыба тянет назад
    public float struggleForce = 5f;

    // Внутренние
    private int direction = 1;
    public bool isHooked = false;

    void Start()
    {
        if (transform.position.x > rightBorder) direction = -1;
        UpdateRotation();
    }

    void Update()
    {
        if (isHooked)
        {
            // Если рыба поймана, она хаотично дергается (визуально)
            // Но физическую силу мы будем брать в контроллере
            return;
        }

        // Обычное плавание
        transform.position += Vector3.right * speed * direction * Time.deltaTime;

        if (transform.position.x > rightBorder && direction > 0)
        {
            direction = -1;
            UpdateRotation();
        }
        else if (transform.position.x < leftBorder && direction < 0)
        {
            direction = 1;
            UpdateRotation();
        }
    }

    void UpdateRotation()
    {
        // Поворот спрайта
        float zAngle = (direction == 1) ? 90 : -90;
        transform.rotation = Quaternion.Euler(0, 0, zAngle);
    }
}