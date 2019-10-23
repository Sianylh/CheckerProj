/////////////////////////////////////////////////////////////////////////////////////
// MainWindow.xaml.cs - Allows user to connect and find bindings, and execute them //
//                                                                                 //
// ver 3.0                                                                         //
// Author: Debopriyo Bhattacharya                                                  //
//                                                                                 //
/////////////////////////////////////////////////////////////////////////////////////
/*
 * Package Operations:
 * -------------------
 * This program connects to SSMS database and then queries it for a table with bindings.
 * Then the bindings are grouped by their names and a dropdown list is generated.
 * The user can select the dropdown list and run a group of bindings or run all of them.
 * The results of the execution are shown in the grid table.
 * There is a status bar at the bottom to show the connection status, success and failure.
 * 
 * Maintenance History:
 * --------------------
 * ver 3.0 : 23 Sept 2019
 * - added second dropdown list of individual queries in a group, 
 *   which gets generated based on selection of the group
 * - ability to run queries as a group or run individual queries
 * - radiobutton to hide success or failures or choose to see them both
 * ver 2.1 : 14 Sept 2019
 * - added datagrid to display name of query group, status of execution, query, details  
 * ver 2.0 : 13 Sept 2019
 * - Added Results Tab
 * - Queries with Similar class get added to drop down list
 * - User can choose the group of queries and run them
 * - Success and Fails shows at the end of execution in the bottom status bar
 * ver 1.1 : 6 Sept 2019
 *   - added functionality to execute sql queries found after executing a query
 * ver 1.0 : 30 Sept 2019
 * - first prototype
 * 
 * Requirements: Must have either login ID and Password for the MSSQL database connection 
 * Or have windows authentication activated for the connection
 * 
 * USE: Execute program, connect, check connection, Click on Test1 for now, 
 * then in Results tab select binding group and Run. 
 * 
 * 
 */



using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;

namespace CheckerProj
{

    public class ExecBinding
    {
        public string Name { get; set; }
        public string Status { get; set; }
        public string Detail { get; set; }
        public string Query { get; set; }
    }

    public partial class MainWindow : Window
    {
        SqlConnection cnn;
        List<string> allQueries = new List<string>();
        List<string> allNames = new List<string>();
        Hashtable dropNames = new Hashtable();  //Not using currently except for generating the groups
                                                //Might find a use for it later on, can be replaced by a simple list
        Hashtable dropNames_2 = new Hashtable();// Using this mostly since we are generating a second dropdown list
        List<ExecBinding> items = new List<ExecBinding>();
        bool handle = true;  //to handle dropdown change event
        public MainWindow()
        {
            InitializeComponent();
            both.IsChecked = true;
            genListView();
        }

        public void genListView()
        {

            execResults.SelectionMode = DataGridSelectionMode.Single;
            execResults.SelectionUnit = DataGridSelectionUnit.Cell;

            Style colStyle = new Style(typeof(TextBlock));
            Setter setter = new Setter(TextBlock.TextWrappingProperty, TextWrapping.Wrap);
            colStyle.Setters.Add(setter);

            DataGridTextColumn col1 = new DataGridTextColumn();
            col1.ElementStyle = colStyle;
            col1.Header = "Name";
            col1.Binding = new Binding("Name");
            col1.CanUserSort = true;
            execResults.Columns.Add(col1);

            DataGridTextColumn col2 = new DataGridTextColumn();
            col2.ElementStyle = colStyle;
            col2.Header = "Status";
            col2.Binding = new Binding("Status");
            col2.CanUserSort = true;
            execResults.Columns.Add(col2);

            DataGridTextColumn col3 = new DataGridTextColumn();
            col3.ElementStyle = colStyle;
            col3.Header = "Detail";
            col3.Binding = new Binding("Detail");
            col3.CanUserSort = true;
            execResults.Columns.Add(col3);

            DataGridTextColumn col4 = new DataGridTextColumn();
            col4.ElementStyle = colStyle;
            col4.Header = "SQL Query";
            col4.Binding = new Binding("Query");
            col4.CanUserSort = false;
            execResults.Columns.Add(col4);

        }


