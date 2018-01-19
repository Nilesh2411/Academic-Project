using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.Kinect;
using Microsoft.Speech.Recognition;
using Microsoft.Speech.AudioFormat;
using System.IO;


namespace DrawShapeFromSpeechLec12
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        KinectSensor sensor;
        Stream audioStream;
        SpeechRecognitionEngine speechEngine;

        Skeleton[] totalSkeleton = new Skeleton[6];
        WriteableBitmap colorBitmap;
        byte[] colorPixels;
        Skeleton skeleton;
        int currentSkeletonID = 0;

        public MainWindow()
        {
            InitializeComponent();
            Loaded += new RoutedEventHandler(Window_Loaded);
        }
        private void Window_Closed(object sender, EventArgs e)
        {

        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            this.sensor = KinectSensor.KinectSensors[0];
            this.sensor.ColorStream.Enable();
            this.sensor.SkeletonStream.TrackingMode = SkeletonTrackingMode.Default;
            this.sensor.SkeletonStream.Enable();
            this.colorPixels = new byte[this.sensor.ColorStream.FramePixelDataLength];
            this.colorBitmap = new WriteableBitmap(this.sensor.ColorStream.FrameWidth, this.sensor.ColorStream.FrameHeight, 96.0, 96.0, PixelFormats.Bgr32, null);
            this.image.Source = this.colorBitmap;

            this.sensor.ColorFrameReady += this.colorFrameReady;
            this.sensor.SkeletonFrameReady += skeletonFrameReady;
            this.sensor.Start();

            audioStream = this.sensor.AudioSource.Start();
            RecognizerInfo recognizerInfo = GetKinectRecognizer();
            if (recognizerInfo == null)
            {
                MessageBox.Show("Could not find Kinect speech recognizer");
                return;
            }

            BuildGrammarforRecognizer(recognizerInfo); // provided earlier
            statusBar.Text = "Speech Recognizer is ready";
        }
        private void speechRecognitionRejected(object sender, SpeechRecognitionRejectedEventArgs e)
        {

        }

        private void speechHypothesized(object sender, SpeechHypothesizedEventArgs e)
        {
            wordsTenative.Text = e.Result.Text;
        }

        private void speechRecognized(object sender, SpeechRecognizedEventArgs e1)
        {
            wordsRecognized.Text = e1.Result.Text;
            confidenceTxt.Text = e1.Result.Confidence.ToString();
            float confidenceThreshold = 0.2f;
            if (e1.Result.Confidence > confidenceThreshold)
            {
                CommandsParser(e1);
            }
        }
        public static int lefthanddraw = 0, righthanddraw = 0;
        Shape drawObject;
        Color objectColor;
        public void CommandsParser(SpeechRecognizedEventArgs e1)
        {
           
            var result = e1.Result;


            System.Collections.ObjectModel.ReadOnlyCollection<RecognizedWordUnit> words = e1.Result.Words;

            if (words[0].Text == "draw")
            {
                string colorObject = words[1].Text;
                switch (colorObject)
                {
                    case "red":
                        
                        objectColor = Colors.Red;
                        break;
                    case "green":
                       
                        objectColor = Colors.Green;
                        break;
                    case "blue":
                    
                        objectColor = Colors.Blue;
                        break;
                    case "yellow":
                       
                        objectColor = Colors.Yellow;
                        break;
                    
                    default:
                        return;
                }
                var shapeString = words[2].Text;
                switch (shapeString)
                {
                    case "circle":
                       
                        drawObject = new Ellipse();
                        drawObject.Width = 25; drawObject.Height = 25;
                        break;
                    case "square":
                       
                        drawObject = new Rectangle();
                        drawObject.Width = 25; drawObject.Height = 25;
                        break;
                    case "rectangle":
                       
                        drawObject = new Rectangle();
                        drawObject.Width = 25; drawObject.Height = 15;
                        break;
                    case "triangle":
                        
                        var polygon = new Polygon();
                        polygon.Points.Add(new Point(0, 8));
                        polygon.Points.Add(new Point(-15, -8));
                        polygon.Points.Add(new Point(15, -8));
                        drawObject = polygon;
                        break;
                    default:
                        return;
                }

                if (words[3].Text == "right" && words[4].Text == "hand")
                {
                    righthanddraw = 1;
                    lefthanddraw = 0;
                    
                }

                else if (words[3].Text == "left" && words[4].Text == "hand")
                {
                    lefthanddraw = 1;
                    righthanddraw = 0;
                }

                /*if (words[3].Text == "right" && words[4].Text == "knee")
                {
                    Point rightknee = ScalePosition(skeleton.Joints[JointType.KneeRight].Position);
                    canvas2.Children.Clear();
                    drawObject.SetValue(Canvas.LeftProperty, rightknee.X);
                    drawObject.SetValue(Canvas.TopProperty, rightknee.Y);
                }

                if (words[3].Text == "left" && words[4].Text == "knee")
                {
                    Point leftknee = ScalePosition(skeleton.Joints[JointType.KneeLeft].Position);
                    canvas2.Children.Clear();
                    drawObject.SetValue(Canvas.LeftProperty, leftknee.X);
                    drawObject.SetValue(Canvas.TopProperty, leftknee.Y);
                }
                */

                drawObject.Fill = new SolidColorBrush(objectColor);


            }

            if (words[0].Text == "close" && words[1].Text == "the" && words[2].Text == "application")
            {
                this.Close();
            }
        }

        private void BuildGrammarforRecognizer(RecognizerInfo recognizerInfo)
        {
            var grammarBuilder = new GrammarBuilder { Culture = recognizerInfo.Culture };
            // first say Draw
            grammarBuilder.Append(new Choices("draw"));
            var colorObjects = new Choices();
            colorObjects.Add("red"); colorObjects.Add("green"); colorObjects.Add("blue");
            colorObjects.Add("yellow"); colorObjects.Add("gray");
            // New Grammar builder for color
            grammarBuilder.Append(colorObjects);

            // Another Grammar Builder for object
            grammarBuilder.Append(new Choices("circle", "square", "triangle", "rectangle"));

            // Another Grammar Builder for object
            grammarBuilder.Append(new Choices("left", "right"));

            // Another Grammar Builder for object
            grammarBuilder.Append(new Choices("hand", "knee"));

            // Create Grammar from GrammarBuilder
            var grammar = new Grammar(grammarBuilder);

            // Creating another Grammar and load
            var newGrammarBuilder = new GrammarBuilder();
            newGrammarBuilder.Append("close the application");
            var grammarClose = new Grammar(newGrammarBuilder);
            // Start the speech recognizer
            speechEngine = new SpeechRecognitionEngine(recognizerInfo.Id);
            speechEngine.LoadGrammar(grammar); // loading grammer into recognizer
            speechEngine.LoadGrammar(grammarClose);

            // Attach the speech audio source to the recognizer
            int SamplesPerSecond = 16000; int bitsPerSample = 16;
            int channels = 1; int averageBytesPerSecond = 32000; int blockAlign = 2;
            speechEngine.SetInputToAudioStream(
                 audioStream, new SpeechAudioFormatInfo(EncodingFormat.Pcm,
                 SamplesPerSecond, bitsPerSample, channels, averageBytesPerSecond,
                  blockAlign, null));

            // Register the event handler for speech recognition
            speechEngine.SpeechRecognized += speechRecognized;
            speechEngine.SpeechHypothesized += speechHypothesized;
            speechEngine.SpeechRecognitionRejected += speechRecognitionRejected;

            speechEngine.RecognizeAsync(RecognizeMode.Multiple);
        }
        private static RecognizerInfo GetKinectRecognizer()
        {
            foreach (RecognizerInfo recognizer in SpeechRecognitionEngine.InstalledRecognizers())
            {
                string value;
                recognizer.AdditionalInfo.TryGetValue("Kinect", out value);
                if ("True".Equals(value, StringComparison.OrdinalIgnoreCase) && "en-US".Equals(recognizer.Culture.Name, StringComparison.OrdinalIgnoreCase))
                {
                    return recognizer;
                }
            }
            return null;
        }

        void skeletonFrameReady(object sender, SkeletonFrameReadyEventArgs e)
        {
            canvas1.Children.Clear();       
            
            using (SkeletonFrame skeletonFrame = e.OpenSkeletonFrame())
            {
                if (skeletonFrame == null) { return; }
                skeletonFrame.CopySkeletonDataTo(totalSkeleton);
                skeleton = (from trackskeleton in totalSkeleton
                            where trackskeleton.TrackingState == SkeletonTrackingState.Tracked
                            select trackskeleton).FirstOrDefault();
                if (skeleton == null)
                    return;
                if (skeleton != null && this.currentSkeletonID != skeleton.TrackingId)
                {
                    this.currentSkeletonID = skeleton.TrackingId;
                    int totalTrackedJoints = skeleton.Joints.Where(item => item.TrackingState == JointTrackingState.Tracked).Count();
                    string TrackedTime = DateTime.Now.ToString("hh:mm:ss");
                    //string status = "Skeleton Id: " + this.currentSkeletonID + ", total tracked joints: " + totalTrackedJoints + ", TrackTime: " + TrackedTime + "\n";
                   // this.textBlock1.Text += status;
                }
                
                DrawSkeleton(skeleton,righthanddraw,lefthanddraw);
            }
        }
        private void DrawSkeleton(Skeleton skeleton,int righthanddraw, int lefthanddraw)
        {
            drawBone(skeleton.Joints[JointType.Head], skeleton.Joints[JointType.ShoulderCenter], righthanddraw, lefthanddraw);
            drawBone(skeleton.Joints[JointType.ShoulderCenter], skeleton.Joints[JointType.Spine], righthanddraw, lefthanddraw);

            drawBone(skeleton.Joints[JointType.ShoulderCenter], skeleton.Joints[JointType.ShoulderLeft], righthanddraw, lefthanddraw);
            drawBone(skeleton.Joints[JointType.ShoulderLeft], skeleton.Joints[JointType.ElbowLeft], righthanddraw, lefthanddraw);
            drawBone(skeleton.Joints[JointType.ElbowLeft], skeleton.Joints[JointType.WristLeft], righthanddraw, lefthanddraw);
            drawBone(skeleton.Joints[JointType.WristLeft], skeleton.Joints[JointType.HandLeft], righthanddraw, lefthanddraw);

            drawBone(skeleton.Joints[JointType.ShoulderCenter], skeleton.Joints[JointType.ShoulderRight], righthanddraw, lefthanddraw);
            drawBone(skeleton.Joints[JointType.ShoulderRight], skeleton.Joints[JointType.ElbowRight], righthanddraw, lefthanddraw);
            drawBone(skeleton.Joints[JointType.ElbowRight], skeleton.Joints[JointType.WristRight], righthanddraw, lefthanddraw);
            drawBone(skeleton.Joints[JointType.WristRight], skeleton.Joints[JointType.HandRight], righthanddraw, lefthanddraw);
            drawBone(skeleton.Joints[JointType.Spine], skeleton.Joints[JointType.HipCenter], righthanddraw, lefthanddraw);
            drawBone(skeleton.Joints[JointType.HipCenter], skeleton.Joints[JointType.HipLeft], righthanddraw, lefthanddraw);
            drawBone(skeleton.Joints[JointType.HipLeft], skeleton.Joints[JointType.KneeLeft], righthanddraw, lefthanddraw);
            drawBone(skeleton.Joints[JointType.KneeLeft], skeleton.Joints[JointType.AnkleLeft], righthanddraw, lefthanddraw);
            drawBone(skeleton.Joints[JointType.AnkleLeft], skeleton.Joints[JointType.FootLeft], righthanddraw, lefthanddraw);

            drawBone(skeleton.Joints[JointType.HipCenter], skeleton.Joints[JointType.HipRight], righthanddraw, lefthanddraw);
            drawBone(skeleton.Joints[JointType.HipRight], skeleton.Joints[JointType.KneeRight], righthanddraw, lefthanddraw);
            drawBone(skeleton.Joints[JointType.KneeRight], skeleton.Joints[JointType.AnkleRight], righthanddraw, lefthanddraw);
            drawBone(skeleton.Joints[JointType.AnkleRight], skeleton.Joints[JointType.FootRight], righthanddraw, lefthanddraw);
        }
        void drawBone(Joint trackedJoint1, Joint trackedJoint2, int righthanddraw, int lefthanddraw)
        {
            Line bone = new Line();
            bone.Stroke = Brushes.Red;
            bone.StrokeThickness = 3;
            Point joint1 = this.ScalePosition(trackedJoint1.Position);
            bone.X1 = joint1.X;
            bone.Y1 = joint1.Y;

            Point joint2 = this.ScalePosition(trackedJoint2.Position);
            bone.X2 = joint2.X;
            bone.Y2 = joint2.Y;

            canvas2.Children.Clear();
            if (righthanddraw == 1)
            {
                Point righthand = ScalePosition(skeleton.Joints[JointType.HandRight].Position);
                
                drawObject.SetValue(Canvas.LeftProperty, righthand.X-15);
                drawObject.SetValue(Canvas.TopProperty, righthand.Y-20);
                canvas2.Children.Add(drawObject);
            }
            else if (lefthanddraw == 1)
            {
                Point lefthand = ScalePosition(skeleton.Joints[JointType.HandLeft].Position);
                
                drawObject.SetValue(Canvas.LeftProperty, lefthand.X-15);
                drawObject.SetValue(Canvas.TopProperty, lefthand.Y-20);
                canvas2.Children.Add(drawObject);
            }
                       
            //canvas1.Children.Add(bone);
            
        }
        private Point ScalePosition(SkeletonPoint skeletonPoint)
        {
            ColorImagePoint depthPoint = this.sensor.CoordinateMapper.MapSkeletonPointToColorPoint(skeletonPoint, ColorImageFormat.RgbResolution640x480Fps30);
            return new Point(depthPoint.X, depthPoint.Y);
        }
        void colorFrameReady(object sender, ColorImageFrameReadyEventArgs e)
        {
            using (ColorImageFrame imageFrame = e.OpenColorImageFrame())
            {
                if (null == imageFrame)
                    return;

                imageFrame.CopyPixelDataTo(colorPixels);
                int stride = imageFrame.Width * imageFrame.BytesPerPixel;

                // Write the pixel data into our bitmap
                this.colorBitmap.WritePixels(
                    new Int32Rect(0, 0, this.colorBitmap.PixelWidth , this.colorBitmap.PixelHeight),
                        this.colorPixels, stride, 0);
            }
        }
    }
}

