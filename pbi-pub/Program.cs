﻿using Microsoft.PowerBI.Api.V2;
using Microsoft.PowerBI.Api.V2.Models;
using Microsoft.Rest;
using System.IO;
using System.Linq;

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

            var token = AuthenticationHelper.GetAccessToken(clientId, clientSecret, username, password);

            var tokenCredentials = new TokenCredentials(token, "Bearer");
            Group group;
            using (var client = new PowerBIClient(tokenCredentials))
            {
                var groups = client.Groups.GetGroups().Value;
                var query = from g in groups
                            where g.Name == groupName
                            select g;
                group = query.FirstOrDefault();
                if (group == null)
                {
                    group = client.Groups.CreateGroupAsync(new GroupCreationRequest { Name = groupName }).Result;
                }

                var fileName = Path.GetFileName(path);
                FileStream fileStream = File.OpenRead(path);
                HttpOperationResponse<Import> response;
                try
                {
                    response = client.Imports.PostImportFileWithHttpMessage(group.Id, fileStream, fileName, "Overwrite").Result;
                }
                catch (System.Exception)
                {
                    fileStream = File.OpenRead(path);
                    response = client.Imports.PostImportFileWithHttpMessage(group.Id, fileStream, fileName).Result;
                }
            }
        }
    }
}