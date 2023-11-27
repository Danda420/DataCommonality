using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using System.Xml.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.WindowsAPICodePack.Dialogs;

namespace DataCommonalityChecker
{
    public partial class Form1 : Form
    {
        private List<string> sourceCodeFile = new List<string>();
        private string[] csFiles;
        private string sourceCodeSingle;

        bool dataCommonalityMultiple = false;
        bool countVarsDeclarationsAlso = false;

        // UPDATE VARCOUNT FOR dataCommonalitySingle
        public void updateVarCount(Dictionary<string, Dictionary<string, int>> varCount, string varName)
        {
            int count = 1;

            if (varCount.ContainsKey(varName))
            {
                if (varCount[varName].ContainsKey(""))
                {
                    varCount[varName][""] += count;
                }
                else
                {
                    varCount[varName][""] = count;
                }
            }
            else
            {
                varCount[varName] = new Dictionary<string, int> { { "", count } };
            }
        }

        // UPDATE VARCOUNT FOR dataCommonalityMulti
        public void updateVarCountMulti(Dictionary<string, Dictionary<string, int>> varCount, string varName, string fileName)
        {
            if (!varCount.ContainsKey(varName))
            {
                varCount[varName] = new Dictionary<string, int>();
            }

            if (varCount[varName].ContainsKey(fileName))
            {
                varCount[varName][fileName]++;
            }
            else
            {
                varCount[varName][fileName] = 1;
            }
        }

        // MERGE VARCOUNT AND FILENAME (THAT HAS THE SAME VARIABLE) ACROSS ALL FILES SUPPLIED
        public void mergeVarCount(Dictionary<string, Dictionary<string, int>> varCount)
        {
            var mergedVarCount = new Dictionary<string, Dictionary<string, int>>();

            foreach (var varName in varCount.Keys)
            {
                var fileCounts = varCount[varName];

                foreach (var fileName in fileCounts.Keys)
                {
                    var splitFileNames = fileName.Split(',');

                    foreach (var splitFileName in splitFileNames)
                    {
                        if (mergedVarCount.ContainsKey(varName))
                        {
                            if (mergedVarCount[varName].ContainsKey(splitFileName))
                            {
                                mergedVarCount[varName][splitFileName] += fileCounts[fileName];
                            }
                            else
                            {
                                mergedVarCount[varName][splitFileName] = fileCounts[fileName];
                            }
                        }
                        else
                        {
                            mergedVarCount[varName] = new Dictionary<string, int> { { splitFileName, fileCounts[fileName] } };
                        }
                    }
                }
            }

            varCount.Clear();
            foreach (var varName in mergedVarCount.Keys)
            {
                string combinedFileNames = string.Join(", ", mergedVarCount[varName].Keys);

                int totalVarCount = mergedVarCount[varName].Values.Sum();

                if (varCount.ContainsKey(varName))
                {
                    varCount[varName] = new Dictionary<string, int> { { combinedFileNames, totalVarCount } };
                }
                else
                {
                    varCount[varName] = new Dictionary<string, int> { { combinedFileNames, totalVarCount } };
                }
            }
        }

