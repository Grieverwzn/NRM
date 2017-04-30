using com.foxmail.wyyuan1991.NRM.ALP;
using com.foxmail.wyyuan1991.NRM.Common;
using com.foxmail.wyyuan1991.NRM.RailwayModel;
using com.foxmail.wyyuan1991.NRMSolver;
using ILOG.Concert;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace com.foxmail.wyyuan1991.NRM.RailwaySolver
{
    /// <summary>
    /// Dynamic Disaggregation method (Railway version)
    /// </summary>
    public class RailwayNRMSolver_DD : DD_Solver
    {
        private NRMDataAdapter _Data;
        private DecisionSpace _ds = new DecisionSpace();

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

        # region Implement of abstract methods
        /// <summary>
        /// Initinalize the Solver
        /// </summary>
        public override void Init()
        {
            alpha = Data.TimeHorizon - 1;

            #region //////////////初始化变量//////////////
            InitVariables();
            for (int i = 0; i < Data.MarketInfo.AggRo.Count; i++)
            {
                Add_Agg_Vars(i);
            }
            #endregion

            #region //////////////生成目标//////////////
            INumExpr exp5 = RMPModel.NumExpr();
            exp5 = RMPModel.Sum(exp5, AggVar2[0]);
            foreach (IALPResource re in Data.RS)
            {
                exp5 = RMPModel.Sum(exp5, RMPModel.Prod((Data.InitialState as IALPState)[re], AggVar1[0][Data.RS.IndexOf(re)]));
            }
            IObjective cost = RMPModel.AddMinimize(exp5);
            #endregion

            #region //////////////生成基本约束//////////////
            for (int i = 0; i < Data.MarketInfo.AggRo.Count; i++)
            {
                Add_Agg_Squ_Constraint(i);
            }
            #endregion

            RMPModel.SetOut(null);
            CreateFeasibleSolution();
        }
        /// <summary>
        /// Push Alpha backward, add disaggregated constraints and drop aggregated constraints
        /// </summary>
        public override void StepForward()
        {
            print("--------{0}Starting Step Forward--------", System.DateTime.Now.ToLongTimeString());
            alpha -= step;

            #region Delete Agg constraint(s)
            Dictionary<int, List<IALPDecision>> dic = new Dictionary<int, List<IALPDecision>>();
            int i = findAggRo(alpha);
            for (int k = i; AggConstraints.ContainsKey(k); k++)
            {
                dic.Add(k, AggConstraints[k].Keys.ToList());
                foreach (var a in AggConstraints[k])
                {
                    RMPModel.Remove(a.Value);
                }
                if (k > i)
                {
                    AggConstraints.Remove(k);
                }
            }
            AggConstraints[i].Clear();
            #endregion

            #region Center constraints Agg
            foreach (IALPResource re in Data.RS)
            {
                RMPModel.Remove(AggCenterRange1[Data.RS.IndexOf(re)]);
                INumExpr exp1 = RMPModel.Sum(AggVar1[i][Data.RS.IndexOf(re)], RMPModel.Prod(-1, CenterVar1[Data.RS.IndexOf(re)]));
                AggCenterRange1[Data.RS.IndexOf(re)] = RMPModel.AddGe(exp1, 0);
            }
            RMPModel.Remove(AggCenterRange2);
            INumExpr exp2 = RMPModel.Sum(AggVar2[i], RMPModel.Prod(-1, CenterVar2));
            AggCenterRange2 = RMPModel.AddGe(exp2, 0);
            #endregion

            #region  Add DisAgg variables and constraints
            for (int t = alpha + 1; t <= alpha + step; t++)
            {
                DisConstraints.Add(t, new Dictionary<IALPDecision, IRange>());
                DisVar1.Add(t, RMPModel.NumVarArray(Data.RS.Count, 0, double.MaxValue));
                DisVar2.Add(t, RMPModel.NumVar(0, double.MaxValue));
                DisV.Add(t, new double[Data.RS.Count]);
                DisSita.Add(t, 0);
            }

            //Add Squence constraint(s)
            for (int t = alpha + 1; t <= alpha + step; t++)
            {
                if (t < Data.TimeHorizon - 1)
                {
                    foreach (IALPResource re in Data.RS)
                    {
                        INumExpr exp3 = RMPModel.Sum(DisVar1[t][Data.RS.IndexOf(re)], RMPModel.Prod(-1, DisVar1[t + 1][Data.RS.IndexOf(re)]));
                        RMPModel.AddGe(exp3, 0);
                    }
                    INumExpr exp4 = RMPModel.Sum(DisVar2[t], RMPModel.Prod(-1, DisVar2[t + 1]));
                    RMPModel.AddGe(exp4, 0);
                }
            }
            #endregion

            //Add Center Constraints
            foreach (IALPResource re in Data.RS)
            {
                RMPModel.Remove(DisCenterRange1[Data.RS.IndexOf(re)]);
                INumExpr exp5 = RMPModel.Sum(CenterVar1[Data.RS.IndexOf(re)], RMPModel.Prod(-1, DisVar1[alpha + 1][Data.RS.IndexOf(re)]));
                DisCenterRange1[Data.RS.IndexOf(re)] = RMPModel.AddGe(exp5, 0);
            }
            RMPModel.Remove(DisCenterRange2);
            INumExpr exp6 = RMPModel.Sum(CenterVar2, RMPModel.Prod(-1, DisVar2[alpha + 1]));
            DisCenterRange2 = RMPModel.AddGe(exp6, 0);

            /*
             * 这个步骤比较耗时间，改为并行。
             */
            foreach (IALPDecision d in dic[i])
            {
                IALPDecision de = d;
                int k = i;
                Task aggta = factory.StartNew(() =>
                {
                    Add_Agg_Constraint(k, de);
                }, cts.Token);
                tasks.Add(aggta);
            }
            for (int iteration = alpha + 1; iteration <= alpha + step; iteration++)
            {
                int k = findAggRo(iteration);
                foreach (IALPDecision d in dic[k])
                {
                    int t = iteration;
                    IALPDecision de = d;
                    Task ta = factory.StartNew(() =>
                    {
                        Add_Dis_Constraint(t, de);
                    }, cts.Token);
                    tasks.Add(ta);
                }
            }
            Task.WaitAll(tasks.ToArray());
            tasks.Clear();
            print("--------{0}Step Forward Finished--------", System.DateTime.Now.ToLongTimeString());
        }
        /// <summary>
        /// Solve the (RLP-Alpha) problem
        /// </summary>
        public override void DoCalculate()
        {
            print("------{0} Attemping with [Alpha = {1}],Current DS:{2}", System.DateTime.Now.ToLongTimeString(), alpha, _ds.Count);
            bool IsOptimal = true;
            double tempObj = 0;
            double tempLastObj = Threshold;
            int tol = ObeyTime;
            for (int iter = 1; ; iter++)
            {
                //print("------{0} Starting iteration:{1}", System.DateTime.Now.ToLongTimeString(), iter);
                if (RMPModel.Solve())
                {
                    #region 判断是否终止
                    tempObj = RMPModel.GetObjValue();
                    if ((tempObj / tempLastObj) - 1 < Threshold)
                    {
                        tol--;
                    }
                    else
                    {
                        tol = ObeyTime;
                    }
                    tempLastObj = tempObj;
                    #endregion
                }
                print("------{0} Starting iteration:{1},Current Obj Value:{2},Current DS:{3}", System.DateTime.Now.ToLongTimeString(), iter, tempObj, _ds.Count);
                UpdateValues();

                #region 判断是否终止
                if (tol < 0)
                {
                    print("--------{0} Attemping Stoped due to Ending conditions！iter:{1}", System.DateTime.Now.ToLongTimeString(), iter);
                    UpdateBidPrice();
                    break;
                }
                #endregion

                IsOptimal = SolveSubProblem();

                if (IsOptimal)
                {
                    print("--------{0} Processing Completed！iter:{1}", System.DateTime.Now.ToLongTimeString(), iter);
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
        /// <summary>
        /// To validate if it is optimal
        /// </summary>
        /// <returns></returns>
        public override bool TestValidation()
        {
            //IALPDecision a; //CG(alpha, out a) &&
            //protected Dictionary<int, Dictionary<IALPDecision, IRange>> AggConstraints;
            if (alpha + 1 < Data.TimeHorizon)
            {
                int i = findAggRo(alpha);
                foreach (IALPDecision a in AggConstraints[AggConstraints.Count - 1].Keys)
                {
                    double v = Data.Rt(alpha, a) - Data.RS.Where(re => a.UseResource(re)).Sum(re => Data.Qti(alpha, re, a) * DisV[alpha + 1][Data.RS.IndexOf(re)]) - (AggSita[i] - DisSita[alpha + 1] / last(i));
                    if (v > Threshold) return false;
                }
            }
            else
            {
                return false;
            }
            return true;//testValidation());
        }

        protected override void Add_Agg_Constraint(int i, IALPDecision a)
        {
            if (AggConstraints[i].ContainsKey(a)) return;
            int t = Data.MarketInfo.AggRo[i].Time;
            INumExpr exp1 = RMPModel.NumExpr();
            if (i < AggConstraints.Count - 1)
            {
                exp1 = RMPModel.Sum(AggVar2[i], RMPModel.Prod(-1, AggVar2[i + 1]));
                foreach (IALPResource re in Data.RS)
                {
                    if (a.UseResource(re))
                    {
                        exp1 = RMPModel.Sum(exp1,
                            RMPModel.Prod(last(i) * Data.Qti(t, re, a), AggVar1[i][Data.RS.IndexOf(re)]));
                    }
                }
            }
            else
            {
                exp1 = RMPModel.Sum(AggVar2[i], RMPModel.Prod(-1, CenterVar2));
                foreach (IALPResource re in Data.RS)
                {
                    if (a.UseResource(re))
                    {
                        exp1 = RMPModel.Sum(exp1,
                            RMPModel.Prod(last(i) * Data.Qti(t, re, a), CenterVar1[Data.RS.IndexOf(re)]));
                    }
                }
            }

            lock (RMPModel)
            {
                AggConstraints[i].Add(a, RMPModel.AddGe(exp1, last(i) * Data.Rt(t, a)));
            }
        }
        protected override void Add_Dis_Constraint(int t, IALPDecision a)//Add a column into RMP model
        {
            if (DisConstraints[t].ContainsKey(a)) return;

            INumExpr exp1 = RMPModel.NumExpr();
            if (t < Data.TimeHorizon - 1)
            {
                exp1 = RMPModel.Sum(DisVar2[t], RMPModel.Prod(-1, DisVar2[t + 1]));
                foreach (IALPResource re in Data.RS)
                {
                    if (a.UseResource(re))
                    {
                        exp1 = RMPModel.Sum(exp1, DisVar1[t][Data.RS.IndexOf(re)], RMPModel.Prod(Data.Qti(t, re, a) - 1, DisVar1[t + 1][Data.RS.IndexOf(re)]));
                    }
                }
            }
            else
            {
                exp1 = RMPModel.Sum(exp1, DisVar2[t]);
                foreach (IALPResource re in Data.RS)
                {
                    if (a.UseResource(re))
                    {
                        exp1 = RMPModel.Sum(exp1, DisVar1[t][Data.RS.IndexOf(re)]);
                    }
                }
            }
            lock (RMPModel)
            {
                DisConstraints[t].Add(a, RMPModel.AddGe(exp1, Data.Rt(t, a)));
            }
        }

        protected override void UpdateValues()
        {
            //print("--------{0}更新参数开始--------", System.DateTime.Now.ToLongTimeString());
            for (int i = 0; i < AggConstraints.Count; i++)
            {
                AggV[i] = RMPModel.GetValues(AggVar1[i]);
                AggSita[i] = RMPModel.GetValue(AggVar2[i]);
            }
            for (int t = alpha + 1; t < Data.TimeHorizon; t++)
            {
                DisV[t] = RMPModel.GetValues(DisVar1[t]);
                DisSita[t] = RMPModel.GetValue(DisVar2[t]);
            }
            //print("--------{0}更新参数结束--------", System.DateTime.Now.ToLongTimeString());
        }
        protected override void UpdateBidPrice()
        {
            BidPrice = new double[1 + DisV.Count][];
            //BidPrice结构BidPrice[time][resource] = [Sita,V]
            int i = findAggRo(alpha);
            BidPrice[0] = new double[Data.RS.Count + 1];
            BidPrice[0][0] = AggSita[i];
            for (int re = 1; re <= Data.RS.Count; re++)
            {
                BidPrice[0][re] = AggV[i][re - 1];
            }

            for (int t = 1; t < Data.TimeHorizon - alpha; t++)
            {
                //BidPrice[t] = DisV[t];
                BidPrice[t] = new double[Data.RS.Count + 1];
                BidPrice[t][0] = DisSita[t + alpha];
                for (int re = 1; re <= Data.RS.Count; re++)
                {
                    BidPrice[t][re] = DisV[t + alpha][re - 1];
                }
            }

        }
        #endregion

        #region private methods
        #region Time/Interval Convertor
        private int findAggRo(int t)
        {
            if (Data.MarketInfo.AggRo.Count == 1) return 0;
            for (int i = 1; i < Data.MarketInfo.AggRo.Count; i++)
            {
                if (t < Data.MarketInfo.AggRo[i].Time) return i - 1;
            }
            return Data.MarketInfo.AggRo.Count - 1;
        }
        private int last(int i)
        {
            if (i < Data.MarketInfo.AggRo.Count - 1)
            {
                if (Data.MarketInfo.AggRo[i].Time <= alpha && Data.MarketInfo.AggRo[i + 1].Time <= alpha)
                {
                    return Data.MarketInfo.AggRo[i + 1].Time - Data.MarketInfo.AggRo[i].Time;
                }
                else if (Data.MarketInfo.AggRo[i].Time <= alpha && Data.MarketInfo.AggRo[i + 1].Time > alpha)
                {
                    return alpha - Data.MarketInfo.AggRo[i].Time + 1;
                }
                else
                {
                    return 0;
                }
            }
            else
            {
                if (Data.MarketInfo.AggRo[i].Time <= alpha)
                {
                    return alpha - Data.MarketInfo.AggRo[i].Time + 1;
                }
                else
                {
                    return 0;
                }
            }
        }
        #endregion

        #region Initialize methods
        private void CreateFeasibleSolution()
        {
            Decision d = new Decision();
            _ds.Add(d);
            for (int i = 0; i < Data.MarketInfo.AggRo.Count; i++)
            {
                Add_Agg_Constraint(i, d);
            }
        }
        private void Add_Agg_Squ_Constraint(int i)
        {
            if (i < Data.MarketInfo.AggRo.Count - 1)
            {
                foreach (IALPResource re in Data.RS)
                {
                    INumExpr exp1 = RMPModel.Sum(AggVar1[i][Data.RS.IndexOf(re)], RMPModel.Prod(-1, AggVar1[i + 1][Data.RS.IndexOf(re)]));
                    RMPModel.AddEq(exp1, 0);
                }
                INumExpr exp2 = RMPModel.Sum(AggVar2[i], RMPModel.Prod(-1, AggVar2[i + 1]));
                RMPModel.AddGe(exp2, 0);
            }
            else if (i == Data.MarketInfo.AggRo.Count - 1)
            {
                foreach (IALPResource re in Data.RS)
                {
                    INumExpr exp1 = RMPModel.Sum(AggVar1[i][Data.RS.IndexOf(re)], RMPModel.Prod(-1, CenterVar1[Data.RS.IndexOf(re)]));
                    AggCenterRange1[Data.RS.IndexOf(re)] = RMPModel.AddGe(exp1, 0);
                }
                INumExpr exp2 = RMPModel.Sum(AggVar2[i], RMPModel.Prod(-1, CenterVar2));
                AggCenterRange2 = RMPModel.AddGe(exp2, 0);
            }
        }
        private void InitVariables()
        {
            DisVar1 = new Dictionary<int, INumVar[]>();
            DisVar2 = new Dictionary<int, INumVar>();
            AggVar1 = new Dictionary<int, INumVar[]>();
            AggVar2 = new Dictionary<int, INumVar>();
            CenterVar1 = RMPModel.NumVarArray(Data.RS.Count, 0, double.MaxValue);
            CenterVar2 = RMPModel.NumVar(0, double.MaxValue);

            DisV = new Dictionary<int, double[]>();
            DisSita = new Dictionary<int, double>();
            AggV = new Dictionary<int, double[]>();
            AggSita = new Dictionary<int, double>();
            CenV = new double[Data.RS.Count];
            CenSita = 0;

            DisConstraints = new Dictionary<int, Dictionary<IALPDecision, IRange>>();
            AggConstraints = new Dictionary<int, Dictionary<IALPDecision, IRange>>();
            AggCenterRange1 = new IRange[Data.RS.Count];
            DisCenterRange1 = new IRange[Data.RS.Count];
        }
        private void Add_Agg_Vars(int i)
        {
            AggConstraints.Add(i, new Dictionary<IALPDecision, IRange>());
            AggVar1.Add(i, RMPModel.NumVarArray(Data.RS.Count, 0, double.MaxValue));
            AggVar2.Add(i, RMPModel.NumVar(0, double.MaxValue));
            AggV.Add(i, new double[Data.RS.Count]);
            AggSita.Add(i, 0);
        }
        #endregion

        #region Solving sub problems
        private bool SolveSubProblem()
        {
            bool IsOpt = true;

            for (int i = 0; i < AggConstraints.Count; i++)//遍历聚合约束，求解子问题
            {
                IALPDecision deci_a1 = null;
                bool IsSubOpt1 = Agg_CG(i, out deci_a1);
                if (!IsSubOpt1)
                {
                    Add_Agg_Constraint(i, deci_a1);
                }
                IsOpt = IsSubOpt1 && IsOpt;
            }
            IALPDecision tempd = null;
            for (int t = alpha + 1; t <= Data.TimeHorizon - 1; t++)// 遍历非聚合约束
            {
                if (t == alpha + 1 || PreCG2(t, tempd))
                {
                    IALPDecision deci_a = null;
                    bool IsSubOpt = Dis_CG(t, out deci_a);
                    if (!IsSubOpt)
                    {
                        Add_Dis_Constraint(t, deci_a);
                        tempd = deci_a;
                    }
                    IsOpt = IsSubOpt && IsOpt;
                }
                else
                {
                    bool IsSubOpt = false;
                    Add_Dis_Constraint(t, tempd);
                    IsOpt = IsSubOpt && IsOpt;
                }
            }
            return IsOpt;
        }
        private bool PreCG2(int t, IALPDecision d)//判断一个解是否满足检验数
        {
            if (d == null) return true;
            List<IProduct> li = (d as Decision).OpenProductSet.ToList();
            if (computeValue(t, li, null) < Tolerance ||
                (t > alpha && DisConstraints[t].ContainsKey(d)))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        private bool CG(int t, out IALPDecision deci_a)
        {
            deci_a = null;

            #region  贪婪算法求解组合优化问题
            List<IProduct> list = (Data.ProSpace as List<Product>).ToList<IProduct>();
            /* 优先关闭机会成本低的产品，但是由于换乘不能这样。
            //List <IProduct> list = (Data.ProSpace as List<Product>).Where(j =>
            //{
            //    double w = bidprice(t, j);
            //    if (j.Fare < w)
            //    {
            //        return false;
            //    }
            //    else
            //    {
            //        return true;
            //    }
            //}).ToList<IProduct>();
            */
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
                (t > alpha ? DisConstraints[t].ContainsKey(deci_a) :
                AggConstraints[findAggRo(t)].ContainsKey(deci_a)))
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        protected bool Agg_CG(int i, out IALPDecision deci_a)
        {
            return CG(Data.MarketInfo.AggRo[i].Time, out deci_a);
        }
        protected bool Dis_CG(int t, out IALPDecision deci_a)
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
                        w += DisV[t][Data.RS.IndexOf(r)];
                    }
                }
            }
            else
            {
                int i = findAggRo(t);
                foreach (Resource r in Data.RS)
                {
                    if (route.Exists(p => p.Contains(r)))
                    {
                        w += AggV[i][Data.RS.IndexOf(r)];
                    }
                }
            }

            return w;
        }
        private double bidprice(int t, IProduct p)
        {
            double w = 0;
            if (t >= alpha + 1)
            {
                foreach (Resource r in Data.RS)
                {
                    if (p.Contains(r))
                    {
                        w += DisV[t][Data.RS.IndexOf(r)];
                    }
                }
            }
            else
            {
                int i = findAggRo(t);
                foreach (Resource r in Data.RS)
                {
                    if (p.Contains(r))
                    {
                        w += AggV[i][Data.RS.IndexOf(r)];
                    }
                }
            }

            return w;
        }
        private double computeValue(int t, List<Route> list, Route tp)
        {
            double v = 0;
            if (t <= alpha)
            {
                int i = findAggRo(t);
                v = calFractionValue(i, list, tp);
                v = v * last(i);
                if (i < AggConstraints.Count - 1)
                {
                    v -= AggSita[i] - AggSita[i + 1];
                }
                else
                {
                    v -= AggSita[i] - CenSita;
                }
            }
            else if (t > alpha && t < Data.TimeHorizon - 1)
            {
                v = calFractionValue(t, list, tp);
                foreach (Resource r in Data.RS)
                {
                    if (list.Exists(i => i.Exists(x => x.Contains(r))) || (tp != null && tp.Exists(x => x.Contains(r))))
                    {
                        v -= DisV[t][Data.RS.IndexOf(r)] - DisV[t + 1][Data.RS.IndexOf(r)];
                    }
                }
                v -= DisSita[t] - DisSita[t + 1];
            }
            else
            {
                v = calFractionValue(t, list, tp);
                foreach (Resource r in Data.RS)
                {
                    if (list.Exists(i => i.Exists(x => x.Contains(r))) || (tp != null && tp.Exists(x => x.Contains(r))))
                    {
                        v -= DisV[t][Data.RS.IndexOf(r)];
                    }
                }
                v -= DisSita[t];
            }
            return v;
        }
        private double computeValue(int t, List<IProduct> list, IProduct tp)
        {
            double v = 0;

            if (t <= alpha)
            {
                int i = findAggRo(t);
                v = calFractionValue(i, list, tp);
                v = v * last(i);
                if (i < AggConstraints.Count - 1)
                {
                    v -= AggSita[i] - AggSita[i + 1];
                }
                else
                {
                    v -= AggSita[i] - CenSita;
                }
            }
            else if (t > alpha && t < Data.TimeHorizon - 1)
            {
                v = calFractionValue(t, list, tp);
                foreach (Resource r in Data.RS)
                {
                    if (list.Exists(i => i.Contains(r)) || (tp != null && tp.Contains(r)))
                    {
                        v -= DisV[t][Data.RS.IndexOf(r)] - DisV[t + 1][Data.RS.IndexOf(r)];
                    }
                }
                v -= DisSita[t] - DisSita[t + 1];
            }
            else
            {
                v = calFractionValue(t, list, tp);
                foreach (Resource r in Data.RS)
                {
                    if (list.Exists(i => i.Contains(r)) || (tp != null && tp.Contains(r)))
                    {
                        v -= DisV[t][Data.RS.IndexOf(r)];
                    }
                }
                v -= DisSita[t];
            }
            return v;
        }
        private double calFractionValue(int t, List<IProduct> list, IProduct tp)
        {
            double v = 0;
            foreach (MarketSegment l in Data.MarketInfo)
            {
                double a = 0; double b = l.Retreat;
                foreach (Route r in l.ConsiderationDic.Keys)
                {
                    List<IProduct> templist = new List<IProduct>(); templist.AddRange(list); templist.Add(tp);
                    if (r.TrueForAll(i => list.Contains(i)) || r.TrueForAll(i => templist.Contains(i)))
                    {
                        a += l.ConsiderationDic[r] * (r.TicketPrice - bidprice(t, r));
                        b += l.ConsiderationDic[r];
                    }
                }
                v += Data.MarketInfo.Ro(t) * l.Lamada(t) * (a / b);
            }

            return v;
        }
        private double calFractionValue(int t, List<Route> list, Route tp)
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
            return v;
        }
        #endregion
        #endregion
    }
}
