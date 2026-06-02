using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class AnimatedUIImage : MonoBehaviour
{
    [Header("Animation")]
    [SerializeField] private Image targetImage;
    [SerializeField] private Sprite[] frames;
    [SerializeField] private float framesPerSecond = 18f;
    [SerializeField] private bool hideWhenFinished = true;

    private Coroutine animationRoutine;

    private void Awake()
    {
        Hide();
    }

    public void Play()
    {
        if (targetImage == null || frames == null || frames.Length == 0)
            return;

        if (animationRoutine != null)
            StopCoroutine(animationRoutine);

        animationRoutine = StartCoroutine(PlayRoutine());
    }

    public void Hide()
    {
        if (targetImage != null)
        {
            targetImage.enabled = false;
        }
    }

    private IEnumerator PlayRoutine()
    {
        targetImage.enabled = true;

        float delay = 1f / framesPerSecond;

        for (int i = 0; i < frames.Length; i++)
        {
            targetImage.sprite = frames[i];
            yield return new WaitForSeconds(delay);
        }

        if (hideWhenFinished)
        {
            targetImage.enabled = false;
        }

        animationRoutine = null;
    }
}