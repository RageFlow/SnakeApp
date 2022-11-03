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

public class GameEngine
{
    public GameEngine()
    {
    }

    private static readonly object DirectionLock = new object();
    private static AutoResetEvent _blockGameThread = new AutoResetEvent(true);
    private static ManualResetEvent _GameThread = new ManualResetEvent(false);

    private List<Food> food = new();
    Dictionary<int, SnakePart> snake = new Dictionary<int, SnakePart>();
    private int foodCollected { get; set; }

    Direction GameSnakeDirection = Direction.Right;
    Direction IgnoreDirection = Direction.Left;
    bool SnakeAlive = false;
    bool GameActive = false;

    public void Resume() => _GameThread.Set();
    public void Pause() => _GameThread.Reset();

    public void ResetGame()
    {
        System.Windows.Application.Current.Dispatcher.Invoke((Action)delegate // "GridLengthConverter" & "SnakeGrid" is owned by default WPF Thread
        {
            Menu.Visibility = Visibility.Visible;
            StartButton.Visibility = Visibility.Visible;
            ResumeButton.Visibility = Visibility.Hidden;

            //Resetting props
            SnakeGrid.Children.Clear();
            SnakeGrid.RowDefinitions.Clear();
            SnakeGrid.ColumnDefinitions.Clear();
            food = new();
            snake = new();
            foodCollected = 0;
            GameSnakeDirection = Direction.Right;
            IgnoreDirection = Direction.Left;
            SnakeAlive = false;
            GameActive = false;
            //Resetting props

            UpdateGameSettings();

            GridLengthConverter gridLengthConverter = new GridLengthConverter();

            for (int i = 0; i < GameMenu.GameSize; i++)
            {
                SnakeGrid.RowDefinitions.Add(new RowDefinition() { Height = (GridLength)gridLengthConverter.ConvertFrom("*") });
                SnakeGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = (GridLength)gridLengthConverter.ConvertFrom("*") });
            }
        });
    }

    private void StopGame(object sender, RoutedEventArgs e)
    {
        SnakeAlive = false;
        GameActive = false;
        ResetGame(); // Reset game for new run
        Resume(); // Starts GameTimer (Activates _GameThread which we use for pausing the game)
        StopGameButton.Visibility = Visibility.Hidden;

        GameMenu.ShowInputFields();
    }

    private void StartGame(object sender, RoutedEventArgs e)
    {
        GameMenu.HideInputFields(); // Hide input fields when game is active

        ResetGame(); // Reset game for new run
        SnakeAlive = true;
        GameActive = true;
        Resume(); // Starts GameTimer (Activates _GameThread which we use for pausing the game)

        Thread timerThread = new Thread(GameTimer);
        Thread controllerThread = new Thread(GameController);
        timerThread.Start();
        controllerThread.Start();
    }

    private void GameTimer()
    {
        while (GameActive)
        {
            _GameThread.WaitOne(); // Check for pause
            Thread.Sleep(GameSpeed);
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

            UpdateSnake(GameSnakeDirection);
            CheckIfAddFood();
            CheckIfColOnSnake();
            CheckIfColOnFood();
        }

        ResetGame(); // If snake dies!
    }

    private void SetupGame() // First init of game after user pressed the Play Button
    {
        int middle = GameSize / 2;

        System.Windows.Application.Current.Dispatcher.Invoke((Action)delegate
        {
            Menu.Visibility = Visibility.Hidden;

            Ellipse newFood = new Ellipse();
            newFood.Fill = Brushes.Yellow;

            food.Add(new Food() { FoodBox = newFood, Active = true }); ;
            SnakeGrid.Children.Add(newFood);
        });

        for (int i = 0; i < 3; i++)
        {
            System.Windows.Application.Current.Dispatcher.Invoke((Action)delegate
            {
                Canvas part = new Canvas() { Name = $"Snake{i}" };

                if (i == 0)
                {
                    part.Background = Brushes.DarkRed;
                }
                else
                {
                    part.Background = Brushes.Red;
                }
                snake.Add(i, new SnakePart() { Part = part, X = middle, Y = middle });
                SnakeGrid.Children.Add(part);
                SetPosition(part, middle, middle);
            });
        }
    }

    public void AddSnakePart()
    {
        System.Windows.Application.Current.Dispatcher.Invoke((Action)delegate // "Canvas" & "SnakeGrid" is owned by default WPF Thread
        {
            ScoreBoardScore.Content = foodCollected * 1000; // Adding score to scoreboard

            SnakePart temp = snake.Last().Value;
            Canvas part = new Canvas() { Name = $"Snake{snake.Count - 1}" };
            part.Background = Brushes.Red;
            SnakeGrid.Children.Add(part);
            snake.Add(snake.Count, new SnakePart() { Part = part, X = temp.X, Y = temp.Y });
            SetPosition(part, temp.X, temp.Y);
        });
    }

    public static void SetPosition(Canvas part, int x, int y)
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
    public static void SetPosition(Ellipse part, int x, int y) // Overload of SetPosition(Canvas part, int x, int y)
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

        foreach (var item in snake.OrderByDescending(x => x.Key).ToArray()) //Going throud Snake from last piece to first.
        {
            if (item.Key == 0)
            {
                lock (DirectionLock) // Direction selected through user input
                {
                    if (direction == Direction.Up)
                    {
                        item.Value.Y -= 1;
                        IgnoreDirection = Direction.Down;
                    }
                    else if (direction == Direction.Right)
                    {
                        item.Value.X += 1;
                        IgnoreDirection = Direction.Left;
                    }
                    if (direction == Direction.Down)
                    {
                        item.Value.Y += 1;
                        IgnoreDirection = Direction.Up;
                    }
                    if (direction == Direction.Left)
                    {
                        item.Value.X -= 1;
                        IgnoreDirection = Direction.Right;
                    }
                }

                // Section for Game border transition
                if (item.Value.Y < 0)
                {
                    item.Value.Y = GameSize - 1; // Y is ZeroIndex (-1)
                }
                else if (item.Value.X > GameSize - 1)// X is ZeroIndex (-1)
                {
                    item.Value.X = 0;
                }
                else if (item.Value.Y > GameSize - 1) // Y is ZeroIndex (-1)
                {
                    item.Value.Y = 0;
                }
                else if (item.Value.X < 0)
                {
                    item.Value.X = GameSize - 1;  // X is ZeroIndex (-1)
                }
                SetPosition(item.Value.Part, item.Value.X, item.Value.Y);
            }
            else
            {
                int nextPartId = item.Key - 1;
                SnakePart nextPart = snake[nextPartId];
                if (item.Value.X != nextPart.X || item.Value.Y != nextPart.Y)
                {
                    item.Value.X = nextPart.X;
                    item.Value.Y = nextPart.Y;
                }
                SetPosition(item.Value.Part, item.Value.X, item.Value.Y);
            }
        }

        UpdateSnakeTimer.Stop();
    }

    public void CheckIfColOnSnake()
    {
        if (snake.Count > 3)
        {
            var tempSnake = snake.FirstOrDefault();
            if (snake.Any(x => x.Key != 0 && x.Value.X == tempSnake.Value.X && x.Value.Y == tempSnake.Value.Y))
            {
                SnakeAlive = false;
            }
        }
    }

    public void CheckIfColOnFood()
    {
        if (snake.Count > 0)
        {
            var tempSnake = snake.FirstOrDefault();

            List<Food> TempFood = new();
            foreach (var item in food)
            {
                if (tempSnake.Value.X == item.X && tempSnake.Value.Y == item.Y) // Check if snake head is on food
                {
                    System.Windows.Application.Current.Dispatcher.Invoke((Action)delegate
                    {
                        SnakeGrid.Children.Remove(item.FoodBox);
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
        if (food.Count == 0 || GameBonusActive && GameBonusAdd)
        {
            GameBonusAdd = false;

            Random rand = new Random();

            Food newFood = new();

            while (true) // Try to spawn food
            {
                int rndX = rand.Next(GameSize); // Random X
                int rndY = rand.Next(GameSize); // Random Y
                if (!snake.Any(x => x.Value.X == rndX && x.Value.Y == rndY)) // If Random X & Y does not match any snake positions
                {
                    newFood.X = rndX;
                    newFood.Y = rndY;
                    newFood.Active = true;

                    System.Windows.Application.Current.Dispatcher.Invoke((Action)delegate
                    {
                        Ellipse newFoodBox = new Ellipse();
                        newFoodBox.Fill = Brushes.Yellow;
                        newFood.FoodBox = newFoodBox;
                        SnakeGrid.Children.Add(newFoodBox);
                    });

                    food.Add(newFood);

                    SetPosition(newFood.FoodBox, newFood.X, newFood.Y); // Places food in the game
                    break;
                }
            }
        }
    }

    public void CheckIfAddFood()
    {
        if (GameActive && GameBonusCounter >= GameBonus)
        {
            GameBonusAdd = true;
            GameBonusCounter = 0;
        }
        if (GameActive)
        {
            GameBonusCounter++;
        }
    }
}
