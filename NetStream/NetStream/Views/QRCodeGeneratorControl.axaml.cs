using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media.Imaging;
using QRCoder;

namespace NetStream.Views
{
    public partial class QRCodeGeneratorControl : UserControl
    {
        private const int WebSocketPort = 4649;
        private const string WebSocketEndpoint = "/NetStreamSocket";
        private string currentIpAddress = string.Empty;
        
        public QRCodeGeneratorControl()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private async void QRCodeGeneratorControl_Loaded(object sender, RoutedEventArgs e)
        {
            await GenerateQRCodeAsync();
        }
        
        private async void RefreshQRCode_Click(object sender, RoutedEventArgs e)
        {
            await GenerateQRCodeAsync();
        }
        
        private async Task GenerateQRCodeAsync()
        {
            // UI bileşenlerini bul
            var ipAddressText = this.FindControl<TextBlock>("IpAddressText");
            var qrCodeImage = this.FindControl<Image>("QRCodeImage");
            
            // IP adresini asenkron olarak bul
            string ipAddress = await Task.Run(() => GetLocalIPv4Address());
            currentIpAddress = ipAddress;
            
            // WebSocket URL'sini oluştur
            string websocketUrl = $"@ws://{ipAddress}:{WebSocketPort}{WebSocketEndpoint}";
            
            // IP adresi metnini güncelle
            ipAddressText.Text = websocketUrl;
            
            // QR kodunu oluştur
            using (QRCodeGenerator qrGenerator = new QRCodeGenerator())
            {
                QRCodeData qrCodeData = qrGenerator.CreateQrCode(websocketUrl, QRCodeGenerator.ECCLevel.Q);
                PngByteQRCode qrCode = new PngByteQRCode(qrCodeData);
                byte[] qrCodeBytes = qrCode.GetGraphic(20);
                
                // QR kod görüntüsünü oluştur ve ekrana göster
                using (var memoryStream = new MemoryStream(qrCodeBytes))
                {
                    var bitmap = new Bitmap(memoryStream);
                    qrCodeImage.Source = bitmap;
                }
            }
        }
        
        public static string GetLocalIPv4Address()
        {
            try
            {
                // İlk yöntem: Doğrudan internete bağlanarak hangi adaptörün kullanıldığını bul
                using (Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, 0))
                {
                    // Google DNS'e bağlanmaya çalışarak aktif bağlantıyı tespit et
                    socket.Connect("8.8.8.8", 65530);
                    IPEndPoint endPoint = socket.LocalEndPoint as IPEndPoint;
                    if (endPoint != null)
                    {
                        return endPoint.Address.ToString();
                    }
                }
                
                // Yedek yöntem: Tüm ağ adaptörlerini kontrol et ve uygun olanı bul
                var candidateInterfaces = new List<(NetworkInterface Interface, int Metric)>();
                
                foreach (NetworkInterface ni in NetworkInterface.GetAllNetworkInterfaces())
                {
                    // Sadece aktif ve loopback olmayan bağlantıları kontrol et
                    if (ni.OperationalStatus == OperationalStatus.Up && 
                        ni.NetworkInterfaceType != NetworkInterfaceType.Loopback)
                    {
                        IPv4InterfaceProperties ipv4Props = ni.GetIPProperties().GetIPv4Properties();
                        if (ipv4Props != null)
                        {
                            foreach (UnicastIPAddressInformation ip in ni.GetIPProperties().UnicastAddresses)
                            {
                                if (ip.Address.AddressFamily == AddressFamily.InterNetwork)
                                {
                                    // IP adresi ve metrik değerini kaydet
                                    candidateInterfaces.Add((ni, ipv4Props.Index));
                                    break;
                                }
                            }
                        }
                    }
                }
                
                // Metrik değerine göre sırala (düşük metrik değeri = yüksek öncelik)
                candidateInterfaces.Sort((a, b) => a.Metric.CompareTo(b.Metric));
                
                // En düşük metrik değerine sahip arayüzün IP adresini döndür
                if (candidateInterfaces.Count > 0)
                {
                    foreach (UnicastIPAddressInformation ip in candidateInterfaces[0].Interface.GetIPProperties().UnicastAddresses)
                    {
                        if (ip.Address.AddressFamily == AddressFamily.InterNetwork)
                        {
                            return ip.Address.ToString();
                        }
                    }
                }
                
                // Son çare olarak localhost döndür
                return "";
            }
            catch (Exception ex)
            {
                Console.WriteLine($"IP adresi bulunamadı: {ex.Message}");
                return "";
            }
        }
    }
} 