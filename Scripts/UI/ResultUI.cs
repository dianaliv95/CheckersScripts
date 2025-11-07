using Photon.Pun;
using TMPro;
using UnityEngine;

public class ResultUI : MonoBehaviourPunCallbacks
{
    [Header("Główny napis (jeden Text TMP)")]
    [SerializeField] private TMP_Text txtRezultat;  // <-- перетащи сюда Twój "TxtRezultat"

    [Header("Ikony (opcjonalnie)")]
    [SerializeField] private GameObject wygranaObj;     // ikona pucharu (możesz zostawić puste)
    [SerializeField] private GameObject przegranaObj;   // ikona przegranej (opcjonalnie)
    [SerializeField] private GameObject remisObj;       // ikona remisu (opcjonalnie)

    [Header("Panel pytania")]
    [SerializeField] private GameObject panelCzyZagrasz;
    [SerializeField] private TMP_Text   txtDialogInfo;

    [Header("Statystyka (opcjonalnie)")]
    [SerializeField] private TMP_Text txtStats;

    private RoomRematch rematch;

    // ---- helpers ----
    private T FindOne<T>() where T : Object
    {
#if UNITY_2023_1_OR_NEWER
        return Object.FindFirstObjectByType<T>();
#else
        return Object.FindObjectOfType<T>();
#endif
    }

    private void SetResultUI(int last)
    {
        // last: 1 = win, 0 = draw, -1 = lose
        string msg = last == 1 ? "Wygrana!" : last == -1 ? "Przegrana!" : "Remis";
        if (txtRezultat) txtRezultat.text = msg;

        if (wygranaObj)   wygranaObj.SetActive(last == 1);
        if (przegranaObj) przegranaObj.SetActive(last == -1);
        if (remisObj)     remisObj.SetActive(last == 0);
    }

    private void SetStatsText()
    {
        if (!txtStats) return;
        int g = PlayerPrefs.GetInt("games",  0);
        int w = PlayerPrefs.GetInt("wins",   0);
        int l = PlayerPrefs.GetInt("losses", 0);
        int d = PlayerPrefs.GetInt("draws",  0);
        txtStats.text = $"Gry: {g}\nWygrane: {w}\nPrzegrane: {l}\nRemisy: {d}";
    }

    // ---- lifecycle ----
    void Start()
    {
        rematch = FindOne<RoomRematch>();

        // ustaw wynik
        int last = PlayerPrefs.GetInt("lastResult", 0);  // 1 / 0 / -1
        SetResultUI(last);

        // panel pytania — wyłączony na starcie
        if (panelCzyZagrasz) panelCzyZagrasz.SetActive(false);
        if (txtDialogInfo)   txtDialogInfo.text = "";

        // statystyka
        SetStatsText();
    }

    // ---- UI callbacks ----
    public void OnClickZagrajPonownie()
    {
        if (!rematch) rematch = FindOne<RoomRematch>();

        // pokaż panel u siebie, a przez RoomRematch pokaż go także u przeciwnika
        if (panelCzyZagrasz) panelCzyZagrasz.SetActive(true);
        if (txtDialogInfo)   txtDialogInfo.text = "";

        if (rematch != null)
            rematch.AskForRematch();  // wyśle RPC do obu stron, żeby panel się pojawił
        else
            Debug.LogWarning("[ResultUI] RoomRematch nie znaleziony.");
    }

    public void OnClickTak()  { if (rematch != null) rematch.SendChoice(true);  }
    public void OnClickNie()  { if (rematch != null) rematch.SendChoice(false); }

    // ---- Wywoływane z RoomRematch (RPC/bezpośrednio) ----
    public void ShowAskPanelRPC()
    {
        if (panelCzyZagrasz) panelCzyZagrasz.SetActive(true);
        if (txtDialogInfo)   txtDialogInfo.text = "";
    }

    public void ShowWaiting()
    {
        if (panelCzyZagrasz) panelCzyZagrasz.SetActive(true);
        if (txtDialogInfo)   txtDialogInfo.text = "Czekam na decyzję przeciwnika…";
    }

    public void ShowDeclinedAndExit()
    {
        if (panelCzyZagrasz) panelCzyZagrasz.SetActive(true);
        if (txtDialogInfo)   txtDialogInfo.text = "Przeciwnik odmówił";
        Invoke(nameof(GoMenu), 1.2f);
    }

    // jeśli RoomRematch zadecyduje o rewanżu, on zrobi PhotonNetwork.LoadLevel("Game")
    // (tu nic nie trzeba dodawać)

    private void GoMenu()
    {
        if (PhotonNetwork.InRoom) PhotonNetwork.LeaveRoom();
        else UnityEngine.SceneManagement.SceneManager.LoadScene("Menu");
    }

    public override void OnLeftRoom()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene("Menu");
    }
}
