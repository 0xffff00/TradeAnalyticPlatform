﻿using BackTestingPlatform.Core;
using BackTestingPlatform.Model.Common;
using BackTestingPlatform.Model.Positions;
using BackTestingPlatform.Utilities.Option;
using BackTestingPlatform.Utilities.TimeList;
using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using Autofac;
using BackTestingPlatform.DataAccess.Option;
using System.Text;
using System.Threading.Tasks;
using BackTestingPlatform.Model.Option;
using BackTestingPlatform.Utilities.Futures;

namespace BackTestingPlatform.AccountOperator.Minute.maoheng
{
    public class AccountUpdatingWithMinuteBar
    {
        //初始化log组件
        static Logger log = LogManager.GetCurrentClassLogger();
        static Dictionary<string,OptionInfo>optionInfoList=OptionInfoReform.ReformByCode(Platforms.container.Resolve<OptionInfoRepository>().fetchFromLocalCsvOrWindAndSaveAndCache(0));
        public static void computeAccount(ref BasicAccount myAccount, SortedDictionary<DateTime, Dictionary<string, PositionsWithDetail>> positions, DateTime now, int nowIndex,Dictionary<string, List<KLine>> data)
        {
            myAccount.time = now;
            //若position为null，直接跳过
            if (positions.Count == 0)
            {
                return;
            }
            //提取初始资产
            double initialCapital = myAccount.initialAssets;
            Dictionary<string, PositionsWithDetail> nowPosition = new Dictionary<string, PositionsWithDetail>();
            nowPosition = positions[positions.Keys.Last()];
            //初始化保证金，可用现金
            double totalMargin = 0;
            double totalCashFlow = 0;
            double totalPositionValue = 0;
            double totalAssets = 0;
            //当前时间对应data中timeList 的序号
            int index = nowIndex;
            if (index < 0)
            {
                log.Warn("Signal时间出错，请查验");
                return;
            }
            foreach (var item in nowPosition)
            {
                PositionsWithDetail position0 = item.Value;
                if (position0.volume!=0)
                {
                    double price = data[position0.code][index].close;
                    totalPositionValue += price * position0.volume;
                }
                if (position0.volume<0) //计算保证金
                {
                    if (position0.tradingVarieties=="option") //按每分钟收盘价来近似期权的保证金
                    {
                        totalMargin += (OptionMargin.ComputeMaintenanceMargin(data["510050.SH"][index].close, data[position0.code][index].close, optionInfoList[position0.code].strike, optionInfoList[position0.code].optionType, Math.Abs(position0.volume)));
                    }
                    else if (position0.tradingVarieties == "stock") //股票卖空按照一半保证金计算
                    {
                        totalMargin += 0.5*data[position0.code][index].close * Math.Abs(position0.volume);
                    }
                    else if (position0.tradingVarieties =="futures")
                    {
                        totalMargin += FutureMargin.ComputeOpenMargin(data[position0.code][index].close, 0.4, position0.volume);
                    }
                }
                totalCashFlow += position0.totalCashFlow;
            }
            myAccount.totalAssets = initialCapital + totalCashFlow + totalPositionValue;
            myAccount.freeCash = initialCapital  + totalCashFlow - totalMargin;
            myAccount.margin = totalMargin;
            myAccount.positionValue = totalPositionValue;
           
        }
    }
}
