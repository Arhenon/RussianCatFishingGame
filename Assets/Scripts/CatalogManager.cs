using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CatalogManager : MonoBehaviour
{
    [Header("База данных (АВТОЗАГРУЗКА)")]
    // Тебе больше не нужно заполнять этот массив руками!
    // Я оставил его видимым в инспекторе, просто чтобы ты мог проверять, что рыбы загрузились.
    public FishData[] allFishDatabase;

    [Header("Связи UI")]
    public GameObject catalogWindow;
    public Transform scrollContent;
    public GameObject fishRowPrefab;
    public TMP_Text completionText;

    private FishData.FishSize currentTab = FishData.FishSize.Small;

    void Awake()
    {
        // !!! АВТОМАТИЗАЦИЯ !!!
        // Unity заходит в папку Resources/FishSettings/Рыбы и ищет все файлы типа FishData
        // Она автоматически просканирует все вложенные папки (Маленькие, Средние и т.д.)
        allFishDatabase = Resources.LoadAll<FishData>("FishSettings/Рыбы");

        Debug.Log("Каталог загружен! Найдено рыб в базе: " + allFishDatabase.Length);
    }

    void Start()
    {
        if (catalogWindow) catalogWindow.SetActive(false);
    }

    public void UnlockFish(string caughtFishName)
    {
        PlayerPrefs.SetInt("Unlocked_" + caughtFishName, 1);
        PlayerPrefs.Save();
        Debug.Log("Разблокировано: " + caughtFishName);
    }

    public void OpenCatalog()
    {
        catalogWindow.SetActive(true);
        OpenTab((int)FishData.FishSize.Small);
        UpdateCompletion();
    }

    public void CloseCatalog()
    {
        catalogWindow.SetActive(false);
    }

    public void OpenTab(int sizeIndex)
    {
        currentTab = (FishData.FishSize)sizeIndex;
        RefreshScrollList();
    }

    private void RefreshScrollList()
    {
        foreach (Transform child in scrollContent) Destroy(child.gameObject);

        foreach (FishData fish in allFishDatabase)
        {
            if (fish.sizeClass == currentTab)
            {
                GameObject newRow = Instantiate(fishRowPrefab, scrollContent);
                CatalogRowUI rowUI = newRow.GetComponent<CatalogRowUI>();

                bool isUnlocked = PlayerPrefs.GetInt("Unlocked_" + fish.fishName, 0) == 1;

                if (isUnlocked)
                {
                    rowUI.fishIcon.sprite = fish.fishIcon;
                    rowUI.fishIcon.color = Color.white;
                    rowUI.nameText.text = fish.fishName;
                    rowUI.priceText.text = fish.price + " RUB";
                    rowUI.descText.text = fish.description;
                }
                else
                {
                    if (fish.fishIcon != null)
                    {
                        rowUI.fishIcon.sprite = fish.fishIcon;
                        rowUI.fishIcon.color = Color.black;
                    }
                    else
                    {
                        rowUI.fishIcon.color = new Color(0, 0, 0, 0);
                    }

                    rowUI.nameText.text = "???";
                    rowUI.priceText.text = "??? RUB";
                    rowUI.descText.text = "Поймай меня, если сможешь!";
                }
            }
        }
    }

    private void UpdateCompletion()
    {
        if (allFishDatabase.Length == 0) return;

        int unlockedCount = 0;
        foreach (FishData fish in allFishDatabase)
        {
            if (PlayerPrefs.GetInt("Unlocked_" + fish.fishName, 0) == 1) unlockedCount++;
        }

        float percentage = ((float)unlockedCount / allFishDatabase.Length) * 100f;
        if (completionText) completionText.text = $"Завершено: {Mathf.RoundToInt(percentage)}%";
    }
}