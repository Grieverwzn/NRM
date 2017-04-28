using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using com.foxmail.wyyuan1991.NRM.Common;
using com.foxmail.wyyuan1991.NRM.RailwayModel;
using System.IO;
using com.foxmail.wyyuan1991.NRM.ALP;

namespace com.foxmail.wyyuan1991.NRM.Simulator
{
    /*BidPrice[time][sita,V]
    * 倒序与时间匹配
    */

    #region 删除
    /*
    public class BidPriceControl : IController
    {
        #region Variables
        private MDP.CD1_DW_Solver Solver = new MDP.CD1_DW_Solver();
        private DataAdapter m_DataAdapter;// = new DataAdapter();
        #endregion

        #region Attributes
        public DataAdapter DataAdapter
        {
            get
            {
                return m_DataAdapter;
            }

            set
            {
                m_DataAdapter = value;
            }
        }
        #endregion

        public void Init()
        {
            m_DataAdapter.GenDS();
            m_DataAdapter.InitialState = m_DataAdapter.CreateOrFind(m_DataAdapter.GenInitialState());
            //Solver.Data = m_DataAdapter;
            Solver.Tolerance = 1e-1;
            Solver.Init();
        }
        public void Reset()
        {
            m_DataAdapter.InitialState = m_DataAdapter.CreateOrFind(m_DataAdapter.GenInitialState());
            Solver.Reset();
        }
        public void SetOut(TextWriter tw)
        {
            Solver.SolverTextWriter = tw;
        }
        public List<IProduct> OpenProductList(int time, ResouceState r)
        {
            List<IProduct> res = new List<IProduct>();
            m_DataAdapter.InitialState = m_DataAdapter.CreateOrFind(r); //在生成状态空间之前指定初始化状态
            m_DataAdapter.GenSS2(r); 
            Solver.CurrentTime = time;
            Solver.DoCalculate();
            foreach (IProduct p in m_DataAdapter.ProSpace)
            {
                //Console.WriteLine("产品{0}\t价格{1}\t投标价格{2}", p.Description, p.Fare, p.Sum(i => Solver.DualValue1[time][m_DataAdapter.RS.IndexOf(i as Resource)]));
                if (r.CanSupportProduct(p) && p.Fare >= Math.Floor(p.Sum(i => Solver.BidPrice[time][m_DataAdapter.RS.IndexOf(i as Resource)])))
                { 
                    res.Add(p);
                }
            }
            return res;
        }
    }
    */
    #endregion 
    public class BidPriceController : IController
    {
        public void Init() { }
        public void Update() { }
        public void ReadFromTXT(string path)
        {
            List<double[]> res = new List<double[]>();
            string[] a;
            StreamReader sr = new StreamReader(path, Encoding.Default);
            for (int i = 0; sr.EndOfStream==false; i++)
            {
                string line = sr.ReadLine();
                a = line.Split(',');
                double[] d = new double[a.Length];
                for (int j = 0; j < a.Length; j++)
                {
                    d[j] =Convert.ToDouble(a[j]);
                }
                res.Add(d);
            }
            BidPrice = res.ToArray();
        }
        private NRMDataAdapter m_DataAdapter;
        public NRMDataAdapter DataAdapter
        {
            get
            {
                return m_DataAdapter;
            }
            set
            {
                m_DataAdapter = value;
            }
        }
        public double[][] BidPrice;//[时间][资源编号] 的投标价格
        public List<IProduct> OpenProductList(int time, MetaResouceState r, IConOL cl)
        {
            List<IProduct> res = new List<IProduct>();

            foreach (Product p in m_DataAdapter.ProSpace)
            {
                //Console.WriteLine("产品{0}\t价格{1}\t投标价格{2}", p.Description, p.Fare, p.Sum(i => Solver.DualValue1[time][m_DataAdapter.RS.IndexOf(i as Resource)]));
                if (r.CanSupportProduct(p) && p.Fare >= Math.Floor((p as List<Resource>).Sum(i => BidPrice[time2index(time)][1 + m_DataAdapter.RS.IndexOf(i as Resource)])))
                {
                    res.Add(p);
                }
            }
            return res;
        }
        public List<Product> OPL(int time, MetaResouceState r)
        {
            List<Product> res = new List<Product>();

            foreach (Product p in m_DataAdapter.ProSpace)
            {
                //Console.WriteLine("产品{0}\t价格{1}\t投标价格{2}", p.Description, p.Fare, p.Sum(i => Solver.DualValue1[time][m_DataAdapter.RS.IndexOf(i as Resource)]));
                if (r.CanSupportProduct(p) && p.Fare >= Math.Floor((p as List<Resource>).Sum(i => BidPrice[time2index(time)][1 + m_DataAdapter.RS.IndexOf(i as Resource)])))
                {
                    res.Add(p);
                }
            }
            return res;
        }

