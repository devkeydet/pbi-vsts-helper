using Microsoft.PowerBI.Api.V2;
using Microsoft.PowerBI.Api.V2.Models;
using Microsoft.Rest;
using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;

namespace pbi_pub
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            var command = args[0];
            var path = args[1];
            var groupName = args[2];
            var clientId = args[3];
            var clientSecret = args[4];
            var username = args[5];
            var password = args[6];
            var dataSourceUpdates = args[7]; // Example JSON below
                                             //var dataSourceUpdates =
                                             //    @"{
                                             //      ""updateDetails"":[
                                             //        {
                                             //          ""connectionDetails"":
                                             //          {
                                             //            ""url"": ""https://marcsc-corrmgmt.api.crm.dynamics.com/api/data/v8.2""
                                             //          },
                                             //          ""datasourceSelector"":
                                             //          {
                                             //            ""datasourceType"": ""OData"",
                                             //                ""connectionDetails"": {
                                             //                    ""url"": ""https://cmv9.api.crm.dynamics.com/api/data/v8.2""
                                             //                }
                                             //          }
                                             //        }
                                             //      ]
                                             //    }";

            var token = AuthenticationHelper.GetAccessToken(clientId, clientSecret, username, password);

            var tokenCredentials = new TokenCredentials(token, "Bearer");
            Group group;
            using (var pbiClient = new PowerBIClient(tokenCredentials))
            {
                // Get groups/workspaces
                var groups = pbiClient.Groups.GetGroups().Value;
                var groupsQuery = from g in groups
                                  where g.Name == groupName
                                  select g;
                group = groupsQuery.FirstOrDefault();
                // Create group if it doesn't exist
                if (group == null)
                {
                    group = pbiClient.Groups.CreateGroupAsync(new GroupCreationRequest { Name = groupName }).Result;
                }

                // Import .pbix into group
                var fileName = Path.GetFileName(path);
                var fileStream = File.OpenRead(path);
                HttpOperationResponse<Import> response;
                try
                {
                    response = pbiClient.Imports.PostImportFileWithHttpMessage(group.Id, fileStream, fileName, "Overwrite").Result;
                }
                catch (Exception)
                {
                    fileStream = File.OpenRead(path);
                    response = pbiClient.Imports.PostImportFileWithHttpMessage(group.Id, fileStream, fileName).Result;
                }

                // Get the datasource of the updated .pbix
                var dataSets = pbiClient.Datasets.GetDatasetsInGroupAsync(group.Id).Result;
                var dataSetsQuery = from d in dataSets.Value
                                    where d.Name == fileName.Replace(".pbix", "")
                                    select d;
                var dataSetId = dataSetsQuery.First().Id;

                // Update the data sources so that they point to the right Dynamics instance.
                var content = new StringContent(dataSourceUpdates, Encoding.UTF8, "application/json");
                using (var httpClient = new HttpClient())
                {
                    httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
                    //httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                    var result = httpClient.PostAsync(
                        $"{pbiClient.BaseUri}/v1.0/myorg/groups/{group.Id}/datasets/{dataSetId}/updatedatasources",
                        content).Result;
                    if (!result.IsSuccessStatusCode)
                    {
                        throw new Exception("failed to update the datasource");
                    }
                }

                // TODO: would be great if we could update the data source credentials and then refresh it.
                // Don't see how to do that with the current API:https://msdn.microsoft.com/en-us/library/mt784652.aspx
            }
        }
    }
}