        public void dataCommonalitySingle(string sourceCodeFile)
        {
            dataGridView1.Rows.Clear();
            label2.Text = string.Empty;
            int totalModules = 0;

            if (sourceCodeFile == null)
            {
                label2.Text = "No C# Source Code File selected!";
                return;
            }

            string fileName = Path.GetFileNameWithoutExtension(sourceCodeFile);
            string sourceCode = File.ReadAllText(sourceCodeFile);

            SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(sourceCode);
            var root = syntaxTree.GetRoot();

            var classDec = new List<ClassDeclarationSyntax>();

            var partialClassDec = root.DescendantNodes().OfType<ClassDeclarationSyntax>()
                .Where(c => c.Modifiers.Any(modifier => modifier.IsKind(SyntaxKind.PublicKeyword)) &&
                            c.Modifiers.Any(modifier => modifier.IsKind(SyntaxKind.PartialKeyword)));
            var methodDec = root.DescendantNodes().OfType<MethodDeclarationSyntax>();

            foreach (var partialClass in partialClassDec)
            {
                var cDec = partialClass.DescendantNodes().OfType<ClassDeclarationSyntax>();
                classDec.AddRange(cDec);
            }

            totalModules += classDec.Count() + methodDec.Count();

            Dictionary<string, Dictionary<string, int>> varCount = new Dictionary<string, Dictionary<string, int>>();

            var addedVars = new HashSet<string>();
            var addedVarsDecs = new HashSet<string>();

            // VAR USAGES & DECS START
            foreach (var cDec in classDec)
            {
                var propertyDec = cDec.DescendantNodes().OfType<PropertyDeclarationSyntax>();
                var varUsages = cDec.DescendantNodes().OfType<IdentifierNameSyntax>();

                foreach (var usages in varUsages)
                {
                    string varName = usages.Identifier.Text;

                    if (!string.IsNullOrEmpty(varName) && !varName.StartsWith("_") && char.IsLower(varName[0]) && (varName != "var"))
                    {
                        if (addedVars.Add(varName))
                        {
                            updateVarCount(varCount, varName);
                        }
                    }
                }
                // VAR DECS IN CLASSES START
                if (countVarsDeclarationsAlso == true)
                {
                    var varDecsC = cDec.DescendantNodes().OfType<VariableDeclarationSyntax>();

                    foreach (var vDec in varDecsC)
                    {
                        string varName = vDec.Variables.FirstOrDefault()?.Identifier.Text;

                        if (!string.IsNullOrEmpty(varName) && !varName.StartsWith("_") && char.IsLower(varName[0]) && (varName != "var"))
                        {
                            if (addedVars.Add(varName))
                            {
                                updateVarCount(varCount, varName);
                            }
                        }
                    }
                }
                // VAR DECS IN CLASSES END

                foreach (var pDecC in propertyDec)
                {
                    var varUsagesP = pDecC.DescendantNodes().OfType<IdentifierNameSyntax>();

                    foreach (var usages in varUsages)
                    {
                        string varName = usages.Identifier.Text;

                        if (!string.IsNullOrEmpty(varName) && !varName.StartsWith("_") && char.IsLower(varName[0]) && (varName != "var"))
                        {
                            if (addedVars.Add(varName))
                            {
                                updateVarCount(varCount, varName);
                            }
                        }
                    }

                    // VAR DECS IN PROPERTY START
                    if (countVarsDeclarationsAlso == true)
                    {
                        var varDecsP = pDecC.DescendantNodes().OfType<VariableDeclarationSyntax>();

                        foreach (var vDecP in varDecsP)
                        {
                            string varName = vDecP.Variables.FirstOrDefault()?.Identifier.Text;

                            if (!string.IsNullOrEmpty(varName) && !varName.StartsWith("_") && char.IsLower(varName[0]) && (varName != "var"))
                            {
                                if (addedVars.Add(varName))
                                {
                                    updateVarCount(varCount, varName);
                                }
                            }
                        }
                    }
                    // VAR DECS IN PROPERTY END
                }
                addedVars.Clear();
                addedVarsDecs.Clear();
            }

            foreach (var mDec in methodDec)
            {
                var varUsages = mDec.DescendantNodes().OfType<IdentifierNameSyntax>();

                foreach (var usages in varUsages)
                {
                    string varName = usages.Identifier.Text;

                    if (!string.IsNullOrEmpty(varName) && !varName.StartsWith("_") && char.IsLower(varName[0]) && (varName != "var"))
                    {
                        if (addedVars.Add(varName))
                        {
                            updateVarCount(varCount, varName);
                        }
                    }
                }
                // VAR DECS IN METHOD START
                if (countVarsDeclarationsAlso == true)
                {
                    var varDecsM = mDec.DescendantNodes().OfType<VariableDeclarationSyntax>();

                    foreach (var vDecM in varDecsM)
                    {
                        string varName = vDecM.Variables.FirstOrDefault()?.Identifier.Text;

                        if (!string.IsNullOrEmpty(varName) && !varName.StartsWith("_") && char.IsLower(varName[0]) && (varName != "var"))
                        {
                            if (addedVars.Add(varName))
                            {
                                updateVarCount(varCount, varName);
                            }
                        }
                    }
                }
                // VAR DECS IN METHOD END
                addedVars.Clear();
                addedVarsDecs.Clear();
            }

            // VAR DECS IN ROOT START
            if (countVarsDeclarationsAlso == true)
            {
                var varDecsR = root.DescendantNodes().OfType<VariableDeclarationSyntax>();

                foreach (var vDecR in varDecsR)
                {
                    string varName = vDecR.Variables.FirstOrDefault()?.Identifier.Text;

                    if (!string.IsNullOrEmpty(varName) && !varName.StartsWith("_") && char.IsLower(varName[0]) && (varName != "var"))
                    {
                        if (addedVars.Add(varName))
                        {
                            updateVarCount(varCount, varName);
                        }
                    }
                }
            }
            addedVars.Clear();
            addedVarsDecs.Clear();
            // VAR DECS IN ROOT END

            // VAR USAGES & DECS END

            // Count Data Commonality and output it into dataGridView
            label2.Text = $"Total Number of Modules : {totalModules}";
            foreach (var a in varCount)
            {
                foreach (var count in a.Value.Values)
                {
                    double dataCommonality = ((double)count / totalModules) * 100;
                    dataGridView1.Rows.Add(fileName, a.Key, count, $"{dataCommonality}%");
                }

            }
            dataGridView1.Sort(dataGridView1.Columns[2], System.ComponentModel.ListSortDirection.Descending);
        }

