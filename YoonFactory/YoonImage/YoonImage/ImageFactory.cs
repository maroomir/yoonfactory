﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;

namespace YoonFactory.Image
{
    /// <summary>
    /// Self-made Image Processing Class by .Net Framework
    /// </summary>
    public static class ImageFactory
    {
        private const uint MAX_LABEL = 10000;
        private const uint MAX_OBJECT = 10000;
        private const uint MAX_PICK_NUM = 100;
        private const uint MAX_FILL_NUM = 1000;

        // Converter
        public static class Converter
        {
            public static byte[] To8BitGrayBuffer(int[] pBuffer, int nWidth, int nHeight)
            {
                if (pBuffer.Length != nWidth * nHeight) return null;

                byte[] pByte = new byte[pBuffer.Length];
                for (int j = 0; j < nHeight; j++)
                {
                    for (int i = 0; i < nWidth; i++)
                    {
                        byte[] pBytePixel = BitConverter.GetBytes(pBuffer[j * nWidth + i]); // Order by {B/G/R/A}
                        pByte[j * nWidth + i] = (byte)(0.299f * pBytePixel[2] + 0.587f * pBytePixel[1] + 0.114f * pBytePixel[0]); // ITU-RBT.709, YPrPb
                    }
                }
                return pByte;
            }

            public static byte[] To8BitGrayBufferWithRescaling<T>(T[] pBuffer, int nWidth, int nHeight) where T : IComparable, IComparable<T>
            {
                if (pBuffer.Length != nWidth * nHeight) return null;

                byte[] pByte = new byte[pBuffer.Length];
                double nValueMax = 0;
                double nValueMin = 65536;
                double dRatio = 1.0;
                for (int j = 0; j < nHeight; j++)
                {
                    for (int i = 0; i < nWidth; i++)
                    {
                        double value = Convert.ToDouble(pBuffer[j * nWidth + i]);
                        if (value > nValueMax) nValueMax = value;
                        if (value < nValueMin) nValueMin = value;
                    }
                }
                ////  최대 Gray Level값이 255를 넘는 경우, 이에 맞게 Image 전체 Gray Level을 조정함.
                if (nValueMax > 255) dRatio = 255.0 / nValueMax;
                for (int j = 0; j < nHeight; j++)
                {
                    for (int i = 0; i < nWidth; i++)
                    {
                        pByte[j * nWidth + i] = Convert.ToByte(Convert.ToDouble(pBuffer[j * nWidth + i]) * dRatio);
                    }
                }
                return pByte;
            }

            public static int[] To24BitColorBuffer(byte[] pRed, byte[] pGreen, byte[] pBlue, int nWidth, int nHeight)
            {
                if (pRed == null || pRed.Length != nWidth * nHeight ||
                    pGreen == null || pGreen.Length != nWidth * nHeight ||
                    pBlue == null || pBlue.Length != nWidth * nHeight)
                    return null;

                int[] pPixel = new int[nWidth * nHeight];
                for (int j = 0; j < nHeight; j++)
                {
                    for (int i = 0; i < nWidth; i++)
                    {
                        byte[] pBytePixel = new byte[4];
                        pBytePixel[0] = pBlue[j * nWidth + i];
                        pBytePixel[1] = pGreen[j * nWidth + i];
                        pBytePixel[2] = pRed[j * nWidth + i];
                        pBytePixel[3] = (byte)0;
                        pPixel[i] = BitConverter.ToInt32(pBytePixel, 0);
                    }
                }
                return pPixel;
            }

            public static int[] To24BitColorBufferWithUpscaling<T>(T[] pBuffer, int nWidth, int nHeight) where T : IComparable, IComparable<T>
            {
                if (pBuffer.Length != nWidth * nHeight) return null;

                int[] pPixel = new int[pBuffer.Length];
                for (int j = 0; j < nHeight; j++)
                {
                    for (int i = 0; i < nWidth; i++)
                    {
                        pPixel[j * nWidth + i] = 3 * Math.Max((byte)0, Math.Min(Convert.ToByte(pBuffer[j * nWidth + i]), (byte)255));
                    }
                }
                return pPixel;
            }

            public static byte[] To8BitRedBuffer(int[] pBuffer, int nWidth, int nHeight)
            {
                if (pBuffer.Length != nWidth * nHeight) return null;

                byte[] pByte = new byte[pBuffer.Length];
                for (int j = 0; j < nHeight; j++)
                {
                    for (int i = 0; i < nWidth; i++)
                    {
                        byte[] pBytePixel = BitConverter.GetBytes(pBuffer[j * nWidth + i]); // Order by {B/G/R/A}
                        pByte[j * nWidth] = pBytePixel[2];
                    }
                }
                return pByte;
            }

            public static byte[] To8BitGreenBuffer(int[] pBuffer, int nWidth, int nHeight)
            {
                if (pBuffer.Length != nWidth * nHeight) return null;

                byte[] pByte = new byte[pBuffer.Length];
                for (int j = 0; j < nHeight; j++)
                {
                    for (int i = 0; i < nWidth; i++)
                    {
                        byte[] pBytePixel = BitConverter.GetBytes(pBuffer[j * nWidth + i]); // Order by {B/G/R/A}
                        pByte[j * nWidth] = pBytePixel[1];
                    }
                }
                return pByte;
            }

            public static byte[] To8BitBlueBuffer(int[] pBuffer, int nWidth, int nHeight)
            {
                if (pBuffer.Length != nWidth * nHeight) return null;

                byte[] pByte = new byte[pBuffer.Length];
                for (int j = 0; j < nHeight; j++)
                {
                    for (int i = 0; i < nWidth; i++)
                    {
                        byte[] pBytePixel = BitConverter.GetBytes(pBuffer[j * nWidth + i]); // Order by {B/G/R/A}
                        pByte[j * nWidth] = pBytePixel[0];
                    }
                }
                return pByte;
            }
        }

        // Pattern Match
        public static class PatternMatch
        {
            public static IYoonObject FindPatternAsBinary(YoonImage pPatternImage, YoonImage pSourceImage)
            {
                if (pPatternImage.Format != PixelFormat.Format8bppIndexed || pSourceImage.Format != PixelFormat.Format8bppIndexed)
                    throw new FormatException("[YOONIMAGE EXCEPTION] Image arguments is not 8bit format");
                return FindPatternAsBinary(pPatternImage.GetGrayBuffer(), pPatternImage.Width, pPatternImage.Height, pSourceImage.GetGrayBuffer(), pSourceImage.Width, pSourceImage.Height);
            }

            public static IYoonObject FindPatternAsBinary(byte[] pPatternBuffer, int patternWidth, int patternHeight, byte[] pSourceBuffer, int sourceWidth, int sourceHeight, bool bWhite = true)
            {
                int findPosX, findPosY;
                int startX, startY, jumpX, jumpY;
                int graySource, grayPattern;
                int whiteCount, totalCountWhite;
                int blackCount, totalCountBlack;
                int whiteCountMax, blackCountMax;
                double dCoefficient = 0.0;
                ////  초기화
                YoonRect2N findRect = new YoonRect2N(0, 0, 0, 0);
                ////  Skip 정도 지정
                jumpX = patternWidth / 30;
                jumpY = patternHeight / 30;
                if (jumpX < 1) jumpX = 1;
                if (jumpY < 1) jumpY = 1;
                findPosX = 0;
                findPosY = 0;
                ////  Match 갯수 및 정도를 찾는다.
                whiteCountMax = 0;
                blackCountMax = 0;
                for (int iY = 0; iY < sourceHeight - patternHeight; iY += 1)
                {
                    for (int iX = 0; iX < sourceWidth - patternWidth; iX += 1)
                    {
                        startX = iX;
                        startY = iY;
                        ////// 전체 영역 내에서의 차이 값을 구한다.
                        whiteCount = 0;
                        blackCount = 0;
                        totalCountWhite = 0;
                        totalCountBlack = 0;
                        for (int y = 0; y < patternHeight - jumpY; y += jumpY)
                        {
                            for (int x = 0; x < patternWidth - jumpX; x += jumpX)
                            {
                                graySource = pSourceBuffer[(startY + y) * sourceWidth + startX + x];
                                grayPattern = pPatternBuffer[y * patternWidth + x];
                                //////  Pattern과 Source의 Gray Level이 같은 경우 match Count를 늘린다.
                                if (grayPattern == 0)
                                {
                                    totalCountBlack++;
                                    if (grayPattern == graySource)
                                        blackCount++;
                                }
                                if (grayPattern == 255)
                                {
                                    totalCountWhite++;
                                    if (grayPattern == graySource)
                                        whiteCount++;
                                }
                            }
                        }
                        ////// 최대한 White IYoonVector가 많은 Pattern을 찾는다.
                        if (bWhite)
                        {
                            if (whiteCount > whiteCountMax)
                            {
                                whiteCountMax = whiteCount;
                                findPosX = iX;
                                findPosY = iY;
                                dCoefficient = 0.0;
                                if (totalCountWhite > 1)
                                    dCoefficient = whiteCount * 100.0 / totalCountWhite;
                            }
                        }
                        else
                        {
                            if (blackCount > blackCountMax)
                            {
                                blackCountMax = blackCount;
                                findPosX = iX;
                                findPosY = iY;
                                dCoefficient = 0.0;
                                if (totalCountBlack > 1)
                                    dCoefficient = blackCount * 100.0 / totalCountBlack;
                            }
                        }
                    }
                }
                findRect.CenterPos.X = findPosX;
                findRect.CenterPos.Y = findPosY;
                findRect.Width = patternWidth;
                findRect.Height = patternHeight;
                return new YoonObject<YoonRect2N>(0, findRect, dCoefficient, (bWhite) ? whiteCountMax : blackCountMax);
            }

            public static IYoonObject FindPatternAsBinary(YoonRect2N scanArea, YoonImage pPatternImage, YoonImage pSourceImage)
            {
                if (pPatternImage.Format != PixelFormat.Format8bppIndexed || pSourceImage.Format != PixelFormat.Format8bppIndexed)
                    throw new FormatException("[YOONIMAGE EXCEPTION] Image arguments is not 8bit format");
                if (!pPatternImage.IsVerifiedArea(scanArea))
                    throw new ArgumentOutOfRangeException("[YOONIMAGE EXCEPTION] Scan area is not verified");
                return FindPatternAsBinary(scanArea, pPatternImage.GetGrayBuffer(), pPatternImage.Width, pPatternImage.Height, pSourceImage.GetGrayBuffer(), pSourceImage.Width, pSourceImage.Height);
            }

            public static IYoonObject FindPatternAsBinary(YoonRect2N scanArea, byte[] pPatternBuffer, int patternWidth, int patternHeight, byte[] pSourceBuffer, int sourceWidth, int sourceHeight)
            {
                int findPosX, findPosY;
                int startX, startY, jumpX, jumpY;
                int graySource, grayPattern;
                int whiteCount, totalCountWhite;
                int blackCount, totalCountBlack;
                int matchCount, matchCountMax;
                double dCoefficient = 0.0;
                ////  초기화
                YoonRect2N findRect = new YoonRect2N(0, 0, 0, 0);
                ////  Skip 정도 지정
                jumpX = patternWidth / 30;
                jumpY = patternHeight / 30;
                if (jumpX < 1) jumpX = 1;
                if (jumpY < 1) jumpY = 1;
                findPosX = 0;
                findPosY = 0;
                ////  Match 갯수 및 정도를 찾는다.
                matchCountMax = 0;
                for (int iY = scanArea.Top; iY < scanArea.Bottom - patternHeight; iY += 1)
                {
                    for (int iX = scanArea.Left; iX < scanArea.Right - patternWidth; iX += 1)
                    {
                        startX = iX;
                        startY = iY;
                        ////// Scan Area 영역 내에서의 차이 값을 구한다.
                        matchCount = 0;
                        whiteCount = 0;
                        blackCount = 0;
                        totalCountWhite = 0;
                        totalCountBlack = 0;
                        for (int y = 0; y < patternHeight - jumpY; y += jumpY)
                        {
                            for (int x = 0; x < patternWidth - jumpX; x += jumpX)
                            {
                                graySource = pSourceBuffer[(startY + y) * sourceWidth + startX + x];
                                grayPattern = pPatternBuffer[y * patternWidth + x];
                                //////  Pattern과 Source의 Gray Level이 같은 경우 match Count를 늘린다.
                                if (grayPattern == graySource) matchCount++;
                                if (grayPattern == 0)
                                {
                                    totalCountBlack++;
                                    if (grayPattern == graySource)
                                        blackCount++;
                                }
                                if (grayPattern == 255)
                                {
                                    totalCountWhite++;
                                    if (grayPattern == graySource)
                                        whiteCount++;
                                }
                            }
                        }
                        matchCount = blackCount;
                        ////// 최대한 Matching IYoonVector가 많은 Pattern을 찾는다.
                        if (matchCount > matchCountMax)
                        {
                            matchCountMax = matchCount;
                            findPosX = iX;
                            findPosY = iY;
                            dCoefficient = 0.0;
                            if (totalCountBlack > totalCountWhite)
                                dCoefficient = blackCount * 100.0 / totalCountBlack;
                            else
                                dCoefficient = whiteCount * 100.0 / totalCountWhite;
                        }
                    }
                }
                findRect.CenterPos.X = findPosX;
                findRect.CenterPos.Y = findPosY;
                findRect.Width = patternWidth;
                findRect.Height = patternHeight;
                return new YoonObject<YoonRect2N>(0, findRect, dCoefficient, matchCountMax);
            }

            public static IYoonObject FindPattern(YoonImage pPatternImage, YoonImage pSourceImage, int nDiffThreshold = 10)
            {
                if (pPatternImage.Plane == 1 && pSourceImage.Plane == 1)
                    return FindPattern(pPatternImage.GetGrayBuffer(), pPatternImage.Width, pPatternImage.Height, pSourceImage.GetGrayBuffer(), pSourceImage.Width, pSourceImage.Height, (byte)nDiffThreshold);
                else if (pPatternImage.Plane == 4 && pSourceImage.Plane == 4)
                    return FindPattern(pPatternImage.GetARGBBuffer(), pPatternImage.Width, pPatternImage.Height, pSourceImage.GetARGBBuffer(), pSourceImage.Width, pSourceImage.Height, nDiffThreshold);
                else
                    throw new FormatException("[YOONIMAGE EXCEPTION] Image format arguments is not comportable");
            }

            public static IYoonObject FindPattern(byte[] pPatternBuffer, int patternWidth, int patternHeight, byte[] pSourceBuffer, int sourceWidth, int sourceHeight, byte diffThreshold)
            {
                int minDiff, sumDiff;
                int count, findPosX, findPosY;
                int graySource, grayPattern;
                int startX, startY, jumpX, jumpY;
                double dCoefficient = 0.0;
                ////  초기화
                YoonRect2N findRect = new YoonRect2N(0, 0, 0, 0);
                minDiff = 2147483647;
                sumDiff = 0;
                count = 0;
                findPosX = 0;
                findPosY = 0;
                ////  Skip 정도 지정
                jumpX = patternWidth / 30;
                jumpY = patternHeight / 30;
                if (jumpX < 1) jumpX = 1;
                if (jumpY < 1) jumpY = 1;
                ////  Match 갯수 및 정도를 찾는다.
                for (int iY = 0; iY < sourceHeight - patternHeight; iY += 1)
                {
                    for (int iX = 0; iX < sourceWidth - patternWidth; iX += 1)
                    {
                        startX = iX;
                        startY = iY;
                        ////// 전체 영역 내에서의 차이 값을 구한다.
                        sumDiff = 0;
                        for (int y = 0; y < patternHeight - jumpY; y += jumpY)
                        {
                            for (int x = 0; x < patternWidth - jumpX; x += jumpX)
                            {
                                graySource = pSourceBuffer[(startY + y) * sourceWidth + startX + x];
                                grayPattern = pPatternBuffer[y * patternWidth + x];
                                sumDiff += Math.Abs(graySource - grayPattern);
                                if (Math.Abs(graySource - grayPattern) < diffThreshold)
                                    count++;
                            }
                        }
                        ////// Diff가 최소인 지점을 찾는다.
                        if (sumDiff < minDiff)
                        {
                            minDiff = sumDiff;
                            findPosX = iX;
                            findPosY = iY;
                        }
                    }
                }
                findRect.CenterPos.X = findPosX;
                findRect.CenterPos.Y = findPosY;
                findRect.Width = patternWidth;
                findRect.Height = patternHeight;
                ////  상관계수 구하기
                byte[] pTempBuffer;
                pTempBuffer = new byte[patternWidth * patternHeight];
                for (int j = 0; j < patternHeight; j++)
                    for (int i = 0; i < patternWidth; i++)
                        pTempBuffer[j * patternWidth + i] = pSourceBuffer[(findPosY + j) * sourceWidth + (findPosX + i)];
                dCoefficient = MathFactory.GetCorrelationCoefficient(pPatternBuffer, pTempBuffer, patternWidth, patternHeight);

                return new YoonObject<YoonRect2N>(0, findRect, dCoefficient, count);
            }

            public static IYoonObject FindPattern(int[] pPatternBuffer, int patternWidth, int patternHeight, int[] pSourceBuffer, int sourceWidth, int sourceHeight, int diffThreshold)
            {
                int minDiff, sumDiff;
                int count, findPosX, findPosY;
                int graySource, grayPattern;
                int startX, startY, jumpX, jumpY;
                double dCoefficient = 0.0;
                ////  초기화
                YoonRect2N findRect = new YoonRect2N(0, 0, 0, 0);
                minDiff = 2147483647;
                sumDiff = 0;
                count = 0;
                findPosX = 0;
                findPosY = 0;
                ////  Skip 정도 지정
                jumpX = patternWidth / 60;
                jumpY = patternHeight / 60;
                if (jumpX < 1) jumpX = 1;
                if (jumpY < 1) jumpY = 1;
                ////  Match 갯수 및 정도를 찾는다.
                for (int iY = 0; iY < sourceHeight - patternHeight; iY += 1)
                {
                    for (int iX = 0; iX < sourceWidth - patternWidth; iX += 1)
                    {
                        startX = iX;
                        startY = iY;
                        ////// 전체 영역 내에서의 차이 값을 구한다.
                        sumDiff = 0;
                        for (int y = 0; y < patternHeight - jumpY; y += jumpY)
                        {
                            for (int x = 0; x < patternWidth - jumpX; x += jumpX)
                            {
                                graySource = pSourceBuffer[(startY + y) * sourceWidth + startX + x];
                                grayPattern = pPatternBuffer[y * patternWidth + x];
                                sumDiff += Math.Abs(graySource - grayPattern);
                                if (Math.Abs(graySource - grayPattern) < diffThreshold)
                                    count++;
                            }
                        }
                        ////// Diff가 최소인 지점을 찾는다.
                        if (sumDiff < minDiff)
                        {
                            minDiff = sumDiff;
                            findPosX = iX;
                            findPosY = iY;
                        }
                    }
                }
                findRect.CenterPos.X = findPosX;
                findRect.CenterPos.Y = findPosY;
                findRect.Width = patternWidth;
                findRect.Height = patternHeight;
                ////  상관계수 구하기
                int[] pTempBuffer;
                pTempBuffer = new int[patternWidth * patternHeight];
                for (int j = 0; j < patternHeight; j++)
                    for (int i = 0; i < patternWidth; i++)
                        pTempBuffer[j * patternWidth + i] = pSourceBuffer[(findPosY + j) * sourceWidth + (findPosX + i)];
                dCoefficient = MathFactory.GetCorrelationCoefficient(pPatternBuffer, pTempBuffer, patternWidth, patternHeight);

                return new YoonObject<YoonRect2N>(0, findRect, dCoefficient, count);
            }

            public static IYoonObject FindPattern(YoonRect2N scanArea, YoonImage pPatternImage, YoonImage pSourceImage, int nDiffThreshold)
            {
                if (pPatternImage.Format != PixelFormat.Format8bppIndexed || pSourceImage.Format != PixelFormat.Format8bppIndexed)
                    throw new FormatException("[YOONIMAGE EXCEPTION] Image arguments is not 8bit format");
                if (!pPatternImage.IsVerifiedArea(scanArea))
                    throw new ArgumentOutOfRangeException("[YOONIMAGE EXCEPTION] Scan area is not verified");
                return FindPattern(scanArea, pPatternImage.GetGrayBuffer(), pPatternImage.Width, pPatternImage.Height, pSourceImage.GetGrayBuffer(), pSourceImage.Width, pSourceImage.Height, nDiffThreshold);
            }

