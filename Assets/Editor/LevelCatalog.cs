using System.IO;
using UnityEditor;
using UnityEngine;
using WordDrop.Gameplay;

namespace WordDrop.EditorTools
{
    /// <summary>
    /// Starting values for the 10 levels of world 1, "Primeiras Palavras"
    /// (GDD 5.3), for Endless (GDD 4.2) and for the Daily Challenge (GDD 4.3). Assets are created only when
    /// missing, so tuning done in the Inspector is never overwritten; delete a
    /// level asset to get these defaults back.
    /// </summary>
    public static class LevelCatalog
    {
        public const string Folder = "Assets/Content/Levels";
        public const string EndlessPath = Folder + "/Endless.asset";
        public const string DailyPath = Folder + "/Daily.asset";

        public static string LevelPath(int number) => $"{Folder}/Level{number:00}.asset";

        public static void CreateMissing()
        {
            Directory.CreateDirectory(Folder);
            for (int n = 1; n <= GameSession.CampaignLevelCount; n++)
            {
                if (AssetDatabase.LoadAssetAtPath<LevelDefinition>(LevelPath(n)) == null)
                {
                    AssetDatabase.CreateAsset(Campaign(n), LevelPath(n));
                }
            }

            if (AssetDatabase.LoadAssetAtPath<LevelDefinition>(EndlessPath) == null)
            {
                AssetDatabase.CreateAsset(Endless(), EndlessPath);
            }

            if (AssetDatabase.LoadAssetAtPath<LevelDefinition>(DailyPath) == null)
            {
                AssetDatabase.CreateAsset(Daily(), DailyPath);
            }

            AssetDatabase.SaveAssets();
        }

        private static LevelObjective Goal(ObjectiveType type, int target) => new LevelObjective(type, target);

