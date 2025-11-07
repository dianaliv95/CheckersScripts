using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

public class PhotonLauncher : MonoBehaviourPunCallbacks
{
    [SerializeField] private string gameVersion = "1.0";
    [SerializeField] private GameObject panelLobby; // przypnij z Menu
    [SerializeField] private byte maxPlayersPerRoom = 2;

    void Awake()
    {
        PhotonNetwork.AutomaticallySyncScene = true;
    }

    public void ConnectAndQuickMatch()
    {
        if (panelLobby) panelLobby.SetActive(true);

        if (PhotonNetwork.IsConnected)
        {
            PhotonNetwork.JoinRandomRoom();
        }
        else
        {
            PhotonNetwork.GameVersion = gameVersion;
            PhotonNetwork.ConnectUsingSettings();
        }
    }

    public override void OnConnectedToMaster()
    {
        PhotonNetwork.JoinRandomRoom();
    }

    public override void OnJoinRandomFailed(short returnCode, string message)
    {
        RoomOptions opts = new RoomOptions { MaxPlayers = maxPlayersPerRoom };
        PhotonNetwork.CreateRoom(null, opts);
    }

    public override void OnJoinedRoom()
    {
        // Kiedy pokój ma 2 graczy → Master ładuje Game
        if (PhotonNetwork.CurrentRoom.PlayerCount == maxPlayersPerRoom && PhotonNetwork.IsMasterClient)
        {
            PhotonNetwork.LoadLevel("Game");
        }
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        if (PhotonNetwork.CurrentRoom.PlayerCount == maxPlayersPerRoom && PhotonNetwork.IsMasterClient)
        {
            PhotonNetwork.LoadLevel("Game");
        }
    }
}
