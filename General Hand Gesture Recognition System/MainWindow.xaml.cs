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
using System.Diagnostics;
using System.IO;


namespace General_Hand_Gesture_Recognition_System
{

    public partial class MainWindow : Window
    {

        #region Member Variables
        private KinectSensor _Kinect;
        private TCPconnnctionClient connection = new TCPconnnctionClient();

        private readonly Brush[] _SkeletonBrushes;
        private Skeleton[] _FrameSkeletons;

        // color stream         
        private byte[] colorPixelData;
        private WriteableBitmap outputImage;

        // depth stream
        private short[] depthPixelData;
        private byte[] depthFrame32;
        private WriteableBitmap outputBitmap;
        #endregion Member Variables

        #region Constructor
        public MainWindow()
        {

            InitializeComponent();

            //check if kinect mode ( connected , power , issues ) 
            this.Loaded += (s, e) => { DiscoverKinectSensor(); };
            this.Unloaded += (s, e) => { this.Kinect = null; };

            // Colors used for skeleton draw
            this._SkeletonBrushes = new[] { Brushes.Black, Brushes.Crimson, Brushes.Indigo,
                                            Brushes.DodgerBlue, Brushes.Purple, Brushes.Pink };
            // create connection as client side ( connect to server ) 
            connection.createConnection();

        }
        #endregion Constructor

        #region FrameReady
        void KinectDevice_DepthFrameReady(object sender, DepthImageFrameReadyEventArgs e)
        {

            using (DepthImageFrame depthFrame = e.OpenDepthImageFrame())
            {
                if (depthFrame != null)
                {
                    //Using standard SDK 
                    this.depthPixelData = new short[depthFrame.PixelDataLength];
                    this.depthFrame32 = new byte[depthFrame.Width * depthFrame.Height * 4];
                    depthFrame.CopyPixelDataTo(this.depthPixelData);
                    byte[] convertedDepthBits = this.ConvertDepthFrame(this.depthPixelData, ((KinectSensor)sender).DepthStream);

                    this.outputBitmap = new WriteableBitmap(depthFrame.Width, depthFrame.Height, 96, 96, PixelFormats.Bgr32, null);

                    this.outputBitmap.WritePixels(
                        new Int32Rect(0, 0, depthFrame.Width, depthFrame.Height),
                        convertedDepthBits,
                        depthFrame.Width * 4,
                        0);

                    this.kinectDepthImage.Source = this.outputBitmap;


                }

            }
        }

        void KinectDevice_ColorFrameReady(object sender, ColorImageFrameReadyEventArgs e)
        {
            using (ColorImageFrame colorFrame = e.OpenColorImageFrame())
            {
                if (colorFrame != null)
                {
                    //Using standard SDK
                    this.colorPixelData = new byte[colorFrame.PixelDataLength];

                    colorFrame.CopyPixelDataTo(this.colorPixelData);

                    this.outputImage = new WriteableBitmap(
                    colorFrame.Width,
                    colorFrame.Height,
                    96,  // DpiX
                    96,  // DpiY
                    PixelFormats.Bgr32,
                    null);

                    this.outputImage.WritePixels(
                    new Int32Rect(0, 0, colorFrame.Width, colorFrame.Height),
                    this.colorPixelData,
                    colorFrame.Width * 4,
                    0);
                    this.kinectColorImage.Source = this.outputImage;

                    //Using Coding4Fun Kinect Toolkit
                    //kinectColorImage.Source = imageFrame.ToBitmapSource();

                }
            }
        }

        private void KinectDevice_SkeletonFrameReady(object sender, SkeletonFrameReadyEventArgs e)
        {
            using (SkeletonFrame frame = e.OpenSkeletonFrame())
            {

                if (frame != null)
                {
                    // create skeleton frame
                    frame.CopySkeletonDataTo(this._FrameSkeletons);

                    //send data to client
                    //connection.sendDataByte(Race());
                    connection.sendData(RaceString());

                    //show skeleton on WPF
                    drawSkeleton();

                }
            }
        }
        #endregion FrameReady