        private void Window_Closed(object sender, EventArgs e)
        {
            if (cnn != null && cnn.State == ConnectionState.Open)
            {
                cnn.Close();
            }

            // The step below should not be nessary, but I've apparently caused a closing event to 
            // hang by manually renaming packages instead of getting Visual Studio to rename them.

            System.Diagnostics.Process.GetCurrentProcess().Kill();
        }
        //----< not currently being used >-------------------------------

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
        }

        private void Connection_Click(object sender, RoutedEventArgs e)
        {
            //After making connection we update the status

            string connetionString;
            if(auth1.IsChecked == true)
                connetionString = @"data source=" + server.Text + ";initial catalog=" + db.Text + ";Integrated Security = True;";//"; User ID=" + id.Text + ";Password=" + pass.Text + ";";
            else
                connetionString = @"data source=" + server.Text + ";initial catalog=" + db.Text + ";Integrated Security = False; User ID=" + id.Text + ";Password=" + pass.Text + ";";

            cnn = new SqlConnection(connetionString);
            try
            {
                Status.Content = "Connecting... Please Wait.";
                cnn.Open();
            }
            catch (Exception ex)
            {
                Status.Content = "Connection Failed";
                Console.WriteLine(ex);
                return;
            }
            //ConnectionDetails.Text = connetionString;

            if (cnn != null && cnn.State == ConnectionState.Open)
            {
                Status.Content = "Connected Successfully";
            }
            else
            {
                Status.Content = "Connection Failed";
            }
        }

        private void DisConnect_Click(object sender, RoutedEventArgs e)
        {
            if (cnn != null && cnn.State == ConnectionState.Open)
            {
                cnn.Close();
                Status.Content = "Connection Closed";
            }
        }

        private void ConButton_Click(object sender, RoutedEventArgs e)
        {
            if (cnn != null && cnn.State == ConnectionState.Open)
                Status.Content = "Connection is Live";

            else
                Status.Content = "No Connection";
        }

        private void Start_Click(object sender, RoutedEventArgs e)
        {

            Status.Content = "Finding Queries";

            //string queryString = "SELECT [queryText] FROM[openemr].[dbo].[queries]; ";

            //We will either take user input or make other changes to make this easily modifiable by user
            string queryString = $@" SELECT 
               b.BindingClassificationCD
              ,CASE b.BindingClassificationCD
                    WHEN 'Event' THEN 1
                     WHEN 'Rule' THEN 2
                     WHEN 'Population' THEN 3
                     WHEN 'Summary' THEN 4
                     WHEN 'ReportingView' THEN 5
                     END AS ClassificationSortNBR
              ,b.BindingNM
              ,oa.AttributeValueLongTXT
       FROM EDWAdmin.CatalystAdmin.ObjectAttributeBASE oa
       INNER JOIN  EDWAdmin.CatalystAdmin.BindingBASE b
       ON oa.ObjectID = b.BindingID
              INNER JOIN EDWAdmin.CatalystAdmin.DataMartBASE dm
              ON dm.DataMartID = b.DataMartID
       WHERE oa.AttributeNM = 'UserDefinedSQL'
              AND dm.DataMartNM = 'SharedClinicalProfiling'
       ORDER BY ClassificationSortNBR, BindingNM; ";
            int queryCounter=0;

            if (cnn != null && cnn.State == ConnectionState.Open)
            {
                SqlDataReader rdr = null;
                SqlCommand cmd;

                try
                {
                    cmd = new SqlCommand(queryString, cnn);

                    // get query results
                    rdr = cmd.ExecuteReader();

                    // returns each queries

                    //int count = rdr.FieldCount;

                    int comCol = rdr.GetOrdinal("AttributeValueLongTXT");
                    int nmCol = rdr.GetOrdinal("BindingNM");

                    while (rdr.Read())
                    {
                        /*
                        for (int i = 0; i < count; i++)
                        {
                            queryCounter++;
                            Console.WriteLine(rdr.GetValue(i));
                            //allQueries.Add((string)rdr.GetValue(i));
                            
                        }
                        */
                        queryCounter++;
                        Console.WriteLine(rdr.GetValue(comCol));
                        allQueries.Add((string)rdr.GetValue(comCol));
                        allNames.Add((string)rdr.GetValue(nmCol));

                    }
                    rdr.Close();

                }
                catch
                {
                    Status.Content = "Queries Not Found";
                    return;
                }
                finally
                {
                    // close the reader
                    if (rdr != null)
                    {
                        rdr.Close();
                    }

                }

                dropDownGen(allNames, allQueries);
                tabControl.SelectedIndex = 1; //Going to the Results tab

            }
        }


