using CacheService;
using CacheService.Communications;
using CacheService.Models;
namespace CacheService
{
    public class DecisionPoint
    {
        public class WatchdogResponder
        {
            private DecisionPoint _parent;
            public WatchdogResponder(DecisionPoint parent)
            {
                _parent = parent;
                Log.Information("Watchdog responder for DP {0} initialized", _parent._id);
            }

            public Action<Subscriber, Message> HandleTelegram(Telegram54 t)
            {
                if (!t.Fields["type"].ToString().Equals(Telegram54.TelegramTypeStrings[Telegram54.TelegramType.WDG])) return (s, m) => { };
                var nt = new Telegram54(t).Type(Telegram54.TelegramType.WDGA).Build();

                return (s, m) =>
                {
                    s.Send(new Message(nt.GetString()), m.recipientId);
                };
            }
        }

        private WatchdogResponder _watchdogResponder;
        private string _id = Guid.NewGuid().ToString();
        public string Node = String.Empty;
        public DecisionPoint(string node)
        {
            Node = node;
            _watchdogResponder = new WatchdogResponder(this);
            Log.Information("Decision point {0} started", _id);
        }

        public DecisionPoint AssignSender(ISender sender)
        {
            sender.Subscribe(true).StartReadingMessages(OnMessage);
            return this;
        }

        private void OnMessage(Subscriber s, Message message)
        {
            var telegram = Telegram54.Parse(message.GetString());
            if (telegram is null) return;
            
            _watchdogResponder.HandleTelegram(telegram).DynamicInvoke(s, message);
        }
    }




    class LabelSender
    {
        private Subscriber? labelerSubscriber;
        private Subscriber? commandSubscriber;
        public string[] labelPaths;
        public LabelSender()
        {
            labelPaths = Directory.GetFiles(@"X:\Projekty\Euronet\etykiety");
        }

        public LabelSender AssignCommandSender(ISender sender)
        {
            commandSubscriber = sender.Subscribe(true);
            commandSubscriber.StartReadingMessages(OnCommandMessage);
            return this;
        }

        public LabelSender AssignLabelerSender(ISender sender)
        {
            labelerSubscriber = sender.Subscribe(false);
            return this;
        }

        private void OnCommandMessage(Subscriber s, Message m)
        {
            if (m.GetString().Equals("<label>", StringComparison.InvariantCultureIgnoreCase)) {
                var label = GetLabel();
                s.Send(new Message(m, "SENT: " + label.Key));
                labelerSubscriber.Send(new Message(label.Value));
            }
        }

        private KeyValuePair<string, byte[]> GetLabel()
        {
            foreach (var filepath in labelPaths)
            {
                Log.Information("Label found: {0}", filepath);
            }
            var labelpath = labelPaths.Where(x => x.Contains("DHL")).First();
            var labelbytes = File.ReadAllBytes(labelpath);

            return new KeyValuePair<string, byte[]>(labelpath, labelbytes);
        }
        
    }
}