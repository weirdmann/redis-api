using CacheService;
using CacheService.Communications;
using CacheService.Models;


public class Main
{
    private AsynchronousClient testClient;
    private AsynchronousClient labelerClient;
    private Echo? testEcho;
    private DecisionPoint? testDecisionPoint;
    private LabelSender labelSender;
    public Main()
    {
        testClient = new AsynchronousClient("172.17.0.12", 4161);
        labelerClient = new AsynchronousClient("172.17.0.54", 9100);



        //testDecisionPoint = new DecisionPoint("TEST")
        //    .AssignSender(testClient);

        var labelSenderClient = new AsynchronousClient("127.0.0.1", 11001);
        labelSender = new LabelSender()
            .AssignCommandSender(testClient)
            .AssignAuxCommandSender(labelSenderClient)
            .AssignLabelerSender(labelerClient);




        var mockServer = new AsynchronousSocketListener("127.0.0.1", 11001);
        mockServer.StartListeningThread();
        var mockS = mockServer.Subscribe(false);
        //ManualResetEvent stop = new ManualResetEvent(true);
        //mock.StartListeningThread();
        //mockS.StartReadingMessages((s, m) =>
        //{
        //    if (m.GetString() == "stop") { stop.Set(); s.Send(new Message(m, "stopped"), m.recipientId); }
        //    if (m.GetString() == "start") { stop.Reset(); s.Send(new Message(m, "started"), m.recipientId); }
        //});
        Task.Factory.StartNew(() =>
        {
            Log.Information("Starting test sender task");
            Thread.Sleep(5000);
            Log.Information("Started");
            while (true)
            {
                //stop.WaitOne();
                Thread.Sleep(10);
                Log.Information("pinkg");
                mockS.Send(new Message(new Telegram54()
                    .Type(Type54.SOK)
                    .Addr1("EAN ").Addr2("  OK")
                    .SequenceNo(123)
                    .Build()
                    .GetBytes())) ;
            }
        });
    }
}