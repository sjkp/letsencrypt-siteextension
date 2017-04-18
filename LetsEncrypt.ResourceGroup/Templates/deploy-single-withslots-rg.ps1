
Select-AzureRmSubscription -SubscriptionId 3f09c367-93e0-4b61-bbe5-dcb5c686bf8a ###Pay-As-You-Go

$appPlanRgName = "LetsEncrypt-SiteExtension2"
$webAppRgName = "sjkp.letsencrypt"
$loc = "WestEurope"

New-AzureRmResourceGroup -Name $appPlanRgName -Location $loc

New-AzureRmResourceGroupDeployment -Name "Test" -ResourceGroupName $appPlanRgName -TemplateParameterFile .\azuredeploy.parameters.local.json -TemplateFile .\azuredeploy.slots.json

