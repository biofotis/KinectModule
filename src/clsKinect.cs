using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using Microsoft.Kinect;
using Microsoft.Kinect.Face;
using System.Windows.Media.Media3D;
using System.Threading;
using System.Media;
using System.Runtime.InteropServices;
using Emgu.CV;
using Emgu.CV.Structure;


namespace KinectModule
{
    public class clsKinect
    {
        
        // variables for audio tracking
        private const int SamplesPerMillisecond = 16;
        private const int BytesPerSample = sizeof(float);
        private const int MinEnergy = -90;
        private byte[] audioBuffer = null;
        private const int EnergyBitmapWidth = 780;
        private readonly float[] energy = new float[(uint)(EnergyBitmapWidth * 1.25)];
        private readonly object energyLock = new object();
        private AudioBeamFrameReader audioReader = null;
        private float beamAngle = 0;
        private float beamAngleConfidence = 0;
        private float accumulatedSquareSum;
        private int accumulatedSampleCount;
        private int energyIndex;
        private int newEnergyAvailable;
        private DateTime? lastEnergyRefreshTime;
        private int energyRefreshIndex;
        private float energyError;
        private const int SamplesPerColumn = 40;

        private KinectModule forminstance;
        public ATimer videoTimer;
        private SoundPlayer simpleSound;
        public bool chkVideo = false;
        public bool chkVideolog = false;
        private bool activevideolog = false;
        private bool activelog = false;

        public ATimer saver;
        private double logtime = 0;
        private const Int16 hertz = 4;
        TimeSpan timetmp;
        private clsLogSave filesave;
        private int currentID = 0;
        private int bodycount = 0;
        public string kinectcache = "";
        

        //image definitions
        public Image<Bgra, byte> imageBuffer;
        public Image<Bgr, byte> imageCurrent;
        public Image<Bgr, byte> imageOld;
        
        private VideoWriter videoOut = null;

        //variables for kinect
        public class KinectData
        {
            public AU mAU;
            public ExtraData mExtraData;
            public HeadDir mHeadDir;
            public HeadLoc mHeadLoc;
            public Pointing mPointing;
            public KinectData()
            {
                mAU = new AU();
                mExtraData = new ExtraData();
                mHeadDir = new HeadDir();
                mHeadLoc = new HeadLoc();
                mPointing = new Pointing();
            }
        }
        public class AU
        {
            public double JawOpen = 0;
            public double JawSlideRight = 0;
            public double LeftcheekPuff = 0;
            public double LefteyebrowLowerer = 0;
            public double LefteyeClosed = 0;
            public double LipCornerDepressorLeft = 0;
            public double LipCornerDepressorRight = 0;
            public double LipCornerPullerLeft = 0;
            public double LipCornerPullerRight = 0;
            public double LipPucker = 0;
            public double LipStretcherLeft = 0;
            public double LipStretcherRight = 0;
            public double LowerlipDepressorLeft = 0;
            public double LowerlipDepressorRight = 0;
            public double RightcheekPuff = 0;
            public double RighteyebrowLowerer = 0;
            public double RighteyeClosed = 0;
        }
        public class HeadDir
        {
            public double headYaw = 0;
            public double headRoll = 0;
            public double headPitch = 0;
        }
        public class HeadLoc
        {
            public double headX = 0;
            public double headY = 0;
            public double headZ = 0;
        }
        public class ExtraData
        {
            public double AudioAngle = 0;
            public double AudioLevel = 0;
            public double leanX = 0;
            public double leanY = 0;
            public string Happiness = "Unknown";
        }
        public class Pointing
        {
            public double lefthandX = 0;
            public double lefthandY = 0;
            public double lefthandZ = 0;
            public double righthandX = 0;
            public double righthandY = 0;
            public double righthandZ = 0;
        }
        
        public KinectData mKinectData;
        public Boolean DetectedPerson;

        private Joint headPosition;
        private KinectSensor sensor = null;
        private BodyFrameSource bodySource = null;
        private HighDefinitionFaceFrameSource highDefinitionFaceFrameSource = null;
        private HighDefinitionFaceFrameReader highDefinitionFaceFrameReader = null;
        private byte[] colorImageBuffer;
        private FaceModel faceModel;


