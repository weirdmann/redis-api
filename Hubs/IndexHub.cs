using Microsoft.AspNetCore.SignalR;
using CacheService.Communications;

namespace CacheService.Hubs
{
    public class IndexHub : Hub
    {
        private readonly Main _main;
        private readonly Subscriber subscriber;
        public readonly IHubContext<IndexHub> _hubContext;
        public IndexHub(IHubContext<IndexHub> hubContext, Main main)
        {
            _hubContext = hubContext;
            _main = main;
            subscriber = _main.signalTestClient.Subscribe(true);
            subscriber.StartReadingMessages((s, m) => SendMessage(m.GetString()));
        }

        public void SendMessage(string message)
        {
            Clients.All.SendAsync("ReceiveMessage", message);
            Log.Information("Sent to client : {0}", message);
        }
    }
}
