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
    [SerializeField] private TMP_Text txtDialogInfo;
    [SerializeField] private string waitingStatusText = "Czekam na decyzję przeciwnika…";
    [SerializeField] private string declinedStatusText = "Przeciwnik odmówił";
    [SerializeField] private string acceptedStatusText = "Obaj gracze zaakceptowali. Ładowanie gry…";
    [Header("Statystyka (opcjonalnie)")]
    [SerializeField] private TMP_Text txtStats;

    private RoomRematch rematch;  
    private string dialogInitialText = string.Empty;
    private bool exitRequested;
    private bool menuSceneLoading;


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
    }    

    // ---- UI callbacks ----
    public void OnClickZagrajPonownie()
    {
        if (!rematch) rematch = FindOne<RoomRematch>();

        if (!PhotonNetwork.InRoom || rematch == null)
        {
            Debug.LogWarning("[ResultUI] Brak połączenia z pokojem lub RoomRematch niegotowy.");
            return;
        }
        exitRequested = false;
        menuSceneLoading = false;
        if (panelCzyZagrasz) panelCzyZagrasz.SetActive(true);
        ResetDialogStatus();
        rematch.AskForRematch();        
    }

    public void OnClickTak()
    {
        if (!rematch) rematch = FindOne<RoomRematch>();
        rematch?.SendChoice(true);
    }

    public void OnClickNie()
    {
        if (!rematch) rematch = FindOne<RoomRematch>();
        rematch?.SendChoice(false);
    }

    // ---- Wywoływane z RoomRematch (RPC/bezpośrednio) ----
    public void ShowAskPanelRPC()
    {
        if (panelCzyZagrasz) panelCzyZagrasz.SetActive(true);
        ResetDialogStatus();
        if (!exitRequested)
        {
            CancelInvoke(nameof(BeginExitToMenu));
            menuSceneLoading = false;
        }
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
        if (!exitRequested)
        {
            exitRequested = true;
            Invoke(nameof(BeginExitToMenu), 1.2f);
        }
    }
    public void ShowAccepted()
    {
        if (panelCzyZagrasz) panelCzyZagrasz.SetActive(true);
        if (txtDialogInfo) txtDialogInfo.text = acceptedStatusText;
    }
    // jeśli RoomRematch zadecyduje o rewanżu, on zrobi PhotonNetwork.LoadLevel("Game")
    // (tu nic nie trzeba dodawać)

     private void BeginExitToMenu()
    {
        if (menuSceneLoading) return;

        if (PhotonNetwork.InRoom)
        {
            PhotonNetwork.LeaveRoom();
        }
        else
        {
            LoadMenuScene();
        }
    }

    public override void OnLeftRoom()
    {
        CancelInvoke(nameof(BeginExitToMenu));
        LoadMenuScene();
    }
    private void ResetDialogStatus()
    {
        if (txtDialogInfo) txtDialogInfo.text = dialogInitialText;
    }
     private void LoadMenuScene()
    {
        if (menuSceneLoading) return;
        menuSceneLoading = true;
        UnityEngine.SceneManagement.SceneManager.LoadScene("Menu");
    }
}
