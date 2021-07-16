﻿//  Copyright (c)2018 by cheoljoung.yoon@lge.com

using System;
using System.IO;
using System.Drawing;
using System.Drawing.Imaging;
using System.Windows.Forms;

namespace YoonFactory.Viewer
{
    public partial class ImageViewer : UserControl
    {
        const double _PI = 1.57079632679489661923;

        public delegate void OnPixelCallback(object sender, PixelArgs e);                 //해당 픽셀정보를 알려줄 대리자   
        public delegate void OnMeasureCallback(object sender, MeasureArgs e);             //영상 측정정보를 알려줄 대리자
        public delegate void OnGuidePointCallback(object sender, PointArgs e);            //해당 위치정보를 알려줄 대리자   
        public event OnMeasureCallback OnMeasurement;                                     //영상 측정정보를 알려줄 이벤트   
        public event OnPixelCallback OnGetPixel;                                          //해당 픽셀정보를 알려줄 이벤트   
        public event OnGuidePointCallback OnGetGuidePoint;                                //해당 위치정보를 알려줄 이벤트

        private int m_nImageWidth = 0;                                                //Draw Image Width
        private int m_nImageHeight = 0;                                               //Draw Image Height
        private Image m_pDummyImage = null;                                           //Double Buffering Image
        private Graphics m_pDummyGrphics;                                             //Double Buffering Graphics
        private byte[] m_pDummyProfile = null;                                      //Profile 정보 담기
        private Point m_posMouse = new Point(0, 0);                                //Mouse Click 위치
        private Point m_posMouseCenterZoom = new Point(0, 0);                      //Zoom시 Mouse 중심으로 이동
        private Point m_posMeasureStart = new Point(0, 0);                         //Measure 시작점
        private Point m_posMeasureEnd = new Point(0, 0);                           //Measure 끝점
        private Point m_posMouseDragStart = new Point(0, 0);                       //Mouse Drag Start Position
        private Point m_posMouseDragEnd = new Point(0, 0);                         //Mouse Drag End Position

        private bool m_bMeasureHorz = true;
        private bool m_bMeasureVert = false;
        private bool m_bMouseLeft = false;
        private bool m_bMouseRight = false;

        private Image m_pImage = null;                                              //Viewer에 Setting 된 Image
        private bool m_bEnabledDraw = false;                                        //Draw 여부
        private double m_dZoom = 0.0;                                               //Zoom (0.0:Fix to Scrren, 0.5, 1, 2, 4, 8)
        private double m_dZoomX = 0.0;                                              //Fix 옵션일 경우 X Zoom
        private double m_dZoomY = 0.0;                                              //Fix 옵션일 경우 Y Zoom
        private PixelFormat m_nPixelFormat = PixelFormat.Format8bppIndexed;         //Pixel Format
        private int m_nBitCount = 8;                                                //BitCount 
        private Point m_posScroll;                                            //Scroll Position
        private bool m_bDefectROI = false;                                          //Defect 그리기
        private bool m_bEnbaledROI = false;                                           //ROI 그리기
        private bool m_bMeasure = false;                                            //영상측정
        private bool m_bProfile = false;                                            //Profile 그리기
        private bool m_bGuideLine = false;                                          //Guide Line 그리기
        private bool m_bObservationROI = false;                                     //휘도 영역 그리기 여부
        private bool m_bRoiDraw = false;                                            // 수동 ROI 그리기 
        private Rectangle m_rectROI;                                               // ROI 영역
        private Point m_posStart;
        private Point m_posCurrent;
        private bool m_bDrawing;
        private int m_nKindView = 0;

        #region property
        public int KindView
        {
            get { return m_nKindView; }
            set { m_nKindView = value; }
        }

        public Image InputImage
        {
            set { m_pImage = value; }
        }

        public bool IsEnabledDraw
        {
            get { return m_bEnabledDraw; }
            set { m_bEnabledDraw = value; }
        }
        public double Zoom
        {
            get { return m_dZoom; }
            set
            {
                vScrollBar.Value = 0;
                hScrollBar.Value = 0;

                m_dZoom = value;
                m_dZoomX = value;
                m_dZoomY = value;
            }
        }
        public double ZoomX
        {
            get { return m_dZoomX; }
        }
        public double ZoomY
        {
            get { return m_dZoomY; }
        }
        public PixelFormat PixFormat
        {
            get { return m_nPixelFormat; }
            set { m_nPixelFormat = value; }
        }
        public int BitCount
        {
            get { return m_nBitCount; }
            set { m_nBitCount = value; }
        }
        public Point ScrollPosition
        {
            get { return m_posScroll; }
            set { m_posScroll = value; }
        }
        public Point RealMousePosition
        {
            get
            {
                Point Pos = this.PointToClient(Control.MousePosition);
                return new Point((int)(Pos.X / m_dZoomX + 0.5) + hScrollBar.Value, (int)(Pos.Y / m_dZoomY + 0.5) + vScrollBar.Value);
            }
        }
        public bool IsDefectROI
        {
            get { return m_bDefectROI; }
            set { m_bDefectROI = value; }
        }
        public bool IsEnabledROI
        {
            get { return m_bEnbaledROI; }
            set
            {
                m_bEnbaledROI = value;
                m_bRoiDraw = value;
            }
        }
        public bool IsMeasure
        {
            get { return m_bMeasure; }
            set
            {
                m_bMeasure = value;
                m_posMeasureStart = m_posMeasureEnd = new Point(0, 0);           //Measure 초기화
            }
        }
        public bool IsProfile
        {
            get { return m_bProfile; }
            set { m_bProfile = value; }
        }
        public bool IsGuideLine
        {
            get { return m_bGuideLine; }
            set { m_bGuideLine = value; }
        }
        public bool IsObservationROI
        {
            get { return m_bObservationROI; }
            set { m_bObservationROI = value; }
        }
        public bool IsROIDraw
        {
            get { return m_bRoiDraw; }
            set { m_bRoiDraw = value; }
        }
        public Point StartPos
        {
            get { return m_posStart; }
            set { m_posStart = value; }
        }
        public Point CurrentPos
        {
            get { return m_posCurrent; }
            set { m_posCurrent = value; }
        }
        public bool IsDrawing
        {
            get { return m_bDrawing; }
            set { m_bDrawing = value; }
        }
        public Rectangle ROI
        {
            get { return m_rectROI; }
            set { m_rectROI = value; }
        }
        #endregion