        private byte[] pixels = null;
        private FaceAlignment currentFaceAlignment = null;
        private FaceFrameReader faceReader = null;
        private FaceFrameSource faceSource = null;


        private ulong? currentTrackingId = 0;
        private MultiSourceFrameReader reader;
        private Body[] bodies;
        private string currentBuilderStatus = string.Empty;
        private const FaceFrameFeatures DefaultFaceFrameFeatures = FaceFrameFeatures.PointsInColorSpace
                                | FaceFrameFeatures.Happy
                                | FaceFrameFeatures.FaceEngagement
                                | FaceFrameFeatures.Glasses
                                | FaceFrameFeatures.LeftEyeClosed
                                | FaceFrameFeatures.RightEyeClosed
                                | FaceFrameFeatures.MouthOpen
                                | FaceFrameFeatures.MouthMoved
                                | FaceFrameFeatures.LookingAway
                                | FaceFrameFeatures.RotationOrientation;

        private ulong? CurrentTrackingId
        {
            get
            { return this.currentTrackingId; }
            set
            { this.currentTrackingId = value; }
        }
        public void InitializeKinect(KinectModule frminst)
        {
            this.sensor = KinectSensor.GetDefault();
            this.bodySource = this.sensor.BodyFrameSource;
            FrameDescription colorFrameDescription = this.sensor.ColorFrameSource.CreateFrameDescription(ColorImageFormat.Bgra);

            this.highDefinitionFaceFrameSource = new HighDefinitionFaceFrameSource(sensor);
            this.highDefinitionFaceFrameSource.TrackingIdLost += this.HdFaceSource_TrackingIdLost;

            this.highDefinitionFaceFrameReader = this.highDefinitionFaceFrameSource.OpenReader();
            this.highDefinitionFaceFrameReader.FrameArrived += this.HdFaceReader_FrameArrived; //event gor high def face

            this.reader = sensor.OpenMultiSourceFrameReader(FrameSourceTypes.Body | FrameSourceTypes.Color);

            this.reader.MultiSourceFrameArrived += OnMultiSourceFrameArrived; //event for multiple source (Position)
            this.currentFaceAlignment = new FaceAlignment();

            faceSource = new FaceFrameSource(sensor, 0, DefaultFaceFrameFeatures);
            faceReader = faceSource.OpenReader();
            faceReader.FrameArrived += OnFaceFrameArrived; //event for face data

            faceSource.TrackingIdLost += OnTrackingIdLost;

            AudioSource audioSource = this.sensor.AudioSource;
            this.audioBuffer = new byte[audioSource.SubFrameLengthInBytes];
            this.audioReader = audioSource.OpenReader();

            if (this.audioReader != null)
            {
                // Subscribe to new audio frame arrived events
                this.audioReader.FrameArrived += this.audioReader_FrameArrived;
            }
            this.pixels = new byte[colorFrameDescription.Width * colorFrameDescription.Height * colorFrameDescription.BytesPerPixel];
            this.sensor.IsAvailableChanged += this.Sensor_IsAvailableChanged;
            this.sensor.Open();
            this.colorImageBuffer = new byte[4 * sensor.ColorFrameSource.FrameDescription.LengthInPixels];

            videoTimer = new ATimer(3,40, saveframe); //25fps
            videoTimer.Stop();
            
            
            mKinectData = new KinectData(); //create the generic variable for kinect data
            forminstance = frminst; //use ssame instance

            saver = new ATimer(3, System.Convert.ToInt16(((double)1 / (double)hertz) * 1000), OnsaveEvent);
            saver.Stop();
            filesave = new clsLogSave();
        }

