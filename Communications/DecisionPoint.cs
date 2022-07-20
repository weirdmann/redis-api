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
                if (!t.Fields["type"].ToString().Equals(Telegram54.TelegramTypeStrings[Type54.WDG])) return (s, m) => { };
                var nt = new Telegram54(t).Type(Type54.WDGA).Build();

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
            var telegram = Telegram54.Parse.New(message.GetString());
            if (telegram is null) return;

            _watchdogResponder.HandleTelegram(telegram).DynamicInvoke(s, message);
        }
    }




    class LabelSender
    {
        private Subscriber? labelerSubscriber;
        private Subscriber? commandSubscriber;
        public string[] labelPaths;
        public Dictionary<string, byte[]?> labels;
        public LabelSender()
        {
            labelPaths = Directory.GetFiles(@"X:\Projekty\Euronet\etykiety");
            labels = new();
            //labels.Add(null, new byte[0]);
            foreach (var filepath in labelPaths)
            {
                Log.Information("Label found: {0}", filepath);
                try
                {
                    labels.Add(filepath, File.ReadAllBytes(filepath));
                }
                catch (Exception e)
                {
                    Log.Error(e, "Error while reading label {0}", filepath);
                    labels.Add(filepath, null);
                }
                Log.Information("Label loaded: {0}", filepath);
            }
        }

        public LabelSender AssignCommandSender(ISender sender)
        {
            commandSubscriber = sender.Subscribe(true);
            commandSubscriber.StartReadingMessages(OnCommandMessage);
            return this;
        }

        public LabelSender AssignAuxCommandSender(ISender sender)
        {
            sender.Subscribe(true).StartReadingMessages(OnCommandMessage);
            return this;
        }

        public LabelSender AssignLabelerSender(ISender sender)
        {
            labelerSubscriber = sender.Subscribe(false);
            return this;
        }

        private void OnCommandMessage(Subscriber s, Message m)
        {
            if (labelerSubscriber is null) return;

            var telegram = Telegram54.Parse.New(m.GetString());
            if (telegram is null) return;

            if (telegram.TypeValue != Type54.SOK) return;
            if (telegram.Addr1Value != "EAN " & telegram.Addr2Value != "  OK") return;
            var labelToSend = labelPaths[telegram.SequenceNoValue % labelPaths.Length];

            s.Send(
                new Message(
                    new Telegram54()
                    .Type("LBL ")
                    .SequenceNo(telegram.SequenceNoValue)
                    .Barcode(labelToSend.Length > 32 ? labelToSend.Substring(labelToSend.Length - 32) : labelToSend)
                    .Addr2((telegram.SequenceNoValue % labelPaths.Length).ToString())
                    .Build()
                    .GetString()
                    )
                );

            labelerSubscriber.Send(new Message(labels[labelToSend] ?? new byte[0]));
            
            //if (m.getstring().equals("<label>", stringcomparison.invariantcultureignorecase))
            //{
            //    var label = getlabel();
            //    s.send(new message(m, "sent: " + label.key));
            //    labelersubscriber.send(new message(label.value));
            //}
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