            public static IYoonObject FindPattern(YoonRect2N scanArea, byte[] pPatternBuffer, int patternWidth, int patternHeight, byte[] pSourceBuffer, int sourceWidth, int sourceHeight, int diffThreshold)
            {
                int minDiff, sumDiff;
                int count, findPosX, findPosY;
                int graySource, grayPattern;
                int startX, startY, jumpX, jumpY;
                double dCoefficient = 0.0;
                ////  초기화
                YoonRect2N findRect = new YoonRect2N(0, 0, 0, 0);
                if (patternWidth < 1 || patternHeight < 1)
                    throw new ArgumentException("[YOONIMAGE EXCEPTION] Pattern size is not verified");
                minDiff = 2147483647;
                sumDiff = 0;
                count = 0;
                findPosX = 0;
                findPosY = 0;
                ////  Skip 정도 지정
                jumpX = patternWidth / 60;
                jumpY = patternHeight / 60;
                if (jumpX < 1) jumpX = 1;
                if (jumpY < 1) jumpY = 1;
                for (int iY = scanArea.Top; iY < scanArea.Bottom - patternHeight; iY += 2)
                {
                    for (int iX = scanArea.Left; iX < scanArea.Right - patternWidth; iX += 2)
                    {
                        startX = iX;
                        startY = iY;
                        ////// 전체 영역 내에서의 차이 값을 구한다.
                        sumDiff = 0;
                        for (int y = 0; y < patternHeight - jumpY; y += jumpY)
                        {
                            for (int x = 0; x < patternWidth - jumpX; x += jumpX)
                            {
                                graySource = pSourceBuffer[(startY + y) * sourceWidth + startX + x];
                                grayPattern = pPatternBuffer[y * patternWidth + x];
                                sumDiff += Math.Abs(graySource - grayPattern);
                                if (Math.Abs(graySource - grayPattern) < diffThreshold)
                                    count++;
                            }
                        }
                        ////// Diff가 최소인 지점을 찾는다.
                        if (sumDiff < minDiff)
                        {
                            minDiff = sumDiff;
                            findPosX = iX;
                            findPosY = iY;
                        }
                    }
                }
                findRect.CenterPos.X = findPosX;
                findRect.CenterPos.Y = findPosY;
                findRect.Width = patternWidth;
                findRect.Height = patternHeight;
                ////  상관계수 구하기
                byte[] pTempBuffer;
                pTempBuffer = new byte[patternWidth * patternHeight];
                for (int j = 0; j < patternHeight; j++)
                    for (int i = 0; i < patternWidth; i++)
                        pTempBuffer[j * patternWidth + i] = pSourceBuffer[(findPosY + j) * sourceWidth + (findPosX + i)];
                dCoefficient = MathFactory.GetCorrelationCoefficient(pPatternBuffer, pTempBuffer, patternWidth, patternHeight);

                return new YoonObject<YoonRect2N>(0, findRect, dCoefficient, count);
            }
        }

        public static class TwoImageProcess
        {
            public static YoonImage Combine(YoonImage pSourceImage, YoonImage pObjectImage)
            {
                if (pSourceImage.Width != pObjectImage.Width || pSourceImage.Height != pObjectImage.Height)
                    throw new ArgumentException("[YOONIMAGE EXCEPTION] Source and object size is not same");
                if (pSourceImage.Format != PixelFormat.Format8bppIndexed)
                    throw new FormatException("[YOONIMAGE EXCEPTION] Image arguments is not 8bit format");
                return new YoonImage(Combine(pSourceImage.GetGrayBuffer(), pObjectImage.GetGrayBuffer(), pSourceImage.Width, pSourceImage.Height),
                    pSourceImage.Width, pSourceImage.Height, PixelFormat.Format8bppIndexed);
            }

            public static byte[] Combine(byte[] pSourceBuffer, byte[] pObjectBuffer, int width, int height)
            {
                int i, j;
                byte[] pResultBuffer;
                pResultBuffer = new byte[width * height];
                for (j = 0; j < height; j++)
                {
                    for (i = 0; i < width; i++)
                    {
                        if (pSourceBuffer[j * width + i] > pObjectBuffer[j * width + i])
                            pResultBuffer[j * width + i] = pSourceBuffer[j * width + i];
                        else
                            pResultBuffer[j * width + i] = pObjectBuffer[j * width + i];
                    }
                }
                return pResultBuffer;
            }

            public static YoonImage Add(YoonImage pSourceImage, YoonImage pObjectImage)
            {
                if (pSourceImage.Width != pObjectImage.Width || pSourceImage.Height != pObjectImage.Height)
                    throw new ArgumentException("[YOONIMAGE EXCEPTION] Source and object size is not same");
                if (pSourceImage.Format != PixelFormat.Format8bppIndexed)
                    throw new FormatException("[YOONIMAGE EXCEPTION] Image arguments is not 8bit format");
                return new YoonImage(Add(pSourceImage.GetGrayBuffer(), pObjectImage.GetGrayBuffer(), pSourceImage.Width, pSourceImage.Height),
                    pSourceImage.Width, pSourceImage.Height, PixelFormat.Format8bppIndexed);
            }

            public static byte[] Add(byte[] pSourceBuffer, byte[] pObjectBuffer, int width, int height)
            {
                int i, j, value;
                int maxValue, minValue;
                int[] pTempBuffer;
                byte[] pResultBuffer;
                double ratio;
                maxValue = 0;
                minValue = 1024;
                pTempBuffer = new int[width * height];
                pResultBuffer = new byte[width * height];
                for (j = 0; j < height; j++)
                {
                    for (i = 0; i < width; i++)
                    {
                        pTempBuffer[j * width + i] = pSourceBuffer[j * width + i] + pObjectBuffer[j * width + i];
                    }
                }
                ////  합해진 Buffer(pBuffer)의 최대 Gray Level 값과 최소 Gray Level 값을 산출함.
                for (j = 0; j < height; j++)
                {
                    for (i = 0; i < width; i++)
                    {
                        value = pTempBuffer[j * width + i];
                        if (value < minValue) minValue = value;
                        if (value > maxValue) maxValue = value;
                    }
                }
                ////  최대 Gray Level값이 255를 넘는 경우, 이에 맞게 Image 전체 Gray Level을 조정함.
                if (maxValue > 255)
                {
                    ratio = 255.0 / (double)maxValue;
                    for (j = 0; j < height; j++)
                    {
                        for (i = 0; i < width; i++)
                        {
                            value = (int)(pTempBuffer[j * width + i] * ratio);
                            if (value > 255) value = 255;
                            if (value < 0) value = 0;
                            pResultBuffer[j * width + i] = (byte)value;
                        }
                    }
                }
                return pResultBuffer;
            }

            public static YoonImage Subtract(YoonImage pSourceImage, YoonImage pObjectImage)
            {
                if (pSourceImage.Width != pObjectImage.Width || pSourceImage.Height != pObjectImage.Height)
                    throw new ArgumentException("[YOONIMAGE EXCEPTION] Source and object size is not same");
                if (pSourceImage.Format != PixelFormat.Format8bppIndexed)
                    throw new FormatException("[YOONIMAGE EXCEPTION] Image arguments is not 8bit format");
                return new YoonImage(Subtract(pSourceImage.GetGrayBuffer(), pObjectImage.GetGrayBuffer(), pSourceImage.Width, pSourceImage.Height),
                    pSourceImage.Width, pSourceImage.Height, PixelFormat.Format8bppIndexed);
            }

            public static byte[] Subtract(byte[] pSourceBuffer, byte[] pObjectBuffer, int width, int height)
            {
                int i, j, value;
                byte[] pResultBuffer;
                pResultBuffer = new byte[width * height];
                for (j = 0; j < height; j++)
                {
                    for (i = 0; i < width; i++)
                    {
                        value = pSourceBuffer[j * width + i] - pObjectBuffer[j * width + i];
                        if (value > 255) value = 255;
                        if (value < 0) value = 0;
                        pResultBuffer[j * width + i] = (byte)value;
                    }
                }
                return pResultBuffer;
            }
        }

        // Main Filtering in Image Processing
        public static class Filter
        {
            public static YoonImage Sobel(YoonImage pSourceImage, int nIntensity, bool bCombine = true)
            {
                if (pSourceImage.Format != PixelFormat.Format8bppIndexed)
                    throw new FormatException("[YOONIMAGE EXCEPTION] Image arguments is not 8bit format");
                return new YoonImage(Sobel(pSourceImage.GetGrayBuffer(), pSourceImage.Width, pSourceImage.Height, nIntensity, bCombine),
                    pSourceImage.Width, pSourceImage.Height, PixelFormat.Format8bppIndexed);
            }

            public static byte[] Sobel(byte[] pBuffer, int width, int height, int nIntensity, bool bCombineSource)
            {
                int x, y, i, j;
                int posX, posY;
                int imageWidth, imageHeight, imageSize;
                int centerValue1, centerValue2, sum, value;
                byte[] pResultBuffer;
                ////  Sobel Mask 생성.
                int maskValue = nIntensity;
                int[,] mask1 = new int[3, 3] {{-maskValue, 0, maskValue},
                                          {-maskValue, 0, maskValue},
                                          {-maskValue, 0, maskValue}};
                int[,] mask2 = new int[3, 3] {{maskValue,  maskValue,  maskValue},
                                          {0,  0,  0},
                                          {-maskValue, -maskValue, -maskValue}};
                imageWidth = width;
                imageHeight = height;
                imageSize = imageWidth * imageHeight;
                pResultBuffer = new byte[imageSize];
                ////  Sobel Mask 처리.
                for (y = 0; y < height - 2; y++)
                {
                    for (x = 0; x < width - 2; x++)
                    {
                        centerValue1 = 0;
                        centerValue2 = 0;
                        for (j = 0; j < 3; j++)
                        {
                            for (i = 0; i < 3; i++)
                            {
                                posX = x + i;
                                posY = y + j;
                                value = pBuffer[posY * imageWidth + posX];
                                centerValue1 += value * mask1[i, j];
                                centerValue2 += value * mask2[i, j];
                            }
                        }
                        sum = Math.Abs(centerValue1) + Math.Abs(centerValue2);

                        if (sum > 255) sum = 255;
                        if (sum < 0) sum = 0;
                        posX = x + 1;
                        posY = y + 1;
                        pResultBuffer[posY * imageWidth + posX] = (byte)sum;
                    }
                }
                ////  Sobel Filtering 결과와 원본을 합친 영상을 원하는 경우.
                if (bCombineSource)
                {
                    pResultBuffer = TwoImageProcess.Combine(pBuffer, pResultBuffer, width, height);
                }
                return pResultBuffer;
            }

            public static YoonImage Laplacian(YoonImage pSourceImage, int nIntensity, bool bCombine = true)
            {
                if (pSourceImage.Format != PixelFormat.Format8bppIndexed)
                    throw new FormatException("[YOONIMAGE EXCEPTION] Image arguments is not 8bit format");
                return new YoonImage(Laplacian(pSourceImage.GetGrayBuffer(), pSourceImage.Width, pSourceImage.Height, nIntensity, bCombine),
                    pSourceImage.Width, pSourceImage.Height, PixelFormat.Format8bppIndexed);
            }

            public static byte[] Laplacian(byte[] pBuffer, int width, int height, int Intensity, bool bCombineSource)
            {
                if (width < 1 || height < 1)
                    throw new ArgumentException("[YOONIMAGE EXCEPTION] Buffer size is not normalized");
                int i, j;
                int centerValue, value;
                byte[] pResultBuffer;
                centerValue = 4 * Intensity;
                pResultBuffer = new byte[width * height];
                ////  Laplacian Mask 처리.
                for (j = 1; j < height - 1; j++)
                {
                    for (i = 1; i < width - 1; i++)
                    {
                        value = centerValue * pBuffer[j * width + i] - pBuffer[(j - 1) * width + i] - pBuffer[j * width + i + 1] - pBuffer[(j + 1) * width + i] - pBuffer[j * width + i - 1];
                        if (value < 0) value = 0;
                        if (value > 255) value = 255;
                        pResultBuffer[j * width + i] = (byte)value;
                    }
                }
                ////  Laplacian 결과와 원본 영상을 합친 경우.
                if (bCombineSource)
                {
                    pResultBuffer = TwoImageProcess.Combine(pBuffer, pResultBuffer, width, height);
                }
                return pResultBuffer;
            }

            public static YoonImage RC1D(YoonImage pSourceImage, double dFrequency, bool bCombine = true)
            {
                if (pSourceImage.Format != PixelFormat.Format8bppIndexed)
                    throw new FormatException("[YOONIMAGE EXCEPTION] Image arguments is not 8bit format");
                return new YoonImage(RC1D(pSourceImage.GetGrayBuffer(), pSourceImage.Width, dFrequency, bCombine),
                    pSourceImage.Width, pSourceImage.Height, PixelFormat.Format8bppIndexed);
            }

            public static byte[] RC1D(byte[] pBuffer, int size, double frequency, bool bCombineSource)
            {
                int i, j;
                double value;
                int width, height;
                double[] pWidth, pHeight;
                byte[] pResultBuffer;
                width = size;
                height = 1;
                pWidth = new double[width];
                pHeight = new double[height];
                pResultBuffer = new byte[width * height];
                ////  가로방향 Filtering
                for (j = 0; j < height; j++)
                {
                    pHeight[j] = pBuffer[j * width + 0];
                    pResultBuffer[j * width + 0] = (byte)(0.5 * pHeight[j]);
                    for (i = 1; i < width; i++)
                    {
                        pHeight[j] = frequency * pHeight[j] + (1 - frequency) * pBuffer[j * width + i];
                        value = 0.5 * pHeight[j];
                        if (value < 0) value = 0;
                        if (value > 255) value = 255;
                        pResultBuffer[j * width + i] = (byte)value;
                    }
                }
                for (j = 0; j < height; j++)
                {
                    pHeight[j] = pBuffer[j * width + (width - 1)];
                    value = 0.5 * pHeight[j] + pResultBuffer[j * width + (width - 1)];
                    if (value < 0) value = 0;
                    if (value > 255) value = 255;
                    pResultBuffer[j * width + (width - 1)] = (byte)value;
                    for (i = width - 2; i >= 0; i--)
                    {
                        pHeight[j] = frequency * pHeight[j] + (1 - frequency) * pBuffer[j * width + i];
                        value = pResultBuffer[j * width + i] + 0.5 * pHeight[j];
                        if (value < 0) value = 0;
                        if (value > 255) value = 255;
                        pResultBuffer[j * width + i] = (byte)value;
                    }
                }
                ////  RC 결과와 원본 영상을 합친 경우.
                if (bCombineSource)
                {
                    pResultBuffer = TwoImageProcess.Combine(pBuffer, pResultBuffer, width, height);
                }
                return pResultBuffer;
            }

            public static YoonImage RC2D(YoonImage pSourceImage, double dFrequency, bool bCombine = true)
            {
                if (pSourceImage.Format != PixelFormat.Format8bppIndexed)
                    throw new FormatException("[YOONIMAGE EXCEPTION] Image arguments is not 8bit format");
                return new YoonImage(RC2D(pSourceImage.GetGrayBuffer(), pSourceImage.Width, pSourceImage.Height, dFrequency, bCombine),
                    pSourceImage.Width, pSourceImage.Height, PixelFormat.Format8bppIndexed);
            }

            public static byte[] RC2D(byte[] pBuffer, int width, int height, double frequency, bool bSumSource)
            {
                int i, j;
                double value;
                double[] pWidth, pHeight;
                byte[] pResultBuffer;
                pWidth = new double[width];
                pHeight = new double[height];
                pResultBuffer = new byte[width * height];
                ////  가로방향 Filtering
                for (j = 0; j < height; j++)
                {
                    pHeight[j] = pBuffer[j * width + 0];
                    pResultBuffer[j * width + 0] = (byte)(0.5 * pHeight[j]);
                    for (i = 1; i < width; i++)
                    {
                        pHeight[j] = frequency * pHeight[j] + (1 - frequency) * pBuffer[j * width + i];
                        value = 0.5 * pHeight[j];
                        if (value < 0) value = 0;
                        if (value > 255) value = 255;
                        pResultBuffer[j * width + i] = (byte)value;
                    }
                }
                for (j = 0; j < height; j++)
                {
                    pHeight[j] = pBuffer[j * width + (width - 1)];
                    value = 0.5 * pHeight[j] + pResultBuffer[j * width + (width - 1)];
                    if (value < 0) value = 0;
                    if (value > 255) value = 255;
                    pResultBuffer[j * width + (width - 1)] = (byte)value;
                    for (i = width - 2; i >= 0; i--)
                    {
                        pHeight[j] = frequency * pHeight[j] + (1 - frequency) * pBuffer[j * width + i];
                        value = pResultBuffer[j * width + i] + 0.5 * pHeight[j];
                        if (value < 0) value = 0;
                        if (value > 255) value = 255;
                        pResultBuffer[j * width + i] = (byte)value;
                    }
                }
                ////  세로방향 Filtering
                for (i = 0; i < width; i++)
                {
                    pWidth[i] = pResultBuffer[0 * width + i];
                    pResultBuffer[0 * width + i] = (byte)(0.5 * pWidth[i]);
                    for (j = 1; j < height; j++)
                    {
                        pWidth[i] = frequency * pWidth[i] + (1 - frequency) * pResultBuffer[j * width + i];
                        value = 0.5 * pWidth[i];
                        if (value < 0) value = 0;
                        if (value > 255) value = 255;
                        pResultBuffer[j * width + i] = (byte)value;
                    }
                }
                for (i = 0; i < width; i++)
                {
                    pWidth[i] = pResultBuffer[(height - 1) * width + i];
                    value = 0.5 * pWidth[i] + pResultBuffer[(height - 1) * width + i];
                    if (value < 0) value = 0;
                    if (value > 255) value = 255;
                    pResultBuffer[(height - 1) * width + i] = (byte)value;
                    for (j = height - 2; j >= 0; j--)
                    {
                        pWidth[i] = frequency * pWidth[i] + (1 - frequency) * pResultBuffer[j * width + i];
                        value = pResultBuffer[j * width + i] + 0.5 * pWidth[i];
                        if (value < 0) value = 0;
                        if (value > 255) value = 255;
                        pResultBuffer[j * width + i] = (byte)value;
                    }
                }
                ////  RC 결과와 원본 영상을 합친 경우.
                if (bSumSource)
                {
                    pResultBuffer = TwoImageProcess.Combine(pBuffer, pResultBuffer, width, height);
                }
                return pResultBuffer;
            }

            public static YoonImage Level2D(YoonImage pSourceImage, ref double dSum)
            {
                if (pSourceImage.Format != PixelFormat.Format8bppIndexed)
                    throw new FormatException("[YOONIMAGE EXCEPTION] Image arguments is not 8bit format");
                return new YoonImage(Level2D(pSourceImage.GetGrayBuffer(), pSourceImage.Width, pSourceImage.Height, ref dSum),
                    pSourceImage.Width, pSourceImage.Height, PixelFormat.Format8bppIndexed);
            }

            public static byte[] Level2D(byte[] pBuffer, int width, int height, ref double sum)
            {
                double diffDx, diffDy;
                double inverseDx, inverseDy;
                int centerX, centerY;
                int count;
                int x1, x2, y1, y2;
                double[,] pAverage = new double[2, 2];
                double value;
                byte[] pResultBuffer = new byte[width * height];
                inverseDx = 1 / (double)width;
                inverseDy = 1 / (double)height;
                centerX = width / 2;
                centerY = height / 2;
                count = 0;
                sum = 0;
                x1 = 0;
                y1 = 0;
                x2 = 0;
                y2 = 0;
                ////  Level 필터 적용하기.
                for (int iNo = 0; iNo < 4; iNo++)
                {
                    int iRow = iNo / 2;
                    int iCol = iNo % 2;
                    count = 0;
                    //////  Source 크기에 맞게 각기 다른 4개의 Filter 배열 생성하기.
                    switch (iNo)
                    {
                        case 0:
                            x1 = 2;
                            y1 = 2;
                            x2 = centerX / 2 - 2;
                            y2 = centerY / 2 - 2;
                            break;
                        case 1:
                            x1 = centerX + centerX / 2;
                            x2 = width - 2;
                            y1 = 2;
                            y2 = centerY / 2 - 2;
                            break;
                        case 2:
                            x1 = 2;
                            y1 = centerY + centerY / 2;
                            x2 = centerX / 2 - 2;
                            y2 = height - 2;
                            break;
                        case 3:
                            x1 = centerX + centerX / 2;
                            y1 = centerY + centerY / 2;
                            x2 = width - 2;
                            y2 = height - 2;
                            break;
                    }
                    //////  4개 Filter를 통과한 배열의 평균값 구하기.
                    for (int j = y1; j < y2; j += 2)
                    {
                        for (int i = x1; i < x2; i += 2)
                        {
                            pAverage[iRow, iCol] = pAverage[iRow, iCol] + pBuffer[j * width + i];
                            count++;
                        }
                    }
                    ////  4가지 Filter 간의 평균값 구하기.
                    if (count > 1) pAverage[iRow, iCol] = pAverage[iRow, iCol] / (float)count;
                    else pAverage[iRow, iCol] = 0;
                    sum += pAverage[iRow, iCol];
                }
                sum /= (float)4.0;
                //// 역수곱 계산
                diffDx = inverseDx * (pAverage[1, 0] + pAverage[1, 1] - pAverage[0, 0] - pAverage[0, 1]);
                diffDy = inverseDy * (pAverage[0, 1] + pAverage[1, 1] - pAverage[0, 0] - pAverage[1, 0]);
                ////  Filtering 결과 출력.
                for (int j = 0; j < height; j++)
                {
                    for (int i = 0; i < width; i++)
                    {
                        value = 100 * pBuffer[j * width + i] / (sum + diffDx * (i - centerX) + diffDy * (j - centerY));
                        if (value > 255) value = 255;
                        if (value < 0) value = 0;
                        pResultBuffer[j * width + i] = (byte)value;
                    }
                }
                return pResultBuffer;
            }

