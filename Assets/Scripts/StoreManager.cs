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
        public TMP_Text nameText;
        public TMP_Text descriptionText;
        public Button buyButton;
        public long unlockCost;

        private string originalName;
        private string originalDescription;

        public void SaveOriginalData()
        {
            if (nameText != null) originalName = nameText.text;
            if (descriptionText != null) originalDescription = descriptionText.text;

            HideContent();
        }

        public void HideContent()
        {
            if (iconImage != null) iconImage.enabled = false;
            if (nameText != null) nameText.text = "???";
            if (descriptionText != null) descriptionText.text = "???";
            if (buyButton != null) buyButton.interactable = false;
        }

        public void ShowContent()
        {
            if (iconImage != null) iconImage.enabled = true;
            if (nameText != null && !string.IsNullOrEmpty(originalName)) nameText.text = originalName;
            if (descriptionText != null && !string.IsNullOrEmpty(originalDescription)) descriptionText.text = originalDescription;
            if (buyButton != null) buyButton.interactable = true;
        }
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

            Transform nameTransform = obj.transform.Find("NameText");
            if (nameTransform != null)
            {
                item.nameText = nameTransform.GetComponent<TMP_Text>();
            }

            Transform descTransform = obj.transform.Find("DescriptionText");
            if (descTransform != null)
            {
                item.descriptionText = descTransform.GetComponent<TMP_Text>();
            }

            Transform buttonTransform = obj.transform.Find("BuyButton");
            if (buttonTransform != null)
            {
                item.buyButton = buttonTransform.GetComponent<Button>();
            }

            storeItems.Add(item);
        }

        SaveAllOriginalData();
    }

    private void SaveAllOriginalData()
    {
        for (int i = 0; i < storeItems.Count; i++)
        {
            storeItems[i].SaveOriginalData();
        }
    }

    public void UpdateItemVisuals()
    {
        for (int i = 0; i < storeItems.Count; i++)
        {
            if (playerMoney >= storeItems[i].unlockCost)
            {
                storeItems[i].ShowContent();
            }
            else
            {
                storeItems[i].HideContent();
            }
        }
    }
}