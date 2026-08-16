using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

[RequireComponent(typeof(Selectable))]
public class ButtonSound : MonoBehaviour, IPointerClickHandler
{
    public enum SoundType 
    { 
        StandardClick, 
        BuyUpgrade, 
        PlayButton, 
        Error 
    }

    [Header("Audio Settings")]
    [Tooltip("What kind of sound should this button make?")]
    public SoundType soundType = SoundType.StandardClick;

    private Selectable selectableComponent;

    private void Awake()
    {
        selectableComponent = GetComponent<Selectable>();
    }

    /// <summary>
    /// Uses Unity EventSystem directly. Immune to RemoveAllListeners()!
    /// </summary>
    public void OnPointerClick(PointerEventData eventData)
    {
        // Don't play sound if the UI button/selectable is disabled or non-interactable
        if (selectableComponent != null && !selectableComponent.interactable) return;

        PlaySound();
    }

    public void PlaySound()
    {
        if (AudioManager.instance == null) return;

        switch (soundType)
        {
            case SoundType.StandardClick: 
                AudioManager.instance.PlayStandardClick(); 
                break;
            case SoundType.BuyUpgrade: 
                AudioManager.instance.PlayBuyUpgradeClick(); 
                break;
            case SoundType.PlayButton: 
                AudioManager.instance.PlayPlayButtonClick(); 
                break;
            case SoundType.Error: 
                AudioManager.instance.PlayErrorClick(); 
                break;
        }
    }
}