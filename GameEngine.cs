using System;
using static SnakeApp.MainWindow;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows;
using SnakeApp;
using System.Linq;

public class GameEngine : GameFunktions, IGameFunktions
{
    public static readonly object DirectionLock = new object();
    private static AutoResetEvent _blockGameThread = new AutoResetEvent(true);
    private static ManualResetEvent _GameThread = new ManualResetEvent(false);

    private List<Food> food = new();
    private List<SnakePart> snake = new List<SnakePart>();
    private int foodCollected { get; set; }

    public static Direction GameSnakeDirection = Direction.Right;
    public static Direction IgnoreDirection = Direction.Left;

    bool SnakeAlive = false;
    public bool GameActive = false;

    public static void Resume() => _GameThread.Set();
    public static void Pause() => _GameThread.Reset();

    public void ResetGame()
    {
        System.Windows.Application.Current.Dispatcher.Invoke((Action)delegate // "GridLengthConverter" & "SnakeGrid" is owned by default WPF Thread
        {
            if (snake != null && snake.Count > 0) // This section is only neccessary because of WPF's shorty Witchcraft....
            {
                Polygon ko = AppWindow.SnakeTail;
                Border koParent = (Border)ko.Parent;

                Grid kow = AppWindow.SnakeEyes;
                Border kowParent = (Border)kow.Parent;

                SnakePart ko3 = snake.Where(x => x.Part != null && x.Part.Uid == koParent.Uid).FirstOrDefault() ?? new SnakePart();
                if (ko3.Part != null)
                {
                    ko3.Part.Child = null;
                }
                SnakePart ko4 = snake.Where(x => x.Part != null && x.Part.Uid == kowParent.Uid).FirstOrDefault() ?? new SnakePart();
                if (ko4.Part != null)
                {
                    ko4.Part.Child = null;
                }

                AppWindow.SnakeEyeHolder.Child = AppWindow.SnakeEyes;
                AppWindow.SnakeTailHolder.Child = AppWindow.SnakeTail;
            }
            AppWindow.HideStopButton();

            gameMenu.UpdateHighscore(foodCollected);

            // Shows input fields
            AppWindow.ShowInputFields();

            // Resetting menu buttons to default
            AppWindow.SetupForNoGame();

            // Resetting props
            AppWindow.ClearGameArea();

            food = new();
            snake = new();
            foodCollected = 0;
            GameSnakeDirection = Direction.Right;
            IgnoreDirection = Direction.Left;
            SnakeAlive = false;
            GameActive = false;
            //Resetting props

            gameMenu.UpdateGameSettings();

            GridLengthConverter gridLengthConverter = new GridLengthConverter();

            for (int i = 0; i < GameMenu.GameSize; i++)
            {
#pragma warning disable CS8605 // Unboxing a possibly null value from WPF's "Engine".
                AppWindow.SnakeGrid.RowDefinitions.Add(new RowDefinition() { Height = (GridLength)gridLengthConverter.ConvertFrom("*") });
                AppWindow.SnakeGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = (GridLength)gridLengthConverter.ConvertFrom("*") });
#pragma warning restore CS8605 // Unboxing a possibly null value from WPF's "Engine".
            }
        });
    }

    public void StopGame()
    {
        SnakeAlive = false;
        GameActive = false;
        ResetGame(); // Reset game for new run
        Resume(); // Starts GameTimer (Activates _GameThread which we use for pausing the game)

        AppWindow.HideStopButton();

        AppWindow.ShowInputFields();
    }

    public void StartGame()
    {
        AppWindow.HideInputFields(); // Hide input fields when game is active

        ResetGame(); // Reset game for new run
        SnakeAlive = true;
        GameActive = true;
        Resume(); // Starts GameTimer (Activates _GameThread which we use for pausing the game)

        Thread timerThread = new Thread(GameTimer);
        Thread controllerThread = new Thread(() => GameController());
        timerThread.Start();
        controllerThread.Start();
    }

    public override void GameTimer()
    {
        while (GameActive)
        {
            _GameThread.WaitOne(); // Check for pause
            Thread.Sleep(GameMenu.GameSpeed);
            _blockGameThread.Set();
        }
    }

    private void GameController()
    {
        SetupGame(); // Game Setup

        while (SnakeAlive)
        {
            _blockGameThread.WaitOne();
            _GameThread.WaitOne(); // Check for pause

            CheckIfAddFood();

            UpdateSnake(GameSnakeDirection);

            CheckIfColOnSnake();

            CheckIfColOnFood();

            CheckIfWon();
        }

        ResetGame(); // If snake dies!
    }

    public override void SetupGame() // First init of game after user pressed the Play Button
    {
        int middle = GameMenu.GameSize / 2;

        System.Windows.Application.Current.Dispatcher.Invoke((Action)delegate
        {
            AppWindow.HideMenu();
        });

        for (int i = 0; i < 3; i++)
        {
            System.Windows.Application.Current.Dispatcher.Invoke((Action)delegate
            {
                Border part = new Border();
                //part.Name = $"snake{part.PersistId}";
                part.Uid = Guid.NewGuid().ToString();

                if (i == 0)
                {
                    part.Background = Brushes.DarkRed;
                    Grid HeadSection = AppWindow.SnakeEyes;

                    AppWindow.SnakeEyeHolder.Child = null; // Remove Eyes from Template Holder

                    part.Child = HeadSection; // Add Eyes to first Snake Part
                }
                else
                {
                    part.Background = Brushes.Red;
                }

                if (i == 2)
                {
                    AppWindow.SnakeTailHolder.Child = null;
                    AppWindow.SnakeTailHolder.RenderTransformOrigin = new Point(0.5, 0.5);
                    AppWindow.SnakeTail.Fill = Brushes.Red;
                    part.Background = Brushes.Transparent;
                    part.Child = AppWindow.SnakeTail;
                }

                snake.Add(new SnakePart() { Part = part, X = middle, Y = middle });
                AppWindow.SnakeGrid.Children.Add(part);
                SetPosition(part, middle, middle);
            });
        }
    }

    public void AddSnakePart()
    {
        if (snake.Count > 0)
        {
            System.Windows.Application.Current.Dispatcher.Invoke((Action)delegate // "Border" & "SnakeGrid" is owned by default WPF Thread
            {
                AppWindow.ScoreBoardScore.Content = foodCollected * 1000; // Adding score to scoreboard

                SnakePart temp = snake.Last();
                Border part = new Border();
                part.CornerRadius = new CornerRadius(5, 5, 5, 5);
                part.Uid = Guid.NewGuid().ToString();
                part.Background = Brushes.Transparent;
                AppWindow.SnakeGrid.Children.Add(part);
                snake.Add(new SnakePart() { Part = part, X = temp.X, Y = temp.Y });
                SetPosition(part, temp.X, temp.Y);
            });
        }
    }

    public static void SetPosition(Border part, int x, int y)
    {
        if (part != null && System.Windows.Application.Current != null)
        {
            System.Windows.Application.Current.Dispatcher.Invoke((Action)delegate // "Grid" is owned by default WPF Thread
            {
                Grid.SetColumn(part, x);
                Grid.SetRow(part, y);
            });
        }
    }
    public static void SetPosition(Ellipse part, int x, int y) // Overload of SetPosition(Border part, int x, int y)
    {
        if (part != null && System.Windows.Application.Current != null)
        {
            System.Windows.Application.Current.Dispatcher.Invoke((Action)delegate // "Grid" is owned by default WPF Thread
            {
                Grid.SetColumn(part, x);
                Grid.SetRow(part, y);
            });
        }
    }

    public void UpdateSnake(Direction direction)
    {
        Stopwatch UpdateSnakeTimer = new();
        UpdateSnakeTimer.Start();

        SnakePart tempSnakeHead = snake.FirstOrDefault() ?? new SnakePart();

        SnakePart newSnakeHead = new(); // 
        try
        {
            if (tempSnakeHead.Part != null)
            {
                System.Windows.Application.Current.Dispatcher.Invoke((Action)delegate // "Grid" is owned by default WPF Thread
                {
                    Grid HeadSection = AppWindow.SnakeEyes; // Snake Eyes

                    RotateTransform rotateTransform1 = new RotateTransform(
                        direction == Direction.Up ? 0 : direction == Direction.Right ? 90 : direction == Direction.Down ? 180 : 270
                        );
                    HeadSection.RenderTransform = rotateTransform1;

                    tempSnakeHead.Part.Child = null; // Remove Eyes from First snake section                    

                    tempSnakeHead.Part.Background = Brushes.Red;

                    Border part = new Border();
                    part.CornerRadius = new CornerRadius(5, 5, 5, 5);
                    part.Background = Brushes.DarkRed;
                    newSnakeHead = new SnakePart() { Part = part, X = tempSnakeHead.X, Y = tempSnakeHead.Y };
                    newSnakeHead.Part.Uid = Guid.NewGuid().ToString();

                    newSnakeHead.Part.Child = HeadSection;


                    AppWindow.SnakeGrid.Children.Add(part);
                    snake.Insert(0, newSnakeHead);
                    SetPosition(newSnakeHead.Part, newSnakeHead.X, newSnakeHead.Y);

                    SnakePart lastSnakePart = snake.Last() ?? new SnakePart();
                    if (lastSnakePart.Part != null)
                    {
                        lastSnakePart.Part.Child = null;
                        AppWindow.SnakeGrid.Children.Remove(lastSnakePart.Part);
                        snake.RemoveAt(snake.Count - 1);

                        SnakePart newLastSnake = snake.Last() ?? new SnakePart();

                        if (newLastSnake.Part != null)
                        {
                            newLastSnake.Part.Background = Brushes.Transparent;
                            newLastSnake.Part.Child = AppWindow.SnakeTail;

                            SnakePart secondLastSnake = snake[snake.Count() - 2];
                            if (secondLastSnake != null)
                            {
                                RotateTransform rotateTransformLast = new RotateTransform(
                                    secondLastSnake.Y < newLastSnake.Y ? 90 : secondLastSnake.X > newLastSnake.X ? 180 : secondLastSnake.Y > newLastSnake.Y ? 270 : 0
                                    );
                                AppWindow.SnakeTail.RenderTransform = rotateTransformLast;
                            }
                        }
                    }
                });
            }
        }
        catch (Exception)
        {
            return;
        }

        if (tempSnakeHead != null)
        {
            lock (DirectionLock) // Direction selected through user input
            {
                if (direction == Direction.Up)
                {
                    newSnakeHead.Y = tempSnakeHead.Y - 1;
                    IgnoreDirection = Direction.Down;
                }
                else if (direction == Direction.Right)
                {
                    newSnakeHead.X = tempSnakeHead.X + 1;
                    IgnoreDirection = Direction.Left;
                }
                if (direction == Direction.Down)
                {
                    newSnakeHead.Y = tempSnakeHead.Y + 1;
                    IgnoreDirection = Direction.Up;
                }
                if (direction == Direction.Left)
                {
                    newSnakeHead.X = tempSnakeHead.X - 1;
                    IgnoreDirection = Direction.Right;
                }
            }

            // Section for Game border transition
            if (newSnakeHead.Y < 0)
            {
                newSnakeHead.Y = GameMenu.GameSize - 1; // Y is ZeroIndex (-1)
            }
            else if (newSnakeHead.X > GameMenu.GameSize - 1)// X is ZeroIndex (-1)
            {
                newSnakeHead.X = 0;
            }
            else if (newSnakeHead.Y > GameMenu.GameSize - 1) // Y is ZeroIndex (-1)
            {
                newSnakeHead.Y = 0;
            }
            else if (newSnakeHead.X < 0)
            {
                newSnakeHead.X = GameMenu.GameSize - 1;  // X is ZeroIndex (-1)
            }

            if (newSnakeHead.Part != null)
            {
                SetPosition(newSnakeHead.Part, newSnakeHead.X, newSnakeHead.Y);
            }
        }

        UpdateSnakeTimer.Stop();
    }

    public void CheckIfColOnSnake()
    {
        if (snake.Count > 3)
        {
            var tempSnake = snake.FirstOrDefault() ?? new SnakePart();
            if (snake.Any(x => x != snake.FirstOrDefault() && x.X == tempSnake.X && x.Y == tempSnake.Y))
            {
                SnakeAlive = false;
            }
        }
    }

    public void CheckIfColOnFood()
    {
        if (snake.Count > 0)
        {
            var tempSnake = snake.FirstOrDefault() ?? new SnakePart();

            List<Food> TempFood = new();
            foreach (var item in food)
            {
                if (tempSnake.X == item.X && tempSnake.Y == item.Y) // Check if snake head is on food
                {
                    System.Windows.Application.Current.Dispatcher.Invoke((Action)delegate
                    {
                        AppWindow.SnakeGrid.Children.Remove(item.FoodBox);
                    });

                    TempFood.Add(item);
                    foodCollected++;
                    AddSnakePart(); // Adds a section to the snake
                }
            }

            foreach (var item in TempFood)
            {
                food.Remove(item);
            }
        }
        if (food.Count == 0 || gameMenu.GameBonusActive && gameMenu.GameBonusAdd)
        {
            Random rand = new Random();

            Food newFood = new();

            int maxSpaces = GameMenu.GameSize * GameMenu.GameSize;
            int ko = food.Count + snake.Count;

            if (ko < maxSpaces)
            {
                gameMenu.GameBonusAdd = false;

                while (true) // Try to spawn food
                {
                    int rndX = rand.Next(GameMenu.GameSize); // Random X
                    int rndY = rand.Next(GameMenu.GameSize); // Random Y
                    if (!snake.Any(x => x.X == rndX && x.Y == rndY) && !food.Any(x => x.X == rndX && x.Y == rndY)) // If Random X & Y does not match any snake positions
                    {
                        newFood.X = rndX;
                        newFood.Y = rndY;
                        newFood.Active = true;

                        System.Windows.Application.Current.Dispatcher.Invoke((Action)delegate
                        {
                            Ellipse newFoodBox = new Ellipse();
                            newFoodBox.Fill = Brushes.Yellow;
                            newFood.FoodBox = newFoodBox;
                            AppWindow.SnakeGrid.Children.Add(newFoodBox);
                        });

                        food.Add(newFood);

                        if (newFood.FoodBox != null)
                        {
                            SetPosition(newFood.FoodBox, newFood.X, newFood.Y); // Places food in the game
                        }
                        break;
                    }
                }
            }
        }
    }

    private void CheckIfWon()
    {
        if (food.Count == 0 && snake.Count >= GameMenu.GameSize * GameMenu.GameSize)
        {
            GameActive = false;
        }
    }

    public void CheckIfAddFood()
    {
        if (GameActive && gameMenu.GameBonusCounter >= GameMenu.GameBonus)
        {
            gameMenu.GameBonusAdd = true;
            gameMenu.GameBonusCounter = 0;
        }
        if (GameActive)
        {
            gameMenu.GameBonusCounter++;
        }
    }
}

public class SnakePart
{
    public Border? Part { get; set; }
    public int X { get; set; }
    public int Y { get; set; }
}

public class Food
{
    public Ellipse? FoodBox { get; set; }
    public bool Active { get; set; }
    public int X { get; set; }
    public int Y { get; set; }
}

public interface IGameFunktions{
    public void ResetGame();
    public void StopGame();
    public void StartGame();
}

public abstract class GameFunktions
{
    public virtual void SetupGame()
    {
        Console.WriteLine("No game setup");
    }
    public virtual void GameTimer()
    {
        Console.WriteLine("No game timer setup");
    }
}
