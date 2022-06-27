
using System.Threading;
using System.Threading.Tasks;


namespace CacheService.Models
{
    public interface IStrategy
    {
        public Thread ExecuteForked();
        public void ThreadExecute();
        public void Execute();
    }
    public static class Strategist
    {
        public static void Do(IStrategy strategy)
        {
            strategy.Execute();
        }
        public static void Then(IStrategy strategy)
        {
            Do(strategy);
        }

        public static Thread WaitAndThen(Thread thread, IStrategy strategy)
        { 

            void ThreadStart() { thread.Join();  strategy.ThreadExecute(); }
            var new_thread = new Thread(ThreadStart);
            new_thread.Start(); 
            return new_thread;
        }

        public static Thread Fork(IStrategy strategy)
        {
            return strategy.ExecuteForked();
            //return this;
        }
    }

    public abstract class Strategy : IStrategy
    {
        private Thread? thread;
        
        public abstract void Execute();
        public abstract void ThreadExecute();

        public Thread ExecuteForked()
        {
            if (thread == null)
            {
                thread = new Thread(new ThreadStart(ThreadExecute));
                thread.Start();
            }
            return thread;
        }
    }

    public class Parse : Strategy
    {
        private TelegramBuilder telegram;
        public Parse(TelegramBuilder t)
        {
            telegram = t;
        }
        public override void Execute()
        {
            Console.WriteLine(telegram.GetString());
        }

        public override void ThreadExecute()
        {
            //Thread.Sleep(5000);
            Execute();
        }
    }
}