        public void dataCommonalityMulti(IEnumerable<string> csFiles)
        {
            dataGridView1.Rows.Clear();
            label2.Text = string.Empty;
            int totalModules = 0;

            if (csFiles == null)
            {
                label2.Text = "No folder selected!";
                return;
            }

            Dictionary<string, Dictionary<string, int>> varCount = new Dictionary<string, Dictionary<string, int>>();

            foreach (var filePath in csFiles)
            {
                string fileName = Path.GetFileNameWithoutExtension(filePath);
                string sourceCode = File.ReadAllText(filePath);

                SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(sourceCode);
                var root = syntaxTree.GetRoot();

                var classDec = new List<ClassDeclarationSyntax>();
                var partialClassDec = root.DescendantNodes().OfType<ClassDeclarationSyntax>()
                    .Where(c => c.Modifiers.Any(modifier => modifier.IsKind(SyntaxKind.PublicKeyword)) &&
                                c.Modifiers.Any(modifier => modifier.IsKind(SyntaxKind.PartialKeyword)));
                var methodDec = root.DescendantNodes().OfType<MethodDeclarationSyntax>();

                foreach (var partialClass in partialClassDec)
                {
                    var cDec = partialClass.DescendantNodes().OfType<ClassDeclarationSyntax>();
                    classDec.AddRange(cDec);
                }

                totalModules += classDec.Count() + methodDec.Count();

                var addedVars = new HashSet<string>();
                var addedVarsDecs = new HashSet<string>();

                // VAR USAGES & DECS START
                foreach (var cDec in classDec)
                {
                    var propertyDec = cDec.DescendantNodes().OfType<PropertyDeclarationSyntax>();
                    var varUsages = cDec.DescendantNodes().OfType<IdentifierNameSyntax>();

                    foreach (var usages in varUsages)
                    {
                        string varName = usages.Identifier.Text;

                        if (!string.IsNullOrEmpty(varName) && !varName.StartsWith("_") && char.IsLower(varName[0]) && (varName != "var"))
                        {
                            if (addedVars.Add(varName))
                            {
                                updateVarCountMulti(varCount, varName, fileName);
                            }
                        }
                    }

                    // VAR DECS IN CLASSES START
                    if (countVarsDeclarationsAlso == true)
                    {
                        var varDecsC = cDec.DescendantNodes().OfType<VariableDeclarationSyntax>();

                        foreach (var vDec in varDecsC)
                        {
                            string varName = vDec.Variables.FirstOrDefault()?.Identifier.Text;

                            if (!string.IsNullOrEmpty(varName) && !varName.StartsWith("_") && char.IsLower(varName[0]) && (varName != "var"))
                            {
                                if (addedVars.Add(varName))
                                {
                                    updateVarCountMulti(varCount, varName, fileName);
                                }
                            }
                        }
                    }
                    // VAR DECS IN CLASSES END

                    foreach (var pDecC in propertyDec)
                    {
                        var varUsagesP = pDecC.DescendantNodes().OfType<IdentifierNameSyntax>();

                        foreach (var usages in varUsagesP)
                        {
                            string varName = usages.Identifier.Text;

                            if (!string.IsNullOrEmpty(varName) && !varName.StartsWith("_") && char.IsLower(varName[0]) && (varName != "var"))
                            {
                                if (addedVars.Add(varName))
                                {
                                    updateVarCountMulti(varCount, varName, fileName);
                                }
                            }
                        }

                        // VAR DECS IN PROPERTY START
                        if (countVarsDeclarationsAlso == true)
                        {
                            var varDecsP = pDecC.DescendantNodes().OfType<VariableDeclarationSyntax>();

                            foreach (var vDecP in varDecsP)
                            {
                                string varName = vDecP.Variables.FirstOrDefault()?.Identifier.Text;

                                if (!string.IsNullOrEmpty(varName) && !varName.StartsWith("_") && char.IsLower(varName[0]) && (varName != "var"))
                                {
                                    if (addedVars.Add(varName))
                                    {
                                        updateVarCountMulti(varCount, varName, fileName);
                                    }
                                }
                            }
                        }
                        // VAR DECS IN PROPERTY END
                    }
                    addedVars.Clear();
                    addedVarsDecs.Clear();
                }

                foreach (var mDec in methodDec)
                {
                    var varUsages = mDec.DescendantNodes().OfType<IdentifierNameSyntax>();

                    foreach (var usages in varUsages)
                    {
                        string varName = usages.Identifier.Text;

                        if (!string.IsNullOrEmpty(varName) && !varName.StartsWith("_") && char.IsLower(varName[0]) && (varName != "var"))
                        {
                            if (addedVars.Add(varName))
                            {
                                updateVarCountMulti(varCount, varName, fileName);
                            }
                        }
                    }
                    // VAR DECS IN METHOD START
                    if (countVarsDeclarationsAlso == true)
                    {
                        var varDecsM = mDec.DescendantNodes().OfType<VariableDeclarationSyntax>();

                        foreach (var vDecM in varDecsM)
                        {
                            string varName = vDecM.Variables.FirstOrDefault()?.Identifier.Text;

                            if (!string.IsNullOrEmpty(varName) && !varName.StartsWith("_") && char.IsLower(varName[0]) && (varName != "var"))
                            {
                                if (addedVars.Add(varName))
                                {
                                    updateVarCountMulti(varCount, varName, fileName);
                                }
                            }
                        }
                    }
                    // VAR DECS IN METHOD END
                    addedVars.Clear();
                    addedVarsDecs.Clear();
                }

                // VAR DECS IN ROOT START
                if (countVarsDeclarationsAlso == true)
                {
                    var varDecsR = root.DescendantNodes().OfType<VariableDeclarationSyntax>();

                    foreach (var vDecR in varDecsR)
                    {
                        string varName = vDecR.Variables.FirstOrDefault()?.Identifier.Text;

                        if (!string.IsNullOrEmpty(varName) && !varName.StartsWith("_") && char.IsLower(varName[0]) && (varName != "var"))
                        {
                            if (addedVars.Add(varName))
                            {
                                updateVarCountMulti(varCount, varName, fileName);
                            }
                        }
                    }
                }
                addedVars.Clear();
                addedVarsDecs.Clear();
                // VAR DECS IN ROOT END

                // VAR USAGES & DECS END
            }

            // Count Data Commonality and output it into dataGridView
            label2.Text = $"Total Number of Modules : {totalModules}";
            mergeVarCount(varCount);
            var dupe = new HashSet<string>();
            foreach (var varName in varCount.Keys)
            {
                foreach (var count in varCount[varName].Values)
                {
                    double dataCommonality = ((double)count / totalModules) * 100;
                    string key = string.Join(", ", varCount[varName].Keys);
                    string entry = $"{key}, {varName}";

                    if (!dupe.Contains(entry))
                    {
                        dataGridView1.Rows.Add(key, varName, count, $"{dataCommonality}%");
                        dupe.Add(entry);
                    }
                }
            }
            dataGridView1.Sort(dataGridView1.Columns[2], System.ComponentModel.ListSortDirection.Descending);
        }

