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
        public bool isUnlocked;
    }

    [SerializeField] private List<CharacterData> characters = new List<CharacterData>();
    [SerializeField] private List<GameObject> characterContainers = new List<GameObject>();
    [SerializeField] private List<Sprite> characterSprites = new List<Sprite>();

    private void Awake()
    {
        // Гарантируем, что первый персонаж открыт при запуске игры
        EnsureFirstCharacterUnlocked();
        UpdateCollectionVisuals();
    }

    [ContextMenu("Initialize Collection")]
    private void InitializeCollection()
    {
        Dictionary<int, long> savedCosts = new Dictionary<int, long>();

        // Сохраняем только цены, так как состояние unlock мы контролируем программно для первого
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
            if (viewTransform != null) charImage = viewTransform.GetComponent<Image>();

            Transform buttonTransform = container.transform.Find("AmountButton");
            Button charButton = null;
            if (buttonTransform != null) charButton = buttonTransform.GetComponent<Button>();

            Sprite assignedSprite = null;
            if (i < characterSprites.Count) assignedSprite = characterSprites[i];

            if (charImage != null && assignedSprite != null)
            {
                charImage.sprite = assignedSprite;
            }

            long cost = savedCosts.ContainsKey(i) ? savedCosts[i] : 0;

            CharacterData data = new CharacterData
            {
                viewImage = charImage,
                amountButton = charButton,
                unlockCost = cost,
                isUnlocked = false // По умолчанию закрыт, кроме первого
            };

            characters.Add(data);
        }
    }

    private void EnsureFirstCharacterUnlocked()
    {
        if (characters.Count > 0)
        {
            CharacterData firstChar = characters[0];
            if (!firstChar.isUnlocked)
            {
                firstChar.isUnlocked = true;
                characters[0] = firstChar;
            }
        }
    }

    public int GetTotalCharacters()
    {
        return characters.Count;
    }

    public long GetNextUnlockCost()
    {
        // Начинаем с 1, так как 0-й всегда открыт
        for (int i = 1; i < characters.Count; i++)
        {
            if (!characters[i].isUnlocked)
            {
                return characters[i].unlockCost;
            }
        }
        return 0; // Все открыты
    }

    public void TryUnlockNextCharacter(long playerMoney)
    {
        bool unlockedSomething = false;

        for (int i = 1; i < characters.Count; i++)
        {
            if (!characters[i].isUnlocked)
            {
                if (playerMoney >= characters[i].unlockCost)
                {
                    CharacterData data = characters[i];
                    data.isUnlocked = true;
                    characters[i] = data;
                    unlockedSomething = true;
                    Debug.Log($"[Collection] Character {i + 1} Unlocked!");
                }
                else
                {
                    break;
                }
            }
        }

        if (unlockedSomething)
        {
            UpdateCollectionVisuals();
        }
    }

    private void UpdateCollectionVisuals()
    {
        for (int i = 0; i < characters.Count; i++)
        {
            if (characters[i].viewImage != null)
            {
                characters[i].viewImage.color = characters[i].isUnlocked ? Color.white : Color.black;
            }

            if (characters[i].amountButton != null)
            {
                characters[i].amountButton.gameObject.SetActive(!characters[i].isUnlocked);
            }
        }
    }
}