using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CollectionManager : MonoBehaviour
{
    [System.Serializable]
    public struct CharacterData
    {
        public Image viewImage;
        public Button amountButton;
        public long unlockCost;
    }

    [SerializeField] private List<CharacterData> characters = new List<CharacterData>();
    [SerializeField] private List<GameObject> characterContainers = new List<GameObject>();
    [SerializeField] private List<Sprite> characterSprites = new List<Sprite>();

    [ContextMenu("Initialize Collection")]
    [ContextMenu("Initialize Collection")]
    private void InitializeCollection()
    {
        Dictionary<int, long> savedCosts = new Dictionary<int, long>();

        for (int i = 0; i < characters.Count; i++)
        {
            if (i < characterContainers.Count && characterContainers[i] != null)
            {
                savedCosts[i] = characters[i].unlockCost;
            }
        }

        characters.Clear();

        for (int i = 0; i < characterContainers.Count; i++)
        {
            GameObject container = characterContainers[i];
            if (container == null) continue;

            Transform viewTransform = container.transform.Find("View");
            Image charImage = null;

            if (viewTransform != null)
            {
                charImage = viewTransform.GetComponent<Image>();
            }

            Transform buttonTransform = container.transform.Find("AmountButton");
            Button charButton = null;

            if (buttonTransform != null)
            {
                charButton = buttonTransform.GetComponent<Button>();
            }

            Sprite assignedSprite = null;
            if (i < characterSprites.Count)
            {
                assignedSprite = characterSprites[i];
            }

            if (charImage != null && assignedSprite != null)
            {
                charImage.sprite = assignedSprite;
            }

            long cost = 0;
            if (savedCosts.ContainsKey(i))
            {
                cost = savedCosts[i];
            }

            CharacterData data = new CharacterData
            {
                viewImage = charImage,
                amountButton = charButton,
                unlockCost = cost
            };

            characters.Add(data);
        }
    }
}