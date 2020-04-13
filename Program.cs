using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.System;

using Windows.Devices.Bluetooth.Advertisement;
using Windows.Storage.Streams;

namespace ConsoleApplication1
{

    class Program
    {


        static void Main(string[] args)
        {
            // Start the program
            var program = new Program();

            // Close on key press
            Console.ReadLine();
        }

        public void StartWatcher()
        {
            // Create Bluetooth Listener
            var watcher = new BluetoothLEAdvertisementWatcher();

            watcher.ScanningMode = BluetoothLEScanningMode.Passive;

            // Only activate the watcher when we're recieving values >= -80
            watcher.SignalStrengthFilter.InRangeThresholdInDBm = -100;

            // Stop watching if the value drops below -90 (user walked away)
            watcher.SignalStrengthFilter.OutOfRangeThresholdInDBm = -110;

            // Register callback for when we see an advertisements
            watcher.Received += OnAdvertisementReceived;

            // Wait 5 seconds to make sure the device is really out of range
            watcher.SignalStrengthFilter.OutOfRangeTimeout = TimeSpan.FromMilliseconds(5000);
            watcher.SignalStrengthFilter.SamplingInterval = TimeSpan.FromMilliseconds(2000);

            // Starting watching for advertisements
            watcher.Start();
        }

        public Program()
        {
            StartWatcher();
        }

        private void OnAdvertisementReceived(BluetoothLEAdvertisementWatcher watcher, BluetoothLEAdvertisementReceivedEventArgs eventArgs)
        {



            ConbeeBeacon beacon = new ConbeeBeacon();

            if (eventArgs.BluetoothAddress.ToString("X2").StartsWith("D0") || true)
            {

                var manufacturerSections = eventArgs.Advertisement.ManufacturerData;
                if (manufacturerSections.Count > 0)
                {
                    // Only print the first one of the list
                    var manufacturerData = manufacturerSections[0];
                    var data = new byte[manufacturerData.Data.Length];
                    using (var reader = DataReader.FromBuffer(manufacturerData.Data))
                    {
                        reader.ReadBytes(data);

                        beacon = ParseConbee(data);
                        
                    }

                }
                if ((beacon.foundDegreeRough || beacon.foundDegreeDetailed || beacon.foundLux) && eventArgs.BluetoothAddress.ToString("X2").StartsWith("D0"))
                {
                    Console.WriteLine();
                    Console.WriteLine(String.Format("  {0} {1} (dBm: {2}) °C: {3} / {4} - Lux: {5}", DateTime.Now.ToString(), FormatMacID(eventArgs.BluetoothAddress.ToString("X2")), eventArgs.RawSignalStrengthInDBm, beacon.degreeCelsiusrough, beacon.degreeCelsiusdetailed, beacon.lux));

                }
                else
                {


                    if (eventArgs.BluetoothAddress.ToString("X2").StartsWith("D0"))
                    {
                        Console.Write(".");
                    }
                    else
                    {
                        Console.Write("+");
                    }
                }

            }
        }
        public static string ByteArrayToString(byte[] ba)
        {
            StringBuilder hex = new StringBuilder(ba.Length * 2);
            foreach (byte b in ba)
                hex.AppendFormat("{0:x2}", b);
            return hex.ToString();
        }
        public static ConbeeBeacon ParseConbee(byte[] data)
        {

            ConbeeBeacon returnvalue = new ConbeeBeacon();

            int toLength = data.Length;
            int pointer = 0;

            while (pointer < toLength)
            {
            
                byte[] temparray = new byte[data[pointer] + 1];
                System.Buffer.BlockCopy(data, pointer, temparray, 0, data[pointer] + 1);

                if (temparray[1].Equals(0x01))
                {
                
                    returnvalue.AmbientLight = ByteArrayToString(temparray);
                    String tempValue = ByteArrayToString(new byte[] { temparray[3], temparray[4] });
                    
                    returnvalue.lux = (float)Int16.Parse(tempValue, System.Globalization.NumberStyles.HexNumber);
                    returnvalue.foundLux = true;
                }

                if (temparray[1].Equals(0x02))
                {
                    
                    if (temparray[2].Equals(0x01))
                    {
                    
                        returnvalue.TemperatureLong = ByteArrayToString(temparray);
                        
                        byte[] bytes = new byte[] { temparray[3], temparray[4], temparray[5], temparray[6] };
                        bytes = bytes.Reverse().ToArray();
                        returnvalue.degreeCelsiusdetailed = BitConverter.ToSingle(bytes, 0); ;
                        returnvalue.foundDegreeDetailed = false;
                    }
                    if (temparray[2].Equals(0x04))
                    {
                        
                        returnvalue.TemperatureShort = ByteArrayToString(temparray);

                        String tempValue = ByteArrayToString(new byte[] { temparray[3], temparray[4] });
                        returnvalue.degreeCelsiusrough = (float)unchecked(Int16.Parse(tempValue, System.Globalization.NumberStyles.HexNumber)) / 100;
                        returnvalue.foundDegreeRough = false;

                    }
                }



                pointer = pointer + data[pointer] + 1;

            }



            return returnvalue;
        }
        public static String FormatMacID(String macid)
        {
            String returnvalue = "";
            for (int i = 0; i < macid.Length; i = i + 2)
            {
                if (returnvalue.Length > 0)
                {
                    returnvalue = returnvalue + ":";
                }
                returnvalue = returnvalue + macid.Substring(i, 2);
            }
            return returnvalue;
        }
    }
    class ConbeeBeacon
    {
        public String AmbientLight;
        public String TemperatureLong;
        public String TemperatureShort;
        public float lux;
        public float degreeCelsiusrough;
        public float degreeCelsiusdetailed;
        public bool foundLux = false;
        public bool foundDegreeRough = false;
        public bool foundDegreeDetailed = false;

    }
}