        /**************************************************************************************************************/
        /*    각 Position 계산                                                                                        */
        /**************************************************************************************************************/
        public Point RealToScreen(Point realPoint)
        {
            return new Point((int)((realPoint.X - hScrollBar.Value) * m_dZoomX + 0.5), (int)((realPoint.Y - vScrollBar.Value) * m_dZoomY + 0.5));
        }

        public Size RealToScreen(Size realSize)
        {
            return new Size((int)(realSize.Width * m_dZoomX + 0.5), (int)(realSize.Height * m_dZoomY + 0.5));
        }

        public Rectangle RealToScreen(Rectangle realRect)
        {
            return new Rectangle(RealToScreen(realRect.Location), RealToScreen(realRect.Size));
        }

        /**************************************************************************************************************/
        /*    영상측정 정보                                                                                           */
        /**************************************************************************************************************/
        double AngleCal(Point start, Point end)
        {
            return Math.Atan2((double)end.Y - start.Y, (double)end.X - start.X);      //두점 각도(첫번째 점이 시작점)
        }

        Point RotateSegment(double d, double theta)
        {
            double x = d * Math.Cos(theta);                               //원점중심으로 (d, 0)점이 theta이동 했을 경우의 위치
            double y = d * Math.Sin(theta);

            return new Point((int)(x + .5), (int)(y + .5));
        }

