using UnityEngine;
[CreateAssetMenu(fileName = "New Rod", menuName = "Fishing/Rod Data")]
public class RodData : ScriptableObject
{
    [Header("Магазин")]
    public string rodName = "Новая удочка";
    public int price = 1000;
    public Sprite shopIcon;   
    public Sprite rodSprite;  
    [TextArea] public string description = "Описание удочки";

    [Header("Характеристики Физики")]
    [Tooltip("Прочность (делит урон от рыбы)")]
    public float rodStrength = 1.0f;

    [Tooltip("Макс. длина лески (глубина)")]
    public float maxRodLength = 30f; [Tooltip("Сила тяги катушки (перетягивание рыбы)")]
    public float fightingReelForce = 40f; [Tooltip("Макс. сила заброса")]
    public float maxCastPower = 25f;
}