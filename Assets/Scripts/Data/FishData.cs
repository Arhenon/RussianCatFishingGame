
using UnityEngine;
[CreateAssetMenu(fileName = "New Fish", menuName = "Fishing/Fish Data")]
public class FishData : ScriptableObject
{
    public enum FishSize { Small, Medium, Large, Giant }

    [Header("Каталог")]
    public string fishName = "Новая Рыба";
    public FishSize sizeClass = FishSize.Small;
    public Sprite fishIcon;
    [TextArea] public string description = "Описание рыбы...";

    [Header("Экономика и Физика")]
    public int price = 50;
    public float weight = 1.5f;
    public float swimSpeed = 2f;      // Ловкость (скорость патруля и рывков)
    public float struggleForce = 15f; // Сила, с которой тянет леску
}