using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace CacheService.Communications
{
    public interface ISender
    {
        public Subscriber Subscribe(bool receiving);
        public void UnSubscribe(Subscriber s);
        public void Send(Message m, string? clientId = null);
        public void Send(Message[] m, string? clientId = null);

    }

    public class Subscriber
    {
        public readonly string guid = Guid.NewGuid().ToString();
        protected BlockingCollection<Message>? received;
        protected readonly ISender parent;
        public CancellationTokenSource cts = new CancellationTokenSource();
        public bool unsubscribed = false;
        
        public Subscriber(ISender parent)
        {
            Console.WriteLine("new subscriber");
            this.parent = parent ?? throw new ArgumentNullException(nameof(parent));
        }

        public void Notify(Message m) 
        {
            if (received is null) return; // subscriber doesn't want to receive
            if (unsubscribed) return; // subscriber unsubscribed
            received.Add(m);
            Console.WriteLine("Notified:" + m.GetString());
        }

        public void StartReadingMessages(Action<Message> callback)
        {
            received = new BlockingCollection<Message>();
            Task.Factory.StartNew(() =>
            {

                //if (received == null) return;
                try
                {
                    Message m;
                    while (!cts.Token.IsCancellationRequested)
                    {
                        Console.WriteLine("Taking...");
                        m = received.Take(cts.Token);
                        Console.WriteLine("Taken: " + m.GetString());
                        callback(m);
                    }
                }
                catch (OperationCanceledException)
                {

                }

            }, cts.Token);
        }


        public void Send(Message m, string? clientId = null) { parent.Send(m, clientId); }

        public void Send(Message[] m, string? clientId = null) { parent.Send(m, clientId); }



    }

    public class Message
    {
        public byte[] bytes = new byte[1];
        public DateTime timestamp;
        public string recipientId = Guid.NewGuid().ToString();

        public Message(byte[] bytes)
        {
            SetContent(bytes);
            if (bytes == null) bytes = new byte[1];
            timestamp = DateTime.Now;
        }

        public Message(string str)
        {
            SetContent(str);
            if (bytes == null) bytes = new byte[str.Length];
            timestamp = DateTime.Now;
        }

        public Message(Message m, byte[] bytes)
        {
            this.bytes = bytes;
            recipientId = m.recipientId;
        }

        public void SetContent(string src)
        {
            bytes = Encoding.ASCII.GetBytes(src);
        }

        public void SetContent(byte[] src)
        {
            bytes = src;
        }

        public string GetString()
        {
            return Encoding.ASCII.GetString(bytes);
        }

        public byte[] GetBytes()
        {
            return bytes;
        }
    }

    // State object for reading client data asynchronously  
    public class RemoteStateObject
    {
        // Size of receive buffer.  
        public const int BufferSize = 1024;

        // Receive buffer.  
        public byte[] buffer = new byte[BufferSize];
        // Received data string.
        public StringBuilder sb = new StringBuilder();

        // Client socket.
        public Socket socket;
        public string id = Guid.NewGuid().ToString();

        // message queue
        //public ConcurrentQueue<Message> messagesToSend = new ConcurrentQueue<Message>();
        public BlockingCollection<Message> ToSendQueue = new BlockingCollection<Message>();
        public Task? receiveTask, sendTask;

        //public CancellationToken cancellationToken = new CancellationToken();
        public CancellationTokenSource cts = new CancellationTokenSource();
        public RemoteStateObject(Socket socket)
        {
            this.socket = socket ?? throw new ArgumentNullException(nameof(socket));
        }
    }

    public class AsynchronousSocketListener : ISender
    {
        // Thread signal.  
        public static ManualResetEvent clientAccepted = new ManualResetEvent(false);

        private IPAddress ipAddress { get; set; }
        private IPEndPoint localEndPoint { get; set; }

        private Dictionary<string, Subscriber> subscribers = new Dictionary<string, Subscriber>();
        private BlockingCollection<Message> mainSendMessageQueue;
        public ConcurrentDictionary<string, RemoteStateObject> clientDictionary = new ConcurrentDictionary<string, RemoteStateObject>();
        public AsynchronousSocketListener(string ipaddress, int port)
        {
            ipAddress = IPAddress.Parse(ipaddress);
            localEndPoint = new IPEndPoint(ipAddress, port);
            mainSendMessageQueue = new BlockingCollection<Message>();
        }


        public Task StartListeningThread()
        {
            return Task.Factory.StartNew(() => StartAndWaitForClients());
        }

        public void StartAndWaitForClients()
        {
            // Establish the local endpoint for the socket. 
            //IPHostEntry ipHostInfo = Dns.GetHostEntry(Dns.GetHostName());
            //IPAddress ipAddress = IPAddress.Parse("127.0.0.1");
            //IPEndPoint localEndPoint = new IPEndPoint(ipAddress, 11000);

            // Create a TCP/IP socket.  
            Socket listener = new Socket(ipAddress.AddressFamily,
                SocketType.Stream, ProtocolType.Tcp);

            // Bind the socket to the local endpoint and listen for incoming connections.  
            try
            {
                listener.Bind(localEndPoint);
                listener.Listen(100);

                while (true)
                {
                    // Set the event to nonsignaled state.  
                    clientAccepted.Reset();

                    // Start an asynchronous socket to listen for connections.  
                    Console.WriteLine("Waiting for a connection..." + localEndPoint.ToString() + "/" + ipAddress.ToString());
                    listener.BeginAccept(
                        new AsyncCallback(AcceptNewClientCallback),
                        listener);

                    // Wait until a connection is made before continuing.  
                    clientAccepted.WaitOne();
                }

            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }

            Console.WriteLine("\nPress ENTER to continue...");
            Console.Read();

        }

        public void AcceptNewClientCallback(IAsyncResult ar)
        {
            // Signal the main thread to continue.  
            clientAccepted.Set();

            // Get the socket that handles the client request.  
            var listener = ar.AsyncState as Socket;
            if (listener == null) throw new SocketException();
            //Socket clientState = listener.EndAccept(ar);

            // Create the state object.  
            RemoteStateObject newClientState = new RemoteStateObject(listener.EndAccept(ar));
            clientDictionary[newClientState.id] = newClientState;
            Console.WriteLine("Started receiving from client id: " + newClientState.id);
            newClientState.sendTask = Task.Factory.StartNew(() => QueuedSenderThread(newClientState));
            newClientState.socket.BeginReceive(newClientState.buffer, 0, RemoteStateObject.BufferSize, 0,
                new AsyncCallback(ReceiveCallback), newClientState);
        }

        public void ReceiveCallback(IAsyncResult ar)
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
                    Console.WriteLine("Client disconnected, cancelling...");
                    state.cts.Cancel();
                }
            }
            catch (SocketException e)
            {
                Console.WriteLine(e.Message);
            }
            if (bytesRead > 0)
            {
                // There  might be more data, so store the data received so far.  
                state.sb.Append(Encoding.ASCII.GetString(
                    state.buffer, 0, bytesRead));

                // Check for end-of-transmission tag. If it is not there, read
                // more data.  
                content = state.sb.ToString();
                if (content.IndexOf(">") > -1)
                {
                    // All the data has been read from the
                    // client. Display it on the console.  
                    //Console.WriteLine("Read {0} bytes from socket. \n Data : {1}",
                    //    content.Length, content);
                    // Echo the data back to the client.  
                    //state.messagesToSend.Enqueue(new Message(content));
                    var m = new Message(content);
                    m.recipientId = state.id;
                    #region Notify subscribers
                    foreach (var subscriber in subscribers)
                    {
                        subscriber.Value.Notify(m);
                    }
                    #endregion
                    //state.ToSendQueue.Add(new Message(content));
                    state.sb.Clear();
                }
                //else
                {
                    // receive again 
                    clientState.BeginReceive(state.buffer, 0, RemoteStateObject.BufferSize, 0,
                    new AsyncCallback(ReceiveCallback), state);
                }
            }
        }

        private static void SendData(RemoteStateObject clientState, String data)
        {
            // Convert the string data to byte data using ASCII encoding.  
            byte[] byteData = Encoding.ASCII.GetBytes(data);

            // Begin sending the data to the remote device.  
            clientState.socket.BeginSend(byteData, 0, byteData.Length, 0,
                new AsyncCallback(EndSendCallback), clientState);
        }

        private void QueuedSenderThread(RemoteStateObject state)
        {
            Console.WriteLine("Started sending thread for client id: " + state.id);
            Message? message;
            while (!state.cts.Token.IsCancellationRequested)
            {
                try
                {
                    // blocking - wait for a message in the queue
                    message = state.ToSendQueue.Take(state.cts.Token);
                    SendData(state, message.GetString());
                }
                catch (OperationCanceledException e)
                {
                    Console.WriteLine(e.Message);
                    state.socket.Disconnect(false);
                }
                catch (SocketException e)
                {
                    Console.WriteLine(e.Message);
                    state.cts.Cancel();
                    state.socket.Disconnect(false);
                }
            }
        }

        private static void EndSendCallback(IAsyncResult ar)
        {
            // Retrieve the socket from the state object.  
            var clientState = ar.AsyncState as RemoteStateObject;
            if (clientState == null) { throw new ArgumentNullException(nameof(ar)); };

            try
            {
                // Complete sending the data to the remote device.  
                int bytesSent = clientState.socket.EndSend(ar);
                //Console.WriteLine("Sent {0} bytes to client.", bytesSent);
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
            if (s == null) throw new ArgumentNullException(nameof(s));
            if (subscribers == null) return;

            subscribers[s.guid].unsubscribed = true;

        }

        public void Send(Message m, string? clientId = null)
        {
            if (clientId == null) { mainSendMessageQueue.Add(m); return; };

            if (clientDictionary.ContainsKey(clientId))
                clientDictionary[clientId].ToSendQueue.Add(m);

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