using UnityEngine;
using TMPro;

public class BoatShopManager : MonoBehaviour
{
    [Header("База Лодок (Автозагрузка)")]
    public BoatData[] allBoats;

    [Header("Связи UI")]
    public GameObject boatShopWindow;
    public Transform gridContent;
    public GameObject boatRowPrefab; [Header("Связи Скриптов")]
    public FishingController fishingController;
    public BoatController boatController;

    private string equippedBoatName = "";

    void Awake()
    {
        allBoats = Resources.LoadAll<BoatData>("Boats");
        foreach (var boat in allBoats)
        {
            if (boat.price == 0) PlayerPrefs.SetInt("UnlockedBoat_" + boat.boatName, 1);
        }
    }

    void Start()
    {
        if (boatShopWindow) boatShopWindow.SetActive(false);

        equippedBoatName = PlayerPrefs.GetString("EquippedBoat", "");

        // Пытаемся найти сохраненную лодку. Если её нет - ничего не надеваем! Кот остается пешком.
        if (!string.IsNullOrEmpty(equippedBoatName))
        {
            foreach (var boat in allBoats)
            {
                if (boat.boatName == equippedBoatName)
                {
                    boatController.EquipBoat(boat);
                    break;
                }
            }
        }
    }

    public void OpenBoatShop()
    {
        boatShopWindow.SetActive(true);
        RefreshShop();
    }

    public void CloseBoatShop()
    {
        boatShopWindow.SetActive(false);
    }

    private void RefreshShop()
    {
        foreach (Transform child in gridContent) Destroy(child.gameObject);

        foreach (BoatData boat in allBoats)
        {
            GameObject newRow = Instantiate(boatRowPrefab, gridContent);
            newRow.transform.localScale = Vector3.one;

            ShopRowUI rowUI = newRow.GetComponent<ShopRowUI>();

            rowUI.icon.sprite = boat.shopIcon != null ? boat.shopIcon : boat.boatSprite;
            rowUI.nameText.text = boat.boatName;

            // Показываем скорость и дальность
            if (rowUI.statsText != null)
                rowUI.statsText.text = $"Дальность: {boat.maxDistance}м\nСкорость: {boat.boatSpeed}";

            bool isUnlocked = PlayerPrefs.GetInt("UnlockedBoat_" + boat.boatName, 0) == 1;
            bool isEquipped = (boat.boatName == equippedBoatName);

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
                rowUI.buttonText.text = "СЕСТЬ";
                rowUI.actionButton.interactable = true;
                rowUI.actionButton.onClick.AddListener(() => EquipBoat(boat));
            }
            else
            {
                rowUI.priceText.text = boat.price + " RUB";
                rowUI.buttonText.text = "КУПИТЬ";
                rowUI.actionButton.interactable = (fishingController.money >= boat.price);
                rowUI.actionButton.onClick.AddListener(() => BuyBoat(boat));
            }
        }
    }

    private void BuyBoat(BoatData boat)
    {
        if (fishingController.money >= boat.price)
        {
            fishingController.money -= boat.price;
            if (fishingController.moneyText) fishingController.moneyText.text = fishingController.money + " RUB";

            PlayerPrefs.SetInt("UnlockedBoat_" + boat.boatName, 1);
            PlayerPrefs.Save();
            EquipBoat(boat);
        }
    }

    private void EquipBoat(BoatData boat)
    {
        equippedBoatName = boat.boatName;
        PlayerPrefs.SetString("EquippedBoat", boat.boatName);
        PlayerPrefs.Save();

        boatController.EquipBoat(boat);
        RefreshShop();
    }
}