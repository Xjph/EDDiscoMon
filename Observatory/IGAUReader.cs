﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Windows.Forms;

namespace Observatory
{
    static class IGAUReader
    {
        public static void ProcessCodex(CodexEntry codexEntry)
        {
            if (Properties.Observatory.Default.SendToIGAU && codexEntry.VoucherAmount > 0)
            {

                JObject POST_content = JObject.FromObject(
                    new
                    {
                        timestamp = codexEntry.Timestamp.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                        EntryID = codexEntry.EntryID.ToString(),
                        Name = codexEntry.Name
                            .Replace("$", string.Empty)
                            .Replace(";", string.Empty)
                            .Replace("_Name", string.Empty)
                            .ToLower(),
                        Name_Localised = codexEntry.NameLocalised,
                        codexEntry.System,
                        SystemAddress = codexEntry.SystemAddress.ToString(),
                        App_Name = "Elite_Observatory",
                        App_Version = Application.ProductVersion
                    });

                var request = new HttpRequestMessage
                {
                    Method = HttpMethod.Post,
                    RequestUri = new Uri($"https://ddss70885k.execute-api.us-west-1.amazonaws.com/Prod"),
                    Content = new StringContent(POST_content.ToString(Formatting.None))
                };

                QueueRequest(request);

            }
        }

        public static void ProcessOrganic(ScanOrganicEvent scanOrganicEvent)
        {
            if (Properties.Observatory.Default.SendToIGAU)
            {
                JObject POST_content = JObject.FromObject(
                    new
                    {
                        timestamp = scanOrganicEvent.Timestamp.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                        EntryID = "-",
                        Name = scanOrganicEvent.Species
                            .Replace("$", string.Empty)
                            .Replace(";", string.Empty)
                            .Replace("_Name", string.Empty)
                            .ToLower(),
                        Name_Localised = scanOrganicEvent.Species_Localised,
                        System = scanOrganicEvent.CurrentSystem,
                        SystemAddress = scanOrganicEvent.SystemAddress.ToString(),
                        App_Name = "Elite_Observatory",
                        App_Version = Application.ProductVersion
                    });

                var request = new HttpRequestMessage
                {
                    Method = HttpMethod.Post,
                    RequestUri = new Uri($"https://ddss70885k.execute-api.us-west-1.amazonaws.com/Prod"),
                    Content = new StringContent(POST_content.ToString(Formatting.None))
                };

                QueueRequest(request);

            }
        }

        private static List<HttpRequestMessage> RequestQueue;

        private static bool RequestsSending;

        private static int startTicks;

        private static void QueueRequest(HttpRequestMessage request)
        {
            if (RequestQueue == null)
            {
                RequestQueue = new List<HttpRequestMessage>();
            }

            RequestQueue.Add(request);
            SendRequests();
        }

        private static async void SendRequests()
        {
            if (!RequestsSending)
            {
                RequestsSending = true;
                startTicks = Environment.TickCount;
                var mainForm = System.Windows.Forms.Application.OpenForms.OfType<ObservatoryFrm>().First();
                while (RequestQueue.Count > 0)
                {
                    var SendRequestTask = HttpClient.SendRequestAsync(RequestQueue.First());

                    await SendRequestTask;

                    if (!SendRequestTask.Result.IsSuccessStatusCode)
                    {
                        System.Windows.Forms.MessageBox.Show($"HTTP Error: {SendRequestTask.Result.StatusCode.ToString()}\r\nContent: {SendRequestTask.Result.Content}\r\nTransmission disabled.", "Error Sending IGAU Data");
                        RequestQueue.Clear();
                        mainForm.SetStatusText(string.Empty);
                        Properties.Observatory.Default.SendToIGAU = false;
                        Properties.Observatory.Default.Save();
                        break;
                    }

                    RequestQueue.Remove(RequestQueue.First());

                    mainForm.SetStatusText(RequestQueue.Count > 0 ? $"{RequestQueue.Count} pending IGAU transmission{(RequestQueue.Count > 1 ? "s" : "")}.":"");
                    
                }
                RequestsSending = false;
            }
        }
    }
}
