﻿using BackTestingPlatform.Core;
using BackTestingPlatform.Model.Option;
using BackTestingPlatform.Utilities;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WAPIWrapperCSharp;

namespace BackTestingPlatform.DataAccess.Option
{
    public class OptionMinuteDataRepository
    {
        public const string PATH_KEY = "CacheData.Path.Option";
        public List<OptionMinuteKLine> fetchMinuteDataFromWind(string stockCode, DateTime time)
        {
            WindAPI w = Platforms.GetWindAPI();
            var timeStr = time.ToString("yyyyMM");
            string start = time.ToString("yyyy-MM-dd 00:00:00");
            string end = time.ToString("yyyy-MM-dd 23:59:59");
            WindData wd = w.wsi(stockCode.ToUpper(), "open,high,low,close,volume,amt", start,end, "periodstart=09:30:00;periodend=15:00:00;Fill=Previous");
            int len = wd.timeList.Length;
            int fieldLen = wd.fieldList.Length;
            List<OptionMinuteKLine> items = new List<OptionMinuteKLine>(len * fieldLen);
            if (!(wd.data is double[])) return null;
            double[] dataList = (double[])wd.data;
            DateTime[] timeList = wd.timeList;
            for (int k = 0; k < len; k++)
            {
                items.Add(new OptionMinuteKLine
                {
                    time = timeList[k],
                    open = (double)dataList[k * fieldLen + 0],
                    high = (double)dataList[k * fieldLen + 1],
                    low = (double)dataList[k * fieldLen + 2],
                    close = (double)dataList[k * fieldLen + 3],
                    volume = (double)dataList[k * fieldLen + 4],
                    amount = (double)dataList[k * fieldLen + 5]
                });
            }
            return items;
        }

        public void saveToLocalCsvFile(List<OptionMinuteKLine> optionMinuteData,string type,string code,DateTime date)
        {
            
         
        }
       
        public List<OptionMinuteKLine> fetchAllFromLocalCsvFile(string filePath)
        {

            if (!File.Exists(filePath)) return null;
            DataTable dt = CsvFileUtils.ReadFromCsvFile(filePath);
            
            return dt.AsEnumerable().Select(row => new OptionMinuteKLine
            {
                time = Kit.ToDateTime(row["time"]),
                open = Kit.ToDouble(row["open"]),
                high = Kit.ToDouble(row["high"]),
                low = Kit.ToDouble(row["low"]),
                close = Kit.ToDouble(row["close"]),
                volume = Kit.ToDouble(row["volume"]),
                amount = Kit.ToDouble(row["amount"]),
            }).ToList();
        }
            
    }
}