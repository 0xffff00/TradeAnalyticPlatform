using Autofac;
using BackTestingPlatform.Core;
using BackTestingPlatform.DataAccess;
using BackTestingPlatform.DataAccess.Futures;
using BackTestingPlatform.DataAccess.Option;
using BackTestingPlatform.DataAccess.Stock;
using BackTestingPlatform.Model.Common;
using BackTestingPlatform.Model.Option;
using BackTestingPlatform.Model.Positions;
using BackTestingPlatform.Model.Signal;
using BackTestingPlatform.Model.Stock;
using BackTestingPlatform.Transaction;
using BackTestingPlatform.Transaction.MinuteTransactionWithSlip;
using BackTestingPlatform.Utilities;
using BackTestingPlatform.Utilities.Option;
using BackTestingPlatform.Utilities.TimeList;
using NLog;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BackTestingPlatform.Strategies.Stock.StockSample;
using BackTestingPlatform.Strategies.Stock.StockSample01;
using BackTestingPlatform.Utilities.Common;
using BackTestingPlatform.AccountOperator.Minute;
using System.Windows.Forms;
using BackTestingPlatform.Charts;
using BackTestingPlatform.Strategies.Stock.StockSample.NDaysReversionStg;

namespace BackTestingPlatform.Strategies.Stock.StockSample
{
    public class NDaysReversion01
    {
        static Logger log = LogManager.GetCurrentClassLogger();
        private DateTime startDate, endDate;
        public NDaysReversion01(int start, int end)
        {
            startDate = Kit.ToDate(start);
            endDate = Kit.ToDate(end);
        }
        //�ز��������
        private double initialCapital = 10000000;
        private double slipPoint = 0.001;
        private static int contractTimes = 100;

