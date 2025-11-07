using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using Photon.Realtime;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class GameManager : MonoBehaviourPunCallbacks
{
    [Header("Refs")]
    [SerializeField] private Board board;
    [SerializeField] private Camera cam;
    [SerializeField] private TMP_Text txtTura;
    [SerializeField] private TMP_Text txtTimer;
    [SerializeField] private TMP_Text txtKtoChodzi;
    [SerializeField] private TMP_Text txtInfo;

    [Header("Ustawienia")]
    public int turnSeconds = 30;

    [Header("Timer UI")]
    [SerializeField] private int   warningThresholdSeconds = 5;
    [SerializeField] private Color timerNormalColor  = Color.white;
    [SerializeField] private Color timerWarningColor = new Color(0.9f, 0.2f, 0.2f);

    private PieceColor myColor;
    private PieceColor currentTurn;
    private float timer;
    private bool gameOver;
    private bool leaveInProgress;

    private string whiteName = "—";
    private string blackName = "—";
    private string myName    = "Ty";

    private Vector2Int? selectedPos;
    private readonly List<Move> currentSelectableMoves = new();

    // ===== lifecycle =====
    private void Awake()
    {
        if (!board) board = FindObjectOfType<Board>();
        if (!cam)   cam   = Camera.main ? Camera.main : FindObjectOfType<Camera>();
    }

    private void Start()
    {
        PhotonNetwork.AutomaticallySyncScene = true;

        EnsureNickname();
        AssignColorsAndNames();

        // ВАЖНО: старт партии делает только мастер
        if (PhotonNetwork.IsMasterClient)
        {
            // страховка от «залипших» сообщений из предыдущей комнаты
            PhotonNetwork.CleanRpcBufferIfMine(photonView);
            photonView.RPC(nameof(RPC_BeginNewGame), RpcTarget.All);
        }
    }

    [PunRPC]
    private void RPC_BeginNewGame()
    {
        gameOver        = false;
        leaveInProgress = false;

        // начнём всегда с белых
        currentTurn = PieceColor.White;
        timer       = turnSeconds;

        // камера: белые смотрят «снизу», чёрных поворачиваем
        if (myColor == PieceColor.Black && cam)
            cam.transform.rotation = Quaternion.Euler(0f, 0f, 180f);
        else if (cam)
            cam.transform.rotation = Quaternion.identity;

        // очистить выбор/подсказки и переставить фигуры
        selectedPos = null;
        currentSelectableMoves.Clear();
        if (board)
        {
            board.ClearHints();
            board.SetupInitial(PieceColor.White);
        }

        if (txtTimer) txtTimer.color = timerNormalColor;
        UpdateTurnUI();
    }

    private void Update()
    {
        if (gameOver) return;

        if (IsMyTurn())
        {
            timer -= Time.deltaTime;

            int secs = Mathf.CeilToInt(Mathf.Max(0f, timer));
            if (txtTimer)
            {
                txtTimer.text  = $"Czas: {secs}";
                txtTimer.color = (secs <= warningThresholdSeconds) ? timerWarningColor : timerNormalColor;
            }

            if (timer <= 0f)
            {
                photonView.RPC(nameof(RPC_TimeoutTurn), RpcTarget.All);
                return;
            }
        }

        if (IsMyTurn() && PointerClicked())
        {
            if (!cam || !board) return;

            Vector2 screen = PointerPosition();
            float zToPlane0 = -cam.transform.position.z;
            Vector3 world = cam.ScreenToWorldPoint(new Vector3(screen.x, screen.y, zToPlane0));
            Vector2Int cell = board.WorldToBoard(world);

            TrySelectOrMove(cell);
        }
    }

    // ===== input =====
    private bool PointerClicked()
    {
#if ENABLE_INPUT_SYSTEM
        if (Mouse.current?.leftButton?.wasPressedThisFrame == true) return true;
        if (Touchscreen.current?.primaryTouch?.press?.wasPressedThisFrame == true) return true;
        return false;
#else
        return Input.GetMouseButtonDown(0) ||
               (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began);
#endif
    }

    private Vector2 PointerPosition()
    {
#if ENABLE_INPUT_SYSTEM
        if (Mouse.current != null) return Mouse.current.position.ReadValue();
        var touch = Touchscreen.current?.primaryTouch;
        if (touch != null) return touch.position.ReadValue();
        return Vector2.zero;
#else
        return (Input.touchCount > 0) ? (Vector2)Input.GetTouch(0).position
                                      : (Vector2)Input.mousePosition;
#endif
    }

    // ===== UI =====
    private void UpdateTurnUI()
    {
        bool mine = IsMyTurn();
        if (txtTura)  txtTura.text  = mine ? "Twoja tura" : "Tura przeciwnika";

        if (!mine && txtTimer)
        {
            txtTimer.text  = "Czas: —";
            txtTimer.color = timerNormalColor;
        }

        bool whiteTurn = currentTurn == PieceColor.White;
        string kolor   = whiteTurn ? "Białe" : "Czarne";
        string ktoNick = whiteTurn ? whiteName : blackName;

        bool toJaTeraz = (whiteTurn && myColor == PieceColor.White) ||
                         (!whiteTurn && myColor == PieceColor.Black);

        if (txtKtoChodzi)
            txtKtoChodzi.text = toJaTeraz
                ? $"Ruch: {kolor} (Ty)"
                : $"Ruch: {kolor} — przeciwnik: {ktoNick}";
    }

    // ===== PUN: nicki/strony =====
    private void EnsureNickname()
    {
        if (string.IsNullOrEmpty(PhotonNetwork.NickName))
            PhotonNetwork.NickName = $"Gracz{Random.Range(1000, 9999)}";
        myName = PhotonNetwork.NickName;
    }

    private void AssignColorsAndNames()
    {
        var players = new List<Player>(PhotonNetwork.PlayerList);
        players.Sort((a, b) => a.ActorNumber.CompareTo(b.ActorNumber));

        whiteName = players.Count >= 1 ? SafeNick(players[0]) : myName;
        blackName = players.Count >= 2 ? SafeNick(players[1]) : (whiteName == myName ? "—" : myName);

        myColor = (players.Count == 0 || PhotonNetwork.LocalPlayer == players[0])
                  ? PieceColor.White
                  : PieceColor.Black;
    }

    private static string SafeNick(Player p) =>
        string.IsNullOrEmpty(p?.NickName) ? $"Gracz{p?.ActorNumber}" : p.NickName;

    // ===== tury i ruchy =====
    private bool IsMyTurn() => currentTurn == myColor;

    private void ResetSelection()
    {
        selectedPos = null;
        currentSelectableMoves.Clear();
        board?.ClearHints();
    }

    private void TrySelectOrMove(Vector2Int p)
    {
        if (!Board.InBounds(p)) return;

        if (!selectedPos.HasValue)
        {
            var piece = board.GetPiece(p);
            if (piece == null || piece.color != myColor) return;

            var legal = board.GetLegalMoves(myColor);
            currentSelectableMoves.Clear();
            currentSelectableMoves.AddRange(legal.FindAll(m => m.from == p));

            if (currentSelectableMoves.Count > 0)
            {
                selectedPos = p;
                board.ShowHints(currentSelectableMoves);
            }
            return;
        }

        foreach (var mv in currentSelectableMoves)
        {
            if (mv.to != p) continue;

            ExecuteMoveLocal(mv);

            if (PhotonNetwork.InRoom)
            {
                photonView.RPC(nameof(RPC_ApplyMove), RpcTarget.Others,
                    mv.from.x, mv.from.y, mv.to.x, mv.to.y,
                    mv.captured.HasValue ? 1 : 0,
                    mv.captured.HasValue ? mv.captured.Value.x : -1,
                    mv.captured.HasValue ? mv.captured.Value.y : -1);
            }
            return;
        }

        ResetSelection();
        TrySelectOrMove(p);
    }

    private void ExecuteMoveLocal(Move m)
    {
        if (gameOver) return;

        board.ApplyMove(m);
        board.ClearHints();

        var movedPiece   = board.GetPiece(m.to);
        var moreCaptures = CaptureContinuations(movedPiece);

        if (m.captured.HasValue && moreCaptures.Count > 0)
        {
            currentTurn = movedPiece.color;
            timer       = turnSeconds;
            if (txtTimer) txtTimer.color = timerNormalColor;

            selectedPos = m.to;
            currentSelectableMoves.Clear();
            currentSelectableMoves.AddRange(moreCaptures);
            board.ShowHints(moreCaptures);
            UpdateTurnUI();
            return;
        }

        currentTurn = (currentTurn == PieceColor.White) ? PieceColor.Black : PieceColor.White;
        timer       = turnSeconds;
        if (txtTimer) txtTimer.color = timerNormalColor;
        ResetSelection();

        if (!board.HasAnyMoves(currentTurn))
        {
            var loser  = currentTurn;
            int result = (loser == PieceColor.White) ? -1 : +1; // -1=white win, +1=black win

            // БЕЗ буферизации!
            photonView.RPC(nameof(RPC_EndGame), RpcTarget.All, result);
            return;
        }

        UpdateTurnUI();
    }

    [PunRPC]
    private void RPC_ApplyMove(int fx, int fy, int tx, int ty, int capFlag, int cx, int cy)
    {
        if (gameOver) return;
        var from = new Vector2Int(fx, fy);
        var to   = new Vector2Int(tx, ty);
        Vector2Int? cap = (capFlag == 1) ? new Vector2Int(cx, cy) : (Vector2Int?)null;
        ExecuteMoveLocal(new Move(from, to, cap));
    }

    [PunRPC]
    private void RPC_TimeoutTurn()
    {
        if (gameOver) return;
        currentTurn = (currentTurn == PieceColor.White) ? PieceColor.Black : PieceColor.White;
        timer = turnSeconds;
        if (txtTimer) txtTimer.color = timerNormalColor;
        ResetSelection();
        UpdateTurnUI();
    }

    private List<Move> CaptureContinuations(Piece piece)
    {
        if (piece == null) return new List<Move>();
        var all = board.GetMovesForPiece(piece, piece.boardPos);
        return all.FindAll(m => m.captured.HasValue);
    }

    // ===== koniec gry =====
    [PunRPC]
    private void RPC_EndGame(int resultFlag)
    {
        if (gameOver) return;
        gameOver = true;

        var winner = resultFlag < 0 ? PieceColor.White : PieceColor.Black;
        bool iWon  = (winner == myColor);
        PlayerStatsStorage.SetInt("lastResult", iWon ? 1 : (resultFlag == 0 ? 0 : -1)); // 1 win, 0 draw, -1 lose
        PlayerStatsStorage.Increment("games");
       
        if (resultFlag == 0)
            PlayerStatsStorage.Increment("draws");
        else if (iWon)
            PlayerStatsStorage.Increment("wins");
        else
             PlayerStatsStorage.Increment("losses");
        PlayerStatsStorage.Save();

        if (PhotonNetwork.IsMasterClient)
            PhotonNetwork.LoadLevel("Result");
    }

    // ===== Wyjście в меню =====
    public void OnClickWyjdz()
    {
        if (leaveInProgress) return;
        StartCoroutine(ExitToMenu("Wyszedłeś z gry"));
    }

    private IEnumerator ExitToMenu(string info)
    {
        leaveInProgress = true;
        gameOver = true;
        board?.ClearHints();
        if (txtInfo && !string.IsNullOrEmpty(info)) txtInfo.text = info;

        if (PhotonNetwork.InRoom) PhotonNetwork.LeaveRoom();
        while (PhotonNetwork.InRoom) yield return null;

        if (PhotonNetwork.IsConnected) PhotonNetwork.Disconnect();
        while (PhotonNetwork.IsConnected) yield return null;

        SceneManager.LoadScene("Menu");
    }

    // ===== PUN callbacks =====
    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        AssignColorsAndNames();
        UpdateTurnUI();
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        if (leaveInProgress) return;
        string nick = SafeNick(otherPlayer);
        StartCoroutine(ExitToMenu($"Przeciwnik opuścił grę ({nick})"));
    }

    public override void OnLeftRoom() { }
}
