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

namespace DataCommonalityChecker
{
    public partial class Form1 : Form
    {
        public string sourceCode;

        public void UpdateVariableCount(Dictionary<string, int> varCount, string varName)
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
            OpenFileDialog dialog = new OpenFileDialog();
            dialog.Title = "Open Source Code File";

            if (dialog.ShowDialog() == DialogResult.OK)
            {
                sourceCode = File.ReadAllText(dialog.FileName);
                
            }
        }

        private void btnCheck_Click(object sender, EventArgs e)
        {
            dataGridView1.Rows.Clear();
            if (sourceCode == null)
            {
                dataGridView1.Rows.Add("No File Supplied!", "No File Supplied!", "No File Supplied!", "No File Supplied!");
            }
            else
            {
                SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(sourceCode);
                var root = syntaxTree.GetRoot();

                Dictionary<string, int> varCount = new Dictionary<string, int>();

                var fieldDec = root.DescendantNodes().OfType<FieldDeclarationSyntax>();
                foreach (var fDec in fieldDec)
                {
                    foreach (var variable in fDec.Declaration.Variables)
                    {
                        string varName = variable.Identifier.Text;
                        UpdateVariableCount(varCount, varName);
                    }
                }

                var propertyDec = root.DescendantNodes().OfType<PropertyDeclarationSyntax>();
                foreach (var pDec in propertyDec)
                {
                    string varName = pDec.Identifier.Text;
                    UpdateVariableCount(varCount, varName);
                }

                var methodDec = root.DescendantNodes().OfType<MethodDeclarationSyntax>();
                foreach (var mDec in methodDec)
                {
                    var localDec = mDec.DescendantNodes().OfType<VariableDeclarationSyntax>();
                    foreach (var localVar in localDec)
                    {
                        foreach (var variable in localVar.Variables)
                        {
                            string varName = variable.Identifier.Text;
                            UpdateVariableCount(varCount, varName);
                        }
                    }
                }

                foreach (var kvp in varCount)
                {
                    dataGridView1.Rows.Add(kvp.Key, kvp.Value);
                }
            }
        }

        private void btnReset_Click(object sender, EventArgs e)
        {
            dataGridView1.Rows.Clear();
        }
    }
}