            public static YoonImage DeMargin2D(YoonImage pSourceImage, ref double dSum)
            {
                if (pSourceImage.Format != PixelFormat.Format8bppIndexed)
                    throw new FormatException("[YOONIMAGE EXCEPTION] Image arguments is not 8bit format");
                return new YoonImage(DeMargin2D(pSourceImage.GetGrayBuffer(), pSourceImage.Width, pSourceImage.Height),
                    pSourceImage.Width, pSourceImage.Height, PixelFormat.Format8bppIndexed);
            }

            public static byte[] DeMargin2D(byte[] pBuffer, int width, int height)
            {
                int centerX, centerY;
                float[] pWidth, pHeight;
                float norm;
                byte[] pResultBuffer = new byte[width * height];
                centerX = width / 2;
                centerY = height / 2;
                pWidth = new float[width];
                pHeight = new float[height];
                ////  각 행별 Data를 Filter에 더하기.
                for (int j = 0; j < height; j++)
                {
                    pHeight[j] = pBuffer[j * width + 0];
                    for (int i = 1; i < width; i++)
                    {
                        pHeight[j] = pHeight[j] + pBuffer[j * width + i];
                    }
                }
                ////  Filter 중심 기준의 배율로 Filter 덮어쓰기.
                norm = pHeight[centerY];
                for (int j = 0; j < height; j++)
                {
                    if (pHeight[j] > 1)
                        pHeight[j] = norm / pHeight[j];
                    else
                        pHeight[j] = 1;
                }
                ////  세로 방향 Filter를 Buffer에 적용하기.
                for (int j = 0; j < height; j++)
                    for (int i = 0; i < width; i++)
                        pResultBuffer[j * width + i] = (byte)(pHeight[j] * pBuffer[j * width + i]);
                ////  각 열별 Data를 Filter에 더하기.
                for (int i = 0; i < width; i++)
                {
                    pWidth[i] = pResultBuffer[0 * width + i];
                    for (int j = 0; j < height; j++)
                    {
                        pWidth[i] = pWidth[i] + pResultBuffer[j * width + i];
                    }
                }
                ////  Filter 중심 기준의 배율로 Filter 덮어쓰기.
                norm = pWidth[centerX];
                for (int i = 0; i < width; i++)
                {
                    if (pWidth[i] > 0)
                        pWidth[i] = norm / pWidth[i];
                    else
                        pWidth[i] = 1;
                }
                ////  가로 방향 Filter를 Buffer에 적용하기.
                for (int j = 0; j < height; j++)
                    for (int i = 0; i < width; i++)
                        pResultBuffer[j * width + i] = (byte)(pWidth[i] * pResultBuffer[j * width + i]);
                return pResultBuffer;
            }

            public static YoonImage Smooth1D(YoonImage pSourceImage, int nMargin = 1, int nStep = 3)
            {
                if (pSourceImage.Format != PixelFormat.Format8bppIndexed)
                    throw new FormatException("[YOONIMAGE EXCEPTION] Image arguments is not 8bit format");
                return new YoonImage(Smooth1D(pSourceImage.GetGrayBuffer(), pSourceImage.Width * pSourceImage.Height, nMargin, nStep),
                    pSourceImage.Width, pSourceImage.Height, PixelFormat.Format8bppIndexed);
            }

            public static byte[] Smooth1D(byte[] pBuffer, int bufferSize, int margin, int step)
            {
                int x, i, ii;
                int sum, count;
                byte[] pResultBuffer = new byte[bufferSize];
                sum = 0;
                count = 0;
                if (step < 1)
                    step = 1;
                for (i = margin; i < bufferSize - margin; i++)
                {
                    sum = 0;
                    count = 0;
                    ////  (marginX2) 정도의 크기만큼을 Sampling 해야한다.
                    for (ii = -margin; ii <= margin; ii += step)
                    {
                        x = i + ii;
                        if (x >= bufferSize)
                            continue;
                        sum += pBuffer[x];
                        count++;
                    }
                    if (count < 1)
                        count = 1;
                    //////  Sampling한 주변 Pixel들의 Gray Level을 Buffer에 넣는다. (평균값 필터)
                    pResultBuffer[i] = (byte)(sum / count);
                }
                return pResultBuffer;
            }

            public static YoonImage Smooth2D(YoonImage pSourceImage, int nStep = 5)
            {
                if (pSourceImage.Format != PixelFormat.Format8bppIndexed)
                    throw new FormatException("[YOONIMAGE EXCEPTION] Image arguments is not 8bit format");
                return new YoonImage(Smooth2D(pSourceImage.GetGrayBuffer(), pSourceImage.Width, pSourceImage.Height, nStep),
                    pSourceImage.Width, pSourceImage.Height, PixelFormat.Format8bppIndexed);
            }

            public static byte[] Smooth2D(byte[] pBuffer, int width, int height, int nBlurStep)
            {
                if (width < 1 || height < 1 || pBuffer == null)
                    throw new ArgumentException("[YOONIMAGE EXCEPTION] Buffer size is not normalized");
                int i, j, ii, jj, x, y;
                int count, sum;
                int stepSize;
                byte[] pResultBuffer = new byte[width * height];
                stepSize = 2 * nBlurStep / 10;
                if (stepSize < 1)
                    stepSize = 1;
                ////  각 Pixel마다 평균값 필터 씌우기.
                for (j = 0; j < height; j++)
                {
                    count = 0;
                    sum = 0;
                    for (i = 0; i < width; i++)
                    {
                        sum = 0;
                        count = 0;
                        //////  각 Pixel에서 좌우로 Blur Size만큼의 Sampling Pixel을 추출해서 평균값을 구한다.
                        for (jj = -nBlurStep; jj <= nBlurStep; jj += stepSize)
                        {
                            y = j + jj;
                            for (ii = -nBlurStep; ii <= nBlurStep; ii += stepSize)
                            {
                                x = i + ii;
                                if (x < 0 || y < 0 || x >= width || y >= height)
                                    continue;
                                sum += pBuffer[y * width + x];
                                count++;
                            }
                        }
                        if (count < 1)
                            count = 1;
                        //////  Sampling으로 구한 평균값을 그대로 각 Pixel에 씌운다.
                        pResultBuffer[j * width + i] = (byte)(sum / count);
                    }
                }
                return pResultBuffer;
            }
        }

        // Pixel 채우기
        public static class Fill
        {
            public static YoonImage FillBound(YoonImage pSourceImage, int nValue)
            {
                if (pSourceImage.Plane == 1)
                    return new YoonImage(FillBound(pSourceImage.GetGrayBuffer(), pSourceImage.Width, pSourceImage.Height, (byte)nValue),
                        pSourceImage.Width, pSourceImage.Height, 1);
                else if (pSourceImage.Plane == 4)
                    return new YoonImage(FillBound(pSourceImage.GetARGBBuffer(), pSourceImage.Width, pSourceImage.Height, nValue),
                        pSourceImage.Width, pSourceImage.Height, 4);
                else
                    throw new FormatException("[YOONIMAGE EXCEPTION] Image format is not correct");
            }

            //  테두리 채우기
            public static int[] FillBound(int[] pBuffer, int width, int height, int value)
            {
                int[] pResultBuffer = new int[width * height];
                Array.Copy(pBuffer, pResultBuffer, width * height);
                ////  양끝단만 한줄씩 지운다.
                for (int x = 0; x < width; x++)
                {
                    pResultBuffer[0 * width + x] = value;
                    pResultBuffer[(height - 1) * width + x] = value;
                }
                for (int y = 0; y < height; y++)
                {
                    pResultBuffer[y * width + 0] = value;
                    pResultBuffer[y * width + (width - 1)] = value;
                }
                return pResultBuffer;
            }

            //  테두리 채우기
            public static byte[] FillBound(byte[] pBuffer, int width, int height, byte value)
            {
                byte[] pResultBuffer = new byte[width * height];
                Array.Copy(pBuffer, pResultBuffer, width * height);
                ////  양끝단만 한줄씩 지운다.
                for (int x = 0; x < width; x++)
                {
                    pResultBuffer[0 * width + x] = value;
                    pResultBuffer[(height - 1) * width + x] = value;
                }
                for (int y = 0; y < height; y++)
                {
                    pResultBuffer[y * width + 0] = value;
                    pResultBuffer[y * width + (width - 1)] = value;
                }
                return pResultBuffer;
            }

            public static YoonImage FillFlood(YoonImage pSourceImage, YoonVector2N pVector, int nThreshold = 128, bool bFillWhite = true, int nValue = 0)
            {
                int iFillCount = 0;
                int iTotalCount = 0;
                if (pSourceImage.Plane == 1)
                {
                    byte[] pBuffer = pSourceImage.GetGrayBuffer();
                    FillFlood(ref pBuffer, ref iFillCount, pSourceImage.Width, pSourceImage.Height, pVector, (byte)nThreshold, bFillWhite, (byte)nValue, ref iTotalCount);
                    return new YoonImage(pBuffer, pSourceImage.Width, pSourceImage.Height, 1);
                }

                else if (pSourceImage.Plane == 4)
                {
                    int[] pBuffer = pSourceImage.GetARGBBuffer();
                    FillFlood(ref pBuffer, ref iFillCount, pSourceImage.Width, pSourceImage.Height, pVector, nThreshold, bFillWhite, nValue, ref iTotalCount);
                    return new YoonImage(pBuffer, pSourceImage.Width, pSourceImage.Height, 1);
                }
                else
                    throw new FormatException("[YOONIMAGE EXCEPTION] Image format is not correct");
            }

            //  영역 가득 채우기  (영역 내에 value값 채우기)
            public static bool FillFlood(ref int[] pBuffer, ref int fillCount, int width, int height, YoonVector2N pVector, int threshold, bool isWhite, int value, ref int totalCount)
            {
                if (pVector.X < 0 || pVector.X >= width) return true;
                if (pVector.Y < 0 || pVector.Y >= height) return true;
                totalCount++;   // FillFlood의 동작 횟수
                if (totalCount > MAX_FILL_NUM) return false;
                //// x, y가 지정치보다 크거나, stact count가 높을 때 Value 값 채우기를 그만한다.
                if (isWhite)
                {
                    if (pBuffer[pVector.Y * width + pVector.X] >= threshold)
                    {
                        pBuffer[pVector.Y * width + pVector.X] = value;
                        ////  Object 찾기와 연동하기 위한 Counter.
                        fillCount++;    // 실제로 Fill 된 Count
                        ////  8방향으로 Flood 영역을 찾는 재귀 함수.
                        foreach (eYoonDir2D nDir in YoonDirFactory.GetClockDirections())
                        {
                            FillFlood(ref pBuffer, ref fillCount, width, height, (YoonVector2N)pVector.GetNextVector(nDir), threshold, isWhite, value, ref totalCount);
                        }
                    }
                }
                else
                {
                    if (pBuffer[pVector.Y * width + pVector.X] < threshold)
                    {
                        pBuffer[pVector.Y * width + pVector.X] = value;
                        fillCount++;
                        ////  화면에 Display할 경우 사용함.
                        ////  8방향으로 Flood 영역을 찾는 재귀 함수.
                        foreach (eYoonDir2D nDir in YoonDirFactory.GetClockDirections())
                        {
                            FillFlood(ref pBuffer, ref fillCount, width, height, (YoonVector2N)pVector.GetNextVector(nDir), threshold, isWhite, value, ref totalCount);
                        }
                    }
                }
                return true;
            }

            //  영역 가득 채우기  (영역 내에 value값 채우기)
            public static bool FillFlood(ref byte[] pBuffer, ref int fillCount, int width, int height, YoonVector2N pVector, byte threshold, bool isWhite, byte value, ref int totalCount)
            {
                if (pVector.X < 0 || pVector.X >= width) return true;
                if (pVector.Y < 0 || pVector.Y >= height) return true;
                totalCount++;   // FillFlood의 동작 횟수
                if (totalCount > MAX_FILL_NUM) return false;
                //// x, y가 지정치보다 크거나, stact count가 높을 때 Value 값 채우기를 그만한다.
                if (isWhite)
                {
                    if (pBuffer[pVector.Y * width + pVector.X] >= threshold)
                    {
                        pBuffer[pVector.Y * width + pVector.X] = value;
                        ////  Object 찾기와 연동하기 위한 Counter.
                        fillCount++;    // 실제로 Fill 된 Count
                        ////  8방향으로 Flood 영역을 찾는 재귀 함수.
                        foreach (eYoonDir2D nDir in YoonDirFactory.GetClockDirections())
                        {
                            FillFlood(ref pBuffer, ref fillCount, width, height, (YoonVector2N)pVector.GetNextVector(nDir), threshold, isWhite, value, ref totalCount);
                        }
                    }
                }
                else
                {
                    if (pBuffer[pVector.Y * width + pVector.X] < threshold)
                    {
                        pBuffer[pVector.Y * width + pVector.X] = value;
                        fillCount++;
                        ////  화면에 Display할 경우 사용함.
                        ////  8방향으로 Flood 영역을 찾는 재귀 함수.
                        foreach (eYoonDir2D nDir in YoonDirFactory.GetClockDirections())
                        {
                            FillFlood(ref pBuffer, ref fillCount, width, height, (YoonVector2N)pVector.GetNextVector(nDir), threshold, isWhite, value, ref totalCount);
                        }
                    }
                }
                return true;
            }

            public static YoonImage FillInside1D(YoonImage pSourceImage, int nThreshold = 128, bool bFillWhite = true, int nSize = 5)
            {
                if (pSourceImage.Plane == 1)
                    return new YoonImage(FillInside1D(pSourceImage.GetGrayBuffer(), pSourceImage.Width * pSourceImage.Height, (byte)nThreshold, bFillWhite, nSize),
                        pSourceImage.Width, pSourceImage.Height, PixelFormat.Format32bppArgb);
                else if (pSourceImage.Plane == 4)
                    return new YoonImage(FillInside1D(pSourceImage.GetARGBBuffer(), pSourceImage.Width * pSourceImage.Height, nThreshold, bFillWhite, nSize),
                        pSourceImage.Width, pSourceImage.Height, PixelFormat.Format32bppArgb);
                else
                    throw new FormatException("[YOONIMAGE EXCEPTION] Image format is not correct");
            }

            //  Pixel을 채우는 함수.
            public static int[] FillInside1D(int[] pBuffer, int bufferSize, int threshold, bool isWhite, int size)
            {
                int i;
                int start, end, differential;
                int intensity;
                int[] pResultBuffer = new int[bufferSize];
                Array.Copy(pBuffer, pResultBuffer, bufferSize);
                ////  흰색으로 채울 경우.
                if (isWhite)
                {
                    start = -1;
                    end = -1;
                    for (i = 0; i < bufferSize; i++)
                    {
                        if (pBuffer[i] < threshold)
                        {
                            if (start < 0)
                                start = i;
                        }
                        else
                        {
                            if (start > 0 && end < 0)
                            {
                                intensity = pBuffer[i];
                                end = i;
                                differential = end - start;
                                if (differential <= size)
                                {
                                    for (int ii = start; ii <= end; ii++)
                                    {
                                        pResultBuffer[ii] = intensity;
                                    }
                                }
                            }
                            start = -1;
                            end = -1;
                        }
                    }
                }
                ////  검은색으로 채울 경우.
                else
                {
                    start = -1;
                    end = -1;
                    for (i = 0; i < bufferSize; i++)
                    {
                        if (pBuffer[i] >= threshold)
                        {
                            if (start < 0)
                                start = i;
                        }
                        else
                        {
                            if (start > 0 && end < 0)
                            {
                                intensity = pBuffer[i];
                                end = i;
                                differential = end - start;
                                if (differential <= size)
                                {
                                    for (int ii = start; ii <= end; ii++)
                                    {
                                        pResultBuffer[ii] = 0;
                                    }
                                }
                            }
                            start = -1;
                            end = -1;
                        }
                    }
                }
                return pResultBuffer;
            }

            //  Pixel을 채우는 함수.
            public static byte[] FillInside1D(byte[] pBuffer, int bufferSize, byte threshold, bool isWhite, int size)
            {
                int i;
                int start, end, differential;
                byte intensity;
                byte[] pResultBuffer = new byte[bufferSize];
                Array.Copy(pBuffer, pResultBuffer, bufferSize);
                ////  흰색으로 채울 경우.
                if (isWhite)
                {
                    start = -1;
                    end = -1;
                    for (i = 0; i < bufferSize; i++)
                    {
                        if (pBuffer[i] < threshold)
                        {
                            if (start < 0)
                                start = i;
                        }
                        else
                        {
                            if (start > 0 && end < 0)
                            {
                                intensity = pBuffer[i];
                                end = i;
                                differential = end - start;
                                if (differential <= size)
                                {
                                    for (int ii = start; ii <= end; ii++)
                                    {
                                        pResultBuffer[ii] = intensity;
                                    }
                                }
                            }
                            start = -1;
                            end = -1;
                        }
                    }
                }
                ////  검은색으로 채울 경우.
                else
                {
                    start = -1;
                    end = -1;
                    for (i = 0; i < bufferSize; i++)
                    {
                        if (pBuffer[i] >= threshold)
                        {
                            if (start < 0)
                                start = i;
                        }
                        else
                        {
                            if (start > 0 && end < 0)
                            {
                                intensity = pBuffer[i];
                                end = i;
                                differential = end - start;
                                if (differential <= size)
                                {
                                    for (int ii = start; ii <= end; ii++)
                                    {
                                        pResultBuffer[ii] = intensity;
                                    }
                                }
                            }
                            start = -1;
                            end = -1;
                        }
                    }
                }
                return pResultBuffer;
            }

            public static YoonImage FillInside2D(YoonImage pSourceImage, YoonRect2N scanArea, eYoonDir2DMode nDirMode, int nThreshold = 128, bool bFillWhite = true, int nSize = 5)
            {
                if (pSourceImage.Plane == 1)
                {
                    if (nDirMode == eYoonDir2DMode.AxisX)
                        return new YoonImage(FillHorizontal(pSourceImage.GetGrayBuffer(), pSourceImage.Width, scanArea, (byte)nThreshold, bFillWhite, nSize),
                            pSourceImage.Width, pSourceImage.Height, PixelFormat.Format32bppArgb);
                    else if (nDirMode == eYoonDir2DMode.AxisY)
                        return new YoonImage(FillVertical(pSourceImage.GetGrayBuffer(), pSourceImage.Width, scanArea, (byte)nThreshold, bFillWhite, nSize),
                            pSourceImage.Width, pSourceImage.Height, PixelFormat.Format32bppArgb);
                    else
                        throw new ArgumentException("[YOONIMAGE EXCEPTION] Direction of filling is not correct");

                }
                else if (pSourceImage.Plane == 4)
                {
                    if (nDirMode == eYoonDir2DMode.AxisX)
                        return new YoonImage(FillHorizontal(pSourceImage.GetARGBBuffer(), pSourceImage.Width, scanArea, nThreshold, bFillWhite, nSize),
                            pSourceImage.Width, pSourceImage.Height, PixelFormat.Format32bppArgb);
                    else if (nDirMode == eYoonDir2DMode.AxisY)
                        return new YoonImage(FillVertical(pSourceImage.GetARGBBuffer(), pSourceImage.Width, scanArea, nThreshold, bFillWhite, nSize),
                            pSourceImage.Width, pSourceImage.Height, PixelFormat.Format32bppArgb);
                    else
                        throw new ArgumentException("[YOONIMAGE EXCEPTION] Direction of filling is not correct");
                }
                else
                    throw new FormatException("[YOONIMAGE EXCEPTION] Image format is not correct");
            }

