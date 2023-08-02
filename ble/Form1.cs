using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.Advertisement;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Devices.Enumeration;
using Windows.Foundation.Collections;
using Windows.Storage.Streams;
//gfhrfgrhfgh
namespace ble
{
    public partial class Form1 : Form
    {

        #region  Tanımlamalar

        private BluetoothLEAdvertisementWatcher watcher = null;
        private BluetoothLEDevice bluetoothLEDevice = null; 

        public GattDeviceServicesResult result { get; set; }
        public DeviceInformation device = null;

        public GattCharacteristic selectedCharacteristic = null;
        public GattDeviceService selectedService = null;

        private static int notificationCounter;
        private static DateTime lastValueChangedTime;

        public string HEART_RATE_SERVICE_ID = "6e400001-b5a3-f393-e0a9-e50e24dcca9e";
        private bool connected = false;

        #endregion

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            CheckForIllegalCrossThreadCalls = false;
        }


        #region Ble Connection
        
        
        private void Ble_startScanner()
        {
            listView1.Items.Clear();

            watcher = new BluetoothLEAdvertisementWatcher();
            
            watcher.ScanningMode = BluetoothLEScanningMode.Active;
            watcher.Received += OnAdcertisementReceived;

            watcher.SignalStrengthFilter.OutOfRangeTimeout = TimeSpan.FromMilliseconds(1000);
            watcher.SignalStrengthFilter.SamplingInterval = TimeSpan.FromMilliseconds(500);
            watcher.Start();

        }
        string name_buffer = "";
        string name_s = "";
        string Deviceid = "";
        private void OnAdcertisementReceived(BluetoothLEAdvertisementWatcher sender, BluetoothLEAdvertisementReceivedEventArgs args)
        {
            name_buffer = args.Advertisement.LocalName;

            if (name_buffer !=string.Empty)
            {
                name_s = name_buffer;
                Deviceid = args.BluetoothAddress.ToString();

                string[] bilgiler = { name_s, Deviceid };
                ListViewItem lst = new ListViewItem(bilgiler);
                listView1.Items.Add(lst);
            }
        }
        
        private void Ble_stopScanner()
        {
            watcher.Stop();
            watcher.ScanningMode = BluetoothLEScanningMode.Passive;
            watcher.Received -= OnAdcertisementReceived;
        }

