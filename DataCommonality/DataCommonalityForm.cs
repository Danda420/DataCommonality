using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.WindowsAPICodePack.Dialogs;

namespace DataCommonality
{
    public partial class Form1 : Form
    {
        private string[] csFiles;
        double dataCommonality_;
        int totalModules = 0;

        // UPDATE Variable Details
        public void updateVarDetails(Dictionary<(string, string), Dictionary<string, int>> varCount, string varName, string dataType, string fileName)
        {
            var key = (varName, dataType);

            if (!varCount.ContainsKey(key))
            {
                varCount[key] = new Dictionary<string, int>();
            }

            if (varCount[key].ContainsKey(fileName))
            {
                varCount[key][fileName]++;
            }
            else
            {
                varCount[key][fileName] = 1;
            }
        }

        public void mergeVarCount(Dictionary<(string, string), Dictionary<string, int>> varCount)
        {
            var mergedVarCount = new Dictionary<(string, string), Dictionary<string, int>>();

            foreach (var (varName, dataType) in varCount.Keys)
            {
                var fileCounts = varCount[(varName, dataType)];

                foreach (var fileName in fileCounts.Keys)
                {
                    var splitFileNames = fileName.Split(',');

                    foreach (var splitFileName in splitFileNames)
                    {
                        var key = (varName, dataType);

                        if (mergedVarCount.ContainsKey(key))
                        {
                            if (mergedVarCount[key].ContainsKey(splitFileName))
                            {
                                mergedVarCount[key][splitFileName] += fileCounts[fileName];
                            }
                            else
                            {
                                mergedVarCount[key][splitFileName] = fileCounts[fileName];
                            }
                        }
                        else
                        {
                            mergedVarCount[key] = new Dictionary<string, int> { { splitFileName, fileCounts[fileName] } };
                        }
                    }
                }
            }

            varCount.Clear();
            foreach (var (varName, dataType) in mergedVarCount.Keys)
            {
                string combinedFileNames = string.Join(", ", mergedVarCount[(varName, dataType)].Keys);

                int totalVarCount = mergedVarCount[(varName, dataType)].Values.Sum();

                varCount[(varName, dataType)] = new Dictionary<string, int> { { combinedFileNames, totalVarCount } };
            }
        }

        public void dataCommonality(IEnumerable<string> csFiles)
        {
            dataGridView1.Rows.Clear();
            label2.Text = string.Empty;

            if (csFiles == null)
            {
                return;
            }

            Dictionary<(string, string), Dictionary<string, int>> varCount = new Dictionary<(string, string), Dictionary<string, int>>();

            totalModules = csFiles.Count();

            foreach (var filePath in csFiles)
            {
                string fileName = Path.GetFileNameWithoutExtension(filePath);
                string sourceCode = File.ReadAllText(filePath);

                SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(sourceCode);
                var root = syntaxTree.GetRoot();

                var addedVars = new HashSet<string>();

                var varDecs = root.DescendantNodes().OfType<VariableDeclarationSyntax>();
                foreach (var varDec in varDecs)
                {
                    string varName = varDec.DescendantNodes().OfType<VariableDeclaratorSyntax>().First().Identifier.Text;
                    string dataType = varDec.Type.ToString();

                    if (!string.IsNullOrEmpty(varName) && !varName.StartsWith("_") && char.IsLower(varName[0]))
                    {
                        if (addedVars.Add(varName))
                        {
                            updateVarDetails(varCount, varName, dataType, fileName);
                        }
                    }
                }
            }

            List<string> results = new List<string>();

            // Output combined filenames, varName, varCount, dataTypes, and dataCommonality????
            label2.Text = $"{totalModules}";
            mergeVarCount(varCount);
            var dupe = new HashSet<string>();
            var dupeName = new HashSet<string>();

            foreach (var key in varCount.Keys)
            {
                foreach (var count in varCount[key].Values)
                {
                    string filenames = string.Join(", ", varCount[key].Keys);
                    string varName = key.Item1;
                    string dataType = key.Item2;
                    var entry = $"{filenames}, {key.Item1}, {dataType}";

                    if (!dupe.Contains(entry))
                    {
                        int isUsedOnAllModules = 0;
                        if (count == totalModules)
                        {
                            isUsedOnAllModules = 100;
                        }
                        dataGridView1.Rows.Add(filenames, varName, dataType, count, $"{isUsedOnAllModules}%");
                        if (dupeName.Add(varName))
                        {
                            results.Add($"{varName} {isUsedOnAllModules}%");
                        }
                        dupe.Add(entry);
                    }
                }
            } 
            dataGridView1.Sort(dataGridView1.Columns[3], System.ComponentModel.ListSortDirection.Descending);

            int totalResults = results.Count();
            int hundredPercent = results.Count(entry => entry.Contains("100%"));

            dataCommonality_ = ((double)hundredPercent / totalResults) * 100;

            label4.Text = $"{dataCommonality_}%";
        }

