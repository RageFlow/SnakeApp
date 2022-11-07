using System;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Shapes;

namespace SnakeApp
{
    public partial class MainWindow : Window
    {
        public static GameEngine gameEngine { get; set; } = new GameEngine();
        public static GameMenu gameMenu { get; set; } = new GameMenu();
        public static MainWindow AppWindow { get; set; } = new MainWindow();

        public MainWindow()
        {
            AppWindow = this;

            InitializeComponent();

            AppWindow.StateChanged += Window_StateChanged; // Adding Window_StateChanged to the windows StateChanged event
        }

        private void StartGame(object sender, RoutedEventArgs e) // StartGame method called by the GameMenu Start button
        {
            gameEngine.StartGame();
        }
        private void StopGame(object sender, RoutedEventArgs e) // StopGame method called by the GameMenu Stop button
        {
            gameEngine.StopGame();
        }

        // Methods for hiding and showing GameMenu UI buttons and elements
        public void HideMenu()
        {
            Menu.Visibility = Visibility.Hidden;
        }
        public void HideInputFields()
        {
            GameSizeInputBox.Visibility = Visibility.Hidden;
            GameSpeedInputBox.Visibility = Visibility.Hidden;
            GameBonusInputBox.Visibility = Visibility.Hidden;
        }
        public void ShowInputFields()
        {
            GameSizeInputBox.Visibility = Visibility.Visible;
            GameSpeedInputBox.Visibility = Visibility.Visible;
            GameBonusInputBox.Visibility = Visibility.Visible;
        }
        public void HideStopButton()
        {
            StopGameButton.Visibility = Visibility.Hidden;
        }
        public void SetupForNoGame()
        {
            AppWindow.Menu.Visibility = Visibility.Visible;
            AppWindow.StartButton.Visibility = Visibility.Visible;
            AppWindow.ResumeButton.Visibility = Visibility.Hidden;
        }
        public void ClearGameArea()
        {
            SnakeGrid.Children.Clear();
            SnakeGrid.RowDefinitions.Clear();
            SnakeGrid.ColumnDefinitions.Clear();
        }

        private static readonly Regex _regex = new Regex("[^0-9]+"); // Regex that matches only numbers (For number input of GameMenu input fields)
        private void NumberValidationTextBox(object sender, TextCompositionEventArgs e) // Input number validation
        {
            e.Handled = _regex.IsMatch(e.Text);
            return;
        }
        private void SpeedTextChanged(object sender, TextChangedEventArgs args) // TextChangedEventHandler delegate method from UI (MainWindow.xaml file).
        {
            gameMenu.GameSpeedInputText = GameSpeedInput.Text;
        }
        private void SizeTextChanged(object sender, TextChangedEventArgs args) // TextChangedEventHandler delegate method from UI (MainWindow.xaml file).
        {
            gameMenu.GameSizeInputText = GameSizeInput.Text;
        }
        private void BonusTextChanged(object sender, TextChangedEventArgs args) // TextChangedEventHandler delegate method from UI (MainWindow.xaml file).
        {
            gameMenu.GameBonusInputText = GameBonusInput.Text;
        }

        private void Window_PreviewKeyDown(object sender, KeyEventArgs e) // KeyDown event from UI (MainWindow.xaml file), so we can have game input through the users keyboard.
        {
            if (e.OriginalSource is TextBox) // Check if KeyEvent is from Textbox
            {
                e.Handled = false; // Ignore Handle and let it hit "NumberValidationTextBox();"
                return;
            }

            if (Monitor.TryEnter(GameEngine.DirectionLock)) // Check if we can access the DirectionLock though the TryEnter.
            {
                // Set the Direction the snake should take in the new Game Tick (Each time our Timer thread is through a cycle)
                if (e.Key == Key.Up && GameEngine.IgnoreDirection != Direction.Up)
                {
                    GameEngine.GameSnakeDirection = Direction.Up;
                }
                else if (e.Key == Key.Right && GameEngine.IgnoreDirection != Direction.Right)
                {
                    GameEngine.GameSnakeDirection = Direction.Right;
                }
                else if (e.Key == Key.Down && GameEngine.IgnoreDirection != Direction.Down)
                {
                    GameEngine.GameSnakeDirection = Direction.Down;
                }
                else if (e.Key == Key.Left && GameEngine.IgnoreDirection != Direction.Left)
                {
                    GameEngine.GameSnakeDirection = Direction.Left;
                }

                Monitor.Exit(GameEngine.DirectionLock);
            }

            if (e.Key == Key.Escape && gameEngine.GameActive) // If we want to pause the game with the Escape key (But only if the game is running)
            {
                if (Menu.Visibility == Visibility.Hidden) // If menu is hidden then show the menu and pause the game
                {
                    GameEngine.Pause();
                    Menu.Visibility = Visibility.Visible;

                    StartButton.Visibility = Visibility.Hidden; // Hide StartButton
                    ResumeButton.Visibility = Visibility.Visible; // Show Resume button
                    StopGameButton.Visibility = Visibility.Visible; // Show New Game button

                    HideInputFields();
                }
                else if (Menu.Visibility == Visibility.Visible) // If menu is visible then hide menu and resume the game
                {
                    GameEngine.Resume();
                    Menu.Visibility = Visibility.Hidden;
                    StopGameButton.Visibility = Visibility.Hidden;
                }
            }

            //Process user input so the system knows that it shouldn't do anything else with the KeyDown event.
            e.Handled = true;
        }

