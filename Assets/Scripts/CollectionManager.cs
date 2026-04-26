using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CollectionManager : MonoBehaviour
{
    [System.Serializable]
    public struct CharacterData
    {
        public Image viewImage;
        public long unlockCost;
    }

    [SerializeField] private List<CharacterData> characters = new List<CharacterData>();
    [SerializeField] private List<GameObject> characterContainers = new List<GameObject>();
    [SerializeField] private List<Sprite> characterSprites = new List<Sprite>();

    [ContextMenu("Initialize Collection")]
    private void InitializeCollection()
    {
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

            Sprite assignedSprite = null;
            if (i < characterSprites.Count)
            {
                assignedSprite = characterSprites[i];
            }

            if (charImage != null && assignedSprite != null)
            {
                charImage.sprite = assignedSprite;
            }

            CharacterData data = new CharacterData
            {
                viewImage = charImage,
                unlockCost = 0
            };

            characters.Add(data);
        }
    }
}