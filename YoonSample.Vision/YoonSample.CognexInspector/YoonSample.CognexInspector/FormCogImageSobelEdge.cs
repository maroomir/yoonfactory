﻿using Cognex.VisionPro;
using Cognex.VisionPro.ImageProcessing;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using YoonFactory.Cognex.Tool;
using YoonFactory;

namespace YoonSample.CognexInspector
{
    public partial class Form_CogImageSobelEdge : Form
    {
        public eYoonCognexType ToolType = eYoonCognexType.Sobel;
        public eLabelInspect CogToolLabel;
        public CogSobelEdgeTool CogTool;
        public CogImage8Grey CogImageSource;
        public event PassCogToolCallback OnUpdateCogToolEvent;

        public Form_CogImageSobelEdge()
        {
            InitializeComponent();
        }

        private void Form_CogImageSobelEdge_Load(object sender, EventArgs e)
        {
            if (CogImageSource == null || CogTool == null)
            {
                Close();
                return;
            }

            cogSobelEdgeEditV2.Subject = CogTool;
            cogSobelEdgeEditV2.Subject.InputImage = CogImageSource;
        }

        private void Form_CogImageSobelEdge_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (CogImageSource == null || CogTool == null)
                return;

            DialogResult result = MessageBox.Show("Save This Cognex Tool?", "", MessageBoxButtons.YesNo);

            if (result == DialogResult.Yes)
            {
                ////  Working 영역에 Vpp File 별도 저장 (백업 및 확인용)
                string strFilePath = Path.Combine(CommonClass.strCurrentWorkingDirectory, string.Format("{0}.vpp", ToolType.ToString()));
                if (ToolFactory.SaveCognexToolToVpp(CogTool, strFilePath))
                    CogTool = ToolFactory.LoadCognexToolFromVpp(strFilePath) as CogSobelEdgeTool;
                if (CogTool == null) return;
                ////  Main Form에 Cognex Tool 전달
                OnUpdateCogToolEvent(this, new CogToolArgs(ToolType, CogToolLabel, CogTool, CogTool.Result.EdgeMagnitudeImage));
                Thread.Sleep(100);
            }
        }
    }
}
