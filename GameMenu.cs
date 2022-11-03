using System;
using System.Threading.Tasks;
using static SnakeApp.MainWindow;

namespace SnakeApp
{
    public class GameMenu
    {
        public static int GameSize = 14; // Blocks
        public static int GameSpeed = 300; // Milliseconds

        public static int GameBonus = 20; // Ticks
        public bool GameBonusActive = false;
        public bool GameBonusAdd = false;
        public int GameBonusCounter = 0;
        public string GameSizeInputText { get; set; } = GameSize.ToString();
        public string GameSpeedInputText { get; set; } = GameSpeed.ToString();
        public string GameBonusInputText { get; set; } = GameBonus.ToString();

        public int Highscore = 0;

        public void UpdateGameSettings()
        {
            if (Int32.TryParse(GameSizeInputText, out int sizeResult) && sizeResult >= 1) // Set GameSize to user inputted size
            {
                GameSize = sizeResult;
            }
            else
            {
                GameSize = 10;
                AppWindow.GameSizeInput.Text = "10";
            }

            if (Int32.TryParse(GameSpeedInputText, out int speedResult) && speedResult > 1) // Set GameSize to user inputted size
            {
                GameSpeed = speedResult;
            }
            else
            {
                GameSpeed = 400;
                AppWindow.GameSpeedInput.Text = "400";
            }

            if (Int32.TryParse(GameBonusInputText, out int bonusResult) && bonusResult >= 1) // Set GameSize to user inputted size
            {
                GameBonus = bonusResult;
            }
            else
            {
                GameBonus = 20;
                AppWindow.GameBonusInput.Text = "20";
            }

            if (AppWindow.GameBonusInputCheckbox.IsChecked.HasValue && AppWindow.GameBonusInputCheckbox.IsChecked.Value)
            {
                GameBonusActive = true;
            }
            else
            {
                GameBonusActive = false;
            }
        }

        public void UpdateHighscore(int score)
        {
            Highscore = score > gameMenu.Highscore ? score : gameMenu.Highscore;
            if (gameMenu.Highscore > 0)
            {
                AppWindow.Highscore.Content = $"Highscore: {gameMenu.Highscore * 1000}";
            }
            else
            {
                AppWindow.Highscore.Content = "";
            }

            AppWindow.ScoreBoardScore.Content = "";
        }
    }
}
