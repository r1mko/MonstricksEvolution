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

    [Header("Ad Pre-roll UI")]
    [SerializeField] private GameObject adPreRollPanel;
    [SerializeField] private TextMeshProUGUI adPreRollText;

    [Header("Character Animation")]
    [SerializeField] private Animator mainCharacterAnimator;

    private const string MONEY_KEY = "PlayerMoney";
    private const string CLICK_POWER_KEY = "ClickPower";
    private const string AUTO_INCOME_KEY = "AutoIncome";
    private const string LAST_SAVE_TIME_KEY = "LastSaveTime";
    private float timeSinceLastAd = 0f;
    private const float AD_COOLDOWN = 75f;

    private Vector3 originalScale;
    private const float CLICK_SCALE = 0.8f;
    private const float ANIMATION_DURATION = 0.1f;

    private long playerMoney = 0;
    private long clickPower = 1;
    private long moneyPerSecond = 0;

    private bool isBoostActive = false;
    private bool characterInit = false;
    private bool isGamePaused = false;
    private bool isBoostPaused = false;
    private bool isAutoIncomePaused = false;
    private Coroutine boostCoroutine;
    private Coroutine pulseCoroutine;
    private Coroutine preRollCoroutine;
    private Coroutine sliderCoroutine;
    private Coroutine autoIncomeCoroutine;

    private List<TextMeshProUGUI> activeTexts = new List<TextMeshProUGUI>();
    private RectTransform buttonRect;
    private Transform canvasTransform;
    private Image mainCharacterImage;
    private GameObject boostTimerObject;

    private float currentSliderValue = 0f;
    private Quaternion originalButtonRotation;
    private float originalAnimatorSpeed = 1f;

    private void Awake()
    {
        if (mainCharacterButton != null)
        {
            originalScale = mainCharacterButton.transform.localScale;
            originalButtonRotation = mainCharacterButton.transform.rotation;
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

        if (mainCharacterAnimator == null)
        {
            mainCharacterAnimator = mainCharacterButton?.GetComponent<Animator>();
        }
        if (mainCharacterAnimator != null)
        {
            originalAnimatorSpeed = mainCharacterAnimator.speed;
        }

        LoadProgress();

        autoIncomeCoroutine = StartCoroutine(AutoIncomeCoroutine());
    }

    private void Start()
    {
        CheckLevelUp();
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
            timeSinceLastAd += Time.unscaledDeltaTime;
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
        isBoostPaused = false;

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
                if (isBoostPaused)
                {
                    yield return null;
                    continue;
                }

                timer += Time.deltaTime;
                float curveValue = boostPulseCurve.Evaluate(timer % duration);

                if (boostIconImage != null)
                {
                    boostIconImage.transform.localScale = Vector3.one * curveValue;
                }

                yield return null;
            }
        }

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
            if (isBoostPaused)
            {
                yield return null;
                continue;
            }

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
        isBoostPaused = false;

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

        Debug.Log($"[GameManager] Progress Loaded. Money: {playerMoney}, Power: {clickPower}, Auto: {moneyPerSecond}");
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
                if (characterInit)
                    SoundManager.Instance.PlayOpenCharacter();

                characterInit = true;

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

            if (isAutoIncomePaused) continue;

            if (moneyPerSecond > 0)
            {
                AddMoney(moneyPerSecond);
            }
        }
    }

    private void OnMainCharacterClick()
    {
        if (isGamePaused) return;

        SoundManager.Instance.PlayClick();

        TryShowInterstitialAd();
        StartCoroutine(AnimateClick());
        SpawnFloatingText();
        AddMoney(GetClickPower());
    }

    private void TryShowInterstitialAd()
    {
        if (timeSinceLastAd >= AD_COOLDOWN)
        {
            StartAdPreRollSequence();
        }
        else
        {
            Debug.Log($"[Ads] Ad on cooldown. Wait {AD_COOLDOWN - timeSinceLastAd:F1} seconds.");
        }
    }

    private void StartAdPreRollSequence()
    {
        if (preRollCoroutine != null) StopCoroutine(preRollCoroutine);

        SetGamePause(true);

        if (adPreRollPanel != null) adPreRollPanel.SetActive(true);

        preRollCoroutine = StartCoroutine(PreRollCountdownRoutine());
    }

    private IEnumerator PreRollCountdownRoutine()
    {
        int countdown = 3;

        while (countdown > 0)
        {
            if (adPreRollText != null)
            {
                adPreRollText.text = $"Просмотр рекламы через: {countdown}";
            }

            yield return new WaitForSeconds(1f);
            countdown--;
        }

        ShowActualAd();
    }

    private void ShowActualAd()
    {
        if (adPreRollPanel != null) adPreRollPanel.SetActive(false);

        SetGamePause(false);

        YG2.InterstitialAdvShow();
        timeSinceLastAd = 0f;

        Debug.Log("[Ads] Interstitial Ad Showed");
    }

    private void SetGamePause(bool pause)
    {
        isGamePaused = pause;
        isBoostPaused = pause;
        isAutoIncomePaused = pause;

        if (pause)
        {
            AudioListener.pause = true;

            if (mainCharacterAnimator != null)
            {
                originalAnimatorSpeed = mainCharacterAnimator.speed;
                mainCharacterAnimator.speed = 0f;
            }

            if (buttonRect != null)
            {
                originalButtonRotation = buttonRect.rotation;
            }
        }
        else
        {
            AudioListener.pause = false;

            if (mainCharacterAnimator != null)
            {
                mainCharacterAnimator.speed = originalAnimatorSpeed;
            }

            if (buttonRect != null)
            {
                buttonRect.rotation = originalButtonRotation;
            }
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

    [ContextMenu("Clear Saves")]
    private void ClearSaves()
    {
        PlayerPrefs.DeleteAll();
        Debug.Log("[GameManager] All saves cleared.");
    }
}