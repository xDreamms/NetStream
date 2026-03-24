using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Threading;
using BTCPayServer.Client.Models;
using MovieCollection.OpenSubtitles.Models;
using NetStream.Annotations;
using Serilog;
using Bitmap = Avalonia.Media.Imaging.Bitmap;

namespace NetStream.Views
{
    public partial class SubPlansPage : UserControl
    {
        private bool showTrial;

        public SubPlansPage()
        {
            InitializeComponent();
        }
        
        public SubPlansPage( bool showTrial)
        {
            InitializeComponent();
            this.showTrial = showTrial;
            Load();
        }

        private MainAccountPage mainAccountPage;
        
        public SubPlansPage( bool showTrial, MainAccountPage mainAccountPage)
        {
            InitializeComponent();
            this.mainAccountPage = mainAccountPage;
            this.showTrial = showTrial;
            Load();
        }

        private async void Load()
        {
            SubPlansDisplay.ItemsSource = FirestoreManager.SubPlans;
            await FirestoreManager.ListenSubPlans(showTrial);
        }

        private async void SubPlansPage_OnUnloaded(object sender, RoutedEventArgs e)
        {
            // await FirestoreManager.lisSubPlans.StopAsync();
        }
        
        DispatcherTimer timer = new DispatcherTimer();
        private string currentInvoiceId;
        private SubPlan currentSubPlan;
        
        private async void BuyButtonPreviewMouseLeftDown(object? sender, RoutedEventArgs routedEventArgs)
        {
            try
            {
                var btn = sender as Button;
                if (btn != null)
                {
                    var selectedPlan = btn.DataContext as SubPlan;
                    if (selectedPlan != null)
                    {
                        currentSubPlan = selectedPlan;

                        if (selectedPlan.PlanName.ToLower().Contains("trial"))
                        {
                            await FirestoreManager.Register(currentSubPlan);
                            MainWindow.Instance.SetContent(MainView.Instance);
                        }
                        else
                        {
                            DialogHost.IsOpen = true;
                            var invoice = await PaymentManager.CreatePayment(selectedPlan);
                            currentInvoiceId = invoice.Id;
                            timer.Interval = TimeSpan.FromSeconds(2);
                            timer.Tick += (sender, e) => TimerOnTick(sender, e, invoice);
                            timer.Start();
                        }
                    }
                }
            }
            catch (Exception exception)
            {
                var errorMessage = $"Error: {exception.Message}{Environment.NewLine}StackTrace: {exception.StackTrace}";
                Log.Error(errorMessage);
            }
        }

        private bool result = false;
        
        private async void TimerOnTick(object sender, EventArgs eventArgs, InvoiceData invoice)
        {
            try
            {
                if (!result)
                {
                    var updated = await PaymentManager.GetPaymentResult(invoice);
                    if (updated.Status == InvoiceStatus.Settled)
                    {
                        timer.Stop();
                        ShowApp();
                        result = true;
                        DialogHost.IsOpen = false;
                        await FirestoreManager.Register(currentSubPlan);
                        MainWindow.Instance.SetContent(MainView.Instance);
                    
                        if (mainAccountPage != null)
                        {
                            await Dispatcher.UIThread.InvokeAsync(async () =>
                            {
                                mainAccountPage.ExpireTextBlock.Text = await FirestoreManager.GetRelativeSubEndTime(FirestoreManager.ExpiryDate);
                            });
                        }
                    }
                }
            }
            catch (Exception e)
            {
                var errorMessage = $"Error: {e.Message}{Environment.NewLine}StackTrace: {e.StackTrace}";
                Log.Error(errorMessage);
            }
        }
        
        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);
        
        private void ShowApp()
        {
            try
            {
                string processName = "NetStream.Desktop";

                Process[] processes = Process.GetProcessesByName(processName);

                if (processes.Length > 0)
                {
                    IntPtr mainWindowHandle = processes[0].MainWindowHandle;
                    SetForegroundWindow(mainWindowHandle);
                }
                else
                {
                    Log.Error("Application not found.");
                }
            }
            catch (Exception e)
            {
                var errorMessage = $"Error: {e.Message}{Environment.NewLine}StackTrace: {e.StackTrace}";
                Log.Error(errorMessage);
            }
        }

        private async void BtnCancelDialog_OnPreviewMouseLeftButtonDown(object? sender, RoutedEventArgs routedEventArgs)
        {
            DialogHost.IsOpen = false;
            await PaymentManager.DeleteInvoice(currentInvoiceId);
        }
        
        private void FrameworkElement_OnLoaded(object sender, RoutedEventArgs e)
        {
            try
            {
                var btn = sender as Button;
                if (btn != null)
                {
                    var subPlan = btn.DataContext as SubPlan;
                    if (subPlan != null && subPlan.PlanName.ToLower().Contains("trial"))
                    {
                        btn.Content = Application.Current.FindResource("Try").ToString();
                    }
                }
            }
            catch (Exception exception)
            {
                var errorMessage = $"Error: {exception.Message}{Environment.NewLine}StackTrace: {exception.StackTrace}";
                Log.Error(errorMessage);
            }
        }
        
        
        private void BtnCancel_OnPreviewMouseLeftButtonDown(object? sender, RoutedEventArgs e)
        {
            MainWindow.Instance.SetContent(MainView.Instance);
        }
    }

    public class SubPlan : INotifyPropertyChanged
    {
        public string Id { get; set; }
        
        public Bitmap PlanImage
        {
            get { return new Bitmap(AssetLoader.Open(new Uri("avares://NetStream/Assets/NetStreamLogo.png"))); }
        }
        
        private string planName;
        public string PlanName
        {
            get
            {
                return planName;
            }
            set
            {
                planName = value;
                OnPropertyChanged("PlanName");
            }
        }

        private string localizedName;
        public string LocalizedName
        {
            get
            {
                return Application.Current.FindResource(PlanName.Replace(" ", "")).ToString();
            }
            set
            {
                localizedName = value;
                OnPropertyChanged("LocalizedName");
            }
        }

        private decimal planPrice;
        public decimal PlanPrice
        {
            get
            {
                return planPrice;
            }
            set
            {
                planPrice = value;
                OnPropertyChanged("PlanPrice");
            }
        }
        
        private string planPriceString;
        public string PlanPriceString
        {
            get
            {
                return planPriceString;
            }
            set
            {
                planPriceString = value;
                OnPropertyChanged("PlanPriceString");
            }
        }
        
        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
} 