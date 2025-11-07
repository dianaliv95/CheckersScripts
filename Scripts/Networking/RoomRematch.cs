using System.Collections.Generic;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using ExitGames.Client.Photon;

public class RoomRematch : MonoBehaviourPunCallbacks, IOnEventCallback    // <-- ВАЖНО
{
    private const byte EventAskRematch   = 1;
    private const byte EventSetChoice    = 2;
    private const byte EventDeclined     = 3;
    private ResultUI ui;

    // -1 = Nie, 0 = нет ответа, +1 = Tak
    private readonly Dictionary<int, int> choice = new();
    private bool declineBroadcasted;
    private bool declineHandled;
    private bool acceptedBroadcasted;

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
    private static void RaiseEvent(byte eventCode, object content)
    {
        var options = new RaiseEventOptions { Receivers = ReceiverGroup.All };
        PhotonNetwork.RaiseEvent(eventCode, content, options, SendOptions.SendReliable);
    }
    
    // ---------- lifecycle ----------
    private void Awake()
    {
        EnsureUI();
        if (PhotonNetwork.LocalPlayer != null)
            choice[PhotonNetwork.LocalPlayer.ActorNumber] = 0;

        var view = GetComponent<PhotonView>();
        if (view)
        {
            PhotonNetwork.RemoveRPCs(view);
            var method = typeof(PhotonNetwork).GetMethod(
                "UnregisterPhotonView",
                System.Reflection.BindingFlags.Static |
                System.Reflection.BindingFlags.Public |
                System.Reflection.BindingFlags.NonPublic);
            method?.Invoke(null, new object[] { view });
            Destroy(view);
        }
    }

    private void OnEnable()
    {
        PhotonNetwork.AddCallbackTarget(this);
    }

    private void OnDisable()
    {
        PhotonNetwork.RemoveCallbackTarget(this);
    }
    

    // ---------- public API ----------
    public void AskForRematch()
    {
        RaiseEvent(EventAskRematch, null);
    }

    public void SendChoice(bool yes)
    {
        int val = yes ? 1 : -1;
       object[] payload = { PhotonNetwork.LocalPlayer.ActorNumber, val };
        RaiseEvent(EventSetChoice, payload);
    }

     // ---------- Photon callbacks ----------
    public void OnEvent(EventData photonEvent)
    {
        switch (photonEvent.Code)
        {
            case EventAskRematch:
                HandleAskEvent();
                break;
            case EventSetChoice:
                if (photonEvent.CustomData is object[] data && data.Length >= 2)
                {
                    int actor = (int)data[0];
                    int val   = (int)data[1];
                    HandleChoiceEvent(actor, val);
                }
                break;
            case EventDeclined:
                HandleDeclinedEvent();
                break;
        }
    }

    // ---------- logic ----------
     private void HandleAskEvent()
    {
        EnsureUI();

        choice.Clear();
        foreach (Player p in PhotonNetwork.PlayerList)
            choice[p.ActorNumber] = 0;

        declineBroadcasted  = false;
        declineHandled      = false;
        acceptedBroadcasted = false;


        ui?.ShowAskPanelRPC();
    }

    
    private void HandleChoiceEvent(int actorNumber, int val)
    {
        choice[actorNumber] = val;

        // локально показ «Жду…» только у нажавшего Tak
        if (actorNumber == PhotonNetwork.LocalPlayer.ActorNumber && val == 1)
            ui?.ShowWaiting();

        EvaluateState();
    }

  
   private void HandleDeclinedEvent()
    {
        if (declineHandled) return;
        declineHandled = true;
        EnsureUI();
        ui?.ShowDeclinedAndExit();
    }

    private void BroadcastDeclined()
    {
        if (declineBroadcasted) return;
        declineBroadcasted = true;
        RaiseEvent(EventDeclined, null);
    }
    
    private void EvaluateState()
    {
        EnsureUI();
        foreach (var kv in choice)
            if (kv.Value == -1)
            {
                BroadcastDeclined();
                HandleDeclinedEvent();
                return;
            }

        // актуальный список актёров
        var actors = new List<int>();
        foreach (Player p in PhotonNetwork.PlayerList) actors.Add(p.ActorNumber);

        bool allYes = actors.Count > 0;
        foreach (int a in actors)
            if (!choice.TryGetValue(a, out int v) || v != 1) { allYes = false; break; }

         if (allYes && !acceptedBroadcasted)
        {
            acceptedBroadcasted = true;
            ui?.ShowAccepted();
            if (PhotonNetwork.IsMasterClient)
                PhotonNetwork.LoadLevel("Game"); // AutomaticallySyncScene = true
           
        }
        // иначе — ничего: у согласившегося уже «Жду…», у другого — панель с кнопками
    }

    // кто-то вышел со сцены результата → считаем отказом
    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        declineBroadcasted = true;
        HandleDeclinedEvent();
    }
}
