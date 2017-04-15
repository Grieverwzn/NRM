using System;
using com.foxmail.wyyuan1991.NRM.Test;

namespace com.foxmail.wyyuan1991.NRM.Simulator
{
    public static class test
    {
        public static void Main(string[] args)
        {

            AirlineDataGenerator dg = new AirlineDataGenerator();
            dg.GenData1();

            //RailDataGenerator dg = new RailDataGenerator();
            //dg.GenDataWithCT

            #region 生成到达
            PrimalArrivalData pad = new PrimalArrivalData();//到达数据
            //到达序列生成器
            ArrivalSimulator arr = new ArrivalSimulator()
            {
                MaxLamada = 0.3,
                TimeHorizon = 1000,
                MarketInfo = dg.market
            };
            for (int i = 1; i <= 2000; i++)
            {
                pad.Data.Add(arr.Gen(i));
            }
            pad.SaveToXml("D:\\arrtest.txt");
            #endregion

            //初始化
            BookingSimulator sim = new BookingSimulator()
            {
                MarketInfo = dg.market,
                ResourceSpace = dg.RS,
                ControlStrategy = new OpenAllStrategy() { proSpace = dg.proSpace }
            };

            SellingRecordList sr;//售票记录
            ControlRecordList cr;//控制记录
            pad.LoadFromXml("D:\\arrtest.txt");

            foreach (PrimalArrivalList res in pad.Data)
            {
                sim.Process(res, out sr, out cr);
                foreach (var a in sr)
                {
                    Console.WriteLine(a.ToString());
                }
            }
            Console.WriteLine("Press <Enter> to Exit");
            Console.ReadLine();
        }
    }
}