        //���Բ����趨
        private int period = 1;//Ӧ������
        private int NDays = 6 * 1;//5���Ӽ���
        private int lengthOfBackLooking = 20;//�ؿ�����
        private double toleranceDegree = 0.005;//���̶ȣ�������λ�ķ���
        string targetVariety = "510050.SH";
        /// <summary>
        /// 50ETF��ʱ���Բ��ԣ�N-Days Reversion�����߶�
        /// </summary>
        public void compute()
        {
            log.Info("��ʼ�ز�(�ز���{0}��{1})", Kit.ToInt_yyyyMMdd(startDate), Kit.ToInt_yyyyMMdd(endDate));

            ///�˻���ʼ��
            //��ʼ��positions
            SortedDictionary<DateTime, Dictionary<string, PositionsWithDetail>> positions = new SortedDictionary<DateTime, Dictionary<string, PositionsWithDetail>>();
            //��ʼ��Account��Ϣ
            BasicAccount myAccount = new BasicAccount();
            myAccount.totalAssets = initialCapital;
            myAccount.freeCash = myAccount.totalAssets;
            myAccount.initialAssets = initialCapital;
            //��¼��ʷ�˻���Ϣ
            List<BasicAccount> accountHistory = new List<BasicAccount>();
            //��¼benchmark����
            List<double> benchmark = new List<double>();

            ///����׼��
            //��������Ϣ
            List<DateTime> tradeDays = DateUtils.GetTradeDays(startDate, endDate);
            //50etf��������׼����ȡȫ�ز��ڵ����ݴ����data
            Dictionary<string, List<KLine>> data = new Dictionary<string, List<KLine>>();
            foreach (var tempDay in tradeDays)
            {
                var ETFData = Platforms.container.Resolve<StockMinuteRepository>().fetchFromLocalCsvOrWindAndSave(targetVariety, tempDay);
                if (!data.ContainsKey(targetVariety))
                    data.Add(targetVariety, ETFData.Cast<KLine>().ToList());
                else
                    data[targetVariety].AddRange(ETFData.Cast<KLine>().ToList());
            }

            //Ƶ��ת������
            List<KLine> data_5min = MinuteFrequencyTransferUtils.MinuteToNPeriods(data[targetVariety], "Minutely", 5);
            List<KLine> data_15min = MinuteFrequencyTransferUtils.MinuteToNPeriods(data[targetVariety], "Minutely", 15);
            //List<KLine> data_1Day = MinuteFrequencyTransferUtils.MinuteToNPeriods(data[targetVariety], "Daily", 1);
            //List<KLine> data_1Month = MinuteFrequencyTransferUtils.MinuteToNPeriods(data[targetVariety], "Monthly", 1);
            // List<KLine> data_1Week = MinuteFrequencyTransferUtils.MinuteToNPeriods(data[targetVariety], "Weekly", 1);

            int indexOfNow = -1;//��¼����data��������һ����K���ϵ�����
            int indexOf5min = 0;//�����K���ϵ�����
            int indexOf15min = 0;//ʮ�����K���ϵ�����

            ///�ز�ѭ��
            //�ز�ѭ��--By Day
            foreach (var day in tradeDays)
            {
                //ȡ�����������
                Dictionary<string, List<KLine>> dataToday = new Dictionary<string, List<KLine>>();
                foreach (var variety in data)
                {
                    dataToday.Add(variety.Key, data[variety.Key].FindAll(s => s.time.Year == day.Year && s.time.Month == day.Month && s.time.Day == day.Day));
                }

                int index = 0;
                //���׿������ã�����day���Ľ��׿���
                bool tradingOn = true;//�ܽ��׿���
                bool openingOn = true;//���ֿ���
                bool closingOn = true;//ƽ�ֿ���

                //�Ƿ�Ϊ�ز����һ��
                bool isLastDayOfBackTesting = day.Equals(endDate);

                //�ز�ѭ�� -- By Minute
                //��������ͬһ��1minBar�Ͽ�ƽ��
                while (index < 240)
                {
                    int nextIndex = index + 1;
                    //��������ֵ
                    indexOfNow++;
                    //N����k�ߵĵ�ǰ����ֵ�ǵ�ǰʱ���֮ǰ��һ��������k�ߣ�Ϊ����data snooping��ȡ��ǰʱ�������
                    indexOf5min = indexOfNow / 5;
                    indexOf15min = indexOfNow / 15;

                    //ʵ�ʲ����ӵ�һ�������ں�ʼ

                    if (indexOfNow < lengthOfBackLooking - 1 || indexOf5min < lengthOfBackLooking - 1)
                    {
                        index = nextIndex;
                        continue;
                    }

                    DateTime now = TimeListUtility.IndexToMinuteDateTime(Kit.ToInt_yyyyMMdd(day), index);
                    Dictionary<string, MinuteSignal> signal = new Dictionary<string, MinuteSignal>();
                    DateTime next = new DateTime();
                    myAccount.time = now;
                    //int indexOfNow = data[targetVariety].FindIndex(s => s.time == now);
                    double nowClose = dataToday[targetVariety][index].close;
                    //����ӷ�ת��

                    double nowUpReversionPoint = ComputeReversionPoint02.findUpReversionPoint(data_5min, indexOf5min, NDays, lengthOfBackLooking);
                    double nowDownReversionPoint = ComputeReversionPoint02.findDownReversionPoint(data_5min, indexOf5min, NDays, lengthOfBackLooking);

                    if (nowDownReversionPoint < 0 || nowUpReversionPoint < 0)
                    {
                        index = nextIndex;
                        continue;
                    }



                    try
                    {
                        //�ֲֲ�ѯ����ƽ��
                        //����ǰ�гֲ� �� ����ƽ��
                        //�Ƿ��ǿղ�,��position������Ʒ��volum��Ϊ0����˵���ǿղ�     
                        bool isEmptyPosition = positions.Count != 0 ? positions[positions.Keys.Last()].Values.Sum(x => Math.Abs(x.volume)) == 0 : true;
                        //����ǰ�гֲ���������
                        if (!isEmptyPosition && closingOn)
                        {
                            ///ƽ������
                            /// ��1������ǰΪ �ز������ �� tradingOn Ϊfalse��ƽ��
                            /// ��2������ǰ�´��·�ת��*��1-���̶ȣ���ƽ��                    
                            //��1������ǰΪ �ز������ �� tradingOn Ϊfalse��ƽ��
                            if (isLastDayOfBackTesting || tradingOn == false)
                                next = MinuteCloseAllPositonsWithSlip.closeAllPositions(dataToday, ref positions, ref myAccount, now: now, slipPoint: slipPoint);
                            //��2������ǰ�´��·�ת��*��1-���̶ȣ���ƽ��
                            else
                            {
                                //�����·�ת��
                               

                                if (data[targetVariety][indexOfNow - 1].close >= nowDownReversionPoint * (1 - toleranceDegree) && nowClose < nowDownReversionPoint * (1 - toleranceDegree))
                                    next = MinuteCloseAllPositonsWithSlip.closeAllPositions(dataToday, ref positions, ref myAccount, now: now, slipPoint: slipPoint);
 
                            }
                        }
                        //�ղ� �ҿɽ��� �ɿ���
                        else if (isEmptyPosition && tradingOn && openingOn)
                        {
                            ///��������
                            /// �����ʽ��㹻���ҳ����Ϸ�ת�ź�
                            double nowFreeCash = myAccount.freeCash;
                            //���������������
                            double openVolume = Math.Truncate(nowFreeCash / data[targetVariety][indexOfNow].close / contractTimes) * contractTimes;
                            //��ʣ���ʽ����ٹ���һ�� �� ���Ϸ�ת�ź� ����
                            if (openVolume >= 1 && data[targetVariety][indexOfNow - 1].close <= nowUpReversionPoint * (1 + toleranceDegree) && nowClose > nowUpReversionPoint * (1 + toleranceDegree))
                            {
                                MinuteSignal openSignal = new MinuteSignal() { code = targetVariety, volume = openVolume, time = now, tradingVarieties = "stock", price = dataToday[targetVariety][index].close, minuteIndex = index };
                                signal.Add(targetVariety, openSignal);
                                next = MinuteTransactionWithSlip.computeMinuteOpenPositions(signal, dataToday, ref positions, ref myAccount, slipPoint: slipPoint, now: now);
                                //�������벻������
                                closingOn = false;
                            }
                        }

                        //�˻���Ϣ����
                        AccountUpdatingForMinute.computeAccountUpdating(ref myAccount, positions, now, dataToday);
                    }

                    catch (Exception)
                    {
                        throw;
                    }
                    nextIndex = Math.Max(nextIndex, TimeListUtility.MinuteToIndex(next));
                    index = nextIndex;
                }
                //�˻���Ϣ��¼By Day            
                //���ڼ�¼����ʱ�˻�
                BasicAccount tempAccount = new BasicAccount();
                tempAccount.time = myAccount.time;
                tempAccount.freeCash = myAccount.freeCash;
                tempAccount.margin = myAccount.margin;
                tempAccount.positionValue = myAccount.positionValue;
                tempAccount.totalAssets = myAccount.totalAssets;
                accountHistory.Add(tempAccount);
                //ץȡbenchmark
                benchmark.Add(dataToday[targetVariety].Last().close);

                //��ʾ��ǰ��Ϣ
                Console.WriteLine("Time:{0,-8:F},netWorth:{1,-8:F3}", day, myAccount.totalAssets / initialCapital);
            }

            //���������console   
            /*
            foreach (var account in accountHistory)
                Console.WriteLine("time:{0,-8:F}, netWorth:{1,-8:F3}\n", account.time, account.totalAssets / initialCapital);
             */
            //���Լ�Чͳ�Ƽ����
            PerformanceStatisics myStgStats = new PerformanceStatisics();
            myStgStats = PerformanceStatisicsUtils.compute(accountHistory, positions, benchmark.ToArray());

            //ͳ��ָ����console �����
            Console.WriteLine("--------Strategy Performance Statistics--------\n");
            Console.WriteLine(" netProfit:{0,-3:F} \n totalReturn:{1,-3:F} \n anualReturn:{2,-3:F} \n anualSharpe :{3,-3:F} \n winningRate:{4,-3:F} \n PnLRatio:{5,-3:F} \n maxDrawDown:{6,-3:F} \n maxProfitRatio:{7,-3:F} \n informationRatio:{8,-3:F} \n alpha:{9,-3:F} \n beta:{10,-3:F} \n averageHoldingRate:{11,-3:F} \n", myStgStats.netProfit, myStgStats.totalReturn, myStgStats.anualReturn, myStgStats.anualSharpe, myStgStats.winningRate, myStgStats.PnLRatio, myStgStats.maxDrawDown, myStgStats.maxProfitRatio, myStgStats.informationRatio, myStgStats.alpha, myStgStats.beta, myStgStats.averageHoldingRate);
            Console.WriteLine("-----------------------------------------------\n");

            //��ͼ
            Dictionary<string, double[]> line = new Dictionary<string, double[]>();
            double[] netWorth = accountHistory.Select(a => a.totalAssets / initialCapital).ToArray();
            line.Add("NetWorth", netWorth);

            //benchmark��ֵ
            List<double> netWorthOfBenchmark = benchmark.Select(x => x / benchmark[0]).ToList();
            line.Add("50ETF", netWorthOfBenchmark.ToArray());

            string[] datestr = accountHistory.Select(a => a.time.ToString("yyyyMMdd")).ToArray();
            Application.Run(new PLChart(line, datestr));
            /*
            //��accountHistory�����csv
            var resultPath = ConfigurationManager.AppSettings["CacheData.ResultPath"] + "accountHistory.csv";
            var dt = DataTableUtils.ToDataTable(accountHistory);          // List<MyModel> -> DataTable
            CsvFileUtils.WriteToCsvFile(resultPath, dt);	// DataTable -> CSV File
           */

            Console.ReadKey();
        }


    }
}