        #region Initialization
        private void InitializeKinectSensor(KinectSensor sensor)
        {

            if (sensor != null)
            {
                // skeleteon stm
                initializeSkeletonSensor(sensor);

                // color stm
                initializeColorSensor(sensor);

                // Depth stm
                initializeDepthSensor(sensor);

                //start sensor
                sensor.Start();
            }
        }



        private void initializeDepthSensor(KinectSensor sensor)
        {
            sensor.DepthStream.Enable(DepthImageFormat.Resolution640x480Fps30);
            sensor.DepthFrameReady += new EventHandler<DepthImageFrameReadyEventArgs>(KinectDevice_DepthFrameReady);
        }

        private void initializeColorSensor(KinectSensor sensor)
        {
            sensor.ColorStream.Enable(ColorImageFormat.RgbResolution640x480Fps30);
            sensor.ColorFrameReady += new EventHandler<ColorImageFrameReadyEventArgs>(KinectDevice_ColorFrameReady);
        }

        private void initializeSkeletonSensor(KinectSensor sensor)
        {
            sensor.SkeletonStream.Enable();     // when this enables it generate the skeleton frame object p.91
            this._FrameSkeletons = new Skeleton[sensor.SkeletonStream.FrameSkeletonArrayLength];       // constant array            
            sensor.SkeletonFrameReady += KinectDevice_SkeletonFrameReady;
        }



        private void UninitializeKinectSensor(KinectSensor sensor)
        {
            if (sensor != null)
            {
                //colse TCP connetion
                connection.TCPConnectionClose();

                sensor.ColorFrameReady -= KinectDevice_ColorFrameReady;
                sensor.DepthFrameReady -= KinectDevice_DepthFrameReady;
                sensor.SkeletonFrameReady -= KinectDevice_SkeletonFrameReady;

                sensor.ColorStream.Disable();
                sensor.DepthStream.Disable();
                sensor.SkeletonStream.Disable();

                _FrameSkeletons = null;

                sensor.Stop();

            }
        }
        #endregion Initialization

        #region Methods

        #region Helper Method
        // Converts a 16-bit grayscale depth frame which includes player indexes into a 32-bit frame 
        // that displays different players in different colors 
        // color divisors for tinting depth pixels 
        //private static readonly int[] IntensityShiftByPlayerR = { 1, 2, 0, 2, 0, 0, 2, 0 };
        private static readonly int[] IntensityShiftByPlayerG = { 1, 2, 2, 0, 2, 0, 0, 1 };
        private static readonly int[] IntensityShiftByPlayerB = { 1, 0, 2, 2, 0, 2, 0, 2 };

        private const int RedIndex = 2;
        private const int GreenIndex = 1;
        private const int BlueIndex = 0;

        private byte[] ConvertDepthFrame(short[] depthFrame, DepthImageStream depthStream)
        {
            int tooNearDepth = depthStream.TooNearDepth;
            int tooFarDepth = depthStream.TooFarDepth;
            int unknownDepth = depthStream.UnknownDepth;

            for (int i16 = 0, i32 = 0; i16 < depthFrame.Length && i32 < this.depthFrame32.Length; i16++, i32 += 4)
            {
                int player = depthFrame[i16] & DepthImageFrame.PlayerIndexBitmask;
                int realDepth = depthFrame[i16] >> DepthImageFrame.PlayerIndexBitmaskWidth;

                // transform 13-bit depth information into an 8-bit intensity appropriate 
                // for display (we disregard information in most significant bit)
                byte intensity = (byte)(~(realDepth >> 4));

                if (player == 0 && realDepth == 0)
                {
                    // white this.depthFrame32[i32 + RedIndex] = 255;
                    this.depthFrame32[i32 + GreenIndex] = 255;
                    this.depthFrame32[i32 + BlueIndex] = 255;
                }
                else if (player == 0 && realDepth == tooFarDepth)
                {
                    // dark purple this.depthFrame32[i32 + RedIndex] = 66;
                    this.depthFrame32[i32 + GreenIndex] = 0;
                    this.depthFrame32[i32 + BlueIndex] = 66;
                }
                else if (player == 0 && realDepth == unknownDepth)
                {
                    // dark brown this.depthFrame32[i32 + RedIndex] = 66;
                    this.depthFrame32[i32 + GreenIndex] = 66;
                    this.depthFrame32[i32 + BlueIndex] = 33;
                }
                else
                {
                    // tint the intensity by dividing by per-player values 
                    //this.depthFrame32[i32 + RedIndex] = (byte)(intensity >> IntensityShiftByPlayerR[player]);
                    this.depthFrame32[i32 + GreenIndex] = (byte)(intensity >> IntensityShiftByPlayerG[player]);
                    this.depthFrame32[i32 + BlueIndex] = (byte)(intensity >> IntensityShiftByPlayerB[player]);
                }
            }

            return this.depthFrame32;
        }
        #endregion Helper Method

