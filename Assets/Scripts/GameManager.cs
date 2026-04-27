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

    [Header("Floating Text Settings")]
    [SerializeField] private float floatHeight = 100f;
    [SerializeField] private float randomXOffset = 50f;
    [SerializeField] private float animationDuration = 0.8f;
    [SerializeField] private float waitBeforeReset = 0.2f;

    private Vector3 originalScale;
    private const float CLICK_SCALE = 0.8f;
    private const float ANIMATION_DURATION = 0.1f;

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

    private void OnMainCharacterClick()
    {
        StartCoroutine(AnimateClick());
        SpawnFloatingText();
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
            // Вытаскиваем на уровень Canvas
            text.transform.SetParent(canvasTransform);

            // Сбрасываем скейл, чтобы он не зависел от кнопки
            text.transform.localScale = Vector3.one;

            // Позиция и ротация
            Vector3 worldPos = buttonRect.position;
            float randomX = Random.Range(-randomXOffset, randomXOffset);
            text.rectTransform.position = worldPos + new Vector3(randomX, 0, 0);
            text.rectTransform.rotation = Quaternion.identity;

            // Цвет
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

        // Возвращаем в кнопку
        text.transform.SetParent(buttonRect);

        // Сброс трансформаций
        text.rectTransform.localPosition = Vector3.zero;
        text.rectTransform.localRotation = Quaternion.identity;
        text.transform.localScale = Vector3.one;

        // Сброс цвета
        Color resetColor = text.color;
        resetColor.a = 1f;
        text.color = resetColor;

        text.gameObject.SetActive(false);
    }
}