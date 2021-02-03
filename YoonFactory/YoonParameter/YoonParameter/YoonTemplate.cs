﻿using System;
using System.Collections.Generic;
using YoonFactory.Files;
using System.IO;

namespace YoonFactory.Param
{
    public class YoonTemplate<T> : YoonContainer<T>, IYoonTemplate where T : IConvertible
    {
        public int No { get; set; }
        public string Name { get; set; }
        public string RootDirectory { get; set; }

        public override string ToString()
        {
            return string.Format("{0:D2}_{1}", No, Name);
        }

        public YoonTemplate()
        {
            No = 0;
            Name = "Default";
            RootDirectory = Path.Combine(System.IO.Directory.GetCurrentDirectory(), "YoonFactory");
            m_pDicParam = new Dictionary<T, YoonParameter>(DefaultComparer);
        }

        public void CopyFrom(IYoonTemplate pTemplate)
        {
            if (pTemplate is YoonTemplate<T> pTempOrigin)
            {
                Clear();

                No = pTempOrigin.No;
                Name = pTempOrigin.Name;
                RootDirectory = pTempOrigin.RootDirectory;
                foreach (T pKey in pTempOrigin.Keys)
                {
                    Add(pKey, pTempOrigin[pKey]);
                }
            }
        }

        public new IYoonTemplate Clone()
        {
            YoonTemplate<T> pTemplate = new YoonTemplate<T>();
            {
                pTemplate.No = No;
                pTemplate.Name = Name;
                pTemplate.RootDirectory = RootDirectory;
                pTemplate.m_pDicParam = new Dictionary<T, YoonParameter>(m_pDicParam, DefaultComparer);
            }
            return pTemplate;
        }

        public bool LoadTemplate()
        {
            if (RootDirectory == string.Empty || m_pDicParam == null)
                return false;

            string strIniFilePath = Path.Combine(RootDirectory, @"YoonTemplate.ini");
            base.FilesDirectory = Path.Combine(RootDirectory, ToString());
            bool bResult = true;
            using (YoonIni pIni = new YoonIni(strIniFilePath))
            {
                pIni.LoadFile();
                No = pIni["HEAD"]["No"].ToInt(No);
                Name = pIni["HEAD"]["Name"].ToString(Name);
                int nCount = pIni["HEAD"]["Count"].ToInt(0);
                for (int iParam = 0; iParam < nCount; iParam++)
                {
                    T pKey = pIni["KEY"][iParam.ToString()].To(default(T));
                    if (!LoadValue(pKey))
                        bResult = false;
                }
            }
            return bResult;
        }

        public bool SaveTemplate()
        {
            if (RootDirectory == string.Empty || m_pDicParam == null)
                return false;

            string strIniFilePath = Path.Combine(RootDirectory, @"YoonTemplate.ini");
            base.FilesDirectory = Path.Combine(RootDirectory, ToString());
            bool bResult = true;
            using (YoonIni pIni = new YoonIni(strIniFilePath))
            {
                int iParam = 0;
                pIni["HEAD"]["No"] = No;
                pIni["HEAD"]["Name"] = Name;
                pIni["HEAD"]["Count"] = Count;
                foreach (T pKey in Keys)
                {
                    pIni["KEY"][(iParam++).ToString()] = pKey.ToString();
                    if (!SaveValue(pKey))
                        bResult = false;
                }
                pIni.SaveFile();
            }
            return bResult;
        }
    }

}
