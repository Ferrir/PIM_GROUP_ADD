using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace PIM_GROUP
{
    public static class Post_PIM_Group
    {
        public class Role
        {
            public String RoleDefinitionId { get; set; }
            public String ResourceId { get; set; }
        }

        public class Body
        {
            [JsonProperty("roleDefinitionId")]
            public String RoleDefinitionId { get; set; }
            [JsonProperty("resourceId")]
            public String ResourceId { get; set; }
            [JsonProperty("subjectId")]
            public String SubjectId { get; set; }
            [JsonProperty("assignmentState")]
            public String AssignmentState { get; set; } = "Active";
            [JsonProperty("type")]
            public String Type { get; set; } = "UserAdd";
            [JsonProperty("reason")]
            public String Reason { get; set; } = "Development";
            [JsonProperty("ticketNumber")]
            public String TicketNumber { get; set; } = "123456";
            [JsonProperty("ticketSystem")]
            public String TicketSystem { get; set; } = "";
            [JsonProperty("schedule")]
            public Schedule Schedule { get; set; }
            [JsonProperty("linkedEligibleRoleAssignmentId")]
            public String LinkedEligibleRoleAssignmentId { get; set; } = "";
            [JsonProperty("scopedResourceId")]
            public String ScopedResourceId { get; set; } = "";
        }

        public class Schedule
        {
            [JsonProperty("type")]
            public String Type { get; set; } = "Once";
            [JsonProperty("startDateTime")]
            public DateTime? StartDateTime { get; set; }
            [JsonProperty("endDateTime")]
            public DateTime? EndDateTime { get; set; } = null;
            [JsonProperty("duration")]
            public String Duration { get; set; } = "PT600M";
        }

        private static readonly String AZURE_URI = "https://api.azrbac.mspim.azure.com/api/v2/privilegedAccess/aadGroups/roleAssignmentRequests";

        [FunctionName("Post_PIM_Group")]
        public static async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Function, "post", Route = "pim")] HttpRequestMessage req, ExecutionContext executionContext, ILogger log)
        {
            Int64 treeKeys = 0;

            //String token = req.Headers.GetValues("Authorization").FirstOrDefault();
            //String sessionId = req.Headers.GetValues("x-ms-client-session-id").FirstOrDefault();
            //String requestId = req.Headers.GetValues("x-ms-client-request-id").FirstOrDefault();

            String token = String.Empty;
            String sessionId = String.Empty;
            String requestId = String.Empty;

            String payload = await req.Content.ReadAsStringAsync();

            if (String.IsNullOrEmpty(payload))
            {
                throw new Exception("Object payload is empty");
            }//if

            var configs = payload.Split(@"\");

            foreach (var c in configs)
            {
                var config = c.Split(':');

                if (config.Length < 1)
                {
                    continue;
                } //if

                var key = config[0].ToUpper().Replace("\r\n", "");

                if (key.Contains("AUTHORIZATION") || key.Contains("X-MS-CLIENT-SESSION-ID") || key.Contains("X-MS-CLIENT-REQUEST-ID"))
                {
                    treeKeys++;
                    var value = config[1];

                    switch (key)
                    {
                        case String _ when key.Contains("AUTHORIZATION"):
                            token = value.Replace("'", "").Trim();
                            break;
                        case String _ when key.Contains("X-MS-CLIENT-SESSION-ID"):
                            sessionId = value.Replace("'", "").Trim();
                            break;
                        case String _ when key.Contains("X-MS-CLIENT-REQUEST-ID"):
                            requestId = value.Replace("'", "").Trim();
                            break;
                        default:
                            break;
                    }//switch
                }//if

                if (treeKeys == 3)
                {
                    break;
                }//if
            }//foreach

            //Holydays
            List<DateOnly> holyDays = new();

            //Thanksgiving Day
            //holyDays.Add(new DateOnly(2024, 11, 28));

            //Vacations
            //holyDays.Add(new DateOnly(2024, 12, 23));
            //holyDays.Add(new DateOnly(2024, 12, 24));
            //holyDays.Add(new DateOnly(2024, 12, 25));
            //holyDays.Add(new DateOnly(2024, 12, 26));
            //holyDays.Add(new DateOnly(2024, 12, 27));
            //holyDays.Add(new DateOnly(2024, 12, 28));
            //holyDays.Add(new DateOnly(2024, 12, 29));
            //holyDays.Add(new DateOnly(2024, 12, 30));

            // Holy Days
            holyDays.Add(new DateOnly(2025, 05, 26));
            holyDays.Add(new DateOnly(2025, 06, 19));
            holyDays.Add(new DateOnly(2025, 07, 04));
            holyDays.Add(new DateOnly(2025, 09, 01));
            holyDays.Add(new DateOnly(2025, 10, 13));
            holyDays.Add(new DateOnly(2025, 12, 24));
            holyDays.Add(new DateOnly(2025, 12, 25));
            holyDays.Add(new DateOnly(2025, 12, 31));

            DateTime startDate = DateTime.Parse(req.RequestUri.ParseQueryString().Get("startDate"));
            Int64 days = Int64.Parse(req.RequestUri.ParseQueryString().Get("days"));
            Boolean force = Boolean.Parse(req.RequestUri.ParseQueryString().Get("force") ?? "false");

            String SUBJECT_ID = "9c616340-25eb-40ca-bc5d-35c6f28f0288";

            List<Object> result = new();

            //List<Role> lstRoles = new()
            //{
            //    //qd - aadg - database - developer
            //    new Role { RoleDefinitionId = "b7215255-6d04-49aa-ae3c-a06b0b48ad75", ResourceId = "137e8d99-ee38-49eb-acaf-9651fdda78c3" },
            //    //qd - aadg - azure - subscription - contributor
            //    new Role { RoleDefinitionId = "0bb44aa4-3cf9-4505-bf8d-cb0ee46ace18", ResourceId = "7bfe2bb8-6954-4214-be74-d44cd3ea007b" },
            //};

            List<Role> lstRoles = new()
            {
                //qd-aadg-database-developer
                new Role { RoleDefinitionId = "b7215255-6d04-49aa-ae3c-a06b0b48ad75", ResourceId = "137e8d99-ee38-49eb-acaf-9651fdda78c3" },
                //qd-aadg-azure-subscription-contributor
                new Role { RoleDefinitionId = "0bb44aa4-3cf9-4505-bf8d-cb0ee46ace18", ResourceId = "7bfe2bb8-6954-4214-be74-d44cd3ea007b" },
                //qd-aadg-interface-devops
                new Role { RoleDefinitionId = "6a6cbbbc-8f77-487b-8432-d11fff082b55", ResourceId = "a2401277-b469-4a0e-9ad7-a8cd0d82484a" },
            };

            var timeNow = new TimeOnly(DateTime.UtcNow.Hour, DateTime.UtcNow.Minute, DateTime.UtcNow.Second, DateTime.UtcNow.Millisecond);
            var timeStart = new TimeOnly(11, 30, 0, 0);

            for (int i = 0; i < days; i++)
            {
                if (timeNow > timeStart && (startDate - DateTime.Now).Days == 0)
                {
                    startDate = new DateTime(startDate.Year, startDate.Month, startDate.Day, DateTime.UtcNow.Hour, DateTime.UtcNow.Minute, DateTime.UtcNow.Second, DateTimeKind.Utc);
                }
                else
                {
                    startDate = new DateTime(startDate.Year, startDate.Month, startDate.Day, 11, 30, 0, DateTimeKind.Utc);
                }//if

                //Skip days week
                DayOfWeek day = CultureInfo.InvariantCulture.Calendar.GetDayOfWeek(startDate);
                if (!force && (day == DayOfWeek.Saturday || day == DayOfWeek.Sunday))
                {
                    continue;
                }//if

                //Skip holyDays
                if (!force && (holyDays.Where(x => x.Equals(new DateOnly(startDate.Year, startDate.Month, startDate.Day))).Any()))
                {
                    continue;
                }//if

                foreach (Role role in lstRoles)
                {
                    var client = new RestClient(AZURE_URI)
                    {
                        Timeout = -1
                    };

                    var request = new RestRequest(Method.POST);
                    request.AddHeader("x-ms-client-session-id", sessionId);
                    request.AddHeader("Authorization", token);
                    request.AddHeader("Referer", "");
                    request.AddHeader("Accept-Language", "en");
                    request.AddHeader("x-ms-client-request-id", requestId);
                    client.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/130.0.0.0 Safari/537.36";
                    request.AddHeader("Accept", "*/*");
                    request.AddHeader("Content-Type", "application/json");
                    request.AddHeader("x-ms-effective-locale", "en.en-us");

                    Body body = new()
                    {
                        RoleDefinitionId = role.RoleDefinitionId,
                        ResourceId = role.ResourceId,
                        SubjectId = SUBJECT_ID,
                        Schedule = new Schedule()
                        {
                            StartDateTime = startDate
                        }
                    };

                    request.AddJsonBody(JsonConvert.SerializeObject(body));
                    //request.AddParameter("application/json", JsonConvert.SerializeObject(body), ParameterType.RequestBody);
                    IRestResponse response = client.Execute(request);
                    result.Add(response.Content);
                }//foreach

                startDate = startDate.AddDays(1);
            }//for

            return new OkObjectResult(result);
        }
    }
}