        void OnsaveEvent() //logger timer
        { //pull data from kinect variables and store them on string buffer
            logtime = logtime + (double)1/(double)hertz;
            timetmp = TimeSpan.FromSeconds(logtime);
            kinectcache += timetmp.ToString(@"hh\:mm\:ss\:fff") + "," + DetectedPerson.ToString() + "," + bodycount.ToString() + "," + mKinectData.mHeadDir.headPitch.ToString() + "," + mKinectData.mHeadDir.headRoll.ToString() + 
            ","  + mKinectData.mHeadDir.headYaw.ToString() + "," + mKinectData.mHeadLoc.headX.ToString() + "," + mKinectData.mHeadLoc.headY.ToString() + "," + mKinectData.mHeadLoc.headZ.ToString() 
            + "," + mKinectData.mPointing.righthandX.ToString() + "," + mKinectData.mPointing.righthandY.ToString() + "," + mKinectData.mPointing.righthandZ.ToString() +
            "," + mKinectData.mPointing.lefthandX.ToString() + "," + mKinectData.mPointing.lefthandX.ToString() + "," + mKinectData.mPointing.lefthandX.ToString() +
            "," + mKinectData.mExtraData.AudioLevel.ToString() + "," + mKinectData.mExtraData.AudioAngle.ToString() + "," +
            mKinectData.mExtraData.leanX.ToString() + "," + mKinectData.mExtraData.leanY.ToString() + "," + mKinectData.mExtraData.Happiness.ToString() + "," +
            mKinectData.mAU.JawOpen.ToString() + "," + mKinectData.mAU.JawSlideRight.ToString() + "," + mKinectData.mAU.LeftcheekPuff.ToString() + "," +
            mKinectData.mAU.LefteyebrowLowerer.ToString() + "," + mKinectData.mAU.LefteyeClosed.ToString() + "," + mKinectData.mAU.LipCornerDepressorLeft.ToString() + "," +
            mKinectData.mAU.LipCornerDepressorRight.ToString() + "," + mKinectData.mAU.LipCornerPullerLeft.ToString() + "," + mKinectData.mAU.LipCornerPullerRight.ToString() + "," +
            mKinectData.mAU.LipPucker.ToString() + "," + mKinectData.mAU.LipStretcherLeft.ToString() + "," + mKinectData.mAU.LipStretcherRight.ToString() + "," +
            mKinectData.mAU.LipStretcherRight.ToString() + "," + mKinectData.mAU.LowerlipDepressorLeft.ToString() + "," + mKinectData.mAU.LowerlipDepressorRight.ToString() + "," +
            mKinectData.mAU.RightcheekPuff.ToString() + "," + mKinectData.mAU.RighteyebrowLowerer.ToString() + "," + mKinectData.mAU.RighteyeClosed.ToString() + "," +  
            System.Environment.NewLine;

        }
        public void startcapture(string participantID)
        {
            
                if (chkVideolog == true)
                {
                
                    //video capture
                    imageCurrent = new Image<Bgr, byte>(1920, 1080);
                    imageOld = new Image<Bgr, byte>(1920, 1080);
                    videoOut = new VideoWriter("video_" + participantID + ".avi", CvInvoke.CV_FOURCC('D', 'I', 'V', 'X'), 25, 1920, 1080, true);
                    videoTimer.Start();
                    activevideolog = true;
                }
            if (activelog == false)
            {
                activelog = true;
                currentID = System.Convert.ToInt16(participantID); //store participant's ID
                System.Threading.Thread.Sleep(500); //wait for 0.5 second to create the file
                kinectcache = "Log file for participant ID:" + participantID + " Started on:" + DateTime.Now.TimeOfDay + "," +
                    "DetectedPerson,BodyCount,HeadPitch,HeadRoll,HeadYaw,HeadX,HeadY,HeadZ,RightHandX,RightHandY,RightHandZ,LeftHandX,LeftHandY" +
                    ",LeftHandZ,AudioLevel,AudioAngle,BodyLeanX,BodyLeanY,Happiness,JawOpen,JawSlideRight,LeftcheekPuff," +
                    "LefteyebrowLowerer,LefteyeClosed,LipCornerDepressorLeft,LipCornerDepressorRight,LipCornerPullerLeft," +
                    "LipCornerPullerRight,LipPucker,LipStretcherLeft,LipStretcherRight,,LowerlipDepressorLeft,LowerlipDepressorRight" +
                    ",RightcheekPuff,RighteyebrowLowerer,RighteyeClosed" + System.Environment.NewLine;


                saver.Start(); //start logging timer

                simpleSound = new SoundPlayer(@"countA.wav");
                simpleSound.Play();
                //audio capture here
            }
        }
        public void stopcapture()
        {
            
           if (chkVideolog == true)
                {
               
                    videoTimer.Stop();
                    Thread.Sleep(1000);
                    videoOut.Dispose();
                    activevideolog = false;
                }
            if (activelog == true)
            {

                activelog = false;
                saver.Stop(); //stop logging timer
                kinectcache += "Log file END. Finished on:" + DateTime.Now.TimeOfDay;
                //save the log file using a filename that doesnt exist
                filesave.SaveToFile(kinectcache, currentID);
                var simpleSound = new SoundPlayer(@"countA.wav");
                simpleSound.Play();
            }
        }

