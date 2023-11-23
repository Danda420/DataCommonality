using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
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
        public string fileName;

        public void updateVarCount(Dictionary<string, int> varCount, string varName)
        {
            if (!varCount.ContainsKey(varName))
            {
                varCount[varName] = 1;
            }
            else
            {
                varCount[varName]++;
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
                string[] csFiles = Directory.GetFiles(folderPath, "*.cs");

                foreach (var file in csFiles)
                {
                    string fileName = Path.GetFileName(file);
                    string fileContent = File.ReadAllText(file);
                    sourceCodeFile.Add(fileContent);
                }
            }
        }

        private void btnCheck_Click(object sender, EventArgs e)
        {
            dataGridView1.Rows.Clear();
            label2.Text = string.Empty;
            Dictionary<string, int> varCount = new Dictionary<string, int>();
            int totalModules = 0;

            foreach (var sourceCode in sourceCodeFile)
            {
                SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(sourceCode);
                var root = syntaxTree.GetRoot();

                totalModules += root.DescendantNodes().OfType<ClassDeclarationSyntax>().Count() +
                                root.DescendantNodes().OfType<MethodDeclarationSyntax>().Count();

                dataGridView1.Rows.Clear();
                label2.Text = string.Empty;
                var fieldDec = root.DescendantNodes().OfType<FieldDeclarationSyntax>();
                var propertyDec = root.DescendantNodes().OfType<PropertyDeclarationSyntax>();
                var classDec = root.DescendantNodes().OfType<ClassDeclarationSyntax>();

                if (sourceCode == null)
                {
                    label2.Text = "No File Supplied!";
                }
                else
                {
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

                    foreach (var cDec in classDec)
                    {
                        var methodDec = cDec.DescendantNodes().OfType<MethodDeclarationSyntax>();
                        foreach (var mDec in methodDec)
                        {
                            var addedVars = new HashSet<string>();

                            var varUsage = mDec.DescendantNodes().OfType<IdentifierNameSyntax>();
                            foreach (var usage in varUsage)
                            {
                                string varName = usage.Identifier.Text;

                                if (!varName.StartsWith("_") && char.IsLower(varName[0]) && (varName != "var"))
                                {
                                    if (addedVars.Add(varName))
                                    {
                                        updateVarCount(varCount, varName);
                                    }
                                }
                            }
                        }
                    }

                    label2.Text = "Total Number of Modules : " + totalModules.ToString();
                    foreach (var a in varCount)
                    {
                        float dataCommonality = ((float)a.Value / totalModules) * 100;
                        dataGridView1.Rows.Add(a.Key, a.Value, $"{dataCommonality}%");
                    }
                }

            }
        }

        private void btnReset_Click(object sender, EventArgs e)
        {
            dataGridView1.Rows.Clear();
            label2.Text = string.Empty;
        }
    }
}