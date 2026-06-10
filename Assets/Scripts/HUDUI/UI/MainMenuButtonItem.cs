using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

public class MainMenuButtonItem : MonoBehaviour, IPointerEnterHandler, IPointerClickHandler
{
    [Header("Button Info")]
    public string buttonName;

    [Header("References")]
    public RectTransform rectTransform;
    public TextMeshProUGUI text;

    private MainMenuController controller;
    private Vector2 startPos;

    public Vector2 StartPos => startPos;

    private void Awake()
    {
        if (rectTransform == null)
            rectTransform = GetComponent<RectTransform>();

        if (text == null)
            text = GetComponent<TextMeshProUGUI>();

        startPos = rectTransform.anchoredPosition;
    }

    public void Setup(MainMenuController newController)
    {
        controller = newController;
    }

    public void SetVisual(bool selected, float speed, float selectedScale, float normalScale, float selectedXOffset)
    {
        float targetScale = selected ? selectedScale : normalScale;
        Vector2 targetPos = startPos + new Vector2(selected ? selectedXOffset : 0f, 0f);

        rectTransform.localScale = Vector3.Lerp(
            rectTransform.localScale,
            Vector3.one * targetScale,
            Time.unscaledDeltaTime * speed
        );

        rectTransform.anchoredPosition = Vector2.Lerp(
            rectTransform.anchoredPosition,
            targetPos,
            Time.unscaledDeltaTime * speed
        );
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (controller != null)
            controller.SelectButton(this);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (controller != null)
            controller.ActivateSelected();
    }
}