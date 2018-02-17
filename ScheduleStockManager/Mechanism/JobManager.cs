using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace ScheduleStockManager.Mechanism
{
    public class JobManager
    {

        private LogWriter _logger = new LogWriter();

        public static bool IsRealClass(Type testType)
        {
            return testType.IsAbstract == false
                   && testType.IsGenericTypeDefinition == false
                   && testType.IsInterface == false;
        }

        public void ExecuteAllJobs()
        {
            Console.WriteLine("Begin Method");
            _logger.LogWrite("Begin Method");

            try
            {
                var jobs = GetAllTypesImplementingInterface(typeof(Job));
                if (jobs != null && jobs.Any())
                {
                    foreach (var job in jobs)
                    {
                        if (IsRealClass(job))
                        {
                            try
                            {
                                var instanceJob = (Job)Activator.CreateInstance(job);
                                Console.WriteLine($"The Job has been instantiated successfully.");
                                _logger.LogWrite("The Job has been instantiated successfully.");
                                var thread = new Thread(instanceJob.ExecuteJob);
                                thread.Start();
                                Console.WriteLine($"The Job generate has its thread started successfully.");
                                _logger.LogWrite("The Job generate has its thread started successfully.");
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"The Job \"{job.Name}\" could not be instantiated or executed.", ex);
                                _logger.LogWrite($"The Job \"{job.Name}\" could not be instantiated or executed. /n" + ex);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("An error has occured while instantiating or executing Jobs for the Scheduler Framework.", ex);
                _logger.LogWrite("An error has occured while instantiating or executing Jobs for the Scheduler Framework. /n" + ex);
            }

            Console.WriteLine("End Method");
            _logger.LogWrite("End Method");
        }

        private static IEnumerable<Type> GetAllTypesImplementingInterface(Type desiredType)
        {
            return AppDomain.CurrentDomain.GetAssemblies().SelectMany(assembly => assembly.GetTypes())
                .Where(desiredType.IsAssignableFrom);
        }
    }
}
