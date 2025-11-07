using Photon.Pun;
using UnityEngine;

public class PhotonBootstrap : MonoBehaviour
{
    void Awake()
    {
        PhotonNetwork.AutomaticallySyncScene = true; // включаем один раз на старте
        DontDestroyOnLoad(gameObject);               // чтобы не потерялось после загрузки сцен
    }
}
