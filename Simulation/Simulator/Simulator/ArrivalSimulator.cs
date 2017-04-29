using com.foxmail.wyyuan1991.NRM.Common;
using MathNet.Numerics.Random;
using System;
using System.Collections.Generic;

namespace com.foxmail.wyyuan1991.NRM.Simulator
{
    /// <summary>
    /// 旅客到达模拟
    /// </summary>
    public class ArrivalSimulator
    {
        Random rng = SystemRandomSource.Default;//随机数发生器

        public IMarket MarketInfo { get; set; }
        public int TimeHorizon { get; set; }
        public double MaxLamada { get; set; }
        //public TimeFunction lamada { get; set; }

        

        /// <summary>
        /// 稀疏法（包云phD）
        /// </summary>
        /// <returns></returns>
        private List<int> GenerateLine()
        {
            List<int> list = new List<int>();
            for (int t = 0; t < TimeHorizon;)
            {
                double u1 = rng.NextDouble();
                double u2 = rng.NextDouble();
                t = (int)Math.Ceiling(t - (1 / MaxLamada) * Math.Log(u1));//这里向上取整。
                if ((u2 <= (MarketInfo.Ro(t) / MaxLamada))&& t < TimeHorizon)
                {
                    list.Add(t);
                }
            }
            return list;
        }

        private IMarketSegment rollMS(int time,double u)
        {
            double x = 0; int i = 0;
            //double u_modified = u * MarketInfo.Sum(m =>m.Lamada(time));
            foreach (IMarketSegment ms in MarketInfo)
            {
                x+=ms.Lamada(time);
                if (x >= u)
                {
                    return ms;
                }
                i++;
            }
            return null;// MarketInfo[i];
        }
        //public PrimalArrivalList Gen(int id)
        //{
        //    PrimalArrivalList arr = new PrimalArrivalList() { PAListID = id };
        //    for (int t = 0; t < TimeHorizon;)
        //    {
        //        double u1 = rng.NextDouble();
        //        double u2 = rng.NextDouble();
        //        t = (int)Math.Ceiling(t - (1 / MaxLamada) * Math.Log(u1));//这里向上取整，可能会影响结果。
        //        if ((u2 <= (MarketInfo.Ro(t) / MaxLamada)) && t < TimeHorizon)
        //        {
        //            double u3 = rng.NextDouble();
        //            PrimalArrival temp = new PrimalArrival()
        //            {
        //                ArriveTime = t,
        //                IndexOfMS = rollMS(t, u3).MSID
        //            };
        //            arr.Add(temp);
        //        }
        //    }
        //    return arr;
        //}
        public PrimalArrivalList Gen(int id)
        {
            PrimalArrivalList arr = new PrimalArrivalList() { PAListID = id,TimeHorizon= TimeHorizon };
            for (int t = 0; t < TimeHorizon; t++)
            {
                double u1 = rng.NextDouble();
                double u2 = rng.NextDouble();
                double u3 = rng.NextDouble();
                //t = (int)Math.Ceiling(t - (1 / MaxLamada) * Math.Log(u1));//这里向上取整，可能会影响结果。
                //if ((u2 <= (MarketInfo.Ro(t) / MaxLamada)) && t < TimeHorizon)
                if (u1<MarketInfo.Ro(t))
                {
                    //double u3 = rng.NextDouble();
                    PrimalArrival temp = new PrimalArrival()
                    {
                        ArriveTime = t,
                        IndexOfMS = rollMS(t, u2).MSID,
                        ChoosingParam=u3
                    };
                    arr.Add(temp);
                }
            }
            return arr;
        }
    }
}
