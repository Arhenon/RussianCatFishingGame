using UnityEngine;

public class Hook : MonoBehaviour
{
    public FishAI caughtFish = null;

    void OnTriggerEnter2D(Collider2D other)
    {
        if (caughtFish != null) return; // Уже занято

        FishAI fish = other.GetComponent<FishAI>();
        if (fish != null)
        {
            AttachFish(fish);
        }
    }

    void AttachFish(FishAI fish)
    {
        caughtFish = fish;
        fish.isHooked = true;

        // 1. Отключаем физику рыбы, чтобы она не спорила с крючком
        Rigidbody2D fishRb = fish.GetComponent<Rigidbody2D>();
        if (fishRb != null)
        {
            fishRb.simulated = false; // Рыба перестает быть физическим объектом
        }

        // 2. Отключаем коллайдер, чтобы не цеплять дно
        Collider2D fishCol = fish.GetComponent<Collider2D>();
        if (fishCol != null)
        {
            fishCol.enabled = false;
        }

        // 3. Жесткая привязка к крючку
        fish.transform.SetParent(this.transform);
        fish.transform.localPosition = Vector3.zero; // Сдвигаем ровно в центр крючка

        // 4. Поворот головой вверх (визуально)
        fish.transform.localRotation = Quaternion.Euler(0, 0, 90);
    }
}