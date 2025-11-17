using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class ScreenFader : MonoBehaviour
{
    public static ScreenFader Instance { get; private set; }
    
    [SerializeField] private Image fadeImage;
    [SerializeField] private float fadeDuration = 0.5f;
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            
            // Ensure fade image is set up
            if (fadeImage == null)
            {
                fadeImage = GetComponentInChildren<Image>();
            }
            
            // Start fully transparent
            if (fadeImage != null)
            {
                Color c = fadeImage.color;
                c.a = 0f;
                fadeImage.color = c;
                fadeImage.raycastTarget = false;
            }
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    public IEnumerator FadeOut(float duration = -1f)
    {
        if (fadeImage == null) yield break;
        
        float actualDuration = duration > 0 ? duration : fadeDuration;
        fadeImage.raycastTarget = true;
        
        float elapsed = 0f;
        Color c = fadeImage.color;
        
        while (elapsed < actualDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            c.a = Mathf.Lerp(0f, 1f, elapsed / actualDuration);
            fadeImage.color = c;
            yield return null;
        }
        
        c.a = 1f;
        fadeImage.color = c;
    }
    
    public IEnumerator FadeIn(float duration = -1f)
    {
        if (fadeImage == null) yield break;
        
        float actualDuration = duration > 0 ? duration : fadeDuration;
        
        float elapsed = 0f;
        Color c = fadeImage.color;
        
        while (elapsed < actualDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            c.a = Mathf.Lerp(1f, 0f, elapsed / actualDuration);
            fadeImage.color = c;
            yield return null;
        }
        
        c.a = 0f;
        fadeImage.color = c;
        fadeImage.raycastTarget = false;
    }
    
    public void SetBlack()
    {
        if (fadeImage != null)
        {
            Color c = fadeImage.color;
            c.a = 1f;
            fadeImage.color = c;
            fadeImage.raycastTarget = true;
        }
    }
    
    public void SetTransparent()
    {
        if (fadeImage != null)
        {
            Color c = fadeImage.color;
            c.a = 0f;
            fadeImage.color = c;
            fadeImage.raycastTarget = false;
        }
    }
}
