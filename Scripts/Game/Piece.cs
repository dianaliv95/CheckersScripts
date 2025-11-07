using UnityEngine;

public enum PieceColor { White, Black }

public class Piece : MonoBehaviour
{
    public PieceColor color;
    public bool isKing;

    [Header("Korona (opcjonalnie przypnij w Inspectorze)")]
    [SerializeField] private GameObject crown;   // child "Crown" (Sprite)

    [HideInInspector] public Vector2Int boardPos;

    private void Awake()
    {
        // Если не задано в инспекторе – попробуем найти, даже если объект неактивен
        if (!crown)
        {
            foreach (var t in GetComponentsInChildren<Transform>(true))
                if (t.name == "Crown") { crown = t.gameObject; break; }
        }
        SyncCrown();
    }

    private void OnValidate()
    {
        // Чтобы в редакторе корона соответствовала флажку isKing
        if (Application.isPlaying) return;
        if (!crown && transform != null)
        {
            foreach (var t in GetComponentsInChildren<Transform>(true))
                if (t.name == "Crown") { crown = t.gameObject; break; }
        }
        SyncCrown();
    }

    public void PromoteToKing()
    {
        if (isKing) return;
        SetKing(true);
    }

    /// <summary>Единая точка смены статуса дамки (и визуала).</summary>
    public void SetKing(bool value)
    {
        if (isKing == value) { SyncCrown(); return; }
        isKing = value;
        SyncCrown();
    }

    /// <summary>Принудительно синхронизировать вид короны с флажком isKing.</summary>
    public void SyncCrown()
    {
        if (crown) crown.SetActive(isKing);
    }
}
