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

    [Header("Floating Text Settings")]
    [SerializeField] private float floatHeight = 100f;
    [SerializeField] private float randomXOffset = 50f;
    [SerializeField] private float animationDuration = 0.8f;
    [SerializeField] private float waitBeforeReset = 0.2f;

    private Vector3 originalScale;
    private const float CLICK_SCALE = 0.8f;
    private const float ANIMATION_DURATION = 0.1f;

    private long playerMoney = 0;
    private long clickPower = 1;
    private long moneyPerSecond = 0;
    private int playerLevel = 1;

    private List<TextMeshProUGUI> activeTexts = new List<TextMeshProUGUI>();
    private RectTransform buttonRect;
    private Transform canvasTransform;

    private void Awake()
    {
        if (mainCharacterButton != null)
        {
            originalScale = mainCharacterButton.transform.localScale;
            mainCharacterButton.onClick.AddListener(OnMainCharacterClick);
            buttonRect = mainCharacterButton.GetComponent<RectTransform>();

            Canvas canvas = buttonRect.GetComponentInParent<Canvas>();
            if (canvas != null) canvasTransform = canvas.transform;

            InitializePool();
        }

        UpdateMoneyUI();
        StartCoroutine(AutoIncomeCoroutine());
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

    public long GetClickPower()
    {
        return clickPower;
    }

    public void AddMoney(long amount)
    {
        playerMoney += amount;
        UpdateMoneyUI();
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

    private void UpdateMoneyUI()
    {
        if (playerMoneyText != null)
        {
            playerMoneyText.text = $"{Helper.FormatNumber(playerMoney)} / Lvl {playerLevel}";
        }

        if (playerMoneyInSecText != null)
        {
            playerMoneyInSecText.text = $"{Helper.FormatNumber(moneyPerSecond)} монет в сек.";
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