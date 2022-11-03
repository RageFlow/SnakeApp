using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace SnakeApp
{
    internal class GameMenu
    {
        public static int GameSize = 10; // Blocks
        public static int GameSpeed = 500; // Milliseconds

        public static int GameBonus = 20; // Ticks
        public static bool GameBonusActive = false;
        public static bool GameBonusAdd = false;
        public static int GameBonusCounter = 0;
        public static string GameSizeInputText { get; set; } = "10";
        public static string GameSpeedInputText { get; set; } = "500";
        public static string GameBonusInputText { get; set; } = "20";

        public static void HideInputFields()
        {
            GameSizeInputBox.Visibility = Visibility.Hidden;
            GameSpeedInputBox.Visibility = Visibility.Hidden;
            GameBonusInputBox.Visibility = Visibility.Hidden;
        }

        public static void ShowInputFields()
        {
            GameSizeInputBox.Visibility = Visibility.Visible;
            GameSpeedInputBox.Visibility = Visibility.Visible;
            GameBonusInputBox.Visibility = Visibility.Visible;
        }

        public static void UpdateGameSettings()
        {
            if (Int32.TryParse(GameSizeInputText, out int sizeResult) && sizeResult >= 1) // Set GameSize to user inputted size
            {
                GameSize = sizeResult;
            }
            else
            {
                GameSize = 10;
                GameSizeInput.Text = "10";
            }

            if (Int32.TryParse(GameSpeedInputText, out int speedResult) && speedResult > 1) // Set GameSize to user inputted size
            {
                GameMenu.GameSpeed = speedResult;
            }
            else
            {
                GameSpeed = 400;
                GameSpeedInput.Text = "400";
            }

            if (Int32.TryParse(GameBonusInputText, out int bonusResult) && bonusResult > 1) // Set GameSize to user inputted size
            {
                GameBonus = bonusResult;
            }
            else
            {
                GameBonus = 20;
                GameBonusInput.Text = "20";
            }

            if (GameBonusInputCheckbox.IsChecked.HasValue && GameBonusInputCheckbox.IsChecked.Value)
            {
                GameBonusActive = true;
            }
            else
            {
                GameBonusActive = false;
            }
        }
    }
}