        //将当前时间转为loopuptable里面的索引
        private int time2index(int time)
        {
            int t = BidPrice.Length - (DataAdapter.TimeHorizon - time);
            return Math.Max(0, t);

        }
        public double loadFactor()
        {
           
            double a = 0; double b = 0; double c = 0;
            MetaResouceState rs = new MetaResouceState(DataAdapter.InitState);
            //List<Product> openProductList; //= DataAdapter.ProSpace;

            HashSet<IProduct> _ss=new HashSet<IProduct>() ;
            Decision d;

            //int alpha = DataAdapter.TimeHorizon - BidPrice.Length;
            for(int interval = 0; interval < DataAdapter.MarketInfo.AggRo.Count; interval++)
            {
                //if (DataAdapter.MarketInfo.AggRo[interval].Time <= alpha)
                //{
                    int time = DataAdapter.MarketInfo.AggRo[interval].Time;
                    //if (DataAdapter.MarketInfo.AggRo[interval + 1].Time <= alpha)
                    //{
                   int len =0;
                   if(interval < DataAdapter.MarketInfo.AggRo.Count-1)
                   {
                        len = DataAdapter.MarketInfo.AggRo[interval + 1].Time - DataAdapter.MarketInfo.AggRo[interval].Time;
                   }else{
                        len = DataAdapter.TimeHorizon - DataAdapter.MarketInfo.AggRo[interval].Time;
                   }
                    //}
                    //else
                    //{
                    //    len = alpha - DataAdapter.MarketInfo.AggRo[interval].Time;
                    //}
                   //openProductList = CG(time, rs);//this.OPL(time, rs);new HashSet<Product>(openProductList);
                    _ss = CG(time, rs); 
                    d = new Decision(_ss);
                    c = (DataAdapter.ResSpace as IALPResourceSpace).Sum(i => DataAdapter.Qti(time, i, d));
                    a += c * len;
                    //Console.WriteLine("Interval={0},a={1}", interval, a);
                //}
                //else
                //{
                //    break;
                //}
            }

            //for (int t = alpha + 1; t < DataAdapter.TimeHorizon; t++)
            //{
            //    //openProductList = this.OPL(t, rs);
            //    if (findAggRo(t, alpha) == findAggRo(t - 1, alpha))
            //    {
            //        //TODO: 比较openProduct
            //         //HashSet<Product> temp =new HashSet<Product>(openProductList);
            //         //int count = _ss.Except(temp).ToList().Count;//比较集合相等性
            //         //int count2 = temp.Except(_ss).ToList().Count;//比较集合相等性
            //         //if (count == 0 && count2==0)
            //         //{
            //             _ss = new HashSet<Product>(openProductList);
            //             a += c;
            //         //    continue;
            //         //}
            //    }

            //    //openProductList = this.OPL(t, rs);
            //    _ss = new HashSet<Product>(openProductList);
            //    d = new Decision(_ss);
            //    c =  DataAdapter.ResSpace.AsParallel().Sum(i => DataAdapter.Qti(t, i, d));
            //    a+=c;

            //Console.WriteLine("t={0},a={1}" , t , a);

            //TODO将b修改正确
            b = 1;
            //b= DataAdapter.ResSpace.Sum(i => i.Capacity);
            return a / b;
        }
        #region Time/Interval Convertor
        private int findAggRo(int t)
        {
            if (DataAdapter.MarketInfo.AggRo.Count == 1) return 0;
            for (int i = 1; i < DataAdapter.MarketInfo.AggRo.Count; i++)
            {
                if (t < DataAdapter.MarketInfo.AggRo[i].Time) return i - 1;
            }
            return DataAdapter.MarketInfo.AggRo.Count - 1;
        }
        private int last(int i,int alpha)
        {
            if (i < DataAdapter.MarketInfo.AggRo.Count - 1)
            {
                if (DataAdapter.MarketInfo.AggRo[i].Time <= alpha && DataAdapter.MarketInfo.AggRo[i + 1].Time <= alpha)
                {
                    return DataAdapter.MarketInfo.AggRo[i + 1].Time - DataAdapter.MarketInfo.AggRo[i].Time;
                }
                else if (DataAdapter.MarketInfo.AggRo[i].Time <= alpha && DataAdapter.MarketInfo.AggRo[i + 1].Time > alpha)
                {
                    return alpha - DataAdapter.MarketInfo.AggRo[i].Time + 1;
                }
                else
                {
                    return 0;
                }
            }
            else
            {
                if (DataAdapter.MarketInfo.AggRo[i].Time <= alpha)
                {
                    return alpha - DataAdapter.MarketInfo.AggRo[i].Time + 1;
                }
                else
                {
                    return 0;
                }
            }
        }
        #endregion 
   