        private void auth_option(object sender, RoutedEventArgs e)
        {
            if (auth1.IsChecked == true)
            {
                id.IsEnabled = false;
                pass.IsEnabled = false;
            }
            else
            {
                id.IsEnabled = true;
                pass.IsEnabled = true;
            }

        }


        private void dropDownGen(List<string> qNM, List<string> qCmd)
        {
            //Your code goes here
            Console.WriteLine("/////////////Gen Query Bundles///////////////");
            

            //List<string> mylist = new List<string>() { "Dimension Value Distribution", "Event Counts", "Event Ratios", "Last Profiling Run", "Profile Allergy", "Profile Allergy (Since Last Profile)", "Profile Column Stats", "Profile Compare Latest Vs Hist", "Profile Compare Latest Vs Hist (Target Systems)", "Profile Compare Norm Vs Orig", "Profile Compare Shared Vs Alt Source", "Profile Details", "Profile Details History", "Profile Diagnosis", "Profile Diagnosis (Since Last Profile)", "Profile Encounter", "Profile Encounter (Inpatient Only)", "Profile Encounter (Since Last Profile)", "Profile FacilityAccount", "Profile FacilityAccount (Inpatient Only)", "Profile FacilityAccount (Since Last Profile)", "Profile LabResult", "Profile LabResult (Since Last Profile)", "Profile MedicationAdministration", "Profile MedicationAdministration (Since Last Profile)", "Profile MedicationOrder", "Profile MedicationOrder (Since Last Profile)", "Profile Observations", "Profile Observations (Since Last Profile)", "Profile Orders", "Profile Orders (Since Last Profile)", "Profile Procedures", "Profile Procedures (Since Last Profile)", "Profiling Settings", "Sample Diagnosis", "Sample Encounter", "Sample FacilityAccount", "Sample LabResult", "Sample Master Encounter", "Sample MedicationAdministration", "Sample MedicationOrder", "Sample Orders", "Sample Procedures", "Unit Test" };

            //obj.Add("1", list);
            //        ^the name, the commands

            for (var i = 0; i < qNM.Count; i++)
            {
                var possibleName = qNM[i].Split(' ').First(); // splits the strings by spaces and then grabs the first one

                var times = qNM.Count(x => x.Split(' ').First().Contains(possibleName));

                //Console.WriteLine("{0}  {1} {2}", i, possibleName, times); 

                if (times == 1) // We saying this group has only one
                {
                    dropNames.Add(qNM[i], new List<string>() { qCmd[i] });
                }
                else if (!dropNames.ContainsKey(possibleName))   // That key does not exist
                {
                    List<string> sameCmds = new List<string>();

                    for (var j = i; j < qNM.Count; j++)
                    {
                        if (possibleName == qNM[j].Split(' ').First())
                            sameCmds.Add(qCmd[j]);
                    }

                    dropNames.Add(possibleName, sameCmds);

                }

            }

            //Displaying Query Class and Queries
            //Also adding Query Names to Dropdown list

            foreach (string key in dropNames.Keys)
            {
                options.Items.Add(key);
                Console.WriteLine(String.Format("Commands for {0} are : ", key));

                foreach (var item in (List<string>)dropNames[key])
                {
                    Console.WriteLine(item);
                }

            }
            

            options.SelectedItem = options.Items[0];
            Status.Content = "Queries Found";

        }


        private void queryExecutor()
        {
            Status.Content = "Running Queries";
            items.Clear();//Clearing the previous output items

            //clear the current datagrid
            execResults.Items.Clear();

            int querySuccess = 0;
            int queryFails = 0;

            //run each query
            //

            foreach (string key in dropNames_2.Keys)
            {
                string command = (string)((List<string>)dropNames_2[key])[0];
                
                ExecBinding result = RunQuery(key,command);
                items.Add(result);
                if (result.Status == "Success")
                    querySuccess++;
                else
                    queryFails++;
            }

            //Now Display all Items here
            hider("SomethingRandomThatDoesn'tMatch");

            Status.Content = "Success: " + querySuccess + " Fails: " + queryFails;

            execResults.Items.Refresh();

        }

