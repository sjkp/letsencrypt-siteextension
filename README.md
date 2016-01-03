# Let's Encrypt Site Extension
This Azure Web App Site Extension enables easy install and configuration of [Let's Encrypt](https://letsencrypt.org/) issued SSL certifcates for you custom domain names. 

The site extension requires that you have configured a DNS entry for you custom domain to point to Azure Web App. 
*Note with the fully automated mode, you don't even have to configure the custom domain in Azure the extension will do that for you.*

## Semi-Automated Installation
With the semi-automated installation you manually add the site extension to your web app. Open the extension and manually click through the 3 dialogs. 

Once this process is complete your custom domain for the site is setup with a Let's Encrypt issued SSL certificate. 

## Fully-Automated Installation
If you setup your site with an Azure Resource Manager template, and what to configure SSL as part of this process you are currently out of luck as ARM templates doesn't support custom hostnames. 

The extension removes this limitations by automatially setting up the custom hostname and requesting a Let's Encrypt certificate.

To use the Fully Automated Installtion the following Web App settings must be added. 

| Key |	Value
|-----| ----
| letsencrypt:Tenant |	The tenant name e.g. myazuretenant.onmicrosoft.com
| letsencrypt:SubscriptionId |	The subscription id
| letsencrypt:ClientId	| The value of the clientid of the service principal
| letsencrypt:ClientSecret	| The secret for the service principal
| letsencrypt:ResourceGroupName |	The name of the resource group this web app belongs to
| letsencrypt:AcmeBaseUri |	The url to Let's Encrypt servers e.g. https://acme-v01.api.letsencrypt.org/ or https://acme-staging.api.letsencrypt.org/ (defaults to this)
| letsencrypt:Email	| The Email used for registering with Let's Encrypt
| letsencrypt:Hostnames |	Comma separated list of custom hostnames (externally hosted setup with CNames), that should automatically be configured for the site.

As it can be seen from the list of App Settings a service principal is needed. The extension uses this service principal that must be assigned permissions to the web app, for installing and updating the certificate. 
