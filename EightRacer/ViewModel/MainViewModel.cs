using EightRacer.Model;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using Microsoft.Devices.Sensors;
using Microsoft.Xna.Framework;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using Windows.Networking.Proximity;
using Windows.Networking.Sockets;
using Windows.Phone.Speech.Synthesis;
using Windows.Storage.Streams;

namespace EightRacer.ViewModel
{
    /// <summary>
    /// This class contains properties that the main View can data bind to.
    /// <para>
    /// See http://www.galasoft.ch/mvvm
    /// </para>
    /// </summary>
    public class MainViewModel : ViewModelBase
    {
        private readonly IDataService _dataService;
        private PairedDeviceInfo pdi;
        private DataWriter dw;
        private StreamSocket socket;
        private bool isConnected;

        private Accelerometer accelerometer;
        private DispatcherTimer timer;
        private Vector3 acceleration;
        private bool isDataValid;

        /// <summary>
        /// Initializes a new instance of the MainViewModel class.
        /// </summary>
        public MainViewModel(IDataService dataService)
        {
            ButtonConnect = new RelayCommand(DoButtonConnect);
            ButtonDisconnect = new RelayCommand(DoButtonDisconnect);
            PairedDeviceSelected = new RelayCommand<SelectionChangedEventArgs>(DoPairedDeviceSelected);
            CheckBlueToothOn();
            _pdis = new ObservableCollection<PairedDeviceInfo>();
            PopulatePairedDevicesListBox();
            InitializeSensors(); // wire up accelerometer 
        }

        #region properties

        public RelayCommand ButtonConnect { get; private set; }
        public RelayCommand ButtonDisconnect { get; private set; }
        public ICommand PairedDeviceSelected { get; private set; }

        /// <summary>
        /// The <see cref="DebugMessages" /> property's name.
        /// </summary>
        public const string DebugMessagesPropertyName = "DebugMessages";

        private string _debugMessages = "";

        /// <summary>
        /// Sets and gets the DebugMessages property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        public string DebugMessages
        {
            get { return _debugMessages; }

            set
            {
                if (_debugMessages == value)
                {
                    return;
                }

                RaisePropertyChanging(DebugMessagesPropertyName);
                _debugMessages = value;
                RaisePropertyChanged(DebugMessagesPropertyName);
            }
        }

        /// <summary>
        /// The <see cref="Pdis" /> property's name.
        /// Paired Device Info
        /// </summary>
        public const string PdiPropertyName = "Pdis";

        private ObservableCollection<PairedDeviceInfo> _pdis;

        /// <summary>
        /// Sets and gets the Pdis property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        public ObservableCollection<PairedDeviceInfo> Pdis
        {
            get { return _pdis; }

            set
            {
                if (_pdis == value)
                {
                    return;
                }

                RaisePropertyChanging(PdiPropertyName);
                _pdis = value;
                RaisePropertyChanged(PdiPropertyName);
            }
        }

        #endregion

        #region Methods

        private IBuffer GetBufferFromByte(byte package)
        {
            using (DataWriter dw = new DataWriter())
            {
                dw.WriteByte(package);
                return dw.DetachBuffer();
            }
        }

        //private byte IntToByte(uint a, uint b)
        //{
        //    uint x = a << 4;
        //    uint y = x | b;
        //    return (byte)y;
        //}

        private byte Shifty(int a, int b)
        {
            int x = a << 4;
            return (byte)(x | b);
        }

        private async void DoButtonConnect()
        {
            using (SpeechSynthesizer synth = new SpeechSynthesizer())
            {
                await synth.SpeakTextAsync("Connecting to Selected");
            }

            try
            {
                socket = new StreamSocket();
                var peer = pdi.PeerInfo;
                //string serviceName = "MiRacer";

                await socket.ConnectAsync(peer.HostName, "1");
                isConnected = true;
                DebugMessages += String.Format("AppResources.Msg_ConnectedTo {0}",
                                               socket.Information.RemoteAddress.DisplayName);
            }
            catch (Exception e)
            {
                if (pdi == null)
                {
                    MessageBox.Show("Please select Dagu car first"); // user may not have selected the paired Dagu car from list.
                }
                else
                    MessageBox.Show("Sorry, failed to connect");
                Debug.WriteLine(e.ToString());
                isConnected = false;
            }
        }

