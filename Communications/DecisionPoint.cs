using CacheService;
using CacheService.Communications;
using CacheService.Models;
namespace CacheService
{
    public class DecisionPoint
    {
        public class WatchdogResponder
        {
            public WatchdogResponder()
            {
            }

            public Action<Subscriber, Message> HandleTelegram(Telegram54 t)
            {
                if (!t.Fields["type"].ToString().Equals(Telegram54.TelegramTypeStrings[Telegram54.TelegramType.WDG])) return (s, m) => { };
                var nt = new Telegram54(t).Type(Telegram54.TelegramType.WDGA).Build();
                
                return (s, m) => s.Send(new Message(nt.GetString()), m.recipientId);
            }
        }

        private readonly Subscriber _subscriber;
        private WatchdogResponder _watchdogResponder;
        public DecisionPoint(ISender sender)
        {
            _watchdogResponder = new WatchdogResponder();
            _subscriber = sender.Subscribe(true);
            _subscriber.StartReadingMessages(OnMessage);
        }

        private void OnMessage(Message message)
        {
            var telegram = Telegram54.Parse(message.GetString());
            if (telegram is null) return;

            _watchdogResponder.HandleTelegram(telegram).DynamicInvoke(_subscriber, message);
        }
    }
}