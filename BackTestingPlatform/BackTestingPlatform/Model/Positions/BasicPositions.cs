﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BackTestingPlatform.Model.Positions
{
    public class BasicPositions
    {
        public string code { get; set; }
        public DateTime time { get; set; }
        public double positions { get; set; }
        public double averageOpenPirce { get; set; }
        public double price { get; set; }
        public double transactionFee { get; set; }
    }

    public class MinutePositions : BasicPositions
    {
        public int minuteIndex { get; set; }
    }
}
