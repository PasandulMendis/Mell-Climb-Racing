using UnityEngine;
using System.Collections.Generic;

public class MemeManager : MonoBehaviour
{
    public static MemeManager instance;

    [Header("Core Settings")]
    [Tooltip("The exact string name of your Idle category below")]
    public string idleCategoryName = "Idle";

    [Header("Meme Categories (Expandable!)")]
    [Tooltip("Add as many unique situations here as you want! (e.g., UFO_Abduct, Meteor_Hit)")]
    public List<MemeCategory> categories = new List<MemeCategory>();

    private List<MemeScreen> activeScreens = new List<MemeScreen>();

    // LEGACY: Kept so existing scripts (GameManager, Maps, etc.) don't break while we upgrade!
    public enum MemeEvent { Idle, Happy, Angry, Scared }

    void Awake()
    {
        if (instance == null) instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        ApplyMuteState();
        TriggerReaction(idleCategoryName); // Start driving
    }

    // --- SCREEN MANAGEMENT ---
    public void RegisterScreen(MemeScreen screen)
    {
        if (!activeScreens.Contains(screen)) activeScreens.Add(screen);
        
        // Apply current mute state the moment a new passenger spawns in!
        screen.ApplyMute(PlayerPrefs.GetInt("IsMuted", 0) == 1);
    }

    public void UnregisterScreen(MemeScreen screen)
    {
        if (activeScreens.Contains(screen)) activeScreens.Remove(screen);
    }

    // --- THE NEW BROADCAST SYSTEM ---
    public void TriggerReaction(string categoryName, bool isGameOver = false)
    {
        // Find the category setup in the Inspector
        MemeCategory cat = categories.Find(c => c.categoryName == categoryName);
        
        if (cat == null)
        {
            Debug.LogWarning("MemeManager: Attempted to trigger unknown category '" + categoryName + "'!");
            return;
        }

        // Shout the event to every single video player in the game!
        foreach (MemeScreen screen in activeScreens)
        {
            screen.TryPlayReaction(cat, isGameOver);
        }
    }

    // --- BACKWARDS COMPATIBILITY FOR OLD SCRIPTS ---
    // Maps the old enum system directly into our new powerful string system
    public void TriggerMeme(MemeEvent memeType, bool forceOverride = false, bool isGameOver = false)
    {
        string targetCategory = memeType.ToString(); // Converts MemeEvent.Happy -> "Happy"
        TriggerReaction(targetCategory, isGameOver);
    }

    public void ReturnToIdle(MemeScreen screen)
    {
        MemeCategory idleCat = categories.Find(c => c.categoryName == idleCategoryName);
        if (idleCat != null)
        {
            screen.TryPlayReaction(idleCat, false);
        }
    }

    public void ApplyMuteState()
    {
        bool isMuted = PlayerPrefs.GetInt("IsMuted", 0) == 1;
        foreach (MemeScreen screen in activeScreens)
        {
            screen.ApplyMute(isMuted);
        }
    }
}