        /**************************************************************************************************************/
        /*    선택한 포지션으로 이동                                                                                   */
        /**************************************************************************************************************/
        public void MoveSelectPosition(int left, int top, int right, int bottom)
        {
            //fZoom == 0.0f
            if (m_dZoom == 0.0f || toolStripMenuItem_FixToScreen.Checked == true)
            {
                MessageBox.Show("Image of Zoom ratio Fix To Screen If you can not move to the Defect.", "Zoom Error", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            hScrollBar.Value = 0;
            vScrollBar.Value = 0;

            //-- 실제 이동할 위치를 계산한다. ------------------------------------------------------------------------//
            Point movPoint = new Point(left + ((right - left) / 2), top + ((bottom - top) / 2));
            //--------------------------------------------------------------------------------------------------------//

            //-- Scroll Bar 이동 계산 --------------------------------------------------------------------------------//
            Rectangle dcRect = ClientRectangle;

            int xScroll = movPoint.X - (int)((dcRect.Width / m_dZoom + .5) / 2);
            int yScroll = movPoint.Y - (int)((dcRect.Height / m_dZoom + .5) / 2);
            //--------------------------------------------------------------------------------------------------------//

            //-- Scroll Bar 좌상단, 우하단 일 경우 이동 계산 ---------------------------------------------------------//
            if (xScroll < 0) xScroll = 0;
            if (yScroll < 0) yScroll = 0;

            if (movPoint.X + (int)((dcRect.Width / m_dZoom + .5) / 2) > hScrollBar.Maximum) xScroll = hScrollBar.Maximum - (int)(dcRect.Width / m_dZoom + .5);
            if (movPoint.Y + (int)((dcRect.Height / m_dZoom + .5) / 2) > vScrollBar.Maximum) yScroll = vScrollBar.Maximum - (int)(dcRect.Height / m_dZoom + .5);
            //--------------------------------------------------------------------------------------------------------//

            yScroll = top;

            if (xScroll < 0) xScroll = 0;
            if (yScroll < 0) yScroll = 0;

            //-- Scroll Bar 이동 -------------------------------------------------------------------------------------//
            hScrollBar.Value = xScroll;
            vScrollBar.Value = yScroll;
            //--------------------------------------------------------------------------------------------------------//
        }

        /**************************************************************************************************************/
        /*    Set Double Buffering / Set Iamge Size                                                                   */
        /**************************************************************************************************************/
        public void SetDoubleBuffering()
        {
            //-- Dummy HDC 공간 만들기 -------------------------------------------------------------------------------//
            Rectangle dumRc = new Rectangle();
            dumRc = this.ClientRectangle;

            m_pDummyImage = (Image)new Bitmap(dumRc.Width, dumRc.Height);
            m_pDummyGrphics = Graphics.FromImage(m_pDummyImage);
            //--------------------------------------------------------------------------------------------------------//
        }

        public void SetImageSize(int width, int height)
        {
            m_nImageWidth = width;               // 원본 이미지 width
            m_nImageHeight = height;              // 원본 이미지 height
        }

        /**************************************************************************************************************/
        /*    생성자 - 초기화                                                                                         */
        /**************************************************************************************************************/
        public ImageViewer()
        {
            InitializeComponent();

            this.m_bEnabledDraw = false;                                 //Draw 여부
            this.m_dZoom = 0.0;                                          //Zoom (0.0:Fix to Scrren, 0.5, 1, 2, 4, 8)
            this.m_dZoomX = 0.0;                                         //Fix 옵션일 경우 X Zoom
            this.m_dZoomY = 0.0;                                         //Fix 옵션일 경우 Y Zoom
            this.m_nPixelFormat = PixelFormat.Format8bppIndexed;         //Pixel Format
            this.m_nBitCount = 8;                                        //BitCount 
            this.m_posScroll = new Point(0, 0);                    //Scroll Position
            this.m_bDefectROI = false;                                   //Defect 그리기
            this.m_bEnbaledROI = false;                                    //ROI 그리기
            this.m_bMeasure = false;                                     //영상측정
            this.m_bProfile = false;                                     //Profile 그리기
            this.m_bGuideLine = false;                                   //Guide Line 그리기
            this.m_bObservationROI = false;                              //휘도 영역 그리기
            this.m_bRoiDraw = false;

            this.vScrollBar.Enabled = false;
            this.hScrollBar.Enabled = false;

            //-- Double Buffering Option -----------------------------------------------------------------------------//
            this.SetStyle(ControlStyles.DoubleBuffer, true);
            this.SetStyle(ControlStyles.AllPaintingInWmPaint, true);
            this.SetStyle(ControlStyles.UserPaint, true);
            //--------------------------------------------------------------------------------------------------------//

            //-- Test 용 Source
#if DEBUG
            //OnTestProcess();
#endif
        }

        private void OnTestProcess()
        {
            this.InputImage = new Bitmap(Path.Combine(Directory.GetCurrentDirectory(), @"Sample", @"Sample1.bmp"));
            this.IsEnabledDraw = true;
            this.SetImageSize(m_pImage.Width, m_pImage.Height);
            this.SetDoubleBuffering();
            this.DrawImage();
        }

        /**************************************************************************************************************/
        /*    Draw                                                                                                    */
        /**************************************************************************************************************/
        private enum eStepROIDraw
        {
            INIT=0,
            DRAW,
            LABELING,
            EXIT,
        }

        private void ImageViewer_Paint(object sender, PaintEventArgs e)
        {
            if (IsEnabledDraw)
            {
                //-- 원본 영상 그리기 --------------------------------------------------------------------------------//
                Graphics g = e.Graphics;
                g.DrawImage(m_pDummyImage, new Point(0, 0));

                #region ROI_DRAWING
                if (IsROIDraw)
                {
                    Pen rPen = new Pen(Color.Red, 1);

                    Font drawFont = new Font("Arial", 9, FontStyle.Bold);
                    SolidBrush drawBrush = new SolidBrush(Color.Aqua);

                    Rectangle rect = new Rectangle(
                    Math.Min(StartPos.X, CurrentPos.X),
                    Math.Min(StartPos.Y, CurrentPos.Y),
                    Math.Abs(StartPos.X - CurrentPos.X),
                    Math.Abs(StartPos.Y - CurrentPos.Y));

                    Point posStart = RealToScreen(new Point(rect.Left, rect.Top));                              //실제 Start Point
                    Point posEnd = RealToScreen(new Point(rect.Left + rect.Width, rect.Top + rect.Height));       //실제 End Point

                    g.DrawRectangle(rPen, posStart.X, posStart.Y, posEnd.X - posStart.X, posEnd.Y - posStart.Y);
                }
                #endregion

                //-- ROI 그리기 --------------------------------------------------------------------------------------//
                #region ROI_DISPLAY
                if (IsEnabledROI)
                {
                    Pen rPen = new Pen(Color.Yellow, 1);
                    Pen rPenR = new Pen(Color.Lime, 1);
                    Pen rPenA = new Pen(Color.Red, 1);
                    Pen rPenM = new Pen(Color.Green, 2);

                    Font drawFont = new Font("Arial", 9, FontStyle.Bold);
                    SolidBrush drawBrush = new SolidBrush(Color.Red);

                    bool isROIDrawing = true;
                    Rectangle rect = new Rectangle(m_rectROI.Left, m_rectROI.Top, m_rectROI.Width, m_rectROI.Height);
                    eStepROIDraw jobStep = eStepROIDraw.INIT;
                    Point posStart = new Point(); Point posEnd = new Point();
                    while (isROIDrawing)
                    {
                        switch (jobStep)
                        {
                            case eStepROIDraw.INIT:
                                if (rect.Left == 0 || rect.Top == 0) jobStep = eStepROIDraw.EXIT;
                                else jobStep = eStepROIDraw.DRAW;
                                break;
                            case eStepROIDraw.DRAW:
                                posStart = RealToScreen(new Point(rect.Left, rect.Top));                        // 실제 Start Point
                                posEnd = RealToScreen(new Point(rect.Left + rect.Width, rect.Top + rect.Height)); // 실제 End Point
                                g.DrawRectangle(rPen, posStart.X, posStart.Y, posEnd.X - posStart.X, posEnd.Y - posStart.Y);
                                jobStep = eStepROIDraw.LABELING;
                                break;
                            case eStepROIDraw.LABELING:
                                PointF posDraw = new PointF((float)posStart.X, (float)posStart.Y - 15);
                                string strDraw = "ROI";
                                g.DrawString(strDraw, drawFont, drawBrush, posDraw);
                                jobStep = eStepROIDraw.EXIT;
                                break;
                            case eStepROIDraw.EXIT:
                                isROIDrawing = false;
                                break;
                        }
                    }
                }
                #endregion
                //----------------------------------------------------------------------------------------------------//

                //-- Profile 그리기 ----------------------------------------------------------------------------------//
                #region PROFILE_DISPLAY
                if (IsProfile)
                {
                    if (m_pDummyProfile == null) return;

                    Pen pPen = new Pen(Color.Yellow, 2);
                    Point posOld = new Point(0, 0);
                    for (int i = 0; i < m_pDummyImage.Width; i++)                    // 화면에 보이는 Bitmap 크기
                    {
                        Point posNew = new Point(i, m_pDummyProfile[i]);
                        g.DrawLine(pPen, posOld, posNew);                        // 프로파일 그리기
                        posOld = posNew;
                    }

                    Pen gPen = new Pen(Color.WhiteSmoke);
                    gPen.DashStyle = System.Drawing.Drawing2D.DashStyle.Dot;

                    Font drawFont = new Font("Arial", 9, FontStyle.Bold);
                    SolidBrush drawBrush = new SolidBrush(Color.Yellow);

                    for (int m = 0; m < 12; m++)
                    {
                        g.DrawLine(gPen, new Point(0, m * 20), new Point(m_pDummyImage.Width, m * 20));

                        PointF drawPos = new PointF((float)m_pDummyImage.Width - 70, (m * 20) + 2);
                        string drawStr = Convert.ToString(m * 20);
                        g.DrawString(drawStr, drawFont, drawBrush, drawPos);
                    }
                    g.DrawLine(gPen, new Point(0, 255), new Point(m_pDummyImage.Width, 255));

                    PointF drawPosLast = new PointF((float)m_pDummyImage.Width - 70, 255 + 2);

                    g.DrawString("255", drawFont, drawBrush, drawPosLast);
                }
                #endregion
                //----------------------------------------------------------------------------------------------------//

                //-- 가이드 라인 그리기 ------------------------------------------------------------------------------//
                #region GUIDE_LINE_DISPLAY
                if (IsGuideLine)
                {
                    Pen gPen = new Pen(Color.Aqua);
                    g.DrawLine(gPen, new Point(0, m_posMouse.Y), new Point(m_pDummyImage.Width, m_posMouse.Y));     //가이드라인 X 그리기
                    g.DrawLine(gPen, new Point(m_posMouse.X, 0), new Point(m_posMouse.X, m_pDummyImage.Height));    //가이드라인 Y 그리기
                }
                #endregion
                //----------------------------------------------------------------------------------------------------//

                //-- Measure 그리기 ----------------------------------------------------------------------------------//
                #region MEASURE_DISPLAY
                if (m_bMeasure)
                {
                    double angle = 0.0;
                    Point ptGuideStart = new Point(0, 0);
                    Point ptGuideEnd = new Point(0, 0);

                    Point ptStart = m_posMeasureStart;                            //드레그 시작 포인트
                    Point ptEnd = m_posMeasureEnd;                              //드레그 종료 포인트 

                    angle = AngleCal(m_posMeasureStart, m_posMeasureEnd);

                    ptGuideStart = RotateSegment(10, angle + _PI);               //시작 가이드 선길이 및 각도
                    ptGuideEnd = RotateSegment(10, angle - _PI);               //끝 가이드 선길이 및 각도

                    Pen pen = new Pen(Color.Aqua);

                    Point guideStart1 = new Point(ptGuideStart.X + ptStart.X, ptGuideStart.Y + ptStart.Y);
                    Point guideEnd1 = new Point(ptGuideEnd.X + ptStart.X, ptGuideEnd.Y + ptStart.Y);
                    Point guideStart2 = new Point(ptGuideStart.X + ptEnd.X, ptGuideStart.Y + ptEnd.Y);
                    Point guideEnd2 = new Point(ptGuideEnd.X + ptEnd.X, ptGuideEnd.Y + ptEnd.Y);

                    g.DrawLine(pen, guideStart1, guideEnd1);                     //시작점의 가이드선 그리기
                    g.DrawLine(pen, ptStart, ptEnd);                         //실제 Measure Line 그리기
                    g.DrawLine(pen, guideStart2, guideEnd2);                     //끝점의 가이드선 그리기

                    //-- 측정거리 이벤트로 알려줌 -------------------------------------------------------------------//
                    int xlen = 0;
                    int ylen = 0;

                    if (m_dZoom == 0.0)
                    {
                        xlen = (int)((m_posMeasureEnd.X - m_posMeasureStart.X) / m_dZoomX + .5);
                        ylen = (int)((m_posMeasureEnd.Y - m_posMeasureStart.Y) / m_dZoomY + .5);
                    }
                    else
                    {
                        xlen = (int)((m_posMeasureEnd.X - m_posMeasureStart.X) / m_dZoom + .5);
                        ylen = (int)((m_posMeasureEnd.Y - m_posMeasureStart.Y) / m_dZoom + .5);
                    }

                    double dist = Math.Sqrt(Math.Pow((double)xlen, 2) + Math.Pow((double)ylen, 2));

                    // 이곳에 화면에 거리값 표현 구현
                    Point PoViewDis = new Point(guideEnd2.X + 17, guideEnd2.Y + 5);
                    Pen rPen = new Pen(Color.Yellow, 1);
                    Font drawFont = new Font("Arial", 15, FontStyle.Bold);
                    SolidBrush drawBrush = new SolidBrush(Color.Yellow);
                    string drawStr = "";
                    drawStr = string.Format("{0:0.000}pixel  {1:0.00}º", dist, -1 * angle * 180.0 / (_PI * 2));
                    g.DrawString(drawStr, drawFont, drawBrush, PoViewDis);

                    if (OnMeasurement != null) OnMeasurement(this, new MeasureArgs(dist, xlen, ylen));
                    //------------------------------------------------------------------------------------------------//
                }
                #endregion
                //----------------------------------------------------------------------------------------------------//
            }
        }

        public delegate void DrawInitCallback();
        public void DrawInit()
        {
            if (this.vScrollBar.InvokeRequired)
            {
                this.BeginInvoke(new DrawInitCallback(DrawInit));
            }
            else
            {
                vScrollBar.Enabled = false;
                hScrollBar.Enabled = false;
            }

            Invalidate(false);
        }

        public void DrawImage()
        {
            if (m_bEnabledDraw == false) return;
            if (m_pImage == null) return;

            Rectangle dcRect = new Rectangle();                                    //ClientDC
            Rectangle bitmapRect = new Rectangle(0, 0, m_nImageWidth, m_nImageHeight);     //그릴 이미지 크기 지정
            dcRect = ClientRectangle;

            //-- 화면에 맞춤 -----------------------------------------------------------------------------------------//
            if (m_dZoom == 0.0)
            {
                vScrollBar.Value = 0;
                hScrollBar.Value = 0;
                m_dZoom = m_dZoomX = m_dZoomY = (double)(this.ClientSize.Width) / m_nImageWidth;       //변경되는 FITWIDTH Zoom x 비율 계산

                vScrollBar.Enabled = false;
                hScrollBar.Enabled = false;

                bitmapRect.X = hScrollBar.Value;
                bitmapRect.Y = vScrollBar.Value;                                   //보여줘야 할 영상의 y시작점(비율에 따라)
                bitmapRect.Width = (int)(dcRect.Width / m_dZoom + 0.5);             //가로 크기
                bitmapRect.Height = (int)(dcRect.Height / m_dZoom + 0.5);            //세로 크기

                hScrollBar.Minimum = 0;
                hScrollBar.LargeChange = bitmapRect.Width;
                hScrollBar.Maximum = m_nImageWidth;

                vScrollBar.Minimum = 0;
                vScrollBar.LargeChange = bitmapRect.Height;
                vScrollBar.Maximum = m_nImageHeight;

            }
            //--------------------------------------------------------------------------------------------------------//

            //-- 배율에 맞게 Rect 생성 -------------------------------------------------------------------------------//
            else
            {
                Size drawSize = new Size();

                drawSize.Width = (int)(bitmapRect.Width * m_dZoom + .5);            //영상 Width를 비율에 맞게 조정
                drawSize.Height = (int)(bitmapRect.Height * m_dZoom + .5);           //영상 Height를 비율에 맞게 조정

                if (drawSize.Width <= this.ClientSize.Width)                       //Client Width 영역이 영상 보다 클경우 처리
                {
                    dcRect.Width = drawSize.Width;
                    hScrollBar.Enabled = false;
                }
                else
                {
                    hScrollBar.Enabled = true;
                }

                if (drawSize.Height <= this.ClientSize.Height)                     //Client Width 영역이 영상 보다 클경우 처리
                {
                    dcRect.Height = drawSize.Height;
                    vScrollBar.Enabled = false;
                }
                else
                {
                    vScrollBar.Enabled = true;
                }

                bitmapRect.Y = vScrollBar.Value;                                   //보여줘야 할 영상의 x시작점(비율에 따라)
                bitmapRect.X = hScrollBar.Value;                                   //보여줘야 할 영상의 y시작점(비율에 따라)
                bitmapRect.Width = (int)(dcRect.Width / m_dZoom + 0.5);             //가로 크기
                bitmapRect.Height = (int)(dcRect.Height / m_dZoom + 0.5);            //세로 크기

                vScrollBar.Minimum = 0;
                vScrollBar.LargeChange = bitmapRect.Height;
                vScrollBar.Maximum = m_nImageHeight;

                hScrollBar.Minimum = 0;
                hScrollBar.LargeChange = bitmapRect.Width;
                hScrollBar.Maximum = m_nImageWidth;
            }
            //--------------------------------------------------------------------------------------------------------//

            //-- DrawIamge -------------------------------------------------------------------------------------------//
            m_pDummyGrphics.DrawImage(m_pImage, dcRect, bitmapRect, GraphicsUnit.Pixel);
            //--------------------------------------------------------------------------------------------------------//

            //-- Profile 정보 담기 -----------------------------------------------------------------------------------//
            if (m_bProfile) ProfileInfo();
            //--------------------------------------------------------------------------------------------------------//

            m_posScroll = new Point(hScrollBar.Value, vScrollBar.Value);

            // Refresh();
            Invalidate(false);

            return;
        }

        /**************************************************************************************************************/
        /*    Profile 정보 담기                                                                                       */
        /**************************************************************************************************************/
        private void ProfileInfo()
        {
            Bitmap bitmap = (Bitmap)m_pDummyImage;

            //-- Profile 정보 담기 -----------------------------------------------------------------------------------//
            m_pDummyProfile = new byte[bitmap.Width];                      //Profile 정보담을 배열

            for (int i = 0; i < bitmap.Width; i++)
            {
                Color color = Color.Black;
                color = bitmap.GetPixel(i, m_posMouse.Y);                 //Pixel 정보 가져오기

                m_pDummyProfile[i] = (byte)(0.2126*color.R + 0.7152*color.G + 0.0722*color.B);         //Profile 정보 담기
            }
            //--------------------------------------------------------------------------------------------------------//
        }

        /**************************************************************************************************************/
        /*    기타 공통 함수                                                                                          */
        /**************************************************************************************************************/
        private int min(int x, int y)
        {
            int ret = 0;

            if (x >= y) ret = y;
            if (x <= y) ret = x;

            return ret;
        }

        private int max(int x, int y)
        {
            int ret = 0;

            if (x >= y) ret = x;
            if (x <= y) ret = y;

            return ret;
        }
        
        /**************************************************************************************************************/
        /*    Mouse 오른쪽 클릭 메뉴 이벤트                                                                           */
        /**************************************************************************************************************/
        private void OnButtonZoomClick(object sender, EventArgs e)
        {
            toolStripMenuItem_FixToScreen.Checked = false;
            toolStripMenuItem_Zoom10.Checked = false;
            toolStripMenuItem_Zoom50.Checked = false;
            toolStripMenuItem_Zoom100.Checked = false;
            toolStripMenuItem_Zoom200.Checked = false;
            toolStripMenuItem_Zoom400.Checked = false;

            ((ToolStripMenuItem)sender).Checked = true;

            int nIndex = int.Parse((string)((ToolStripMenuItem)sender).Tag);
            if (nIndex == 0) Zoom = 0.0;        // Fix To Screen
            if (nIndex == 1) Zoom = 0.1;        // 0.1배
            if (nIndex == 2) Zoom = 0.5;        // 0.5배 
            if (nIndex == 3) Zoom = 1.0;        // 1배 
            if (nIndex == 4) Zoom = 2.0;        // 2배
            if (nIndex == 5) Zoom = 4.0;        // 4배

            SetDoubleBuffering();               // Double Buffering 공간 지운다.

            m_posMeasureStart = m_posMeasureEnd = new Point(0, 0);        //Measure 초기화

            //-- 현재 마우스 포인트 중심을 확대/축소 한다. -----------------------------------------------------------//
            if (m_bEnabledDraw && Zoom != 0.0)
            {
                hScrollBar.Value = 0;
                vScrollBar.Value = 0;

                Point movPoint = new Point(m_posMouseCenterZoom.X, m_posMouseCenterZoom.Y);                     //확대/축소 중심 위치

                //-- Scroll Bar 이동 계산 ----------------------------------------------------------------------------//
                Rectangle dcRect = ClientRectangle;

                int xScroll = movPoint.X - (int)((dcRect.Width / m_dZoom + .5) / 2);
                int yScroll = movPoint.Y - (int)((dcRect.Height / m_dZoom + .5) / 2);
                //----------------------------------------------------------------------------------------------------//

                //-- Scroll Bar 좌상단, 우하단 일 경우 이동 계산 -----------------------------------------------------//
                if (xScroll < 0) xScroll = 0;
                if (yScroll < 0) yScroll = 0;

                if (movPoint.X + (int)((dcRect.Width / m_dZoom + .5) / 2) > hScrollBar.Maximum) xScroll = hScrollBar.Maximum - (int)(dcRect.Width / m_dZoom + .5);
                if (movPoint.Y + (int)((dcRect.Height / m_dZoom + .5) / 2) > vScrollBar.Maximum) yScroll = vScrollBar.Maximum - (int)(dcRect.Height / m_dZoom + .5);
                //----------------------------------------------------------------------------------------------------//

                if (xScroll < 0) xScroll = 0;
                if (yScroll < 0) yScroll = 0;

                //-- Scroll Bar 이동 ---------------------------------------------------------------------------------//
                hScrollBar.Value = xScroll;
                vScrollBar.Value = yScroll;
                //----------------------------------------------------------------------------------------------------//
            }
            //--------------------------------------------------------------------------------------------------------//
            DrawImage();
        }
        
        private void toolStripMenuItem_ROIEnabled_Click(object sender, EventArgs e)
        {
            if (IsEnabledROI == false) toolStripMenuItem_ROIEnabled.Checked = IsEnabledROI = true;
            else toolStripMenuItem_ROIEnabled.Checked = IsEnabledROI = false;
        }

        private void toolStripMenuItem_Profile_Click(object sender, EventArgs e)
        {
            if (IsProfile == false) toolStripMenuItem_Profile.Checked = IsProfile = true;
            else toolStripMenuItem_Profile.Checked = IsProfile = false;
        }

        private void toolStripMenuItem_GuideLine_Click(object sender, EventArgs e)
        {
            if (IsGuideLine == false) toolStripMenuItem_GuideLine.Checked = IsGuideLine = true;
            else toolStripMenuItem_GuideLine.Checked = IsGuideLine = false;
        }


        private void toolStripMenuItem_Measure_Click(object sender, EventArgs e)
        {
            if (IsMeasure == false) toolStripMenuItem_Measure.Checked = IsMeasure = true;
            else toolStripMenuItem_Measure.Checked = IsMeasure = false;
        }

        /**************************************************************************************************************/
        /*    기타 이벤트                                                                                             */
        /**************************************************************************************************************/
        private void hScrollBar_Scroll(object sender, ScrollEventArgs e)
        {
            hScrollBar.Value = e.NewValue;
            DrawImage();
        }

        private void vScrollBar_Scroll(object sender, ScrollEventArgs e)
        {
            vScrollBar.Value = e.NewValue;
            DrawImage();
        }

        private void ImageViewer_MouseClick(object sender, MouseEventArgs e)
        {
            m_posMouse.X = e.X;                //Mouse X Pos
            m_posMouse.Y = e.Y;                //Mouse Y Pos

            //-- 프로파일 / 가이드 라인 그리기 ----------------------------------------------------------------------//
            if (m_bProfile || m_bGuideLine)
            {
                if (m_bProfile)
                {
                    ProfileInfo();  //Profile 정보담기  
                }

                Invalidate(false);
            }
            //--------------------------------------------------------------------------------------------------------//
        }

        private void ImageViewer_MouseDown(object sender, MouseEventArgs e)
        {
            m_posMouseDragStart = RealMousePosition;        //Mouse Drag 시작점

            if (e.Button == MouseButtons.Left) m_bMouseLeft = true;
            if (e.Button == MouseButtons.Right) m_bMouseRight = true;

            //-- 영상 측정 시작 점 등록 ------------------------------------------------------------------------------//
            if (m_bMeasure && m_bMouseLeft /*e.Button == MouseButtons.Left*/)
            {
                m_posMeasureStart = e.Location;
                m_posMeasureEnd = e.Location;

                Invalidate(false);
            }
            //--------------------------------------------------------------------------------------------------------//

            //-- 영상 측정 시작 점 등록 ------------------------------------------------------------------------------//
            if (m_bMouseRight /*e.Button == MouseButtons.Right*/)
            {
                m_posMeasureStart = e.Location;
                m_posMeasureEnd = e.Location;

                // Draw ROI
                CurrentPos = StartPos = RealMousePosition;
                IsDrawing = true;

                Invalidate(false);
            }
            //--------------------------------------------------------------------------------------------------------//
        }

        private void ImageViewer_MouseUp(object sender, MouseEventArgs e)
        {
            //-- 영상 측정 끝 점 등록 --------------------------------------------------------------------------------//
            if (m_bMeasure && m_bMouseLeft /*e.Button == MouseButtons.Left*/)
            {
                m_posMeasureEnd = e.Location;
                if (m_bMeasureHorz == true && m_bMeasureVert == false)
                {
                    m_posMeasureEnd.X = e.Location.X;
                    m_posMeasureEnd.Y = m_posMeasureStart.Y;
                }
                else if (m_bMeasureHorz == false && m_bMeasureVert == true)
                {
                    m_posMeasureEnd.X = m_posMeasureStart.X;
                    m_posMeasureEnd.Y = e.Location.Y;
                }
                Invalidate(false);
            }
            //--------------------------------------------------------------------------------------------------------//

            //-- 영상 Zoom시 마우스 포이터 중심으로 / Calibration 좌표 -----------------------------------------------//
            else if (m_bMouseRight /*e.Button == MouseButtons.Right*/)
            {
                m_posMouseCenterZoom = new Point(RealMousePosition.X, RealMousePosition.Y);

                if (IsDrawing)
                {
                    IsDrawing = false;
                }
                //               Invalidate(false);
            }
            //--------------------------------------------------------------------------------------------------------//
            if (e.Button == MouseButtons.Left) m_bMouseLeft = false;
            if (e.Button == MouseButtons.Right) m_bMouseRight = false;
        }

        private void ImageViewer_MouseMove(object sender, MouseEventArgs e)
        {
            if (m_bEnabledDraw == false) return;

            //-- Mouse 위치에 해당하는 Pixel값을 보여준다. -----------------------------------------------------------//
            if (m_bMouseLeft || m_bMouseRight /*e.Button != MouseButtons.Left && e.Button != MouseButtons.Right*/)
            {
                Point pPos = new Point(RealMousePosition.X, RealMousePosition.Y);               //Pixel 위치   
                Bitmap bitmap = (Bitmap)m_pDummyImage;

                Color color = Color.Black;
                if ((pPos.X > 0 && pPos.X < m_nImageWidth) && (pPos.Y > 0 && pPos.Y < m_nImageHeight))
                {
                    color = bitmap.GetPixel(e.X, e.Y);                                          //Pixel 값

                    //-- 해당 위치 이벤트로 알려줌 -------------------------------------------------------------------//
                    if (OnGetPixel != null) OnGetPixel(this, new PixelArgs(pPos, color.R));
                    //------------------------------------------------------------------------------------------------//
                }
            }
            //--------------------------------------------------------------------------------------------------------//

            if (m_bRoiDraw && m_bMouseRight /*e.Button == MouseButtons.Right*/)
            {
                if (IsDrawing)
                {
                    CurrentPos = RealMousePosition;
                    Invalidate(false);
                }
            }

            //-- 영상 측정을 위한 Drag 계산 --------------------------------------------------------------------------//
            if (m_bMeasure && m_bMouseLeft /*e.Button == MouseButtons.Left*/)
            {
                m_posMeasureEnd = e.Location;
                bool one = false, two = false;
                if (Math.Abs(m_posMeasureStart.Y - e.Location.Y) < 3)
                {
                    m_bMeasureHorz = true;
                    m_bMeasureVert = false;
                    one = true;
                }


                if (Math.Abs(m_posMeasureStart.X - e.Location.X) < 3)
                {
                    m_bMeasureHorz = false;
                    m_bMeasureVert = true;
                    two = true;
                }

                if (one == false && two == false)
                {
                    m_bMeasureHorz = false;
                    m_bMeasureVert = false;
                }

                if (m_bMeasureHorz == true && m_bMeasureVert == false)
                {
                    m_posMeasureEnd.X = e.Location.X;
                    m_posMeasureEnd.Y = m_posMeasureStart.Y;
                }
                else if (m_bMeasureHorz == false && m_bMeasureVert == true)
                {
                    m_posMeasureEnd.X = m_posMeasureStart.X;
                    m_posMeasureEnd.Y = e.Location.Y;
                }
                Invalidate(false);
            }
            //--------------------------------------------------------------------------------------------------------//

            //-- Mouse Drag 이동 -------------------------------------------------------------------------------------//
            if (m_bMeasure == false && m_bGuideLine == false && m_bProfile == false && m_bMouseLeft /*e.Button == MouseButtons.Left*/)
            {
                if (!((e.X >= 0 && e.X <= this.Size.Width) && (e.Y >= 0 && e.Y <= this.Size.Height))) return;

                m_posMouseDragEnd = RealMousePosition;

                try
                {
                    int ValueX = (int)((m_posMouseDragStart.X - m_posMouseDragEnd.X));
                    int ValueY = (int)((m_posMouseDragStart.Y - m_posMouseDragEnd.Y));


                    if (vScrollBar.Value + ValueY < 0)
                    {
                        vScrollBar.Value = 0;
                    }
                    else if (vScrollBar.Value + ValueY > (vScrollBar.Maximum - ((this.Size.Height) / m_dZoomY)))
                    {
                        vScrollBar.Value = vScrollBar.Maximum - (int)((this.Size.Height) / m_dZoomY);
                    }
                    else
                    {
                        vScrollBar.Value += ValueY;
                    }

                    if (hScrollBar.Value + ValueX < 0)
                    {
                        hScrollBar.Value = 0;
                    }
                    else if (hScrollBar.Value + ValueX > (hScrollBar.Maximum - ((this.Size.Width) / m_dZoomX)))
                    {
                        hScrollBar.Value = hScrollBar.Maximum - (int)((this.Size.Width) / m_dZoomX);
                    }
                    else
                    {
                        hScrollBar.Value += ValueX;
                    }

                    DrawImage();
                }
                catch
                {
                }
            }
            //--------------------------------------------------------------------------------------------------------//
        }

        private void ImageViewer_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            if (m_bGuideLine)
            {
                Point nowPosition = new Point(RealMousePosition.X, RealMousePosition.Y);

                //-- 해당 위치 이벤트로 알려줌 -----------------------------------------------------------------------//
                if (OnGetGuidePoint != null) OnGetGuidePoint(this, new PointArgs(nowPosition.X, nowPosition.Y));
                //----------------------------------------------------------------------------------------------------//
            }
        }

    }