        public void CloseGame(object sender, RoutedEventArgs e)
        {
            System.Windows.Application.Current.Shutdown(); // Closing the game (Exiting)
        }
        public void UnpauseGame(object sender, RoutedEventArgs e)
        {
            Menu.Visibility = Visibility.Hidden; // Hide menu and unpause the game
            GameEngine.Resume();
        }

        public enum Direction // Direction Enum
        {
            Up,
            Right,
            Down,
            Left
        }


        // WPF Custom styling!
        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e) //Checks if user clicks on window
        {
            DragMove(); //Used for dragging window
        }
        private void WindowClose(object sender, RoutedEventArgs e) //Close window when user click "Close"
        {
            Close(); //Closes window (Application)
        }
        private void WindowMaximize(object sender, RoutedEventArgs e) // Maximize or Normalize window when user clicks the Maximize/Normalize button
        {
            if (WindowState == WindowState.Maximized) // If maximized then normalize window
            {
                WindowState = WindowState.Normal;
            }
            else
            {
                WindowState = WindowState.Maximized;
            }
        }
        private void WindowMinimize(object sender, RoutedEventArgs e) // Minimize window when user clicks the minimize button
        {
            WindowState = WindowState.Minimized;
        }


        private void Window_StateChanged(object? sender, EventArgs e) => CheckWindowState(); // Method used by the App Windows StateChanged event
        private void CheckWindowState() // Method for checking the App Window state
        {
            if (WindowState == WindowState.Maximized) // If maximized show UI Normalize element
            {
                MaximizeButtonNormalizeShape.Visibility = Visibility.Visible;
            }
            else if (WindowState == WindowState.Normal) // If Minimized show UI Normalize element
            {
                MaximizeButtonNormalizeShape.Visibility = Visibility.Hidden;
            }
            Console.WriteLine();
        }

        // Ignore all below!
        // This has technically no value for the OOP Project, but I hate the standard WPF window styling and sizing!
        // The window styling is set in the "Styling.xaml" file.

        #region ScaleValue Dependency Property (Used to scale the window and its contents)

        // DependencyProperty used by UI to fix Scaling of UI Content. // Scaling content instead of just widening the window itself
        public static readonly DependencyProperty ScaleValueProperty = DependencyProperty.Register(
            "ScaleValue",
            typeof(double),
            typeof(MainWindow),
            new UIPropertyMetadata(
                1.0,
                new PropertyChangedCallback(OnScaleValueChanged),
                new CoerceValueCallback(OnCoerceScaleValue)
                )
            );

        private static object OnCoerceScaleValue(DependencyObject DependObject, object value) // Value is by default 1 (Window scale of 1)
        {
            MainWindow? mainWindow = DependObject as MainWindow; // Cast to MainWindow class
            if (mainWindow != null)
            {
                return mainWindow.OnCoerceScaleValue((double)value); // Return value by using overload with its value
            }
            else
            {
                return value;
            }
        }

        protected virtual double OnCoerceScaleValue(double value) // Overload for "OnCoerceScaleValue(DependencyObject DependObject, object value)" method
        {
            if (double.IsNaN(value)) // Check if value is not a number
            {
                return 1.0f; // Set default value to 1 (Default scale value of UI)
            }
            value = Math.Max(0.1, value);

            return value;
        }

        private static void OnScaleValueChanged(DependencyObject DependObject, DependencyPropertyChangedEventArgs e)
        {
            MainWindow? mainWindow = DependObject as MainWindow; // Cast to MainWindow class
            if (mainWindow != null) // Code error-cheking
            {
                mainWindow.OnScaleValueChanged((double)e.OldValue, (double)e.NewValue);
            }
        }

        protected virtual void OnScaleValueChanged(double oldValue, double newValue) { }

        public double ScaleValue
        {
            get => (double)GetValue(ScaleValueProperty); // Get value of DependencyObject
            set => SetValue(ScaleValueProperty, value); // Set value of DependencyObject
        }
        #endregion

        #region Check Window Scale and Apply to fix UI
        private void MainGrid_SizeChanged(object sender, EventArgs e) => CalculateScale();

        private void CalculateScale()
        {
            if (DefaultWindowHeight == 0f || DefaultWindowWidth == 0f)
            {
                DefaultWindowHeight = (float)Math.Round(ActualHeight, 0, MidpointRounding.ToEven); // Set DefaultWindowHeight to current size (Set only once on app startup)
                DefaultWindowWidth = (float)Math.Round(ActualWidth, 0, MidpointRounding.ToEven); // Set DefaultWindowWidth to current size (Set only once on app startup)
            }
            double yScale = ActualHeight / DefaultWindowHeight; // Y Scale based on Actual height and Default Window height
            double xScale = ActualWidth / DefaultWindowWidth; // X Scale based on Actual width and Default Window width
            double value = Math.Min(xScale, yScale); // Calculate scale based on the Windows actual Height and Width

            ScaleValue = (double)OnCoerceScaleValue(SnakeMainWindow, value);
        }

        private static float DefaultWindowHeight { get; set; } = 0f;
        private static float DefaultWindowWidth {get;set; } = 0f;
        #endregion
    }
}