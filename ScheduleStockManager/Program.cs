using ScheduleStockManager.Mechanism;

namespace ScheduleStockManager
{
    class Program
    {
        public static void Main(string[] args)
        {
            Start();
        }

        private static void Start()
        {
            var jobManager = new JobManager();
            jobManager.ExecuteAllJobs();
        }
    }
}
