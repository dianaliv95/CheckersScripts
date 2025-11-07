using System.Collections.Generic;
using UnityEngine;

public class Board : MonoBehaviour
{
    public const int Size = 8;

    [Header("Prefaby")]
    public GameObject whitePiecePrefab;
    public GameObject blackPiecePrefab;
    public Transform  piecesRoot;

    [Header("Pozycjonowanie")]
    public Vector2 boardOrigin = new(-3.5f, -3.5f);
    public float  cellSize     = 1f;

    [Tooltip("Środek ciemnego pola a1 (lewy-dolny róg planszy).")]
    public Transform a1Mark;
    [Tooltip("Środek ciemnego pola h8 (prawy-górny róg planszy).")]
    public Transform h8Mark;

    [Header("Kolor a1 (0,0)")]
    public bool a1IsDark = true;

    [Header("Reguły")]
    public bool allowBackwardCapture = true; // простые шашки могут бить назад

    [Header("UI – podpowiedzi")]
    public GameObject hintPrefab;

    private readonly List<GameObject> activeHints = new();
    private Piece[,] grid = new Piece[Size, Size];

    // ------------------- lifecycle -------------------
    void Awake()
    {
        if (!piecesRoot)
        {
            var go = new GameObject("PiecesRoot");
            go.transform.SetParent(transform, false);
            piecesRoot = go.transform;
        }
    }

    void OnValidate()
    {
        if (a1Mark && h8Mark)
        {
            boardOrigin = a1Mark.position;
            float dx = (h8Mark.position.x - a1Mark.position.x) / 7f;
            float dy = (h8Mark.position.y - a1Mark.position.y) / 7f;
            cellSize = (Mathf.Abs(dx) + Mathf.Abs(dy)) * 0.5f;
        }
    }

    // ------------------- konwersje -------------------
    public static bool InBounds(Vector2Int p) => p.x >= 0 && p.x < Size && p.y >= 0 && p.y < Size;

    public Vector3 CellToWorld(int x, int y) =>
        new Vector3(boardOrigin.x + x * cellSize,
                    boardOrigin.y + y * cellSize,
                    0.1f);

    public Vector3 BoardToWorld(Vector2Int p) => CellToWorld(p.x, p.y);

    private bool IsPlayable(int x, int y)
    {
        int s = (x + y) & 1;                // a1 даёт 0
        return a1IsDark ? (s == 0) : (s == 1);
    }

    public Vector2Int WorldToBoard(Vector3 w)
    {
        int x = Mathf.RoundToInt((w.x - boardOrigin.x) / cellSize);
        int y = Mathf.RoundToInt((w.y - boardOrigin.y) / cellSize);
        return new Vector2Int(x, y);
    }