        public void saveframe()
        {
            if(videoOut != null)
            if (videoOut.Ptr != IntPtr.Zero)
            {
                try
                {
                    if (imageCurrent == null)
                        videoOut.WriteFrame(imageOld); //write old frame in case img3 is not pulled yet
                    else
                    {
                        videoOut.WriteFrame(imageCurrent);
                            imageOld = imageCurrent;
                            imageCurrent = null;
                    }


                }
                catch { }
            } //save frames every tick
        }

        private void Sensor_IsAvailableChanged(object sender, IsAvailableChangedEventArgs e)
        {
            if ((sensor.IsAvailable == true) && (sensor.IsOpen == false)) InitializeKinect(forminstance);
            forminstance.kinectstatus(sensor.IsAvailable.ToString());
        }
        #region Receive and Process Audio

        private void audioReader_FrameArrived(object sender, AudioBeamFrameArrivedEventArgs e)
        {
            AudioBeamFrameReference frameReference = e.FrameReference;
            
            try
            {
                AudioBeamFrameList frameList = frameReference.AcquireBeamFrames();

                if (frameList != null)
                {
                    // AudioBeamFrameList is IDisposable
                    using (frameList)
                    {
                        // Only one audio beam is supported. Get the sub frame list for this beam
                        IReadOnlyList<AudioBeamSubFrame> subFrameList = frameList[0].SubFrames;

                        // Loop over all sub frames, extract audio buffer and beam information
                        foreach (AudioBeamSubFrame subFrame in subFrameList)
                        {
                            // Check if beam angle and/or confidence have changed
                            bool updateBeam = false;

                            if (subFrame.BeamAngle != this.beamAngle)
                            {
                                this.beamAngle = subFrame.BeamAngle;
                                updateBeam = true;
                            }

                            if (subFrame.BeamAngleConfidence != this.beamAngleConfidence)
                            {
                                this.beamAngleConfidence = subFrame.BeamAngleConfidence;
                                updateBeam = true;
                            }

                            if (updateBeam)
                            {
                                // Refresh display of audio beam
                                this.AudioBeamChanged();
                            }

                            // Process audio buffer
                            subFrame.CopyFrameDataToArray(this.audioBuffer);
                            
                            for (int i = 0; i < this.audioBuffer.Length; i += BytesPerSample)
                            {
                                // Extract the 32-bit IEEE float sample from the byte array
                                float audioSample = BitConverter.ToSingle(this.audioBuffer, i);

                                this.accumulatedSquareSum += audioSample * audioSample;
                                ++this.accumulatedSampleCount;

                                if (this.accumulatedSampleCount < SamplesPerColumn)
                                {
                                    continue;
                                }

                                float meanSquare = this.accumulatedSquareSum / SamplesPerColumn;

                                if (meanSquare > 1.0f)
                                {
                                    // A loud audio source right next to the sensor may result in mean square values
                                    // greater than 1.0. Cap it at 1.0f for display purposes.
                                    meanSquare = 1.0f;
                                }

                                // Calculate energy in dB, in the range [MinEnergy, 0], where MinEnergy < 0
                                float energy = MinEnergy;

                                if (meanSquare > 0)
                                {
                                    energy = (float)(10.0 * Math.Log10(meanSquare));
                                }

                                lock (this.energyLock)
                                {
                                    // Normalize values to the range [0, 1] for display
                                    this.energy[this.energyIndex] = (MinEnergy - energy) / MinEnergy;
                                    this.energyIndex = (this.energyIndex + 1) % this.energy.Length;
                                    ++this.newEnergyAvailable;

                                    if (this.energy[this.energyIndex] < 0) { this.energy[this.energyIndex] = 0; }
                                        mKinectData.mExtraData.AudioLevel = this.energy[this.energyIndex];
                                }

                                this.accumulatedSquareSum = 0;
                                this.accumulatedSampleCount = 0;
                            }
                        }
                    }
                }
            }
            catch (Exception)
            {
                // Ignore if the frame is no longer available
            }
        }

