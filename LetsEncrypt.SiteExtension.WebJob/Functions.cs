using LetsEncrypt.Azure.Core;
using LetsEncrypt.Azure.Core.Models;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Timers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using System.Web;

namespace LetsEncrypt.SiteExtension
{
    public class MyDailySchedule : DailySchedule
    {
        public MyDailySchedule() : base(DateTime.Now.TimeOfDay)
        {

        }
    }

    public class MonthlySchedule : TimerSchedule
    {
        public MonthlySchedule()
        { }

        public override bool AdjustForDST => false;

        public override DateTime GetNextOccurrence(DateTime now)
        {
            return now.AddMonths(1);
        }
    }

    public class Functions
    {
        public static void AddCertificate([TimerTrigger(typeof(MonthlySchedule), RunOnStartup = true)] TimerInfo timerInfo, [Blob("letsencrypt/firstrun.job")] string input, [Blob("letsencrypt/firstrun.job")] out string output)
        {
            Console.WriteLine("Starting add certificate job");
            var environment = new AppSettingsAuthConfig();
            string websiteName = environment.WebAppName + "-" + environment.SiteSlotName + "|";
            if (string.IsNullOrEmpty(input) || !input.Contains(websiteName))
            {
                Console.WriteLine($"First run of add certificate for {websiteName}");
                var status = new CertificateManager(environment).AddCertificate().GetAwaiter().GetResult();
                if (status != null)
                {
                    Console.WriteLine("Certificate was added with success");
                }
                output = string.IsNullOrEmpty(input) ? websiteName : input + websiteName;
            }
            else
            {
                output = input;
            }
            Console.WriteLine("Completed add certificate job");
        }

        public static async Task RenewCertificate([TimerTrigger(typeof(MyDailySchedule), RunOnStartup = true)] TimerInfo timerInfo)
        {
            Console.WriteLine("Renew certificate");
            var config = new AppSettingsAuthConfig();
            var certManager = new CertificateManager(new AppSettingsAuthConfig());

            var renewedCerts = await certManager.RenewCertificate(renewXNumberOfDaysBeforeExpiration: config.RenewXNumberOfDaysBeforeExpiration, throwOnRenewalFailure: config.ThrowOnRenewalFailure);

            Console.WriteLine($"Completed renewal of '{renewedCerts.Count()}' certificates");
        }   
        
        public static async Task Cleanup([TimerTrigger(typeof(MyDailySchedule), RunOnStartup = true)] TimerInfo timerInfo)
        {
            Console.WriteLine("Clean up");
            var res = await new CertificateManager(new AppSettingsAuthConfig()).Cleanup();
            res.ForEach(s => Console.WriteLine($"Removed certificate with thumbprint {s}"));
        }    

    }

}