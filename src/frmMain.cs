using NAudio.CoreAudioApi;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace KinectModule
{
    public partial class KinectModule : Form
    { 
        #region Variables

        private string lastIP;
        
        public static string startstop = "";
        public bool videorecording = false;

        #endregion
        private AsynchronousClient client;
        public clsKinect kinectclass;

        private clsKinectAudio kinectaudio;

        private clsKinect.KinectData mKinectData;
        
        public void kinectstatus(string status)
        {
            statuslabel.Invoke((MethodInvoker)delegate () {
                statuslabel.Text = status;

            });
        }


        public KinectModule()
        {
            InitializeComponent();
        }

        internal void updatecombo(List<MMDevice> devices)
        {
            comboWasapiDevices.DataSource = devices;
            comboWasapiDevices.DisplayMember = "FriendlyName";
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            kinectclass = new clsKinect(); //create new instance of kinect class
            kinectclass.InitializeKinect(this); //initialize the class using this form
            kinectaudio = new clsKinectAudio();
            comboWasapiDevices.DataSource = kinectaudio.LoadWasapiDevicesCombo();

            mKinectData = new clsKinect.KinectData();//kinect data struct
            client = new AsynchronousClient(); //client for TCP connection
            //set defaults
            chkVideo.Checked = true;
            chkVideoLog.Checked = true;
            chkLogging.Checked = true;
        }

        
        //updates the preview window with bitmap data from kinect
        public void update_preview(Bitmap tmpbitmap)
        {
            CurrentVideoFrame.Image = tmpbitmap;
        }

        //here we update the form controls every 500ms to reduce cpu usage
        private void interfaceTimer_Tick(object sender, EventArgs e)
        {

            if (statuslabel.Text == "True")
            {
                mKinectData = kinectclass.returnKinect();
                //data for AU so update UI
                if (mKinectData.mAU.LefteyebrowLowerer != 0)
                {
                    lbltxtJawOpen.Text = mKinectData.mAU.JawOpen.ToString("0.00");
                    lbltxtJawSlide.Text = mKinectData.mAU.JawSlideRight.ToString("0.00");
                    lbltxtLeftCheek.Text = mKinectData.mAU.LeftcheekPuff.ToString("0.00");
                    lbltxtLEftEyeBrow.Text = mKinectData.mAU.LefteyebrowLowerer.ToString("0.00");
                    lbltxtLeftEye.Text = mKinectData.mAU.LefteyeClosed.ToString("0.00");
                    lbltxtLipCornerDepLeft.Text = mKinectData.mAU.LipCornerDepressorLeft.ToString("0.00");
                    lbltxtLipCornerDepRight.Text = mKinectData.mAU.LipCornerDepressorRight.ToString("0.00");
                    lbltxtLipCornerPullLeft.Text = mKinectData.mAU.LipCornerPullerLeft.ToString("0.00");
                    lbltxtLipCornerPullRight.Text = mKinectData.mAU.LipCornerPullerRight.ToString("0.00");
                    lbltxtLipPucker.Text = mKinectData.mAU.LipPucker.ToString("0.00");
                    lbltxtLipStretcherLeft.Text = mKinectData.mAU.LipStretcherLeft.ToString("0.00");
                    lbltxtLipStretcherRight.Text = mKinectData.mAU.LipStretcherRight.ToString("0.00");
                    lbltxtLowerlipDepLeft.Text = mKinectData.mAU.LowerlipDepressorLeft.ToString("0.00");
                    lbltxtLowerlipDepRight.Text = mKinectData.mAU.LowerlipDepressorRight.ToString("0.00");
                    lbltxtRightcheeck.Text = mKinectData.mAU.RightcheekPuff.ToString("0.00");
                    lbltxtRightEyeBrow.Text = mKinectData.mAU.RighteyebrowLowerer.ToString("0.00");
                    lbltxtRightEye.Text = mKinectData.mAU.RighteyeClosed.ToString("0.00");
                }
                //data for skeleton so update UI
                if (mKinectData.mHeadLoc.headX != 0)
                {   //left hand coordinates
                    lbltxthandLX.Text = mKinectData.mPointing.lefthandX.ToString("0.00");
                    lbltxthandLY.Text = mKinectData.mPointing.lefthandY.ToString("0.00");
                    lbltxthandLZ.Text = mKinectData.mPointing.lefthandZ.ToString("0.00");
                    //right hand coordinates
                    lbltxthandRX.Text = mKinectData.mPointing.righthandX.ToString("0.00");
                    lbltxthandRY.Text = mKinectData.mPointing.righthandY.ToString("0.00");
                    lbltxthandRZ.Text = mKinectData.mPointing.righthandZ.ToString("0.00");
                    //grip status
                    lbltxthandRgrip.Text = mKinectData.mPointing.rightgrip.ToString();
                    lbltxthandLgrip.Text = mKinectData.mPointing.leftgrip.ToString();
                    //lean body position
                    lbltxtLeanX.Text = mKinectData.mExtraData.leanX.ToString("0.00");
                    lbltxtLeanY.Text = mKinectData.mExtraData.leanY.ToString("0.00");
                    //head location
                    lbltxtHeadXPos.Text = mKinectData.mHeadLoc.headX.ToString("0.00");
                    lbltxtHeadYPos.Text = mKinectData.mHeadLoc.headY.ToString("0.00");
                    lbltxtHeadZPos.Text = mKinectData.mHeadLoc.headZ.ToString("0.00");
                }
                //face data so update UI
                if (mKinectData.mHeadDir.headPitch != 0)
                {
                    lbltxtHeadPitch.Text = mKinectData.mHeadDir.headPitch.ToString("0.00");
                    lbltxtHeadYaw.Text = mKinectData.mHeadDir.headYaw.ToString("0.00");
                    lbltxtHeadRoll.Text = mKinectData.mHeadDir.headRoll.ToString("0.00");

                }
                //extra data
                if(mKinectData.mExtraData.AudioAngle!=0)
                {
                    lbltxtAudioAngle.Text = mKinectData.mExtraData.AudioAngle.ToString("0.00");
                    lbltxtAudioEnergy.Text = mKinectData.mExtraData.AudioLevel.ToString("0.00");

                }
                if(mKinectData.mExtraData.leanX!=0)
                {
                    lbltxtLeanX.Text = mKinectData.mExtraData.leanX.ToString("0.00");
                    lbltxtLeanY.Text = mKinectData.mExtraData.leanY.ToString("0.00");
                }
                lbltxtHappy.Text = mKinectData.mExtraData.Happiness;
            }
        }

        

        
        //connect to tcp server (NAO python server)
        private void cmdConnect_Click(object sender, EventArgs e)
        {
            lastIP = txtRobotIP.Text;
            txtRobotIP.Text= client.StartClient(System.Net.IPAddress.Parse(txtRobotIP.Text),this);
            cmdConnect.Enabled = !client.bConnected;
            cmdDisconnect.Enabled=client.bConnected;
            if (client.bConnected == true)
                addlog(txtRobotIP.Text);
            else
                addlog("Failed to connect");

        }
        //disconnect from tcp server
        private void cmdDisconnect_Click(object sender, EventArgs e)
        {
            client.CloseSocket();
            txtRobotIP.Text = lastIP;
            cmdConnect.Enabled = !client.bConnected;
            cmdDisconnect.Enabled = client.bConnected;
            addlog("Disconnected from server");
        }
        delegate void addlogcallback(string text);
        public void addlog(string datain)
        {
            if(logbox.InvokeRequired)
            {
                addlogcallback d = new addlogcallback(addlog);
                Invoke(d, new object[] { datain });
            }
            else
                logbox.Items.Insert(0,DateTime.Now + " : " + datain);
        }

        private void chkVideo_CheckedChanged(object sender, EventArgs e)
        {
            kinectclass.chkVideo = chkVideo.Checked;
        }

        private void chkVideoLog_CheckedChanged(object sender, EventArgs e)
        {
            kinectclass.chkVideolog = chkVideoLog.Checked;
        }

        public void startaudio(int participantID)
        {
            this.Invoke(new Action(() =>
            kinectaudio.startaudiocapture((MMDevice)comboWasapiDevices.SelectedItem, participantID)
            ));
        }

        public void stopaudio()
        {
            kinectaudio.StopRecording();
        }

        private void txtRobotIP_TextChanged(object sender, EventArgs e)
        {

        }
    }
   
}
