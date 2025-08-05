using McTools.Xrm.Connection;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Microsoft.Xrm.Tooling.Connector;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using XrmToolBox.Extensibility;

namespace CloudFlowTracker
{
    public partial class MyPluginControl : PluginControlBase
    {
        private Settings mySettings;
        string defSolutionId = string.Empty;
        public MyPluginControl()
        {
            InitializeComponent();


        }

        private void MyPluginControl_Load(object sender, EventArgs e)
        {
            //ShowInfoNotification("This is a notification that can lead to XrmToolBox repository", new Uri("https://github.com/MscrmTools/XrmToolBox"));
            ExecuteMethod(GetDefSolution);
            // Loads or creates the settings for the plugin
            if (!SettingsManager.Instance.TryLoad(GetType(), out mySettings))
            {
                mySettings = new Settings();

                LogWarning("Settings not found => a new settings file has been created!");
            }
            else
            {
                LogInfo("Settings found and loaded");
            }

        }

        private void tsbClose_Click(object sender, EventArgs e)
        {
            CloseTool();
        }

        private void tsbFlowList_Click(object sender, EventArgs e)
        {
            // The ExecuteMethod method handles connecting to an
            // organization if XrmToolBox is not yet connected
            ExecuteMethod(GetFlow);
        }

        private void GetAccounts()
        {
            WorkAsync(new WorkAsyncInfo
            {
                Message = "Getting accounts",
                Work = (worker, args) =>
                {
                    args.Result = Service.RetrieveMultiple(new QueryExpression("account")
                    {
                        TopCount = 50
                    });
                },
                PostWorkCallBack = (args) =>
                {
                    if (args.Error != null)
                    {
                        MessageBox.Show(args.Error.ToString(), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                    var result = args.Result as EntityCollection;
                    if (result != null)
                    {
                        MessageBox.Show($"Found {result.Entities.Count} accounts");
                    }
                }
            });
        }


        private void GetFlow()
        {
            WorkAsync(new WorkAsyncInfo
            {
                Message = "Getting FlowList",
                Work = (worker, args) =>
                {

                    QueryExpression qe = new QueryExpression("workflow");
                    qe.ColumnSet = new ColumnSet(true);
                    qe.Criteria.AddCondition(new ConditionExpression("category", ConditionOperator.Equal, 5));
                    qe.Criteria.AddCondition(new ConditionExpression("clientdata", ConditionOperator.Like, "%When_a_row_is_added,_modified_or_deleted%"));



                    if (!string.IsNullOrEmpty(stripTextEntity.Text))
                        qe.Criteria.AddCondition(new ConditionExpression("clientdata", ConditionOperator.Like, $"%{stripTextEntity.Text}%"));

                    listView1.Items.Clear();

                    args.Result = Service.RetrieveMultiple(qe);

                },
                PostWorkCallBack = (args) =>
                {
                    if (args.Error != null)
                    {
                        MessageBox.Show(args.Error.ToString(), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                    var result = args.Result as EntityCollection;
                    if (result != null)
                    {
                        string clientdata = string.Empty;
                        foreach (var ent in result.Entities)
                        {
                            ListViewItem lstItem = new ListViewItem();

                            lstItem.Text = ent.Attributes["name"]?.ToString();



                            if (ent.Contains("clientdata"))
                            {
                                clientdata = ent.Attributes["clientdata"]?.ToString();

                                clientdata = clientdata.Replace("When_a_row_is_added,_modified_or_deleted", "When_a_row_is_added_modified_or_deleted")
                                    .Replace("subscriptionRequest/entityname", "subscriptionRequestentityname")
                                    .Replace("subscriptionRequest/message", "subscriptionRequestmessage");

                                ClientData clData = Newtonsoft.Json.JsonConvert.DeserializeObject<ClientData>(clientdata);

                                var eName = clData.properties.definition.triggers?.When_a_row_is_added_modified_or_deleted?.inputs?.parameters?.subscriptionRequestentityname;

                                //if (!string.IsNullOrEmpty(entityName) && eName?.ToLower() != entityName.ToLower()) continue;

                                lstItem.SubItems.Add(eName);
                                 int ? msg = clData.properties.definition.triggers?.When_a_row_is_added_modified_or_deleted?.inputs?.parameters?.subscriptionRequestmessage;

                                    lstItem.SubItems.Add(msg?.ToString());

                                    
                                lstItem.SubItems.Add(ent.GetAttributeValue<OptionSetValue>("statecode")?.Value == 0 ? "Draft" : "Activated");
                                lstItem.SubItems.Add(ent.Attributes["createdon"]?.ToString());

                                string url = "";
                                
                                //url = ConnectionDetail.OriginalUrl + "/" + ConnectionDetail.EnvironmentId + "/" + defSolutionId + "/" + ent.Id.ToString();
                                url = $"https://make.powerapps.com/environments/{ConnectionDetail.GetCrmServiceClient().EnvironmentId}/solutions/{defSolutionId}/objects/cloudflows/{ent.Id}/view";
                                lstItem.SubItems.Add(url);

                                lstItem.SubItems.Add(ent.Id.ToString());

                                listView1.Items.Add(lstItem);
                            }

                        }
                    }
                }
            });
        }


        private void GetDefSolution()
        {
            WorkAsync(new WorkAsyncInfo
            {
                Message = "Getting DefSolution",
                Work = (worker, args) =>
                {

                    QueryExpression qe = new QueryExpression("solution");
                    qe.Criteria.AddCondition(new ConditionExpression("uniquename", ConditionOperator.Equal, "Default"));
                    
                    args.Result = Service.RetrieveMultiple(qe);

                },
                PostWorkCallBack = (args) =>
                {
                    if (args.Error != null)
                    {
                        MessageBox.Show(args.Error.ToString(), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                    var ec = args.Result as EntityCollection;
                    if (ec != null)
                    {
                        if (ec != null && ec.Entities.Count > 0)
                        {
                            defSolutionId = ec.Entities[0].Id.ToString();
                        }

                    }
                }
            });
        }


        /// <summary>
        /// This event occurs when the plugin is closed
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MyPluginControl_OnCloseTool(object sender, EventArgs e)
        {
            // Before leaving, save the settings
            SettingsManager.Instance.Save(GetType(), mySettings);
        }

        /// <summary>
        /// This event occurs when the connection has been updated in XrmToolBox
        /// </summary>
        public override void UpdateConnection(IOrganizationService newService, ConnectionDetail detail, string actionName, object parameter)
        {
            base.UpdateConnection(newService, detail, actionName, parameter);

            if (mySettings != null && detail != null)
            {
                mySettings.LastUsedOrganizationWebappUrl = detail.WebApplicationUrl;
                LogInfo("Connection has changed to: {0}", detail.WebApplicationUrl);
            }
        }

        private void splitContainer1_Panel2_Paint(object sender, PaintEventArgs e)
        {

        }

        private void splitContainer1_Panel1_Paint(object sender, PaintEventArgs e)
        {

        }
        public void Blank()
        {

        }

        private void listView1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if(listView1.SelectedItems.Count > 0)
            {
                txtName.Text = listView1.SelectedItems[0].Text.ToString();
                txtEntity.Text = listView1.SelectedItems[0].SubItems[1].Text.ToString();
                txtMessage.Text = listView1.SelectedItems[0].SubItems[2].Text.ToString();
                txtStateCode.Text = listView1.SelectedItems[0].SubItems[3].Text.ToString();
                txtCreatedOn.Text = listView1.SelectedItems[0].SubItems[4].Text.ToString();
                txtURL.Text = listView1.SelectedItems[0].SubItems[5].Text.ToString();


            }
        }

        private void label1_Click(object sender, EventArgs e)
        {

        }

        private void label2_Click(object sender, EventArgs e)
        {

        }

        private void label3_Click(object sender, EventArgs e)
        {

        }

        private void label4_Click(object sender, EventArgs e)
        {

        }

        private void textBox2_TextChanged(object sender, EventArgs e)
        {

        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {

        }

        private void textBox6_TextChanged(object sender, EventArgs e)
        {

        }

        private void btnCopyURL_Click(object sender, EventArgs e)
        {
            Clipboard.SetText(txtURL.Text);
        }
    }


    public class ClientData
    {
        public Properties properties { get; set; }
    }

    public class Properties
    {
        public Definition definition { get; set; }
    }

    public class Definition
    {
        public Triggers triggers { get; set; }
    }

    public class Triggers
    {
        public When_A_Row_Is_Added_Modified_Or_Deleted When_a_row_is_added_modified_or_deleted { get; set; }
    }

    public class When_A_Row_Is_Added_Modified_Or_Deleted
    {
        public Inputs inputs { get; set; }
    }


    public class Inputs
    {
        public Parameters1 parameters { get; set; }
    }

    public class Parameters1
    {
        public int subscriptionRequestmessage { get; set; }
        public string subscriptionRequestentityname { get; set; }
        public int subscriptionRequestscope { get; set; }
    }

    public class Actions
    {
        public Condition Condition { get; set; }
    }

    public class Condition
    {
        public Actions1 actions { get; set; }
        public Runafter runAfter { get; set; }
        public Expression expression { get; set; }
        public Metadata1 metadata { get; set; }
        public string type { get; set; }
    }

    public class Actions1
    {
    }

    public class Runafter
    {
    }

    public class Expression
    {
        public int[] equals { get; set; }
    }

    public class Metadata1
    {
        public string operationMetadataId { get; set; }
    }


}