using UnityEngine;
using UnityEngine.Video;

[System.Serializable]
public class MemeCategory
{
    [Tooltip("The exact name of the event (e.g., 'Idle', 'Happy', 'IcicleHit')")]
    public string categoryName = "New Category";
    
    [Tooltip("Higher numbers interrupt lower numbers. (1 = Idle, 2 = Minor, 5 = Game Over)")]
    public int priorityTier = 1;
    
    [Tooltip("If checked, this meme CANNOT be interrupted even by higher tiers. It MUST finish.")]
    public bool forceFullPlaytime = false;

    [Tooltip("If checked, this video loops forever until something interrupts it (Perfect for Game Overs!)")]
    public bool loopsInfinitely = false;

    [Header("Driver Clips")]
    public VideoClip[] driverClips;

    [Header("Passenger Clips (Chaos!)")]
    [Tooltip("These clips are exclusively for passengers on buses/flatbeds!")]
    public VideoClip[] passengerClips;
}