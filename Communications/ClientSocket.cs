using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Text;
using System.Collections.Concurrent;

namespace CacheService.Communications
{

    public class AsynchronousClient : ISender
    {
        // ManualResetEvent instances signal completion.  
        private ManualResetEvent connectDone = new ManualResetEvent(false);
        private ManualResetEvent connectFailed = new ManualResetEvent(false);
        private ManualResetEvent sendDone = new ManualResetEvent(false);
        private ManualResetEvent receiveDone = new ManualResetEvent(false);

        private BlockingCollection<Message> mainMessageQueue
         = new BlockingCollection<Message>();
        private Dictionary<string, Subscriber> subscribers = new Dictionary<string, Subscriber>();

        // The response from the remote device.  
        private static string response = string.Empty;
        private string _endString = ">";
        private IPAddress ipAddress { get; set; }
        private IPEndPoint remoteEndPoint { get; set; }
        public Task clientTask { get; set; }
        private Task? sendTask { get; set; }
        private Socket? _socket;
        private CancellationTokenSource cts = new CancellationTokenSource();

        public AsynchronousClient(string ip, int port)
        {
            ipAddress = IPAddress.Parse(ip);
            remoteEndPoint = new IPEndPoint(ipAddress, port);

            clientTask = StartClientTask();
        }

        public Socket CreateNewSocket() {
            Socket s = new(ipAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

            s.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.DontLinger, true);
            //_socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.DontFragment, true);
            s.NoDelay = true; // disable Nagle's algorithm

            return s;
        }

        public Task StartClientTask()
        {
            return Task.Factory.StartNew(StartClient);
        }

        private void SendingTask(RemoteStateObject clientState)
        {
            while (!cts.IsCancellationRequested)
            {
                try
                {
                    var message = mainMessageQueue.Take(cts.Token);
                    SendData(clientState.socket, message.GetString());
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }
        }

        private void StartClient()
        {
            // reset the cancellation token source
            if (cts.IsCancellationRequested) {
                cts.Dispose();
                cts = new CancellationTokenSource();
            }
            // Connect to a remote device.  
            try
            {
                do
                {
                    if (_socket is not null) _socket.Close();

                    _socket = CreateNewSocket();

                    connectDone.Reset();
                    _socket.BeginConnect(remoteEndPoint, new AsyncCallback(ConnectCallback), _socket);
                    connectDone.WaitOne();

                } while (!_socket.Connected & !cts.Token.IsCancellationRequested);


                var clientState = new RemoteStateObject(_socket);


                if (sendTask is not null) sendTask.Dispose();
                sendTask = Task.Factory.StartNew(() => SendingTask(clientState), cts.Token);


                // Receive the response from the remote device.
                // create new state object


                receiveDone.Reset();
                Receive(clientState);
                receiveDone.WaitOne(); // wait for receiving task to finish


                // if the receiving task has finished, the connection must have been ended
                // (disconnected or interrupted by some error)
                cts.Cancel(); // cancel all tasks
                sendTask.Wait(); // wait for sending task to finish

                // Release the socket.  
                _socket.Shutdown(SocketShutdown.Both);
                _socket.Disconnect(true);

                StartClient();

            }

            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        private void ConnectCallback(IAsyncResult ar)
        {
            // Retrieve the socket from the state object.  
            var client = ar.AsyncState as Socket;
            if (client == null) throw new ArgumentNullException(nameof(client));
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
            Log.Information("Socket connected to {0}", client.RemoteEndPoint?.ToString());

            // Signal that the connection has been made.  
            connectDone.Set();
        }

        private void Receive(RemoteStateObject clientState)
        {
            try
            {
                // Begin receiving the data from the remote device.  
                clientState.socket.BeginReceive(clientState.buffer, 0, RemoteStateObject.BufferSize, 0,
                    new AsyncCallback(ReceiveCallback), clientState);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        private void ReceiveCallback(IAsyncResult ar)
        {
            string content = string.Empty;

            // Retrieve the state object and the clientState socket  
            // from the asynchronous state object.  
            RemoteStateObject? state = ar.AsyncState as RemoteStateObject;
            if (state == null) return;

            Socket clientState = state.socket;
            int bytesRead = 0;
            // Read data from the client socket.
            try
            {
                bytesRead = clientState.EndReceive(ar);
                if (bytesRead <= 0)
                {
                    Log.Warning("{0} Disconnected, cancelling...", clientState.RemoteEndPoint);
                    receiveDone.Set();
                }
            }
            catch (SocketException e)
            {
                Console.WriteLine(e.Message);
            }
            if (bytesRead > 0)
            {
                // There  might be more data, so store the data received so far.  
                state.sb.Append(Encoding.ASCII.GetString(state.buffer, 0, bytesRead));
                
                // Check for end-of-transmission tag. If it is not there, read
                // more data.  
                content = state.sb.ToString(); // 
                


                if (content.IndexOf(_endString) > -1) // if the message contains an ending character
                {
                    
                    // Process received message
                    var m = new Message(content); // create message from received data
                    m.recipientId = state.id; // set recipient id 

                    Log.Information("{0} \u001b[30m\u001b[43m >RCV> \u001b[0m '{2}' ({1} bytes)", state.socket.RemoteEndPoint, content.Length, content);
                    // Notify subscribers
                    foreach (var subscriber in subscribers)
                    {
                        subscriber.Value.Notify(m); // notify all subscribers
                    }
                    

                    state.sb.Clear(); // clear the string builder for next message 
                }

                // receive again 
                clientState.BeginReceive(state.buffer, 0, RemoteStateObject.BufferSize, 0,
                new AsyncCallback(ReceiveCallback), state);

            }
        }
        private byte[]? byteData;
        private void SendData(Socket client, String data)
        {
            // Convert the string data to byte data using ASCII encoding.  
            byteData = Encoding.ASCII.GetBytes(data);

            // Begin sending the data to the remote device.  
            client.BeginSend(byteData, 0, byteData.Length, 0,
                new AsyncCallback(SendCallback), client);
            Log.Information("{0} \u001b[30m\u001b[42m <SNT< \u001b[0m '{2}' ({1} bytes)", client.RemoteEndPoint, data.Length, data);
        }

        private void SendCallback(IAsyncResult ar)
        {
            try
            {
                // Retrieve the socket from the state object.  
                if (ar.AsyncState  is null) throw new ArgumentNullException(nameof(ar.AsyncState));

                // Complete sending the data to the remote device.  
                //int bytesSent =
                    ((Socket)ar.AsyncState).EndSend(ar);
                //Console.WriteLine("Sent {0} bytes to server.", bytesSent);

                // Signal that all bytes have been sent.  
                sendDone.Set();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }


        public Subscriber Subscribe(bool receiving)
        {
            if (subscribers == null) subscribers = new Dictionary<string, Subscriber>();
            var newSubscriber = new Subscriber(this);

            subscribers[newSubscriber.guid] = newSubscriber;
            return newSubscriber;
        }

        public void UnSubscribe(Subscriber s)
        {
            if (s is null) throw new ArgumentNullException(nameof(s));
            if (subscribers is null) return;

            subscribers[s.guid].unsubscribed = true;

        }

        public void Send(Message m, string? clientId = null)
        {
            mainMessageQueue.Add(m);
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