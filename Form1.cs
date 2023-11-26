using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Windows.Forms;
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

        private void updateVarCount(Dictionary<string, Dictionary<string, int>> varCountDict, string varName)
        {
            if (!varCountDict.ContainsKey(varName))
            {
                varCountDict[varName] = new Dictionary<string, int>();
            }

            if (!varCountDict[varName].ContainsKey(""))
            {
                varCountDict[varName][""] = 0;
            }

            varCountDict[varName][""]++;
        }

        private void dataCommonalityFolder(IEnumerable<string> csFiles)
        {
            dataGridView1.Rows.Clear();
            label2.Text = string.Empty;
            int totalModules = 0;

            if (csFiles == null)
            {
                label2.Text = "No folder selected!";
                return;
            }

            foreach (var fp in csFiles)
            {
                string sourceCode = File.ReadAllText(fp);

                SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(sourceCode);
                var root = syntaxTree.GetRoot();

                var classDec = root.DescendantNodes().OfType<ClassDeclarationSyntax>();
                var methodDec = root.DescendantNodes().OfType<MethodDeclarationSyntax>()
                                    .Where(m => m.Modifiers.Any(SyntaxKind.PrivateKeyword));

                totalModules += classDec.Count() + methodDec.Count();
            }

            foreach (var filePath in csFiles)
            {
                string fileName = Path.GetFileNameWithoutExtension(filePath);
                string sourceCode = File.ReadAllText(filePath);

                SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(sourceCode);
                var root = syntaxTree.GetRoot();

                var classDec = root.DescendantNodes().OfType<ClassDeclarationSyntax>();
                var methodDec = root.DescendantNodes().OfType<MethodDeclarationSyntax>()
                                    .Where(m => m.Modifiers.Any(SyntaxKind.PrivateKeyword));

                Dictionary<string, Dictionary<string, int>> varCount = new Dictionary<string, Dictionary<string, int>>();

                foreach (var cDec in classDec)
                {
                    var fieldDec = cDec.DescendantNodes().OfType<FieldDeclarationSyntax>();
                    var propertyDec = cDec.DescendantNodes().OfType<PropertyDeclarationSyntax>();

                    foreach (var fDec in fieldDec)
                    {
                        var varDec = fDec.DescendantNodes().OfType<VariableDeclaratorSyntax>();
                        foreach (var vDec in varDec)
                        {
                            string varName = vDec.Identifier.Text;

                            if (!varName.StartsWith("_") && char.IsLower(varName[0]) && (varName != "var"))
                            {
                                updateVarCount(varCount, varName);
                            }
                        }
                    }

                    foreach (var pDec in propertyDec)
                    {
                        string varName = pDec.Identifier.Text;
                        updateVarCount(varCount, varName);
                    }
                }

                foreach (var mDec in methodDec)
                {
                    var varDec = mDec.DescendantNodes().OfType<VariableDeclarationSyntax>();
                    var addedVars = new HashSet<string>();
                    foreach (var vDec in varDec)
                    {
                        string varName = vDec.Variables.FirstOrDefault()?.Identifier.Text;
                        string typeName = vDec.Type.ToString();

                        if (!string.IsNullOrEmpty(varName) && !varName.StartsWith("_") && char.IsLower(varName[0]) && (varName != "var"))
                        {
                            if (addedVars.Add(varName))
                            {
                                updateVarCount(varCount, varName);
                            }
                        }
                    }

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
                }

                label2.Text = $"Total Number of Modules : {totalModules}";
                foreach (var a in varCount)
                {
                    string varName = a.Key;
                    foreach (var typePair in a.Value)
                    {
                        string typeName = typePair.Key;
                        int count = typePair.Value;

                        double dataCommonality = ((double)count / totalModules) * 100;
                        dataGridView1.Rows.Add(fileName, varName, count, $"{dataCommonality}%");
                    }
                }
                dataGridView1.Sort(dataGridView1.Columns[2], System.ComponentModel.ListSortDirection.Descending);
            }
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
                sourceCodeFile.Clear();

                string folderPath = dialog.FileName;
                csFiles = Directory.GetFiles(folderPath, "*.cs");
            }
        }

        private void btnCheck_Click(object sender, EventArgs e)
        {
            dataCommonalityFolder(csFiles);
        }

        private void btnReset_Click(object sender, EventArgs e)
        {
            dataGridView1.Rows.Clear();
            label2.Text = string.Empty;
        }
    }
}