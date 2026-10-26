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

        // Main menu
        public const string Play = "Jogar";
        public const string PlayLevelFormat = "Jogar · Nível {0}";
        public const string Endless = "Modo Infinito";
        public const string EndlessBestFormat = "Recorde: {0}";
        public const string DailyChallenge = "Desafio Diário";
        public const string HowToPlay = "Como Jogar";
        public const string Settings = "Configurações";
        public const string ComingSoon = "Em breve!";

        public const string MusicOn = "Música: Ligada";
        public const string MusicOff = "Música: Desligada";
        public const string SoundOn = "Sons: Ligados";
        public const string SoundOff = "Sons: Desligados";
        public const string VibrationOn = "Vibração: Ligada";
        public const string VibrationOff = "Vibração: Desligada";
        public const string Close = "Fechar";

        public const string Credits = "Música: MANYUKO - Burger Flip";

        // Gameplay HUD
        public const string Score = "Pontos";
        public const string LevelFormat = "Nível {0}";
        public const string MovesFormat = "Jogadas: {0}";
        public const string SpeedFormat = "Velocidade {0}";
        public const string Next = "Próxima";
        public const string WordsShort = "Palavras";
        public const string RowsShort = "Linhas";
        public const string ChainsShort = "Combos";

        // Objectives
        public const string Objective = "Objetivo";
        public const string ObjectiveScore = "Faça {0} pontos";
        public const string ObjectiveWord = "Forme {0} palavra";
        public const string ObjectiveWords = "Forme {0} palavras";
        public const string ObjectiveRow = "Complete {0} linha";
        public const string ObjectiveRows = "Complete {0} linhas";
        public const string ObjectiveChain = "Faça {0} combo";
        public const string ObjectiveChains = "Faça {0} combos";
        public const string EndlessGoal = "Faça o máximo de pontos!";
        public const string Start = "Começar";

        // Clear banners
        public const string WordFound = "Palavra encontrada!";
        public const string WordsFound = "Palavras encontradas!";
        public const string RowCleared = "Linha completa!";
        public const string RowsCleared = "Linhas completas!";
        public const string ComboFormat = "Combo ×{0}!";
        public const string OneRow = "1 linha";
        public const string RowsFormat = "{0} linhas";
        public const string MultiwordBonus = "Bônus de palavras!";
        public const string MultiwordFormat = "{0} palavras de uma vez";

        // Pause and confirmation
        public const string Paused = "Pausado";
        public const string Resume = "Continuar";
        public const string RestartLevel = "Reiniciar";
        public const string MainMenu = "Menu principal";
        public const string ConfirmRestart = "Reiniciar? O progresso desta partida será perdido.";
        public const string ConfirmMainMenu = "Voltar ao menu? O progresso desta partida será perdido.";
        public const string Yes = "Sim";
        public const string No = "Não";

        // Results
        public const string LevelComplete = "Nível concluído!";
        public const string GreatJob = "Muito bem!";
        public const string ObjectiveDone = "Objetivo concluído";
        public const string Best = "Recorde";
        public const string WordsFoundCount = "Palavras encontradas";
        public const string NextLevel = "Próximo nível";
        public const string PlayAgain = "Jogar novamente";
        public const string CampaignFinished = "Você completou todos os níveis disponíveis!";

        public const string GameOver = "Fim de jogo";
        public const string BoardFull = "Não há espaço para a próxima peça.";
        public const string OutOfMoves = "Suas jogadas acabaram antes de completar o objetivo.";
        public const string NewRecord = "Novo recorde!";
        public const string TryAgain = "Tentar novamente";

        // Level map
        public const string LevelMap = "Mapa de níveis";
        public const string World1 = "Mundo 1 · Primeiras Palavras";
        public const string Back = "Voltar";

        // How to play
        public const string HowToPlayStep1Title = "Solte as peças";
        public const string HowToPlayStep1 = "As peças caem do alto. Toque para girar, arraste para mover e deslize para baixo para soltar.";
        public const string HowToPlayStep2Title = "Forme palavras";
        public const string HowToPlayStep2 = "Junte letras na horizontal (da esquerda para a direita) ou na vertical (de cima para baixo). Acentos contam!";
        public const string HowToPlayStep3Title = "Limpe o tabuleiro";
        public const string HowToPlayStep3 = "Palavras e linhas completas somem. As letras de cima caem e podem formar combos.";
        public const string HowToPlayStep4Title = "Cores e reforços";
        public const string HowToPlayStep4 = "Peças da mesma cor formam uma palavra juntas. Use a Bomba, o Embaralhar e a Estrela quando precisar.";
        public const string GotIt = "Entendi!";

        // Daily challenge
        public const string DailyGoal = "Encontre o máximo de palavras que conseguir!";
        public const string DailyNotPlayed = "Você ainda não jogou hoje.";
        public const string DailyWordsFormat = "Palavras {0}/{1}";
        public const string DailyTimeFormat = "Tempo {0}";
        public const string DailyDone = "Desafio concluído!";
        public const string ClaimReward = "Resgatar recompensa";
        public const string RewardClaimed = "Recompensa resgatada!";
        public const string DailyRewardToast = "+1 Bomba, +1 Embaralhar e +1 Estrela!";

        // Boosters
        public const string Bomb = "Bomba";
        public const string Shuffle = "Embaralhar";
        public const string Star = "Estrela";

        // Achievements and statistics
        public const string Achievements = "Conquistas";
        public const string AchievementUnlockedFormat = "Conquista desbloqueada: {0}!";
        public const string RewardFormat = "+{0} {1}";
        public const string AchievementsCountFormat = "{0} de {1} conquistas";
        public const string Statistics = "Estatísticas";
        public const string StatsFormat =
            "Palavras formadas: {0}\nPalavras diferentes: {1}\nPalavra mais longa: {2}\n" +
            "Linhas completas: {3}\nMaior combo: {4}\nPeças jogadas: {5}\nDesafios diários: {6}";

        // Leaderboards
        public const string Leaderboards = "Recordes";
        public const string LeaderboardEndless = "Modo Infinito · Top 10";
        public const string LeaderboardDaily = "Desafio Diário · Hoje";
        public const string LeaderboardCampaign = "Campanha · Melhores pontuações";
        public const string LeaderboardEmpty = "Nenhuma pontuação ainda.";
        public const string LeaderboardOnlineSoon = "Recordes on-line em breve.";

        // Word meanings
        public const string Meanings = "Significados";
        public const string MeaningsEmpty = "Nenhuma palavra formada nesta partida.";

        private static readonly string[] Months =
        {
            "janeiro", "fevereiro", "março", "abril", "maio", "junho",
            "julho", "agosto", "setembro", "outubro", "novembro", "dezembro",
        };

        /// <summary>pt-BR long date: "22 de outubro de 2026".</summary>
        public static string FormatDate(System.DateTime date) => $"{date.Day} de {Months[date.Month - 1]} de {date.Year}";

        /// <summary>Minutes and seconds: 84 → "01:24".</summary>
        public static string FormatTime(int seconds) => $"{seconds / 60:00}:{seconds % 60:00}";

        /// <summary>Brazilian number format: 1240 → "1.240".</summary>
        public static string FormatNumber(int value) =>
            value.ToString("N0", System.Globalization.CultureInfo.InvariantCulture).Replace(',', '.');

        /// <summary>A multiplier in tenths as pt-BR text: 15 → "1,5", 20 → "2".</summary>
        public static string FormatMultiplier(int tenths) =>
            tenths % 10 == 0 ? (tenths / 10).ToString() : $"{tenths / 10},{tenths % 10}";
    }
}
