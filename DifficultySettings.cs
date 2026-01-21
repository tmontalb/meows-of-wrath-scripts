using UnityEngine;

public static class DifficultySettings
{
    private const string EasyModeKey = "EasyMode";

    // Default is NORMAL (false) when the key doesn't exist yet.
    public static bool EasyMode
    {
        get => PlayerPrefs.GetInt(EasyModeKey, 0) == 1;
        set
        {
            PlayerPrefs.SetInt(EasyModeKey, value ? 1 : 0);
            PlayerPrefs.Save();
        }
    }

    // Normal values
    public const float NormalWallStickTime = 0.25f;
    public const float NormalWallSlideSpeedMax = 10f;

    // Easy mode values
    public const float EasyWallStickTime = 1f;
    public const float EasyWallSlideSpeedMax = 3f;
}
