using Photon.Pun;
using UnityEngine;

public static class PlayerStatsStorage
{
    private const string KeyPrefix = "stats_";

    private static string ComposeKey(string baseKey)
    {
        var localPlayer = PhotonNetwork.LocalPlayer;
        string suffix = null;

        if (localPlayer != null)
        {
            if (!string.IsNullOrEmpty(localPlayer.UserId))
                suffix = $"uid_{localPlayer.UserId}";
            else if (!string.IsNullOrEmpty(localPlayer.NickName))
                suffix = $"nick_{localPlayer.NickName}";
        }

        if (string.IsNullOrEmpty(suffix))
            suffix = "local";

        return $"{KeyPrefix}{suffix}_{baseKey}";
    }

    public static int GetInt(string baseKey, int defaultValue = 0)
    {
        return PlayerPrefs.GetInt(ComposeKey(baseKey), defaultValue);
    }

    public static void SetInt(string baseKey, int value)
    {
        PlayerPrefs.SetInt(ComposeKey(baseKey), value);
    }

    public static int Increment(string baseKey)
    {
        int newValue = GetInt(baseKey) + 1;
        SetInt(baseKey, newValue);
        return newValue;
    }

    public static void Save()
    {
        PlayerPrefs.Save();
    }
}