            //  수평 방향으로 Pixel을 채우는 함수.
            public static byte[] FillHorizontal(byte[] pBuffer, int imageWidth, YoonRect2N scanArea, byte threshold, bool isWhite, int size)
            {
                if (scanArea == null)
                    throw new NullReferenceException("[YOONIMAGE EXCEPTION] Scan area has null reference");
                int i, j;
                int startX, endX, differX;
                byte intensity;
                byte[] pResultBuffer = new byte[pBuffer.Length];
                Array.Copy(pBuffer, pResultBuffer, pBuffer.Length);
                ////  흰색으로 채울 경우.
                if (isWhite)
                {
                    for (j = scanArea.Top; j < scanArea.Bottom; j++)
                    {
                        startX = -1;
                        endX = -1;
                        for (i = scanArea.Left; i < scanArea.Right; i++)
                        {
                            ////  시작 지점이 검은색일 경우. (시작지점의 색으로 채우기를 못함)
                            if (pBuffer[j * imageWidth + i] < threshold)
                            {
                                if (startX < 0)
                                    startX = i;
                            }
                            ////  시작 지점이  흰색이기 때문에 시작지점의 색으로 채우기 가능.
                            else
                            {
                                if (startX > scanArea.Left && endX < 0)
                                {
                                    intensity = pBuffer[j * imageWidth + i];
                                    endX = i;
                                    differX = endX - startX;
                                    if (differX <= size)
                                    {
                                        for (int ii = startX; ii <= endX; ii++)
                                        {
                                            pResultBuffer[j * imageWidth + ii] = intensity;
                                        }
                                    }
                                }
                                startX = -1;
                                endX = -1;
                            }
                        }
                    }
                }
                ////  검은색으로 채울 경우.
                else
                {
                    for (j = (int)scanArea.Top; j < scanArea.Bottom; j++)
                    {
                        startX = -1;
                        endX = -1;
                        for (i = (int)scanArea.Left; i < scanArea.Right; i++)
                        {
                            ////  시작 지점이 흰색일 경우.  (시작지점의 색으로 채우기를 못함)
                            if (pBuffer[j * imageWidth + i] >= threshold)
                            {
                                if (startX < 0)
                                    startX = i;
                            }
                            ////  시작 지점이 검은색이기 때문에 시작지점 색으로 채우기 가능.
                            else
                            {
                                //////  단, 처음부터 색을 채워나가는 것은 막아야한다.
                                if (startX > scanArea.Left && endX < 0)
                                {
                                    intensity = pBuffer[j * imageWidth + i];
                                    endX = i;
                                    differX = endX - startX;
                                    if (differX <= size)
                                    {
                                        for (int ii = startX; ii <= endX; ii++)
                                        {
                                            pResultBuffer[j * imageWidth + ii] = 0;
                                        }
                                    }
                                }
                                startX = -1;
                                endX = -1;
                            }
                        }
                    }
                }
                return pResultBuffer;
            }

            //  수평 방향으로 Pixel을 채우는 함수.
            public static int[] FillHorizontal(int[] pBuffer, int imageWidth, YoonRect2N scanArea, int threshold, bool isWhite, int size)
            {
                if (scanArea == null)
                    throw new NullReferenceException("[YOONIMAGE EXCEPTION] Scan area has null reference");
                int i, j;
                int startX, endX, differX;
                int intensity;
                int[] pResultBuffer = new int[pBuffer.Length];
                Array.Copy(pBuffer, pResultBuffer, pBuffer.Length);
                ////  흰색으로 채울 경우.
                if (isWhite)
                {
                    for (j = scanArea.Top; j < scanArea.Bottom; j++)
                    {
                        startX = -1;
                        endX = -1;
                        for (i = scanArea.Left; i < scanArea.Right; i++)
                        {
                            ////  시작 지점이 검은색일 경우. (시작지점의 색으로 채우기를 못함)
                            if (pBuffer[j * imageWidth + i] < threshold)
                            {
                                if (startX < 0)
                                    startX = i;
                            }
                            ////  시작 지점이  흰색이기 때문에 시작지점의 색으로 채우기 가능.
                            else
                            {
                                if (startX > scanArea.Left && endX < 0)
                                {
                                    intensity = pBuffer[j * imageWidth + i];
                                    endX = i;
                                    differX = endX - startX;
                                    if (differX <= size)
                                    {
                                        for (int ii = startX; ii <= endX; ii++)
                                        {
                                            pResultBuffer[j * imageWidth + ii] = intensity;
                                        }
                                    }
                                }
                                startX = -1;
                                endX = -1;
                            }
                        }
                    }
                }
                ////  검은색으로 채울 경우.
                else
                {
                    for (j = (int)scanArea.Top; j < scanArea.Bottom; j++)
                    {
                        startX = -1;
                        endX = -1;
                        for (i = (int)scanArea.Left; i < scanArea.Right; i++)
                        {
                            ////  시작 지점이 흰색일 경우.  (시작지점의 색으로 채우기를 못함)
                            if (pBuffer[j * imageWidth + i] >= threshold)
                            {
                                if (startX < 0)
                                    startX = i;
                            }
                            ////  시작 지점이 검은색이기 때문에 시작지점 색으로 채우기 가능.
                            else
                            {
                                //////  단, 처음부터 색을 채워나가는 것은 막아야한다.
                                if (startX > scanArea.Left && endX < 0)
                                {
                                    intensity = pBuffer[j * imageWidth + i];
                                    endX = i;
                                    differX = endX - startX;
                                    if (differX <= size)
                                    {
                                        for (int ii = startX; ii <= endX; ii++)
                                        {
                                            pResultBuffer[j * imageWidth + ii] = 0;
                                        }
                                    }
                                }
                                startX = -1;
                                endX = -1;
                            }
                        }
                    }
                }
                return pResultBuffer;
            }

            //  수직 방향으로 Pixel을 채우는 함수.
            public static byte[] FillVertical(byte[] pBuffer, int imageWidth, YoonRect2N scanArea, byte threshold, bool isWhite, int size)
            {
                if (scanArea == null)
                    throw new NullReferenceException("[YOONIMAGE EXCEPTION] Scan area has null reference");
                int i, j;
                int startY, endY, differY;
                byte intensity;
                byte[] pResultBuffer = new byte[pBuffer.Length];
                Array.Copy(pBuffer, pResultBuffer, pBuffer.Length);
                ////  흰색으로 채울 경우.
                if (isWhite)
                {
                    for (i = scanArea.Left; i < scanArea.Right; i++)
                    {
                        startY = -1;
                        endY = -1;
                        for (j = scanArea.Top; j < scanArea.Bottom; j++)
                        {
                            if (pBuffer[j * imageWidth + i] < threshold)
                            {
                                if (startY < 0)
                                    startY = j;
                            }
                            else
                            {
                                if (startY > scanArea.Top && endY < 0)
                                {
                                    intensity = pBuffer[j * imageWidth + i];
                                    endY = j;
                                    differY = endY - startY;
                                    if (differY <= size)
                                    {
                                        for (int jj = startY; jj <= endY; jj++)
                                        {
                                            pResultBuffer[jj * imageWidth + i] = intensity;
                                        }
                                    }
                                }
                                startY = -1;
                                endY = -1;
                            }
                        }
                    }
                }
                ////  검은색으로 채울 경우.
                else
                {
                    for (i = scanArea.Left; i < scanArea.Right; i++)
                    {
                        startY = -1;
                        endY = -1;
                        for (j = scanArea.Top; j < scanArea.Bottom; j++)
                        {
                            if (pBuffer[j * imageWidth + i] >= threshold)
                            {
                                if (startY < 0)
                                    startY = j;
                            }
                            else
                            {
                                if (startY > scanArea.Top && endY < 0)
                                {
                                    intensity = pBuffer[j * imageWidth + i];
                                    endY = j;
                                    differY = endY - startY;
                                    if (differY <= size)
                                    {
                                        for (int jj = startY; jj <= endY; jj++)
                                        {
                                            pResultBuffer[jj * imageWidth + i] = intensity;
                                        }
                                    }
                                }
                                startY = -1;
                                endY = -1;
                            }
                        }
                    }
                }
                return pResultBuffer;
            }

            //  수직 방향으로 Pixel을 채우는 함수.
            public static int[] FillVertical(int[] pBuffer, int imageWidth, YoonRect2N scanArea, int threshold, bool isWhite, int size)
            {
                if (scanArea == null)
                    throw new NullReferenceException("[YOONIMAGE EXCEPTION] Scan area has null reference");
                int i, j;
                int startY, endY, differY;
                int intensity;
                int[] pResultBuffer = new int[pBuffer.Length];
                Array.Copy(pBuffer, pResultBuffer, pBuffer.Length);
                ////  흰색으로 채울 경우.
                if (isWhite)
                {
                    for (i = scanArea.Left; i < scanArea.Right; i++)
                    {
                        startY = -1;
                        endY = -1;
                        for (j = scanArea.Top; j < scanArea.Bottom; j++)
                        {
                            if (pBuffer[j * imageWidth + i] < threshold)
                            {
                                if (startY < 0)
                                    startY = j;
                            }
                            else
                            {
                                if (startY > scanArea.Top && endY < 0)
                                {
                                    intensity = pBuffer[j * imageWidth + i];
                                    endY = j;
                                    differY = endY - startY;
                                    if (differY <= size)
                                    {
                                        for (int jj = startY; jj <= endY; jj++)
                                        {
                                            pResultBuffer[jj * imageWidth + i] = intensity;
                                        }
                                    }
                                }
                                startY = -1;
                                endY = -1;
                            }
                        }
                    }
                }
                ////  검은색으로 채울 경우.
                else
                {
                    for (i = scanArea.Left; i < scanArea.Right; i++)
                    {
                        startY = -1;
                        endY = -1;
                        for (j = scanArea.Top; j < scanArea.Bottom; j++)
                        {
                            if (pBuffer[j * imageWidth + i] >= threshold)
                            {
                                if (startY < 0)
                                    startY = j;
                            }
                            else
                            {
                                if (startY > scanArea.Top && endY < 0)
                                {
                                    intensity = pBuffer[j * imageWidth + i];
                                    endY = j;
                                    differY = endY - startY;
                                    if (differY <= size)
                                    {
                                        for (int jj = startY; jj <= endY; jj++)
                                        {
                                            pResultBuffer[jj * imageWidth + i] = intensity;
                                        }
                                    }
                                }
                                startY = -1;
                                endY = -1;
                            }
                        }
                    }
                }
                return pResultBuffer;
            }
        }

        // 객체 찾기
        public static class ObjectDetection
        {
            //  최대 크기 객체 찾기.
            public static IYoonObject FindMaxObject(YoonImage pSourceImage, YoonRect2N scanArea, byte nThreshold = 128, bool bWhite = false)
            {
                if (pSourceImage.Format != PixelFormat.Format8bppIndexed)
                    throw new FormatException("[YOONIMAGE EXCEPTION] Image format is not correct");
                return FindMaxObject(pSourceImage.GetGrayBuffer(), pSourceImage.Width, scanArea, nThreshold, bWhite);
            }

            public static IYoonObject FindMaxObject(byte[] pBuffer, int imageWidth, YoonRect2N scanArea, byte threshold, bool bWhite, bool bSquareOnly = false, bool bNormalOnly = false)
            {
                YoonRect2N maxArea;
                int maxLen = 0;
                int maxLabel = 0;
                int width, height, len;
                double maxScore = 0.0;
                ////  Object 찾기 작
                ObjectList<YoonRect2N> pListObjectInfo = FindObjects(pBuffer, imageWidth, scanArea, threshold, bWhite);
                maxLen = 0;
                maxArea = new YoonRect2N(0, 0, 0, 0);
                for (int iObject = 0; iObject < pListObjectInfo.Count; iObject++)
                {
                    width = pListObjectInfo[iObject].Object.Width;
                    height = pListObjectInfo[iObject].Object.Height;
                    len = pListObjectInfo[iObject].PixelCount;
                    ////  정사각에 가까운 Object만 취급함.
                    if (bSquareOnly)
                    {
                        if (width > 3 * height || height > 3 * width)
                        {
                            pListObjectInfo.RemoveAt(iObject);
                            continue;
                        }
                    }
                    ////  시작지점이 정상적(양수)인 경우만 취급함.
                    if (bNormalOnly)
                    {
                        int diffLeft = pListObjectInfo[iObject].Object.Left;
                        int diffTop = pListObjectInfo[iObject].Object.Top;
                        if (diffLeft <= 1 || diffTop <= 1)
                        {
                            pListObjectInfo.RemoveAt(iObject);
                            continue;
                        }
                    }
                    ////  발견된 object의 길이가 최대치일 때.
                    if (len > maxLen)
                    {
                        maxLen = len;
                        maxLabel = pListObjectInfo[iObject].Label;
                        maxScore = pListObjectInfo[iObject].Score;
                        maxArea.CenterPos.X = scanArea.Left + pListObjectInfo[iObject].Object.Left + pListObjectInfo[iObject].Object.Width / 2;
                        maxArea.CenterPos.Y = scanArea.Top + pListObjectInfo[iObject].Object.Top + pListObjectInfo[iObject].Object.Height / 2;
                        maxArea.Width = pListObjectInfo[iObject].Object.Right - pListObjectInfo[iObject].Object.Left;
                        maxArea.Height = pListObjectInfo[iObject].Object.Bottom - pListObjectInfo[iObject].Object.Top;
                    }
                }
                return new YoonObject<YoonRect2N>(maxLabel, maxArea, maxScore, maxLen);
            }

            //  최대 크기 객체 찾기.
            public static IYoonObject FindMaxObject(YoonImage pSourceImage, byte nThreshold = 128, bool bWhite = false)
            {
                if (pSourceImage.Format != PixelFormat.Format8bppIndexed)
                    throw new FormatException("[YOONIMAGE EXCEPTION] Image format is not correct");
                return FindMaxObject(pSourceImage.GetGrayBuffer(), pSourceImage.Width, pSourceImage.Height, nThreshold, bWhite);
            }

            public static IYoonObject FindMaxObject(byte[] pBuffer, int imageWidth, int imageHeight, byte threshold, bool isWhite, bool bSquareOnly = false, bool bNormalOnly = false)
            {
                YoonRect2N maxArea;
                int maxLen = 0;
                int maxLabel = 0;
                int width, height, len;
                double maxScore = 0.0;
                ////  객체 찾기. 찾은 객체 정보는 m_objectInfo에 저장.
                ObjectList<YoonRect2N> pListObjectInfo = FindObjects(pBuffer, imageWidth, imageHeight, threshold, isWhite);
                maxLen = 0;
                maxArea = new YoonRect2N(0, 0, 0, 0);
                for (int iObject = 0; iObject < pListObjectInfo.Count; iObject++)
                {

                    width = pListObjectInfo[iObject].Object.Width;
                    height = pListObjectInfo[iObject].Object.Height;
                    len = pListObjectInfo[iObject].PixelCount;
                    ////  정사각에 가까운 Object만 취급함.
                    if (bSquareOnly)
                    {
                        if (width > 3 * height || height > 3 * width)
                        {
                            pListObjectInfo.RemoveAt(iObject);
                            continue;
                        }
                    }
                    ////  시작지점이 정상적(양수)인 경우만 취급함.
                    if (bNormalOnly)
                    {
                        int diffLeft = pListObjectInfo[iObject].Object.Left;
                        int diffTop = pListObjectInfo[iObject].Object.Top;
                        if (diffLeft <= 1 || diffTop <= 1)
                        {
                            pListObjectInfo.RemoveAt(iObject);
                            continue;
                        }
                    }
                    if (len > maxLen)
                    {
                        maxLen = len;
                        maxLabel = pListObjectInfo[iObject].Label;
                        maxScore = pListObjectInfo[iObject].Score;
                        maxArea = pListObjectInfo[iObject].Object.Clone() as YoonRect2N;
                    }
                }
                return new YoonObject<YoonRect2N>(maxLabel, maxArea, maxScore, maxLen);
            }

            //  객체 찾기.
            public static ObjectList<YoonRect2N> FindObjects(YoonImage pSourceImage, YoonRect2N scanArea, byte nThreshold = 128, bool bWhite = false)
            {
                if (pSourceImage.Format != PixelFormat.Format8bppIndexed)
                    throw new FormatException("[YOONIMAGE EXCEPTION] Image format is not correct");
                return FindObjects(pSourceImage.GetGrayBuffer(), pSourceImage.Width, scanArea, nThreshold, bWhite);
            }

            public static ObjectList<YoonRect2N> FindObjects(byte[] pBuffer, int imageWidth, YoonRect2N scanArea, byte threshold, bool isWhite)
            {
                YoonVector2N startPos, resultPos;
                ObjectList<YoonRect2N> pListResult;
                int labelNo = 0;
                int width = scanArea.Width;
                int height = scanArea.Height;
                if (threshold < 10) threshold = 10;
                byte[] pTempBuffer = new byte[width * height];
                startPos = new YoonVector2N(0, 0);
                resultPos = new YoonVector2N();
                pListResult = new ObjectList<YoonRect2N>();
                //// 임시 Buffer 상에 원본 Buffer 복사.  (일부 복사)
                for (int j = 0; j < height; j++)
                {
                    int y = scanArea.Top + j;
                    for (int i = 0; i < width; i++)
                    {
                        int x = scanArea.Left + i;
                        pTempBuffer[j * width + i] = pBuffer[y * imageWidth + x];
                    }
                }
                ////  Temp Buffer의 테두리를 지운다.
                if (isWhite) pTempBuffer = Fill.FillBound(pTempBuffer, width, height, (byte)0);  // white object를 찾기 위함.
                else pTempBuffer = Fill.FillBound(pTempBuffer, width, height, (byte)255);    // black object를 찾기 위함.
                ////  Object를 전부 찾을 때까지 과정을 반복한다.
                while (true)
                {
                    //////  Object 시작지점을 가져온다.
                    resultPos = Scanner.Scan2D(pTempBuffer, width, height, eYoonDir2D.Right, startPos, threshold, isWhite) as YoonVector2N;
                    //////  object 찾기 결과 끝에 도달한 경우...
                    if (resultPos.X == -1 && resultPos.Y == -1)
                        break;
                    //////  1 Pixel 이상의 Object를 Bind 한다.
                    YoonObject<YoonRect2N> pObject = ProcessBind(pTempBuffer, width, height, eYoonDir2D.Right, resultPos, threshold, isWhite) as YoonObject<YoonRect2N>;
                    //////  DetectEdge에서 Error가 발생했을 경우.
                    if (pObject.Object.Left == 0 || pObject.Object.Top == 0 || pObject.Object.Right == 0 || pObject.Object.Bottom == 0)
                        break;
                    ////// 하나의 점으로 구성된 경우 저장할 Rect를 지운다.
                    if (pObject.Object.Left == -1 || pObject.Object.Top == -1 || pObject.Object.Right == -1 || pObject.Object.Bottom == -1)
                        continue;
                    ////// 찾은 영역을 List 상에 저장한다.
                    if (pListResult.Count < MAX_OBJECT)
                    {
                        pObject.Label = labelNo++;
                        pListResult.Add(pObject);
                    }
                    ////// Start Pos를 Rect 끝으로 재조정한다
                    startPos = pObject.Object.BottomRight.Clone() as YoonVector2N + new YoonVector2N(1, 1); // Buffer
                }
                return pListResult;
            }

            //  객체 찾기.
            public static ObjectList<YoonRect2N> FindObjects(YoonImage pSourceImage, byte nThreshold = 128, bool bWhite = false)
            {
                if (pSourceImage.Format != PixelFormat.Format8bppIndexed)
                    throw new FormatException("[YOONIMAGE EXCEPTION] Image format is not correct");
                return FindObjects(pSourceImage.GetGrayBuffer(), pSourceImage.Width, pSourceImage.Height, nThreshold, bWhite);
            }