    /******************************************************************************************************************/
    /*    픽셀 정보 Event Class                                                                                       */
    /******************************************************************************************************************/
    public class PixelArgs : EventArgs
    {
        private Point fPixelPos;
        private byte fPixel;

        public Point PixelPos
        {
            get { return fPixelPos; }
            set { fPixelPos = value; }
        }
        public byte Pixel
        {
            get { return fPixel; }
            set { fPixel = value; }
        }

        public PixelArgs(Point pos, byte pixel)
        {
            this.fPixelPos = pos;
            this.fPixel = pixel;
        }
    }

    /******************************************************************************************************************/
    /*    영상 측정 정보 Event Class                                                                                  */
    /******************************************************************************************************************/
    public class MeasureArgs : EventArgs
    {
        private double fMeasureLen;
        private int fMeasureX;
        private int fMeasureY;

        public double MeasureLen
        {
            get { return fMeasureLen; }
            set { fMeasureLen = value; }
        }
        public int MeasureX
        {
            get { return fMeasureX; }
            set { fMeasureX = value; }
        }
        public int MeasureY
        {
            get { return fMeasureY; }
            set { fMeasureY = value; }
        }

        public MeasureArgs(double measurelen, int measurex, int measurey)
        {
            this.fMeasureLen = measurelen;
            this.fMeasureX = measurex;
            this.fMeasureY = measurey;
        }
    }