        public Form1()
        {
            InitializeComponent();
        }

        private void btnBrowse_Click(object sender, EventArgs e)
        {
            CommonOpenFileDialog dialog = new CommonOpenFileDialog();
            dialog.Title = "Open Source Code Folder";
            dialog.IsFolderPicker = true;

            if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
            {
                string folderPath = dialog.FileName;
                btnCheck.Enabled = true;
                csFiles = Directory.GetFiles(folderPath, "*.cs");
                string[] javaFiles = Directory.GetFiles(folderPath, "*.java");
                csFiles = csFiles.Concat(javaFiles).ToArray();

            }
        }

        private void btnCheck_Click(object sender, EventArgs e)
        {
            totalModules = 0;
            dataCommonality(csFiles);
            btnCSV.Enabled = true;
        }

        private void btnReset_Click(object sender, EventArgs e)
        {
            dataGridView1.Rows.Clear();
            label2.Text = "null";
            csFiles = null;
            btnCheck.Enabled = false;
            btnCSV.Enabled = false;
            totalModules = 0;
        }

        private void helpBtn_Click(object sender, EventArgs e)
        {
            MessageBox.Show("1. Press 'Open Folder' button to select a folder that has your application source code files \n" +
                "\n" +
                "2. Press 'Start Testing' button to check the use of standard data types and structure inside of your app \n" +
                "\n" +
                "3. After pressing 'Start Testing' button, the result will be displayed \n" +
                "\n" +
                "4. you can export the output into a csv file with 'Export CSV' button\n" +
                "\n" +
                "\n" +
                "This app only accepts C# and Java source codes as input \n" +
                "It might not display results correctly tho if you use java as input...");
        }

        private void btnCSV_Click(object sender, EventArgs e)
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.Filter = "CSV Files (*.csv)|*.csv";
            saveFileDialog.Title = "Save CSV File";
            
            if (saveFileDialog.ShowDialog() == DialogResult.OK)
            {
                StringBuilder sb = new StringBuilder();
            
                // Header
                for (int i = 0; i < dataGridView1.Columns.Count; i++)
                {
                    sb.Append($"\"{dataGridView1.Columns[i].HeaderText}\"");
                    sb.Append(i == dataGridView1.Columns.Count - 1 ? "\n" : ",");
                }
            
                // Rows
                foreach (DataGridViewRow row in dataGridView1.Rows)
                {
                    for (int i = 0; i < dataGridView1.Columns.Count; i++)
                    {
                        object cellValue = row.Cells[i].Value;
                        string cellText = (cellValue == null) ? string.Empty : cellValue.ToString();
            
                        sb.Append($"\"{cellText}\"");
                        sb.Append(i == dataGridView1.Columns.Count - 1 ? "\n" : ",");
                    }
                }
            
                // Additional Information
                sb.AppendLine($"Total Number of Modules : {totalModules}");
                sb.AppendLine($"Data Commonality : {dataCommonality_}%");
            
                File.WriteAllText(saveFileDialog.FileName, sb.ToString(), Encoding.UTF8);
            }
        }
    }
}
