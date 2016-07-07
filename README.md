This KinectModule allows you to capture all the available streams from Kinect v2.

At this stage this module behaves as client while the the server sits on the NAO and waits for connections. 
In order to run it, you must install the service on NAO similarly to the SimpeTask project.
Once installed you can run the client module within C# and define your robot's IP address and press connect. 
Once connected, the robot can start/stop the data/video/audio logging by sending the commands. You can manually trigger the functions by activating these commands with SSH
L2Kinect.startcapture participants_name wil activate the capture on kinect
L2Kinect.stopcapture will finish the capture and save the data

