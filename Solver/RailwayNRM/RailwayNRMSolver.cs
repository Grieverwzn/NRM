using com.foxmail.wyyuan1991.NRM.RailwayModel;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using com.foxmail.wyyuan1991.NRM.ALP;
using com.foxmail.wyyuan1991.NRMSolver;
using ILOG.Concert;

namespace com.foxmail.wyyuan1991.NRM.RailwaySolver
{
    //public class RailwayNRMSolver_CD3 : CD3_DW_Solver
    //{
    //    private DataAdapter _Data;
    //    public new DataAdapter Data
    //    {
    //        get
    //        {
    //            return _Data;
    //        }
    //        set
    //        {
    //            base.Data = value as IALPDataAdapter;
    //            _Data = value;
    //        }
    //    }

    //    DecisionSpace _ds = new DecisionSpace();

    //    protected override void CreateFeasibleSolution()
    //    {
    //        _ds.Clear();
    //        _ds.Add(new Decision());
    //        IALPDecision d = (_ds as IALPDecisionSpace).CloseAllDecision() as IALPDecision;
    //        for (int t = CurrentTime; t < Data.TimeHorizon; t++)
    //        {
    //            AddCol(t, d);
    //        }
    //    }

    //    /*
    //    public override void DoCalculate()//Calculate the Bid-Price of current time
    //    {
    //        CreateFeasibleSolution();
    //        bool IsOptimal = true;
    //        for (int iter = 1; ; iter++)
    //        {
    //            IsOptimal = true;
    //            if (RMPModel.Solve())
    //            {
    //                UpdateDualValues();
    //            }
    //            for (int t = CurrentTime; t < Data.TimeHorizon; t++)
    //            {
    //                int iteration = t;
    //                Task ta = factory.StartNew(() =>
    //                {
    //                    IALPDecision deci_a = null;
    //                    bool IsSubOpt = CG(iteration, out deci_a);
    //                    if (!IsSubOpt)
    //                    {
    //                        lock (_ds)
    //                        {
    //                            AddCol(iteration, deci_a);
    //                        }
    //                    }
    //                    IsOptimal = IsSubOpt && IsOptimal;
    //                }, cts.Token);
    //                tasks.Add(ta);
    //            }
    //            Task.WaitAll(tasks.ToArray());
    //            tasks.Clear();
    //            ///
    //            if (IsOptimal)
    //            {
    //                print("--------已经达到最优！", System.DateTime.Now.ToLongTimeString());
    //                UpdateDualValues();
    //                UpdateBidPrice();
    //                //SA();
    //                break;
    //            }
    //        }
    //    }
    //    */
    //    protected override bool CG(int t, out IALPDecision deci_a)
    //    {
    //        deci_a = null;
    //        CP cp = new CP();
    //        INumExpr cost = cp.NumExpr();

    //        IIntVar[] u = cp.IntVarArray(Data.ProSpace.Count, 0, 1);
    //        IIntVar[] y = cp.IntVarArray(Data.pathList.Count, 0, 1);
    //        IIntVar[] z = cp.IntVarArray(Data.RS.Count, 0, 1);

    //        #region 生成目标函数
    //        foreach (MarketSegment ms in Data.MarketInfo)
    //        {
    //            INumExpr a = cp.NumExpr();
    //            INumExpr b = cp.NumExpr();
    //            foreach (Route h in ms.ConsiderationDic.Keys)
    //            {
    //                #region  计算wh
    //                double w = 0;
    //                w += h.TicketPrice;
    //                foreach (Resource r in Data.RS)
    //                {
    //                    if (h.Find(i => i.Contains(r)) != null)
    //                    {
    //                        for (int k = t + 1; k < Data.TimeHorizon; k++)
    //                        {
    //                            w -= DualValue1[k][Data.RS.IndexOf(r)];
    //                        }
    //                    }
    //                }
    //                #endregion

