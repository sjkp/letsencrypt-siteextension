using LetsEncrypt.SiteExtension.Core;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Timers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
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

        public override DateTime GetNextOccurrence(DateTime now)
        {
            return now.AddMonths(1);
        }
    }

    public class Functions
    {       
        public static void SetupHostNameAndCertificate([TimerTrigger(typeof(MonthlySchedule), RunOnStartup = true)] TimerInfo timerInfo, [Blob("letsencrypt/firstrun.job")] string input, [Blob("letsencrypt/firstrun.job")] out string output)
        {
            Console.WriteLine("Starting setup hostname and certificate");
            if (string.IsNullOrEmpty(input))
            {
                new CertificateManager().SetupHostnameAndCertificate();
                output = DateTime.UtcNow.ToString();
            }
            else
            {
                output = input;
            }            
            Console.WriteLine("Completed setup hostname and certificate");
        }

        public static void RenewCertificate([TimerTrigger(typeof(MyDailySchedule), RunOnStartup = true)] TimerInfo timerInfo)
        {
            Console.WriteLine("Renew certificate");
            new CertificateManager().RenewCertificate();
            Console.WriteLine("Completed renew certificate");
        }        

    }

}