        #region Kinect Controller


        public KinectSensor Kinect
        {
            get { return this._Kinect; }
            set
            {
                if (this._Kinect != value)
                {
                    if (this._Kinect != null)
                    {
                        UninitializeKinectSensor(this._Kinect);
                        this._Kinect = null;
                    }
                    if (value != null && value.Status == KinectStatus.Connected)
                    {
                        this._Kinect = value;
                        Trace.WriteLine("\n\n\nWelcome----------------------------------------------------------");

                        InitializeKinectSensor(this._Kinect);

                    }
                }
            }


        }
        private void DiscoverKinectSensor()
        {
            // IMAGE sESNSOR
            KinectSensor.KinectSensors.StatusChanged += KinectSensors_StatusChanged;
            //COMMON
            this.Kinect = KinectSensor.KinectSensors.FirstOrDefault(x => x.Status == KinectStatus.Connected);

            if (_Kinect == null)   // CHECK THE kINECT OJECT STATUS  
                System.Windows.MessageBox.Show("Kinect Not Connected");
        }

        private void KinectSensors_StatusChanged(object sender, StatusChangedEventArgs e)
        {
            switch (e.Status)
            {
                case KinectStatus.Connected:
                    if (this.Kinect == null)
                    {
                        this.Kinect = e.Sensor;
                    }
                    break;
                case KinectStatus.NotPowered:
                    System.Windows.MessageBox.Show("Kinect Not Powered");
                    break;
                    
                case KinectStatus.Disconnected:
                    if (this.Kinect == e.Sensor)
                    {
                        this.Kinect = null;
                        this.Kinect = KinectSensor.KinectSensors
                        .FirstOrDefault(x => x.Status == KinectStatus.Connected);
                        if (this.Kinect == null)
                        {
                            System.Windows.MessageBox.Show("sensor is disconnected");
                        }
                    }
                    break;
            }
        }
        #endregion Kinect Controller        
                
        #region Algorithm 
        public byte[] Race()
        {
            // no action = 0   , right = 4
            // low speed = 1   , center = 5  
            // mid speed = 2   , left = 6
            // high = 3

            byte[] actions = new Byte[2];
            Skeleton skeleton;
            Joint shoulderCenter, shoulderRight, shoulderLeft, hipsCenter, handRight, handLeft;

            for (int i = 0; i < this._FrameSkeletons.Length; i++)     // his loop depend on each new user Skeleton apear
            {
                skeleton = this._FrameSkeletons[i];
                if (skeleton.TrackingState == SkeletonTrackingState.Tracked)
                {
                    if (skeleton.TrackingState == SkeletonTrackingState.Tracked)
                    {
                        shoulderCenter = skeleton.Joints[JointType.ShoulderCenter];
                        shoulderRight = skeleton.Joints[JointType.ShoulderRight];
                        shoulderLeft = skeleton.Joints[JointType.ShoulderLeft];
                        hipsCenter = skeleton.Joints[JointType.HipCenter];
                        handRight = skeleton.Joints[JointType.HandRight];
                        handLeft = skeleton.Joints[JointType.HandLeft];

                        if (handRight.Position.Y > hipsCenter.Position.Y)
                        {

                            if (handLeft.Position.Y < hipsCenter.Position.Y)
                            {
                                actions[0] = 1;
                            }
                            else if (handLeft.Position.Y > shoulderCenter.Position.Y)
                            {
                                actions[0] = 3;
                            }
                            else
                            {
                                actions[0] = 2;
                            }
                            if (handRight.Position.X > shoulderRight.Position.X && handRight.Position.X > shoulderLeft.Position.X)
                            {
                                actions[1] = 4;
                            }
                            else if (handRight.Position.X < shoulderRight.Position.X && handRight.Position.X < shoulderLeft.Position.X)
                            {
                                actions[1] = 5;
                            }
                            else
                            {
                                actions[1] = 6;
                            }

                        }
                        else
                        {
                            actions[0] = 0;
                            actions[0] = 0;
                        }

                        //connection.sendData(Data);
                    }
                }
            }
            return actions;
        }

