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
using System.Threading.Tasks;

public class GameEngine : GameFunctions, IGameFunctions
{
    public static readonly object DirectionLock = new object();
    private static AutoResetEvent _blockGameThread = new AutoResetEvent(true);
    private static ManualResetEvent _GameThread = new ManualResetEvent(false);

    private List<Food> food = new();
    private List<SnakePart> snake = new List<SnakePart>();
    private int foodCollected { get; set; }
    public int FoodCollected
    {
        get { return foodCollected; }
    }

    public static Direction GameSnakeDirection = Direction.Right;
    public static Direction IgnoreDirection = Direction.Left;

    bool SnakeAlive = false;
    public bool GameActive = false;

    public async Task StopGameProgram()
    {
        await Task.CompletedTask;
        Environment.Exit(0);
    }

    public static void Resume() => _GameThread.Set();
    public static void Pause() => _GameThread.Reset();

    public void ResetGame()
    {
        System.Windows.Application.Current.Dispatcher.Invoke((Action)delegate // "GridLengthConverter" & "SnakeGrid" is owned by default WPF Thread
        {
            if (snake != null && snake.Count > 0) // This section is only neccessary because of WPF's shorty Witchcraft....
            {
                Polygon snakeTail = AppWindow.SnakeTail;
                Border snakeTailParent = (Border)snakeTail.Parent;

                Grid snakeEyes = AppWindow.SnakeEyes;
                Border snakeEyesParent = (Border)snakeEyes.Parent;

                SnakePart ko3 = snake.Where(x => x.Part != null && x.Part.Uid == snakeTailParent.Uid).FirstOrDefault() ?? new SnakePart();
                if (ko3.Part != null)
                {
                    ko3.Part.Child = null; // Removing dynamic UI Snake tail element from current snake tail parent
                }
                SnakePart ko4 = snake.Where(x => x.Part != null && x.Part.Uid == snakeEyesParent.Uid).FirstOrDefault() ?? new SnakePart();
                if (ko4.Part != null)
                {
                    ko4.Part.Child = null;// Removing dynamic UI Snake eye element from current snake eye parent
                }

                AppWindow.SnakeEyeHolder.Child = AppWindow.SnakeEyes; // Adding snake eye element to SnakeEyeHolder
                AppWindow.SnakeTailHolder.Child = AppWindow.SnakeTail; // Adding snake eye element to SnakeTailHolder
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

            GridLengthConverter gridLengthConverter = new GridLengthConverter(); // WPF grid converter/creator
            // Adding grid section to the UI based on how big the user set the field to be
            for (int i = 0; i < GameMenu.GameSize; i++)
            {
                var gridlength = gridLengthConverter.ConvertFrom("*"); // New Gridlength
                if (gridlength != null)
                {
                    AppWindow.SnakeGrid.RowDefinitions.Add(new RowDefinition() { Height = (GridLength)gridlength }); // Adding grid row to UI
                    AppWindow.SnakeGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = (GridLength)gridlength }); // Adding grid column to UI
                }

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

        // Threads for GameTimer and GameController
        // GameTimer controls how often the game should do a move (Move snake by one grid section)
        // GameController moves snake based on User Input and check collision of Snake to Snake and Snake to Food (Also controls when and where to add new food)
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
            Thread.Sleep(GameMenu.GameSpeed); // Wait amount set by user (Game Speed)
            _blockGameThread.Set(); // Tell GameController event to fire (Do one round of GameControl)
        }
    }

    private async void GameController()
    {
        SetupGame(); // Game Setup

        while (SnakeAlive)
        {
            _blockGameThread.WaitOne();
            _GameThread.WaitOne(); // Check for pause

            if (!GameActive) // Check if Game is still active (This check is for Exiting the Game/Program)
            {
                return;
            }

            CheckIfAddFood();

            UpdateSnake(GameSnakeDirection);

            CheckIfColOnSnake();

            CheckIfColOnFood();

            await CheckIfWon();
        }

        ResetGame(); // When snake dies!
    }

    public override void SetupGame() // First init of game after user pressed the Play Button
    {
        int middle = GameMenu.GameSize / 2; // Middle grid piece of Ui Grid

        System.Windows.Application.Current.Dispatcher.Invoke((Action)delegate // "UI" Inside "HideMenu" method is owned by default WPF Thread
        {
            AppWindow.HideMenu(); // Hide menu since the game should now start
        });

        // Setting up snake for game start (3 section long snake)
        for (int i = 0; i < 3; i++)
        {
            System.Windows.Application.Current.Dispatcher.Invoke((Action)delegate // "Border", "Grid" and "AppWindow (Window and elements)" is owned by default WPF Thread
            {
                Border part = new Border(); // New border element (Snake UI Element)
                part.Uid = Guid.NewGuid().ToString();  // Random new Uid so we can change it in the UI through the snake part from the list

                if (i == 0) // Check if part should be head piece
                {
                    part.Background = DefaultSnakeHeadColor; // Set snake head to Head color
                    Grid HeadSection = AppWindow.SnakeEyes; // Snake eye element from UI

                    AppWindow.SnakeEyeHolder.Child = null; // Remove Eyes from Template Holder

                    part.Child = HeadSection; // Add Eyes to first Snake Part
                }
                else
                {
                    part.Background = DefaultSnakeColor; // Setting UI Element color if not Head piece
                }

                if (i == 2)
                {
                    AppWindow.SnakeTailHolder.Child = null; // Removing snake tail from default snake tail holder
                    AppWindow.SnakeTailHolder.RenderTransformOrigin = new Point(0.5, 0.5);
                    AppWindow.SnakeTail.Fill = DefaultSnakeColor; // Setting snake tail color to default snake color
                    AppWindow.SnakeTail.Stroke = DefaultSnakeColor; // Setting snake tail stroke to default snake color
                    part.Background = Brushes.Transparent;
                    part.Child = AppWindow.SnakeTail; // Adding snake tail to snake part (Last snake part in the chain)
                }

                snake.Add(new SnakePart() { Part = part, X = middle, Y = middle }); // Adding snakepart to snake list
                AppWindow.SnakeGrid.Children.Add(part); // Adding snake part to UI grid
                SetPosition(part, middle, middle); // Setting snake part grid position
            });
        }
    }

    public void AddSnakePart()
    {
        if (snake != null && snake.Count > 0) // Check if snake is not null and that it has been activated (Has more than 1 item) 
        {
            System.Windows.Application.Current.Dispatcher.Invoke((Action)delegate // "Border" & "SnakeGrid" is owned by default WPF Thread
            {
                AppWindow.ScoreBoardScore.Content = FoodCollected * 1000; // Adding score to scoreboard form FoodCollected interface Get Prop

                SnakePart temp = snake.Last(); // Get last snake part from snake list
                Border part = new Border(); // Make new Border (UI Square of snake)
                part.CornerRadius = new CornerRadius(5, 5, 5, 5);
                part.Uid = Guid.NewGuid().ToString(); // Random new Uid so we can change it in the UI through the snake part from the list
                part.Background = Brushes.Transparent;

                AppWindow.SnakeGrid.Children.Add(part); // Adding snake part to UI Grid
                snake.Add(new SnakePart() { Part = part, X = temp.X, Y = temp.Y }); // Adding snake part to snake list
                SetPosition(part, temp.X, temp.Y); // Set snake part grid position int the UI based on its coordinates (X & Y)
            });
        }
    }

    public static void SetPosition(Border part, int x, int y)
    {
        if (part != null && System.Windows.Application.Current != null) // Code error-checking
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
        if (part != null && System.Windows.Application.Current != null) // Code error-checking
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

        SnakePart tempSnakeHead = snake.FirstOrDefault() ?? new SnakePart(); // At this time snake head
        SnakePart newSnakeHead = new(); // New snake head after snake has moved
        try
        {
            if (tempSnakeHead.Part != null) // Code error-checking
            {
                System.Windows.Application.Current.Dispatcher.Invoke((Action)delegate // "Grid", "RotateTransform" and "Border" is owned by default WPF Thread
                {
                    Grid HeadSection = AppWindow.SnakeEyes; // Snake Eyes

                    RotateTransform rotateTransform1 = new RotateTransform( // Snake part UI Rotation property
                        direction == Direction.Up ? 0 : direction == Direction.Right ? 90 : direction == Direction.Down ? 180 : 270
                        );
                    HeadSection.RenderTransform = rotateTransform1;

                    tempSnakeHead.Part.Child = null; // Remove Eyes from First snake section                    
                    tempSnakeHead.Part.Background = DefaultSnakeColor; // Change snake part color to normal body color

                    Border part = new Border(); // New snake head UI element
                    part.CornerRadius = new CornerRadius(5, 5, 5, 5);
                    part.Background = DefaultSnakeHeadColor; // Setting new snake head to Head color

                    newSnakeHead = new SnakePart() { Part = part, X = tempSnakeHead.X, Y = tempSnakeHead.Y };
                    newSnakeHead.Part.Uid = Guid.NewGuid().ToString(); // Random new Uid so we can change it in the UI through the snake part from the list

                    newSnakeHead.Part.Child = HeadSection; // Setting snake eye section to the newsnake head


                    AppWindow.SnakeGrid.Children.Add(part); // Adding new snake head to UI Grid
                    snake.Insert(0, newSnakeHead); // Adding new snake head to the start of the snake list
                    SetPosition(newSnakeHead.Part, newSnakeHead.X, newSnakeHead.Y); // Set new snake head grid position

                    SnakePart lastSnakePart = snake.Last() ?? new SnakePart(); // Current Snake tail part
                    if (lastSnakePart.Part != null) // Code error-checking
                    {
                        lastSnakePart.Part.Child = null; // Removing snake tail polygon from last snake part
                        AppWindow.SnakeGrid.Children.Remove(lastSnakePart.Part); // Removing last snake part from UI Grid
                        snake.RemoveAt(snake.Count - 1); // Removing last elemt in Snake list (now old snake tail part)

                        SnakePart newLastSnake = snake.Last() ?? new SnakePart(); // Get new snake tail part

                        if (newLastSnake.Part != null) // Code error-checking
                        {
                            newLastSnake.Part.Background = Brushes.Transparent;
                            newLastSnake.Part.Child = AppWindow.SnakeTail; // Adding snake tail polygon to the last snake part

                            SnakePart secondLastSnake = snake[snake.Count() - 2]; // Getting 2nd last snake part to check snake tail direction
                            if (secondLastSnake != null) // Code error-checking
                            {
                                RotateTransform rotateTransformLast = new RotateTransform( // Snake part UI Rotation property
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
            return; // Stop if code fails
        }

        // Code for setting snake head position and rotations
        if (tempSnakeHead != null)
        {
            lock (DirectionLock) // Direction selected through user input
            {
                if (direction == Direction.Up)
                {
                    newSnakeHead.Y = tempSnakeHead.Y - 1; // If snake goes UP
                    IgnoreDirection = Direction.Down;
                }
                else if (direction == Direction.Right)
                {
                    newSnakeHead.X = tempSnakeHead.X + 1; // If snake goes RIGHT
                    IgnoreDirection = Direction.Left;
                }
                if (direction == Direction.Down)
                {
                    newSnakeHead.Y = tempSnakeHead.Y + 1; // If snake goes DOWN
                    IgnoreDirection = Direction.Up;
                }
                if (direction == Direction.Left)
                {
                    newSnakeHead.X = tempSnakeHead.X - 1; // If snake goes LEFT
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
                SetPosition(newSnakeHead.Part, newSnakeHead.X, newSnakeHead.Y); // Set new snake head grid position
            }
        }

        UpdateSnakeTimer.Stop();
    }

    public void CheckIfColOnSnake()
    {
        if (snake.Count > 3) // Check if snake is longer than start length (Start length is 3, Head, normal and tail section)
        {
            var tempSnake = snake.FirstOrDefault() ?? new SnakePart(); // Get snake head part
            if (snake.Any(x => x != snake.FirstOrDefault() && x.X == tempSnake.X && x.Y == tempSnake.Y)) // Check if head is colliding with any snake pieces
            {
                SnakeAlive = false; // Set SnakeAlive to false since the snake would be dead
            }
        }
    }

    public void CheckIfColOnFood()
    {
        if (snake != null && snake.Count > 0) // Check if snake is not null and that it has been activated (Has more than 1 item) 
        {
            var tempSnake = snake.FirstOrDefault() ?? new SnakePart(); // Get snake Head part

            List<Food> TempFood = new(); // Temp food list
            foreach (var item in food)
            {
                if (tempSnake.X == item.X && tempSnake.Y == item.Y) // Check if snake head is on food (Colliding)
                {
                    System.Windows.Application.Current.Dispatcher.Invoke((Action)delegate // "Grid" is owned by default WPF Thread
                    {
                        AppWindow.SnakeGrid.Children.Remove(item.FoodBox); // Remove food from UI Grid
                    });

                    TempFood.Add(item); // Add food to temp food list
                    foodCollected++; // Add to foodCollected (Used for game score)
                    AddSnakePart(); // Adds a section to the snake
                }
            }

            // Remove all the food the snake has collided with (Should only be 1, but this is done because we can't remove it while iterating over the list above)
            foreach (var item in TempFood)
            {
                food.Remove(item);
            }
        }
        if (food.Count == 0 && snake != null || gameMenu.GameBonusActive && gameMenu.GameBonusAdd && snake != null) // Check if there is no active food on the game board, or if the GameBonus wants us to add food
        {
            Random rand = new Random(); // New random

            Food newFood = new(); // New food

            int maxSpaces = GameMenu.GameSize * GameMenu.GameSize; // Max game UI size, set by GameMenu's "GameSize"
            int totalSquaresUsed = food.Count + snake.Count; // Total squares taken up by snake and food on the UI Grid

            if (totalSquaresUsed < maxSpaces) // Check if there is room for more food
            {
                gameMenu.GameBonusAdd = false; // Resetting the GameBonus add bool

                while (true) // Try to spawn food (This is a bit ugly, but shows while loop running until it succeeds)
                {
                    int rndX = rand.Next(GameMenu.GameSize); // Random X
                    int rndY = rand.Next(GameMenu.GameSize); // Random Y
                    if (!snake.Any(x => x.X == rndX && x.Y == rndY) && !food.Any(x => x.X == rndX && x.Y == rndY)) // If Random X & Y does not match any snake or food positions
                    {
                        newFood.X = rndX; // Setting new food's X
                        newFood.Y = rndY; // Setting new food's Y
                        newFood.Active = true; // Setting it to active

                        System.Windows.Application.Current.Dispatcher.Invoke((Action)delegate // "Elliplse" and "Grid" is owned by default WPF Thread
                        {
                            Ellipse newFoodBox = new Ellipse(); // New food UI element
                            newFoodBox.Fill = DefaultFoodColor;
                            newFood.FoodBox = newFoodBox;
                            AppWindow.SnakeGrid.Children.Add(newFoodBox); // Adding new food to UI grid
                        });

                        food.Add(newFood); // Adding food to our food list

                        if (newFood.FoodBox != null) // Checking if the UI element was created and added to the food
                        {
                            SetPosition(newFood.FoodBox, newFood.X, newFood.Y); // Places food in the game
                        }
                        break; // Exiting While since we now have a new food piece on the UI Grid
                    }
                }
            }
        }
    }

    private async Task CheckIfWon()
    {
        try
        {
            if (food.Count == 0 && snake.Count >= GameMenu.GameSize * GameMenu.GameSize) // Check if snake is taking up the entire play area (All grid positions)
            {
                throw new SnakeException("Game was won!"); // Emulating random exception (Even though it only fires when a player wins a game)
            }
        }
        catch (SnakeException)
        {
            GameActive = false; // Setting the GameActive to false, so our Threads can know that the game has ended
        }        

        await Task.CompletedTask;
    }

    public void CheckIfAddFood()
    {
        // This section is only for the Bonus mechanism of the game
        if (GameActive && gameMenu.GameBonusCounter >= GameMenu.GameBonus) // Check if gamebonus is active and the Bonus timer has reached user input of amount
        {
            gameMenu.GameBonusAdd = true; // Set to true so the game knows it should add a food piece
            gameMenu.GameBonusCounter = 0; // Resetting GameBonusCounter
        }
        if (GameActive) // Check if GameBonus is set to active
        {
            gameMenu.GameBonusCounter++; // Increment bonus counter
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

public interface IGameFunctions{
    int FoodCollected { get; }
    public void ResetGame();
    public void StopGame();
    public void StartGame();
}

public abstract class GameFunctions
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