            public static ObjectList<YoonRect2N> FindObjects(byte[] pBuffer, int width, int height, byte threshold, bool isWhite)
            {
                YoonVector2N startPos, resultPos;
                ObjectList<YoonRect2N> pListResult = new ObjectList<YoonRect2N>();
                int labelNo = 0;
                if (threshold < 10) threshold = 10;
                byte[] pTempBuffer = new byte[width * height];
                startPos = new YoonVector2N(0, 0);
                resultPos = new YoonVector2N();
                //// 임시 Buffer 상에 원본 Buffer 복사.   (전체 복사)
                pBuffer.CopyTo(pTempBuffer, 0);
                ////  Temp Buffer의 테두리를 지운다.
                if (isWhite) pTempBuffer = Fill.FillBound(pTempBuffer, width, height, (byte)0);  // white object를 찾기 위함.
                else pTempBuffer = Fill.FillBound(pTempBuffer, width, height, (byte)255);    // black object를 찾기 위함.
                ////  Object를 전부 찾을 때까지 과정을 반복한다.
                while (true)
                {
                    //////  Object 시작지점을 가져온다.
                    resultPos = Scanner.Scan2D(pTempBuffer, width, height, eYoonDir2D.Right, startPos, threshold, isWhite) as YoonVector2N;
                    //////  object 찾기 결과 끝에 도달한 경우...
                    if (resultPos.X == -1 && resultPos.Y == -1)
                        break;
                    //////  1 Pixel 이상의 Object를 Bind 하다.
                    YoonObject<YoonRect2N> pObject = ProcessBind(pTempBuffer, width, height, eYoonDir2D.Right, resultPos, threshold, isWhite) as YoonObject<YoonRect2N>;
                    //////  DetectEdge에서 Error가 발생했을 경우.
                    if (pObject.Object.Left == 0 || pObject.Object.Top == 0 || pObject.Object.Right == 0 || pObject.Object.Bottom == 0)
                        break;
                    ////// 하나의 점으로 구성된 경우 저장할 Rect를 지운다.
                    if (pObject.Object.Left == -1 || pObject.Object.Top == -1 || pObject.Object.Right == -1 || pObject.Object.Bottom == -1)
                        continue;
                    ////// 찾은 영역을 List 상에 저장한다.
                    if (pListResult.Count < MAX_OBJECT)
                    {
                        pObject.Label = labelNo++;
                        pListResult.Add(pObject);
                    }
                    ////// Start Pos를 Rect 끝으로 재조정한다
                    startPos = pObject.Object.BottomRight.Clone() as YoonVector2N + new YoonVector2N(1, 1); // Buffer
                }
                return pListResult;
            }

            private enum eYoonStepBinding
            {
                Init,
                Check,
                Go,
                Ignore,
                Stack,
                Rotate,
                Error,
                Finish,
            }

            private static IYoonObject ProcessBind(byte[] pBuffer, int nWidth, int nHeight, eYoonDir2D nDir, YoonVector2N vecStart, byte nThreshold, bool bWhite)
            {
                int pixelCount = 0;
                int blankCount = 0;
                bool bRun = true;
                YoonRect2N resultRect = new YoonRect2N(vecStart.X, vecStart.Y, 0, 0);
                eYoonDir2D dirSearch = nDir;
                eYoonDir2D dirDefault = nDir;
                eYoonDir2DMode nDirMode = eYoonDir2DMode.Clock4;
                eYoonDir2DMode nRotateMode = eYoonDir2DMode.AxisX;
                YoonVector2N vecCurrent = new YoonVector2N(vecStart);
                eYoonStepBinding jobStep = eYoonStepBinding.Init;
                eYoonStepBinding jobStepBk = jobStep;
                while (bRun)
                {
                    switch(jobStep)
                    {
                        case eYoonStepBinding.Init:
                            switch (nDir)
                            {
                                case eYoonDir2D.Top:
                                    nRotateMode = eYoonDir2DMode.AxisY;
                                    nDirMode = eYoonDir2DMode.Clock4;
                                    jobStep = eYoonStepBinding.Go;
                                    break;
                                case eYoonDir2D.Right:
                                    nRotateMode = eYoonDir2DMode.AxisX;
                                    nDirMode = eYoonDir2DMode.Clock4;
                                    jobStep = eYoonStepBinding.Go;
                                    break;
                                case eYoonDir2D.Bottom:
                                    nRotateMode = eYoonDir2DMode.AxisY;
                                    nDirMode = eYoonDir2DMode.Clock4;
                                    jobStep = eYoonStepBinding.Go;
                                    break;
                                case eYoonDir2D.Left:
                                    nRotateMode = eYoonDir2DMode.AxisX;
                                    nDirMode = eYoonDir2DMode.AntiClock4;
                                    jobStep = eYoonStepBinding.Go;
                                    break;
                                default:
                                    jobStep = eYoonStepBinding.Error;
                                    break;
                            }
                            break;
                        case eYoonStepBinding.Check:
                            byte value = pBuffer[vecCurrent.Y * nWidth + vecCurrent.X];
                            if (bWhite && value >= nThreshold)
                                jobStep = eYoonStepBinding.Stack;
                            else if (!bWhite && value <= nThreshold)
                                jobStep = eYoonStepBinding.Stack;
                            else
                            {
                                if (jobStepBk == eYoonStepBinding.Stack)
                                    jobStep = eYoonStepBinding.Rotate;
                                else if (jobStepBk == eYoonStepBinding.Ignore)
                                    jobStep = eYoonStepBinding.Ignore;
                                else
                                    jobStep = eYoonStepBinding.Error;
                            }
                            break;
                        case eYoonStepBinding.Go:
                            vecCurrent.Move(dirSearch);
                            jobStep = eYoonStepBinding.Check;
                            break;
                        case eYoonStepBinding.Ignore:
                            jobStepBk = jobStep;
                            if (blankCount++ >= resultRect.Width)
                                jobStep = eYoonStepBinding.Finish;
                            else
                                jobStep = eYoonStepBinding.Go;
                            break;
                        case eYoonStepBinding.Stack:
                            jobStepBk = jobStep;
                            blankCount = 0;
                            pixelCount++;
                            if (vecCurrent.X < resultRect.Left)
                                resultRect.CenterPos.X = vecCurrent.X + resultRect.Width / 2;
                            if (vecCurrent.X > resultRect.Right)
                                resultRect.Width = vecCurrent.X - resultRect.Left;
                            if (vecCurrent.Y < resultRect.Top)
                                resultRect.CenterPos.Y = vecCurrent.Y + resultRect.Height / 2;
                            if (vecCurrent.Y > resultRect.Bottom)
                                resultRect.Height = vecCurrent.Y - resultRect.Top;
                            if (vecCurrent.X == vecStart.X && vecCurrent.Y == vecStart.Y)
                                jobStep = eYoonStepBinding.Finish;
                            else
                                jobStep = eYoonStepBinding.Go;
                            break;
                        case eYoonStepBinding.Rotate:
                            dirSearch = dirSearch.Go(nDirMode);
                            if (dirSearch == dirDefault.Go(nRotateMode))
                            {
                                dirDefault = dirSearch;
                                jobStep = eYoonStepBinding.Ignore;
                            }
                            else
                                jobStep = eYoonStepBinding.Go;
                            break;
                        case eYoonStepBinding.Error:
                            resultRect = new YoonRect2N(-1, -1, 0, 0);
                            bRun = false;
                            break;
                        case eYoonStepBinding.Finish:
                            bRun = false;
                            break;
                    }
                }
                return new YoonObject<YoonRect2N>(0, resultRect, pixelCount);
            }
        }

        // Pixel 이진화
        public static class Binary
        {
            public static YoonImage Binarize(YoonImage pSourceImage, YoonRect2N scanArea, byte nThreshold)
            {
                if (pSourceImage.Format != PixelFormat.Format8bppIndexed)
                    throw new FormatException("[YOONIMAGE EXCEPTION] Image format is not correct");
                return new YoonImage(Binarize(pSourceImage.GetGrayBuffer(), pSourceImage.Width, scanArea, nThreshold),
                    pSourceImage.Width, pSourceImage.Height, PixelFormat.Format8bppIndexed);
            }

            public static byte[] Binarize(byte[] pBuffer, int bufferWidth, YoonRect2N scanArea, byte threshold)
            {
                byte[] pResultBuffer = new byte[pBuffer.Length];
                for (int j = scanArea.Top; j < scanArea.Bottom; j++)
                {
                    for (int i = scanArea.Left; i < scanArea.Right; i++)
                    {
                        if (pBuffer[j * bufferWidth + i] < threshold)
                            pResultBuffer[j * bufferWidth + i] = 0;
                        else
                            pResultBuffer[j * bufferWidth + i] = 255;
                    }
                }
                return pResultBuffer;
            }

            public static YoonImage Binarize(YoonImage pSourceImage, byte nThreshold)
            {
                if (pSourceImage.Format != PixelFormat.Format8bppIndexed)
                    throw new FormatException("[YOONIMAGE EXCEPTION] Image format is not correct");
                return new YoonImage(Binarize(pSourceImage.GetGrayBuffer(), pSourceImage.Width, pSourceImage.Height, nThreshold),
                    pSourceImage.Width, pSourceImage.Height, PixelFormat.Format8bppIndexed);
            }

            public static byte[] Binarize(byte[] pBuffer, int bufferWidth, int bufferHeight, byte threshold)
            {
                int sum = 0;
                int count = 0;
                byte tempThreshold = threshold;
                byte[] pResultBuffer = new byte[bufferWidth * bufferHeight];
                ////  threshold 값 보정.
                if (threshold < 1)
                {
                    for (int j = 0; j < bufferHeight; j++)
                    {
                        for (int i = 0; i < bufferWidth; i++)
                        {
                            sum += pBuffer[j * bufferWidth + i];
                            count++;
                        }
                    }
                    if (count < 1)
                        tempThreshold = 255;
                    else
                        tempThreshold = (byte)(sum / count + 20);
                }
                ////  이진화.
                for (int j = 0; j < bufferHeight; j++)
                {
                    for (int i = 0; i < bufferWidth; i++)
                    {
                        if (pBuffer[j * bufferWidth + i] < tempThreshold)
                            pResultBuffer[j * bufferWidth + i] = 0;
                        else
                            pResultBuffer[j * bufferWidth + i] = 255;
                    }
                }
                return pResultBuffer;
            }
        }

        // 영상 침식/ 팽창
        public static class Morphology
        {
            //  침식 연산.
            public static byte[] Erosion(byte[] pBuffer, int bufferWidth, int bufferHeight)
            {
                int i, j, x, y;
                int posX, posY;
                int value, minValue;
                byte[] pResultBuffer = new byte[bufferWidth * bufferHeight];
                Array.Copy(pBuffer, pResultBuffer, pBuffer.Length);
                ////  침식 연산용 Masking.
                for (y = 0; y < bufferHeight - 2; y++)
                {
                    for (x = 0; x < bufferWidth - 2; x++)
                    {
                        minValue = 100000;
                        //////  주변의 아홉개 IYoonVector 中 최소 Gray Level 산출.
                        for (j = 0; j < 3; j++)
                        {
                            for (i = 0; i < 3; i++)
                            {
                                posX = x + i;
                                posY = y + j;
                                value = pBuffer[posY * bufferWidth + posX];
                                if (value < minValue)
                                    minValue = value;
                            }
                        }
                        //////  다음 IYoonVector에 해당 Gray Level 대입.
                        posX = x + 1;
                        posY = y + 1;
                        pResultBuffer[posY * bufferWidth + posX] = (byte)minValue;
                    }
                }
                return pResultBuffer;
            }

            public static byte[] Erosion(byte[] pBuffer, int bufferWidth, YoonRect2N scanArea)
            {
                int i, j, x, y;
                int posX, posY;
                int value, minValue;
                int scanWidth = scanArea.Width;
                int scanHeight = scanArea.Height;
                byte[] pResultBuffer = new byte[pBuffer.Length];
                Array.Copy(pBuffer, pResultBuffer, pBuffer.Length);
                byte[] pTempBuffer = new byte[scanWidth * scanHeight];
                for (j = 0; j < scanHeight; j++)
                    for (i = 0; i < scanWidth; i++)
                        pTempBuffer[j * scanWidth + i] = pBuffer[(scanArea.Top + j) * bufferWidth + scanArea.Left + i];
                ////  침식 연산용 Masking.
                for (y = 0; y < scanHeight - 2; y++)
                {
                    for (x = 0; x < scanWidth - 2; x++)
                    {
                        minValue = 100000;
                        //////  주변의 아홉개 IYoonVector 中 최소 Gray Level 산출.
                        for (j = 0; j < 3; j++)
                        {
                            for (i = 0; i < 3; i++)
                            {
                                posX = x + i;
                                posY = y + j;
                                value = pTempBuffer[posY * bufferWidth + posX];
                                if (value < minValue)
                                    minValue = value;
                            }
                        }
                        //////  다음 IYoonVector에 해당 Gray Level 대입.
                        posX = scanArea.Left + x + 1;
                        posY = scanArea.Top + y + 1;
                        pResultBuffer[posY * bufferWidth + posX] = (byte)minValue;
                    }
                }
                return pResultBuffer;
            }

            public static byte[] ErosionAsBinary(byte[] pBuffer, int bufferWidth, int bufferHeight)
            {
                int i, j, x, y;
                int posX, posY;
                int sum;
                byte[] pResultBuffer = new byte[bufferWidth * bufferHeight];
                byte[,] mask = new byte[3, 3] { { 255, 255, 255 }, { 255, 255, 255 }, { 255, 255, 255 } };
                Array.Copy(pBuffer, pResultBuffer, pBuffer.Length);
                for (y = 0; y < bufferHeight - 2; y++)
                {
                    for (x = 0; x < bufferWidth - 2; x++)
                    {
                        sum = 0;
                        //////  주변이 모두 흰색일 경우에만 흰색(255)으로 표시할 수 있음.
                        for (i=0; i<3; i++)
                        {
                            for(j=0; j<3; j++)
                            {
                                posX = x + i;
                                posY = y + j;
                                if (pBuffer[posY * bufferWidth + posX] == mask[i, j])
                                    sum++;
                            }
                        }
                        //////  다음 IYoonVector에 침식 Gray Level 결과 대입.
                        posX = x + 1;
                        posY = y + 1;
                        if (sum == 9)
                        {
                            pResultBuffer[posY * bufferWidth + posX] = 255;
                        }
                        else
                        {
                            pResultBuffer[posY * bufferWidth + posX] = 0;
                        }
                    }
                }
                return pResultBuffer;
            }

            public static byte[] ErosionAsBinary(byte[] pBuffer, int bufferWidth, YoonRect2N scanArea)
            {
                int x, y, i, j;
                int posX, posY;
                int sum;
                int scanWidth = scanArea.Width;
                int scanHeight = scanArea.Height;
                byte[] pResultBuffer = new byte[pBuffer.Length];
                Array.Copy(pBuffer, pResultBuffer, pBuffer.Length);
                byte[] pTempBuffer = new byte[scanWidth * scanHeight];
                byte[,] mask = new byte[3, 3] { { 255, 255, 255 }, { 255, 255, 255 }, { 255, 255, 255 } };
                for (j = 0; j < scanHeight; j++)
                    for (i = 0; i < scanWidth; i++)
                        pTempBuffer[j * scanWidth + i] = pBuffer[(scanArea.Top + j) * bufferWidth + scanArea.Left + i];
                ////  침식 연산 Masking 작업.
                for (y = 0; y < scanHeight - 2; y++)
                {
                    for (x = 0; x < scanWidth - 2; x++)
                    {
                        sum = 0;
                        //////  주변이 모두 흰색일 경우에만 흰색(255)으로 표시할 수 있음.
                        for (i = 0; i < 3; i++)
                        {
                            for (j = 0; j < 3; j++)
                            {
                                posX = x + i;
                                posY = y + j;
                                if (pTempBuffer[posY * scanWidth + posX] == mask[i, j])
                                    sum++;
                            }
                        }
                        //////  다음 IYoonVector에 침식 Gray Level 결과 대입.
                        posX = scanArea.Left + x + 1;
                        posY = scanArea.Top + y + 1;
                        if (sum == 9)
                        {
                            pResultBuffer[posY * bufferWidth + posX] = 255;
                        }
                        else
                        {
                            pResultBuffer[posY * bufferWidth + posX] = 0;
                        }
                    }
                }
                return pResultBuffer;
            }

            //  size 조정 가능한 침식 연산.
            public static byte[] ErosionAsBinary(byte[] pBuffer, int bufferWidth, int bufferHeight, int size)
            {
                bool isBlack;
                int i, j, x, y;
                int posX, posY;
                byte[] pResultBuffer = new byte[bufferWidth * bufferHeight];
                Array.Copy(pBuffer, pResultBuffer, pBuffer.Length);
                //// 침식 연산 Masking 작업.
                for (y = 0; y < bufferHeight - size; y++)
                {
                    for (x = 0; x < bufferWidth - size; x++)
                    {
                        isBlack = false;
                        //////  가상 Masking과 비교.
                        for (i = 0; i < size; i++)
                        {
                            for (j = 0; j < size; j++)
                            {
                                //////  주변의 Pixel들中 하나라도 검은색이면 검은색(0)임.
                                posX = x + i;
                                posY = y + j;
                                if (pBuffer[(y + j) * bufferWidth + (x + i)] == 0)
                                {
                                    isBlack = true;
                                    break;
                                }
                            }
                        }
                        //////  다음 IYoonVector에 침식 판단 결과 대입.
                        posX = x + size / 2;
                        posY = y + size / 2;
                        if (isBlack)
                        {
                            pResultBuffer[posY * bufferWidth + posX] = 0;
                        }
                        else
                        {
                            pResultBuffer[posY * bufferWidth + posX] = 255;
                        }
                    }
                }
                return pResultBuffer;
            }

            //  팽장 연산.
            public static void Dilation(ref byte[] pBuffer, int bufferWidth, int bufferHeight)
            {
                int i, j, x, y;
                int posX, posY, value, maxValue;
                int[,] mask = new int[3, 3] { { 0, 0, 0 }, { 0, 0, 0 }, { 0, 0, 0 } };
                byte[] pTempBuffer;
                pTempBuffer = new byte[bufferWidth * bufferHeight];
                pBuffer.CopyTo(pTempBuffer, 0);
                ////  팽창 연산 Masking 작업.
                for (y = 0; y < bufferHeight - 2; y++)
                {
                    for (x = 0; x < bufferWidth - 2; x++)
                    {
                        maxValue = 0;
                        //////  주변의 아홉개 IYoonVector 中 최대 Gray Level 산출.
                        for (j = 0; j < 3; j++)
                        {
                            for (i = 0; i < 3; i++)
                            {
                                posX = x + i;
                                posY = y + j;
                                value = mask[i, j] + pTempBuffer[posY * bufferWidth + posX];
                                if (value > maxValue)
                                    maxValue = value;
                            }
                        }
                        //////  다음 IYoonVector에 해당 Gray Level 대입.
                        posX = x + 1;
                        posY = y + 1;
                        pBuffer[posY * bufferWidth + posX] = (byte)maxValue;
                    }
                }
            }

            public static void DilationBinary(ref byte[] pBuffer, int bufferWidth, YoonRect2N scanArea)
            {
                int i, j;
                int x, y, x1, y1;
                bool isWhite;
                int scanWidth, scanHeight;
                byte[] pTempBuffer;
                scanWidth = scanArea.Width;
                scanHeight = scanArea.Height;
                pTempBuffer = new byte[scanWidth * scanHeight];
                //	memcpy(pTempBuffer, pBuffer, sizeof(byte)*bufferWidth*bufferHeight);
                for (j = 0; j < scanHeight; j++)
                    for (i = 0; i < scanWidth; i++)
                        pTempBuffer[j * scanWidth + i] = pBuffer[(scanArea.Top + j) * bufferWidth + (scanArea.Left + i)];
                ////  팽창 연산 Masking 작업.
                for (y = 0; y < scanHeight - 2; y++)
                {
                    for (x = 0; x < scanWidth - 2; x++)
                    {
                        isWhite = false;
                        for (i = 0; i < 3; i++)
                        {
                            for (j = 0; j < 3; j++)
                            {
                                if (pTempBuffer[(y + j) * scanWidth + (x + i)] > 0)
                                {
                                    isWhite = true;
                                    break;
                                }
                            }
                            if (isWhite) break;
                        }
                        x1 = scanArea.Left + (x + 1);
                        y1 = scanArea.Top + (y + 1);
                        //			if(sum==9)
                        //			{
                        //				pBuffer[y1*bufferWidth + x1] = 0;
                        //			}
                        //			else
                        //			{
                        //				pBuffer[y1*bufferWidth + x1] = 255;
                        //			}
                        if (isWhite) pBuffer[y1 * bufferWidth + x1] = 255;
                        else pBuffer[y1 * bufferWidth + x1] = 0;
                    }
                }
            }

            //  size 조정 가능한 팽창 연산.
            public static void DilationBinary(ref byte[] pBuffer, int bufferWidth, int bufferHeight, int size, YoonRect2N scanArea)
            {
                int i, j, x, y, x1, y1;
                bool isWhite;
                byte[] pTempBuffer;
                pTempBuffer = new byte[bufferWidth * bufferHeight];
                pBuffer.CopyTo(pTempBuffer, 0);
                ////  Buffer 영역 팽창 연산 Masking 작업.
                for (y = 0; y < bufferHeight - size; y++)
                {
                    for (x = 0; x < bufferWidth - size; x++)
                    {
                        isWhite = false;
                        for (i = 0; i < size; i++)
                        {
                            for (j = 0; j < size; j++)
                            {
                                if (pTempBuffer[(y + j) * bufferWidth + (x + i)] > 0)
                                {
                                    isWhite = true;
                                    break;
                                }
                            }
                            if (isWhite) break;
                        }
                        x1 = x + size / 2;
                        y1 = y + size / 2;
                        if (isWhite) pBuffer[y1 * bufferWidth + x1] = 255;
                        else pBuffer[y1 * bufferWidth + x1] = 0;
                    }
                }
            }

