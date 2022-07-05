using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Text;
using System.Collections.Concurrent;

namespace CacheService.Communications
{

    //// State object for receiving data from remote device.  
    //public class StateObject
    //{
    //    // Client socket.  
    //    public Socket workSocket = null;
    //    // Size of receive buffer.  
    //    public const int BufferSize = 256;
    //    // Receive buffer.  
    //    public byte[] buffer = new byte[BufferSize];
    //    // Received data string.  
    //    public StringBuilder sb = new StringBuilder();
    //}

    public class AsynchronousClient : ISender
    {
        // ManualResetEvent instances signal completion.  
        private ManualResetEvent connectDone = new ManualResetEvent(false);
        private ManualResetEvent connectFailed = new ManualResetEvent(false);
        private ManualResetEvent sendDone = new ManualResetEvent(false);
        private ManualResetEvent receiveDone = new ManualResetEvent(false);

        private BlockingCollection<Message> SendQueue = new BlockingCollection<Message>();

        // The response from the remote device.  
        private static string response = string.Empty;

        private IPAddress ipAddress { get; set; }
        private IPEndPoint remoteEndPoint { get; set; }
        public Task clientTask { get; set; }

        public AsynchronousClient(string ip, int port)
        {
            ipAddress = IPAddress.Parse(ip);
            remoteEndPoint = new IPEndPoint(ipAddress, port);
            clientTask = StartClientTask();
        }

        public Task StartClientTask()
        {
            return Task.Factory.StartNew(StartClient);
        }

        private void StartClient()
        {
            // Connect to a remote device.  
            try
            {
                // Establish the remote endpoint for the socket.  
                // The name of the
                // remote device is "host.contoso.com".  
                //IPHostEntry ipHostInfo = Dns.GetHostEntry("host.contoso.com");
                //IPAddress ipAddress = ipHostInfo.AddressList[0];
                //IPEndPoint remoteEP = new IPEndPoint(ipAddress, port);

                // Create a TCP/IP socket.  
                Socket client = new Socket(ipAddress.AddressFamily,
                    SocketType.Stream, ProtocolType.Tcp);
                client.NoDelay = true;

                do
                {
                    Thread.Sleep(1000);
                    // Connect to the remote endpoint.  
                    client.BeginConnect(remoteEndPoint,
                        new AsyncCallback(ConnectCallback), client);
                    connectDone.WaitOne();

                } while (!client.Connected);

                // Send test data to the remote device.  
                Send(client, "This is a test<EOF>");
                sendDone.WaitOne();

                // Receive the response from the remote device.  
                Receive(client);
                receiveDone.WaitOne();

                // Write the response to the console.  
                Console.WriteLine("Response received : {0}", response);

                // Release the socket.  
                client.Shutdown(SocketShutdown.Both);
                client.Close();



            }

            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        private void ConnectCallback(IAsyncResult ar)
        {

            // Retrieve the socket from the state object.  
            Socket client = (Socket)ar.AsyncState;
            try
            {
                // Complete the connection.  
                client.EndConnect(ar);
            }
            catch (SocketException)
            {
                connectDone.Set();
                return;
            }
            Console.WriteLine("Socket connected to {0}",
                client.RemoteEndPoint.ToString());

            // Signal that the connection has been made.  
            connectDone.Set();
        }

        private void Receive(Socket client)
        {
            try
            {
                // Create the state object.  
                RemoteStateObject clientState = new RemoteStateObject(client);

                // Begin receiving the data from the remote device.  
                client.BeginReceive(clientState.buffer, 0, RemoteStateObject.BufferSize, 0,
                    new AsyncCallback(ReceiveCallback), clientState);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        private void ReceiveCallback(IAsyncResult ar)
        {
            try
            {
                // Retrieve the state object and the client socket
                // from the asynchronous state object.  
                if (ar.AsyncState == null) return;
                RemoteStateObject clientState = (RemoteStateObject)ar.AsyncState;
                Socket client = clientState.socket;

                // Read data from the remote device.  
                int bytesRead = client.EndReceive(ar);

                if (bytesRead > 0)
                {
                    // There might be more data, so store the data received so far.  
                    clientState.sb.Append(Encoding.ASCII.GetString(clientState.buffer, 0, bytesRead));

                    // Get the rest of the data.  
                    client.BeginReceive(clientState.buffer, 0, RemoteStateObject.BufferSize, 0,
                        new AsyncCallback(ReceiveCallback), clientState);
                }
                else
                {
                    // All the data has arrived; put it in response.  
                    if (clientState.sb.Length > 1)
                    {
                        response = clientState.sb.ToString();
                    }
                    // Signal that all bytes have been received.  
                    receiveDone.Set();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        private void Send(Socket client, String data)
        {
            // Convert the string data to byte data using ASCII encoding.  
            byte[] byteData = Encoding.ASCII.GetBytes(data);

            // Begin sending the data to the remote device.  
            client.BeginSend(byteData, 0, byteData.Length, 0,
                new AsyncCallback(SendCallback), client);
        }

        private void SendCallback(IAsyncResult ar)
        {
            try
            {
                // Retrieve the socket from the state object.  
                Socket client = (Socket)ar.AsyncState;

                // Complete sending the data to the remote device.  
                int bytesSent = client.EndSend(ar);
                Console.WriteLine("Sent {0} bytes to server.", bytesSent);

                // Signal that all bytes have been sent.  
                sendDone.Set();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        private Dictionary<string, Subscriber> subscribers = new Dictionary<string, Subscriber>();
        private BlockingCollection<Message> mainSendMessageQueue = new();
        public Subscriber Subscribe(bool receiving)
        {
            if (subscribers == null) subscribers = new Dictionary<string, Subscriber>();
            var newSubscriber = new Subscriber(this);

            subscribers[newSubscriber.guid] = newSubscriber;
            return newSubscriber;
        }

        public void UnSubscribe(Subscriber s)
        {
            if (s == null) throw new ArgumentNullException(nameof(s));
            if (subscribers == null) return;

            subscribers[s.guid].unsubscribed = true;

        }

        public void Send(Message m, string? clientId = null)
        {
            if (clientId == null) {
                mainSendMessageQueue.Add(m);
                return;
            };

        }

        public void Send(Message[] m, string? clientId = null)
        {
            foreach (var message in m)
            {
                Send(message, clientId);
            }
        }
    }
}