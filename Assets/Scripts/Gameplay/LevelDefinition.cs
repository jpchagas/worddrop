using UnityEngine;

namespace WordDrop.Gameplay
{
    /// <summary>
    /// The rules of one campaign level, of Endless mode or of the Daily Challenge (GDD 4, 5). Levels
    /// are assets in Assets/Content/Levels, listed in the GameController in the
    /// Game scene. Create more with Assets → Create → Word Drop → Nível.
    /// Fields are internal so the editor's level catalog can set them up.
    /// </summary>
    [CreateAssetMenu(menuName = "Word Drop/Nível", fileName = "Level")]
    public class LevelDefinition : ScriptableObject
    {
        [SerializeField] internal int levelNumber = 1;
        [Tooltip("Easy / Medium / Advanced: which pieces are dealt and how many get color hints.")]
        [SerializeField] internal Difficulty difficulty = Difficulty.Easy;
        [Tooltip("Endless mode: no objectives; speed and board pressure grow over time.")]
        [SerializeField] internal bool endless;
        [Tooltip("Short tutorial tip shown in the level briefing.")]
        [TextArea(2, 4)]
        [SerializeField] internal string tip = "";

        [Header("Objectives")]
        [SerializeField] internal LevelObjective[] objectives = { new LevelObjective(ObjectiveType.Score, 500) };
        [Tooltip("Pieces the player can drop; 0 = unlimited.")]
        [SerializeField] internal int moveLimit;
        [Tooltip("Score for 1, 2 and 3 stars, once the objectives are complete (completing gives at least 1 star).")]
        [SerializeField] internal int[] starScores = { 500, 800, 1100 };

        [Header("Board")]
        [SerializeField] internal int boardWidth = 10;
        [SerializeField] internal int boardHeight = 20;
        [Tooltip("Pre-filled rows, bottom row first. '.' is an empty cell.")]
        [SerializeField] internal string[] prefillRows = { };
        [SerializeField] internal GravityMode gravity = GravityMode.Independent;

        [Header("Pieces")]
        [Tooltip("Seconds between each one-row step of the falling piece.")]
        [SerializeField] internal float fallInterval = 0.8f;
        [Tooltip("Chance that an I, T, L, J or I3 piece without a color hint carries a whole word.")]
        [Range(0f, 1f)]
        [SerializeField] internal float wordPieceChance = 0.6f;
        [Tooltip("Pieces dealt first, in order: 'SHAPE LETTERS [ROTATION]', e.g. 'I3 SOL 2'.")]
        [SerializeField] internal string[] pieceSequence = { };

        [Header("Endless only")]
        [Tooltip("Speed up after this many pieces (0 = never).")]
        [SerializeField] internal int speedUpEveryPieces = 10;
        [SerializeField] internal float speedUpFactor = 0.9f;
        [SerializeField] internal float minFallInterval = 0.12f;
        [Tooltip("Push a row of letters up from the bottom every this many pieces (0 = never).")]
        [SerializeField] internal int garbageEveryPieces = 15;
        [SerializeField] internal int garbageStartPiece = 30;

        public int LevelNumber => levelNumber;
        public Difficulty Difficulty => difficulty;
        public bool Endless => endless;
        public string Tip => tip;
        public LevelObjective[] Objectives => objectives;
        public int MoveLimit => moveLimit;
        public int BoardWidth => boardWidth;
        public int BoardHeight => boardHeight;
        public string[] PrefillRows => prefillRows;
        public GravityMode Gravity => gravity;
        public float FallInterval => fallInterval;
        public float WordPieceChance => wordPieceChance;
        public string[] PieceSequence => pieceSequence;
        public int SpeedUpEveryPieces => speedUpEveryPieces;
        public float SpeedUpFactor => speedUpFactor;
        public float MinFallInterval => minFallInterval;
        public int GarbageEveryPieces => garbageEveryPieces;
        public int GarbageStartPiece => garbageStartPiece;

        public int StarsFor(int score)
        {
            int stars = 0;
            while (stars < starScores.Length && score >= starScores[stars])
            {
                stars++;
            }

            return stars;
        }
    }
}
