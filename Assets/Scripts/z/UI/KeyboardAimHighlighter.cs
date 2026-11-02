
using System.Collections.Generic;
using Keybound.Keyboard;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class KeyboardAimHighlighter : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private KeyboardMapManager keyboardMapManager;
    [SerializeField] private AimConditionProcessor aimProcessor;

    [Header("Colors")]
    [SerializeField] private Color targetColor = Color.yellow;
    [SerializeField] private Color completedColor = Color.green;

    [Header("Press Count Effect")]
    [SerializeField, Min(0f)]
    private float pressFlashDuration = 0.25f;

    [Header("Sequence Warning Dots")]
    [SerializeField] private Color warningDotColor = Color.red;
    [SerializeField, Min(1f)]
    private float warningDotSize = 12f;
    [SerializeField]
    private Vector2 warningDotOffset = new Vector2(-7f, -7f);

    [Header("Hold UI")]
    [SerializeField] private Color holdBarBackground =
        new Color(0f, 0f, 0f, 0.5f);

    [SerializeField] private Color holdBarFillColor = Color.green;

    [SerializeField, Min(1f)]
    private float holdBarHeight = 10f;

    [SerializeField, Min(0f)]
    private float holdBarPadding = 6f;

    [Header("Debug")]
    [SerializeField] private bool logHighlight = true;

    private readonly Dictionary<Key, Image> keyImages =
        new Dictionary<Key, Image>();

    private readonly Dictionary<Key, Color> originalColors =
        new Dictionary<Key, Color>();

    private readonly Dictionary<Key, TMP_Text> keyTexts =
        new Dictionary<Key, TMP_Text>();

    private readonly Dictionary<Key, Color> originalTextColors =
        new Dictionary<Key, Color>();

    private readonly Dictionary<Key, Color> highlightColors =
        new Dictionary<Key, Color>();

    private readonly HashSet<Key> completedKeys =
        new HashSet<Key>();

    private readonly Dictionary<Key, GameObject> warningDots =
        new Dictionary<Key, GameObject>();

    private Sprite circleSprite;
    private Texture2D circleTexture;

    private GameObject holdBarObject;
    private Image holdBarFill;

    private AimData displayedAim;
    private int displayedProgress = -1;
    private bool displayedActive;
    private bool initialized;

    private Key flashingKey = Key.None;
    private float flashEndTime = -1f;

    private void OnEnable()
    {
        if (aimProcessor == null)
        {
            return;
        }

        aimProcessor.OnAimStarted += HandleAimStarted;
        aimProcessor.OnAimProgressChanged += HandleProgressChanged;
        aimProcessor.OnAimCompleted += HandleAimCompleted;
        aimProcessor.OnTargetKeyAccepted += HandleTargetAccepted;
        aimProcessor.OnHoldProgressChanged += HandleHoldProgress;
    }

    private void Start()
    {
        InitializeKeys();
        RefreshHighlights();
    }

    private void LateUpdate()
    {
        if (!initialized)
        {
            InitializeKeys();
        }

        if (!initialized || aimProcessor == null)
        {
            return;
        }

        if (displayedAim != aimProcessor.CurrentAim ||
            displayedProgress != aimProcessor.CurrentProgress ||
            displayedActive != aimProcessor.IsAimActive)
        {
            RefreshHighlights();
        }

        MaintainHighlightColors();
        UpdateWarningDots();
        UpdateHoldBar();
    }

    private void OnDisable()
    {
        if (aimProcessor != null)
        {
            aimProcessor.OnAimStarted -= HandleAimStarted;
            aimProcessor.OnAimProgressChanged -= HandleProgressChanged;
            aimProcessor.OnAimCompleted -= HandleAimCompleted;
            aimProcessor.OnTargetKeyAccepted -= HandleTargetAccepted;
            aimProcessor.OnHoldProgressChanged -= HandleHoldProgress;
        }

        RestoreAllKeys();
        HideWarningDots();
        HideHoldBar();

        highlightColors.Clear();
        completedKeys.Clear();

        displayedAim = null;
        displayedProgress = -1;
        displayedActive = false;

        flashingKey = Key.None;
        flashEndTime = -1f;
    }

    private void OnDestroy()
    {
        if (circleSprite != null)
        {
            Destroy(circleSprite);
        }

        if (circleTexture != null)
        {
            Destroy(circleTexture);
        }
    }

    private void InitializeKeys()
    {
        if (initialized || keyboardMapManager == null)
        {
            return;
        }

        foreach (KeyboardKeyData data in KeyboardMapData.Keys)
        {
            if (!keyboardMapManager.TryGetKey(
                    data.InputKey,
                    out KeyboardKey keyboardKey))
            {
                return;
            }
        }

        keyImages.Clear();
        originalColors.Clear();
        keyTexts.Clear();
        originalTextColors.Clear();

        foreach (KeyboardKeyData data in KeyboardMapData.Keys)
        {
            if (!keyboardMapManager.TryGetKey(
                    data.InputKey,
                    out KeyboardKey keyboardKey))
            {
                continue;
            }

            Key key = data.InputKey;
            Image image = keyboardKey.GetComponent<Image>();

            if (image != null)
            {
                keyImages[key] = image;
                originalColors[key] = image.color;
            }

            TMP_Text text =
                keyboardKey.GetComponentInChildren<TMP_Text>();

            if (text != null)
            {
                keyTexts[key] = text;
                originalTextColors[key] = text.color;
            }
        }

        initialized = true;

        if (logHighlight)
        {
            Debug.Log(
                $"[AimHighlight] Initialized: " +
                $"{keyImages.Count} keys",
                this
            );
        }
    }

    private void HandleAimStarted(AimData aim)
    {
        completedKeys.Clear();

        flashingKey = Key.None;
        flashEndTime = -1f;

        RefreshHighlights();
    }

    private void HandleProgressChanged(int current, int required)
    {
        RefreshHighlights();
    }

    private void HandleAimCompleted(AimData aim)
    {
        RefreshHighlights();
    }

    private void HandleTargetAccepted(Key key)
    {
        if (aimProcessor == null ||
            aimProcessor.CurrentAim == null)
        {
            return;
        }

        switch (aimProcessor.CurrentAim.Type)
        {
            case AimType.PressCount:
                flashingKey = key;
                flashEndTime =
                    Time.unscaledTime + pressFlashDuration;
                break;

            case AimType.AllKeys:
                completedKeys.Add(key);
                break;
        }
    }

    private void HandleHoldProgress(float progress)
    {
        UpdateHoldBar();
    }

    private void RefreshHighlights()
    {
        if (!initialized || aimProcessor == null)
        {
            return;
        }

        RestoreAllKeys();
        highlightColors.Clear();

        displayedAim = aimProcessor.CurrentAim;
        displayedProgress = aimProcessor.CurrentProgress;
        displayedActive = aimProcessor.IsAimActive;

        if (displayedAim == null ||
            displayedAim.TargetKeys == null ||
            displayedAim.TargetKeys.Count == 0)
        {
            HideWarningDots();
            HideHoldBar();
            return;
        }

        int progress = displayedProgress;

        bool completed =
            !displayedActive &&
            progress >= aimProcessor.RequiredProgress;

        switch (displayedAim.Type)
        {
            case AimType.PressCount:
            {
                Key target = displayedAim.TargetKeys[0];

                RegisterHighlight(
                    target,
                    completed ? completedColor : targetColor
                );

                break;
            }

            case AimType.AllKeys:
            {
                foreach (Key key in displayedAim.TargetKeys)
                {
                    RegisterHighlight(
                        key,
                        completed || completedKeys.Contains(key)
                            ? completedColor
                            : targetColor
                    );
                }

                break;
            }

            case AimType.Sequence:
            {
                for (int i = 0;
                     i < progress &&
                     i < displayedAim.TargetKeys.Count;
                     i++)
                {
                    RegisterHighlight(
                        displayedAim.TargetKeys[i],
                        completedColor
                    );
                }

                if (!completed &&
                    progress < displayedAim.TargetKeys.Count)
                {
                    RegisterHighlight(
                        displayedAim.TargetKeys[progress],
                        targetColor
                    );
                }

                if (completed)
                {
                    foreach (Key key in displayedAim.TargetKeys)
                    {
                        RegisterHighlight(key, completedColor);
                    }
                }

                break;
            }

            case AimType.Hold:
            {
                Key target = displayedAim.TargetKeys[0];

                RegisterHighlight(
                    target,
                    completed ? completedColor : targetColor
                );

                break;
            }
        }

        MaintainHighlightColors();
        UpdateWarningDots();
        UpdateHoldBar();
    }

    private void RegisterHighlight(Key key, Color color)
    {
        highlightColors[key] = color;
    }

    private void MaintainHighlightColors()
    {
        bool flashActive =
            flashingKey != Key.None &&
            Time.unscaledTime < flashEndTime;

        foreach (var pair in highlightColors)
        {
            Key key = pair.Key;
            Color desiredColor = pair.Value;

            if (flashActive && key == flashingKey)
            {
                desiredColor = completedColor;
            }

            if (!keyImages.TryGetValue(
                    key,
                    out Image image) ||
                image == null)
            {
                continue;
            }

            if (image.color != desiredColor)
            {
                image.color = desiredColor;
            }

            if (keyTexts.TryGetValue(
                    key,
                    out TMP_Text text) &&
                text != null &&
                text.color != Color.black)
            {
                text.color = Color.black;
            }
        }

        if (!flashActive && flashingKey != Key.None)
        {
            flashingKey = Key.None;
            flashEndTime = -1f;
        }
    }

    private void UpdateWarningDots()
    {
        if (displayedAim == null ||
            displayedAim.Type != AimType.Sequence ||
            displayedAim.TargetKeys == null ||
            !displayedActive)
        {
            HideWarningDots();
            return;
        }

        HashSet<Key> warningKeys = new HashSet<Key>();

        for (int i = displayedProgress + 1;
             i < displayedAim.TargetKeys.Count;
             i++)
        {
            Key key = displayedAim.TargetKeys[i];

            // Do not mark the current target as forbidden
            // if the same key appears later in the sequence.
            if (displayedProgress < displayedAim.TargetKeys.Count &&
                key == displayedAim.TargetKeys[displayedProgress])
            {
                continue;
            }

            warningKeys.Add(key);
        }

        foreach (Key key in warningKeys)
        {
            if (!warningDots.TryGetValue(
                    key,
                    out GameObject dot) ||
                dot == null)
            {
                CreateWarningDot(key);
            }

            if (warningDots.TryGetValue(
                    key,
                    out dot) &&
                dot != null)
            {
                dot.SetActive(true);
            }
        }

        foreach (var pair in warningDots)
        {
            if (pair.Value != null &&
                !warningKeys.Contains(pair.Key))
            {
                pair.Value.SetActive(false);
            }
        }
    }

    private void CreateWarningDot(Key key)
    {
        if (!keyImages.TryGetValue(
                key,
                out Image keyImage) ||
            keyImage == null)
        {
            return;
        }

        GameObject dot = new GameObject(
            "SequenceWarningDot",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image)
        );

        dot.transform.SetParent(keyImage.transform, false);

        RectTransform rect =
            dot.GetComponent<RectTransform>();

        rect.anchorMin = Vector2.one;
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(0.5f, 0.5f);

        rect.sizeDelta =
            new Vector2(warningDotSize, warningDotSize);

        rect.anchoredPosition = warningDotOffset;

        Image image = dot.GetComponent<Image>();
        image.color = warningDotColor;
        image.raycastTarget = false;
        image.sprite = GetCircleSprite();

        warningDots[key] = dot;
    }

    private Sprite GetCircleSprite()
    {
        if (circleSprite != null)
        {
            return circleSprite;
        }

        const int size = 32;

        circleTexture = new Texture2D(
            size,
            size,
            TextureFormat.RGBA32,
            false
        );

        circleTexture.filterMode = FilterMode.Bilinear;
        circleTexture.wrapMode = TextureWrapMode.Clamp;

        Color[] pixels = new Color[size * size];

        float center = (size - 1) * 0.5f;
        float radius = size * 0.46f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float distance = Vector2.Distance(
                    new Vector2(x, y),
                    new Vector2(center, center)
                );

                float alpha = Mathf.Clamp01(
                    radius - distance + 0.5f
                );

                pixels[y * size + x] =
                    new Color(1f, 1f, 1f, alpha);
            }
        }

        circleTexture.SetPixels(pixels);
        circleTexture.Apply();

        circleSprite = Sprite.Create(
            circleTexture,
            new Rect(0f, 0f, size, size),
            new Vector2(0.5f, 0.5f),
            100f
        );

        return circleSprite;
    }

    private void HideWarningDots()
    {
        foreach (GameObject dot in warningDots.Values)
        {
            if (dot != null)
            {
                dot.SetActive(false);
            }
        }
    }

    private void UpdateHoldBar()
    {
        if (displayedAim == null ||
            displayedAim.Type != AimType.Hold ||
            displayedAim.TargetKeys == null ||
            displayedAim.TargetKeys.Count == 0)
        {
            HideHoldBar();
            return;
        }

        Key target = displayedAim.TargetKeys[0];

        if (!keyImages.TryGetValue(
                target,
                out Image keyImage) ||
            keyImage == null)
        {
            HideHoldBar();
            return;
        }

        if (holdBarObject == null ||
            holdBarObject.transform.parent != keyImage.transform)
        {
            CreateHoldBar(keyImage.transform);
        }

        holdBarObject.SetActive(true);

        float progress = aimProcessor != null
            ? aimProcessor.HoldProgress
            : 0f;

        if (holdBarFill != null)
        {
            holdBarFill.fillAmount = progress;
        }
    }

    private void CreateHoldBar(Transform parent)
    {
        if (holdBarObject != null)
        {
            Destroy(holdBarObject);
        }

        holdBarObject = new GameObject(
            "HoldProgressBar",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image)
        );

        holdBarObject.transform.SetParent(parent, false);

        RectTransform backgroundRect =
            holdBarObject.GetComponent<RectTransform>();

        backgroundRect.anchorMin = new Vector2(0f, 0f);
        backgroundRect.anchorMax = new Vector2(1f, 0f);
        backgroundRect.pivot = new Vector2(0.5f, 0f);

        backgroundRect.offsetMin =
            new Vector2(holdBarPadding, holdBarPadding);

        backgroundRect.offsetMax =
            new Vector2(-holdBarPadding,
                holdBarPadding + holdBarHeight);

        Image background =
            holdBarObject.GetComponent<Image>();

        background.color = holdBarBackground;
        background.raycastTarget = false;

        GameObject fillObject = new GameObject(
            "Fill",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image)
        );

        fillObject.transform.SetParent(
            holdBarObject.transform,
            false
        );

        RectTransform fillRect =
            fillObject.GetComponent<RectTransform>();

        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.offsetMin = Vector2.zero;
        fillRect.offsetMax = Vector2.zero;

        holdBarFill = fillObject.GetComponent<Image>();

        holdBarFill.color = holdBarFillColor;
        holdBarFill.raycastTarget = false;
        holdBarFill.sprite = GetUISprite();
        holdBarFill.type = Image.Type.Filled;
        holdBarFill.fillMethod = Image.FillMethod.Horizontal;
        holdBarFill.fillOrigin = 0;
        holdBarFill.fillAmount = 0f;
    }

    private Sprite GetUISprite()
    {
        // Built-in white sprite used for UI fill.
        return GetCircleSprite();
    }

    private void HideHoldBar()
    {
        if (holdBarObject != null)
        {
            holdBarObject.SetActive(false);
        }
    }

    private void RestoreAllKeys()
    {
        foreach (var pair in originalColors)
        {
            if (keyImages.TryGetValue(
                    pair.Key,
                    out Image image) &&
                image != null)
            {
                image.color = pair.Value;
            }
        }

        foreach (var pair in originalTextColors)
        {
            if (keyTexts.TryGetValue(
                    pair.Key,
                    out TMP_Text text) &&
                text != null)
            {
                text.color = pair.Value;
            }
        }
    }
}
