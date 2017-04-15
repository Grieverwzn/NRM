using ILOG.Concert;
using ILOG.CPLEX;
using System.Collections.Generic;
using System.Linq;
using System;
using com.foxmail.wyyuan1991.MDP;
using com.foxmail.wyyuan1991.NRM.ALP;

namespace com.foxmail.wyyuan1991.NRMSolver
{
    public class CLP1_Alpha_Solver : NRM_Solver
    {
        #region variables      
        public Cplex RMPModel = new Cplex();

        //变量
        protected Dictionary<int, INumVar[]> Var1;
        protected Dictionary<int, INumVar> Var2;
        protected INumVar[] AggVar1;
        protected INumVar AggVar2;
        //值
        protected Dictionary<int, double[]> V;
        protected Dictionary<int, double> Sita;
        protected double[] AggV;
        protected double AggSita;
        //约束
        protected Dictionary<int, Dictionary<IALPDecision, IRange>> constraints;
        protected Dictionary<IALPDecision, IRange> Aggconstraints;

        #region 算法参数
        //收敛参数
        public int alpha = 0;
        public int step = 10;
        //停止参数
        private double m_Tolerance = 1e-1;
        private double threshold = 0.1;
        private int obeyTime = 10;
        #endregion
        #endregion

        #region Attributes
        public double Tolerance
        {
            get
            {
                return m_Tolerance;
            }

            set
            {
                m_Tolerance = value;
            }
        }
        protected double Threshold//改进容许值
        {
            get
            {
                return threshold;
            }

            set
            {
                threshold = value;
            }
        }
        protected int ObeyTime//允许在容许值外的次数
        {
            get
            {
                return obeyTime;
            }

            set
            {
                obeyTime = value;
            }
        }

        #endregion