        private async void DoButtonDisconnect()
        {
            try
            {
                var buff = GetBufferFromByte(Shifty(0, 0));
                await socket.OutputStream.WriteAsync(buff);

                socket.Dispose();
                isConnected = false;
            }
            catch (Exception e)
            {
                MessageBox.Show("Error disposing");
            }
        }

        private IBuffer GetBufferFromByteArray(byte[] package)
        {
            using (DataWriter dw = new DataWriter())
            {
                dw.WriteBytes(package);
                return dw.DetachBuffer();
            }
        }

        private async void PopulatePairedDevicesListBox()
        {
            // Configure PeerFinder to search for all paired devices.
            PeerFinder.AlternateIdentities["Bluetooth:Paired"] = "";
            var pairedDevices = await PeerFinder.FindAllPeersAsync();

            if (pairedDevices.Count == 0)
            {
                Debug.WriteLine("No paired devices were found.");
            }
            else
            {
                foreach (var d in pairedDevices)
                {
                    Pdis.Add(new PairedDeviceInfo(d));
                }
            }
        }

        private int x, y, dir, speed;
        private int yLimit = 2;

        private async void timer_Tick(object sender, EventArgs e)
        {
            //DebugMessages = string.Format("X: {0} Y: {1} Z: {2}",
            //                                      acceleration.X.ToString("0.00"),
            //                                      acceleration.Y.ToString("0.00"),
            //                                      acceleration.Z.ToString("0.00")
            //            );

            DebugMessages = string.Format(
                // "X: {0} Y: {1} ", acceleration.X.ToString("0.00"), acceleration.Y.ToString("0.00"));
                "X: {0} Y: {1} ", acceleration.Y.ToString("0.00"), acceleration.X.ToString("0.00")); // x and y are reversed between phone and input to i-Racer

            if (isConnected)
            {
                if (isDataValid)
                {
                    x = (int)ofMap(acceleration.Y, -1.00f, 1.00f, -0xF, 0xF); // X = forward / backward *** Y = left/right on racer
                    // Although I get up to 1 (-1) from acceleration I want to make the control less drastic in the tilt action of the phone. 
                    // 3x = (int)ofMap(acceleration.Y, -0.75f, 0.75f, -0xF, 0xF); // X = forward / backward *** Y = left/right on racer
                    y = (int)ofMap(acceleration.X, -1.00f, 1.00f, -10, 10);

                    if (x == 0 && y == 0)
                    {
                        Debug.WriteLine("Stop - x: {0} \ty: {1}", x, y);
                        dir = 0;
                        speed = 0;
                        goto TheEnd;
                    }

                    // straight
                    if (x > 0 & (y < yLimit & y > -yLimit))
                    {
                        dir = 1;
                        speed = Math.Abs(x);
                        goto TheEnd;
                    }

                    // backward
                    if (x < 0 & (y < yLimit & y > -yLimit))
                    {
                        dir = 2;
                        speed = Math.Abs(x);
                        goto TheEnd;
                    }
                    if (x < 0 && y < yLimit)
                    {
                        Debug.WriteLine("Left Backward \t\tx: {0} \ty: {1}", x, y);
                        dir = 7;
                        speed = Math.Abs(x);
                        goto TheEnd;
                    }
                    if (x < 0 && y > yLimit)
                    {
                        Debug.WriteLine("Right Backward - x < y >  \tx: {0} \ty: {1}", x, y);
                        dir = 8;
                        speed = Math.Abs(x);
                        goto TheEnd;
                    }
                    if (x > 0 && y > yLimit)
                    {
                        Debug.WriteLine("Right Forward - x > y > \tx: {0} \ty: {1}", x, y);
                        dir = 6;
                        speed = Math.Abs(x);
                        goto TheEnd;
                    }
                    if (x > 0 && y < yLimit)
                    {
                        Debug.WriteLine("left forward - x > y <  \tx: {0} \ty: {1}", x, y);
                        dir = 5;
                        speed = Math.Abs(x);
                    }

                TheEnd:

                    var buff = GetBufferFromByte(Shifty(dir, speed));
                    await socket.OutputStream.WriteAsync(buff);
                }
            }
        }

