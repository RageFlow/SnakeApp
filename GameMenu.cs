using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Security.Principal;
using System.Threading.Tasks;
using System.Windows.Documents;
using System.Windows.Input;
using static SnakeApp.MainWindow;
using static System.Formats.Asn1.AsnWriter;
using static System.Net.Mime.MediaTypeNames;

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
        public string GamePlayerNameInputText { get; set; } = "Player";
        private string highscorePath = @"..\SnakeHighscores.txt";
        public ObservableCollection<Score> Highscores = new();

        public void UpdateGameSettings()
        {
            if (Int32.TryParse(GameSizeInputText, out int sizeResult) && sizeResult >= 3 && sizeResult < 1000) // Set GameSize to user inputted size
            {
                GameSize = sizeResult;
            }
            else
            {
                GameSize = 10;
                AppWindow.GameSizeInput.Text = "10";
            }

            if (Int32.TryParse(GameSpeedInputText, out int speedResult) && speedResult >= 1 && speedResult < 10000) // Set GameSize to user inputted size
            {
                GameSpeed = speedResult;
            }
            else
            {
                GameSpeed = 400;
                AppWindow.GameSpeedInput.Text = "400";
            }

            if (Int32.TryParse(GameBonusInputText, out int bonusResult) && bonusResult >= 1 && bonusResult < 1000) // Set GameSize to user inputted size
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
            if (score > 0)
            {
                AppWindow.Highscore.Content = $"Score: {GamePlayerNameInputText} - {score * 1000}"; // Show Last achieved thread

                // New Score
                var ko = new Score() {
                    Number = score * 1000,
                    PlayerName = GamePlayerNameInputText,
                    Bonus = GameBonusActive == true ? GameBonus : 0,
                    Size = GameSize,
                    Speed = GameSpeed 
                };
                Highscores.Add(ko); // Adding score for File Write

                SaveHighScore(); // Write to File

                Highscores.Remove(ko); // Remove Score for UI Update Short

                Score Yikes = Highscores.Where(x => x.Number < score * 1000).FirstOrDefault() ?? new Score(); // Get Score just below in score from new score

                if (Yikes != null && Yikes.Number > 0) // Error Checking
                {
                    Highscores.Insert(Highscores.IndexOf(Yikes), ko); // Insert new score to Top 10 at above any beaten scores
                }
                else
                {
                    Highscores.Add(ko); // Add Score to All Highscore
                }
            }
            else
            {
                AppWindow.Highscore.Content = ""; // Clear Current score if no score was achieved
            }

            AppWindow.ScoreBoardScore.Content = ""; // Clear game score (The one visible while game is active)
        }

        public void SaveHighScore()
        {
            string outputJSON = Newtonsoft.Json.JsonConvert.SerializeObject(Highscores, Newtonsoft.Json.Formatting.Indented); // Serialize Highscore
            File.WriteAllText(highscorePath, outputJSON + Environment.NewLine); // Write Highscore to File
        }

        public void LoadHighscores()
        {
            if (File.Exists(highscorePath)) // Check if highscore file exists
            {
                String JSONtxt = File.ReadAllText(highscorePath); // JsonText based on file
                var scores = Newtonsoft.Json.JsonConvert.DeserializeObject<IEnumerable<Score>>(JSONtxt); // Deserialize file text to List

                if (scores != null) // Error Checking
                {
                    Highscores = new(); // Reset 
                    foreach (var item in scores.OrderByDescending(x => x.Number)) // Add file scores desc to Highscores
                    {
                        Highscores.Add(new Score() {
                            Number = item.Number,
                            PlayerName = item.PlayerName,
                            Bonus = item.Bonus,
                            Size = item.Size,
                            Speed = item.Speed });
                    }
                }
            }
        }
    }

    public class Score
    {
        public string PlayerName { get; set; } = "";
        public int Number { get; set; }
        public int Bonus { get; set; }
        public int Size { get; set; }
        public int Speed { get; set; }
    }
}