        /// <summary>
        /// Method called when audio beam angle and/or confidence have changed.
        /// </summary>
        private void AudioBeamChanged()
        {
            // Maximum possible confidence corresponds to this gradient width
            const float MinGradientWidth = 0.04f;

            // Set width of mark based on confidence.
            // A confidence of 0 would give us a gradient that fills whole area diffusely.
            // A confidence of 1 would give us the narrowest allowed gradient width.
            float halfWidth = Math.Max(1 - this.beamAngleConfidence, MinGradientWidth) / 2;

            // Convert from radians to degrees for display purposes
            float beamAngleInDeg = this.beamAngle * 180.0f / (float)Math.PI;

            // Display new numerical values

            mKinectData.mExtraData.AudioAngle = beamAngleInDeg;
        }

        /// <summary>
        /// Handles rendering energy visualization into a bitmap.
        /// </summary>
        /// <param name="sender">object sending the event.</param>
        /// <param name="e">event arguments.</param>
        private void UpdateEnergy(object sender, EventArgs e)
        {
            lock (this.energyLock)
            {
                // Calculate how many energy samples we need to advance since the last update in order to
                // have a smooth animation effect
                DateTime now = DateTime.UtcNow;
                DateTime? previousRefreshTime = this.lastEnergyRefreshTime;
                this.lastEnergyRefreshTime = now;

                // No need to refresh if there is no new energy available to render
                if (this.newEnergyAvailable <= 0)
                {
                    return;
                }

                if (previousRefreshTime != null)
                {
                    float energyToAdvance = this.energyError + (((float)(now - previousRefreshTime.Value).TotalMilliseconds * SamplesPerMillisecond) / SamplesPerColumn);
                    int energySamplesToAdvance = Math.Min(this.newEnergyAvailable, (int)Math.Round(energyToAdvance));
                    this.energyError = energyToAdvance - energySamplesToAdvance;
                    this.energyRefreshIndex = (this.energyRefreshIndex + energySamplesToAdvance) % this.energy.Length;
                    this.newEnergyAvailable -= energySamplesToAdvance;
                }
            }
        }
        #endregion