            //  Filter 테두리 형태로 팽창 검사.
            public static void DilationBlockBinary(ref byte[] pBuffer, int bufferWidth, int bufferHeight, int size, YoonRect2N scanArea)
            {
                int i, j, i1, j1, i2, j2;
                int x, y, x1, y1;
                bool isWhite;
                byte[] pTempBuffer;
                pTempBuffer = new byte[bufferWidth * bufferHeight];
                pBuffer.CopyTo(pTempBuffer, 0);
                ////  테두리 부분(0, size-1)만 따로 검사.
                for (y = scanArea.Top; y < scanArea.Bottom - size; y++)
                {
                    for (x = scanArea.Left; x < scanArea.Right - size; x++)
                    {
                        isWhite = false;
                        j1 = 0;
                        j2 = size - 1;
                        for (i = 0; i < size; i++)
                        {
                            if (pTempBuffer[(y + j1) * bufferWidth + (x + i)] > 0 || pTempBuffer[(y + j2) * bufferWidth + (x + i)] > 0)
                            {
                                isWhite = true;
                                break;
                            }
                        }
                        i1 = 0;
                        i2 = size - 1;
                        for (j = 0; j < size; j++)
                        {
                            if (isWhite) break;
                            if (pTempBuffer[(y + j) * bufferWidth + (x + i1)] > 0 || pTempBuffer[(y + j) * bufferWidth + (x + i2)] > 0)
                            {
                                isWhite = true;
                                break;
                            }
                        }
                        x1 = x + size / 2;
                        y1 = y + size / 2;
                        if (isWhite) pBuffer[y1 * bufferWidth + x1] = 255;
                        else pBuffer[y1 * bufferWidth + x1] = 0;
                    }
                }
            }
        }

        // 검사 객체 정렬
        public static class Sort // -> Each Module (YoonObject, YoonRect ...)
        {

            #region 각종 객체 정렬하기
            //  Object Info 구조체 안의 Pick Area(Rect)들을 크기에 맞게 정렬(Sorting).
            public static void SortObject(ref List<YoonRectObject> pList, eYoonDir2D direction)
            {
                int minCount, diffValue, height, count;
                YoonRectObject pObjectMin, pObjectCurr, pObjectTemp;
                count = pList.Count;
                minCount = 0;
                diffValue = 0;
                height = 0;
                for (int i = 0; i < count - 1; i++)
                {
                    minCount = i;
                    for (int j = i + 1; j < count; j++)
                    {
                        pObjectMin = (YoonRectObject)pList[minCount];
                        pObjectCurr = (YoonRectObject)pList[j];
                        ////  정렬 방향마다 최소 Count를 다르게 가져간다.
                        switch (direction)
                        {
                            case eYoonDir2D.TopLeft:
                                //////  높이차가 있는 경우 Top 우선, 없는 경우 왼쪽 우선.
                                diffValue = (int)Math.Abs((float)(pObjectMin.PickArea as YoonRect2N).Top - (float)(pObjectCurr.PickArea as YoonRect2N).Top);
                                height = (pObjectMin.PickArea as YoonRect2N).Bottom - (pObjectMin.PickArea as YoonRect2N).Top;
                                if (diffValue <= height / 2)
                                {
                                    if ((pObjectCurr.PickArea as YoonRect2N).Top < (pObjectMin.PickArea as YoonRect2N).Top)
                                        minCount = j;
                                }
                                else
                                {
                                    if ((pObjectCurr.PickArea as YoonRect2N).Left < (pObjectMin.PickArea as YoonRect2N).Left)
                                        minCount = j;
                                }
                                break;
                            case eYoonDir2D.TopRight:
                                diffValue = (int)Math.Abs((float)(pObjectMin.PickArea as YoonRect2N).Top - (float)(pObjectCurr.PickArea as YoonRect2N).Top);
                                height = (pObjectMin.PickArea as YoonRect2N).Bottom - (pObjectMin.PickArea as YoonRect2N).Top;
                                //////  높이차가 있는 경우 Top 우선, 없는 경우 오른쪽 우선.
                                if (diffValue <= height / 2)
                                {
                                    if ((pObjectCurr.PickArea as YoonRect2N).Right > (pObjectMin.PickArea as YoonRect2N).Right)
                                        minCount = j;
                                }
                                else
                                {
                                    if ((pObjectCurr.PickArea as YoonRect2N).Top < (pObjectMin.PickArea as YoonRect2N).Top)
                                        minCount = j;
                                }
                                break;
                            case eYoonDir2D.Left:
                                if ((pObjectCurr.PickArea as YoonRect2N).Left < (pObjectMin.PickArea as YoonRect2N).Left)
                                    minCount = j;
                                break;
                            case eYoonDir2D.Right:
                                if ((pObjectCurr.PickArea as YoonRect2N).Right > (pObjectMin.PickArea as YoonRect2N).Right)
                                    minCount = j;
                                break;
                            default:  // 좌상측 정렬과 같음.
                                      //////  높이차가 있는 경우 Top 우선, 없는 경우 왼쪽 우선.
                                diffValue = (int)Math.Abs((float)(pObjectMin.PickArea as YoonRect2N).Top - (float)(pObjectCurr.PickArea as YoonRect2N).Top);
                                height = (pObjectMin.PickArea as YoonRect2N).Bottom - (pObjectMin.PickArea as YoonRect2N).Top;
                                if (diffValue >= height / 2)
                                    continue;
                                if ((pObjectCurr.PickArea as YoonRect2N).Left < (pObjectMin.PickArea as YoonRect2N).Left)
                                    minCount = j;
                                break;
                        }
                    }
                    ////  애초에 최소 위치인 경우, 다음 순서로 Pass 한다.
                    if (minCount == i) continue;
                    pObjectTemp = new YoonRectObject();
                    pObjectMin = (YoonRectObject)pList[minCount];
                    pObjectCurr = (YoonRectObject)pList[i];
                    ////  (순서상 맨 앞이었던)현재 Object를 백업(tempObject)하고 최소값의 Object를 삽입한다.
                    ////  최소값의 Object가 있었던 주소(*)에 현재 Object를 삽입한다.
                    pObjectTemp.CopyFrom(pObjectCurr);
                    pList[i].CopyFrom(pObjectMin);
                    pList[minCount].CopyFrom(pObjectTemp);
                    //      memcpy(&tempObject, pObjectCurr, sizeof(OBJECT_INFO));
                    //      memcpy(pObjectCurr, pObjectMin,  sizeof(OBJECT_INFO));
                    //      memcpy(pObjectMin,  &tempObject, sizeof(OBJECT_INFO));
                }
            }

            //  일반 Rect 인자들을 정렬(Sorting).
            public static void SortRect(ref List<IYoonRect> pList, eYoonDir2D direction)
            {
                int minCount, diffValue, height, count;
                YoonRect2N pRectMin, pRectCurr, pRectTemp;
                count = pList.Count;
                minCount = 0;
                diffValue = 0;
                height = 0;
                for (int i = 0; i < count - 1; i++)
                {
                    minCount = i;
                    for (int j = i + 1; j < count; j++)
                    {
                        pRectMin = pList[minCount] as YoonRect2N;
                        pRectCurr = pList[j] as YoonRect2N;
                        switch (direction)
                        {
                            case eYoonDir2D.TopLeft:
                                diffValue = (int)Math.Abs((float)pRectMin.Top - (float)pRectCurr.Top);
                                height = pRectMin.Bottom - pRectMin.Top;
                                //////  높이차가 있는 경우 Top 우선, 없는 경우 왼쪽 우선.
                                if (diffValue <= height / 2)
                                {
                                    if (pRectCurr.Left < pRectMin.Left)
                                        minCount = j;
                                }
                                else
                                {
                                    if (pRectCurr.Top < pRectMin.Top)
                                        minCount = j;
                                }
                                break;
                            case eYoonDir2D.TopRight:
                                diffValue = (int)Math.Abs((float)pRectMin.Top - (float)pRectCurr.Top);
                                height = pRectMin.Bottom - pRectMin.Top;
                                //////  높이차가 있는 경우 Top 우선, 없는 경우 오른쪽 우선.
                                if (diffValue <= height / 2)
                                {
                                    if (pRectCurr.Right > pRectMin.Right)
                                        minCount = j;
                                }
                                else
                                {
                                    if (pRectCurr.Top < pRectMin.Top)
                                        minCount = j;
                                }
                                break;
                            case eYoonDir2D.Left:
                                if (pRectCurr.Left < pRectMin.Left)
                                    minCount = j;
                                break;
                            case eYoonDir2D.Right:
                                if (pRectCurr.Right > pRectMin.Right)
                                    minCount = j;
                                break;
                            default:  // 좌상측 정렬과 같음.
                                diffValue = (int)Math.Abs((float)pRectMin.Top - (float)pRectCurr.Top);
                                height = pRectMin.Bottom - pRectMin.Top;
                                //////  높이차가 있는 경우 Top 우선, 없는 경우 왼쪽 우선.
                                if (diffValue <= height / 2)
                                    continue;
                                if (pRectCurr.Left < pRectMin.Left)
                                    minCount = j;
                                break;
                        }
                    }
                    pRectMin = pList[minCount] as YoonRect2N;
                    pRectCurr = pList[i] as YoonRect2N;
                    ////  (순서상 맨 앞이었던)현재 Rect를 백업(tempRect)하고 최소값의 Rect를 삽입한다.
                    ////  최소값의 Rect가 있었던 주소(*)에 현재 Rect를 삽입한다.
                    pRectTemp = new YoonRect2N(pRectCurr.CenterPos.X, pRectCurr.CenterPos.Y, pRectCurr.Width, pRectCurr.Height);
                    pList[i] = new YoonRect2N(pRectMin.CenterPos.X, pRectMin.CenterPos.Y, pRectMin.Width, pRectMin.Height);
                    pList[minCount] = pRectTemp;
                    //      memcpy(&tempRect, pRectCurr, sizeof(IYoonRect));
                    //      memcpy(pRectCurr, pRectMin,  sizeof(IYoonRect));
                    //      memcpy(pRectMin,  &tempRect, sizeof(IYoonRect));
                }
            }

            //  정수 정렬.
            public static void SortInteger(ref List<int> pList, eYoonDir2DMode direction)
            {
                int minCount, maxCount;
                int pMinValue, pMaxValue, pCurrValue, tempValue;
                int count;
                count = pList.Count;
                minCount = 0;
                maxCount = 0;
                ////  오름차순(작은수.큰수) 時  정렬
                if (direction == eYoonDir2DMode.Increase)
                {
                    for (int i = 0; i < count - 1; i++)
                    {
                        minCount = i;
                        for (int j = i + 1; j < count; j++)
                        {
                            pMinValue = (int)pList[minCount];
                            pCurrValue = (int)pList[j];
                            if (pCurrValue < pMinValue)
                                minCount = j;
                        }
                        pMinValue = (int)pList[minCount];
                        pCurrValue = (int)pList[i];
                        tempValue = pCurrValue;
                        pList[i] = pMinValue;
                        pList[minCount] = tempValue;
                    }
                }
                ////  내림차순(큰수.작은수) 時 정렬
                else if (direction == eYoonDir2DMode.Decrease)
                {
                    for (int i = 0; i < count - 1; i++)
                    {
                        maxCount = i;
                        for (int j = i + 1; j < count; j++)
                        {
                            pMaxValue = (int)pList[maxCount];
                            pCurrValue = (int)pList[j];
                            if (pCurrValue > pMaxValue)
                                maxCount = j;
                        }
                        pMaxValue = (int)pList[maxCount];
                        pCurrValue = (int)pList[i];
                        tempValue = pCurrValue;
                        pList[i] = pMaxValue;
                        pList[maxCount] = tempValue;
                    }
                }
            }

            //  사각형(또는 영역)들을 정렬시키는 함수.
            public static void SortRect(ref List<IYoonRect> pList)
            {
                int i, j;
                bool isCombine;
                int totalCount;
                YoonRect2N pRect1, pRect2;
                List<IYoonRect> pListTemp;
                YoonRect2N combineRect;
                isCombine = false;
                pRect1 = new YoonRect2N();
                pListTemp = new List<IYoonRect>();
                totalCount = pList.Count;
                if (totalCount <= 1) return;
                ////  원본(rect1)을 복사(rect2)해서 List에 넣은 後 삭제한다.
                pListTemp = pList.GetRange(0, pList.Count);
                pList.Clear();
                ////  모든 사각형들을 전수 조사해가며 서로 겹치는 사각형이 있는지 찾는다.
                for (i = 0; i < totalCount; i++)
                {
                    pRect1 = pListTemp[i] as YoonRect2N;
                    combineRect = new YoonRect2N(0, 0, 0, 0);
                    if (pRect1.Width == 0)
                        continue;
                    isCombine = false;
                    for (j = 0; j < totalCount; j++)
                    {
                        if (i == j) continue;
                        pRect2 = pListTemp[j] as YoonRect2N;
                        if (pRect2.Width == 0)
                            continue;
                        //////  Rect1와 Rect2가 겹치거나 속해지는 경우...
                        if ((pRect1.Left > pRect2.Left) && (pRect1.Left < pRect2.Right))
                        {
                            if ((pRect1.Top >= pRect2.Top) && (pRect1.Top <= pRect2.Bottom))
                                isCombine = true;
                            if ((pRect1.Bottom >= pRect2.Top) && (pRect1.Bottom <= pRect2.Bottom))
                                isCombine = true;
                            if ((pRect1.Top <= pRect2.Top) && (pRect1.Bottom >= pRect2.Bottom))
                                isCombine = true;
                        }
                        if (pRect1.Right > pRect2.Left && pRect1.Right < pRect2.Right)
                        {
                            if ((pRect1.Top >= pRect2.Top) && (pRect1.Top <= pRect2.Bottom))
                                isCombine = true;
                            if ((pRect1.Bottom >= pRect2.Top) && (pRect1.Bottom <= pRect2.Bottom))
                                isCombine = true;
                            if ((pRect1.Top <= pRect2.Top) && (pRect1.Bottom >= pRect2.Bottom))
                                isCombine = true;
                        }
                        if ((pRect1.Left <= pRect2.Left) && (pRect1.Right >= pRect2.Right))
                        {
                            if ((pRect1.Top >= pRect2.Top) && (pRect1.Top <= pRect2.Bottom))
                                isCombine = true;
                            if ((pRect1.Bottom >= pRect2.Top) && (pRect1.Bottom <= pRect2.Bottom))
                                isCombine = true;
                            if ((pRect1.Top <= pRect2.Top) && (pRect1.Bottom >= pRect2.Bottom))
                                isCombine = true;
                        }
                        //////  Rect들이 겹쳐지는 경우, 결합 Rect는 둘을 모두 포함한다.
                        if (isCombine)
                        {
                            combineRect.CenterPos.X = (pRect1.Left < pRect2.Left) ? pRect1.CenterPos.X : pRect2.CenterPos.X;
                            combineRect.Width = (pRect1.Right > pRect2.Right) ? pRect1.Right - combineRect.Left : pRect2.Right - combineRect.Left;
                            combineRect.CenterPos.Y = (pRect1.Top < pRect2.Top) ? pRect1.CenterPos.Y : pRect2.CenterPos.Y;
                            combineRect.Height = (pRect1.Bottom > pRect2.Bottom) ? pRect1.Bottom - combineRect.Top : pRect2.Bottom - combineRect.Top;
                            pListTemp[i] = new YoonRect2N(0, 0, 0, 0);
                            pListTemp[j] = combineRect;
                            break;
                        }
                    }
                }
                ////  정렬된 사각형들 中 유효한 사각형들만 재정렬시킨다.
                for (i = 0; i < totalCount; i++)
                {
                    pRect1 = pListTemp[i] as YoonRect2N;
                    if (pRect1.Right != 0)
                    {
                        pRect2 = new YoonRect2N(pRect1.CenterPos.X, pRect1.CenterPos.Y, pRect1.Width, pRect1.Height);
                        pList.Add(pRect2);
                    }
                }
                pListTemp.Clear();
            }
            #endregion
        }

        // Pixel Scan 및 추출
        public static class Scanner
        {
            //  왼쪽 방향으로 Scan하며 threshold보다 크거나 작은 Gray Level 값 가져오기.
            public static IYoonVector ScanLeft(int[] pBuffer, int width, int height, YoonVector2N startPos, int threshold, bool isWhite)
            {
                int value;
                YoonVector2N resultPos = new YoonVector2N(startPos);
                value = pBuffer[resultPos.Y * width + resultPos.X];
                if (isWhite)
                {
                    while (value > threshold && resultPos.X > 0)
                    {
                        resultPos.X--;
                        value = pBuffer[resultPos.Y * width + resultPos.X];
                    }
                }
                else
                {
                    while (value <= threshold && resultPos.X > 0)
                    {
                        resultPos.X--;
                        value = pBuffer[resultPos.Y * width + resultPos.X];
                    }
                }
                return resultPos;
            }

            public static IYoonVector ScanLeft(byte[] pBuffer, int width, int height, YoonVector2N startPos, byte threshold, bool isWhite)
            {
                byte value;
                YoonVector2N resultPos = new YoonVector2N(startPos);
                value = pBuffer[resultPos.Y * width + resultPos.X];
                if (isWhite)
                {
                    while (value > threshold && resultPos.X > 0)
                    {
                        resultPos.X--;
                        value = pBuffer[resultPos.Y * width + resultPos.X];
                    }
                }
                else
                {
                    while (value <= threshold && resultPos.X > 0)
                    {
                        resultPos.X--;
                        value = pBuffer[resultPos.Y * width + resultPos.X];
                    }
                }
                return resultPos;
            }

            //  오른쪽 방향으로 Scan하며 threshold보다 크거나 작은 Gray Level 값 가져오기.
            public static IYoonVector ScanRight(int[] pBuffer, int width, int height, YoonVector2N startPos, int threshold, bool isWhite)
            {
                int value;
                YoonVector2N resultPos = new YoonVector2N(startPos);
                value = pBuffer[resultPos.Y * width + resultPos.X];
                if (isWhite)
                {
                    while (value > threshold && resultPos.X < width)
                    {
                        resultPos.X++;
                        value = pBuffer[resultPos.Y * width + resultPos.X];
                    }
                }
                else
                {
                    while (value <= threshold && resultPos.X < width)
                    {
                        resultPos.X++;
                        value = pBuffer[resultPos.Y * width + resultPos.X];
                    }
                }
                return resultPos;
            }

            public static IYoonVector ScanRight(byte[] pBuffer, int width, int height, YoonVector2N startPos, byte threshold, bool isWhite)
            {
                byte value;
                YoonVector2N resultPos = new YoonVector2N(startPos);
                value = pBuffer[resultPos.Y * width + resultPos.X];
                if (isWhite)
                {
                    while (value > threshold && resultPos.X < width)
                    {
                        resultPos.X++;
                        value = pBuffer[resultPos.Y * width + resultPos.X];
                    }
                }
                else
                {
                    while (value <= threshold && resultPos.X < width)
                    {
                        resultPos.X++;
                        value = pBuffer[resultPos.Y * width + resultPos.X];
                    }
                }
                return resultPos;
            }

            //  위쪽 방향으로 Scan하며 threshold보다 크거나 작은 Gray Level 값 가져오기.
            public static IYoonVector ScanTop(int[] pBuffer, int width, int height, YoonVector2N startPos, int threshold, bool isWhite)
            {
                int value;
                YoonVector2N resultPos = new YoonVector2N(startPos);
                value = pBuffer[resultPos.Y * width + resultPos.X];
                if (isWhite)
                {
                    while (value > threshold && resultPos.Y > 0)
                    {
                        resultPos.Y--;
                        value = pBuffer[resultPos.Y * width + resultPos.X];
                    }
                }
                else
                {
                    while (value <= threshold && resultPos.Y > 0)
                    {
                        resultPos.Y--;
                        value = pBuffer[resultPos.Y * width + resultPos.X];
                    }
                }
                return resultPos;
            }