        private async void DoPairedDeviceSelected(SelectionChangedEventArgs args)
        {
            pdi = args.AddedItems[0] as PairedDeviceInfo;
            DebugMessages = pdi.DisplayName;
            DebugMessages += "\n";
            DebugMessages += pdi.HostName;
            DebugMessages += "\n";
            DebugMessages += pdi.ServiceName;
            DebugMessages += "\n";

            using (SpeechSynthesizer synth = new SpeechSynthesizer())
            {
                await synth.SpeakTextAsync(pdi.DisplayName);
            }
        }

        private async void CheckBlueToothOn()
        {
            // Search for all paired devices
            PeerFinder.AlternateIdentities["Bluetooth:Paired"] = "";

            try
            {
                var peers = await PeerFinder.FindAllPeersAsync();
            }
            catch (Exception ex)
            {
                if ((uint)ex.HResult == 0x8007048F)
                {
                    MessageBox.Show("Bluetooth is turned off");
                }
            }
        }

        // From modified from .cpp openframeworks.cc
        float ofMap(float value, float inputMin, float inputMax, float outputMin, float outputMax)
        {
            float outVal = ((value - inputMin) / (inputMax - inputMin) * (outputMax - outputMin) + outputMin);
            return outVal;
        }

        private void InitializeSensors()
        {
            if (!Accelerometer.IsSupported)
            {
                // The device on which the application is running does not support
                // the accelerometer sensor. Alert the user and hide the
                // application bar.
                DebugMessages = "device does not support Accelerometer";
                // ApplicationBar.IsVisible = false;
            }
            else
            {
                // Initialize the timer and add Tick event handler, but don't start it yet.
                timer = new DispatcherTimer();
                timer.Interval = TimeSpan.FromMilliseconds(30);
                timer.Tick += new EventHandler(timer_Tick);

                // Instantiate the accelerometer.
                accelerometer = new Accelerometer();

                // Specify the desired time between updates. The sensor accepts
                // intervals in multiples of 20 ms.
                accelerometer.TimeBetweenUpdates = TimeSpan.FromMilliseconds(20);

                // The sensor may not support the requested time between updates.
                // The TimeBetweenUpdates property reflects the actual rate.
                // timeBetweenUpdatesTextBlock.Text = accelerometer.TimeBetweenUpdates.TotalMilliseconds + " ms";

                accelerometer.CurrentValueChanged += new EventHandler<SensorReadingEventArgs<AccelerometerReading>>(accelerometer_CurrentValueChanged);
            }
            try
            {
                DebugMessages = "starting accelerometer.";
                accelerometer.Start();
                timer.Start();
            }
            catch (InvalidOperationException)
            {
                DebugMessages = "unable to start accelerometer.";
            }
        }

        void accelerometer_CurrentValueChanged(object sender, SensorReadingEventArgs<AccelerometerReading> e)
        {
            // Note that this event handler is called from a background thread
            // and therefore does not have access to the UI thread. To update 
            // the UI from this handler, use Dispatcher.BeginInvoke() as shown.
            // Dispatcher.BeginInvoke(() => { statusTextBlock.Text = "in CurrentValueChanged"; });

            isDataValid = accelerometer.IsDataValid;
            acceleration = e.SensorReading.Acceleration;
        }
        #endregion
    }
}
