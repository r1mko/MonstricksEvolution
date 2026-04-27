using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class StoreManager : MonoBehaviour
{
    public enum UpgradeType
    {
        Click,
        Auto
    }

    [System.Serializable]
    public class StoreItemData
    {
        public Image iconImage;
        public Button buyButton;
        public long cost;
        public long bonusValue;
        public UpgradeType upgradeType;
    }

    [SerializeField] private List<StoreItemData> storeItems = new List<StoreItemData>();
    [SerializeField] private List<GameObject> itemObjects = new List<GameObject>();

    [SerializeField] private GameManager gameManager;

    private void Start()
    {
        if (gameManager == null)
        {
            gameManager = FindObjectOfType<GameManager>();
        }

        foreach (var item in storeItems)
        {
            if (item.buyButton != null)
            {
                StoreItemData currentItem = item;
                item.buyButton.onClick.AddListener(() => TryBuyUpgrade(currentItem));
            }
        }
    }

    private void TryBuyUpgrade(StoreItemData item)
    {
        Debug.Log($"[StoreManager] Button Pressed. Type: {item.upgradeType}, Cost: {item.cost}, Bonus: {item.bonusValue}");

        if (gameManager == null)
        {
            Debug.LogError("[StoreManager] GameManager reference is missing!");
            return;
        }

        long currentMoney = gameManager.GetMoney();
        Debug.Log($"[StoreManager] Current Money: {currentMoney}. Required: {item.cost}");

        if (currentMoney >= item.cost)
        {
            Debug.Log("[StoreManager] Purchase Successful!");
            gameManager.AddMoney(-item.cost);

            if (item.upgradeType == UpgradeType.Click)
            {
                gameManager.AddClickPower(item.bonusValue);
            }
            else if (item.upgradeType == UpgradeType.Auto)
            {
                Debug.Log("[StoreManager] Auto upgrade purchased (logic not implemented yet).");
            }
        }
        else
        {
            Debug.LogWarning("[StoreManager] Not enough money!");
        }
    }

    [ContextMenu("Initialize Store Items")]
    private void InitializeStoreItems()
    {
        Dictionary<int, long> savedCosts = new Dictionary<int, long>();
        Dictionary<int, long> savedBonuses = new Dictionary<int, long>();

        for (int i = 0; i < storeItems.Count; i++)
        {
            if (i < itemObjects.Count && itemObjects[i] != null)
            {
                savedCosts[i] = storeItems[i].cost;
                savedBonuses[i] = storeItems[i].bonusValue;
            }
        }

        storeItems.Clear();

        for (int i = 0; i < itemObjects.Count; i++)
        {
            GameObject obj = itemObjects[i];
            if (obj == null) continue;

            StoreItemData item = new StoreItemData();

            Transform iconTransform = obj.transform.Find("Icon");
            if (iconTransform != null)
            {
                item.iconImage = iconTransform.GetComponent<Image>();
            }

            Transform buttonTransform = obj.transform.Find("BuyButton");
            if (buttonTransform != null)
            {
                item.buyButton = buttonTransform.GetComponent<Button>();
            }

            if (i % 2 == 0)
            {
                item.upgradeType = UpgradeType.Auto;
            }
            else
            {
                item.upgradeType = UpgradeType.Click;
            }

            item.cost = savedCosts.ContainsKey(i) ? savedCosts[i] : 0;
            item.bonusValue = savedBonuses.ContainsKey(i) ? savedBonuses[i] : 0;

            storeItems.Add(item);
        }

        Debug.Log($"[StoreManager] Initialized {storeItems.Count} items.");
    }
}