            public static IYoonVector ScanTop(byte[] pBuffer, int width, int height, YoonVector2N startPos, byte threshold, bool isWhite)
            {
                byte value;
                YoonVector2N resultPos = new YoonVector2N(startPos);
                value = pBuffer[resultPos.Y * width + resultPos.X];
                if (isWhite)
                {
                    while (value > threshold && resultPos.Y > 0)
                    {
                        resultPos.Y--;
                        value = pBuffer[resultPos.Y * width + resultPos.X];
                    }
                }
                else
                {
                    while (value <= threshold && resultPos.Y > 0)
                    {
                        resultPos.Y--;
                        value = pBuffer[resultPos.Y * width + resultPos.X];
                    }
                }
                return resultPos;
            }

            //  위쪽 방향으로 Scan하며 threshold보다 크거나 작은 Gray Level 값 가져오기.
            public static IYoonVector ScanBottom(int[] pBuffer, int width, int height, YoonVector2N startPos, int threshold, bool isWhite)
            {
                int value;
                YoonVector2N resultPos = new YoonVector2N(startPos);
                value = pBuffer[resultPos.Y * width + resultPos.X];
                if (isWhite)
                {
                    while (value > threshold && resultPos.Y < height)
                    {
                        resultPos.Y++;
                        value = pBuffer[resultPos.Y * width + resultPos.X];
                    }
                }
                else
                {
                    while (value <= threshold && resultPos.Y < height)
                    {
                        resultPos.Y++;
                        value = pBuffer[resultPos.Y * width + resultPos.X];
                    }
                }
                return resultPos;
            }

            public static IYoonVector ScanBottom(byte[] pBuffer, int width, int height, YoonVector2N startPos, byte threshold, bool isWhite)
            {
                byte value;
                YoonVector2N resultPos = new YoonVector2N(startPos);
                value = pBuffer[resultPos.Y * width + resultPos.X];
                if (isWhite)
                {
                    while (value > threshold && resultPos.Y < height)
                    {
                        resultPos.Y++;
                        value = pBuffer[resultPos.Y * width + resultPos.X];
                    }
                }
                else
                {
                    while (value <= threshold && resultPos.Y < height)
                    {
                        resultPos.Y++;
                        value = pBuffer[resultPos.Y * width + resultPos.X];
                    }
                }
                return resultPos;
            }

            //  Object, Pattern 등의 시작위치 찾기
            public static IYoonVector Scan2D(byte[] pBuffer, int width, int height, eYoonDir2D nDir, YoonVector2N startPos, byte threshold, bool isWhite)
            {
                if (startPos.X >= width && startPos.Y >= height)
                    return new YoonVector2N(-1, -1);
                if (startPos.X < 0 && startPos.Y < 0)
                    return new YoonVector2N(-1, -1);
                if (startPos.X >= width || startPos.X < 0)
                {
                    startPos.Move(eYoonDir2D.Bottom);
                    return Scan2D(pBuffer, width, height, nDir.ReverseX(), startPos, threshold, isWhite);
                }
                if (startPos.Y >= height || startPos.Y < 0)
                {
                    startPos.Move(eYoonDir2D.Right);
                    return Scan2D(pBuffer, width, height, nDir.ReverseY(), startPos, threshold, isWhite);
                }
                ////  White IYoonVector를 찾는다.
                if (isWhite)
                {
                    if (pBuffer[startPos.Y * width + startPos.X] >= threshold)
                        return startPos.Clone();
                    else
                    {
                        startPos.Move(nDir);
                        return Scan2D(pBuffer, width, height, nDir, startPos, threshold, isWhite);
                    }
                }
                ////  Black IYoonVector를 찾는다.
                else
                {
                    if (pBuffer[startPos.Y * width + startPos.X] < threshold)
                        return startPos.Clone();
                    else
                    {
                        startPos.Move(nDir);
                        return Scan2D(pBuffer, width, height, nDir, startPos, threshold, isWhite);
                    }
                }
            }

            public static IYoonVector Scan2D(int[] pBuffer, int width, int height, eYoonDir2D nDir, YoonVector2N startPos, int threshold, bool isWhite)
            {
                if (startPos.X >= width && startPos.Y >= height)
                    return new YoonVector2N(-1, -1);
                if (startPos.X < 0 && startPos.Y < 0)
                    return new YoonVector2N(-1, -1);
                if (startPos.X >= width || startPos.X < 0)
                {
                    startPos.Move(eYoonDir2D.Bottom);
                    return Scan2D(pBuffer, width, height, nDir.ReverseX(), startPos, threshold, isWhite);
                }
                if (startPos.Y >= height || startPos.Y < 0)
                {
                    startPos.Move(eYoonDir2D.Right);
                    return Scan2D(pBuffer, width, height, nDir.ReverseY(), startPos, threshold, isWhite);
                }
                ////  White IYoonVector를 찾는다.
                if (isWhite)
                {
                    if (pBuffer[startPos.Y * width + startPos.X] >= threshold)
                        return startPos.Clone();
                    else
                    {
                        startPos.Move(nDir);
                        return Scan2D(pBuffer, width, height, nDir, startPos, threshold, isWhite);
                    }
                }
                ////  Black IYoonVector를 찾는다.
                else
                {
                    if (pBuffer[startPos.Y * width + startPos.X] < threshold)
                        return startPos.Clone();
                    else
                    {
                        startPos.Move(nDir);
                        return Scan2D(pBuffer, width, height, nDir, startPos, threshold, isWhite);
                    }
                }
            }
        }

        // Threshold 추출
        public static class Threshold // -> YoonImage
        {
            #region Histogram Graph에서 Threshold 추출하기
            //  Histogram의 Peak점에 위치한 Gray Level을 추출한다.
            public static int GetThresholdHistogram(ref int[] pBuffer, int imageWidth, YoonRect2N scanArea)
            {
                int i, j, value;
                int maxLevel, maxNumber;
                int[] histogram = new int[1024];
                Array.Clear(histogram, 0, histogram.Length);
                ////  Histogram 그래프를 만든다.
                for (i = scanArea.Left; i < scanArea.Right; i++)
                {
                    for (j = scanArea.Top; j < scanArea.Bottom; j++)
                    {
                        value = pBuffer[j * imageWidth + i];
                        if (value > 1023 || value < 0)
                            continue;
                        histogram[value]++;
                    }
                }
                ////  Peak 위치의 Pixel 갯수(number)와 그 때의 X축값(gray level)을 추출한다.
                maxLevel = 0;
                maxNumber = 0;
                for (i = 0; i < 1024; i++)
                {
                    if (histogram[i] > maxNumber)
                    {
                        maxNumber = histogram[i];
                        maxLevel = i;
                    }
                }
                return maxLevel;
            }

            public static int GetThresholdHistogram(ref byte[] pBuffer, int imageWidth, YoonRect2N scanArea)
            {
                int i, j, value;
                int maxLevel, maxNumber;
                int[] histogram = new int[256];
                Array.Clear(histogram, 0, histogram.Length);
                ////  Histogram 그래프를 만든다.
                for (i = scanArea.Left; i < scanArea.Right; i++)
                {
                    for (j = scanArea.Top; j < scanArea.Bottom; j++)
                    {
                        value = pBuffer[j * imageWidth + i];
                        if (value > 255 || value < 0)
                            continue;
                        histogram[value]++;
                    }
                }
                ////  Peak 위치의 Pixel 갯수(number)와 그 때의 X축값(gray level)을 추출한다.
                maxLevel = 0;
                maxNumber = 0;
                for (i = 0; i < 256; i++)
                {
                    if (histogram[i] > maxNumber)
                    {
                        maxNumber = histogram[i];
                        maxLevel = i;
                    }
                }
                return maxLevel;
            }

            public static int GetThresholdHistogram(ref byte[] pBuffer, int size)
            {
                int i, value;
                int maxLevel, maxNumber;
                int[] histogram = new int[256];
                Array.Clear(histogram, 0, histogram.Length);
                ////  Histogram 그래프를 만든다.
                for (i = 0; i < size; i++)
                {
                    value = pBuffer[i];
                    if (value > 255 || value < 0)
                        continue;
                    histogram[value]++;
                }
                ////  Peak 위치의 Pixel 갯수(number)와 그 때의 X축값(gray level)을 추출한다.
                maxLevel = 0;
                maxNumber = 0;
                for (i = 0; i < 256; i++)
                {
                    if (histogram[i] > maxNumber)
                    {
                        maxNumber = histogram[i];
                        maxLevel = i;
                    }
                }
                return maxLevel;
            }
            #endregion

            #region 평균 Threshold 추출하기
            //  Gray Level의 평균을 Threshold로 가져온다.
            public static int GetThresholdAverage(ref byte[] pBuffer, int imageWidth, YoonRect2N scanArea)
            {
                int i, j;
                int sum, count, average;
                sum = 0;
                count = 0;
                for (j = scanArea.Top; j < scanArea.Bottom; j++)
                {
                    for (i = scanArea.Left; i < scanArea.Right; i++)
                    {
                        sum += pBuffer[j * imageWidth + i];
                        count++;
                    }
                }
                if (count < 1) count = 1;
                average = sum / count;
                return average;
            }

            //  Gray Level의 최대값과 최소값 사이를 Threshold로 설정한다.
            public static int GetThresholdMinMax(ref byte[] pBuffer, int imageWidth, YoonRect2N scanArea)
            {
                int average, sum, count;
                int i, j, ii, jj;
                int minLevel, maxLevel, threshold;
                minLevel = 100000;
                maxLevel = 0;
                for (j = scanArea.Top; j < scanArea.Bottom - 3; j++)
                {
                    for (i = scanArea.Left; i < scanArea.Right - 3; i++)
                    {
                        sum = 0;
                        count = 0;
                        for (jj = 0; jj < 3; jj++)
                        {
                            for (ii = 0; ii < 3; ii++)
                            {
                                sum += pBuffer[(j + jj) * imageWidth + (i + ii)];
                                count++;
                            }
                        }
                        average = sum / count;
                        if (average < minLevel) minLevel = average;
                        if (average > maxLevel) maxLevel = average;
                    }
                }
                threshold = (minLevel * 2 + maxLevel) / 3;
                return threshold;
            }
            #endregion

            #region Image 전체의 Gray Level 정보 산출하기
            //  Gray Level 관련 정보 가져오기.
            public static void GetGrayLevelInfo(ref byte[] pBuffer, int imageWidth, YoonRect2N scanArea, out int Min, out int Max, out int Average)
            {
                if (scanArea.Right > imageWidth)
                {
                    Min = Max = Average = -1;
                    return;
                }
                int i, j, ii, jj;
                int count, sum, step;
                double totalSum, totalCount;
                int min, max, average;
                Min = 0;
                Max = 0;
                Average = 0;
                step = 5;
                min = 1000000;
                max = 0;
                totalSum = 0;
                totalCount = 0;
                ////  평균 Gray Level 산출.
                for (j = scanArea.Top; j < scanArea.Bottom; j++)
                {
                    for (i = scanArea.Left; i < scanArea.Right; i++)
                    {
                        if (pBuffer[j * imageWidth + i] >= 255)
                            continue;
                        totalSum += pBuffer[j * imageWidth + i];
                        totalCount++;
                    }
                }
                if (totalCount < 1)
                    totalCount = 1;
                average = (int)(totalSum / totalCount);
                ////  최대, 최소 Gray Level 산출.
                for (j = scanArea.Top; j < scanArea.Bottom - step; j += step)
                {
                    for (i = scanArea.Left; i < scanArea.Right - step; i += step)
                    {
                        sum = 0;
                        count = 0;
                        for (jj = 0; jj < step; jj++)
                        {
                            for (ii = 0; ii < step; ii++)
                            {
                                sum += pBuffer[(j + jj) * imageWidth + (i + ii)];
                                count++;
                            }
                        }
                        sum /= count;
                        if (sum >= 255)
                            continue;
                        if (sum < min) min = sum;
                        if (sum > max) max = sum;
                    }
                }
                Min = min;
                Max = max;
                Average = average;
            }

            public static void GetGrayLevelInfo(ref int[] pBuffer, int imageWidth, YoonRect2N scanArea, out int Min, out int Max, out int Average)
            {
                if (scanArea.Right > imageWidth)
                {
                    Min = Max = Average = -1;
                    return;
                }

                int i, j, ii, jj;
                int count, sum, step;
                double totalSum, totalCount;
                int min, max, average;
                step = 3;
                min = 1000000;
                max = 0;
                totalSum = 0;
                totalCount = 0;
                for (j = scanArea.Top; j < scanArea.Bottom; j++)
                {
                    for (i = scanArea.Left; i < scanArea.Right; i++)
                    {
                        if (pBuffer[j * imageWidth + i] >= 255)
                            continue;
                        totalSum += pBuffer[j * imageWidth + i];
                        totalCount++;
                    }
                }
                if (totalCount < 1)
                    totalCount = 1;
                average = (int)(totalSum / totalCount);

                for (j = scanArea.Top; j < scanArea.Bottom - step; j += step)
                {
                    for (i = scanArea.Left; i < scanArea.Right - step; i += step)
                    {
                        sum = 0;
                        count = 0;
                        for (jj = 0; jj < step; jj++)
                        {
                            for (ii = 0; ii < step; ii++)
                            {
                                sum += pBuffer[(j + jj) * imageWidth + (i + ii)];
                                count++;
                            }
                        }
                        sum /= count;
                        if (sum >= 255)
                            continue;
                        if (sum < min) min = sum;
                        if (sum > max) max = sum;
                    }
                }
                Min = min;
                Max = max;
                Average = average;
            }
            #endregion
        }

        // Image에 각종 도형 그리기
        public static class Draw // -> YoonImage
        {
            #region 각종 그리기
            //  삼각형 칠하기.
            public static void FillTriangle(ref Bitmap pImage, int x, int y, int size, eYoonDir2D direction, Color fillColor, double zoom)
            {
                PointF[] pPoint = new PointF[3];
                pPoint[0].X = (float)(x * zoom);
                pPoint[0].Y = (float)(y * zoom);
                switch (direction)
                {
                    case eYoonDir2D.Top:
                        pPoint[1].X = pPoint[0].X + (float)(size / 2 * zoom);
                        pPoint[1].Y = pPoint[0].Y + (float)(size * zoom);
                        pPoint[2].X = pPoint[0].X - (float)(size / 2 * zoom);
                        pPoint[2].Y = pPoint[0].Y + (float)(size * zoom);
                        break;
                    case eYoonDir2D.Bottom:
                        pPoint[1].X = pPoint[0].X - (float)(size / 2 * zoom);
                        pPoint[1].Y = pPoint[0].Y - (float)(size * zoom);
                        pPoint[2].X = pPoint[0].X + (float)(size / 2 * zoom);
                        pPoint[2].Y = pPoint[0].Y - (float)(size * zoom);
                        break;
                    case eYoonDir2D.Left:
                        pPoint[1].X = pPoint[0].X + (float)(size * zoom);
                        pPoint[1].Y = pPoint[0].Y - (float)(size / 2 * zoom);
                        pPoint[2].X = pPoint[0].X + (float)(size * zoom);
                        pPoint[2].Y = pPoint[0].Y + (float)(size / 2 * zoom);
                        break;
                    case eYoonDir2D.Right:
                        pPoint[1].X = pPoint[0].X - (float)(size * zoom);
                        pPoint[1].Y = pPoint[0].Y + (float)(size / 2 * zoom);
                        pPoint[2].X = pPoint[0].X - (float)(size * zoom);
                        pPoint[2].Y = pPoint[0].Y - (float)(size / 2 * zoom);
                        break;
                }
                using (Graphics graph = Graphics.FromImage(pImage))
                {
                    SolidBrush brush = new SolidBrush(fillColor);
                    graph.FillPolygon(brush, pPoint);
                }
            }

            //  사각형 칠하기.
            public static void FillRect(ref Bitmap pImage, int centerX, int centerY, int width, int height, Color fillColor, double zoom)
            {
                float startX = (float)(centerX - width / 2) * (float)zoom;
                float startY = (float)(centerY - height / 2) * (float)zoom;
                using (Graphics graph = Graphics.FromImage(pImage))
                {
                    SolidBrush brush = new SolidBrush(fillColor);
                    graph.FillRectangle(brush, startX, startY, (float)width, (float)height);
                }
            }

            //  다각형 칠하기.
            public static void FillPoligon(ref Bitmap pImage, YoonVector2N[] pArrayPoint, Color fillColor, double zoom)
            {
                PointF[] pArrayDraw = new PointF[pArrayPoint.Length];
                for(int iPoint = 0; iPoint<pArrayPoint.Length; iPoint++)
                {
                    pArrayDraw[iPoint].X = (float)pArrayPoint[iPoint].X * (float)zoom;
                    pArrayDraw[iPoint].Y = (float)pArrayPoint[iPoint].Y * (float)zoom;
                }

                using (Graphics graph = Graphics.FromImage(pImage))
                {
                    SolidBrush brush = new SolidBrush(fillColor);
                    graph.FillPolygon(brush, pArrayDraw);
                }
            }

            //  전부 칠하기.
            public static void FillCanvas(ref Bitmap pImage, Color fillColor)
            {
                Rectangle pRectCanvas = new Rectangle(0, 0, pImage.Width, pImage.Height);
                using(Graphics graph = Graphics.FromImage(pImage))
                {
                    SolidBrush brush = new SolidBrush(fillColor);
                    Region pRegion = new Region(pRectCanvas);
                    graph.FillRegion(brush, pRegion);
                }
            }

            //  삼각형 그리기.
            public static void DrawTriangle(ref Bitmap pImage, int x, int y, int size, eYoonDir2D direction, int penWidth, Color penColor, double zoom)
            {
                PointF[] pIYoonVector = new PointF[3];
                pIYoonVector[0].X = (float)(x * zoom);
                pIYoonVector[0].Y = (float)(y * zoom);
                switch (direction)
                {
                    case eYoonDir2D.Top:
                        pIYoonVector[1].X = pIYoonVector[0].X + (float)(size / 2 * zoom);
                        pIYoonVector[1].Y = pIYoonVector[0].Y + (float)(size * zoom);
                        pIYoonVector[2].X = pIYoonVector[0].X - (float)(size / 2 * zoom);
                        pIYoonVector[2].Y = pIYoonVector[0].Y + (float)(size * zoom);
                        break;
                    case eYoonDir2D.Bottom:
                        pIYoonVector[1].X = pIYoonVector[0].X - (float)(size / 2 * zoom);
                        pIYoonVector[1].Y = pIYoonVector[0].Y - (float)(size * zoom);
                        pIYoonVector[2].X = pIYoonVector[0].X + (float)(size / 2 * zoom);
                        pIYoonVector[2].Y = pIYoonVector[0].Y - (float)(size * zoom);
                        break;
                    case eYoonDir2D.Left:
                        pIYoonVector[1].X = pIYoonVector[0].X + (float)(size * zoom);
                        pIYoonVector[1].Y = pIYoonVector[0].Y - (float)(size / 2 * zoom);
                        pIYoonVector[2].X = pIYoonVector[0].X + (float)(size * zoom);
                        pIYoonVector[2].Y = pIYoonVector[0].Y + (float)(size / 2 * zoom);
                        break;
                    case eYoonDir2D.Right:
                        pIYoonVector[1].X = pIYoonVector[0].X - (float)(size * zoom);
                        pIYoonVector[1].Y = pIYoonVector[0].Y + (float)(size / 2 * zoom);
                        pIYoonVector[2].X = pIYoonVector[0].X - (float)(size * zoom);
                        pIYoonVector[2].Y = pIYoonVector[0].Y - (float)(size / 2 * zoom);
                        break;
                }
                using (Graphics graph = Graphics.FromImage(pImage))
                {
                    Pen pen = new Pen(penColor, (float)penWidth);
                    graph.DrawLine(pen, pIYoonVector[0], pIYoonVector[1]);
                    graph.DrawLine(pen, pIYoonVector[1], pIYoonVector[2]);
                    graph.DrawLine(pen, pIYoonVector[2], pIYoonVector[0]);
                }
            }

            //  사각형 그리기.
            public static void DrawRect(ref Bitmap pImage, YoonRect2N rect, int penWidth, Color penColor, double ratio)
            {
                if (rect.Right <= rect.Left || rect.Bottom <= rect.Top)
                    return;
                DrawLine(ref pImage, rect.Left, rect.Top, rect.Right, rect.Top, penWidth, penColor, ratio);
                DrawLine(ref pImage, rect.Right, rect.Top, rect.Right, rect.Bottom, penWidth, penColor, ratio);
                DrawLine(ref pImage, rect.Right, rect.Bottom, rect.Left, rect.Bottom, penWidth, penColor, ratio);
                DrawLine(ref pImage, rect.Left, rect.Bottom, rect.Left, rect.Top, penWidth, penColor, ratio);
            }

