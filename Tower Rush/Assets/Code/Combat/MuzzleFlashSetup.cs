using UnityEngine;

[ExecuteInEditMode]
public class MuzzleFlashSetup : MonoBehaviour
{
    [SerializeField] private float size = 0.5f;
    [SerializeField] private Color flashColor = new Color(1f, 0.9f, 0.3f);
    [SerializeField] private float fadeSpeed = 10f;
    
    private SpriteRenderer spriteRenderer;
    private float initialAlpha;
    
    void Start()
    {
        SetupMuzzleFlash();
        
        if (Application.isPlaying)
        {
            initialAlpha = flashColor.a;
            StartCoroutine(FadeOut());
        }
    }
    
    void SetupMuzzleFlash()
    {
        transform.localScale = Vector3.one * size;
        
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
        {
            spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
        }
        
        Sprite flashSprite = CreateFlashSprite();
        spriteRenderer.sprite = flashSprite;
        spriteRenderer.color = flashColor;
        spriteRenderer.sortingOrder = 100;
    }
    
    Sprite CreateFlashSprite()
    {
        Texture2D texture = new Texture2D(64, 64);
        Color[] pixels = new Color[64 * 64];
        
        for (int y = 0; y < 64; y++)
        {
            for (int x = 0; x < 64; x++)
            {
                float distance = Vector2.Distance(new Vector2(x, y), new Vector2(32, 32)) / 32f;
                float alpha = Mathf.Clamp01(1f - distance);
                alpha = Mathf.Pow(alpha, 2f);
                pixels[y * 64 + x] = new Color(1, 1, 1, alpha);
            }
        }
        
        texture.SetPixels(pixels);
        texture.Apply();
        
        return Sprite.Create(texture, new Rect(0, 0, 64, 64), new Vector2(0.5f, 0.5f), 64);
    }
    
    System.Collections.IEnumerator FadeOut()
    {
        float currentAlpha = initialAlpha;
        
        while (currentAlpha > 0)
        {
            currentAlpha -= Time.deltaTime * fadeSpeed;
            flashColor.a = currentAlpha;
            spriteRenderer.color = flashColor;
            yield return null;
        }
    }
}