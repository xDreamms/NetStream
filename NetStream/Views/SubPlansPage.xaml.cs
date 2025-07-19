using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using BTCPayServer.Client.Models;
using HandyControl.Controls;
using HandyControl.Data;
using NetStream.Annotations;
using Serilog;

namespace NetStream.Views
{
    /// <summary>
    /// Interaction logic for SubPlansPage.xaml
    /// </summary>
    public partial class SubPlansPage : HandyControl.Controls.Window
    {
        private bool openMainWindow;
        private bool showTrial;
        public SubPlansPage(bool openMainWindow,bool showTrial)
        {
            InitializeComponent();
            this.openMainWindow = openMainWindow;
            this.showTrial = showTrial;
            Load();
        }

        private MainAccountPage mainAccountPage;
        public SubPlansPage(bool openMainWindow, bool showTrial,MainAccountPage mainAccountPage)
        {
            InitializeComponent();
            this.mainAccountPage = mainAccountPage;
            this.openMainWindow = openMainWindow;
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
        private async void BuyButtonPreviewMouseLeftDown(object sender, MouseButtonEventArgs e)
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
                            if (openMainWindow)
                            {
                                var mainWindow = new MainWindow();
                                mainWindow.Show();
                            }
                            this.Close();
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
        private async void TimerOnTick(object? sender, EventArgs eventArgs, InvoiceData invoice)
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
                        Growl.SuccessGlobal(new GrowlInfo() { Message = App.Current.Resources["PaymentSuccessMessage"].ToString(), WaitTime = 4, StaysOpen = false });

                        if (openMainWindow)
                        {
                            var mainWindow = new MainWindow();
                            mainWindow.Show();
                        }

                        this.Close();
                    
                        if (mainAccountPage != null)
                        {
                            await Application.Current.Dispatcher.BeginInvoke(
                                DispatcherPriority.Background,
                                new Action(async () =>
                                {
                                    mainAccountPage.ExpireTextBlock.Text = await FirestoreManager.GetRelativeSubEndTime(FirestoreManager.ExpiryDate);
                                }));
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
                string processName = "NetStream";

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

        private async void BtnCancelDialog_OnPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            DialogHost.IsOpen = false;
            await PaymentManager.DeleteInvoice(currentInvoiceId);
        }

        private void SubPlansPage_OnMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                this.DragMove();
        }

        private void FrameworkElement_OnLoaded(object sender, RoutedEventArgs e)
        {
            var btn = sender as Button;
            if (btn != null)
            {
                var subPlan = btn.DataContext as SubPlan;
                if (subPlan != null && subPlan.PlanName.ToLower().Contains("trial"))
                {
                    btn.Content = App.Current.Resources["Try"].ToString();
                }
            }
        }
    }

    public class SubPlan : INotifyPropertyChanged
    {
        public string Id { get; set; }
        public BitmapImage PlanImage { get {return  new BitmapImage(new Uri("pack://application:,,,/NetStream;component/logo/NetStreamLogo.png")); } }
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
                return App.Current.Resources[PlanName.Replace(" ", "")].ToString();
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
        public event PropertyChangedEventHandler? PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

}