        public Form1()
        {
            InitializeComponent();
        }

        private void btnBrowse_Click(object sender, EventArgs e)
        {
            if (dataCommonalityMultiple == true)
            {
                CommonOpenFileDialog dialog = new CommonOpenFileDialog();
                dialog.Title = "Open Source Code Folder";
                dialog.IsFolderPicker = true;

                if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
                {
                    sourceCodeFile.Clear();

                    string folderPath = dialog.FileName;
                    csFiles = Directory.GetFiles(folderPath, "*.cs");
                }
            }
            else
            {
                OpenFileDialog dialog = new OpenFileDialog();
                dialog.Title = "Open C# Source Code";
                dialog.Filter = "C# Source Code Files|*.cs";

                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    sourceCodeSingle = dialog.FileName;
                }
            }
        }

        private void btnCheck_Click(object sender, EventArgs e)
        {
            if (dataCommonalityMultiple == true)
            {
                dataCommonalityMulti(csFiles);
            }
            else
            {
                dataCommonalitySingle(sourceCodeSingle);
            }
        }

        private void btnReset_Click(object sender, EventArgs e)
        {
            dataGridView1.Rows.Clear();
            label2.Text = string.Empty;
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            countVarsDeclarationsAlso = checkBox1.Checked;
        }

        private void checkBoxMultipleSC_CheckedChanged(object sender, EventArgs e)
        {
            dataCommonalityMultiple = checkBoxMultipleSC.Checked;
        }
    }
}