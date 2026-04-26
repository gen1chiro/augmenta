using UnityEngine;

public static class CareerProgression
{
    private const string UnlockedIndexKey = "career.unlockedIndex";
    private const string DefeatedPrefix = "career.defeated.";

    private static readonly string[] OpponentIds = { "midas", "metro", "twin", "zeus" };
    private static readonly string[] OpponentScenes = { "vsMidas", "vsMetro", "vsTwin", "vsZeus" };

    public static int MaxOpponentIndex => OpponentIds.Length - 1;

    public static int GetUnlockedIndex()
    {
        int value = PlayerPrefs.GetInt(UnlockedIndexKey, 0);
        return Mathf.Clamp(value, 0, MaxOpponentIndex);
    }

    public static bool IsUnlocked(int opponentIndex)
    {
        return opponentIndex <= GetUnlockedIndex();
    }

    public static bool TryGetOpponentIndexFromScene(string sceneName, out int opponentIndex)
    {
        for (int i = 0; i < OpponentScenes.Length; i++)
        {
            if (OpponentScenes[i] == sceneName)
            {
                opponentIndex = i;
                return true;
            }
        }

        opponentIndex = -1;
        return false;
    }

    public static bool IsOpponentDefeated(int opponentIndex)
    {
        if (opponentIndex < 0 || opponentIndex > MaxOpponentIndex) return false;

        string key = DefeatedPrefix + OpponentIds[opponentIndex];
        return PlayerPrefs.GetInt(key, 0) == 1;
    }

    public static void MarkWinForScene(string sceneName)
    {
        if (!TryGetOpponentIndexFromScene(sceneName, out int defeatedIndex)) return;

        string defeatedKey = DefeatedPrefix + OpponentIds[defeatedIndex];
        PlayerPrefs.SetInt(defeatedKey, 1);

        int currentUnlocked = GetUnlockedIndex();
        int nextUnlocked = Mathf.Clamp(defeatedIndex + 1, 0, MaxOpponentIndex);
        if (nextUnlocked > currentUnlocked)
        {
            PlayerPrefs.SetInt(UnlockedIndexKey, nextUnlocked);
        }

        PlayerPrefs.Save();
    }

    public static void ResetProgress()
    {
        PlayerPrefs.SetInt(UnlockedIndexKey, 0);
        for (int i = 0; i < OpponentIds.Length; i++)
        {
            PlayerPrefs.DeleteKey(DefeatedPrefix + OpponentIds[i]);
        }

        PlayerPrefs.Save();
    }
}
