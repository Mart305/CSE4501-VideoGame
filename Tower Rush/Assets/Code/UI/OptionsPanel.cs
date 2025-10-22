using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Audio;

public class OptionsPanel : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private Button applyAndBackButton; // Apply & Back to Pause Menu
    [SerializeField] private Transform settingsContainer; // Container for dynamically created settings
    
    [Header("UI Prefabs for Dynamic Creation")]
    [SerializeField] private GameObject sliderPrefab; // Prefab with Slider + Label
    
    // Dynamically created UI elements
    private Slider masterVolumeSlider;
    private Slider musicVolumeSlider;
    private Slider sfxVolumeSlider;

    void Start()
    {
        if (applyAndBackButton != null)
            applyAndBackButton.onClick.AddListener(ApplyAndBack);
        
        // Hide panel initially
        if (panelRoot != null)
            panelRoot.SetActive(false);
        
        // Create audio settings dynamically
        CreateAudioSettings();
        
        LoadSettings();
    }
    
    private void CreateAudioSettings()
    {
        if (settingsContainer == null)
        {
            Debug.LogError("Settings Container is not assigned!");
            return;
        }
        
        // Add VerticalLayoutGroup to settings container if it doesn't have one
        if (settingsContainer.GetComponent<UnityEngine.UI.VerticalLayoutGroup>() == null)
        {
            var verticalLayout = settingsContainer.gameObject.AddComponent<UnityEngine.UI.VerticalLayoutGroup>();
            verticalLayout.spacing = 15;
            verticalLayout.padding = new RectOffset(20, 20, 20, 20);
            verticalLayout.childControlWidth = true;
            verticalLayout.childControlHeight = false;
            verticalLayout.childForceExpandWidth = true;
        }
        
        // Create Master Volume Slider
        masterVolumeSlider = CreateSlider("Master Volume", 0f, 1f, 1f);
        if (masterVolumeSlider != null)
            masterVolumeSlider.onValueChanged.AddListener(SetMasterVolume);
        
        // Create Music Volume Slider
        musicVolumeSlider = CreateSlider("Music Volume", 0f, 1f, 1f);
        if (musicVolumeSlider != null)
            musicVolumeSlider.onValueChanged.AddListener(SetMusicVolume);
        
        // Create SFX Volume Slider
        sfxVolumeSlider = CreateSlider("SFX Volume", 0f, 1f, 1f);
        if (sfxVolumeSlider != null)
            sfxVolumeSlider.onValueChanged.AddListener(SetSFXVolume);
    }
    
    private Slider CreateSlider(string labelText, float minValue, float maxValue, float defaultValue)
    {
        GameObject sliderObj;
        
        if (sliderPrefab != null)
        {
            // Use prefab if provided
            sliderObj = Instantiate(sliderPrefab, settingsContainer);
            
            // Find slider in prefab
            Slider slider = sliderObj.GetComponentInChildren<Slider>();
            if (slider != null)
            {
                slider.minValue = minValue;
                slider.maxValue = maxValue;
                slider.value = defaultValue;
            }
            
            // Update label if exists
            TMPro.TextMeshProUGUI label = sliderObj.GetComponentInChildren<TMPro.TextMeshProUGUI>();
            if (label != null)
            {
                label.text = labelText;
            }
            
            return slider;
        }
        else
        {
            // Create basic slider from scratch with proper UI components
            sliderObj = new GameObject(labelText.Replace(" ", ""));
            sliderObj.transform.SetParent(settingsContainer, false);
            
            // Add RectTransform
            RectTransform rectTransform = sliderObj.AddComponent<RectTransform>();
            rectTransform.sizeDelta = new Vector2(300, 50);
            
            // Add horizontal layout group
            var layoutGroup = sliderObj.AddComponent<UnityEngine.UI.HorizontalLayoutGroup>();
            layoutGroup.childControlWidth = true;
            layoutGroup.childControlHeight = false;
            layoutGroup.childForceExpandWidth = true;
            layoutGroup.spacing = 10;
            layoutGroup.padding = new RectOffset(10, 10, 10, 10);
            
            // Create label
            GameObject labelObj = new GameObject("Label");
            labelObj.transform.SetParent(sliderObj.transform, false);
            RectTransform labelRect = labelObj.AddComponent<RectTransform>();
            labelRect.sizeDelta = new Vector2(150, 30);
            
            TMPro.TextMeshProUGUI label = labelObj.AddComponent<TMPro.TextMeshProUGUI>();
            label.text = labelText;
            label.fontSize = 18;
            label.color = Color.white;
            label.alignment = TMPro.TextAlignmentOptions.MidlineLeft;
            
            // Create slider
            GameObject sliderGameObj = new GameObject("Slider");
            sliderGameObj.transform.SetParent(sliderObj.transform, false);
            RectTransform sliderRect = sliderGameObj.AddComponent<RectTransform>();
            sliderRect.sizeDelta = new Vector2(150, 20);
            
            Slider slider = sliderGameObj.AddComponent<Slider>();
            slider.minValue = minValue;
            slider.maxValue = maxValue;
            slider.value = defaultValue;
            
            // Create slider background
            GameObject bgObj = new GameObject("Background");
            bgObj.transform.SetParent(sliderGameObj.transform, false);
            RectTransform bgRect = bgObj.AddComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.sizeDelta = Vector2.zero;
            
            UnityEngine.UI.Image bgImage = bgObj.AddComponent<UnityEngine.UI.Image>();
            bgImage.color = new Color(0.2f, 0.2f, 0.2f, 1f);
            
            // Create slider fill area
            GameObject fillAreaObj = new GameObject("Fill Area");
            fillAreaObj.transform.SetParent(sliderGameObj.transform, false);
            RectTransform fillAreaRect = fillAreaObj.AddComponent<RectTransform>();
            fillAreaRect.anchorMin = Vector2.zero;
            fillAreaRect.anchorMax = Vector2.one;
            fillAreaRect.sizeDelta = new Vector2(-20, 0);
            
            GameObject fillObj = new GameObject("Fill");
            fillObj.transform.SetParent(fillAreaObj.transform, false);
            RectTransform fillRect = fillObj.AddComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.sizeDelta = Vector2.zero;
            
            UnityEngine.UI.Image fillImage = fillObj.AddComponent<UnityEngine.UI.Image>();
            fillImage.color = new Color(0.3f, 0.6f, 1f, 1f);
            
            // Create slider handle
            GameObject handleAreaObj = new GameObject("Handle Slide Area");
            handleAreaObj.transform.SetParent(sliderGameObj.transform, false);
            RectTransform handleAreaRect = handleAreaObj.AddComponent<RectTransform>();
            handleAreaRect.anchorMin = Vector2.zero;
            handleAreaRect.anchorMax = Vector2.one;
            handleAreaRect.sizeDelta = new Vector2(-20, 0);
            
            GameObject handleObj = new GameObject("Handle");
            handleObj.transform.SetParent(handleAreaObj.transform, false);
            RectTransform handleRect = handleObj.AddComponent<RectTransform>();
            handleRect.sizeDelta = new Vector2(20, 20);
            
            UnityEngine.UI.Image handleImage = handleObj.AddComponent<UnityEngine.UI.Image>();
            handleImage.color = Color.white;
            
            // Assign slider components
            slider.fillRect = fillRect;
            slider.handleRect = handleRect;
            slider.targetGraphic = handleImage;
            
            return slider;
        }
    }


    public void OpenPanel()
    {
        if (panelRoot != null)
        {
            panelRoot.SetActive(true);
        }
        
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayButtonClickSound();
        }
    }

    public void ClosePanel()
    {
        if (panelRoot != null)
        {
            panelRoot.SetActive(false);
        }
        
        SaveSettings();
        
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayButtonClickSound();
        }
    }
    
    public void ApplyAndBack()
    {
        // Save all settings
        SaveSettings();
        
        // Close options panel
        if (panelRoot != null)
        {
            panelRoot.SetActive(false);
        }
        
        // Pause menu should already be visible, just close options
        // The pause menu manager handles showing/hiding its own canvas
        
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayButtonClickSound();
        }
    }

    private void SetMasterVolume(float volume)
    {
        // Use AudioListener.volume for master volume (affects all audio)
        AudioListener.volume = volume;
        
        // Also update through AudioManager if available
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.SetMasterVolume(volume);
        }
    }

    private void SetMusicVolume(float volume)
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.SetMusicVolume(volume);
        }
    }

    private void SetSFXVolume(float volume)
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.SetSFXVolume(volume);
        }
    }


    private void SaveSettings()
    {
        if (masterVolumeSlider != null)
            PlayerPrefs.SetFloat("MasterVolume", masterVolumeSlider.value);
        
        if (musicVolumeSlider != null)
            PlayerPrefs.SetFloat("MusicVolume", musicVolumeSlider.value);
        
        if (sfxVolumeSlider != null)
            PlayerPrefs.SetFloat("SFXVolume", sfxVolumeSlider.value);
        
        PlayerPrefs.Save();
    }

    private void LoadSettings()
    {
        if (masterVolumeSlider != null)
        {
            float volume = PlayerPrefs.GetFloat("MasterVolume", 1f);
            masterVolumeSlider.value = volume;
            SetMasterVolume(volume);
        }
        
        if (musicVolumeSlider != null)
        {
            float volume = PlayerPrefs.GetFloat("MusicVolume", 1f);
            musicVolumeSlider.value = volume;
            SetMusicVolume(volume);
        }
        
        if (sfxVolumeSlider != null)
        {
            float volume = PlayerPrefs.GetFloat("SFXVolume", 1f);
            sfxVolumeSlider.value = volume;
            SetSFXVolume(volume);
        }
    }
}
