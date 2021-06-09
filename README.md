## API Management ARM Template Creator

This is a PowerShell script module to extract API Management to ARM templates, focus is to provide a module for easy deployments.  

### How to use
**Install from PowerShell Gallery**  
`PS> Install-Module -Name APIManagementTemplate`

**Update to latest version from PowerShell Gallery**  
`PS> Update-Module -Name APIManagementTemplate`

Install-Module is part of PowerShellGet which is included on Windows 10 and Windows Server 2016. See [this](https://docs.microsoft.com/en-us/powershell/gallery/installing-psget) link for installation instructions on older platforms.

**Import without installing**  
Clone the project, open, and build.

Open PowerShell and Import the module:

`Import-Module C:\{pathToSolution}\APIManagementARMTemplateCreator\APIManagementTemplate\bin\Debug\APIManagementTemplate.dll`

Run the PowerShell command `Get-APIManagementTemplate`.  You can pipe the output as needed, and recommended you pipe in a token from `armclient`

`armclient token 80d4fe69-xxxx-4dd2-a938-9250f1c8ab03 | Get-APIManagementTemplate -APIManagement MyApiManagementInstance -ResourceGroup myResourceGroup -SubscriptionId 80d4fe69-xxxx-4dd2-a938-9250f1c8ab03 | Out-File C:\template.json`

Example when user is connected to multitenants:

`Get-APIManagementTemplate -APIManagement MyApiManagementInstance -ResourceGroup myResourceGroup -SubscriptionId 80d4fe69-xxxx-4dd2-a938-9250f1c8ab03 -TenantName contoso.onmicrosoft.com`

### Specifications

| Parameter | Description | Required | Default |
| --------- | ---------- | -------| --- |
| APIManagement | Name of the API Management instance| true | |
| ResourceGroup | The name of the Resource Group | true | |
| SubscriptionId | The Subscription id (guid)| true | |
| TenantName | Name of the Tenant i.e. contoso.onmicrosoft.com | false | |
| APIFilters | Filter for what API's to exort i.e: path eq 'api/v1/currencyconverter' or endswith(path,'currencyconverter'). In addition to this, is it also possible to filter on productname i.e.: productname eq 'product-x') | false | |
| ExportAuthorizationServers | Flag indicating if Authorization servers should be exported | false | true | 
| ExportPIManagementInstance | Flag indicating if the API Management instance should be exported | false| true | 
| ExportGroups | Flag indicating if Groups should be exported | false | true |
| ExportProducts | Flag indicating if Products should be exported | false | true |
| ExportTags | Flag indicating if Tags should be exported | false
| ExportSwaggerDefinition | Export the API operations and schemas as a swagger/Open API 2.0 definition. If set to false then the operations and schemas of the API will be included as arm templates  | false | false |
| Token | An AAD Token to access the resources - should not include `Bearer`, only the token | false  |  |
| ParametrizePropertiesOnly | If parameters only should be created for properties such as names of apim services or logic apps and not names of groups, apis or products | false | false |
| ReplaceSetBackendServiceBaseUrlWithProperty | If the base-url of <set-backend-service> with should be replaced with a property instead of a parameter. If this is false you will not be able to set SeparatePolicyFile=true for Write-APIManagementTemplates when you have set-backend-service with base-url-attribute in a policy | false | false |
| FixedServiceNameParameter | True if the parameter for the name of the service should have a fixed name (apimServiceName). Otherwise the parameter name will depend on the name of the service (service_PreDemoTest_name)| false | false |
| CreateApplicationInsightsInstance | If an Application Insights instance should be created when used by a logger. Otherwise you need to provide the instrumentation key of an existing Application Insights instance as a parameter| false | false |
| DebugOutPutFolder | If set, result from rest interface will be saved to this folder | false | |
| ApiVersion | If set, api result will be filtered based on this value i.e: v2 | false | |
| ClaimsDump | A dump of claims piped in from `armclient` - should not be manually set | false | |
| ParameterizeBackendFunctionKey | Set to 'true' if you want the backend function key to be parameterized | false | false |
| SeparatePolicyOutputFolder | Set to an output folder if you want to save the policies in a separate file. The output folder must be relative to the directory _artifactsLocation/_artifactsBlobPrefix. Parameters _artifactsLocation, _artifactsBlobPrefix and _artifactsSASToken are added to the template automatically. This parameter is useful when the policy size exceeds the 16KB limit and you do not want separate ARM templates for all objects. | false | |
| ChainDependencies | Set to 'true' if you get the error "Operation on the API is in progress". This option chains the product apis in order to reduce parallelism | false | false |
  
After extraction a parameters file can be created off the ARMTemplate.

### Multiple small ARM-templates with Write-APIManagementTemplates
Use Write-APIManagementTemplates generate many small ARM templates (as suggested in https://github.com/Azure/azure-api-management-devops-example) instead of one big ARM template.

`armclient token 80d4fe69-xxxx-4dd2-a938-9250f1c8ab03 | Get-APIManagementTemplate -APIManagement MyApiManagementInstance -ResourceGroup myResourceGroup -SubscriptionId 80d4fe69-xxxx-4dd2-a938-9250f1c8ab03 | Write-APIManagementTemplates -OutputDirectory C:\temp\templates -SeparatePolicyFile $true`

### Specifications

| Parameter | Description | Required | Default | 
| --------- | ---------- | -------| --- |
| ApiStandalone | If the APIs should be able to be deployed independently of the rest of the resources | false | true | 
| ListApiInProduct | If true only the names of the API will be added as array parameter | false | false |
| OutputDirectory | The directory where the templates are written to | false | . | 
| SeparatePolicyFile | If the policies should be written to a separate xml file. If set to false then the policies are included as a part of the arm templates | false | false | 
| SeparateSwaggerFile | Swagger/Openapi definitions are written to a separate file. If set to false then the Swagger/Openapi definitions are included as part of the arm templates | false | false | 
| MergeTemplates | If the template already exists in the output directory, it will be merged with the new result. | false | false | 
| GenerateParameterFiles | If parameter files should be generated | false | false | 
| ReplaceListSecretsWithParameter | If the key to an Azure Function should be defined in a parameter instead of calling listsecrets | false | false |
| AlwaysAddPropertiesAndBackend | Always add properties and backend, usefull when having logicapp backends and this service is not generated | false | false |
| MergeTemplateForLogicAppBackendAndProperties | Merge backend and properties if logic app backend exists into the [api-name]/[api-name].template.json | false | false |
| DebugTemplateFile | If set, the input ARM template is written to this file | false | |
| ARMTemplate | The ARM template piped from Get-APIManagementTemplate - should not be manually set | false | |

### Using OpenAPI/Swagger definition files
It is possible to generate ARM templates that use Openapi/Swagger 2.0 definition files to describe the operations and schemas of your APIs.

The advantage is that when there is a change in the backend API you just need to update the OpenAPI/Swagger definition file of your API. No need to modify the ARM templates of the operations and schemas by hand or to change the source API Management instance and then update the ARM templates with this tool.

Use ExportSwaggerDefinition=$true as parameter to Get-APIManagementTemplate and SeparateSwaggerFile=$true as parameter to Write-APIManagementTemplates.

Due to limitations in Azure it is not supported to use ExportSwaggerDefinition=$true without using SeparateSwaggerFile=$true.

`armclient token 80d4fe69-xxxx-4dd2-a938-9250f1c8ab03 | Get-APIManagementTemplate -APIManagement MyApiManagementInstance -ResourceGroup myResourceGroup -SubscriptionId 80d4fe69-xxxx-4dd2-a938-9250f1c8ab03 -ParametrizePropertiesOnly $true -FixedServiceNameParameter $true -CreateApplicationInsightsInstance $true -ReplaceSetBackendServiceBaseUrlWithProperty $true -ExportSwaggerDefinition $true | Write-APIManagementTemplates -OutputDirectory C:\temp\templates -ApiStandalone $true -SeparatePolicyFile $true -SeparateSwaggerFile $true -MergeTemplates $true -GenerateParameterFiles $true  -ReplaceListSecretsWithParameter $true -ListApiInProduct $false`

This module export the host property to your OpenAPI/Swagger definition. However it will be overridden by the serviceUrl property of your *.swagger.template file.
By adding this serviceUri as a parameter this enables you to have different serviceUrls for different environments (for example test and production environments).

If you have a host property in your OpenAPI/Swagger definition it will override the serviceUrl property of your API. 
So if you want to have different serviceUrls for different environments (for example test and production environments) you need to do one of the following two options
* Write a policy that changes the backend url
* Modify the OpenAPI/Swagger definition file so that it contains the correct url in host, basePath and schemes before you deploy it
