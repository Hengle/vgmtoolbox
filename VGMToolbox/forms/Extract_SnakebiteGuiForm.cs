﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Drawing;
using System.IO;
using System.Text;
using System.Windows.Forms;

using VGMToolbox.plugin;
using VGMToolbox.tools.extract;

namespace VGMToolbox.forms
{
    public partial class Extract_SnakebiteGuiForm : AVgmtForm
    {
        public Extract_SnakebiteGuiForm(TreeNode pTreeNode) : base(pTreeNode)
        {
            InitializeComponent();

            this.lblTitle.Text = ConfigurationSettings.AppSettings["Form_SnakebiteGUI_Title"];
            this.tbOutput.Text = ConfigurationSettings.AppSettings["Form_SnakebiteGUI_IntroText"];
            this.btnDoTask.Text = ConfigurationSettings.AppSettings["Form_SnakebiteGUI_BtnDoTask"];

            this.grpFiles.Text = ConfigurationSettings.AppSettings["Form_SnakebiteGUI_GrpFiles"];
            this.lblSourceFiles.Text = ConfigurationSettings.AppSettings["Form_SnakebiteGUI_LblSourceFiles"];
            this.lblDragNDrop.Text = ConfigurationSettings.AppSettings["Form_SnakebiteGUI_LblDragNDrop"];
            this.lblOutputFile.Text = ConfigurationSettings.AppSettings["Form_SnakebiteGUI_LblOutputFile"];
            this.grpOptions.Text = ConfigurationSettings.AppSettings["Form_SnakebiteGUI_GrpOptions"];
            this.lblStartAddress.Text = ConfigurationSettings.AppSettings["Form_SnakebiteGUI_LblStartAddress"];
            this.rbEndAddress.Text = ConfigurationSettings.AppSettings["Form_SnakebiteGUI_RbEndAddress"];
            this.rbLength.Text = ConfigurationSettings.AppSettings["Form_SnakebiteGUI_RbLength"];
            this.rbEndOfFile.Text = ConfigurationSettings.AppSettings["Form_SnakebiteGUI_RbEndOfFile"];

            this.rbEndAddress.Checked = true;
        }

        private void btnBrowseSource_Click(object sender, EventArgs e)
        {
            this.tbSourceFiles.Text = base.browseForFile(sender, e);
        }

        private void btnBrowseOutput_Click(object sender, EventArgs e)
        {
            this.tbOutputFile.Text = base.browseForFileToSave(sender, e);
        }

        private void setRadioButtons()
        {
            if (rbEndAddress.Checked)
            {
                tbEndAddress.Enabled = true;
                tbEndAddress.ReadOnly = false;

                tbLength.Enabled = false;
                tbLength.ReadOnly = true;
            }
            else if (rbLength.Checked)
            {
                tbEndAddress.Enabled = false;
                tbEndAddress.ReadOnly = true;

                tbLength.Enabled = true;
                tbLength.ReadOnly = false;
            }
            else if (rbEndOfFile.Checked)
            {
                tbEndAddress.Enabled = false;
                tbEndAddress.ReadOnly = true;

                tbLength.Enabled = false;
                tbLength.ReadOnly = true;
            }
        }

        private void rbEndAddress_CheckedChanged(object sender, EventArgs e)
        {
            this.setRadioButtons();
        }

        private void rbLength_CheckedChanged(object sender, EventArgs e)
        {
            this.setRadioButtons();
        }

        private void rbEndOfFile_CheckedChanged(object sender, EventArgs e)
        {
            this.setRadioButtons();
        }

        protected override void doDragEnter(object sender, DragEventArgs e)
        {
            base.doDragEnter(sender, e);
        } 

        protected override IVgmtBackgroundWorker getBackgroundWorker()
        {
            return new SimpleCutterSnakebiteWorker();
        }
        protected override string getCancelMessage()
        {
            return ConfigurationSettings.AppSettings["Form_SnakebiteGUI_MessageCancel"];
        }
        protected override string getCompleteMessage()
        {
            return ConfigurationSettings.AppSettings["Form_SnakebiteGUI_MessageComplete"];
        }
        protected override string getBeginMessage()
        {
            return ConfigurationSettings.AppSettings["Form_SnakebiteGUI_MessageBegin"];
        }

        private void tbSourceFiles_DragDrop(object sender, DragEventArgs e)
        {
            bool cutFiles = false;
            string warningMessage = 
                ConfigurationSettings.AppSettings["Form_SnakebiteGUI_ErrorSingleFile"];
            
            string[] s = (string[])e.Data.GetData(DataFormats.FileDrop, false);

            if ((s.Length > 1) ||
                ((s.Length == 1) && (Directory.Exists(s[0]))))
            {
                MessageBox.Show(warningMessage, 
                    ConfigurationSettings.AppSettings["Form_Global_ErrorWindowTitle"]);
            }
            else
            {
                cutFiles = true;
            }

            if (cutFiles)
            { 
                this.cutTheFile(s);
            }
        }

        private void cutTheFile(string[] pPaths)
        {
            if (this.validateInputs())
            {
                SimpleCutterSnakebiteWorker.SimpleCutterSnakebiteStruct snbStruct =
                    new SimpleCutterSnakebiteWorker.SimpleCutterSnakebiteStruct();

                snbStruct.EndAddress = this.tbEndAddress.Text;
                snbStruct.Length = this.tbLength.Text;
                snbStruct.OutputFile = this.tbOutputFile.Text;
                snbStruct.SourcePaths = pPaths;
                snbStruct.StartOffset = this.tbStartAddress.Text;
                snbStruct.UseEndAddress = this.rbEndAddress.Checked;
                snbStruct.UseFileEnd = this.rbEndOfFile.Checked;
                snbStruct.UseLength = this.rbLength.Checked;

                base.backgroundWorker_Execute(snbStruct);
            }
        }

        private void btnDoTask_Click(object sender, EventArgs e)
        {
            string[] s = new string[] { this.tbSourceFiles.Text };
            this.cutTheFile(s);
        }

        private bool validateInputs()
        {
            bool ret = true;

            ret &= base.checkFileExists(this.tbSourceFiles.Text, this.lblSourceFiles.Text);
            ret &= base.checkTextBox(this.tbStartAddress.Text, this.lblStartAddress.Text);

            if (rbEndAddress.Checked)
            {
                ret &= base.checkTextBox(this.tbEndAddress.Text, this.rbEndAddress.Text);
            }

            if (rbLength.Checked)
            {
                ret &= base.checkTextBox(this.tbLength.Text, this.rbLength.Text);
            }

            if (this.tbSourceFiles.Text.Equals(this.tbOutputFile.Text))
            {
                MessageBox.Show(ConfigurationSettings.AppSettings["Form_SnakebiteGUI_ErrorInputOutputSame"],
                    ConfigurationSettings.AppSettings["Form_Global_ErrorWindowTitle"]);
                ret = false;
            }

            return ret;
        }
    }
}