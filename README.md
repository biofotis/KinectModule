This KinectModule allows you to capture all the available streams from Kinect v2.

Supports: 
Full HD video recording (1080p) in Xvid format
High Definition Face Data (Auction units) capture
Pointing (both hands) capture
Head Location and rotation capture
Audio Angle and level capture
4 Channel Audio capture
Body Lean position
Video preview

Reguirements:
Microsoft Kinect v2
USB 3.0 port capable for Kinect v2
WIndows 8 or 10 64bit
Microsoft Kinect SDK 2.0

How to test it:
At this stage this module behaves as client while the the server sits on the NAO and waits for connections. 
In order to run it, you must install the service (tools/ServerNAO) on NAO and reboot it. Alternatively, you can create a Server that utilizes the same commands in order to start/stop the KinectModule.
Once installed you can run the client module within C# and define your robot's IP address and press connect. 
Once connected, the robot can start/stop the data/video/audio logging by sending the commands. You can manually trigger the functions by activating these commands with SSH
L2Kinect.startcapture participants_name wil activate the capture on kinect
L2Kinect.stopcapture will finish the capture and save the data

The output files consist of:
Video#.avi High definition recording
Audio#.wav 4 Channel Audio files
LogKinect#.csv CSV files with logged data and timestamps

All of these files are synchronised with each other down to millisecond for offline analysis
