using CacheService;
using CacheService.Communications;
using CacheService.Models;


public class Main {
    AsynchronousClient testClient;
    Echo? testEcho;
    DecisionPoint testDecisionPoint;

    public Main()
    {
        testClient = new AsynchronousClient("127.0.0.1", 11001);
        //testEcho = new Echo(testClient);

        testDecisionPoint = new DecisionPoint("TEST").AssignSender(testClient);

    }
}