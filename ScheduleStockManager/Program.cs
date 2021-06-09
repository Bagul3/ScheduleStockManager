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
            var nonSchedulerJob = new Job();
            nonSchedulerJob.ExecuteJob();
        }
    }
}