    // ------------------- debug siatki -------------------
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        for (int x = 0; x < Size; x++)
        for (int y = 0; y < Size; y++)
            Gizmos.DrawWireCube(CellToWorld(x, y), new Vector3(cellSize, cellSize, 0));
    }

    // ------------------- hints -------------------
    public void ShowHints(IEnumerable<Move> moves)
    {
        ClearHints();
        if (!hintPrefab) return;

        foreach (var m in moves)
        {
            var go = Instantiate(hintPrefab, BoardToWorld(m.to), Quaternion.identity, transform);
            // подгоняем кружок под клетку
            if (go.TryGetComponent(out SpriteRenderer sr))
                FitSpriteToCell(sr, 0.85f);
            activeHints.Add(go);
        }
    }

    public void ClearHints()
    {
        foreach (var h in activeHints)
            if (h) Destroy(h);
        activeHints.Clear();
    }

    // ------------------- API -------------------
    public Piece GetPiece(Vector2Int p) => InBounds(p) ? grid[p.x, p.y] : null;

    public void SetupInitial(PieceColor bottomColor)
    {
        ClearHints();
        foreach (Transform t in piecesRoot) Destroy(t.gameObject);
        grid = new Piece[Size, Size];

        PieceColor topColor = bottomColor == PieceColor.White ? PieceColor.Black : PieceColor.White;

        PlaceRows(bottomColor, 0);
        PlaceRows(topColor,   5);
    }

    void PlaceRows(PieceColor color, int startRow)
    {
        for (int y = startRow; y < startRow + 3; y++)
        for (int x = 0; x < Size; x++)
        {
            if (!IsPlayable(x, y)) continue;

            var p  = new Vector2Int(x, y);
            var pf = (color == PieceColor.White) ? whitePiecePrefab : blackPiecePrefab;
            var go = Instantiate(pf, BoardToWorld(p), Quaternion.identity, piecesRoot);

            if (go.TryGetComponent(out SpriteRenderer sr))
                FitSpriteToCell(sr, 0.90f);

            var piece = go.GetComponent<Piece>();
            piece.color    = color;
            piece.boardPos = p;
            piece.isKing   = false;

            grid[x, y] = piece;
        }
    }

    // подгон спрайта под клетку
    void FitSpriteToCell(SpriteRenderer sr, float factor)
    {
        if (!sr) return;
        float d = Mathf.Max(sr.bounds.size.x, sr.bounds.size.y);
        if (d <= 0f) return;
        float target = cellSize * factor;
        float k = target / d;
        sr.transform.localScale *= k;
    }

    public void ApplyMove(Move m, bool makeKingIfArrived = true)
    {
        var piece = grid[m.from.x, m.from.y];
        grid[m.from.x, m.from.y] = null;

        if (m.captured.HasValue)
        {
            Vector2Int cap = m.captured.Value;
            var capturedPiece = grid[cap.x, cap.y];
            if (capturedPiece) Destroy(capturedPiece.gameObject);
            grid[cap.x, cap.y] = null;
        }

        grid[m.to.x, m.to.y] = piece;
        piece.boardPos = m.to;
        piece.transform.position = BoardToWorld(m.to);

        if (makeKingIfArrived && !piece.isKing)
        {
            if (piece.color == PieceColor.White && m.to.y == Size - 1) piece.PromoteToKing();
            if (piece.color == PieceColor.Black && m.to.y == 0)       piece.PromoteToKing();
        }
    }

    public List<Move> GetLegalMoves(PieceColor player)
    {
        var allMoves     = new List<Move>();
        var captureMoves = new List<Move>();

        for (int x = 0; x < Size; x++)
        for (int y = 0; y < Size; y++)
        {
            var piece = grid[x, y];
            if (piece == null || piece.color != player) continue;

            var pos   = new Vector2Int(x, y);
            var moves = GetMovesForPiece(piece, pos);

            foreach (var mv in moves)
                if (mv.captured.HasValue) captureMoves.Add(mv);
                else                      allMoves.Add(mv);
        }
        return (captureMoves.Count > 0) ? captureMoves : allMoves;
    }

    // ====== Ходы для простой и «летающей» дамки ======
    public List<Move> GetMovesForPiece(Piece piece, Vector2Int pos)
    {
        return piece.isKing ? GetKingMoves(piece, pos)
                            : GetManMoves(piece, pos);
    }

    // простая шашка
    private List<Move> GetManMoves(Piece piece, Vector2Int pos)
    {
        var res = new List<Move>();
        int dir = (piece.color == PieceColor.White) ? +1 : -1;

        Vector2Int[] steps = {
            new(+1, dir), new(-1, dir),
            new(+1, -dir), new(-1, -dir)
        };

        foreach (var s in steps)
        {
            // обычный шаг только вперёд
            bool forwardOk = s.y == dir;
            if (forwardOk)
            {
                Vector2Int to = pos + s;
                if (InBounds(to) && GetPiece(to) == null && IsPlayable(to.x, to.y))
                    res.Add(new Move(pos, to, null));
            }

            // взятие: вперёд всегда, назад — по правилу
            Vector2Int over = pos + s;
            Vector2Int land = pos + s + s;

            if (InBounds(land) && GetPiece(land) == null && IsPlayable(land.x, land.y))
            {
                var overP = InBounds(over) ? GetPiece(over) : null;
                if (overP != null && overP.color != piece.color)
                {
                    bool captureDirOK = allowBackwardCapture || s.y == dir;
                    if (captureDirOK) res.Add(new Move(pos, land, over));
                }
            }
        }
        return res;
    }

    // «летающая» дамка
    private List<Move> GetKingMoves(Piece piece, Vector2Int pos)
    {
        var res = new List<Move>();
        Vector2Int[] dirs = { new(1,1), new(-1,1), new(1,-1), new(-1,-1) };

        foreach (var d in dirs)
        {
            // 1) свободный полёт без взятия
            var p = pos + d;
            while (InBounds(p) && GetPiece(p) == null && IsPlayable(p.x, p.y))
            {
                res.Add(new Move(pos, p, null));
                p += d;
            }

            // 2) проверка взятия на дистанции
            if (!InBounds(p)) continue;

            var first = GetPiece(p);
            if (first == null || first.color == piece.color) continue; // либо пусто (дальше занято), либо своя

            // за одной вражеской — должны идти пустые поля (любое количество)
            var land = p + d;
            while (InBounds(land) && GetPiece(land) == null && IsPlayable(land.x, land.y))
            {
                res.Add(new Move(pos, land, p)); // бьём «first», садимся на любую пустую дальше
                land += d;
            }
        }

        // если есть хоть одно взятие — оставить только взятия (обязательные)
        var caps = res.FindAll(m => m.captured.HasValue);
        return (caps.Count > 0) ? caps : res;
    }

    // для проверки «нет ходов/нет фигур»
    public bool HasAnyMoves(PieceColor player) => GetLegalMoves(player).Count > 0;

    public bool HasAnyPieces(PieceColor player)
    {
        for (int x = 0; x < Size; x++)
        for (int y = 0; y < Size; y++)
            if (grid[x, y] && grid[x, y].color == player) return true;
        return false;
    }
}
