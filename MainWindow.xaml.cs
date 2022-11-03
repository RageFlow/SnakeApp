using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using static SnakeApp.MainWindow;

namespace SnakeApp
{
    public partial class MainWindow : Window
    {        
        public MainWindow()
        {
            InitializeComponent();
        }

        private static readonly Regex _regex = new Regex("[^0-9]+"); //regex that matches disallowed text
        private void NumberValidationTextBox(object sender, TextCompositionEventArgs e) // Input number validation
        {
            e.Handled = _regex.IsMatch(e.Text);
            return;
        }
        private void SpeedTextChanged(object sender, TextChangedEventArgs args) // TextChangedEventHandler delegate method.
        {
            GameSpeedInputText = GameSpeedInput.Text;
        }
        private void SizeTextChanged(object sender, TextChangedEventArgs args) // TextChangedEventHandler delegate method.
        {
            GameSizeInputText = GameSizeInput.Text;
        }
        private void BonusTextChanged(object sender, TextChangedEventArgs args) // TextChangedEventHandler delegate method.
        {
            GameBonusInputText = GameBonusInput.Text;
        }

        private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.OriginalSource is TextBox) // Check if KeyEvent is from Textbox
            {
                e.Handled = false; // Ignore Handle and let it hit "NumberValidationTextBox();"
                return;
            }

            if (Monitor.TryEnter(DirectionLock))
            {
                if (e.Key == Key.Up && IgnoreDirection != Direction.Up)
                {
                    GameSnakeDirection = Direction.Up;
                }
                else if (e.Key == Key.Right && IgnoreDirection != Direction.Right)
                {
                    GameSnakeDirection = Direction.Right;
                }
                else if (e.Key == Key.Down && IgnoreDirection != Direction.Down)
                {
                    GameSnakeDirection = Direction.Down;
                }
                else if (e.Key == Key.Left && IgnoreDirection != Direction.Left)
                {
                    GameSnakeDirection = Direction.Left;
                }

                Monitor.Exit(DirectionLock);
            }

            if (e.Key == Key.Escape) // If we want to pause the game
            {
                if (Menu.Visibility == Visibility.Hidden)
                {
                    Pause();
                    Menu.Visibility = Visibility.Visible;

                    StartButton.Visibility = Visibility.Hidden; // Hide StartButton
                    ResumeButton.Visibility = Visibility.Visible; // Show Resume button
                    StopGameButton.Visibility = Visibility.Visible; // Show New Game button
                }
                else if (Menu.Visibility == Visibility.Visible)
                {
                    Resume();
                    Menu.Visibility = Visibility.Hidden;
                    StopGameButton.Visibility = Visibility.Hidden;
                }
            }

            //Process user input
            e.Handled = true;
        }

        public void CloseGame(object sender, RoutedEventArgs e)
        {
            System.Windows.Application.Current.Shutdown();
        }
        public void UnpauseGame(object sender, RoutedEventArgs e)
        {
            Menu.Visibility = Visibility.Hidden;
            Resume();
        }

        public class SnakePart
        {
            public Canvas Part { get; set; }
            public int X { get; set; }
            public int Y { get; set; }
        }

        public class Food
        {
            public Ellipse FoodBox { get; set; }
            public bool Active { get; set; }
            public int X { get; set; }
            public int Y { get; set; }
        }

        public enum Direction
        {
            Up,
            Right,
            Down,
            Left
        }


        // WPF Custom styling! Ignore
        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e) //Checks if user clicks on window
        {
            DragMove(); //Used for dragging window
        }
        private void WindowClose(object sender, RoutedEventArgs e) //Close window when user click "Close"
        {
            Close(); //Closes window (Application)
        }

        #region ScaleValue Depdency Property
        public static readonly DependencyProperty ScaleValueProperty = DependencyProperty.Register("ScaleValue", typeof(double), typeof(MainWindow), new UIPropertyMetadata(1.0, new PropertyChangedCallback(OnScaleValueChanged), new CoerceValueCallback(OnCoerceScaleValue)));

        private static object OnCoerceScaleValue(DependencyObject o, object value)
        {
            MainWindow mainWindow = o as MainWindow;
            if (mainWindow != null)
                return mainWindow.OnCoerceScaleValue((double)value);
            else return value;
        }

        private static void OnScaleValueChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
        {
            MainWindow mainWindow = o as MainWindow;
            if (mainWindow != null)
                mainWindow.OnScaleValueChanged((double)e.OldValue, (double)e.NewValue);
        }

        protected virtual double OnCoerceScaleValue(double value)
        {
            if (double.IsNaN(value))
                return 1.0f;

            value = Math.Max(0.1, value);
            return value;
        }

        protected virtual void OnScaleValueChanged(double oldValue, double newValue) { }

        public double ScaleValue
        {
            get => (double)GetValue(ScaleValueProperty);
            set => SetValue(ScaleValueProperty, value);
        }
        #endregion

        #region Check Scale and Apply
        private void MainGrid_SizeChanged(object sender, EventArgs e) => CalculateScale();

        private void CalculateScale()
        {
            double yScale = ActualHeight / 910f; //Height specified in .xaml window-properties
            double xScale = ActualWidth / 700f; //Width specified in .xaml window-properties
            double value = Math.Min(xScale, yScale);

            ScaleValue = (double)OnCoerceScaleValue(myMainWindow, value);
        }
        #endregion
    }
}