    //                a = cp.Sum(a, cp.Prod(w * ms.ConsiderationDic[h], y[Data.pathList.IndexOf(h)]));
    //                b = cp.Sum(b, cp.Prod(ms.ConsiderationDic[h], y[Data.pathList.IndexOf(h)]));
    //            }
    //            cost = cp.Sum(cost, cp.Prod(cp.Quot(a, cp.Sum(b, ms.Retreat)), Data.MarketInfo.Ro(t) * ms.Lamada(t)));
    //        }
    //        foreach (Resource r in Data.RS)
    //        {
    //            cost = cp.Sum(cost, cp.Prod(-DualValue1[t][Data.RS.IndexOf(r)], z[Data.RS.IndexOf(r)]));
    //        }
    //        cost = cp.Sum(cost, -DualValue2[t]);
    //        #endregion

    //        cp.Add(cp.Maximize(cost));

    //        #region y-u约束
    //        foreach (Route r in Data.pathList)
    //        {
    //            INumExpr con1 = cp.NumExpr();
    //            foreach (Product p in r)
    //            {
    //                con1 = cp.Sum(con1, u[Data.ProSpace.IndexOf(p)]);
    //                INumExpr con2 = cp.NumExpr();
    //                con2 = cp.Sum(cp.Prod(-1, u[Data.ProSpace.IndexOf(p)]), y[Data.pathList.IndexOf(r)]);
    //                cp.AddLe(con2, 0);
    //            }
    //            con1 = cp.Sum(con1, cp.Prod(-1, y[Data.pathList.IndexOf(r)]));
    //            con1 = cp.Sum(con1, 1 - r.Count);
    //            cp.AddLe(con1, 0);
    //        }
    //        #endregion

    //        #region z-u约束
    //        foreach (Resource r in Data.RS)
    //        {
    //            INumExpr con3 = cp.NumExpr();
    //            foreach (Product p in Data.ProSpace)
    //            {
    //                if (p.Contains(r))
    //                {
    //                    con3 = cp.Sum(con3, u[Data.ProSpace.IndexOf(p)]);
    //                    INumExpr con4 = cp.NumExpr();
    //                    con4 = cp.Sum(z[Data.RS.IndexOf(r)], cp.Prod(-1, u[Data.ProSpace.IndexOf(p)]));
    //                    cp.AddGe(con4, 0);
    //                }
    //            }
    //            con3 = cp.Sum(con3, cp.Prod(-1, z[Data.RS.IndexOf(r)]));
    //            cp.AddGe(con3, 0);
    //        }
    //        #endregion

    //        cp.SetOut(null);

    //        cp.Solve();
    //        if (cp.ObjValue < Tolerance)
    //        {
    //            cp.End();
    //            return true;
    //        }
    //        else
    //        {
    //            double[] temp = new double[Data.ProSpace.Count];
    //            cp.GetValues(u, temp);
    //            //todo 从u生成decision
    //            Decision d = new Decision();
    //            for (int i = 0; i < Data.ProSpace.Count; i++)
    //            {
    //                if (temp[i] == 1)
    //                {
    //                    d.OpenProductSet.Add(Data.ProSpace[i]);
    //                }
    //            }
    //            deci_a = _ds.FirstOrDefault(k => (k as Decision).Equals(d)) as IALPDecision;
    //            if (deci_a == null) { _ds.Add(d); deci_a = d; }
    //            cp.End();
    //            return false;
    //        }
    //    }
    //}
    public class RailwayNRMSolver_CLP : CLP1_Solver
    {
        private NRMDataAdapter _Data;
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

