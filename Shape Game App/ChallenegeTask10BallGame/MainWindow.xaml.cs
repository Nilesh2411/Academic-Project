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
using System.IO;
using System.Windows.Media.Media3D;

namespace ChallenegeTask10BallGame
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        KinectSensor sensor;
        Skeleton[] totalSkeleton = new Skeleton[6];
        WriteableBitmap colorBitmap;
        byte[] colorPixels;
        Skeleton skeleton;
        Thing thing = new Thing(); // a struct for ball
        double gravity = 0.15;
        public int count;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void Window_Closed(object sender, EventArgs e)
        {

        }

        private static double SquaredDistance(double x1, double y1, double x2, double y2)
        {
            return ((x2 - x1) * (x2 - x1)) + ((y2 - y1) * (y2 - y1));
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            if (KinectSensor.KinectSensors.Count > 0)
            {
                this.sensor = KinectSensor.KinectSensors[0];
                if (this.sensor != null && !this.sensor.IsRunning)
                {
                    this.sensor.ColorStream.Enable();
                    this.sensor.SkeletonStream.TrackingMode = SkeletonTrackingMode.Default;
                    this.sensor.SkeletonStream.Enable();
                    this.colorPixels = new byte[this.sensor.ColorStream.FramePixelDataLength];
                    this.colorBitmap = new WriteableBitmap(this.sensor.ColorStream.FrameWidth, this.sensor.ColorStream.FrameHeight, 96.0, 96.0, PixelFormats.Bgr32, null);
                    this.image.Source = this.colorBitmap;
                    this.sensor.ColorFrameReady += this.colorFrameReady;
                    this.sensor.SkeletonFrameReady += skeletonFrameReady;
                    this.sensor.Start();

                    thing.Shape = new Ellipse();
                    thing.Shape.Width = 50; thing.Shape.Height = 50;
                    thing.Shape.Fill = new SolidColorBrush(Color.FromRgb(0, 0, 0));
                    thing.Center.X = 320; thing.Center.Y = 0;
                    thing.Shape.SetValue(Canvas.LeftProperty, thing.Center.X - thing.Shape.Width);
                    thing.Shape.SetValue(Canvas.TopProperty, thing.Center.Y - thing.Shape.Width);
                    canvas1.Children.Add(thing.Shape);

                    // create the smooth parameters
                    var smoothParameters = new TransformSmoothParameters
                    {
                        Correction = 0.1f,
                        JitterRadius = 0.05f,
                        MaxDeviationRadius = 0.05f,
                        Prediction = 0.1f,
                        Smoothing = 0.5f
                    };

                    // Enable the skeleton stream with smooth parameters
                    this.sensor.SkeletonStream.Enable(smoothParameters);

                }
                else
                {
                    MessageBox.Show("No device is connected!");
                    this.Close();
                }
            }
        }
        private struct Thing
        {
            public System.Windows.Point Center;
            public double YVelocity;
            public double XVelocity;
            public Ellipse Shape;
            public bool Yvelocity_stat;
            public bool Hit(System.Windows.Point joint)
            {
                double minDxSquared = this.Shape.RenderSize.Width;
                minDxSquared *= minDxSquared;
                double dist = SquaredDistance(Center.X, Center.Y, joint.X, joint.Y);
                if (dist <= minDxSquared && this.Yvelocity_stat == false)
                {
                    return true;
                }
                else
                    return false;


            }
        }


        void skeletonFrameReady(object sender, SkeletonFrameReadyEventArgs e)
        {
            canvas1.Children.Clear();
            advanceThingPosition();
            canvas1.Children.Add(thing.Shape);
            using (SkeletonFrame skeletonFrame = e.OpenSkeletonFrame())
            {
                if (skeletonFrame == null) { return; }
                skeletonFrame.CopySkeletonDataTo(totalSkeleton);
                skeleton = (from trackskeleton in totalSkeleton
                            where trackskeleton.TrackingState == SkeletonTrackingState.Tracked
                            select trackskeleton).FirstOrDefault();
                if (skeleton == null)
                    return;
                /* if (skeleton != null && this.currentSkeletonID != skeleton.TrackingId)
                 {
                     this.currentSkeletonID = skeleton.TrackingId;
                     int totalTrackedJoints = skeleton.Joints.Where(item => item.TrackingState == JointTrackingState.Tracked).Count();
                     string TrackedTime = DateTime.Now.ToString("hh:mm:ss");
                     string status = "Skeleton Id: " + this.currentSkeletonID + ", total tracked joints: " + totalTrackedJoints + ", TrackTime: " + TrackedTime + "\n";

                 }*/
                DrawSkeleton(skeleton);
                //angle calculation
                Vector3D Spine = new Vector3D(skeleton.Joints[JointType.Spine].Position.X, skeleton.Joints[JointType.Spine].Position.Y, skeleton.Joints[JointType.Spine].Position.Z);
                Vector3D ShoulderCenter = new Vector3D(skeleton.Joints[JointType.ShoulderCenter].Position.X, skeleton.Joints[JointType.ShoulderCenter].Position.Y, skeleton.Joints[JointType.ShoulderCenter].Position.Z);
                Vector3D WristLeft = new Vector3D(skeleton.Joints[JointType.WristLeft].Position.X, skeleton.Joints[JointType.WristLeft].Position.Y, skeleton.Joints[JointType.WristLeft].Position.Z);
                Vector3D WristRight = new Vector3D(skeleton.Joints[JointType.WristRight].Position.X, skeleton.Joints[JointType.WristRight].Position.Y, skeleton.Joints[JointType.WristRight].Position.Z);
                double LeftSideAngle = Angle(ShoulderCenter - WristLeft, Spine - ShoulderCenter);
                double RightSideAngle = Angle(ShoulderCenter - WristRight, Spine - ShoulderCenter);
                //this.textBox1.Text = ("Angle between leftarm and torso is " + LeftSideAngle.ToString() + "Angle between right arm and torso is " + RightSideAngle.ToString());

                Point handPt = ScalePosition(skeleton.Joints[JointType.HandRight].Position);
                Point lefthand = ScalePosition(skeleton.Joints[JointType.HandLeft].Position);

                Joint righthandpt = (skeleton.Joints[JointType.HandRight]);
                Joint lefthandpt = (skeleton.Joints[JointType.HandLeft]);


                if (thing.Hit(lefthand))
                {
                    if (LeftSideAngle < 85)
                    {
                        this.thing.XVelocity = -1.0;

                    }
                    if (LeftSideAngle > 85)
                    {
                        this.thing.XVelocity = 1.0;
                    }
                    // else { this.thing.XVelocity = 0.0; }



                    this.thing.YVelocity = -1.0 * this.thing.YVelocity;
                    this.thing.Yvelocity_stat = true;
                    count++;
                    textBox.Text = ("Current hit count : " + count.ToString());

                }
                if (thing.Hit(handPt))
                {
                    if (RightSideAngle > 85)
                    {
                        this.thing.XVelocity = -1.0;

                    }
                    if (RightSideAngle < 85)
                    {
                        this.thing.XVelocity = +1.0;

                    }
                    //else { this.thing.XVelocity = 0.0; }



                    this.thing.YVelocity = -1.0 * this.thing.YVelocity;
                    this.thing.Yvelocity_stat = true;
                    count++;
                    textBox.Text = ("Current hit count : " + count.ToString());
                }

            }
        }
        public double Angle(Vector3D vectorA, Vector3D vectorB)
        {
            double dotProduct = 0.0;
            vectorA.Normalize();
            vectorB.Normalize();
            dotProduct = Vector3D.DotProduct(vectorA, vectorB);
            return 180 - (double)Math.Acos(dotProduct) / Math.PI * 180;
        }
        private void DrawSkeleton(Skeleton skeleton)
        {
            drawBone(skeleton.Joints[JointType.Head], skeleton.Joints[JointType.ShoulderCenter]);
            drawBone(skeleton.Joints[JointType.ShoulderCenter], skeleton.Joints[JointType.Spine]);

            drawBone(skeleton.Joints[JointType.ShoulderCenter], skeleton.Joints[JointType.ShoulderLeft]);
            drawBone(skeleton.Joints[JointType.ShoulderLeft], skeleton.Joints[JointType.ElbowLeft]);
            drawBone(skeleton.Joints[JointType.ElbowLeft], skeleton.Joints[JointType.WristLeft]);
            drawBone(skeleton.Joints[JointType.WristLeft], skeleton.Joints[JointType.HandLeft]);

            drawBone(skeleton.Joints[JointType.ShoulderCenter], skeleton.Joints[JointType.ShoulderRight]);
            drawBone(skeleton.Joints[JointType.ShoulderRight], skeleton.Joints[JointType.ElbowRight]);
            drawBone(skeleton.Joints[JointType.ElbowRight], skeleton.Joints[JointType.WristRight]);
            drawBone(skeleton.Joints[JointType.WristRight], skeleton.Joints[JointType.HandRight]);
            drawBone(skeleton.Joints[JointType.Spine], skeleton.Joints[JointType.HipCenter]);
            drawBone(skeleton.Joints[JointType.HipCenter], skeleton.Joints[JointType.HipLeft]);
            drawBone(skeleton.Joints[JointType.HipLeft], skeleton.Joints[JointType.KneeLeft]);
            drawBone(skeleton.Joints[JointType.KneeLeft], skeleton.Joints[JointType.AnkleLeft]);
            drawBone(skeleton.Joints[JointType.AnkleLeft], skeleton.Joints[JointType.FootLeft]);

            drawBone(skeleton.Joints[JointType.HipCenter], skeleton.Joints[JointType.HipRight]);
            drawBone(skeleton.Joints[JointType.HipRight], skeleton.Joints[JointType.KneeRight]);
            drawBone(skeleton.Joints[JointType.KneeRight], skeleton.Joints[JointType.AnkleRight]);
            drawBone(skeleton.Joints[JointType.AnkleRight], skeleton.Joints[JointType.FootRight]);
        }
        void drawBone(Joint trackedJoint1, Joint trackedJoint2)
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

            canvas1.Children.Add(bone);
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
                    new Int32Rect(0, 0, this.colorBitmap.PixelWidth, this.colorBitmap.PixelHeight),
                        this.colorPixels, stride, 0);
            }
        }
        void advanceThingPosition()
        {
            thing.Center.Offset(thing.XVelocity, thing.YVelocity);
            thing.YVelocity += this.gravity;
            thing.Shape.SetValue(Canvas.LeftProperty, thing.Center.X - thing.Shape.Width);
            thing.Shape.SetValue(Canvas.TopProperty, thing.Center.Y - thing.Shape.Width);

            // if goes out of bound, reset position, as well as velocity
            if (thing.Center.Y >= canvas1.Height)
            {
                thing.Center.Y = 0;
                thing.XVelocity = 0;
                thing.YVelocity = 0;
                count = 0;
                thing.Center.X = 320;
                textBox.Text = ("Current hit count : " + count.ToString());
            }
            if (thing.Center.Y <= 20)
            {
                thing.Yvelocity_stat = false;

            }


            if (thing.Center.X < 5 && thing.Center.Y < 480)
            {
                textBox2.Text. = ("Your Game Ball is about to fall");

            }
            if(thing.Center.X > 630 && thing.Center.Y < 480)
            {
                textBox2.Text = ("Your Game Ball is about to fall");
            }
            if(thing.Center.X < 0 || thing.Center.X > 640)
            {
                
                textBox2.Clear();
             
            }
        }

            }
}

