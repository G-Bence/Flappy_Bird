using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace Flappy_Bird
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
		private double birdX;
		private double birdY;
		private double birdVelocityY;

		private double gravity;
		private double jumpStrength;

		private DispatcherTimer gameTimer;
            
		public MainWindow()
        {
            InitializeComponent();
        }

		private void Window_Loaded(object sender, RoutedEventArgs e)
		{
			birdX = 30;
			birdY = 200;
			birdVelocityY = 0;

			gravity = 0.6;
			jumpStrength = -9.5;

			Canvas.SetLeft(Bird, birdX);
			Canvas.SetTop(Bird, birdY);

			gameTimer = new DispatcherTimer();
			gameTimer.Interval = TimeSpan.FromMilliseconds(16);
			gameTimer.Tick += GameLoop;
			gameTimer.Start();
		}

		private void GameLoop(object sender, EventArgs e)
		{
			birdVelocityY += gravity;
			birdY += birdVelocityY;

			double floor = GameCanvas.ActualHeight - Bird.Height;
			if (birdY < 0)
			{
				birdY = 0;
				birdVelocityY = 0;
			}
			if (birdY > floor)
			{
				birdY = floor;
				birdVelocityY = 0;
			}

			Canvas.SetTop(Bird, birdY);
		}

		private void Jump()
		{
			birdVelocityY = jumpStrength;
		}

		private void Window_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.Key == Key.Space)
			{
				Jump();
			}
		}


		private void Window_MouseDown(object sender, MouseButtonEventArgs e)
		{
			Jump();
		}
	}
}