        public String RaceString()
        {

            Joint shoulderCenter, shoulderRight, shoulderLeft, hipsCenter, handRight, handLeft;
            String Data = "";
            Skeleton skeleton;

            for (int i = 0; i < this._FrameSkeletons.Length; i++)     // his loop depend on each new user Skeleton apear
            {
                skeleton = this._FrameSkeletons[i];
                if (skeleton.TrackingState == SkeletonTrackingState.Tracked)
                {
                    if (skeleton.TrackingState == SkeletonTrackingState.Tracked)
                    {
                        shoulderCenter = skeleton.Joints[JointType.ShoulderCenter];
                        shoulderRight = skeleton.Joints[JointType.ShoulderRight];
                        shoulderLeft = skeleton.Joints[JointType.ShoulderLeft];
                        hipsCenter = skeleton.Joints[JointType.HipCenter];
                        handRight = skeleton.Joints[JointType.HandRight];
                        handLeft = skeleton.Joints[JointType.HandLeft];

                        if (handRight.Position.Y > hipsCenter.Position.Y)
                        {

                            if (handLeft.Position.Y < hipsCenter.Position.Y)
                            {
                                Trace.WriteLine("Low speed");
                                //Data = "Low Speed ";
                                Data = "1";
                            }
                            else if (handLeft.Position.Y > shoulderCenter.Position.Y)
                            {
                                Trace.WriteLine("High Speed");
                                //Data = "High Speed";
                                Data = "3";
                            }
                            else
                            {
                                Trace.WriteLine("Mid speed");
                                //Data= " Mid Speed";
                                Data = "2";
                            }
                            if (handRight.Position.X > shoulderRight.Position.X && handRight.Position.X > shoulderLeft.Position.X)
                            {
                                Trace.WriteLine("Turn Right");
                                //Data += "- Turn Right";
                                Data += "4";
                            }
                            else if (handRight.Position.X < shoulderRight.Position.X && handRight.Position.X < shoulderLeft.Position.X)
                            {
                                Trace.WriteLine("Turn Left");
                                //Data += "- Turn Left";
                                Data += "6";
                            }
                            else
                            {
                                Trace.WriteLine("Center");
                                //Data += "- Center";
                                Data += "5";
                            }

                        }
                        else
                        {
                            Trace.WriteLine("No action");
                            //Data = "No action";
                            Data = "0";
                        }


                    }
                }
            }
            return Data;
        }
        #endregion Algorithm

        #region Drawwing
        public void drawSkeleton()
        {
            Brush userBrush;
            Skeleton skeleton;

            LayoutRoot.Children.Clear();
            JointType[] joints;

            // coppy data from skeltonFrame object to skeleton object
            for (int i = 0; i < this._FrameSkeletons.Length; i++)     // his loop depend on each new user Skeleton apear
            {
                skeleton = this._FrameSkeletons[i];
                if (skeleton.TrackingState == SkeletonTrackingState.Tracked)
                {

                    userBrush = this._SkeletonBrushes[i % this._SkeletonBrushes.Length];   // depend on user ID to choose color
                    //Draws the skeleton’s head and torso
                    joints = new[] { JointType.Head, JointType.ShoulderCenter,
                            JointType.ShoulderLeft, JointType.Spine,
                            JointType.ShoulderRight, JointType.ShoulderCenter,
                            JointType.HipCenter, JointType.HipLeft,
                            JointType.Spine, JointType.HipRight,
                            JointType.HipCenter };

                    LayoutRoot.Children.Add(CreateFigure(skeleton, userBrush, joints));
                    //Draws the skeleton’s left leg
                    joints = new[] { JointType.HipLeft, JointType.KneeLeft,
                            JointType.AnkleLeft, JointType.FootLeft };
                    LayoutRoot.Children.Add(CreateFigure(skeleton, userBrush, joints));
                    //Draws the skeleton’s right leg
                    joints = new[] { JointType.HipRight, JointType.KneeRight,
                            JointType.AnkleRight, JointType.FootRight };
                    LayoutRoot.Children.Add(CreateFigure(skeleton, userBrush, joints));
                    //Draws the skeleton’s left arm
                    joints = new[] { JointType.ShoulderLeft, JointType.ElbowLeft,
                            JointType.WristLeft, JointType.HandLeft };
                    LayoutRoot.Children.Add(CreateFigure(skeleton, userBrush, joints));
                    //Draws the skeleton’s right arm
                    joints = new[] { JointType.ShoulderRight, JointType.ElbowRight,
                            JointType.WristRight, JointType.HandRight };
                    LayoutRoot.Children.Add(CreateFigure(skeleton, userBrush, joints));

                }
            }
        }

