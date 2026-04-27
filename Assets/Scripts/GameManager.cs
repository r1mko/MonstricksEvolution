using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using YG;

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

    [Header("Boost Settings")]
    [SerializeField] private Button boostButton;
    [SerializeField] private Image boostIconImage;
    [SerializeField] private TextMeshProUGUI boostTimerText;
    [SerializeField] private float boostDuration = 60f;
    [SerializeField] private AnimationCurve boostPulseCurve;

    private const string MONEY_KEY = "PlayerMoney";
    private const string CLICK_POWER_KEY = "ClickPower";
    private const string AUTO_INCOME_KEY = "AutoIncome";
    private const string LAST_SAVE_TIME_KEY = "LastSaveTime";
    private float timeSinceLastAd = 0f;
    private const float AD_COOLDOWN = 30f;

    private Vector3 originalScale;
    private const float CLICK_SCALE = 0.8f;
    private const float ANIMATION_DURATION = 0.1f;

    private long playerMoney = 0;
    private long clickPower = 1;
    private long moneyPerSecond = 0;

    private bool isBoostActive = false;
    private bool shouldShowAdOnNextClick = false;
    private Coroutine boostCoroutine;
    private Coroutine pulseCoroutine;

    private List<TextMeshProUGUI> activeTexts = new List<TextMeshProUGUI>();
    private RectTransform buttonRect;
    private Transform canvasTransform;
    private Image mainCharacterImage;
    private GameObject boostTimerObject;

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

        if (boostTimerText != null) boostTimerObject = boostTimerText.gameObject;
        if (boostIconImage != null) boostIconImage.color = new Color(1, 1, 1, 0.3f);
        if (boostTimerObject != null) boostTimerObject.SetActive(false);

        if (boostButton != null)
        {
            boostButton.onClick.AddListener(ShowRewardedAdForBoost);
        }

        LoadProgress();

        StartCoroutine(AutoIncomeCoroutine());
    }

    private void Start()
    {
        CheckLevelUp();
        UpdateCharacterImage();
        UpdateMoneyUI();
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
        if (timeSinceLastAd < AD_COOLDOWN)
        {
            timeSinceLastAd += Time.deltaTime;
        }

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

    public long GetClickPower()
    {
        return isBoostActive ? clickPower * 2 : clickPower;
    }

    public void AddMoney(long amount)
    {
        playerMoney += amount;
        SaveProgress();
        UpdateMoneyUI();
        CheckLevelUp();
    }

    public void AddClickPower(long amount)
    {
        clickPower += amount;
        SaveProgress();
    }

    public void AddMoneyPerSecond(long amount)
    {
        moneyPerSecond += amount;
        SaveProgress();
        UpdateMoneyUI();
    }

    private void ShowRewardedAdForBoost()
    {
        YG2.RewardedAdvShow("", () =>
        {
            Debug.Log("[Ads] Rewarded Video Success! Activating Boost.");
            ActivateBoost();
        });
    }

    public void ActivateBoost()
    {
        if (isBoostActive)
        {
            if (boostCoroutine != null) StopCoroutine(boostCoroutine);
            if (pulseCoroutine != null) StopCoroutine(pulseCoroutine);
        }

        isBoostActive = true;

        if (boostIconImage != null) boostIconImage.color = Color.white;
        if (boostTimerObject != null) boostTimerObject.SetActive(true);

        boostCoroutine = StartCoroutine(BoostTimerRoutine());

        if (boostIconImage != null && boostPulseCurve != null)
        {
            pulseCoroutine = StartCoroutine(PulseBoostIcon());
        }

        Debug.Log("[GameManager] Boost Activated! x2 Click Power for 60 seconds.");
    }

    private IEnumerator PulseBoostIcon()
    {
        float duration = boostPulseCurve.keys[boostPulseCurve.length - 1].time;

        while (isBoostActive)
        {
            float timer = 0;
            while (timer < duration && isBoostActive)
            {
                timer += Time.deltaTime;
                float curveValue = boostPulseCurve.Evaluate(timer % duration);

                if (boostIconImage != null)
                {
                    boostIconImage.transform.localScale = Vector3.one * curveValue;
                }

                yield return null;
            }
        }

        // Сброс скейла в конце
        if (boostIconImage != null)
        {
            boostIconImage.transform.localScale = Vector3.one;
        }
    }

    private IEnumerator BoostTimerRoutine()
    {
        float timeLeft = boostDuration;

        while (timeLeft > 0)
        {
            timeLeft -= Time.deltaTime;

            if (boostTimerText != null)
            {
                int seconds = Mathf.CeilToInt(timeLeft);
                boostTimerText.text = $"{seconds} сек.";
            }

            yield return null;
        }

        DeactivateBoost();
    }

    private void DeactivateBoost()
    {
        isBoostActive = false;

        if (pulseCoroutine != null) StopCoroutine(pulseCoroutine);

        if (boostIconImage != null)
        {
            boostIconImage.color = new Color(1, 1, 1, 0.3f);
            boostIconImage.transform.localScale = Vector3.one;
        }

        if (boostTimerObject != null) boostTimerObject.SetActive(false);

        Debug.Log("[GameManager] Boost Deactivated.");
    }

    private void SaveProgress()
    {
        PlayerPrefs.SetString(MONEY_KEY, playerMoney.ToString());
        PlayerPrefs.SetString(CLICK_POWER_KEY, clickPower.ToString());
        PlayerPrefs.SetString(AUTO_INCOME_KEY, moneyPerSecond.ToString());
        PlayerPrefs.SetString(LAST_SAVE_TIME_KEY, System.DateTime.Now.ToBinary().ToString());

        PlayerPrefs.Save();
    }

    private void LoadProgress()
    {
        if (PlayerPrefs.HasKey(MONEY_KEY))
        {
            playerMoney = long.Parse(PlayerPrefs.GetString(MONEY_KEY));
        }

        if (PlayerPrefs.HasKey(CLICK_POWER_KEY))
        {
            clickPower = long.Parse(PlayerPrefs.GetString(CLICK_POWER_KEY));
        }

        if (PlayerPrefs.HasKey(AUTO_INCOME_KEY))
        {
            moneyPerSecond = long.Parse(PlayerPrefs.GetString(AUTO_INCOME_KEY));
        }

        CalculateOfflineEarnings();

        Debug.Log($"[GameManager] Progress Loaded. Money: {playerMoney}, Power: {clickPower}, Auto: {moneyPerSecond}");
    }

    private void CalculateOfflineEarnings()
    {
        if (PlayerPrefs.HasKey(LAST_SAVE_TIME_KEY))
        {
            try
            {
                long binaryDate = long.Parse(PlayerPrefs.GetString(LAST_SAVE_TIME_KEY));
                System.DateTime lastSaveTime = System.DateTime.FromBinary(binaryDate);
                System.DateTime now = System.DateTime.Now;

                System.TimeSpan timeDifference = now - lastSaveTime;

                if (timeDifference.TotalMinutes > 5)
                {
                    long secondsOffline = (long)timeDifference.TotalSeconds;
                    long earnedMoney = secondsOffline * moneyPerSecond;

                    AddMoney(earnedMoney);
                    Debug.Log($"[Offline] You were away for {timeDifference.Minutes} minutes. Earned: {Helper.FormatNumber(earnedMoney)}");

                    shouldShowAdOnNextClick = true;
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[Offline] Error calculating offline earnings: {e.Message}");
            }
        }
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
        if (shouldShowAdOnNextClick)
        {
            TryShowInterstitialAd();
            shouldShowAdOnNextClick = false;
        }

        StartCoroutine(AnimateClick());
        SpawnFloatingText();
        AddMoney(GetClickPower());
    }

    private void TryShowInterstitialAd()
    {
        if (timeSinceLastAd >= AD_COOLDOWN)
        {
            YG2.InterstitialAdvShow();
            timeSinceLastAd = 0f;
            Debug.Log("[Ads] Interstitial Ad Showed");
        }
        else
        {
            Debug.Log($"[Ads] Ad on cooldown. Wait {AD_COOLDOWN - timeSinceLastAd:F1} seconds.");
        }
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
            text.text = $"+{Helper.FormatNumber(GetClickPower())}";

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

    [ContextMenu("Test: Activate Boost")]
    private void TestActivateBoost()
    {
        ActivateBoost();
    }

    [ContextMenu("Clear Saves")]
    private void ClearSaves()
    {
        PlayerPrefs.DeleteAll();
        Debug.Log("[GameManager] All saves cleared.");
    }
}