    /******************************************************************************************************************/
    /*    영상의 마우스 포인트 정보 Event Class                                                                       */
    /******************************************************************************************************************/
    public class PointArgs : EventArgs
    {
        private int fPointX;
        private int fPointY;

        public int PointX
        {
            get { return fPointX; }
            set { fPointX = value; }
        }
        public int PointY
        {
            get { return fPointY; }
            set { fPointY = value; }
        }

        public PointArgs(int posx, int posy)
        {
            this.fPointX = posx;
            this.fPointY = posy;
        }
    }

    /******************************************************************************************************************/
    /*    영상의 ROI 정보 Event Class                                                                                 */
    /******************************************************************************************************************/
    public class ROIArgs : EventArgs
    {
        private int fPointX;
        private int fPointY;
        private int fRoiIndex;

        public int PointX
        {
            get { return fPointX; }
            set { fPointX = value; }
        }
        public int PointY
        {
            get { return fPointY; }
            set { fPointY = value; }
        }
        public int RoiIndex
        {
            get { return fRoiIndex; }
            set { fRoiIndex = value; }
        }

        public ROIArgs(int posx, int posy, int RoiIndex)
        {
            this.fPointX = posx;
            this.fPointY = posy;
            this.fRoiIndex = RoiIndex;
        }
    }
}