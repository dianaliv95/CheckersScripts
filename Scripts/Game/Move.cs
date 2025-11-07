using UnityEngine;

public struct Move
{
    public Vector2Int from;
    public Vector2Int to;
    public Vector2Int? captured; // pozycja zbitego pionka (jeśli było bicie)

    public Move(Vector2Int f, Vector2Int t, Vector2Int? c = null)
    {
        from = f; to = t; captured = c;
    }
}