        public void Init()//Reset the RMP Model 
        {
            InitRMPModel();
            //DoCalculate();
            //InitSubModel();
        }
        public virtual void DoCalculate()//Calculate the Bid-Price of current time
        {
            CreateFeasibleSolution();
            for (; alpha - step >= 0;)
            {
                print("------{0} Attemping with [Alpha = {1}]", System.DateTime.Now.ToLongTimeString(), alpha);
                bool IsOptimal = true;
                double tempObj = 0;
                int tol = ObeyTime;
                for (int iter = 1; ; iter++)
                {
                    IsOptimal = true;
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

                    IALPDecision deci_a1 = null;
                    bool IsSubOpt1 = CG1(out deci_a1);
                    if (!IsSubOpt1)
                    {
                        AddConstraint1(deci_a1);
                    }
                    IsOptimal = IsSubOpt1 && IsOptimal;
                    for (int t = alpha + 1; t < Data.TimeHorizon; t++)
                    {
                        IALPDecision deci_a = null;
                        bool IsSubOpt = CG2(t, out deci_a);
                        if (!IsSubOpt)
                        {
                            AddConstraint2(t, deci_a);
                        }
                        IsOptimal = IsSubOpt && IsOptimal;
                    }


                    if (IsOptimal)
                    {
                        print("--------{0} Attemping Completed！iter:{1}", System.DateTime.Now.ToLongTimeString(), iter);
                        UpdateValues();
                        UpdateBidPrice();
                        break;
                    }
                }
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

        public virtual void StepFoward()
        {
            alpha -= step;
            //TODO:Delete Agg constraint(s)
            foreach (var a in Aggconstraints)
            {
                RMPModel.Remove(a.Value);
            }
            List<IALPDecision> list = Aggconstraints.Keys.ToList();
            Aggconstraints.Clear();
            //TODO:Add DisAgg variables and constraint(s)
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

            IALPDecision d = (Data.DS as IALPDecisionSpace).CloseAllDecision() as IALPDecision;
            AddConstraint1(d);
            foreach (IALPDecision de in list)
            {
                AddConstraint1(de);
            }
            for (int t = alpha + 1; t <= alpha + step; t++)
            {
                AddConstraint2(t, d);
            }
        }

        protected void InitRMPModel()
        {
            #region //////////////初始化变量//////////////
            Var1 = new Dictionary<int, INumVar[]>();//INumVar[Data.TimeHorizon - alpha - 1][];
            Var2 = new Dictionary<int, INumVar>();// RMPModel.NumVarArray(Data.TimeHorizon - alpha - 1, 0, double.MaxValue);
            AggVar1 = RMPModel.NumVarArray(Data.RS.Count, 0, double.MaxValue);
            AggVar2 = RMPModel.NumVar(0, double.MaxValue);

            V = new Dictionary<int, double[]>();
            Sita = new Dictionary<int, double>();
            AggV = new double[Data.RS.Count];
            AggSita = 0;

            constraints = new Dictionary<int, Dictionary<IALPDecision, IRange>>();
            Aggconstraints = new Dictionary<IALPDecision, IRange>();

            for (int i = alpha + 1; i < Data.TimeHorizon; i++)
            {
                constraints.Add(i, new Dictionary<IALPDecision, IRange>());
                Var1.Add(i, RMPModel.NumVarArray(Data.RS.Count, 0, double.MaxValue));
                Var2.Add(i, RMPModel.NumVar(0, double.MaxValue));
                V.Add(i, new double[Data.RS.Count]);
                Sita.Add(i, 0);
            }
            #endregion

            #region //////////////生成目标//////////////
            INumExpr exp5 = RMPModel.NumExpr();
            exp5 = RMPModel.Sum(exp5, AggVar2);
            foreach (IALPResource re in Data.RS)
            {
                exp5 = RMPModel.Sum(exp5, RMPModel.Prod((Data.InitialState as IALPState)[re], AggVar1[Data.RS.IndexOf(re)]));
            }
            IObjective cost = RMPModel.AddMinimize(exp5);
            #endregion

            #region //////////////生成基本约束//////////////
            for (int t = alpha + 1; t < Data.TimeHorizon; t++)
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

            #endregion

            RMPModel.SetOut(null);
        }
        protected void InitSubModel()
        {
            #region //////////////生成变量//////////////
            for (int t = 0; t < Data.TimeHorizon; t++)
            {
                foreach (IALPDecision a in Data.DS)
                {
                    AddConstraint2(t, a);
                }
            }
            #endregion
        }

        protected void AddConstraint1(IALPDecision a)
        {
            if (Aggconstraints.ContainsKey(a)) return;
            lock (RMPModel)
            {
                INumExpr exp1 = RMPModel.NumExpr();
                exp1 = RMPModel.Sum(AggVar2, RMPModel.Prod(-1, Var2[alpha + 1]));
                foreach (IALPResource re in Data.RS)
                {
                    if (a.UseResource(re))
                    {
                        exp1 = RMPModel.Sum(exp1,
                            RMPModel.Prod((alpha + 1) * Data.Qti(alpha, re, a), AggVar1[Data.RS.IndexOf(re)]));
                    }
                }
                Aggconstraints.Add(a, RMPModel.AddGe(exp1, (alpha + 1) * Data.Rt(alpha, a)));
            }
        }
        protected virtual bool CG1(out IALPDecision deci_a)
        {
            deci_a = null;
            double temp = 0;
            foreach (IALPDecision d in Data.DS)
            {
                double temp1 = 0;
                temp1 += AggSita - Sita[alpha + 1];
                foreach (IALPResource re in Data.RS)
                {
                    if (d.UseResource(re))
                    {
                        temp1 += AggV[Data.RS.IndexOf(re)] * (1 + alpha) * Data.Qti(alpha, re, d);
                    }
                }

                temp1 = (1 + alpha) * Data.Rt(alpha, d) - temp1;
                if (temp1 > temp)
                {
                    deci_a = d;
                    temp = temp1;
                }
            }
            if (temp <= Tolerance || Aggconstraints.ContainsKey(deci_a))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        protected void AddConstraint2(int t, IALPDecision a)//Add a column into RMP model
        {
            if (constraints[t].ContainsKey(a)) return;
            lock (RMPModel)
            {
                INumExpr exp1 = RMPModel.NumExpr();
                if (t < Data.TimeHorizon - 1)
                {
                    exp1 = RMPModel.Sum(Var2[t], RMPModel.Prod(-1, Var2[t + 1]));
                    foreach (IALPResource re in Data.RS)
                    {
                        if (a.UseResource(re))
                        {
                            exp1 = RMPModel.Sum(exp1, Var1[t][Data.RS.IndexOf(re)], RMPModel.Prod(Data.Qti(t, re, a) - 1, Var1[t + 1][Data.RS.IndexOf(re)]));
                        }
                    }
                }
                else
                {
                    exp1 = RMPModel.Sum(exp1, Var2[t]);
                    foreach (IALPResource re in Data.RS)
                    {
                        if (a.UseResource(re))
                        {
                            exp1 = RMPModel.Sum(exp1, Var1[t][Data.RS.IndexOf(re)]);
                        }
                    }
                }
                constraints[t].Add(a, RMPModel.AddGe(exp1, Data.Rt(t, a)));
            }
        }
        protected virtual bool CG2(int t, out IALPDecision deci_a)
        {
            deci_a = null;
            double temp = 0;
            foreach (IALPDecision d in Data.DS)
            {
                double temp1 = 0;
                if (t < Data.TimeHorizon - 1)
                {
                    temp1 += Sita[t] - Sita[t + 1];
                    foreach (IALPResource re in Data.RS)
                    {
                        if (d.UseResource(re))
                        {
                            temp1 += V[t][Data.RS.IndexOf(re)] +
                               V[t + 1][Data.RS.IndexOf(re)] * (Data.Qti(t, re, d) - 1);
                        }
                    }
                }
                else
                {
                    temp1 += Sita[t];
                    foreach (IALPResource re in Data.RS)
                    {
                        if (d.UseResource(re))
                        {
                            temp1 += V[t][Data.RS.IndexOf(re)];
                        }
                    }
                }
                temp1 = Data.Rt(t, d) - temp1;
                if (temp1 > temp)
                {
                    deci_a = d;
                    temp = temp1;
                }
            }
            if (temp <= Tolerance || constraints[t].ContainsKey(deci_a))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        protected virtual void CreateFeasibleSolution()
        {
            IALPDecision d = (Data.DS as IALPDecisionSpace).CloseAllDecision() as IALPDecision;
            AddConstraint1(d);
            for (int t = alpha + 1; t < Data.TimeHorizon; t++)
            {
                AddConstraint2(t, d);
            }
        }
        protected void UpdateValues()
        {
            //print("--------{0}更新参数开始--------", System.DateTime.Now.ToLongTimeString());
            AggV = RMPModel.GetValues(AggVar1);
            AggSita = RMPModel.GetValue(AggVar2);
            for (int t = alpha + 1; t < Data.TimeHorizon; t++)
            {
                V[t] = RMPModel.GetValues(Var1[t]);
                Sita[t] = RMPModel.GetValue(Var2[t]);
            }
            //print("--------{0}更新参数结束--------", System.DateTime.Now.ToLongTimeString());
        }
        protected void UpdateBidPrice()
        {
            BidPrice = new double[Data.TimeHorizon][];
            for (int i = 0; i < Data.TimeHorizon; i++)
            {
                if (i <= alpha)
                {
                    BidPrice[i] = AggV;
                }
                else
                {
                    BidPrice[i] = V[i];
                }
            }
        }
        public void ClearUnboundedConstraints()
        {
            Dictionary<int, Dictionary<IALPDecision, IRange>> temp = new Dictionary<int, Dictionary<IALPDecision, IRange>>();
            for (int i = alpha + 1; i < Data.TimeHorizon; i++)
            {
                if (constraints[i].Count() < Data.RS.Count * 1.2) continue;
                temp.Add(i, new Dictionary<IALPDecision, IRange>());
                foreach (IALPDecision d in constraints[i].Keys)
                {
                    if (RMPModel.GetSlack(constraints[i][d]) > 0)
                    {
                        temp[i].Add(d, constraints[i][d]);
                    }
                }
            }
            foreach (var pair in temp)
            {
                foreach (var child in temp[pair.Key])
                {
                    RMPModel.Remove(child.Value);
                    constraints[pair.Key].Remove(child.Key);
                }
            }
        }

        public virtual bool TestValidation()
        {
            bool Val = true;
            //double[][] R = Rotate(BidPrice);
            int[] tp = findturnningpoint();
            foreach (IALPResource re in Data.RS)
            {
                if (tp[Data.RS.IndexOf(re)] > alpha + 1)
                {
                    print("资源{0}的拐点是: {1}", Data.RS.IndexOf(re), tp[Data.RS.IndexOf(re)]);
                }
                else
                {
                    Val = false;
                    print("资源{0}无有效拐点", Data.RS.IndexOf(re), tp[Data.RS.IndexOf(re)]);
                }
            }
            return Val;
        }
    }
}
