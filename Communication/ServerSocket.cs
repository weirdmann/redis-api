using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Collections;
using System.Collections.Concurrent;
using System.Threading;

namespace CacheService.Communications
{
    public interface Sender
    {
        public Subscriber Subscribe(bool receiving);
        public void UnSubscribe(Subscriber s);
        public void Send(Message m, string? clientId = null);
        public void Send(Message[] m, string? clientId = null);
        
    }

    public class Subscriber
    {
        public readonly string guid = Guid.NewGuid().ToString();
        private ConcurrentQueue<Message>? received;
        private Sender parent;
        public bool unsubscribed = false;

        public Subscriber(Sender parent, bool receiving = false)
        {
            this.parent = parent ?? throw new ArgumentNullException(nameof(parent));

            if (receiving)
            {
                received = new ConcurrentQueue<Message>();
            }
        }

        public void AddToQueue(Message m)
        {
            if (received == null) return; // subscriber doesn't want to receive
            received.Enqueue(m);
        }

        public void Send(Message m, string? clientId = null)
        {
            parent.Send(m, clientId);
        }

        public void Send(Message[] m, string? clientId = null)
        {
            parent.Send(m, clientId);
        }


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
    public class StateObject
    {
        // Size of receive buffer.  
        public const int BufferSize = 1024;

        // Receive buffer.  
        public byte[] buffer = new byte[BufferSize];
        // Received data string.
        public StringBuilder sb = new StringBuilder();

        // Client socket.
        public Socket workSocket = null;
        public string id = Guid.NewGuid().ToString();

        // message queue
        public ConcurrentQueue<Message> messagesToSend = new ConcurrentQueue<Message>();

        public Task? receiveTask, sendTask;

        public CancellationToken cancellationToken = new CancellationToken();
    }

    public class AsynchronousSocketListener : Sender
    {
        // Thread signal.  
        public static ManualResetEvent allDone = new ManualResetEvent(false);

        private IPAddress ipAddress { get; set; }
        private IPEndPoint localEndPoint { get; set; }

        private Dictionary<string, Subscriber>? subscribers;
        public ConcurrentQueue<Message> messagesToSend;

        public AsynchronousSocketListener(string ipaddress, int port)
        {
            ipAddress = IPAddress.Parse(ipaddress);
            localEndPoint = new IPEndPoint(ipAddress, port);
            messagesToSend = new ConcurrentQueue<Message>();
        }


        public Task StartListeningThread()
        {
            return Task.Factory.StartNew(() => StartListening());
        }

        public void StartListening()
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
                    allDone.Reset();

                    // Start an asynchronous socket to listen for connections.  
                    Console.WriteLine("Waiting for a connection..." + localEndPoint.ToString() + "/" + ipAddress.ToString());
                    listener.BeginAccept(
                        new AsyncCallback(AcceptCallback),
                        listener);

                    // Wait until a connection is made before continuing.  
                    allDone.WaitOne();
                }

            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }

            Console.WriteLine("\nPress ENTER to continue...");
            Console.Read();

        }
        
        public void AcceptCallback(IAsyncResult ar)
        {
            // Signal the main thread to continue.  
            allDone.Set();
            
            // Get the socket that handles the client request.  
            Socket listener = (Socket)ar.AsyncState;
            Socket handler = listener.EndAccept(ar);

            // Create the state object.  
            StateObject state = new StateObject();
            Console.WriteLine("Started receiving from client id: " + state.id);
            state.sendTask = Task.Factory.StartNew(() =>  SenderCallback(state) );
            state.workSocket = handler;
            handler.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0,
                new AsyncCallback(ReadCallback), state);
        }

        public static void ReadCallback(IAsyncResult ar)
        {
            string content = string.Empty;

            // Retrieve the state object and the handler socket  
            // from the asynchronous state object.  
            StateObject? state = ar.AsyncState as StateObject;
            if (state == null) return;

            Socket handler = state.workSocket;

            // Read data from the client socket.
            int bytesRead = handler.EndReceive(ar);

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
                    Console.WriteLine("Read {0} bytes from socket. \n Data : {1}",
                        content.Length, content);
                    // Echo the data back to the client.  
                    state.messagesToSend.Enqueue(new Message(content));
                    Send(handler, content);
                }
                else
                {
                    // Not all data received. Get more.  
                    handler.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0,
                    new AsyncCallback(ReadCallback), state);
                }
            }
        }

        private static void Send(Socket handler, String data)
        {
            // Convert the string data to byte data using ASCII encoding.  
            byte[] byteData = Encoding.ASCII.GetBytes(data);

            // Begin sending the data to the remote device.  
            handler.BeginSend(byteData, 0, byteData.Length, 0,
                new AsyncCallback(SendCallback), handler);
        }

        private void SenderCallback(StateObject state)
        {
            try
            {
                Console.WriteLine("Started sending thread for client id: " + state.id);
                Message? message;
                while (!state.cancellationToken.IsCancellationRequested)
                {
                    if (state.messagesToSend.TryDequeue(result: out message))
                    {
                        if (message == null) continue;
                        Send(state.workSocket, message.GetString());
                    }
                }

            } catch
            {

            }
            }

        private static void SendCallback(IAsyncResult ar)
        {
            try
            {
                // Retrieve the socket from the state object.  
                Socket handler = (Socket)ar.AsyncState;

                // Complete sending the data to the remote device.  
                int bytesSent = handler.EndSend(ar);
                Console.WriteLine("Sent {0} bytes to client.", bytesSent);

                handler.Shutdown(SocketShutdown.Both);
                handler.Close();

            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }


        public Subscriber Subscribe(bool receiving)
        {
            if (subscribers == null) subscribers = new Dictionary<string, Subscriber>();
            var newSubscriber = new Subscriber(this, receiving);

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
            throw new NotImplementedException();
        }

        public void Send(Message[] m, string? clientId = null)
        {
            throw new NotImplementedException();
        }
    }
}