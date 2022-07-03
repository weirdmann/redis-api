



using System.Collections.Concurrent;

namespace CacheService.Communications
{
    public class Echo
    {
        private Subscriber? subscriber { get; set; }

        public Echo(ISender parent)
        {
            subscriber = parent.Subscribe(true);
            subscriber.StartReadingMessages((Message m) =>
            {
                subscriber.Send(m, m.recipientId);
            });
        }

        

    }
}
