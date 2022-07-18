
namespace CacheService.Communications
{
    public class Echo
    {
        private Subscriber? subscriber { get; set; }

        public Echo(ISender parent)
        {
            subscriber = parent.Subscribe(true);
            subscriber.StartReadingMessages((Subscriber s, Message m) =>
            {
                s.Send(m, m.recipientId);
            });
        }
    }
}
