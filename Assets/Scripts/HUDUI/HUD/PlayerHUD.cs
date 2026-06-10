using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class PlayerHUD : MonoBehaviour
{
    [System.Serializable]
    public class HeartUI
    {
        public Image heartImage;
        public AnimatedUIImage damageSplash;
    }

    [Header("Health Hearts")]
    [SerializeField] private HeartUI[] hearts;
    [SerializeField] private Sprite fullHeartSprite;
    [SerializeField] private Sprite lostHeartSprite;

    [Header("Heart Damage Animation")]
    [SerializeField] private float heartShakeDuration = 0.25f;
    [SerializeField] private float heartShakeStrength = 8f;
    [SerializeField] private Color heartFlashColor = Color.white;
    [SerializeField] private float sadHeartDelay = 0.12f;

    [Header("Ink Cloud Charges")]
    [SerializeField] private Image[] inkChargeImages;
    [SerializeField] private Color fullInkColor = new Color(1f, 0.85f, 0.2f, 1f);
    [SerializeField] private Color emptyInkColor = new Color(0.15f, 0.15f, 0.15f, 0.5f);

    [Header("Camo Water Meter")]
    [SerializeField] private Image camoWaterFill;
    [SerializeField] private Color camoFullColor = new Color(0.1f, 0.8f, 1f, 0.75f);
    [SerializeField] private Color camoEmptyColor = new Color(0.1f, 0.2f, 0.35f, 0.35f);

    [Header("Crosshair")]
    [SerializeField] private CanvasGroup crosshairGroup;
    [SerializeField] private RectTransform crosshairTransform;
    [SerializeField] private float normalCrosshairAlpha = 1f;
    [SerializeField] private float hiddenCrosshairAlpha = 0.25f;

    [Header("Crosshair Animation")]
    [SerializeField] private float crosshairScaleSpeed = 12f;

    [Header("Grab Arm Indicator")]
    [SerializeField] private Image grabArmImage;
    [SerializeField] private Color armFreeColor = Color.white;
    [SerializeField] private Color armUsedColor = Color.cyan;

    private int previousHealth = -1;
    private float targetCrosshairScale = 1f;

    private void Start()
    {
        HideAllDamageSplashes();
    }

    private void Update()
    {
        AnimateCrosshairScale();
    }

    public void SetHealth(int currentHealth, int maxHealth)
    {
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);

        for (int i = 0; i < hearts.Length; i++)
        {
            if (hearts[i].heartImage == null)
                continue;

            bool shouldShow = i < maxHealth;
            hearts[i].heartImage.enabled = shouldShow;

            if (!shouldShow)
                continue;

            bool heartIsFull = i < currentHealth;

            if (previousHealth == -1 || currentHealth >= previousHealth)
            {
                hearts[i].heartImage.sprite = heartIsFull ? fullHeartSprite : lostHeartSprite;
            }
            else
            {
                if (heartIsFull)
                    hearts[i].heartImage.sprite = fullHeartSprite;
            }
        }

        if (previousHealth != -1 && currentHealth < previousHealth)
        {
            for (int i = currentHealth; i < previousHealth; i++)
            {
                if (i >= 0 && i < hearts.Length)
                {
                    StartCoroutine(AnimateLostHeart(hearts[i]));
                }
            }
        }

        previousHealth = currentHealth;
    }

    public void SetInkCharges(int currentCharges, int maxCharges)
    {
        currentCharges = Mathf.Clamp(currentCharges, 0, maxCharges);

        for (int i = 0; i < inkChargeImages.Length; i++)
        {
            if (inkChargeImages[i] == null)
                continue;

            inkChargeImages[i].enabled = i < maxCharges;
            inkChargeImages[i].color = i < currentCharges ? fullInkColor : emptyInkColor;
        }
    }

    public void SetCamoAmount(float amount)
    {
        amount = Mathf.Clamp01(amount);

        if (camoWaterFill == null)
            return;

        camoWaterFill.fillAmount = amount;
        camoWaterFill.color = Color.Lerp(camoEmptyColor, camoFullColor, amount);
    }

    public void SetHidden(bool isHidden)
    {
        if (crosshairGroup == null)
            return;

        crosshairGroup.alpha = isHidden ? hiddenCrosshairAlpha : normalCrosshairAlpha;
    }

    public void SetGrabArmUsed(bool isUsed)
    {
        if (grabArmImage == null)
            return;

        grabArmImage.color = isUsed ? armUsedColor : armFreeColor;
    }

    public void SetCrosshairInteractable(bool isInteractable)
    {
        targetCrosshairScale = isInteractable ? 1.25f : 1f;
    }

    private void AnimateCrosshairScale()
    {
        if (crosshairTransform == null)
            return;

        float currentScale = crosshairTransform.localScale.x;

        float newScale = Mathf.Lerp(
            currentScale,
            targetCrosshairScale,
            Time.deltaTime * crosshairScaleSpeed
        );

        crosshairTransform.localScale = Vector3.one * newScale;
    }

    private IEnumerator AnimateLostHeart(HeartUI heartUI)
    {
        if (heartUI == null || heartUI.heartImage == null)
            yield break;

        Image heart = heartUI.heartImage;
        RectTransform rect = heart.GetComponent<RectTransform>();

        Vector3 originalPos = rect.localPosition;
        Color originalColor = heart.color;

        heart.color = heartFlashColor;

        if (heartUI.damageSplash != null)
        {
            heartUI.damageSplash.Play();
        }

        float timer = 0f;

        while (timer < heartShakeDuration)
        {
            timer += Time.deltaTime;

            float offsetX = Random.Range(-heartShakeStrength, heartShakeStrength);
            float offsetY = Random.Range(-heartShakeStrength, heartShakeStrength);

            rect.localPosition = originalPos + new Vector3(offsetX, offsetY, 0f);

            if (timer >= sadHeartDelay && heart.sprite != lostHeartSprite)
            {
                heart.sprite = lostHeartSprite;
            }

            yield return null;
        }

        rect.localPosition = originalPos;
        heart.color = originalColor;
        heart.sprite = lostHeartSprite;
    }

    private void HideAllDamageSplashes()
    {
        for (int i = 0; i < hearts.Length; i++)
        {
            if (hearts[i].damageSplash == null)
                continue;

            hearts[i].damageSplash.Hide();
        }
    }
}