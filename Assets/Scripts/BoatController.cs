using UnityEngine;

public class BoatController : MonoBehaviour
{
    [Header("Связи")]
    public SpriteRenderer boatRenderer;

    private BoatData currentBoat;
    private float startX;

    void Awake()
    {
        // Запоминаем позицию до того, как заработает магазин
        startX = transform.position.x;
    }

    void Start()
    {
        // Прячем картинку лодки, ТОЛЬКО если магазин её еще не надел!
        if (currentBoat == null && boatRenderer != null)
        {
            boatRenderer.color = new Color(1, 1, 1, 0);
        }
    }

    void Update()
    {
        if (currentBoat == null || currentBoat.maxDistance <= 0) return;

        float moveInput = Input.GetAxis("Horizontal");

        if (Mathf.Abs(moveInput) > 0.01f)
        {
            Vector3 pos = transform.position;
            pos.x += moveInput * currentBoat.boatSpeed * Time.deltaTime;
            pos.x = Mathf.Clamp(pos.x, startX, startX + currentBoat.maxDistance);
            transform.position = pos;
        }
    }

    public void EquipBoat(BoatData boat)
    {
        currentBoat = boat;

        if (boatRenderer != null && boat.boatSprite != null)
        {
            boatRenderer.sprite = boat.boatSprite;
            boatRenderer.color = new Color(1, 1, 1, 1); // Делаем видимой
        }

        Debug.Log("Лодка надета: " + boat.boatName);
    }
}