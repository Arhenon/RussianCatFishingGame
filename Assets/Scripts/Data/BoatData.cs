using UnityEngine;
[CreateAssetMenu(fileName = "New Boat", menuName = "Fishing/Boat Data")]
public class BoatData : ScriptableObject
{
    public string boatName = "Новая лодка";
    public int price = 1000;

    public Sprite shopIcon;
    public Sprite boatSprite; [Tooltip("На сколько метров можно уплыть от берега")]
    public float maxDistance = 20f; [Tooltip("Скорость плавания этой лодки")]
    public float boatSpeed = 5f; 
}