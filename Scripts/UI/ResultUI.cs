using Photon.Pun;
using TMPro;
using System.Collections;
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
    [SerializeField] private TMP_Text txtDialogInfo;
    [SerializeField] private string waitingStatusText = "Czekam na decyzję przeciwnika…";
    [SerializeField] private string declinedStatusText = "Przeciwnik odmówił";
    [SerializeField] private string acceptedStatusText = "Obaj gracze zaakceptowali. Ładowanie gry…";
    [Header("Statystyka (opcjonalnie)")]
    [SerializeField] private TMP_Text txtStats;

    private RoomRematch rematch;
    private bool autoAskTriggered;
    private string dialogInitialText = string.Empty;


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

        if (wygranaObj) wygranaObj.SetActive(last == 1);
        if (przegranaObj) przegranaObj.SetActive(last == -1);
        if (remisObj) remisObj.SetActive(last == 0);
    }

    private void SetStatsText()
    {
        if (!txtStats) return;
        int g = PlayerStatsStorage.GetInt("games", 0);
        int w = PlayerStatsStorage.GetInt("wins", 0);
        int l = PlayerStatsStorage.GetInt("losses", 0);
        int d = PlayerStatsStorage.GetInt("draws", 0);
        txtStats.text = $"Gry: {g}\nWygrane: {w}\nPrzegrane: {l}\nRemisy: {d}";
    }

    // ---- lifecycle ----
    void Start()
    {
        rematch = FindOne<RoomRematch>();

        // ustaw wynik
        int last = PlayerStatsStorage.GetInt("lastResult", 0);  // 1 / 0 / -1
        SetResultUI(last);

        // panel pytania — wyłączony na starcie
        if (panelCzyZagrasz) panelCzyZagrasz.SetActive(false);
        if (txtDialogInfo) dialogInitialText = txtDialogInfo.text;

        // statystyka
        SetStatsText();
        TryAutoAskForRematch();
         if (!autoAskTriggered)
            StartCoroutine(WaitForRematchAvailability());
    }
 private IEnumerator WaitForRematchAvailability()
    {
        while (!autoAskTriggered)
        {
            if (!PhotonNetwork.InRoom)
            {
                yield return null;
                continue;
            }

            if (!rematch) rematch = FindOne<RoomRematch>();
            if (!rematch)
            {
                yield return null;
                continue;
            }

            TryAutoAskForRematch();
            if (!autoAskTriggered)
                yield return null;
        }
    }
    private void TryAutoAskForRematch()
    {
        if (autoAskTriggered) return;
        if (!PhotonNetwork.InRoom) return;

        if (!rematch) rematch = FindOne<RoomRematch>();
        if (!rematch) return;

        autoAskTriggered = true;

        if (PhotonNetwork.IsMasterClient)
            rematch.AskForRematch();
    }


    // ---- UI callbacks ----
    public void OnClickZagrajPonownie()
    {
        if (!rematch) rematch = FindOne<RoomRematch>();

        // pokaż panel u siebie, a przez RoomRematch pokaż go także u przeciwnika
        if (panelCzyZagrasz) panelCzyZagrasz.SetActive(true);
        ResetDialogStatus();
        autoAskTriggered = true;

        if (rematch != null)
            rematch.AskForRematch();  // wyśle RPC do obu stron, żeby panel się pojawił
        else
            Debug.LogWarning("[ResultUI] RoomRematch nie znaleziony.");
    }

    public void OnClickTak() { if (rematch != null) rematch.SendChoice(true); }
    public void OnClickNie() { if (rematch != null) rematch.SendChoice(false); }

    // ---- Wywoływane z RoomRematch (RPC/bezpośrednio) ----
    public void ShowAskPanelRPC()
    {
        if (panelCzyZagrasz) panelCzyZagrasz.SetActive(true);
        ResetDialogStatus();
    }

    public void ShowWaiting()
    {
        if (panelCzyZagrasz) panelCzyZagrasz.SetActive(true);
        if (txtDialogInfo) txtDialogInfo.text = waitingStatusText;
    }


    public void ShowDeclinedAndExit()
    {
        if (panelCzyZagrasz) panelCzyZagrasz.SetActive(true);
        if (txtDialogInfo) txtDialogInfo.text = declinedStatusText;
        Invoke(nameof(GoMenu), 1.2f);
    }
     public void ShowAccepted()
    {
        if (panelCzyZagrasz) panelCzyZagrasz.SetActive(true);
        if (txtDialogInfo)   txtDialogInfo.text = acceptedStatusText;
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
     private void ResetDialogStatus()
    {
        if (txtDialogInfo) txtDialogInfo.text = dialogInitialText;
    }
}