        private static LevelDefinition Campaign(int n)
        {
            var level = ScriptableObject.CreateInstance<LevelDefinition>();
            level.levelNumber = n;
            // GDD 5.2: difficulty grows in steps. Levels 1–3 Easy, 4–7 Medium, 8–10 Advanced.
            level.difficulty = n <= 3 ? Difficulty.Easy : n <= 7 ? Difficulty.Medium : Difficulty.Advanced;

            switch (n)
            {
                case 1:
                    level.objectives = new[] { Goal(ObjectiveType.Score, 500) };
                    level.starScores = new[] { 500, 800, 1100 };
                    level.fallInterval = 1.0f;
                    level.wordPieceChance = 0.75f;
                    // Guided start: words to rotate into place.
                    level.pieceSequence = new[] { "I3 SOL 2", "I3 MAR 3", "T LUAE 2", "I3 PÃO 2", "L MELA 3", "I CASA 2", "J RUAS 2" };
                    level.tip = "Toque para girar a peça, arraste para os lados para mover e para baixo para descer. " +
                                "Um toque rápido para baixo solta a peça. Forme palavras!";
                    break;
                case 2:
                    level.objectives = new[] { Goal(ObjectiveType.Words, 3) };
                    level.starScores = new[] { 300, 600, 900 };
                    level.fallInterval = 0.95f;
                    level.wordPieceChance = 0.75f;
                    level.pieceSequence = new[] { "I3 OVO 3", "I3 CÉU 2", "I3 MÃE 2", "I3 FIM 3" };
                    level.tip = "Palavras curtas também valem! Os acentos contam: MÃE precisa de um Ã.";
                    break;
                case 3:
                    level.objectives = new[] { Goal(ObjectiveType.Rows, 2) };
                    level.starScores = new[] { 300, 600, 900 };
                    level.fallInterval = 0.9f;
                    level.wordPieceChance = 0.5f;
                    level.prefillRows = new[] { "WXKQ..YKXW" };
                    level.pieceSequence = new[] { "O SOLA 0" };
                    level.tip = "Preencha uma linha inteira para limpá-la, como no Tetris. Solte a primeira peça no buraco!";
                    break;
                case 4:
                    level.objectives = new[] { Goal(ObjectiveType.Score, 1200) };
                    level.starScores = new[] { 1200, 1600, 2000 };
                    level.fallInterval = 0.85f;
                    level.tip = "Peças mais variadas. A sombra mostra onde a peça vai cair, e o quadro \"Próxima\" mostra a seguinte.";
                    break;
                case 5:
                    level.objectives = new[] { Goal(ObjectiveType.Chains, 1) };
                    level.starScores = new[] { 400, 700, 1000 };
                    level.fallInterval = 0.85f;
                    // Dropping the first piece straight down forms SOL; when it
                    // clears, E and L fall next to M and form MEL: a combo.
                    level.prefillRows = new[] { ".MSO......", "..EL......" };
                    level.pieceSequence = new[] { "I3 UIL 1" };
                    level.tip = "Quando uma palavra some, as letras de cima caem e podem formar outra: é um combo, " +
                                "e vale mais pontos! Solte a primeira peça direto para baixo.";
                    break;
                case 6:
                    level.objectives = new[] { Goal(ObjectiveType.Words, 5) };
                    level.starScores = new[] { 700, 1100, 1500 };
                    level.fallInterval = 0.8f;
                    level.tip = "As palavras valem na horizontal (da esquerda para a direita) e na vertical (de cima para baixo).";
                    break;
                case 7:
                    level.objectives = new[] { Goal(ObjectiveType.Score, 1500) };
                    level.moveLimit = 20;
                    level.starScores = new[] { 1500, 1900, 2300 };
                    level.fallInterval = 0.8f;
                    level.wordPieceChance = 0.75f;
                    level.tip = "Você tem só 20 jogadas! Aproveite cada peça.";
                    break;
                case 8:
                    level.objectives = new[] { Goal(ObjectiveType.Rows, 3), Goal(ObjectiveType.Words, 5) };
                    level.starScores = new[] { 1200, 1700, 2200 };
                    level.fallInterval = 0.75f;
                    level.tip = "Dois objetivos ao mesmo tempo: complete linhas e forme palavras.";
                    break;
                case 9:
                    level.objectives = new[] { Goal(ObjectiveType.Score, 2500) };
                    level.starScores = new[] { 2500, 3000, 3500 };
                    level.fallInterval = 0.5f;
                    level.tip = "As peças caem mais rápido agora. Use a sombra para mirar!";
                    break;
                default:
                    level.objectives = new[]
                    {
                        Goal(ObjectiveType.Score, 2000), Goal(ObjectiveType.Words, 6),
                        Goal(ObjectiveType.Rows, 2), Goal(ObjectiveType.Chains, 1),
                    };
                    level.starScores = new[] { 2000, 2600, 3200 };
                    level.fallInterval = 0.65f;
                    level.tip = "Fim do primeiro mundo: mostre tudo o que aprendeu!";
                    break;
            }

            return level;
        }

        private static LevelDefinition Endless()
        {
            var rules = ScriptableObject.CreateInstance<LevelDefinition>();
            rules.levelNumber = 0;
            rules.endless = true;
            rules.difficulty = Difficulty.Easy; // Rises with speed during the run.
            rules.objectives = new LevelObjective[0];
            rules.starScores = new int[0];
            rules.fallInterval = 0.8f;
            rules.wordPieceChance = 0.5f;
            rules.speedUpEveryPieces = 10;
            rules.speedUpFactor = 0.9f;
            rules.minFallInterval = 0.12f;
            rules.garbageEveryPieces = 15;
            rules.garbageStartPiece = 30;
            rules.tip = "Faça o máximo de pontos. As peças aceleram e, de tempos em tempos, uma linha de letras sobe do fundo!";
            return rules;
        }

        /// <summary>
        /// The Daily Challenge (GDD 4.3): the same seeded pieces for everyone,
        /// form 12 words within 40 pieces. Boosters are off for fairness.
        /// </summary>
        private static LevelDefinition Daily()
        {
            var rules = ScriptableObject.CreateInstance<LevelDefinition>();
            rules.levelNumber = 0;
            rules.difficulty = Difficulty.Medium;
            rules.objectives = new[] { Goal(ObjectiveType.Words, 12) };
            rules.moveLimit = 40;
            rules.starScores = new int[0];
            rules.fallInterval = 0.8f;
            rules.wordPieceChance = 0.6f;
            rules.speedUpEveryPieces = 0;
            rules.garbageEveryPieces = 0;
            rules.tip = "Todos jogam com as mesmas peças hoje. Os reforços ficam desligados para ser justo!";
            return rules;
        }
    }
}
