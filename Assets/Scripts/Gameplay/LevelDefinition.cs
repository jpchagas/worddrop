using UnityEngine;

namespace WordDrop.Gameplay
{
    /// <summary>
    /// The tuning of one level. Create more with Assets → Create → Word Drop → Nível
    /// and add them to the GameController's level list in the Game scene.
    /// </summary>
    [CreateAssetMenu(menuName = "Word Drop/Nível", fileName = "Level")]
    public class LevelDefinition : ScriptableObject
    {
        [SerializeField] private int levelNumber = 1;
        [SerializeField] private int boardWidth = 10;
        [SerializeField] private int boardHeight = 20;
        [Tooltip("How many pieces the player gets in this level.")]
        [SerializeField] private int pieces = 40;
        [Tooltip("Seconds between each one-row step of the falling piece.")]
        [SerializeField] private float fallInterval = 0.7f;
        [Tooltip("Chance that an I, T, L or J piece carries a word (4 letters on I, 3 on the others).")]
        [Range(0f, 1f)]
        [SerializeField] private float wordPieceChance = 0.6f;
        [Tooltip("Score needed for 1, 2 and 3 stars.")]
        [SerializeField] private int[] starScores = { 300, 600, 900 };

        public int LevelNumber => levelNumber;
        public int BoardWidth => boardWidth;
        public int BoardHeight => boardHeight;
        public int Pieces => pieces;
        public float FallInterval => fallInterval;
        public float WordPieceChance => wordPieceChance;
        public int ThreeStarScore => starScores[starScores.Length - 1];

        public int StarScore(int star) => starScores[star];

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
