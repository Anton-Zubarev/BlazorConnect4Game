
namespace BlazorConnect4Game.FourOrderGame;


public class GameState
{
    private readonly object _syncRoot = new();

    public event Func<string, Task>? OnChange;
    public event Func<Task>? OnReset;
    public GameState()
    {
        CalculateWinningPlaces();
    }

    /// <summary>
    /// Indicate whether a player has won, the game is a tie, or game in ongoing
    /// </summary>
    public enum WinState
    {
        No_Winner = 0,
        Player1_Wins = 1,
        Player2_Wins = 2,
        Tie = 3
    }

    public byte LastLandingSpot;
    public byte LastColumn;

    public bool IsXTurn { get; set; } = true;

    // Храним ID игроков
    public string? PlayerXId { get; set; }
    public string? PlayerOId { get; set; }

    /// <summary>
    /// 1 Or 2 - The player whose turn it is.  By default, player 1 starts first
    /// </summary>
    public int PlayerTurn => TheBoard.Count(x => x != 0) % 2 + 1;

    /// <summary>
    /// Number of turns completed and pieces played so far in the game
    /// </summary>
    public int CurrentTurn { get { return TheBoard.Count(x => x != 0); } }

    public readonly List<int[]> WinningPlaces = new();

    public void CalculateWinningPlaces()
    {

        // Horizontal rows
        for (byte row = 0; row < 6; row++)
        {

            byte rowCol1 = (byte)(row * 7);
            byte rowColEnd = (byte)((row + 1) * 7 - 1);
            byte checkCol = rowCol1;
            while (checkCol <= rowColEnd - 3)
            {
                WinningPlaces.Add(new int[] {
                    checkCol,
                    (byte)(checkCol + 1),
                    (byte)(checkCol + 2),
                    (byte)(checkCol + 3)
                    });
                checkCol++;
            }

        }

        // Vertical Columns
        for (byte col = 0; col < 7; col++)
        {

            byte colRow1 = col;
            byte colRowEnd = (byte)(35 + col);
            byte checkRow = colRow1;
            while (checkRow <= 14 + col)
            {
                WinningPlaces.Add(new int[] {
                    checkRow,
                    (byte)(checkRow + 7),
                    (byte)(checkRow + 14),
                    (byte)(checkRow + 21)
                    });
                checkRow += 7;
            }

        }

        // forward slash diagonal "/"
        for (byte col = 0; col < 4; col++)
        {

            // starting column must be 0-3
            byte colRow1 = (byte)(21 + col);
            byte colRowEnd = (byte)(35 + col);
            byte checkPos = colRow1;
            while (checkPos <= colRowEnd)
            {
                WinningPlaces.Add(new int[] {
                    checkPos,
                    (byte)(checkPos - 6),
                    (byte)(checkPos - 12),
                    (byte)(checkPos - 18)
                    });
                checkPos += 7;
            }

        }

        // back slash diaganol "\"
        for (byte col = 0; col < 4; col++)
        {

            // starting column must be 0-3
            byte colRow1 = (byte)(0 + col);
            byte colRowEnd = (byte)(14 + col);
            byte checkPos = colRow1;
            while (checkPos <= colRowEnd)
            {
                WinningPlaces.Add(new int[] {
                    checkPos,
                    (byte)(checkPos + 8),
                    (byte)(checkPos + 16),
                    (byte)(checkPos + 24)
                    });
                checkPos += 7;
            }

        }


    }

    /// <summary>
    /// Check the state of the board for a winning scenario
    /// </summary>
    /// <returns>0 - no winner, 1 - player 1 wins, 2 - player 2 wins, 3 - draw</returns>
    public WinState CheckForWin()
    {

        // Exit immediately if less than 7 pieces are played
        if (TheBoard.Count(x => x != 0) < 7) return WinState.No_Winner;

        foreach (var scenario in WinningPlaces)
        {

            if (TheBoard[scenario[0]] == 0) continue;

            if (TheBoard[scenario[0]] ==
                TheBoard[scenario[1]] &&
                TheBoard[scenario[1]] ==
                TheBoard[scenario[2]] &&
                TheBoard[scenario[2]] ==
                TheBoard[scenario[3]]) return (WinState)TheBoard[scenario[0]];

        }

        if (TheBoard.Count(x => x != 0) == 42) return WinState.Tie;

        return WinState.No_Winner;

    }

