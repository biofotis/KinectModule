using System;

using NAudio.Wave;
using NAudio.CoreAudioApi;
using System.Linq;
using System.Diagnostics;
using System.Collections.Generic;

namespace KinectModule
{
    public class clsKinectAudio
    {
        private IWaveIn waveIn;
        private WaveFileWriter writer;
        private string outputFilename;

        public List<MMDevice> LoadWasapiDevicesCombo()
        {
            var deviceEnum = new MMDeviceEnumerator();
            var devices = deviceEnum.EnumerateAudioEndPoints(DataFlow.Capture, DeviceState.Active).ToList();

            return devices;

        }
        private void Cleanup()
        {
            if (waveIn != null)
            {
                waveIn.Dispose();
                waveIn = null;
            }
            FinalizeWaveFile();
        }

        private void FinalizeWaveFile()
        {
            if (writer != null)
            {
                writer.Dispose();
                writer = null;
            }
        }
        public void StopRecording()
        {
            Debug.WriteLine("StopRecording");
            if (waveIn != null) waveIn.StopRecording();
            FinalizeWaveFile();
        }

        public void startaudiocapture(MMDevice device,int participantID)
        {
            Cleanup(); 

            if (waveIn == null)
            {
                waveIn = CreateWaveInDevice(device);
            }         
            device.AudioEndpointVolume.Mute = false;
            outputFilename = String.Format(participantID.ToString() + " {0:yyy-MM-dd HH-mm-ss}.wav", DateTime.Now);
            writer = new WaveFileWriter(outputFilename, waveIn.WaveFormat);
            waveIn.StartRecording();
            
        }

        void OnDataAvailable(object sender, WaveInEventArgs e)
        {
                writer.Write(e.Buffer, 0, e.BytesRecorded); 
        }

        private IWaveIn CreateWaveInDevice(MMDevice device)
        {
            IWaveIn newWaveIn;
            newWaveIn = new WasapiCapture(device);
            newWaveIn.DataAvailable += OnDataAvailable;
            return newWaveIn;
        }
    }
}