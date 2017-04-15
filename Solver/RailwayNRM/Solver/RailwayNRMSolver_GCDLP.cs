using com.foxmail.wyyuan1991.NRM.ALP;
using com.foxmail.wyyuan1991.NRM.RailwayModel;
using com.foxmail.wyyuan1991.NRMSolver;
using ILOG.Concert;
using System.Collections.Generic;
using System.Linq;

namespace com.foxmail.wyyuan1991.NRM.RailwaySolver
{
    /// <summary>
    /// CDLP method (Railway version)
    /// </summary>
    public class RailwayNRMSolver_GCDLP : GCDLP_Solver
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
        public override void Init()
        {
            #region //////////////初始化变量//////////////
            InitVariables();
            for(int i =0;i<Data.MarketInfo.AggRo.Count;i++)
            {
                Add_Agg_Vars(i);
            }
            #endregion

            #region //////////////生成目标//////////////
            INumExpr ObjExpr = RMPModel.NumExpr();
            ObjExpr = RMPModel.Sum(ObjExpr, AggVar2[0]);
            foreach (IALPResource re in Data.RS)
            {
                ObjExpr = RMPModel.Sum(ObjExpr, RMPModel.Prod((Data.InitialState as IALPState)[re], AggVar1[0][Data.RS.IndexOf(re)]));
            }
            IObjective cost = RMPModel.AddMinimize(ObjExpr);
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
        public override void StepForward()
        {
        }
        public override void DoCalculate()
        {
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
                    if ((tempObj / tempLastObj)-1 < Threshold)
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
                print("------{0} Starting iteration:{1},Current Obj Value:{2}", System.DateTime.Now.ToLongTimeString(), iter, tempObj);
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
                    print("--------{0} Attemping Completed！iter:{1}", System.DateTime.Now.ToLongTimeString(), iter);
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
        public override bool TestValidation()
        {
            return true;
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
                exp1 =AggVar2[i];
                foreach (IALPResource re in Data.RS)
                {
                    if (a.UseResource(re))
                    {
                        exp1 = RMPModel.Sum(exp1,
                            RMPModel.Prod(last(i) * Data.Qti(t, re, a), AggVar1[i][Data.RS.IndexOf(re)]));
                    }
                }
            }
            lock (RMPModel)
            {
                AggConstraints[i].Add(a, RMPModel.AddGe(exp1, last(i) * Data.Rt(t, a)));
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
            //print("--------{0}更新参数结束--------", System.DateTime.Now.ToLongTimeString());
        }
        protected override void UpdateBidPrice()
        {
            BidPrice = new double[Data.TimeHorizon][];
            for (int t = 0; t < Data.TimeHorizon; t++)
            {
                int i = findAggRo(t);
                BidPrice[t] = AggV[i];
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
            //else if (i == Data.MarketInfo.AggRo.Count - 1)
            //{
            //    foreach (IALPResource re in Data.RS)
            //    {
            //        RMPModel.AddGe(AggVar1[i][Data.RS.IndexOf(re)], 0);
            //    }
            //    RMPModel.AddGe(AggVar2[i], 0);
            //}
        }
        private void InitVariables()
        {
            AggVar1 = new Dictionary<int, INumVar[]>();
            AggVar2 = new Dictionary<int, INumVar>();

            AggV = new Dictionary<int, double[]>();
            AggSita = new Dictionary<int, double>();

            AggConstraints = new Dictionary<int, Dictionary<IALPDecision, IRange>>();
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

            for (int i = 0; i<AggConstraints.Count ; i++)
            {
                IALPDecision deci_a1 = null;
                bool IsSubOpt1 = Agg_CG(i, out deci_a1);
                if (!IsSubOpt1)
                {
                    Add_Agg_Constraint(i, deci_a1);
                }
                IsOpt = IsSubOpt1 && IsOpt;
            }
            return IsOpt;
        }

        private bool CG(int t, out IALPDecision deci_a)
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

            if (computeValue(t, tempDecision, null) < Tolerance ||
                AggConstraints[findAggRo(t)].ContainsKey(deci_a))
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
                return Data.MarketInfo.AggRo[i + 1].Time - Data.MarketInfo.AggRo[i].Time;
            }
            else
            {
                return Data.TimeHorizon - Data.MarketInfo.AggRo[i].Time + 1;
            }
        }
        #endregion 

        private double bidprice(int t, Route route)
        {
            double w = 0;
            int i = findAggRo(t);
            foreach (Resource r in Data.RS)
            {
                if (route.Exists(p=>p.Contains(r)))
                {
                    w += AggV[i][Data.RS.IndexOf(r)];
                }
            }
            return w;
        }
        private double bidprice(int t, Product p)
        {
            double w = 0;
            int i = findAggRo(t);
            foreach (Resource r in Data.RS)
            {
                if (p.Contains(r))
                {
                    w += AggV[i][Data.RS.IndexOf(r)];
                }
            }
            return w;
        }
        private double computeValue(int t, List<Route> list, Route tp)
        {
            double v = 0;
            int i = findAggRo(t);
            v = calFractionValue(i, list, tp);
            v = v * last(i);
            if (i < AggConstraints.Count - 1)
            {
                v -= AggSita[i] - AggSita[i + 1];
            }
            else
            {
                v -= AggSita[i];
            }
            return v;
        }
        private double computeValue(int t, List<Product> list, Product tp)
        {
            double v = 0;
            int i = findAggRo(t);
            v = calFractionValue(i, list, tp);
            v = v * last(i);
            if (i < AggConstraints.Count - 1)
            {
                v -= AggSita[i] - AggSita[i + 1];
            }
            else
            {
                v -= AggSita[i];
            }
            return v;
        }
        private double calFractionValue(int t, List<Product> list, Product tp)
        {
            double v = 0;
            foreach (MarketSegment l in Data.MarketInfo)
            {
                double a = 0; double b = l.Retreat;
                foreach (Route r in l.ConsiderationDic.Keys)
                {
                    List<Product> templist = new List<Product>(); templist.AddRange(list); templist.Add(tp);
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

    }
}
