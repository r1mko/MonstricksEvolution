using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class StoreManager : MonoBehaviour
{
    [System.Serializable]
    public class StoreItemData
    {
        public Image iconImage;
        public long priceButton;
    }

    [SerializeField] private List<StoreItemData> storeItems = new List<StoreItemData>();
    [SerializeField] private List<GameObject> itemObjects = new List<GameObject>();

    private long playerMoney = 0;

    [ContextMenu("Initialize Store Items")]
    private void InitializeStoreItems()
    {
        storeItems.Clear();

        foreach (GameObject obj in itemObjects)
        {
            if (obj == null) continue;

            StoreItemData item = new StoreItemData();

            Transform iconTransform = obj.transform.Find("Icon");
            if (iconTransform != null)
            {
                item.iconImage = iconTransform.GetComponent<Image>();
            }

            storeItems.Add(item);
        }
    }
}