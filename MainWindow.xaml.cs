using System.Security.Policy;
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
        private readonly Random rand = new Random();

        public class PipePair
        {
            public Image Top { get; }
            public Image Bottom { get; }

            public double X { get; private set; }

            public PipePair(Image top, Image bottom, double x)
            {
                Top = top;
                Bottom = bottom;
                X = x;
            }

            public void SetX(double x)
            {
                X = x;
                Canvas.SetLeft(Top, X);
                Canvas.SetLeft(Bottom, X);
            }
        }

        private double birdX;
		private double birdY;
		private double birdVelocityY;

        private int score;
        private bool isGameOver = false;

        private double gravity;
		private double jumpStrength;

		private DispatcherTimer gameTimer;
        private ImageSource pipeSource;

        private readonly List<PipePair> pipePairs = new List<PipePair>();

        private const double PipeWidth = 70;
        private const double PipeGap = 160;        
        private const double PipeSpeed = 1.5;     
        private const double PipeSpacing = 750;   
        private const double MinPipeHeight = 60;



        public MainWindow()
        {
            InitializeComponent();
        }

        private void GameOver()
        {
            if (isGameOver) return;

            isGameOver = true;
            gameTimer.Stop();

            FinalScoreText.Text = $"Score: {score}";
            GameOverOverlay.Visibility = Visibility.Visible;
        }

        private void RestartGame()
        {
            GameOverOverlay.Visibility = Visibility.Collapsed;

            isGameOver = false;
            score = 0;

            birdX = 30;
            birdY = 200;
            birdVelocityY = 0;

            Canvas.SetLeft(Bird, birdX);
            Canvas.SetTop(Bird, birdY);
            InitPipes();

            gameTimer.Start();
        }


        private void InitPipes()
        {
            foreach (var pair in pipePairs)
            {
                GameCanvas.Children.Remove(pair.Top);
                GameCanvas.Children.Remove(pair.Bottom);
            }
            pipePairs.Clear();

            double startX = GameCanvas.ActualWidth + 100;

            PipePair first = CreatePipePair(startX);
            pipePairs.Add(first);
        }

        private PipePair CreatePipePair(double x)
        {
            Image topPipe = new Image
            {
                Width = PipeWidth,
                Source = pipeSource,
                Stretch = Stretch.Fill,
                RenderTransformOrigin = new Point(0.5, 0.5),
                RenderTransform = new RotateTransform(180)
            };

            Image bottomPipe = new Image
            {
                Width = PipeWidth,
                Source = pipeSource,
                Stretch = Stretch.Fill
            };


            GameCanvas.Children.Add(topPipe);
            GameCanvas.Children.Add(bottomPipe);

            PipePair pair = new PipePair(topPipe, bottomPipe, x);
            RandomizePipePair(pair);
            pair.SetX(x);

            return pair;
        }

        private void RandomizePipePair(PipePair pair)
        {
            double canvasH = GameCanvas.ActualHeight;
            //if (canvasH <= 0) return;

            double maxTopHeight = canvasH - PipeGap - MinPipeHeight;
            double topHeight = rand.Next((int)MinPipeHeight, (int)maxTopHeight);

            double bottomHeight = canvasH - PipeGap - topHeight;

            pair.Top.Height = topHeight;
            pair.Bottom.Height = bottomHeight;

            Canvas.SetTop(pair.Top, 0);
            Canvas.SetTop(pair.Bottom, topHeight + PipeGap);
        }

        private void CheckCollisions()
        {
            //if (GameCanvas.ActualHeight <= 0 || GameCanvas.ActualWidth <= 0) return;

            double birdLeft = Canvas.GetLeft(Bird);
            double birdTop = Canvas.GetTop(Bird);

            Rect birdRect = new Rect(birdLeft, birdTop, Bird.Width, Bird.Height);


            if (birdRect.Top <= 0 || birdRect.Bottom >= GameCanvas.ActualHeight)
            {
                GameOver();
                return;
            }

            foreach (var pair in pipePairs)
            {
                Rect topPipeRect = new Rect(pair.X, 0, pair.Top.Width, pair.Top.Height);

                double bottomY = Canvas.GetTop(pair.Bottom);
                Rect bottomPipeRect = new Rect(pair.X, bottomY, pair.Bottom.Width, pair.Bottom.Height);

                if (birdRect.IntersectsWith(topPipeRect) || birdRect.IntersectsWith(bottomPipeRect))
                {
                    GameOver();
                    return;
                }
            }
        }

        private void MovePipes()
        {
            foreach (var pair in pipePairs)
            {
                pair.SetX(pair.X - PipeSpeed);
            }

            for (int i = pipePairs.Count - 1; i >= 0; i--)
            {
                if (pipePairs[i].X + PipeWidth < 0)
                {
                    GameCanvas.Children.Remove(pipePairs[i].Top);
                    GameCanvas.Children.Remove(pipePairs[i].Bottom);
                    pipePairs.RemoveAt(i);
                }
            }

            foreach (var pair in pipePairs)
            {
                pair.SetX(pair.X - PipeSpeed);
            }


            double spawnX = GameCanvas.ActualWidth + 100;

            /*
            if (pipePairs.Count == 0)
            {
                pipePairs.Add(CreatePipePair(spawnX));
                return;
            }*/

            PipePair last = pipePairs[pipePairs.Count - 1];

            if (spawnX - last.X >= PipeSpacing)
            {
                pipePairs.Add(CreatePipePair(spawnX));
            }
        }


        private void Jump()
		{
            if (isGameOver) return;
            birdVelocityY = jumpStrength;
        }

        private void GameLoop(object sender, EventArgs e)
        {
            if (isGameOver) return;

            birdVelocityY += gravity;
            birdY += birdVelocityY;
            Canvas.SetTop(Bird, birdY);

            /*
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
			}*/

            MovePipes();

            CheckCollisions();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            birdX = 30;
            birdY = 200;
            birdVelocityY = 0;
            score = 0;

            pipeSource = new BitmapImage(new Uri("pipe-green.png", UriKind.Relative));

            gravity = 0.6;
            jumpStrength = -9.5;

            Canvas.SetLeft(Bird, birdX);
            Canvas.SetTop(Bird, birdY);

            gameTimer = new DispatcherTimer();
            gameTimer.Interval = TimeSpan.FromMilliseconds(16);
            gameTimer.Tick += GameLoop;
            gameTimer.Start();

            Dispatcher.BeginInvoke(new Action(InitPipes), DispatcherPriority.Loaded);
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

        private void RestartButton_Click(object sender, RoutedEventArgs e)
        {
            RestartGame();
        }
    }
}