        public override void DoCalculate()//Calculate the Bid-Price of current time
        {
            CreateFeasibleSolution();
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

                //for (int t = CurrentTime; t < Data.TimeHorizon; t++)
                //{
                //    IALPDecision deci_a = null;
                //    bool IsSubOpt = CG(t, out deci_a);
                //    if (!IsSubOpt)
                //    {
                //        AddConstraint(t, deci_a);
                //    }
                //    IsOptimal = IsSubOpt && IsOptimal;
                //}

                for (int iteration = CurrentTime; iteration < Data.TimeHorizon; iteration++)
                {
                    int t = iteration;
                    Task ta = factory.StartNew(() =>
                    {
                        IALPDecision deci_a = null;
                        bool IsSubOpt = CG(t, out deci_a);
                        if (!IsSubOpt)
                        {
                            lock (RMPModel)
                            {
                                AddConstraint(t, deci_a);
                            }
                        }
                        IsOptimal = IsSubOpt && IsOptimal;
                        //print("#{0}第{1}个子问题已经解决！", System.DateTime.Now.ToLongTimeString(), iteration);
                    }, cts.Token);
                    tasks.Add(ta);
                }
                Task.WaitAll(tasks.ToArray());
                tasks.Clear();

                if (IsOptimal)
                {
                    print("--------已经达到最优！", System.DateTime.Now.ToLongTimeString());
                    UpdateValues();
                    UpdateBidPrice();
                    SA();
                    break;
                }
            }
        }

        DecisionSpace _ds = new DecisionSpace();