    /// <summary>
    /// Поменять UI игроков местами
    /// </summary>
    /// <param name="playerId"></param>
    public void SwapUIPlayer()
    {
        lock (_syncRoot)
        {
            var x = PlayerXId;
            var o = PlayerOId;

            PlayerXId = o;
            PlayerOId = x;
            IsXTurn = !IsXTurn;
        }
    }

    /// <summary>
    /// Takes the current turn and places a piece in the 0-indexed column requested
    /// </summary>
    /// <param name="column">0-indexed column to place the piece into</param>
    /// <param name="playerId">Игрок из UI</param>
    /// <returns>The final array index where the piece resides</returns>
    public byte PlayPiece(byte column, string playerId)
    {

        // Check for a current win
        if (CheckForWin() != 0) throw new ArgumentException("Game is over");

        // Check the column
        if (TheBoard[column] != 0) throw new ArgumentException("Column is full");

        LastColumn = column;
        // Drop the piece in
        var landingSpot = column;

        lock (_syncRoot)
        {
            // 1. Регистрация игроков (кто первый зашел, тот за X)
            if (PlayerXId == null) PlayerXId = playerId;
            else if (PlayerOId == null && playerId != PlayerXId) PlayerOId = playerId;

            // 2. Проверка: ходит ли сейчас этот игрок?
            bool isXPlayer = (playerId == PlayerXId);
            bool isOPlayer = (playerId == PlayerOId);

            if (IsXTurn && !isXPlayer) return 255; // Ход X, но нажал не X
            if (!IsXTurn && !isOPlayer) return 255; // Ход O, но нажал не O

            for (var i = column; i < 42; i += 7)
            {
                if (TheBoard[landingSpot + 7] != 0) break;
                landingSpot = i;
            }

            TheBoard[landingSpot] = PlayerTurn;

            LastLandingSpot = ConvertLandingSpotToRow(landingSpot);
            
            IsXTurn = !IsXTurn; // Передаем ход
            NotifyStateChanged(playerId);

            return LastLandingSpot;
        }

    }

    private void NotifyStateChanged(string playerId) => OnChange?.Invoke(playerId);
    private void NotifyStateChanged1(string playerId)
    {
        var handlers = OnChange?.GetInvocationList();

        if (handlers != null)
        {
            var tasks = handlers.Select(h => ((Func<string, Task>)h)(playerId));

            Task.WhenAll(tasks).ConfigureAwait(false).GetAwaiter().GetResult(); // параллельно ждать все обработчики
        }
        //OnChange?.Invoke(playerId);
    }

    public List<int> TheBoard { get; private set; } = new List<int>(new int[42]);

    public async Task ResetBoard(string playerId = null)
    {
        lock (_syncRoot)
        {
            if (PlayerXId == null) PlayerXId = playerId;
            else if (PlayerOId == null && playerId != PlayerXId) PlayerOId = playerId;

            TheBoard = new List<int>(new int[42]);
            IsXTurn = true;
        }
        //await OnReset?.Invoke(); // можно (так как дальше нет кода, это последняя строка), но ждет только последний обработчик

        var handlers = OnReset?.GetInvocationList();

        if (handlers != null)
        {
            var tasks = handlers.Select(h => ((Func<Task>)h)());

            await Task.WhenAll(tasks); // параллельно ждать все обработчики
        }
    }

    /// <summary>
    /// Позиция в столбике
    /// </summary>
    /// <param name="landingSpot"></param>
    /// <returns></returns>
    private byte ConvertLandingSpotToRow(byte landingSpot)
    {

        return (byte)(Math.Floor(landingSpot / (decimal)7) + 1);

    }

}