using com.foxmail.wyyuan1991.NRM.RailwayModel;
using System.Collections.Generic;
using System.Linq;
using com.foxmail.wyyuan1991.NRM.ALP;
using com.foxmail.wyyuan1991.NRMSolver;

namespace com.foxmail.wyyuan1991.NRM.RailwaySolver
{
    public class RailwayNRMSolver_CLP_v2 : CLP1_Solver
    {
        private NRMDataAdapter _Data;
        DecisionSpace _ds = new DecisionSpace();

        public new NRMDataAdapter Data
        {
            get
            {
                return _Data;
            }
            set
            {
                base.Data = value as IALPFTMDP;
                _Data = value;
            }
        }

        protected override void CreateFeasibleSolution()
        {

        }
        public override void DoCalculate()//Calculate the Bid-Price of current time
        {
            bool IsOptimal = true;
            double tempObj = 0;
            int tol = ObeyTime;

            for (int iter = 1; ; iter++)
            {
                IsOptimal = true;
                if (RMPModel.Solve())
                {
                    #region 判断是否终止
                    if (RMPModel.GetObjValue() - tempObj < threshold)
                    {
                        tol--;
                    }
                    else
                    {
                        tol = ObeyTime;
                    }
                    tempObj = RMPModel.GetObjValue();
                    #endregion
                }
                UpdateValues();

                #region 判断是否终止
                if (tol < 0)
                {
                    print("--------{0}算法达到终止条件而退出--------", System.DateTime.Now.ToLongTimeString());
                    UpdateBidPrice();
                    break;
                }
                #endregion

                for (int t = CurrentTime; t < Data.TimeHorizon; t++)
                {
                    IALPDecision deci_a = null;
                    bool IsSubOpt = CG(t, out deci_a);
                    if (!IsSubOpt)
                    {
                        AddConstraint(t, deci_a);
                    }
                    IsOptimal = IsSubOpt && IsOptimal;
                }

                //for (int iteration = CurrentTime; iteration < Data.TimeHorizon; iteration++)
                //{
                //    int t = iteration;
                //    Task ta = factory.StartNew(() =>
                //    {
                //        IALPDecision deci_a = null;
                //        bool IsSubOpt = CG(t, out deci_a);
                //        if (!IsSubOpt)
                //        {
                //            lock (RMPModel)
                //            {
                //                AddConstraint(t, deci_a);
                //            }
                //        }
                //        IsOptimal = IsSubOpt && IsOptimal;
                //        //print("#{0}第{1}个子问题已经解决！", System.DateTime.Now.ToLongTimeString(), iteration);
                //    }, cts.Token);
                //    tasks.Add(ta);
                //}
                //Task.WaitAll(tasks.ToArray());
                //tasks.Clear();

                if (IsOptimal)
                {
                    print("--------已经达到最优！", System.DateTime.Now.ToLongTimeString());
                    UpdateValues();
                    UpdateBidPrice();
                    break;
                }
            }
            SendEvent(new IterationCompletedEventArgs()
            {
                BidPrice = this.BidPrice,
                ObjValue = tempObj,
                TurnningPoint = findturnningpoint()
            });
        }
       
