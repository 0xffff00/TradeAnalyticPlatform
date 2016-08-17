﻿using BackTestingPlatform.Model.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BackTestingPlatform.Model.Futures
{
    /// <summary>
    /// 期货 基础信息
    /// </summary>
    public class FuturesInfo
    {
        public string code { get; set; }
        public DateTime endDate { get; set; }

        public List<Tick> ticks;
    }

   
}