        protected override void CreateFeasibleSolution()
        {

        }
        protected override bool CG(int t, out IALPDecision deci_a)
        {
            deci_a = null;


            //foreach (OD i in Data.MarketInfo.ODList)
            //{
            //    List<MarketSegment> li = Data.MarketInfo.getMSbyOD(i);
            //    List<Route> Routeli = Data.pathList.Where(x => x.StartStation == i.OriSta && x.EndStation == i.DesSta).ToList();
            //}

            #region 得到 Route List
            List<Route> list = Data.RouteList.Where(i =>
            {
                double w = bidprice(t, i);
                if (i.TicketPrice < w)
                {
                    return false;
                }
                else
                {
                    return true;
                }
            }).ToList();
            List<Route> tempRoutelist = new List<Route>();
            double temp = 0;
            Route tempRoute = null;
            foreach (Route r in list)
            {
                if (temp < computeValue(t, tempRoutelist, r))
                {
                    temp = computeValue(t, tempRoutelist, r);
                    tempRoute = r;
                }
            }
            if (tempRoute != null)
            {
                tempRoutelist.Add(tempRoute);
            }
            list.Remove(tempRoute);
            foreach (Route r in list)
            {
                if (temp < computeValue(t, tempRoutelist, r))
                {
                    tempRoutelist.Add(r);
                }
            }
            #endregion

            List<Product> templist = new List<Product>();

            #region 找到一组u
            foreach (Route r in tempRoutelist)
            {
                foreach (Product p in r)
                {
                    if (!templist.Contains(p)) { templist.Add(p); }
                }
            }
            #endregion

            /*
               #region 找到一个u
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
               }).ToList();
               List<Product> templist = new List<Product>();
               double temp = 0;
               Product tempProduct = null;
               foreach (Product p in list)
               {
                   if (temp < computeValue(t, templist, p))
                   {
                       temp = computeValue(t, templist, p);
                       tempProduct = p;
                   }
               }
               if (tempProduct != null)
               {
                   templist.Add(tempProduct);
               }
               list.Remove(tempProduct);
               foreach (Product p in list)
               {
                   if (temp < computeValue(t, templist, p))
                   {
                       templist.Add(p);
                   }
               }

               #endregion
       */
            //从u生成decision
            Decision d = new Decision();
            d.OpenProductSet.UnionWith(templist);
            deci_a = constraints[t].Keys.FirstOrDefault(k => (k as Decision).Equals(d)) as IALPDecision;
            if (deci_a == null) { _ds.Add(d); deci_a = d; }
            if (computeValue(t, templist, null) < Tolerance || constraints[t].ContainsKey(deci_a))
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
        //protected override bool CG(int t, out IALPDecision deci_a)
        //{
        //    deci_a = null;
        //    CP cp = new CP();
        //    INumExpr cost = cp.NumExpr();

        //    IIntVar[] u = cp.IntVarArray(Data.ProSpace.Count, 0, 1);
        //    IIntVar[] y = cp.IntVarArray(Data.pathList.Count, 0, 1);
        //    IIntVar[] z = cp.IntVarArray(Data.RS.Count, 0, 1);

        //    #region 生成目标函数
        //    foreach (MarketSegment ms in Data.MarketInfo)
        //    {
        //        INumExpr a = cp.NumExpr();
        //        INumExpr b = cp.NumExpr();
        //        foreach (Route h in ms.ConsiderationDic.Keys)
        //        {
        //            #region  计算wh
        //            double w = 0;
        //            w += h.TicketPrice;
        //            foreach (Resource r in Data.RS)
        //            {
        //                if (h.Find(i => i.Contains(r)) != null)
        //                {
        //                    for (int k = t + 1; k < Data.TimeHorizon; k++)
        //                    {
        //                        w -= V[k][Data.RS.IndexOf(r)];
        //                    }
        //                }
        //            }
        //            #endregion

        //            a = cp.Sum(a, cp.Prod(w * ms.ConsiderationDic[h], y[Data.pathList.IndexOf(h)]));
        //            b = cp.Sum(b, cp.Prod(ms.ConsiderationDic[h], y[Data.pathList.IndexOf(h)]));
        //        }
        //        cost = cp.Sum(cost, cp.Prod(cp.Quot(a, cp.Sum(b, ms.Retreat)), Data.MarketInfo.Ro(t) * ms.Lamada(t)));
        //    }
        //    foreach (Resource r in Data.RS)
        //    {
        //        cost = cp.Sum(cost, cp.Prod(-V[t][Data.RS.IndexOf(r)], z[Data.RS.IndexOf(r)]));
        //    }
        //    cost = cp.Sum(cost, -Sita[t]);
        //    #endregion

        //    cp.Add(cp.Maximize(cost));

        //    #region y-u约束
        //    foreach (Route r in Data.pathList)
        //    {
        //        INumExpr con1 = cp.NumExpr();
        //        foreach (Product p in r)
        //        {
        //            con1 = cp.Sum(con1, u[Data.ProSpace.IndexOf(p)]);
        //            INumExpr con2 = cp.NumExpr();
        //            con2 = cp.Sum(cp.Prod(-1, u[Data.ProSpace.IndexOf(p)]), y[Data.pathList.IndexOf(r)]);
        //            cp.AddLe(con2, 0);
        //        }
        //        con1 = cp.Sum(con1, cp.Prod(-1, y[Data.pathList.IndexOf(r)]));
        //        con1 = cp.Sum(con1, 1 - r.Count);
        //        cp.AddLe(con1, 0);
        //    }
        //    #endregion

        //    #region z-u约束
        //    foreach (Resource r in Data.RS)
        //    {
        //        INumExpr con3 = cp.NumExpr();
        //        foreach (Product p in Data.ProSpace)
        //        {
        //            if (p.Contains(r))
        //            {
        //                con3 = cp.Sum(con3, u[Data.ProSpace.IndexOf(p)]);
        //                INumExpr con4 = cp.NumExpr();
        //                con4 = cp.Sum(z[Data.RS.IndexOf(r)], cp.Prod(-1, u[Data.ProSpace.IndexOf(p)]));
        //                cp.AddGe(con4, 0);
        //            }
        //        }
        //        con3 = cp.Sum(con3, cp.Prod(-1, z[Data.RS.IndexOf(r)]));
        //        cp.AddGe(con3, 0);
        //    }
        //    #endregion

        //    cp.SetOut(null);

        //    cp.Solve();
        //    if (cp.ObjValue < Tolerance)
        //    {
        //        cp.End();
        //        return true;
        //    }
        //    else
        //    {
        //        double[] temp = new double[Data.ProSpace.Count];
        //        cp.GetValues(u, temp);
        //        //从u生成decision
        //        Decision d = new Decision();
        //        for (int i = 0; i < Data.ProSpace.Count; i++)
        //        {
        //            if (temp[i] == 1)
        //            {
        //                d.OpenProductSet.Add(Data.ProSpace[i]);
        //            }
        //        }
        //        deci_a = _ds.FirstOrDefault(k => (k as Decision).Equals(d)) as IALPDecision;
        //        if (deci_a == null) { _ds.Add(d); deci_a = d; }
        //        cp.End();
        //        return false;
        //    }
        //}
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
                foreach (Route h in Data.RouteList)
                {
                    double temp = h.TicketPrice;
                    foreach (Resource r in Data.RS)
                    {
                        if (h.Exists(k => k.Contains(r)))
                        {
                            temp -= V[t][Data.RS.IndexOf(r)];
                        }
                    }
                    if (temp > 0) ss += Data.RouteList.IndexOf(h) + ",";
                }
                #endregion
                print("Time:{0},Number of Bounded Constraints:{1}/{2} | {3} | {4}", t, i, b.Length, s, ss);
            }
        }
    }

    public class RailwayNRMSolver_CLP_Alpha : CLP1_Alpha_Solver
    {
        private NRMDataAdapter _Data;
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

        DecisionSpace _ds = new DecisionSpace();
        protected override void CreateFeasibleSolution()
        {
            Decision d = new Decision();
            _ds.Add(d);
            AddConstraint1(d);
            for (int t = alpha + 1; t < Data.TimeHorizon; t++)
            {
                AddConstraint2(t, d);
            }
        }
        public override void StepFoward()
        {
            alpha -= step;
            //Delete Agg constraint(s)
            foreach (var a in Aggconstraints)
            {
                RMPModel.Remove(a.Value);
            }
            List<IALPDecision> list = Aggconstraints.Keys.ToList();
            Aggconstraints.Clear();

            //Add DisAgg variables and constraint(s)
            for (int i = alpha + 1; i <= alpha + step; i++)
            {
                constraints.Add(i, new Dictionary<IALPDecision, IRange>());
                Var1.Add(i, RMPModel.NumVarArray(Data.RS.Count, 0, double.MaxValue));
                Var2.Add(i, RMPModel.NumVar(0, double.MaxValue));
                V.Add(i, new double[Data.RS.Count]);
                Sita.Add(i, 0);
            }

            for (int t = alpha + 1; t <= alpha + step; t++)
            {
                if (t < Data.TimeHorizon - 1)
                {
                    foreach (IALPResource re in Data.RS)
                    {
                        INumExpr exp2 = RMPModel.Sum(Var1[t][Data.RS.IndexOf(re)], RMPModel.Prod(-1, Var1[t + 1][Data.RS.IndexOf(re)]));
                        RMPModel.AddGe(exp2, 0);
                    }
                    INumExpr exp3 = RMPModel.Sum(Var2[t], RMPModel.Prod(-1, Var2[t + 1]));
                    RMPModel.AddGe(exp3, 0);
                }
            }
            foreach (IALPResource re in Data.RS)
            {
                INumExpr exp6 = RMPModel.NumExpr();
                exp6 = RMPModel.Sum(AggVar1[Data.RS.IndexOf(re)], RMPModel.Prod(-1, Var1[alpha + 1][Data.RS.IndexOf(re)]));
                RMPModel.AddGe(exp6, 0);
            }

            INumExpr exp4 = RMPModel.NumExpr();
            exp4 = RMPModel.Sum(exp4, AggVar2, RMPModel.Prod(-1, Var2[alpha + 1]));
            RMPModel.AddGe(exp4, 0);

            foreach (IALPDecision de in list)
            {
                AddConstraint1(de);
            }
            Decision d = _ds[0] as Decision;
            for (int t = alpha + 1; t <= alpha + step; t++)
            {
                AddConstraint2(t, d);
            }
        }
        public void Solve()
        {
            CreateFeasibleSolution();
            for (; alpha - step >= 0;)
            {
                if (TestValidation())
                {
                    print("--------已经达到最优！", System.DateTime.Now.ToLongTimeString());
                    break;
                }
                else
                {
                    StepFoward();
                }
            }
        }
        public override void DoCalculate()//Calculate the Bid-Price of current time
        {
            print("------{0} Attemping with [Alpha = {1}]", System.DateTime.Now.ToLongTimeString(), alpha);
            bool IsOptimal = true;
            double tempObj = 0;
            int tol = ObeyTime;
            for (int iter = 1; ; iter++)
            {
                //print("------{0} Starting iteration:{1}", System.DateTime.Now.ToLongTimeString(), iter);
              
                if (RMPModel.Solve())
                {
                    #region 判断是否终止
                    if (RMPModel.GetObjValue() - tempObj < Threshold)
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
                print("------{0} Starting iteration:{1},Current Obj Value:{2}", System.DateTime.Now.ToLongTimeString(), iter, tempObj);
                UpdateValues();

                #region 判断是否终止
                if (tol < 0)
                {
                    print("--------{0} Attemping Completed！iter:{1}", System.DateTime.Now.ToLongTimeString(), iter);
                    print("--------{0}算法达到终止条件而退出--------", System.DateTime.Now.ToLongTimeString());
                    UpdateBidPrice();
                    break;
                }
                #endregion

                IsOptimal = SolveSubProblem();

                if (IsOptimal)
                {
                    print("--------{0} Attemping Completed！iter:{1}", System.DateTime.Now.ToLongTimeString(), iter);
                    UpdateValues();
                    UpdateBidPrice();
                    break;
                }
            }
            SendEvent(new IterationCompletedEventArgs()
            {
                Alpha = alpha,
                BidPrice = this.BidPrice,
                ObjValue = tempObj,
                TurnningPoint = findturnningpoint()
            });
              
        }

        private bool SolveSubProblem()
        {
            bool IsOpt = true;

            //IALPDecision deci_a1 = null;
            //bool IsSubOpt1 = CG1(out deci_a1);
            //if (!IsSubOpt1)
            //{
            //    AddConstraint1(deci_a1);
            //}
            //IsOpt = IsSubOpt1 && IsOpt;
            //for (int t = alpha + 1; t < Data.TimeHorizon; t++)
            //{
            //    IALPDecision deci_a = null;
            //    bool IsSubOpt = CG2(t, out deci_a);
            //    if (!IsSubOpt)
            //    {
            //        AddConstraint2(t, deci_a);
            //    }
            //    IsOpt = IsSubOpt && IsOpt;
            //}

            Task aggta = factory.StartNew(() =>
            {
                IALPDecision deci_a1 = null;
                bool IsSubOpt1 = CG1(out deci_a1);
                if (!IsSubOpt1)
                {
                    AddConstraint1(deci_a1);
                }
                IsOpt = IsSubOpt1 && IsOpt;
            }, cts.Token);
            tasks.Add(aggta);

            for (int iteration = alpha + 1; iteration < Data.TimeHorizon; iteration++)
            {
                int t = iteration;
                Task ta = factory.StartNew(() =>
                {
                    IALPDecision deci_a = null;
                    bool IsSubOpt = CG2(t, out deci_a);
                    if (!IsSubOpt)
                    {
                        AddConstraint2(t, deci_a);
                    }
                    IsOpt = IsSubOpt && IsOpt;
                }, cts.Token);
                tasks.Add(ta);
            }
            Task.WaitAll(tasks.ToArray());
            tasks.Clear();
            return IsOpt;
        }

        private bool CG(int t, out IALPDecision deci_a)
        {
            deci_a = null;

            #region 
            List<Product> list = (Data.ProSpace as List<Product>).Where(i =>
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
            }).ToList();
            List<Product> tempDecision = new List<Product>();
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
            //贪婪方法加入其他元素        
            for (int length = 0; length < tempDecision.Count();)
            {
                length = tempDecision.Count();
                foreach (Product p in list)
                {
                    double temp1 = computeValue(t, tempDecision, p);
                    if (temp < temp1)
                    {
                        temp = temp1;
                        tempDecision.Add(p);
                    }
                }
            }
            #endregion

            #region
            #region 得到 Route List
            //List<Route> list = Data.pathList.Where(i =>
            //{
            //    double w = bidprice(t, i);
            //    if (i.TicketPrice < w)
            //    {
            //        return false;
            //    }
            //    else
            //    {
            //        return true;
            //    }
            //}).ToList();
            //List<Route> tempRoutelist = new List<Route>();

            //Route tempRoute = null;
            //double temp = computeValue(t, tempRoutelist, tempRoute);
            //foreach (Route r in list)
            //{
            //    if (temp < computeValue(t, tempRoutelist, r))
            //    {
            //        temp = computeValue(t, tempRoutelist, r);
            //        tempRoute = r;
            //    }
            //}
            //if (tempRoute != null)
            //{
            //    tempRoutelist.Add(tempRoute);
            //    list.Remove(tempRoute);
            //}
            ////贪婪方法加入其他元素        
            //for (int length = 0; length < tempRoutelist.Count();)
            //{
            //    length = tempRoutelist.Count();
            //    foreach (Route r in list)
            //    {
            //        double temp1 = computeValue(t, tempRoutelist, r);
            //        if (temp < temp1)
            //        {
            //            temp = temp1;
            //            tempRoutelist.Add(r);
            //        }
            //    }
            //}
            //#endregion

            //List<Product> tempDecision = new List<Product>();

            //#region 找到一个u
            //foreach (Route r in tempRoutelist)
            //{
            //    foreach (Product p in r)
            //    {
            //        if (!tempDecision.Contains(p)) { tempDecision.Add(p); }
            //    }
            //}
            #endregion
#endregion

            //从u生成decision
            Decision d = new Decision();
            d.OpenProductSet.UnionWith(tempDecision);
            lock (_ds)
            {
                deci_a = _ds.FirstOrDefault(k => (k as Decision).Equals(d)) as IALPDecision;
                if (deci_a == null) { _ds.Add(d); deci_a = d; }
            }

            if (computeValue(t, tempDecision, null) < Tolerance ||
                (t > alpha ? constraints[t].ContainsKey(deci_a) :
                Aggconstraints.ContainsKey(deci_a)))
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        protected override bool CG1(out IALPDecision deci_a)
        {
            return CG(alpha, out deci_a);
        }
        protected override bool CG2(int t, out IALPDecision deci_a)
        {
            return CG(t, out deci_a);
        }

        private double bidprice(int t, Route route)
        {
            double w = 0;
            if (t >= alpha + 1)
            {
                foreach (Resource r in Data.RS)
                {
                    if (route.Exists(i => i.Contains(r)))
                    {
                        w += V[t][Data.RS.IndexOf(r)];
                    }
                }
            }
            else
            {
                foreach (Resource r in Data.RS)
                {
                    if (route.Exists(i => i.Contains(r)))
                    {
                        w += AggV[Data.RS.IndexOf(r)];
                    }
                }
            }

            return w;
        }
        private double bidprice(int t, Product i)
        {
            double w = 0;
            if (t >= alpha + 1)
            {
                foreach (Resource r in Data.RS)
                {
                    if (i.Contains(r))
                    {
                        w += V[t][Data.RS.IndexOf(r)];
                    }
                }
            }
            else
            {
                foreach (Resource r in Data.RS)
                {
                    if (i.Contains(r))
                    {
                        w += AggV[Data.RS.IndexOf(r)];
                    }
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

            if (t <= alpha)
            {
                v = v * (1 + alpha);
                v -= AggSita - Sita[alpha + 1];
            }
            else if (t > alpha && t < Data.TimeHorizon - 1)
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
                    List<Product> templist = new List<Product>();templist.AddRange(list);templist.Add(tp);
                    if (r.TrueForAll(i => list.Contains(i)) || r.TrueForAll(i => templist.Contains(i)))
                    {
                        a += l.ConsiderationDic[r] * (r.TicketPrice - bidprice(t, r));
                        b += l.ConsiderationDic[r];
                    }
                }
                v += Data.MarketInfo.Ro(t) * l.Lamada(t) * (a / b);
            }
            if (t <= alpha)
            {
                v = v * (1 + alpha);
                v -= AggSita - Sita[alpha + 1];
            }
            else if (t > alpha && t < Data.TimeHorizon - 1)
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
    }
}

