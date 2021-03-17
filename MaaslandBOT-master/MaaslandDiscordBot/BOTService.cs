namespace MaaslandDiscordBot
{
    using System.ServiceProcess;
    using System.Timers;
    using MaaslandDiscordBot.Extensions;

    public class BOTService : ServiceBase
    {
        public static Timer Timer = new Timer();

        public static Program Program { get; set; }

        protected override void OnStart(string[] args)
        {
            Timer.Elapsed += OnElapsedTime;
            Timer.Interval = 1000;
            Timer.Enabled = true;
        }

        public static void OnElapsedTime(object source, ElapsedEventArgs e)
        {
            if (Program.IsNullOrDefault())
            {
                Program = new Program();
                var start = Program.Start();
                var awaiter = start.GetAwaiter();

                awaiter.GetResult();
            }
        }
    }
}
