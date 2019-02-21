using Microsoft.TeamFoundation.WorkItemTracking.WebApi;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;
using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.WebApi;
using Microsoft.VisualStudio.Services.WebApi.Patch;
using Microsoft.VisualStudio.Services.WebApi.Patch.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace AzureDevopsClient
{
    public partial class Form1 : Form
    {
        VssConnection vssConnection;
        WorkItemTrackingHttpClient witClient;
        WorkItemQueryResult workItemQueryResult;
        int totalThreadCount;

        public const string projectName = "INTCORE3";
        public const string originalEstimate = "Original Estimate";
        public const string remainingWork = "Remaning Work";
        public const string completedWork = "Completed Work";
        public const string iterationPath = "Iteration Path";
        public const string state = "State";
        public const string title = "Title";
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        Dictionary<string, string> fieldReferences = new Dictionary<string, string>();
        List<WorkItemTypeFieldWithReferences> fieldRefs = new List<WorkItemTypeFieldWithReferences>();

        delegate void SetEnabledCallback();
        delegate void UpdateProgressCallback();


        public Form1()
        {
            InitializeComponent();
            InitAzureDevOps();
        }

        private void executeUpdates_Click(object sender, EventArgs e)
        {
            executeUpdates.Enabled = false;

            var t = new Task(() =>
            {
                workItemQueryResult = GetWorkItems().Result;
                totalThreadCount = workItemQueryResult.WorkItems.Count();
            });

            t.Start();
            t.Wait();

            progressBar1.Maximum = totalThreadCount;
            progressBar1.Minimum = 0;

            var t2 = new Task(() =>
            {
                if (workItemQueryResult.WorkItems.Count() != 0)
                {
                    List<int> list = new List<int>();
                    foreach (var item in workItemQueryResult.WorkItems)
                    {
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
                        this.UpdateBugWithChildTasks(item.Id);
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
                    }
                }
            });

            t2.Start();
            t2.Wait();
        }

        public void InitAzureDevOps()
        {
            string text = System.IO.File.ReadAllText("Token.txt");
            string projectUrl = System.IO.File.ReadAllText("ProjectUrl.txt");

            vssConnection = new VssConnection(new Uri(projectUrl), new VssBasicCredential(string.Empty, text));
            witClient = vssConnection.GetClient<WorkItemTrackingHttpClient>();

            var t = Task.Run(() =>
            {
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
                GetFieldReferences();
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
            });
        }

        public async Task GetFieldReferences()
        {
            fieldRefs = await witClient.GetWorkItemTypeFieldsWithReferencesAsync(projectName, "Bug");
            fieldReferences.Add("Id", "System.Id");
            fieldReferences.Add(title, fieldRefs.Single(p => p.Name.Equals("Title")).ReferenceName);
            fieldReferences.Add(state, fieldRefs.Single(p => p.Name.Equals("State")).ReferenceName);
            fieldReferences.Add(originalEstimate, fieldRefs.Single(p => p.Name.Equals("Original Estimate")).ReferenceName);
            fieldReferences.Add(completedWork, fieldRefs.Single(p => p.Name.Equals("Completed Work")).ReferenceName);
            fieldReferences.Add(remainingWork, fieldRefs.Single(p => p.Name.Equals("Remaining Work")).ReferenceName);
            fieldReferences.Add(iterationPath, fieldRefs.Single(p => p.Name.Equals("Iteration Path")).ReferenceName);
            SetEnable();
        }

        private void SetEnable()
        {
            if (executeUpdates.InvokeRequired)
            {
                SetEnabledCallback callback = new SetEnabledCallback(SetEnable);
                this.Invoke(callback);
            }
            else
            {
                executeUpdates.Enabled = true;
            }
        }

        private void UpdateProgress()
        {
            if (progressBar1.InvokeRequired)
            {
                UpdateProgressCallback callback = new UpdateProgressCallback(UpdateProgress);
                this.Invoke(callback);
            }
            else
            {
                progressBar1.Value++;
            }
        }

        public async Task UpdateBugWithChildTasks(int bugId)
        {
            int cumulativeCompletedWork = 0;
            int cumulativeRemainingWork = 0;

            Wiql wiql = new Wiql()
            {
                Query = string.Format("SELECT * FROM WorkItemLinks WHERE [Source].[System.Id] = '{0}' and [Target].[System.WorkItemType] = 'Task' and [Source].[System.WorkItemType] = 'Bug'", bugId.ToString())
            };

            var result = await witClient.QueryByWiqlAsync(wiql);

            if (result.WorkItemRelations.Count() != 0)
            {

                log.Info(string.Format("Update has started for bug: {0}", bugId));

                List<int> list = new List<int>();
                var bug = await this.GetWorkItem(bugId);

                foreach (var item in result.WorkItemRelations)
                {
                    if (item.Source != null && item.Target != null)
                    {
                        var task = await this.GetWorkItem(item.Target.Id);

                        if (!task.Fields[fieldReferences[iterationPath]].ToString().Contains("INTCORE3\\v4.0") &&
                            !task.Fields[fieldReferences[iterationPath]].ToString().Contains("INTCORE3\\v3.2"))
                        {
                            continue;
                        }
                        
                        log.Info(string.Format("Bug: {0} - Task: {1} Completed Work: {2}, Remaining Work: {3}",
                        bugId, item.Target.Id, task.Fields[fieldReferences[completedWork]].ToString(), task.Fields[fieldReferences[remainingWork]].ToString()));

                        var completedRemainingPair = GetSafeCompletedAndRemainig(task.Fields);

                        cumulativeCompletedWork += completedRemainingPair[0];
                        cumulativeRemainingWork += completedRemainingPair[1];
                    }
                }

                await UpdateFieldOfWorkItem(bugId, cumulativeRemainingWork, cumulativeCompletedWork);
            }

            totalThreadCount--;
            UpdateProgress();

            if (totalThreadCount == 0)
            {
                SetEnable();
            }
        }

        public List<int> GetSafeCompletedAndRemainig(IDictionary<string, object> pFields)
        {
            List<int> result = new List<int>();
            var tempCompletedWork = pFields[fieldReferences[completedWork]];
            var tempRemainingWork = pFields[fieldReferences[remainingWork]];
            int incomingCompletedWork;
            int incomingRemainingWork;

            if (tempCompletedWork != null && int.TryParse(tempCompletedWork.ToString(), out incomingCompletedWork))
            {
                result.Add(incomingCompletedWork);
            }
            else
            {
                result.Add(0);

            }

            if (tempRemainingWork != null && int.TryParse(tempRemainingWork.ToString(), out incomingRemainingWork))
            {
                result.Add(incomingRemainingWork);
            }
            else
            {
                result.Add(0);

            }

            return result;
        }

        public async Task UpdateFieldOfWorkItem(int id, int remaining, int completed)
        {

            var oldBug = await this.GetWorkItem(id);

            var completedRemainingPair = GetSafeCompletedAndRemainig(oldBug.Fields);

            if (completedRemainingPair[0] == completed && completedRemainingPair[1] == remaining)
            {
                log.Info(string.Format("No update for Bug: {0}", id));
                return;
            }

            JsonPatchDocument patchDocument = new JsonPatchDocument();

            patchDocument.Add(
                new JsonPatchOperation()
                {
                    Operation = Operation.Add,
                    Path = "/fields/" + fieldReferences[remainingWork],
                    Value = remaining
                }
            );

            patchDocument.Add(
                new JsonPatchOperation()
                {
                    Operation = Operation.Add,
                    Path = "/fields/" + fieldReferences[completedWork],
                    Value = completed
                }
            );

            await witClient.UpdateWorkItemAsync(patchDocument, id);

            log.Info(string.Format("Update Finished for Bug: {0} Completed: {1} Remaining: {2}", id, completed, remaining));

        }

        public async Task<WorkItem> GetWorkItem(int id)
        {
            var workItem = await witClient.GetWorkItemAsync(projectName, id, fieldReferences.Select(p => p.Value), DateTime.Now);

            object outValue;
            if (!workItem.Fields.TryGetValue(fieldReferences[completedWork], out outValue))
            {
                workItem.Fields.Add(fieldReferences[completedWork], 0);
            }

            if (!workItem.Fields.TryGetValue(fieldReferences[remainingWork], out outValue))
            {
                workItem.Fields.Add(fieldReferences[remainingWork], 0);
            }

            return workItem;
        }

        public async Task<WorkItemQueryResult> GetWorkItems()
        {
            Wiql wiql;
            string query = "Select [ID], [State], [Title] " +
                        "From WorkItems " +
                        "WHERE [System.WorkItemType] = 'Bug' " +
                        " AND  (" + fieldReferences[iterationPath] + " UNDER 'INTCORE3\\v4.0' OR " + fieldReferences[iterationPath] + " UNDER 'INTCORE3\\v3.2' )";
            wiql = new Wiql()
            {
                Query = query
            };

            witClient = vssConnection.GetClient<WorkItemTrackingHttpClient>();
            return await witClient.QueryByWiqlAsync(wiql);
        }

        private void openFolder_Click(object sender, EventArgs e)
        {
            Process.Start(AppDomain.CurrentDomain.BaseDirectory);
        }

        public async Task GetTotalEfforts()
        {
            //Wiql wiql;
            //string query = "Select ID " +
            //            "From WorkItems " +
            //            "WHERE [System.WorkItemType] = 'Bug' " +
            //            " ASOF '01/01/2019 12:30' ";
            //wiql = new Wiql()
            //{
            //    Query = query
            //};

            //witClient = vssConnection.GetClient<WorkItemTrackingHttpClient>();
            //var result = await witClient.QueryByWiqlAsync(wiql);

            //int totalCompletedWork = 0;
            //var allWorkItems = witClient.GetWorkItemsAsync(result.WorkItems.Select(p => p.Id)).Result;
            //foreach (var item in allWorkItems)
            //{
            //    if (item.Fields.ContainsKey(fieldReferences[completedWork]))
            //    {
            //        totalCompletedWork += int.Parse(item.Fields[fieldReferences[completedWork]].ToString());
            //    }

            //}

            //MessageBox.Show(totalCompletedWork.ToString());
        }
    }
}
