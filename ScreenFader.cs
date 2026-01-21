using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class ScreenFader : MonoBehaviour
{
    public static ScreenFader I;

    [Header("Setup")]
    [SerializeField] private Image fadeImage;
    [SerializeField] private Color fadeColor = Color.black;

    private Coroutine fadeRoutine;

    private void Awake()
    {
        if (I != null && I != this)
        {
            Destroy(gameObject);
            return;
        }

        I = this;
        DontDestroyOnLoad(gameObject);

        if (fadeImage == null)
            fadeImage = GetComponentInChildren<Image>(true);

        if (fadeImage == null)
        {
            Debug.LogError("[ScreenFader] No Image assigned or found in children.");
            return;
        }

        // Force full-screen overlay
        var rt = fadeImage.rectTransform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        // Make sure the fader canvas is on top
        var canvas = fadeImage.GetComponentInParent<Canvas>();
        if (canvas != null)
        {
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 1000;
        }

        fadeImage.enabled = true;
        fadeImage.raycastTarget = false;

        // Start fully transparent
        Color c = fadeColor;
        c.a = 0f;
        fadeImage.color = c;
    }

    // Convenience: reload a scene with fade
    public void ReloadSceneWithFade(string sceneName, float fadeOut = 0.3f, float fadeIn = 0.3f)
    {
        if (fadeRoutine != null) StopCoroutine(fadeRoutine);
        fadeRoutine = StartCoroutine(FadeOutIn(() =>
        {
            SceneManager.LoadScene(sceneName);
        }, fadeOut, fadeIn));
    }

    public IEnumerator FadeOutIn(Action middleAction, float fadeOutTime, float fadeInTime)
    {
        if (fadeImage == null)
        {
            middleAction?.Invoke();
            fadeRoutine = null;
            yield break;
        }

        fadeImage.enabled = true;
        // Set to true only if you WANT to block clicks during fade
        fadeImage.raycastTarget = false;

        // --- Fade OUT ---
        float t = 0f;
        Color c = fadeColor;

        while (t < fadeOutTime)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / fadeOutTime);
            c.a = k;
            fadeImage.color = c;
            yield return null;
        }

        c.a = 1f;
        fadeImage.color = c;

        middleAction?.Invoke();
        yield return new WaitForEndOfFrame();

        // Refresh reference in case the hierarchy changed
        if (fadeImage == null)
        {
            fadeImage = GetComponentInChildren<Image>(true);
            if (fadeImage == null)
            {
                fadeRoutine = null;
                yield break;
            }
        }

        // Snap to fully opaque after load
        c = fadeColor;
        c.a = 1f;
        fadeImage.color = c;

        // --- Fade IN ---
        t = 0f;
        while (t < fadeInTime)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / fadeInTime);
            c.a = 1f - k;
            fadeImage.color = c;
            yield return null;
        }

        c.a = 0f;
        fadeImage.color = c;
        fadeImage.raycastTarget = false;

        fadeRoutine = null;
    }
}
