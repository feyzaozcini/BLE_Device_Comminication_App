# BluetoothLE Sensor Data Monitor

Bu proje, C# programlama dili kullanılarak geliştirilmiş bir Windows Forms uygulamasını içermektedir. Uygulama, Bluetooth Low Energy (BLE) cihazları ile iletişim kurarak alınan sensör verilerini takip etmek ve kullanıcı arayüzünde göstermek amacını taşımaktadır.

## Özellikler

- BLE cihazlarını tarama ve seçilen cihazlarla bağlantı kurma.
- Alınan sensör verilerini işleme ve kullanıcı arayüzünde gösterme.
- Cihaz bağlantı durumunu izleme ve kesinti sayısını takip etme.
- Seçilen bir referans noktasına göre açı hesaplama.

## Kullanım

1. `Ble_startScanner` fonksiyonu ile BLE cihazlarını tarayın.
2. Listeden bir cihaz seçip `Ble_Connection` fonksiyonu ile bağlantı kurun.
3. Veriler, `Characteristic_ValueChanged` fonksiyonu tarafından alınır ve işlenir.
4. Bağlantıyı sonlandırmak için `Disconnect` fonksiyonunu kullanın.
