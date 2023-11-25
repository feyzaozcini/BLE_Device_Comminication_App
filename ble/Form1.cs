using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.Advertisement;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Devices.Enumeration;
using System.Numerics;
using System.Runtime.InteropServices.WindowsRuntime;

namespace ble
{
    public partial class Form1 : Form
    {

        #region  Definitions

        private BluetoothLEAdvertisementWatcher watcher = null;
        private BluetoothLEDevice bluetoothLEDevice = null; 

        public GattDeviceServicesResult result { get; set; }
        public object MathHelper { get; private set; }

        public DeviceInformation device = null;

        public GattCharacteristic selectedCharacteristic = null;
        public GattDeviceService selectedService = null;

        private static int notificationCounter;
        private static DateTime lastValueChangedTime;

        static int x; 
        static int y; 
        static int z;
        static int w;
        static int h;
        static int r;
        static int p;
        static int battery;
        
        public int index;

        public string SERVICE_ID = "Your Service Id";
        private bool connected = false;
        Vector3 referenceVector = new Vector3(0, 0, 0);
        Vector3 currentVector;
        Vector3 angleDegrees;

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
        private static object angle;
        private static object kalibrasyon;
        private static object checkSum;

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
                        Thread.Sleep(2000);
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
                                if (service.Uuid.ToString() == SERVICE_ID)   
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
                                                    if (characteristic != null)
                                                    {
                                                        characteristic.ValueChanged += Characteristic_ValueChanged;
                                                        timer1.Start();
                                                    }
                                                    lastValueChangedTime = DateTime.Now;
                                                    notificationCounter = 0;   
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
        

        private async void Characteristic_ValueChanged(GattCharacteristic sender, GattValueChangedEventArgs args)
        {
            
            byte[] buffer = new byte[33];

            while (true)
            {
                GattReadResult readResult = await sender.ReadValueAsync();
                if (readResult.Status == GattCommunicationStatus.Success)
                {
                    var data = readResult.Value.ToArray();
                    
                    
                    int dataLength = data.Length;
                    int offset = 0;

                    while (offset < dataLength)
                    {
                        int remainingBytes = dataLength - offset;
                        int bytesToCopy = Math.Min(buffer.Length, remainingBytes);

                        Array.Copy(data, offset, buffer, 0, bytesToCopy);

                        string newBuffer = BitConverter.ToString(buffer);
                        string cleanedData = newBuffer.Replace("-", "");

                        

                        // x,y,z,w,h,r,p 4 bytelık 8 karakterler 

                        for (int i = 0; i < cleanedData.Length; i += 8)
                        {
                            if (i + 8 <= cleanedData.Length)
                            {
                                string hexValue = cleanedData.Substring(i, 8); // Her bir 4 byte'lık (8 karakter) veriyi alıyoruz.


                                int decimalValue = Convert.ToInt32(hexValue, 16);

                                // İlk 4 byte verisi x değişkenine atanıyor.
                                if (i == 0)
                                {
                                    x = decimalValue;
                                    x = x % 360;
                                    
                                }
                                // Sonraki 4 byte verisi y değişkenine atanıyor.
                                else if (i == 8)
                                {
                                    y = decimalValue;
                                    y = y % 360;
                                    
                                   
                                }
                                else if (i == 16)
                                {
                                    z = decimalValue;
                                    z = z % 360;
                                
                                }
                                else if (i == 24)
                                {
                                    w = decimalValue;
                                    w = w % 360;
                                    
                                  
                                }
                                else if (i == 32)
                                {
                                    h = decimalValue;
                                    h = h % 360;

                                }
                                else if (i == 40)
                                {
                                    r = decimalValue;
                                    r = r % 360;

                                }
                                else if (i == 48)
                                {
                                    p = decimalValue;
                                    p = p % 360;

                                }
                            }
                        }
                        
                       
             
                        if (referenceVector != null && currentVector != null)
                        { 
                            angleDegrees = CalculateAngle(referenceVector, currentVector);
                            
                        }

                            //batarya , kalibrasyon , CheckSum, ve 2 bytelık end of packet değerleri ayrı değerlendirildi
                            for (int i = 56; i < cleanedData.Length; i += 2)
                        {
                            if (i + 2 <= cleanedData.Length)
                            {
                                string hexvalue = cleanedData.Substring(i, 2);
                                int decimalValue = Convert.ToInt32(hexvalue, 16);
                                if (i == 56)
                                {
                                    battery = decimalValue;

                                }
                                else if (i == 58)
                                {
                                    int kalibrasyon = decimalValue;
                                    
                                }
                                else if (i == 60)
                                {
                                    int checkSum = decimalValue;
                                    
                                }
                                else if (i == 62)
                                {
                                    int son = decimalValue;

                                }
                                else if (i == 64)
                                {
                                    int end = decimalValue;

                                }
                            }
                        }
                        offset += bytesToCopy;
                    }
                    currentVector = new Vector3(x, y, z);
                    await Task.Delay(5000);
                }
                else
                {
                    // Okuma hatası oluştu.
                    break;
                }
            }

            //Kesinti sayısı
            var currentTime = DateTime.Now;
            var timeDiff = currentTime - lastValueChangedTime;
            lastValueChangedTime = currentTime;

            // Eğer zaman farkı 0.2 saniyeden büyükse, sinyal kesintisi olduğu kabul edilir (ayarlanmadı)
            if (timeDiff.TotalSeconds > 0.4)
            {
                notificationCounter++;
            }
        }
        //Kalibreden sonraki hesap yapma kısmı
        private Vector3 CalculateAngle(Vector3 v1, Vector3 v2)
        {
    
            Vector3 deltaX = v1 - v2;
            return deltaX;
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
            if (args.Name == "Your Device Name")
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
            label2.Text = "Number Of Outages :  : ";
        }

        private void button2_Click(object sender, EventArgs e)
        {
            Ble_stopScanner();
            label1.Text = "Scanning Stop..";
        }

        private void button3_Click(object sender, EventArgs e)
        {
            Ble_Connection();
            
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
            timer1.Stop();
            timer2.Stop();
            label1.Text = "Disconnected";
            label2.Text= $"Toplam Kesinti Sayısı :  {notificationCounter}";
           
        }

        private void button5_Click(object sender, EventArgs e)
        {
            timer2.Start();
            referenceVector = new Vector3(x, y, z);

            label3.Text = $"Ref.Koordinatları : {referenceVector.ToString()}";
        }


        #endregion

        private void listView1_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void listView2_SelectedIndexChanged(object sender, EventArgs e)
        {
            
        }
        #region Timer Settings
        private void timer1_Tick(object sender, EventArgs e)
        {
            ListViewItem newItem = new ListViewItem(new string[] { x.ToString(), y.ToString(), z.ToString(), w.ToString(), battery.ToString() });
            listView2.Items.Add(newItem);
        }

        private void timer2_Tick(object sender, EventArgs e)
        {
            ListViewItem newItem = new ListViewItem(new string[] { angleDegrees.X.ToString(), angleDegrees.Y.ToString(), angleDegrees.Z.ToString() });
            listView3.Items.Add(newItem);
        }

        
        #endregion
    }
}
