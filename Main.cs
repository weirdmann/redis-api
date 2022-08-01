using CacheService;
using CacheService.Communications;
using CacheService.Hubs;
using CacheService.Models;
using Microsoft.AspNetCore.SignalR;

public class Main
{
    public AsynchronousClient signalTestClient;
    private AsynchronousClient testClient;
    private AsynchronousClient labelerClient;
    private Echo? testEcho;
    private DecisionPoint testDecisionPoint;
    private LabelSender labelSender;
    private Dictionary<string, List<AsynchronousClient>> scannerClients = new()
    {
        ["172.17.0.68"] = new(),
        ["172.17.0.69"] = new(),
        ["172.17.0.70"] = new(),
        ["172.17.0.71"] = new(),
        ["172.17.0.72"] = new()
    };

    private IndexHub _iHub;
    

    private List<Subscriber> subs = new();
    public static Action<Subscriber, Message> OnScannerMessage(string ip, int port)
    {
        return (sub, msg) =>
        {
            using var file = File.Open(@".\scanners.json", FileMode.Append);
            file.Write(System.Text.Encoding.ASCII.GetBytes( ip + ":" + port + "\",\n"));
        };
        
    }
    
    public Main(IHubContext<IndexHub> _iHub)
    {
        {
            using var file = File.Open(@".\scanners.json", FileMode.OpenOrCreate);
        }
        
        foreach (var s in scannerClients)
        {
            for (int i = 2001; i <= 2008; i++)
            {
                Log.Information("Initializing {0}:{1}", s.Key, i);
                var c = new AsynchronousClient(s.Key, i);

                c.Subscribe(true).StartReadingMessages(OnScannerMessage(s.Key, i));
                s.Value.Add(c);
            }
        }


        testClient = new AsynchronousClient("172.17.0.12", 4161, endstring: ">");
        labelerClient = new AsynchronousClient("172.17.0.54", 9100);
        
        signalTestClient = new("127.0.0.1", 12000, ">");

        var sb = signalTestClient.Subscribe(true);
        sb.StartReadingMessages((s, m) => { _iHub.Clients.All.SendAsync("ReceiveMessage", m.GetString()); Log.Information("kurde"); });
        subs.Add(sb);
        //testDecisionPoint = new DecisionPoint("TEST")
        //    .AssignSender(testClient);
        labelSender = new LabelSender()
            .AssignCommandSender(testClient)
            .AssignAuxCommandSender(new AsynchronousClient("127.0.0.1", 11001, endstring: ">"))
            .AssignLabelerSender(labelerClient);





        //var mockS = mock.Subscribe(false);
        //ManualResetEvent stop = new ManualResetEvent(true);
        //mock.StartListeningThread();
        //mockS.StartReadingMessages((s, m) =>
        //{
        //    if (m.GetString() == "stop") { stop.Set(); s.Send(new Message(m, "stopped"), m.recipientId); }
        //    if (m.GetString() == "start") { stop.Reset(); s.Send(new Message(m, "started"), m.recipientId); }
        //});
        //Task.Factory.StartNew(() =>
        //{
        //    Thread.Sleep(5000);
        //    while (true)
        //    {
        //        stop.WaitOne();
        //        //Thread.Sleep(10);
        //        Log.Information("pinkg");
        //        mockS.Send(new Message(new Telegram54()
        //            .Type(Type54.SOK)
        //            .SequenceNo(123)
        //            .Build()
        //            .GetBytes()
        //            ), mock.clientDictionary.Last(x => x.Key is not null).Key
        //            );
        //    }
        //});
    }
}