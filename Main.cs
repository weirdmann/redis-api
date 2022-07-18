using CacheService;
using CacheService.Communications;
using CacheService.Models;


public class Main {
    AsynchronousClient testClient;
    AsynchronousClient labelerClient;
    Echo? testEcho;
    DecisionPoint testDecisionPoint;
    LabelSender labelSender;
    public Main()
    {
        testClient = new AsynchronousClient("127.0.0.1", 11001);
        labelerClient = new AsynchronousClient("172.17.0.54", 9100);

        testDecisionPoint = new DecisionPoint("TEST")
            .AssignSender(testClient);
        labelSender = new LabelSender()
            .AssignCommandSender(testClient)
            .AssignLabelerSender(labelerClient);
        
    }
}