using System;
using System.Collections.Generic;
using System.Windows;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace SignalRClientApp
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private HubConnection connection;

        public MainWindow()
        {
            InitializeComponent();
        }

        private async void ConnectButton_Click(object sender, RoutedEventArgs e)
        {
            var endpoint = endpointValue.Text.Trim();
            var authToken = authTokenValue.Text.Trim();

            if (this.connection == null)
            {
                WriteLogOuput("Connecting...");

                this.connection = BuildHubConnection(endpoint, authToken);

                ConfigureResponseSubscription();

                try
                {
                    await this.connection.StartAsync();
                    WriteLogOuput("Connected succesfully!");
                }
                catch (Exception ex)
                {
                    WriteLogOuput(ex.Message);
                }
            }
        }

        private async void DisconnectButton_Click(object sender, RoutedEventArgs e)
        {
            if (this.connection != null)
            {
                WriteLogOuput("Disconnecting...");

                await connection.StopAsync().ConfigureAwait(false);
                await connection.DisposeAsync().ConfigureAwait(false);
                connection = null;

                WriteLogOuput("Disconnected.");
            }
        }

        private async void SendRequestButton_Click(object sender, RoutedEventArgs e)
        {
            if (this.connection == null)
            {
                WriteLogOuput("Please connect to an endpoint before sending request.");
                return;
            }

            var requestMethod = requestMethodValue.Text.Trim();

            if (string.IsNullOrWhiteSpace(requestMethod))
            {
                WriteLogOuput("Please specify request method.");
                return;
            }

            var request = GetRequestObject();

            try
            {
                WriteLogOuput("Sending request..");
                SetResponseOutput(string.Empty);
                await connection.InvokeAsync(requestMethod, request).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                WriteLogOuput(ex.Message);
            }
        }

        private HubConnection BuildHubConnection(string endpoint, string authToken)
        {
            var connectionBuilder = new HubConnectionBuilder()
                .WithUrl(endpoint, options =>
                {
                    options.Headers.Add("Auth-Token", authToken);
                });

            if(GetSelectedProtocol() == Protocol.MessagePack)
            {
                connectionBuilder.AddMessagePackProtocol();
            }

            return connectionBuilder.Build();
        }

        private void ConfigureResponseSubscription()
        {
            connection.On<object>("OnEmailResponse", ReceiveResponse);
            connection.On<object>("OnPhoneResponse", ReceiveResponse);
            connection.On<object>("OnEnrichResponse", ReceiveResponse);
        }

        private void WriteLogOuput(string message)
        {
            this.Dispatcher.Invoke(() =>
            {
                var getCurrentTime = DateTime.Now.ToString("HH:mm:ss");
                logsOuput.AppendText($"{getCurrentTime}: {message}{Environment.NewLine}");
            });
        }

        private void SetResponseOutput(string output)
        {
            this.Dispatcher.Invoke(() => 
            {
                responseValue.Text = output;
            });
        }

        private void ReceiveResponse(object response)
        {
            WriteLogOuput("Response incoming..");

            var selectedProtocol = GetSelectedProtocol();

            if (selectedProtocol == Protocol.JSON)
            {
                SetResponseOutput(response.ToString());
            }
            else if(selectedProtocol == Protocol.MessagePack)
            {
                SetResponseOutput(JsonConvert.SerializeObject(response, Formatting.Indented));
            }
        }

        private Protocol GetSelectedProtocol()
        {
            if(messagePackProtocol.Dispatcher.Invoke(() => messagePackProtocol.IsChecked == true))
            {
                return Protocol.MessagePack;
            }

            return Protocol.JSON;
        }

        private object GetRequestObject()
        {
            var selectedProtocol = GetSelectedProtocol();

            var requestText = requestValue.Text;

            if (selectedProtocol == Protocol.JSON)
            {
                return JsonConvert.DeserializeObject(requestText);
            }
            else if (selectedProtocol == Protocol.MessagePack)
            {
                var requestBytes = MessagePack.MessagePackSerializer.FromJson(requestText);
                return MessagePack.MessagePackSerializer.Deserialize<Dictionary<string, object>>(requestBytes);
            }

            return null;
        }
    }
}