        protected override bool CG(int t, out IALPDecision deci_a)
        {
            deci_a = null;

            #region  贪婪算法求解组合优化问题
            List<Product> list = Data.ProSpace.Where(i =>
            {
                double w = bidprice(t, i);
                if (i.Fare < w)
                {
                    return false;
                }
                else
                {
                    return true;
                }
            }).ToList();//优先关闭机会成本低的产品
            List<Product> tempDecision = new List<Product>();//开放产品集合
            //加入第一个元素
            Product tempProduct = null;
            double temp = computeValue(t, tempDecision, tempProduct);
            foreach (Product p in list)
            {
                if (temp < computeValue(t, tempDecision, p))
                {
                    temp = computeValue(t, tempDecision, p);
                    tempProduct = p;
                }
            }
            if (tempProduct != null)
            {
                tempDecision.Add(tempProduct);
                list.Remove(tempProduct);
            }
            for (; list.Count > 0; )
            {
                temp = computeValue(t, tempDecision, null);
                double temp2 = temp;
                Product tempPro = null;
                foreach (Product p in list)
                {
                    double temp1 = computeValue(t, tempDecision, p);
                    if (temp2 < temp1)
                    {
                        temp2 = temp1;
                        tempPro = p;
                    }
                }
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
            Decision d = new Decision();
            d.OpenProductSet.UnionWith(tempDecision);
            lock (_ds)
            {
                deci_a = _ds.FirstOrDefault(k => (k as Decision).Equals(d)) as IALPDecision;
                if (deci_a == null) { _ds.Add(d); deci_a = d; }
            }
            if (computeValue(t, tempDecision, null) < Tolerance || constraints[t].ContainsKey(deci_a))
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        private double bidprice(int t, Route route)
        {
            double w = 0;
            foreach (Resource r in Data.RS)
            {
                if (route.Exists(i => i.Contains(r)))
                {
                    w += V[t][Data.RS.IndexOf(r)];
                }
            }

            return w;
        }
        private double bidprice(int t, Product i)
        {
            double w = 0;
            foreach (Resource r in Data.RS)
            {
                if (i.Contains(r))
                {
                    w += V[t][Data.RS.IndexOf(r)];
                }
            }

            return w;
        }
        private double computeValue(int t, List<Route> list, Route tp)
        {
            double v = 0;
            foreach (MarketSegment l in Data.MarketInfo)
            {
                double a = 0; double b = l.Retreat;
                foreach (Route r in l.ConsiderationDic.Keys)
                {
                    if (list.Contains(r) || tp == r)
                    {
                        a += l.ConsiderationDic[r] * (r.TicketPrice - bidprice(t, r));
                        b += l.ConsiderationDic[r];
                    }
                }
                v += Data.MarketInfo.Ro(t) * l.Lamada(t) * a / b;
            }
            if (t < Data.TimeHorizon - 1)
            {
                foreach (Resource r in Data.RS)
                {
                    if (list.Exists(i => i.Exists(x => x.Contains(r))) || (tp != null && tp.Exists(x => x.Contains(r))))
                    {
                        v -= V[t][Data.RS.IndexOf(r)] - V[t + 1][Data.RS.IndexOf(r)];
                    }
                }
                v -= Sita[t] - Sita[t + 1];
            }
            else
            {
                foreach (Resource r in Data.RS)
                {
                    if (list.Exists(i => i.Exists(x => x.Contains(r))) || (tp != null && tp.Exists(x => x.Contains(r))))
                    {
                        v -= V[t][Data.RS.IndexOf(r)];
                    }
                }
                v -= Sita[t];
            }
            return v;
        }
        private double computeValue(int t, List<Product> list, Product tp)
        {
            double v = 0;
            foreach (MarketSegment l in Data.MarketInfo)
            {
                double a = 0; double b = l.Retreat;
                foreach (Route r in l.ConsiderationDic.Keys)
                {
                    if (r.TrueForAll(i => list.Contains(i)) || (r.Except(list).Count() == 1 && r.Except(list).First() == tp))
                    {
                        a += l.ConsiderationDic[r] * (r.TicketPrice - bidprice(t, r));
                        b += l.ConsiderationDic[r];
                    }
                }
                v += Data.MarketInfo.Ro(t) * l.Lamada(t) * a / b;
            }
            if (t < Data.TimeHorizon - 1)
            {
                foreach (Resource r in Data.RS)
                {
                    if (list.Exists(i => i.Contains(r)) || (tp != null && tp.Contains(r)))
                    {
                        v -= V[t][Data.RS.IndexOf(r)] - V[t + 1][Data.RS.IndexOf(r)];
                    }
                }
                v -= Sita[t] - Sita[t + 1];
            }
            else
            {
                foreach (Resource r in Data.RS)
                {
                    if (list.Exists(i => i.Contains(r)) || (tp != null && tp.Contains(r)))
                    {
                        v -= V[t][Data.RS.IndexOf(r)];
                    }
                }
                v -= Sita[t];
            }
            return v;
        }

        protected override void SA()
        {
            for (int t = CurrentTime; t < Data.TimeHorizon; t++)
            {
                int i = 0;
                var b = RMPModel.GetSlacks(constraints[t].Values.ToArray());
                string s = "";
                foreach (var c in constraints[t])
                {
                    if (RMPModel.GetSlack(c.Value) == 0)
                    {
                        s += _ds.IndexOf(c.Key) + ",";
                    }
                }
                foreach (var a in b)
                {
                    if (a == 0) i++;
                }
                #region w小于0的路径
                string ss = "";
                foreach (Route h in Data.pathList)
                {
                    double temp = h.TicketPrice;
                    foreach (Resource r in Data.RS)
                    {
                        if (h.Exists(k => k.Contains(r)))
                        {
                            temp -= V[t][Data.RS.IndexOf(r)];
                        }
                    }
                    if (temp > 0) ss += Data.pathList.IndexOf(h) + ",";
                }
                #endregion
                print("Time:{0},Number of Bounded Constraints:{1}/{2} | {3} | {4}", t, i, b.Length, s, ss);
            }
        }
    }
}
