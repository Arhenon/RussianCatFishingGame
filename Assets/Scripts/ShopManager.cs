using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections; // Нужно для задержки

public class ShopManager : MonoBehaviour
{
    [Header("База Удочек (Автозагрузка)")]
    public RodData[] allRods;

    [Header("Связи UI")]
    public GameObject shopWindow;
    public Transform scrollContent;
    public GameObject shopRowPrefab;

    [Header("Связи Скриптов")]
    public FishingController fishingController;

    private string equippedRodName = "";

    void Awake()
    {
        // 1. Загрузка данных
        allRods = Resources.LoadAll<RodData>("Rods");

        // 2. Сразу помечаем бесплатные удочки как купленные
        foreach (var rod in allRods)
        {
            if (rod != null && rod.price == 0)
                PlayerPrefs.SetInt("UnlockedRod_" + rod.rodName, 1);
        }
    }

    void Start()
    {
        if (shopWindow) shopWindow.SetActive(false);

        // 3. Загружаем сохраненную удочку
        equippedRodName = PlayerPrefs.GetString("EquippedRod", "");

        // 4. Если в сохранениях пусто - принудительно ищем базовую удочку
        if (string.IsNullOrEmpty(equippedRodName))
        {
            foreach (var r in allRods)
            {
                if (r.price == 0) { equippedRodName = r.rodName; break; }
            }
        }

        // 5. Передаем её в контроллер
        foreach (var r in allRods)
        {
            if (r.rodName == equippedRodName)
            {
                fishingController.EquipRod(r);
                break;
            }
        }
    }

    public void OpenShop()
    {
        shopWindow.SetActive(true);
        RefreshShop();

        // !!! ЛАЙФХАК ДЛЯ СКРОЛЛА !!!
        // Заставляем Unity пересчитать размеры UI через мизерную паузу
        StartCoroutine(UpdateLayoutCoroutine());
    }

    public void CloseShop()
    {
        shopWindow.SetActive(false);
    }

    public void RefreshShop()
    {
        // Очищаем старое
        foreach (Transform child in scrollContent) Destroy(child.gameObject);

        // Создаем новое
        foreach (RodData rod in allRods)
        {
            // !!! ВАЖНО: Создаем как дочерний объект UI !!!
            GameObject newRow = Instantiate(shopRowPrefab, scrollContent);

            // Сброс масштаба и позиции (критично для UI)
            RectTransform rect = newRow.GetComponent<RectTransform>();
            rect.localScale = Vector3.one;
            rect.localPosition = Vector3.zero;

            ShopRowUI rowUI = newRow.GetComponent<ShopRowUI>();

            // Заполнение данных
            rowUI.icon.sprite = (rod.shopIcon != null) ? rod.shopIcon : rod.rodSprite;
            rowUI.nameText.text = rod.rodName;
            rowUI.statsText.text = $"Сила: {rod.rodStrength} | Длина: {rod.maxRodLength}м\nТяга: {rod.fightingReelForce}";

            bool isUnlocked = PlayerPrefs.GetInt("UnlockedRod_" + rod.rodName, 0) == 1;
            bool isEquipped = (rod.rodName == equippedRodName);

            rowUI.actionButton.onClick.RemoveAllListeners();

            if (isEquipped)
            {
                rowUI.priceText.text = "";
                rowUI.buttonText.text = "НАДЕТО";
                rowUI.actionButton.interactable = false;
            }
            else if (isUnlocked)
            {
                rowUI.priceText.text = "";
                rowUI.buttonText.text = "ВЗЯТЬ";
                rowUI.actionButton.interactable = true;
                rowUI.actionButton.onClick.AddListener(() => EquipRod(rod));
            }
            else
            {
                rowUI.priceText.text = rod.price + " RUB";
                rowUI.buttonText.text = "КУПИТЬ";
                rowUI.actionButton.interactable = (fishingController.money >= rod.price);
                rowUI.actionButton.onClick.AddListener(() => BuyRod(rod));
            }
        }
    }

    private void BuyRod(RodData rod)
    {
        if (fishingController.money >= rod.price)
        {
            fishingController.money -= rod.price;
            if (fishingController.moneyText) fishingController.moneyText.text = fishingController.money + " RUB";

            PlayerPrefs.SetInt("UnlockedRod_" + rod.rodName, 1);
            PlayerPrefs.Save();
            EquipRod(rod);
        }
    }

    private void EquipRod(RodData rod)
    {
        equippedRodName = rod.rodName;
        PlayerPrefs.SetString("EquippedRod", rod.rodName);
        PlayerPrefs.Save();

        fishingController.EquipRod(rod);
        RefreshShop(); // Сразу перерисовываем кнопки
    }

    // Корутина для исправления бага скролла
    IEnumerator UpdateLayoutCoroutine()
    {
        yield return new WaitForEndOfFrame();
        // Насильно заставляем Content Size Fitter обновиться
        LayoutRebuilder.ForceRebuildLayoutImmediate(scrollContent.GetComponent<RectTransform>());
    }
}