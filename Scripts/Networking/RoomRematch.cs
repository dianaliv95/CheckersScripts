using System.Collections.Generic;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

public class RoomRematch : MonoBehaviourPunCallbacks   // <-- ВАЖНО
{
    private ResultUI ui;

    // -1 = Nie, 0 = нет ответа, +1 = Tak
    private readonly Dictionary<int, int> choice = new();

    // ---------- helpers ----------
    private T FindOne<T>() where T : Object
    {
#if UNITY_2023_1_OR_NEWER
        return Object.FindFirstObjectByType<T>();
#else
        return Object.FindObjectOfType<T>();
#endif
    }
    private void EnsureUI() { if (!ui) ui = FindOne<ResultUI>(); }

    // ---------- lifecycle ----------
    private void Awake()
    {
        EnsureUI();
        choice[PhotonNetwork.LocalPlayer.ActorNumber] = 0;
    }

    // ---------- public API ----------
    public void AskForRematch()
    {
        photonView.RPC(nameof(RPC_ShowAskPanel), RpcTarget.All);
    }

    public void SendChoice(bool yes)
    {
        int val = yes ? 1 : -1;
        photonView.RPC(nameof(RPC_SetChoice), RpcTarget.All,
            PhotonNetwork.LocalPlayer.ActorNumber, val);
    }

    // ---------- RPC ----------
    [PunRPC]
    private void RPC_ShowAskPanel()
    {
        EnsureUI();

        choice.Clear();
        foreach (Player p in PhotonNetwork.PlayerList)
            choice[p.ActorNumber] = 0;

        ui?.ShowAskPanelRPC();
    }

    [PunRPC]
    private void RPC_SetChoice(int actorNumber, int val)
    {
        choice[actorNumber] = val;

        // локально показ «Жду…» только у нажавшего Tak
        if (actorNumber == PhotonNetwork.LocalPlayer.ActorNumber && val == 1)
            ui?.ShowWaiting();

        EvaluateState();
    }

    [PunRPC]
    private void RPC_Declined()
    {
        EnsureUI();
        ui?.ShowDeclinedAndExit();
    }

    // ---------- logic ----------
    private void EvaluateState()
    {
        // отказал кто-то?
        foreach (var kv in choice)
            if (kv.Value == -1)
            {
                photonView.RPC(nameof(RPC_Declined), RpcTarget.All);
                return;
            }

        // актуальный список актёров
        var actors = new List<int>();
        foreach (Player p in PhotonNetwork.PlayerList) actors.Add(p.ActorNumber);

        bool allYes = true;
        foreach (int a in actors)
            if (!choice.TryGetValue(a, out int v) || v != 1) { allYes = false; break; }

        if (allYes)
        {
            if (PhotonNetwork.IsMasterClient)
                PhotonNetwork.LoadLevel("Game"); // AutomaticallySyncScene = true
            return;
        }
        // иначе — ничего: у согласившегося уже «Жду…», у другого — панель с кнопками
    }

    // кто-то вышел со сцены результата → считаем отказом
    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        photonView.RPC(nameof(RPC_Declined), RpcTarget.All);
    }
}
