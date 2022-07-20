using CacheService;
using CacheService.Communications;
using CacheService.Models;


public class Main
{
    private AsynchronousClient testClient;
    private AsynchronousClient labelerClient;
    private Echo? testEcho;
    private DecisionPoint testDecisionPoint;
    private LabelSender labelSender;
    public Main()
    {
        testClient = new AsynchronousClient("172.17.0.12", 4161);
        labelerClient = new AsynchronousClient("172.17.0.54", 9100);



        //testDecisionPoint = new DecisionPoint("TEST")
        //    .AssignSender(testClient);
        labelSender = new LabelSender()
            .AssignCommandSender(testClient)
            .AssignAuxCommandSender(new AsynchronousClient("127.0.0.1", 11001))
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