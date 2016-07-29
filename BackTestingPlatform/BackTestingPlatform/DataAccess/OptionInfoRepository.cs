﻿using BackTestingPlatform.Core;
using BackTestingPlatform.Model;
using BackTestingPlatform.Model.Option;
using System;
using System.Collections.Generic;
using WAPIWrapperCSharp;

namespace BackTestingPlatform.DataAccess
{
    public interface OptionInfoRepository
    {
        List<OptionInfo> fetchAll(string underlyingCode="510050.SH");
    }
    class OptionInfoRepositoryFromWind : OptionInfoRepository
    {
        public List<OptionInfo> fetchAll(string underlyingCode = "510050.SH")
        {
            WindAPI wapi = Platforms.GetWindAPI();
            WindData wd = wapi.wset("optioncontractbasicinfo", "exchange=sse;windcode="+underlyingCode+";status=all");
            int len = wd.timeList.Length;
            int fieldLen = wd.fieldList.Length;
            List<OptionInfo> items = new List<OptionInfo>(len);
            object[] dm = (object[])wd.data;
            DateTime[] ttime = wd.timeList;
            for (int k = 0; k < len; k += fieldLen)
            {
                items.Add(new OptionInfo
                {
                });
            }

            return items;

        }
    }
}