        public KinectData returnKinect()
        {
            return mKinectData;
        }
        private Bitmap ImageToBitmap(byte[] buffer, int width, int height)
        {
            Bitmap bmap = new Bitmap(width, height, System.Drawing.Imaging.PixelFormat.Format32bppRgb);
            BitmapData bmapdata = bmap.LockBits(new System.Drawing.Rectangle(0, 0, width, height), ImageLockMode.WriteOnly, bmap.PixelFormat);
            IntPtr ptr = bmapdata.Scan0;

            lock (buffer)
            {
                Marshal.Copy(buffer, 0, ptr, buffer.Length);
            }

            bmap.UnlockBits(bmapdata);
            return bmap;
        }
        #region Receive and Process Frames + Video Recording
        public void OnMultiSourceFrameArrived(object sender, MultiSourceFrameArrivedEventArgs e)
        {

            var frame = e.FrameReference.AcquireFrame();


            using (var colorFrame = frame.ColorFrameReference.AcquireFrame())
            {

                if ((colorFrame != null))
                {
                    if (Monitor.TryEnter(this.colorImageBuffer, 0))
                    {
                        try
                        {
                            if (colorFrame.RawColorImageFormat == ColorImageFormat.Bgra)
                                colorFrame.CopyRawFrameDataToArray(this.colorImageBuffer);
                            else
                                colorFrame.CopyConvertedFrameDataToArray(this.colorImageBuffer, ColorImageFormat.Bgra);
                            if (chkVideo == true) //show video preview
                                forminstance.update_preview(ImageToBitmap(colorImageBuffer, colorFrame.FrameDescription.Width, colorFrame.FrameDescription.Height));
                            if ((chkVideolog == true) && (activevideolog == true))
                            {   //HD video recording using openCV
                                imageBuffer = new Image<Bgra, byte>(1920, 1080);
                                imageBuffer.Bytes = this.colorImageBuffer;
                                imageCurrent = imageBuffer.Convert<Bgr, byte>(); //convert to compatible format
                                imageBuffer.Dispose();
                            }
                        }
                        finally
                        {
                            Monitor.Exit(this.colorImageBuffer);
                        }

                    }
                }
            }


            // BodyFrame

            using (var bodyFrame = frame.BodyFrameReference.AcquireFrame())
            {
                if (bodyFrame != null)
                {
                    if (bodies == null)
                        bodies = new Body[bodyFrame.BodyCount];

                    bodyFrame.GetAndRefreshBodyData(bodies);
                    float closestDistance = 100000f;
                    bodycount = 0;
                    foreach (var body in bodies)
                    {
                        if (body.IsTracked)
                        {
                            var target = body;
                            bodycount++;
                            float headPos = target.Joints[JointType.Head].Position.Z;

                            if (headPos < closestDistance)
                            {
                                //update variables with the closest skeleton
                                closestDistance = headPos;
                                this.faceSource.TrackingId = target.TrackingId;
                                this.highDefinitionFaceFrameSource.TrackingId = target.TrackingId;
                                DetectedPerson = true;
                                //left hand coordinates
                                mKinectData.mPointing.lefthandX = target.Joints[JointType.HandTipLeft].Position.X;
                                mKinectData.mPointing.lefthandY = target.Joints[JointType.HandTipLeft].Position.Y;
                                mKinectData.mPointing.lefthandZ = target.Joints[JointType.HandTipLeft].Position.Z;
                                //right hand coordinates
                                mKinectData.mPointing.righthandX = target.Joints[JointType.HandTipRight].Position.X;
                                mKinectData.mPointing.righthandY = target.Joints[JointType.HandTipRight].Position.Y;
                                mKinectData.mPointing.righthandZ = target.Joints[JointType.HandTipRight].Position.Z;
                                //lean body position
                                mKinectData.mExtraData.leanX = target.Lean.X;
                                mKinectData.mExtraData.leanY = target.Lean.Y;
                                //head location
                                mKinectData.mHeadLoc.headX = target.Joints[JointType.Head].Position.X;
                                mKinectData.mHeadLoc.headY = target.Joints[JointType.Head].Position.Y;
                                mKinectData.mHeadLoc.headZ = target.Joints[JointType.Head].Position.Z;
                                
                                //update headposition
                                headPosition = target.Joints[JointType.Head];

                            }

                        }
                    }

                }
            }


        }
        

        private static void ExtractFaceRotationInDegrees(Vector4 rotQuaternion, out int pitch, out int yaw, out int roll)
        {
            double x = rotQuaternion.X;
            double y = rotQuaternion.Y;
            double z = rotQuaternion.Z;
            double w = rotQuaternion.W;

            double yawD, pitchD, rollD;
            pitchD = Math.Atan2(2 * ((y * z) + (w * x)), (w * w) - (x * x) - (y * y) + (z * z)) / Math.PI * 180.0;
            yawD = Math.Asin(2 * ((w * y) - (x * z))) / Math.PI * 180.0;
            rollD = Math.Atan2(2 * ((x * y) + (w * z)), (w * w) + (x * x) - (y * y) - (z * z)) / Math.PI * 180.0;

            double increment = 1;
            pitch = (int)((pitchD + ((increment / 2.0) * (pitchD > 0 ? 1.0 : -1.0))) / increment) * (int)increment;
            yaw = (int)((yawD + ((increment / 2.0) * (yawD > 0 ? 1.0 : -1.0))) / increment) * (int)increment;
            roll = (int)((rollD + ((increment / 2.0) * (rollD > 0 ? 1.0 : -1.0))) / increment) * (int)increment;
        }
        private void HdFaceSource_TrackingIdLost(object sender, TrackingIdLostEventArgs e)
        {
            var lostTrackingID = e.TrackingId;

            if (this.CurrentTrackingId == lostTrackingID)
            {
                this.CurrentTrackingId = 0;


                this.highDefinitionFaceFrameSource.TrackingId = 0;
                DetectedPerson = false;


            }
        }