        public ExecBinding RunQuery(string key,string command)
        {
            ExecBinding result;
            SqlCommand eachCmd;
            string status = "", detail = "";
            try
            {
                eachCmd = new SqlCommand(command, cnn);
                int a = eachCmd.ExecuteNonQuery();

                if (a == 0)//No Rows were affected
                {
                    status = "Success";
                    detail = "No Rows Affected.";
                    Console.WriteLine("No Rows Affected.");
                }
                else//Updated.
                {
                    status = "Success";
                    detail = "Worked as Expected.";
                    Console.WriteLine("Success!! {0} Rows Affected.", a);
                }
            }
            catch (Exception ex)
            {
                status = "Failure";
                detail = ex.Message;
                Console.WriteLine("Exception! Could not Execute Query.");
                Console.WriteLine(ex.Message);
            }
            finally
            {
                result = new ExecBinding() { Name = key, Status = status, Detail = detail, Query = command };
            }
            return result;
        }

        private void Both(object sender, RoutedEventArgs e)
        {
            hider("SomethingRandomThatDoesn'tMatch");
            execResults.Items.Refresh();
        }


        private void SOnly(object sender, RoutedEventArgs e)
        {
            hider("Failure");
            execResults.Items.Refresh();
        }

        private void FOnly(object sender, RoutedEventArgs e)
        {
            hider("Success");
            execResults.Items.Refresh();
        }

        private void hider(string str)
        {
            //First we clear everything
            execResults.Items.Clear();

            foreach (ExecBinding p in items)
            {
                if (p.Status != str)
                    execResults.Items.Add(p);
            }
        }

        private void FirstCB_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ComboBox cmb = sender as ComboBox;
            handle = !cmb.IsDropDownOpen;
            genOptions2();
        }


        private void FirstCB_DropDownClosed(object sender, EventArgs e)
        {
            if (handle)
                genOptions2();
            handle = true;
        }

        private void genOptions2()
        {
            dropNames_2.Clear(); //We clear the hashtable for second dropdown menu
            options_2.Items.Clear(); //We clear the second dropdown menu
            string choice = options.SelectedItem.ToString();
            Console.WriteLine("You chose:" + choice);
            int count = ((List<string>)dropNames[choice]).Count;
            Console.WriteLine("Total Queries in this Group:" + count);
            if (count == 1)
            {
                dropNames_2.Add(choice, dropNames[choice]);
                options_2.Items.Add(choice);
            }
            else
            {
                for (var i = 0; i < allNames.Count; i++)
                {
                    
                    if (choice == allNames[i].Split(' ').First())
                    {
                        
                        dropNames_2.Add(allNames[i], new List<string>() { allQueries[i] });
                        options_2.Items.Add(allNames[i]);
                    }

                }


            }

            options_2.SelectedItem = options_2.Items[0];
        }
        private void Run_Routine(object sender, RoutedEventArgs e)
        {
            execResults.Items.Clear();
            Console.WriteLine("Running Group"+options.SelectedItem);
            //queryExecutor((List<string>)dropNames[options.SelectedItem], (string)options.SelectedItem);
            queryExecutor();
        }

        private void RunEach_Routine(object sender, RoutedEventArgs e)
        {
            items.Clear();//Clearing the previous output items

            execResults.Items.Clear();//clear the current datagrid

            int querySuccess = 0;
            int queryFails = 0;

            string key = (string)options_2.SelectedItem;
            string command = (string)((List<string>)dropNames_2[key])[0];

            ExecBinding result = RunQuery(key, command);
            items.Add(result);

            if (result.Status == "Success")
                querySuccess++;
            else
                queryFails++;

            //Now Display all Items here
            hider("SomethingRandomThatDoesn'tMatch");

            Status.Content = "Success: " + querySuccess + " Fail: " + queryFails;

            execResults.Items.Refresh();

        }

    }

}
