namespace WordDrop.Core
{
    /// <summary>
    /// Every player-facing text (pt-BR). Kept in one place so the game can be
    /// translated later without hunting through scenes and scripts.
    /// </summary>
    public static class Strings
    {
        public const string Loading = "Carregando...";

        public const string ConsentTitle = "Antes de começar";
        public const string ConsentBody =
            "Para jogar Word Drop, leia e aceite os nossos Termos de Uso e a nossa Política de Privacidade.";
        public const string TermsOfUse = "Termos de Uso";
        public const string PrivacyPolicy = "Política de Privacidade";
        public const string AcceptAndContinue = "Aceitar e continuar";

        public const string Play = "Jogar";
        public const string DailyChallenge = "Desafio Diário";
        public const string HowToPlay = "Como Jogar";
        public const string Settings = "Configurações";
        public const string ComingSoon = "Em breve!";

        public const string MusicOn = "Música: Ligada";
        public const string MusicOff = "Música: Desligada";
        public const string Close = "Fechar";

        public const string Credits = "Música: MANYUKO - Burger Flip";

        // Gameplay
        public const string Score = "Pontos";
        public const string LevelFormat = "Nível {0}";
        public const string PiecesFormat = "Peças: {0}";
        public const string WordFound = "Palavra encontrada!";
        public const string WordsFound = "Palavras encontradas!";
        public const string ComboFormat = "Combo x{0}!";
        public const string RowCleared = "Linha completa!";
        public const string RowsCleared = "Linhas completas!";
        public const string OneRow = "1 linha";
        public const string RowsFormat = "{0} linhas";
        public const string Next = "Próxima";

        public const string Paused = "Pausado";
        public const string Resume = "Continuar";
        public const string RestartLevel = "Reiniciar nível";
        public const string MainMenu = "Menu principal";

        public const string LevelComplete = "Nível concluído!";
        public const string GreatJob = "Muito bem!";
        public const string Best = "Recorde";
        public const string WordsFoundCount = "Palavras encontradas";
        public const string NextLevel = "Próximo nível";
        public const string PlayAgain = "Jogar novamente";

        public const string GameOver = "Fim de jogo";
        public const string BoardFull = "O tabuleiro encheu antes da primeira estrela. Forme palavras para abrir espaço!";
        public const string NotEnoughPoints = "As peças acabaram antes da primeira estrela.";
        public const string TryAgain = "Tentar novamente";

        /// <summary>Brazilian number format: 1240 → "1.240".</summary>
        public static string FormatNumber(int value) =>
            value.ToString("N0", System.Globalization.CultureInfo.InvariantCulture).Replace(',', '.');
    }
}
