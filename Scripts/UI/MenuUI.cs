using UnityEngine;
using TMPro;

public class MenuUI : MonoBehaviour
{
    [Header("Panele")]
    [SerializeField] GameObject panelMenu;
    [SerializeField] GameObject panelLobby;
    [SerializeField] GameObject panelStatystyki;

    [Header("Teksty statystyk (TMP)")]
    [SerializeField] TMP_Text txtWygrane;
    [SerializeField] TMP_Text txtPrzegrane;
    [SerializeField] TMP_Text txtRemisy;
    [SerializeField] TMP_Text txtGry;

    [SerializeField] PhotonLauncher launcher; // przypnij w Inspectorze!

    void Awake()
    {
        // На случай если забыли привязать
#if UNITY_2022_2_OR_NEWER
        if (!launcher) launcher = Object.FindFirstObjectByType<PhotonLauncher>();
#else
        if (!launcher) launcher = FindObjectOfType<PhotonLauncher>();
#endif
        if (!panelMenu || !panelLobby || !panelStatystyki)
            Debug.LogError("[MenuUI] Nie przypięte panele!");
    }

    void Start() => ShowMenu();

    public void OnClickGraj()
    {
        Show(panelLobby);
        launcher?.ConnectAndQuickMatch();
    }

    public void OnClickStatystyki()
    {
        Debug.Log("[MenuUI] Statystyki click");
        Show(panelStatystyki);

        int w = PlayerPrefs.GetInt("wins", 0);
        int l = PlayerPrefs.GetInt("losses", 0);
        int d = PlayerPrefs.GetInt("draws", 0);
        int g = PlayerPrefs.GetInt("games", 0);

        txtWygrane?.SetText($"Wygrane: {w}");
        txtPrzegrane?.SetText($"Przegrane: {l}");
        txtRemisy?.SetText($"Remisy: {d}");
        txtGry?.SetText($"Gry: {g}");
    }

    public void OnClickWrocZStatystyk() => ShowMenu();

    public void OnClickWyjdz()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    void ShowMenu() => Show(panelMenu);

    void Show(GameObject target)
    {
        if (!target) return;
        panelMenu?.SetActive(false);
        panelLobby?.SetActive(false);
        panelStatystyki?.SetActive(false);
        target.SetActive(true);
    }
}
