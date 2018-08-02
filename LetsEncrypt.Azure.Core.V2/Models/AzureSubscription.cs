namespace LetsEncrypt.Azure.Core.V2.Models
{
    public class AzureSubscription
    {
        public string Tenant { get; set; }
        public string SubscriptionId { get; set; }

        /// <summary>
        /// Should be AzureGlobalCloud, AzureChinaCloud, AzureUSGovernment or AzureGermanCloud
        /// </summary>
        public string AzureRegion { get; set; }                            
    }
}