            //  선분 그리기.
            public static void DrawLine(ref Bitmap pImage, int x, int y, int x1, int y1, int penWidth, Color penColor, double ratio)
            {
                double deltaX, deltaY, deltaX1, deltaY1;
                deltaX = (double)x * ratio;
                deltaY = (double)y * ratio;
                deltaX1 = (double)x1 * ratio;
                deltaY1 = (double)y1 * ratio;
                using (Graphics graph = Graphics.FromImage(pImage))
                {
                    Pen pen = new Pen(penColor, penWidth);
                    graph.DrawLine(pen, new PointF((float)Math.Round(deltaX), (float)Math.Round(deltaY)), new PointF((float)Math.Round(deltaX1), (float)Math.Round(deltaY1)));
                }
            }

            //  글자 적기.
            public static void DrawText(ref Bitmap pImage, int x, int y, string text, int fontSize, Color fontColor, double ratio)
            {
                float deltaX, deltaY, size;
                deltaX = (float)(x * ratio);
                deltaY = (float)(y * ratio);
                size = (float)fontSize;
                if (size < 10) size = 10;
                using (Graphics graph = Graphics.FromImage(pImage))
                {
                    Brush brush = new SolidBrush(fontColor);
                    FontFamily fontFamily = new FontFamily("맑은 고딕");
                    Font font = new Font(fontFamily, size, FontStyle.Regular, GraphicsUnit.Pixel);
                    graph.DrawString(text, font, brush, deltaX, deltaY);
                }
            }

            //  십자가 그리기.
            public static void DrawCross(ref Bitmap pImage, int x, int y, int size, int penWidth, Color penColor, double zoom)
            {
                float deltaX, deltaY;
                float x1, x2, y1, y2;
                deltaX = (float)(x * zoom);
                deltaY = (float)(y * zoom);
                x1 = deltaX - size;
                x2 = deltaX + size;
                y1 = deltaY - size;
                y2 = deltaY + size;
                using (Graphics graph = Graphics.FromImage(pImage))
                {
                    Pen pen = new Pen(penColor, (float)penWidth);
                    graph.DrawLine(pen, new PointF(x1, deltaY), new PointF(x2, deltaY));
                    graph.DrawLine(pen, new PointF(deltaX, y1), new PointF(deltaX, y2));
                }
            }

            //  타원 그리기.
            public static void DrawEllipse(ref Bitmap pImage, YoonRect2N rect, int penWidth, Color penColor, double ratio)
            {
                int x1, y1, x2, y2;
                x1 = (int)Math.Round(rect.Left * ratio);
                y1 = (int)Math.Round(rect.Top * ratio);
                x2 = (int)Math.Round(rect.Right * ratio);
                y2 = (int)Math.Round(rect.Bottom * ratio);
                using (Graphics graph = Graphics.FromImage(pImage))
                {
                    Pen pen = new Pen(penColor, (float)penWidth);
                    graph.DrawEllipse(pen, x1, y1, (x2 - x1), (y2 - y1));
                }
            }
            #endregion
        }

        // Image 변형하기 (확대, 축소, 기울이기 등)  
        public static class Transform
        {
            #region Image 확대, 축소, 회전, 기울이기
            //  Image 확대, 축소하기.
            public static void Zoom(out int[] pDestination, int destWidth, int destHeight, ref int[] pSource, int sourceWidth, int sourceHeight)
            {
                int i, j, x, y;
                float x1, y1, x2, y2;                               // 좌표
                float ratioX, ratioY;
                int intensity1, intensity2, intensity3, intensity4; // Gray Level
                float value1, value2, resultLevel;
                ratioX = (float)destWidth / (float)(sourceWidth - 1);
                ratioY = (float)destHeight / (float)(sourceHeight - 1);
                pDestination = new int[destWidth * destHeight];
                ////  비율별 확대, 축소
                for (j = 1; j < sourceHeight; j++)
                {
                    for (i = 1; i < sourceWidth; i++)
                    {
                        x1 = (float)(i - 1) * ratioX;
                        y1 = (float)(j - 1) * ratioY;
                        x2 = (float)i * ratioX;
                        y2 = (float)j * ratioY;

                        if (x2 >= destWidth) x2 = destWidth - 1;
                        if (y2 >= destHeight) y2 = destHeight - 1;
                        intensity1 = pSource[(j - 1) * sourceWidth + (i - 1)];
                        intensity2 = pSource[(j - 1) * sourceWidth + i];
                        intensity3 = pSource[j * sourceWidth + (i - 1)];
                        intensity4 = pSource[j * sourceWidth + i];
                        for (y = (int)y1; y <= (int)y2; y++)
                        {
                            for (x = (int)x1; x <= (int)x2; x++)
                            {
                                value1 = (x - x2) * intensity1 / (x1 - x2) + (x - x1) * intensity2 / (x2 - x1);
                                value2 = (x - x2) * intensity3 / (x1 - x2) + (x - x1) * intensity4 / (x2 - x1);
                                resultLevel = (y - y2) * value1 / (y1 - y2) + (y - y1) * value2 / (y2 - y1);
                                pDestination[y * destWidth + x] = (int)resultLevel;
                            }
                        }
                    }
                }
            }

            //  회전.
            public static void Rotate(ref byte[] pBuffer, int bufferWidth, int bufferHeight, int centerX, int centerY, double angle)
            {
                int i, j;
                int x1, y1, x2, y2;
                double theta, sinTheta, cosTheta;
                double posX1, posY1, posX2, posY2;
                double level1, level2, level3, level4, tempLevel;
                double roundX, roundY;
                byte[] pTempBuffer;
                if (Math.Abs(angle) < 0.001)
                    return;
                theta = angle * 3.141592 / 180.0;
                sinTheta = Math.Sin(theta);
                cosTheta = Math.Cos(theta);
                ////  임시 Buffer에 Image를 복사한다.
                pTempBuffer = new byte[bufferWidth * bufferHeight];
                Array.Clear(pTempBuffer, 0, pTempBuffer.Length);
                ////  임시 Buffer에 회전 처리한 Image를 복사한다.
                //	for(y=0;	y<bufferHeight;	y++)
                //	{
                //		for(x=0;	x<bufferWidth;	x++)
                //		{
                //			i  = -1.0*((double)(y-centerY)*sin) + (double)centerX;
                //			j  = (double)(x-centerX)*cos;
                //			x2 = i + j;
                //			if(x2<0)			x2 = 0;
                //			if(x2>=bufferWidth)	x2 = bufferWidth-1;
                //			//////
                //			m  = (x - centerX)*sinTheta + centerY;
                //			n  = (y - centerY)*cosTheta;
                //			y2 = m + n;
                //			if(y2<0)				y2 = 0;
                //			if(y2>=bufferHeight)	y2 = bufferHeight-1;
                //			//////
                //			pTempBuffer[y2*bufferWidth+x2] = pBuffer[y*bufferWidth+x];
                //		}
                //	}
                //	for(j=0;	j<bufferHeight;	j++)
                //	{
                //		y = j - centerY;
                //		for(i=0;	i<bufferWidth;	i++)
                //		{
                //			x  = i - centerX;
                //			x2 = ROUND((double)centerX + (double)x*cosTheta - (double)y*sinTheta);
                //			y2 = ROUND((double)centerY + (double)x*sinTheta + (double)y*cosTheta);
                //			if(x2<0)				x2 = 0;
                //			if(x2>=bufferWidth)		x2 = bufferWidth-1;
                //			if(y2<0)				y2 = 0;
                //			if(y2>=bufferHeight)	y2 = bufferHeight-1;
                //			//pTempBuffer[y2*bufferWidth+x2] = pBuffer[j*bufferWidth+i];
                //			pTempBuffer[j*bufferWidth+i]   = pBuffer[y2*bufferWidth+x2];
                //		}
                //	}
                ////  회전 알고리즘.
                for (j = 0; j < bufferHeight; j++)
                {
                    posY1 = (double)j - (double)centerY;
                    for (i = 0; i < bufferWidth; i++)
                    {
                        //////  Image 선회전.
                        posX1 = (double)i - (double)centerX;
                        posX2 = (double)centerX + (posX1 * cosTheta - posY1 * sinTheta);
                        posY2 = (double)centerY + (posX1 * sinTheta + posY1 * cosTheta);
                        if (posX2 < 0.0 || posY2 < 0.0)
                            continue;
                        //////  Image 보간.
                        x1 = (int)posX2;  // 정수형으로 변환하여 위치좌표 얻는다.
                        y1 = (int)posY2;
                        x2 = x1 + 1;
                        y2 = y1 + 1;
                        //////  좌표가 한계 범위를 넘은 경우 스킵.
                        if (x1 < 0 || x1 >= bufferWidth || y1 < 0 || y2 >= bufferHeight)
                            continue;
                        roundX = posX2 - x1;
                        roundY = posY2 - y1;
                        //////  보간을 위한 4개 좌표의 Pixel 산출 후 Filtering으로 Gray Level 선정.
                        level1 = pBuffer[y1 * bufferWidth + x1];
                        level2 = pBuffer[y1 * bufferWidth + x2];
                        level3 = pBuffer[y2 * bufferWidth + x1];
                        level4 = pBuffer[y2 * bufferWidth + x2];
                        tempLevel = (1.0 - roundX) * (1.0 - roundY) * level1 + roundX * (1.0 - roundY) * level2 + (1.0 - roundX) * roundY * level3 + roundX * roundY * level4;
                        if (tempLevel < 0) tempLevel = 0;
                        if (tempLevel > 255) tempLevel = 255;
                        pTempBuffer[j * bufferWidth + i] = (byte)tempLevel;
                    }
                }
                pTempBuffer.CopyTo(pBuffer, 0);
            }

            //  Image 반전.
            public static void Reverse(ref byte[] pBuffer, int bufferWidth, int bufferHeight)
            {
                int i, j;

                for (j = 0; j < bufferHeight; j++)
                {
                    for (i = 0; i < bufferWidth; i++)
                    {
                        pBuffer[j * bufferWidth + i] = (byte)(255 - pBuffer[j * bufferWidth + i]);
                    }
                }
            }

            //  직사각형 Image를 평행사변형꼴로 기울인다.
            public static void Warp(ref byte[] pSource, int sourceWidth, int sourceHeight, YoonVector2N[] pEdgePos, ref byte[] pDestination, int destWidth, int destHeight)
            {
                int iX, iY;
                int x1, y1, x2, y2;
                double dx, dy, roundX, roundY;
                double level1, level2, level3, level4, tempLevel;
                double leftTopToLeftBottom_X, leftTopToLeftBottom_Y;
                double rightTopToRightBottom_x, rightTopToRightBottom_y;
                double leftBottomToRightBottom_x, leftBottomToRightBottom_y;
                double newLeftBottomX, newLeftBottomY, newRightBottomX, newRightBottomY;
                YoonVector2N leftTop, rightTop, leftBottom, rightBottom;
                leftTop = new YoonVector2N();
                rightTop = new YoonVector2N();
                leftBottom = new YoonVector2N();
                rightBottom = new YoonVector2N();
                ////  원래 값을 임시 Buffer에 보관해 놓는다.
                Array.Clear(pDestination, 0, pDestination.Length);
                //	enum { EDGE_LEFT_TOP, EDGE_RIGHT_TOP, EDGE_RIGHT_BOTTOM, EDGE_LEFT_BOTTOM, EDGE_TOTAL}; // 각 Edge의 순서 설정.
                leftTop.X = pEdgePos[(int)eYoonDir2D.TopLeft].X;
                leftTop.Y = pEdgePos[(int)eYoonDir2D.TopLeft].Y;
                rightTop.X = pEdgePos[(int)eYoonDir2D.TopRight].X;
                rightTop.Y = pEdgePos[(int)eYoonDir2D.TopRight].Y;
                leftBottom.X = pEdgePos[(int)eYoonDir2D.BottomLeft].X;
                leftBottom.Y = pEdgePos[(int)eYoonDir2D.BottomLeft].Y;
                rightBottom.X = pEdgePos[(int)eYoonDir2D.BottomRight].X;
                rightBottom.Y = pEdgePos[(int)eYoonDir2D.BottomRight].Y;
                leftTopToLeftBottom_X = leftTop.X - leftBottom.X;
                leftTopToLeftBottom_Y = leftTop.Y - leftBottom.Y;
                rightTopToRightBottom_x = rightTop.X - rightBottom.X;
                rightTopToRightBottom_y = rightTop.Y - rightBottom.Y;
                ////  보간 작업을 실시한다.
                for (iY = 0; iY < destHeight; iY++)
                {
                    newLeftBottomX = leftBottom.X + (double)(destHeight - iY) * leftTopToLeftBottom_X / destHeight;
                    newLeftBottomY = leftBottom.Y + (double)(destHeight - iY) * leftTopToLeftBottom_Y / destHeight;
                    newRightBottomX = rightBottom.X + (double)(destHeight - iY) * rightTopToRightBottom_x / destHeight;
                    newRightBottomY = rightBottom.Y + (double)(destHeight - iY) * rightTopToRightBottom_y / destHeight;
                    leftBottomToRightBottom_x = newRightBottomX - newLeftBottomX;
                    leftBottomToRightBottom_y = newRightBottomY - newLeftBottomY;
                    for (iX = 0; iX < destWidth; iX++)
                    {
                        dx = newLeftBottomX + iX * leftBottomToRightBottom_x / destWidth;
                        dy = newLeftBottomY + iX * leftBottomToRightBottom_y / destWidth;
                        x1 = (int)dx;
                        y1 = (int)dy;
                        x2 = x1 + 1;
                        y2 = y1 + 1;
                        if (x2 >= sourceWidth) x2 = sourceWidth - 1;
                        if (y2 >= sourceHeight) y2 = sourceHeight - 1;
                        roundX = dx - x1;
                        roundY = dy - y1;
                        level1 = pSource[y1 * sourceWidth + x1];
                        level2 = pSource[y1 * sourceWidth + x2];
                        level3 = pSource[y2 * sourceWidth + x1];
                        level4 = pSource[y2 * sourceWidth + x2];
                        tempLevel = (1.0 - roundX) * (1.0 - roundY) * level1 + roundX * (1.0 - roundY) * level2 + (1.0 - roundX) * roundY * level3 + roundX * roundY * level4;
                        //////  Level이 지나칠 경우 0 또는 255로 정의.
                        if (tempLevel < 0) tempLevel = 0;
                        if (tempLevel > 255) tempLevel = 255;
                        pDestination[iY * destWidth + iX] = (byte)tempLevel;
                    }
                }
            }
            #endregion
        }

        // Gray Level 변곡점 찾기
        public static class Peak // -> YoonImage
        {
            #region "Gray Level이 가파르게 올라가는" 위치찾기
            //  최저 Gray Level에서 최고 Gray Level로 치고 올라오는 경우 기록함.
            public static int FindPeakPosition(ref int[] pBuffer, out int[] pArrayPeakPos, int size, int diffHeight)
            {
                int i;
                int x0, x1, x2;
                int prevDiff, currDiff, tempDiff;
                int minLevel, referLevel;
                int peakNum = 0;
                pArrayPeakPos = new int[MAX_PICK_NUM];
                minLevel = 100000;
                currDiff = 0;
                prevDiff = 0;
                minLevel = 0;
                for (i = 1; i < size; i++)
                {
                    currDiff = pBuffer[i] - pBuffer[i - 1];
                    if (i == 1)
                    {
                        prevDiff = currDiff;
                        continue;
                    }
                    ////  이전 IYoonVector에 비해 증가 추세일 때
                    if (currDiff >= 0)
                    {
                        ////  하한점에서 치고 올라오는 경우.
                        if (prevDiff < 0)
                        {
                            minLevel = pBuffer[i];
                        }
                    }
                    ////  이전 IYoonVector에 비해 감소 추세일 때
                    else
                    {
                        ////  상한점에서 떨어지고 있는 경우.
                        if (prevDiff >= 0)
                        {
                            tempDiff = pBuffer[i] - minLevel;
                            ////  상한점에서 하한점까지 사이 길이가 기준값(diffHeight)보다 큰 경우.
                            if (tempDiff >= diffHeight)
                            {
                                if (peakNum < MAX_PICK_NUM)
                                {
                                    pArrayPeakPos[peakNum] = i - 1;
                                    peakNum++;
                                }
                            }
                        }
                    }
                    prevDiff = currDiff;
                }
                ////  정확한 센터를 잡기 위해서 Reference를 계산한다.
                for (int iPeak = 0; iPeak < peakNum; iPeak++)
                {
                    x0 = pArrayPeakPos[iPeak];
                    if (x0 < 0 || x0 >= size)
                        continue;
                    referLevel = pBuffer[x0] - 5;  // 5 Level 낮은 부분 검색함.
                                                   ////  왼쪽으로 스캔함.
                    x1 = x0;
                    for (i = x0; i >= 0; i--)
                    {
                        if (pBuffer[i] <= referLevel)
                        {
                            x1 = i;
                            break;
                        }
                    }
                    ////  오른쪽으로 스캔함.
                    x2 = x0;
                    for (i = x0; i < size; i++)
                    {
                        if (pBuffer[i] <= referLevel)
                        {
                            x2 = i;
                            break;
                        }
                    }
                    pArrayPeakPos[iPeak] = (x1 + x2) / 2;
                }
                return peakNum;
            }

            public static int FindPeakPosition(ref float[] pBuffer, out int[] pArrayPeakPos, int size, int limitWidth, int limitHeight)
            {
                bool isSave;
                int startX, endX, centerX;
                int referX, referY, diffPos;
                int pPos, pCenter;
                double maxLevel, minLevel;
                double heightY;
                int peakNum = 0;
                pArrayPeakPos = new int[MAX_PICK_NUM];
                List<int> pListCenterPos = new List<int>();
                maxLevel = 0.0;
                minLevel = 100000.0;
                ////  최소, 최대 Buffer 기록.
                for (int i = 0; i < size; i++)
                {
                    if (pBuffer[i] < minLevel) minLevel = pBuffer[i];
                    if (pBuffer[i] > maxLevel) maxLevel = pBuffer[i];
                }

                for (double fY = maxLevel + 1.0; fY > minLevel; fY -= 1.0)
                {
                    startX = -1;
                    endX = -1;
                    for (int i = 0; i < size; i++)
                    {
                        if (pBuffer[i] > fY)
                        {
                            if (startX < 0)
                            {
                                startX = i;
                            }
                        }
                        else
                        {
                            if (startX >= 0)
                            {
                                endX = i;
                                centerX = (startX + endX) / 2;
                                ////  Start와 End 사이에 이전에 저장한 Center 값이 있을 경우 알고리즘 제외시킴.
                                isSave = true;
                                for (int iList = 0; iList < pListCenterPos.Count; iList++)
                                {
                                    pCenter = (int)pListCenterPos[iList];
                                    if (pCenter >= startX && pCenter <= endX)
                                    {
                                        isSave = false;
                                        break;
                                    }
                                }
                                ////  List 상에 최소 Gray Level을 제외한 나머지 Pixel들의 중간(Center)값을 저장함.
                                if (isSave)
                                {
                                    pCenter = new int();
                                    pCenter = centerX;
                                    pListCenterPos.Add(pCenter);
                                }
                                startX = -1;
                            }
                        }
                    }
                }
                ////  Center 값 정렬.
                Sort.SortInteger(ref pListCenterPos, eYoonDir2DMode.Increase);
                ////   하나씩 검증하면서 진행
                for (int iList = 0; iList < pListCenterPos.Count; iList++)
                {
                    pPos = (int)pListCenterPos[iList];
                    heightY = pBuffer[pPos];
                    referX = pPos;
                    referY = (int)(heightY - limitHeight);
                    //////  높이가 Limit보다 작을 경우는 통과함.
                    if (heightY < (float)limitHeight)
                        continue;
                    startX = 0;
                    for (int i = referX; i > 0; i--)
                    {
                        if (pBuffer[i] < referY)
                        {
                            startX = i;
                            break;
                        }
                    }
                    endX = size;
                    for (int i = referX; i < size; i++)
                    {
                        if (pBuffer[i] < referY)
                        {
                            endX = i;
                            break;
                        }
                    }
                    //////  설정된 폭보다 크면 통과함.
                    diffPos = endX - startX;
                    if (diffPos > limitWidth)
                        continue;
                    //////  고저를 오가는 Graph의 폭이 Image 폭과 같은 경우 통과함.
                    if (startX < 1 && endX >= size)
                        continue;
                    //////  Peak 지점 기록함.
                    if (peakNum < MAX_PICK_NUM - 1)
                    {
                        pArrayPeakPos[peakNum] = pPos;
                        peakNum++;
                    }
                }
                ////  List 삭제.
                pListCenterPos.Clear();
                return peakNum;
            }
            #endregion
        }
    }
}