        private HashSet<IProduct> CG(int t, MetaResouceState r)
        {
            #region  贪婪算法求解组合优化问题
            List<IProduct> list = new List<IProduct>(DataAdapter.ProSpace);//优先关闭机会成本低的产品
            List<IProduct> tempDecision = new List<IProduct>();//开放产品集合
            //加入第一个元素
            IProduct tempProduct = null;
            double temp = computeValue(t, tempDecision, tempProduct);

            foreach (IProduct p in list)
            {
                if (temp < computeValue(t, tempDecision, p))
                {
                    temp = computeValue(t, tempDecision, p);
                    tempProduct = p;
                }
            }
            //tempProduct = list.Where(p =>computeValue(t, tempDecision, p) > temp).OrderBy(p => computeValue(t, tempDecision, p)).FirstOrDefault();

            if (tempProduct != null)
            {
                tempDecision.Add(tempProduct);
                list.Remove(tempProduct);
            }
            for (; list.Count > 0; )
            {
                temp = computeValue(t, tempDecision, null);
                double temp2 = temp;
                IProduct tempPro = null;
                foreach (IProduct p in list)
                {
                    double temp1 = computeValue(t, tempDecision, p);
                    if (temp2 < temp1)
                    {
                        temp2 = temp1;
                        tempPro = p;
                    }
                }

                //tempPro = list.OrderBy(p => computeValue(t, tempDecision, p)).FirstOrDefault();
                //temp2 = computeValue(t, tempDecision, tempPro);

                if (temp2 > temp)
                {
                    tempDecision.Add(tempPro);
                    list.Remove(tempPro);
                }
                else
                {
                    break;
                }
            }

            #endregion

            //从u生成decision
            HashSet<IProduct> tempset = new HashSet<IProduct>(tempDecision);

            return tempset;
        }
        private double computeValue(int t, List<IProduct> tempDecision, IProduct p)
        {
             HashSet<IProduct> temp = new HashSet<IProduct>(tempDecision);
             temp.Add(p);
             Decision d = new Decision(temp);
             return DataAdapter.Rt(t, d);
        }

        public List<Ticket> PrintTickets(MetaResouceState rs, List<IProduct> pro,IConOL cl)
        {
            List<Ticket> res = new List<Ticket>();
            foreach (IProduct p in pro)//一张一张卖
            {
                int n = p.Min(i => i.MetaResList.Count);
                for(int i = 0; i < n; i++)
                {
                    if (p.All(j => !rs.MetaResDic[j.MetaResList[i]]))
                    {
                        Ticket t = new Ticket() { Product = p };
                        foreach(IResource r in p)
                        {
                            t.MetaResList.Add(r.MetaResList[i]);
                        }
                        res.Add(t);
                        break;
                    }
                }                 
            }
            return res;
        }

        public IConOL GenConOL()
        {
            return null;
        }
    }
}
