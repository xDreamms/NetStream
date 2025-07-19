using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.AccessControl;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Threading;
using BTCPayServer.Client;
using BTCPayServer.Client.Models;
using NetStream.Views;
using Newtonsoft.Json;
using Serilog;
using TMDbLib.Objects.Authentication;

namespace NetStream
{
    public class PaymentManager
    {
        private static BTCPayServer.Client.BTCPayServerClient client;
        private static string URL;
        private static string API_KEY;
        private static string STORE_ID;
        public static void Initialize()
        {
            try
            {
                URL = "";
                API_KEY = "";
                STORE_ID = "";
                client = new BTCPayServerClient(new Uri(URL), API_KEY);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }

        public static async Task<InvoiceData> CreatePayment(SubPlan subPlan)
        {
            var invoice = await client.CreateInvoice(STORE_ID, new CreateInvoiceRequest()
            {
                Amount = subPlan.PlanPrice,
                Currency = "USD"
            });
            Process.Start(new ProcessStartInfo(invoice.CheckoutLink) { UseShellExecute = true });
            return invoice;
        }

        public static async Task<InvoiceData> GetPaymentResult(InvoiceData invoiceData)
        {
            var invoice = await client.GetInvoice(STORE_ID, invoiceData.Id);
            return invoice;
        }

        public static async Task DeleteInvoice(string invoiceId)
        {
            await client.ArchiveInvoice(STORE_ID, invoiceId);
        }
    }
}
