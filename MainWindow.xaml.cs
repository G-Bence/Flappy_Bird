using System.Security.Policy;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using WpfAnimatedGif;

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

            public bool HasScored { get; set; } = false;

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

        private bool isRaining = false;
        private int rainStartScore = 0;
        private int nextRainStartScore = 0;

        private DispatcherTimer rainTimer;
        private const int RainDurationSeconds = 15;
        private const int RainMaxScoreGain = 10;

        //private const double RainDimOpacity = 0.35;
        //private const double RainImageOpacity = 0.80;
        private const double RainJumpMultiplier = 0.70;


        private bool isFoggy = false;
        private int fogStartScore = 0;
        private int nextFogStartScore = 0;
        private DispatcherTimer fogTimer;

        private const int FogDurationSeconds = 15;
        private const int FogMaxScoreGain = 10;

        private const double FogImageTargetOpacity = 0.75;
        private const double FogDimContribution = 0.20;


        private const double RainDimContribution = 0.20;
        private const double MaxDimOpacity = 0.40;  
        private const double RainImageTargetOpacity = 0.80;

        private double normalJumpStrength;

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

            StopRain(immediate: true);
            StopFog(immediate: true);
        }

        private void RestartGame()
        {
            GameOverOverlay.Visibility = Visibility.Collapsed;

            StopRain(immediate: true);
            StopFog(immediate: true);

            SetNextRainStartScore();
            SetNextFogStartScore();

            isGameOver = false;
            score = 0;

            birdX = 30;
            birdY = 200;
            birdVelocityY = 0;

            score = 0;
            ScoreText.Text = "Score: 0";

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
            pair.HasScored = false;

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

            Rect birdRect = new Rect(birdLeft + Bird.Width * 0.3, birdTop+ Bird.Height * 0.3, Bird.Width*0.7, Bird.Height*0.7);


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

        private void UpdateScore()
        {
            double birdLeft = Canvas.GetLeft(Bird);

            foreach (var pair in pipePairs)
            {
                double pipeRight = pair.X + PipeWidth;

                if (!pair.HasScored && pipeRight < birdLeft)
                {
                    score++;
                    pair.HasScored = true;
                    ScoreText.Text = $"Score: {score}";

                    if (isRaining && (score - rainStartScore) >= RainMaxScoreGain)
                    {
                        StopRain();
                    }

                    if (isFoggy && (score - fogStartScore) >= FogMaxScoreGain)
                    {
                        StopFog();
                    }



                    if (!isRaining && score >= nextRainStartScore)
                    //if (!isRaining && score >= 0)
                    {
                        StartRain();
                    }


                    if (!isFoggy && score >= nextFogStartScore)
                    //if (!isFoggy && score >= 0)
                    {
                        StartFog();
                    }
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





        private void SetupRain()
        {
            var gif = new BitmapImage(new Uri("Rain.gif", UriKind.Relative));
            ImageBehavior.SetAnimatedSource(RainImage, gif);
            ImageBehavior.SetRepeatBehavior(RainImage, RepeatBehavior.Forever);

            rainTimer = new DispatcherTimer();
            rainTimer.Interval = TimeSpan.FromSeconds(RainDurationSeconds);
            rainTimer.Tick += RainTimer_Tick;

            RainOverlay.Visibility = Visibility.Collapsed;
            RainDim.Opacity = 0;
            RainImage.Opacity = 0;
        }
        private void SetNextRainStartScore()
        {
            int offset = rand.Next(5, 12);
            nextRainStartScore = score + offset;
        }



        private void StartRain()
        {
            if (isGameOver || isRaining) return;

            isRaining = true;
            rainStartScore = score;

            jumpStrength = normalJumpStrength * RainJumpMultiplier;

            UpdateWeatherOverlay();

            rainTimer.Stop();
            rainTimer.Start();
        }

        private void StopRain(bool immediate = false)
        {
            if (!isRaining && !immediate) return;

            isRaining = false;
            rainTimer?.Stop();

            jumpStrength = normalJumpStrength;

            if (immediate)
            {
                RainDim.BeginAnimation(UIElement.OpacityProperty, null);
                RainImage.BeginAnimation(UIElement.OpacityProperty, null);

                RainDim.Opacity = 0;
                RainImage.Opacity = 0;
                RainOverlay.Visibility = Visibility.Collapsed;
            }
            else
            {
                BeginRainFadeOut();
            }

            SetNextRainStartScore();
        }


        /*
        private void BeginRainFadeIn()
        {
            RainOverlay.Visibility = Visibility.Visible;

            RainImage.Opacity = 0;

            var dimAnim = new DoubleAnimation
            {
                From = RainDim.Opacity,
                To = RainDimOpacity,
                Duration = TimeSpan.FromMilliseconds(600)
            };

            dimAnim.Completed += (s, e) =>
            {
                var rainAnim = new DoubleAnimation
                {
                    From = RainImage.Opacity,
                    To = RainImageOpacity,
                    Duration = TimeSpan.FromMilliseconds(400)
                };
                RainImage.BeginAnimation(UIElement.OpacityProperty, rainAnim);
            };

            RainDim.BeginAnimation(UIElement.OpacityProperty, dimAnim);
        }*/

        private void BeginRainFadeOut()
        {
            var rainOut = new DoubleAnimation
            {
                From = RainImage.Opacity,
                To = 0,
                Duration = TimeSpan.FromMilliseconds(400)
            };

            rainOut.Completed += (s, e) =>
            {
                var dimOut = new DoubleAnimation
                {
                    From = RainDim.Opacity,
                    To = 0,
                    Duration = TimeSpan.FromMilliseconds(600)
                };

                dimOut.Completed += (s2, e2) =>
                {
                    RainOverlay.Visibility = Visibility.Collapsed;
                };

                RainDim.BeginAnimation(UIElement.OpacityProperty, dimOut);
            };

            RainImage.BeginAnimation(UIElement.OpacityProperty, rainOut);
        }

        private void RainTimer_Tick(object sender, EventArgs e)
        {
            StopRain();
        }





        private void SetupFog()
        {
            FogImage.Source = new BitmapImage(new Uri("fog_overlay.jpg", UriKind.Relative));

            fogTimer = new DispatcherTimer();
            fogTimer.Interval = TimeSpan.FromSeconds(FogDurationSeconds);
            fogTimer.Tick += (s, e) => StopFog();
        }

        private void SetNextFogStartScore()
        {
            int offset = rand.Next(6, 15);
            nextFogStartScore = score + offset;
        }


        private void StartFog()
        {
            if (isGameOver || isFoggy) return;

            isFoggy = true;
            fogStartScore = score;

            fogTimer.Stop();
            fogTimer.Start();

            UpdateWeatherOverlay();
        }

        private void StopFog(bool immediate = false)
        {
            if (!isFoggy && !immediate) return;

            isFoggy = false;
            fogTimer?.Stop();

            if (immediate)
            {
                FogImage.BeginAnimation(UIElement.OpacityProperty, null);
                FogImage.Opacity = 0;
            }

            UpdateWeatherOverlay(immediate);
            SetNextRainStartScore();
        }


        private void UpdateWeatherOverlay(bool immediate = false)
        {
            bool anyWeather = isRaining || isFoggy;

            if (!anyWeather)
            {
                if (immediate)
                {
                    RainOverlay.Visibility = Visibility.Collapsed;
                    RainDim.Opacity = 0;
                    RainImage.Opacity = 0;
                    FogImage.Opacity = 0;
                    return;
                }

                AnimateOpacity(RainImage, 0, 300);
                AnimateOpacity(FogImage, 0, 300);
                var dimOut = new DoubleAnimation
                {
                    From = RainDim.Opacity,
                    To = 0,
                    Duration = TimeSpan.FromMilliseconds(500)
                };
                dimOut.Completed += (s, e) => RainOverlay.Visibility = Visibility.Collapsed;
                RainDim.BeginAnimation(UIElement.OpacityProperty, dimOut);
                return;
            }

            RainOverlay.Visibility = Visibility.Visible;

            double dimTarget = 0;
            if (isRaining) dimTarget += RainDimContribution;
            if (isFoggy) dimTarget += FogDimContribution;
            dimTarget = Math.Min(dimTarget, MaxDimOpacity);

            double rainTarget = isRaining ? RainImageTargetOpacity * (isFoggy ? 0.75 : 1.0) : 0;
            double fogTarget = isFoggy ? FogImageTargetOpacity * (isRaining ? 0.85 : 1.0) : 0;

            if (immediate)
            {
                RainDim.Opacity = dimTarget;
                RainImage.Opacity = rainTarget;
                FogImage.Opacity = fogTarget;
                return;
            }

            AnimateOpacity(RainDim, dimTarget, 600);
            AnimateOpacity(RainImage, rainTarget, 400, beginMs: (rainTarget > RainImage.Opacity) ? 200 : 0);
            AnimateOpacity(FogImage, fogTarget, 400, beginMs: (fogTarget > FogImage.Opacity) ? 200 : 0);
        }


        private void AnimateOpacity(UIElement element, double to, int durationMs, int beginMs = 0)
        {
            var anim = new DoubleAnimation
            {
                From = element.Opacity,
                To = to,
                Duration = TimeSpan.FromMilliseconds(durationMs),
                BeginTime = TimeSpan.FromMilliseconds(beginMs)
            };

            element.BeginAnimation(UIElement.OpacityProperty, anim);
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
            UpdateScore();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            birdX = 30;
            birdY = 200;
            birdVelocityY = 0;
            score = 0;

            score = 0;
            ScoreText.Text = "Score: 0";

            pipeSource = new BitmapImage(new Uri("pipe-green.png", UriKind.Relative));

            gravity = 0.6;
            normalJumpStrength = -9.5;
            jumpStrength = normalJumpStrength;

            Canvas.SetLeft(Bird, birdX);
            Canvas.SetTop(Bird, birdY);

            gameTimer = new DispatcherTimer();
            gameTimer.Interval = TimeSpan.FromMilliseconds(16);
            gameTimer.Tick += GameLoop;
            gameTimer.Start();

            Dispatcher.BeginInvoke(new Action(InitPipes), DispatcherPriority.Loaded);

            SetupRain();
            SetupFog();

            SetNextRainStartScore();
            SetNextFogStartScore();
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