        public void HdFaceReader_FrameArrived(object sender, HighDefinitionFaceFrameArrivedEventArgs e)
        { 
            ulong? newTrackingId = null;
            using (var frame = e.FrameReference.AcquireFrame())
            {
                // We might miss the chance to acquire the frame; it will be null if it's missed.
                // Also ignore this frame if face tracking failed.
                if (frame != null)
                {
                    if (frame.IsTrackingIdValid && frame.IsFaceTracked)
                    {
                        frame.GetAndRefreshFaceAlignmentResult(this.currentFaceAlignment);
                        faceModel = frame.FaceModel;
                        newTrackingId = frame.TrackingId;

                        mKinectData.mAU.JawOpen = currentFaceAlignment.AnimationUnits[FaceShapeAnimations.JawOpen];
                        mKinectData.mAU.JawSlideRight = currentFaceAlignment.AnimationUnits[FaceShapeAnimations.JawSlideRight];
                        mKinectData.mAU.LeftcheekPuff = currentFaceAlignment.AnimationUnits[FaceShapeAnimations.LeftcheekPuff];
                        mKinectData.mAU.LefteyebrowLowerer = currentFaceAlignment.AnimationUnits[FaceShapeAnimations.LefteyebrowLowerer];
                        mKinectData.mAU.LefteyeClosed = currentFaceAlignment.AnimationUnits[FaceShapeAnimations.LefteyeClosed];
                        mKinectData.mAU.LipCornerDepressorLeft = currentFaceAlignment.AnimationUnits[FaceShapeAnimations.LipCornerDepressorLeft];
                        mKinectData.mAU.LipCornerDepressorRight = currentFaceAlignment.AnimationUnits[FaceShapeAnimations.LipCornerDepressorRight];
                        mKinectData.mAU.LipCornerPullerLeft = currentFaceAlignment.AnimationUnits[FaceShapeAnimations.LipCornerPullerLeft];
                        mKinectData.mAU.LipCornerPullerRight = currentFaceAlignment.AnimationUnits[FaceShapeAnimations.LipCornerPullerRight];
                        mKinectData.mAU.LipPucker = currentFaceAlignment.AnimationUnits[FaceShapeAnimations.LipPucker];
                        mKinectData.mAU.LipStretcherLeft = currentFaceAlignment.AnimationUnits[FaceShapeAnimations.LipStretcherLeft];
                        mKinectData.mAU.LipStretcherRight = currentFaceAlignment.AnimationUnits[FaceShapeAnimations.LipStretcherRight];
                        mKinectData.mAU.LowerlipDepressorLeft = currentFaceAlignment.AnimationUnits[FaceShapeAnimations.LowerlipDepressorLeft];
                        mKinectData.mAU.LowerlipDepressorRight = currentFaceAlignment.AnimationUnits[FaceShapeAnimations.LowerlipDepressorRight];
                        mKinectData.mAU.RightcheekPuff = currentFaceAlignment.AnimationUnits[FaceShapeAnimations.RightcheekPuff];
                        mKinectData.mAU.RighteyebrowLowerer = currentFaceAlignment.AnimationUnits[FaceShapeAnimations.RighteyebrowLowerer];
                        mKinectData.mAU.RighteyeClosed = currentFaceAlignment.AnimationUnits[FaceShapeAnimations.RighteyeClosed];
                    }
                }
            }


        }
        private void OnTrackingIdLost(object sender, TrackingIdLostEventArgs e)
        {
            this.faceSource.TrackingId = 0;
            DetectedPerson = false;

        }

        public void OnFaceFrameArrived(object sender, FaceFrameArrivedEventArgs e)
        {

            using (var faceFrame = e.FrameReference.AcquireFrame())
            {
                if (faceFrame == null) return;
                var result = faceFrame.FaceFrameResult;

                if (result == null)
                    return;

                var rotation = result.FaceRotationQuaternion;
                int x, y, z;
                ExtractFaceRotationInDegrees(rotation, out x, out y, out z);
                Vector3D headRotation = new Vector3D(x, y, z);
                mKinectData.mHeadDir.headYaw = x;
                mKinectData.mHeadDir.headPitch = y;
                mKinectData.mHeadDir.headYaw = z;
                mKinectData.mExtraData.Happiness = result.FaceProperties[FaceProperty.Happy].ToString();
            }
        }
        #endregion
    }
}