        private Polyline CreateFigure(Skeleton skeleton, Brush brush, JointType[] joints)
        {
            Polyline figure = new Polyline();
            figure.StrokeThickness = 8;
            figure.Stroke = brush;
            for (int i = 0; i < joints.Length; i++)
            {
                figure.Points.Add(GetJointPoint(skeleton.Joints[joints[i]]));
            }
            return figure;
        }

        private Point GetJointPoint(Joint joint)
        {
            // joint .position get the position of each joint accordng to the 3 axis
            DepthImagePoint point = this._Kinect.CoordinateMapper.MapSkeletonPointToDepthPoint(joint.Position,
            this._Kinect.DepthStream.Format);
            point.X *= (int)this.LayoutRoot.ActualWidth / this._Kinect.DepthStream.FrameWidth;
            point.Y *= (int)this.LayoutRoot.ActualHeight / this._Kinect.DepthStream.FrameHeight;

            return new Point(point.X, point.Y);
        }

        private static Skeleton GetPrimarySkeleton(Skeleton[] skeletons)
        {
            Skeleton skeleton = null;
            if (skeletons != null)
            {
                //Find the closest skeleton 
                for (int i = 0; i < skeletons.Length; i++)
                {
                    if (skeletons[i].TrackingState == SkeletonTrackingState.Tracked)
                    {
                        if (skeleton == null)
                        {
                            skeleton = skeletons[i];
                        }
                        else
                        {
                            if (skeleton.Position.Z > skeletons[i].Position.Z)
                            {
                                skeleton = skeletons[i];
                            }
                        }
                    }
                }
            }
            return skeleton;
        }

        private static Joint GetPrimaryHand(Skeleton skeleton)
        {
            Joint primaryHand = new Joint();
            if (skeleton != null)
            {
                primaryHand = skeleton.Joints[JointType.HandLeft];
                Joint righHand = skeleton.Joints[JointType.HandRight];
                // to determine which hand is closer to be as primary hand
                if (righHand.TrackingState != JointTrackingState.NotTracked)
                {
                    if (primaryHand.TrackingState == JointTrackingState.NotTracked)
                    {
                        primaryHand = righHand;
                    }
                    else
                    {
                        if (primaryHand.Position.Z > righHand.Position.Z)
                        {
                            primaryHand = righHand;
                        }

                    }
                }
            }
            return primaryHand;
        }

        private void TrackHand(Joint hand)
        {
            if (hand.TrackingState == JointTrackingState.NotTracked)
            {
                Trace.WriteLine("no primary hand");
                HandCursorElement.Visibility = System.Windows.Visibility.Collapsed;
            }
            else
            {

                if (hand.JointType == JointType.HandRight)
                {
                    //    Trace.WriteLine("Right");
                    HandCursorScale.ScaleX = 1;
                }
                else if (hand.JointType == JointType.HandLeft)
                {
                    //  Trace.WriteLine("Left");
                    HandCursorScale.ScaleX = -1;
                }
                else
                {
                    Trace.WriteLine("msh 3arf");

                }
            }
        }
    }
    #endregion Drawwing

        #endregion Methods

}