        string name = "";
        private async void Ble_Connection()
        {
            label1.Text = "Connecting..";

            if (listView1.SelectedItems.Count>0)
            {
                name = listView1.SelectedItems[0].SubItems[0].Text;

                string[] requestedProperties = { "System.Devices.Aep.DeviceAddress", "System.Devices.Aep.IsConnected" , "System.Devices.Aep.Bluetooth.Le.IsConnectable" };
                

                DeviceWatcher deviceWatcher =
                            DeviceInformation.CreateWatcher(
                                    BluetoothLEDevice.GetDeviceSelectorFromPairingState(false),
                                    requestedProperties,
                                    DeviceInformationKind.AssociationEndpoint);


                deviceWatcher.Added += DeviceWatcher_Added;
                deviceWatcher.Updated += DeviceWatcher_Updated;
                deviceWatcher.Removed += DeviceWatcher_Removed;

                deviceWatcher.EnumerationCompleted += DeviceWatcher_EnumerationCompleted;
                deviceWatcher.Stopped += DeviceWatcher_Stopped;

                deviceWatcher.Start();

                if (connected)
                {
                    return; // Zaten bağlantı kurulmuşsa bağlantıyı yeniden başlatma
                }
                connected = true;

                while (connected)
                {
                    if (device==null)
                    {
                        Thread.Sleep(200);
                    }
                    else
                    {

                        bluetoothLEDevice = await BluetoothLEDevice.FromIdAsync(device.Id);
                        result = await bluetoothLEDevice.GetGattServicesAsync();

                        if (result.Status==GattCommunicationStatus.Success)
                        {
                           
                            var services = result.Services;
                            foreach (var service in services)
                            {
                                if (service.Uuid.ToString() == HEART_RATE_SERVICE_ID)
                                {
                                    label1.Text = "Connecting..";
                                    GattCharacteristicsResult characteristicsResult = await service.GetCharacteristicsAsync();
                                    if (characteristicsResult.Status==GattCommunicationStatus.Success)
                                    {
                                        label1.Text = "Connecting..";
                                        var characteristics = characteristicsResult.Characteristics;
                                        foreach (var characteristic in characteristics)
                                        {
                                            GattCharacteristicProperties properties = characteristic.CharacteristicProperties;

                                            if (properties.HasFlag(GattCharacteristicProperties.Notify))
                                            {
                                                GattCommunicationStatus status = await characteristic.WriteClientCharacteristicConfigurationDescriptorAsync(GattClientCharacteristicConfigurationDescriptorValue.Notify);
                                                if (status==GattCommunicationStatus.Success)
                                                {
                                                    label1.Text = "Connected..";
                                                    characteristic.ValueChanged += Characteristic_ValueChanged;
                                                    //bluetoothLEDevice.ConnectionStatusChanged += BluetoothLeDevice_ConnectionStatusChanged;
                                                    selectedCharacteristic = characteristic;


                                                    lastValueChangedTime = DateTime.Now;
                                                    notificationCounter = 0;
                                                    //label1.Text=$"{bluetoothLEDevice.ConnectionStatus}";
                                                }
                                                    
                                             }

                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        

    /*private void BluetoothLeDevice_ConnectionStatusChanged(BluetoothLEDevice sender, object args)
        {
            throw new NotImplementedException();
        }*/

        private void Characteristic_ValueChanged(GattCharacteristic sender, GattValueChangedEventArgs args)
        {
            var reader = DataReader.FromBuffer(args.CharacteristicValue);
            var flags = reader.ReadByte();
            var value = reader.ReadByte();

            // flags ve value değerlerini listView2'ye ekleyelim
            ListViewItem newItem = new ListViewItem(new string[] { flags.ToString(), value.ToString() });

            // listView2'ye yeni öğeyi ekleyelim
            listView2.Items.Add(newItem);
            var currentTime = DateTime.Now;
            var timeDiff = currentTime - lastValueChangedTime;
            lastValueChangedTime = currentTime;

            // Eğer zaman farkı 0.2 saniyeden büyükse, sinyal kesintisi olduğu kabul edilir
            if (timeDiff.TotalSeconds > 0.2)
            {
                notificationCounter++;
                //Console.WriteLine($"Sinyal kesintisi! Kesinti sayısı: {notificationCounter}");
            }
        }

        private void DeviceWatcher_Stopped(DeviceWatcher sender, object args)
        {
            
        }

        private void DeviceWatcher_EnumerationCompleted(DeviceWatcher sender, object args)
        {
            
        }

        private void DeviceWatcher_Removed(DeviceWatcher sender, DeviceInformationUpdate args)
        {
            
        }

        private void DeviceWatcher_Updated(DeviceWatcher sender, DeviceInformationUpdate args)
        {
            
        }

        private void DeviceWatcher_Added(DeviceWatcher sender, DeviceInformation args)
        {

            if (args.Name == "FIZYOSOFT")
            {
                device = args;
            }
            
            
        }
        
        #endregion

        #region Button Click Events

        private void button1_Click(object sender, EventArgs e)
        {

            Ble_startScanner();
            label1.Text = "Scanning..";

            // ListView2'yi temizleme
            listView2.Items.Clear();
            label2.Text = "Toplam Kesinti Sayısı : ";
        }

        private void button2_Click(object sender, EventArgs e)
        {
            Ble_stopScanner();
            label1.Text = "Scanning Stop..";
        }

        private void button3_Click(object sender, EventArgs e)
        {
            Ble_Connection();
            //label1.Text = "Connected..";
        }

        private void button4_Click(object sender, EventArgs e)
        {
            
            // Bağlantıyı durdurma ve gelen verileri temizleme
            connected = false; // Döngüyü sonlandır

            if (bluetoothLEDevice != null)
            {
                if (selectedCharacteristic != null)
                {
                    // Gelen veri olayı bağlantısını kaldırma
                    selectedCharacteristic.ValueChanged -= Characteristic_ValueChanged;
                }

                // Cihazı kapatma
                bluetoothLEDevice.Dispose();
                bluetoothLEDevice = null;
                selectedCharacteristic = null;
            }
            label1.Text = "Disconnected";
            label2.Text= $"Toplam Kesinti Sayısı :  {notificationCounter}";
           

        }

        #endregion

        private void listView1_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void listView2_SelectedIndexChanged(object sender, EventArgs e)
        {
            
        }
       
    }
}
