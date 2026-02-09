using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform target;       // За кем следить сейчас
    public float smoothSpeed = 5f; // Плавность полета (чем меньше, тем "ленивее" камера)
    public Vector3 offset = new Vector3(0, 0, -10); // Сдвиг (важно для Z = -10)

    void LateUpdate()
    {
        if (target == null) return;

        // 1. Вычисляем, где камера хочет быть
        Vector3 desiredPosition = target.position + offset;

        // 2. Ограничим Y (опционально), чтобы камера не улетала слишком глубоко под землю или высоко в небо
        // Если хочешь видеть дно - убери эту строку. 
        // Но обычно в рыбалке камеру чуть приподнимают над целью.
        desiredPosition.y = Mathf.Clamp(desiredPosition.y, -20f, 10f);

        // 3. Плавно перемещаем камеру (Lerp)
        Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed * Time.deltaTime);
        transform.position = smoothedPosition;
    }
}