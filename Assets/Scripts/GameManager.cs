using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class GameManager : MonoBehaviour
{
    [SerializeField] private Button mainCharacterButton;
    [SerializeField] private TextMeshProUGUI floatingTextPrefab;
    [SerializeField] private TextMeshProUGUI playerMoneyText;
    [SerializeField] private TextMeshProUGUI playerMoneyInSecText;
    [SerializeField] private Slider levelProgressSlider;

    [Header("Floating Text Settings")]
    [SerializeField] private float floatHeight = 100f;
    [SerializeField] private float randomXOffset = 50f;
    [SerializeField] private float animationDuration = 0.8f;
    [SerializeField] private float waitBeforeReset = 0.2f;

    [Header("References")]
    [SerializeField] private CollectionManager collectionManager;

    [Header("Slider Settings")]
    [SerializeField] private float sliderSmoothTime = 0.3f;

    private Vector3 originalScale;
    private const float CLICK_SCALE = 0.8f;
    private const float ANIMATION_DURATION = 0.1f;

    private long playerMoney = 0;
    private long clickPower = 1;
    private long moneyPerSecond = 0;

    private List<TextMeshProUGUI> activeTexts = new List<TextMeshProUGUI>();
    private RectTransform buttonRect;
    private Transform canvasTransform;
    private Image mainCharacterImage;

    private Coroutine sliderCoroutine;
    private float currentSliderValue = 0f;

    private void Awake()
    {
        if (mainCharacterButton != null)
        {
            originalScale = mainCharacterButton.transform.localScale;
            mainCharacterButton.onClick.AddListener(OnMainCharacterClick);
            buttonRect = mainCharacterButton.GetComponent<RectTransform>();
            mainCharacterImage = mainCharacterButton.GetComponent<Image>();

            Canvas canvas = buttonRect.GetComponentInParent<Canvas>();
            if (canvas != null) canvasTransform = canvas.transform;

            InitializePool();
        }

        if (collectionManager == null)
        {
            collectionManager = GetComponent<CollectionManager>();
        }

        StartCoroutine(AutoIncomeCoroutine());
    }

    private void Start()
    {
        CheckLevelUp();
        UpdateCharacterImage();
    }

    private void InitializePool()
    {
        activeTexts.Clear();
        foreach (Transform child in buttonRect)
        {
            TextMeshProUGUI textComp = child.GetComponent<TextMeshProUGUI>();
            if (textComp != null)
            {
                textComp.gameObject.SetActive(false);
                activeTexts.Add(textComp);
            }
        }
    }

    private void Update()
    {
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            OnMainCharacterClick();
        }
#else
        if (Input.GetKeyDown(KeyCode.Space))
        {
            OnMainCharacterClick();
        }
#endif
    }

    public long GetMoney()
    {
        return playerMoney;
    }

    public void AddMoney(long amount)
    {
        playerMoney += amount;
        UpdateMoneyUI();
        CheckLevelUp();
    }

    public void AddClickPower(long amount)
    {
        clickPower += amount;
    }

    public void AddMoneyPerSecond(long amount)
    {
        moneyPerSecond += amount;
        UpdateMoneyUI();
    }

    private string GetNextLevelCostString()
    {
        if (collectionManager == null) return "Lvl ???";

        long nextCost = collectionManager.GetNextUnlockCost();

        if (nextCost <= 0)
        {
            return "MAX";
        }

        return Helper.FormatNumber(nextCost);
    }

    private float GetLevelProgress()
    {
        if (collectionManager == null) return 0;

        long nextCost = collectionManager.GetNextUnlockCost();

        if (nextCost <= 0) return 1f;

        return Mathf.Clamp01((float)playerMoney / (float)nextCost);
    }

    private void CheckLevelUp()
    {
        if (collectionManager != null)
        {
            int previousUnlocked = collectionManager.GetUnlockedCount();

            collectionManager.TryUnlockNextCharacter(playerMoney);

            if (collectionManager.GetUnlockedCount() > previousUnlocked)
            {
                UpdateCharacterImage();
            }

            UpdateMoneyUI();
        }
    }

    private void UpdateCharacterImage()
    {
        if (collectionManager != null && mainCharacterImage != null)
        {
            Sprite newSprite = collectionManager.GetCurrentCharacterSprite();
            if (newSprite != null)
            {
                mainCharacterImage.sprite = newSprite;
            }
        }
    }

    private void UpdateMoneyUI()
    {
        string levelInfo = GetNextLevelCostString();
        float targetProgress = GetLevelProgress();

        if (playerMoneyText != null)
        {
            playerMoneyText.text = $"{Helper.FormatNumber(playerMoney)} / {levelInfo}";
        }

        if (playerMoneyInSecText != null)
        {
            playerMoneyInSecText.text = $"{Helper.FormatNumber(moneyPerSecond)} монет в сек.";
        }

        if (levelProgressSlider != null)
        {
            SmoothUpdateSlider(targetProgress);
        }
    }

    private void SmoothUpdateSlider(float targetValue)
    {
        if (sliderCoroutine != null)
        {
            StopCoroutine(sliderCoroutine);
        }
        sliderCoroutine = StartCoroutine(SmoothSliderCoroutine(targetValue));
    }

    private IEnumerator SmoothSliderCoroutine(float targetValue)
    {
        float startValue = currentSliderValue;
        float timer = 0;

        while (timer < sliderSmoothTime)
        {
            timer += Time.deltaTime;
            float t = timer / sliderSmoothTime;

            currentSliderValue = Mathf.Lerp(startValue, targetValue, t);

            if (levelProgressSlider != null)
            {
                levelProgressSlider.value = currentSliderValue;
            }

            yield return null;
        }

        currentSliderValue = targetValue;
        if (levelProgressSlider != null)
        {
            levelProgressSlider.value = currentSliderValue;
        }
    }

    private IEnumerator AutoIncomeCoroutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(1f);
            if (moneyPerSecond > 0)
            {
                AddMoney(moneyPerSecond);
            }
        }
    }

    private void OnMainCharacterClick()
    {
        StartCoroutine(AnimateClick());
        SpawnFloatingText();
        AddMoney(clickPower);
    }

    private IEnumerator AnimateClick()
    {
        float timer = 0;
        Vector3 startScale = mainCharacterButton.transform.localScale;

        while (timer < ANIMATION_DURATION)
        {
            timer += Time.deltaTime;
            float t = timer / ANIMATION_DURATION;
            mainCharacterButton.transform.localScale = Vector3.Lerp(startScale, originalScale * CLICK_SCALE, t);
            yield return null;
        }

        mainCharacterButton.transform.localScale = originalScale * CLICK_SCALE;

        timer = 0;
        startScale = mainCharacterButton.transform.localScale;

        while (timer < ANIMATION_DURATION)
        {
            timer += Time.deltaTime;
            float t = timer / ANIMATION_DURATION;
            mainCharacterButton.transform.localScale = Vector3.Lerp(startScale, originalScale, t);
            yield return null;
        }

        mainCharacterButton.transform.localScale = originalScale;
    }

    private void SpawnFloatingText()
    {
        TextMeshProUGUI text = null;

        foreach (var t in activeTexts)
        {
            if (t != null && !t.gameObject.activeSelf)
            {
                text = t;
                break;
            }
        }

        if (text == null)
        {
            if (floatingTextPrefab != null && canvasTransform != null)
            {
                TextMeshProUGUI newTextObj = Instantiate(floatingTextPrefab, canvasTransform);
                text = newTextObj.GetComponent<TextMeshProUGUI>();
                text.gameObject.SetActive(false);
                activeTexts.Add(text);
            }
        }

        if (text != null)
        {
            text.text = $"+{Helper.FormatNumber(clickPower)}";

            text.transform.SetParent(canvasTransform);
            text.transform.localScale = Vector3.one;

            Vector3 worldPos = buttonRect.position;
            float randomX = Random.Range(-randomXOffset, randomXOffset);
            text.rectTransform.position = worldPos + new Vector3(randomX, 0, 0);
            text.rectTransform.rotation = Quaternion.identity;

            Color c = text.color;
            c.a = 1f;
            text.color = c;

            text.gameObject.SetActive(true);

            StartCoroutine(AnimateFloatingText(text));
        }
    }

    private IEnumerator AnimateFloatingText(TextMeshProUGUI text)
    {
        Vector3 startPos = text.rectTransform.position;
        Vector3 endPos = startPos + new Vector3(0, floatHeight, 0);
        float timer = 0;
        Color startColor = text.color;

        while (timer < animationDuration)
        {
            timer += Time.deltaTime;
            float t = timer / animationDuration;

            text.rectTransform.position = Vector3.Lerp(startPos, endPos, t);

            Color c = startColor;
            c.a = 1f - t;
            text.color = c;

            yield return null;
        }

        yield return new WaitForSeconds(waitBeforeReset);

        text.transform.SetParent(buttonRect);
        text.rectTransform.localPosition = Vector3.zero;
        text.rectTransform.localRotation = Quaternion.identity;
        text.transform.localScale = Vector3.one;

        Color resetColor = text.color;
        resetColor.a = 1f;
        text.color = resetColor;

        text.gameObject.SetActive(false);
    }

    [ContextMenu("Test: Add 1K")]
    private void TestAdd1K()
    {
        AddMoney(1000);
    }

    [ContextMenu("Test: Add 50K")]
    private void TestAdd50K()
    {
        AddMoney(50000);
    }

    [ContextMenu("Test: Add 1M")]
    private void TestAdd1M()
    {
        AddMoney(1000000);
    }

    [ContextMenu("Test: Add 50M")]
    private void TestAdd50M()
    {
        AddMoney(50000000);
    }

    [ContextMenu("Test: Add 1B")]
    private void TestAdd1B()
    {
        AddMoney(1000000000);
    }

    [ContextMenu("Test: Add 50B")]
    private void TestAdd50B()
    {
        AddMoney(50000000000L);
    }

    [ContextMenu("Test: Add 1T")]
    private void TestAdd1T()
    {
        AddMoney(1000000000000L);
    }
}