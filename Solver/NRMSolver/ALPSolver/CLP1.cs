using ILOG.Concert;
using ILOG.CPLEX;
using System.Collections.Generic;
using System.Linq;
using System;
using com.foxmail.wyyuan1991.MDP;
using com.foxmail.wyyuan1991.NRM.ALP;

namespace com.foxmail.wyyuan1991.NRMSolver
{
    public abstract class CLP1_Solver : NRM_Solver
    {
        #region variables      
        //IObjective cost;
        //主问题模型
        public Cplex RMPModel = new Cplex();
        //输出
        //private TextWriter m_TextWriter;
        //主问题第一类约束
        protected INumVar[][] Var1;
        protected double[][] V;
        //主问题第二类约束
        protected INumVar[] Var2;
        protected double[] Sita;
        protected Dictionary<IALPDecision, IRange>[] constraints;
        //初始时间
        private int m_CurrentTime = 0;
        //改进容许值
        protected double threshold = 0.1;
        //允许在容许值外的次数
        protected int ObeyTime = 10;
        #endregion

        #region Attributes
        public int CurrentTime //To calculate bid price in the time period t.
        {
            get
            {
                return m_CurrentTime;
            }
            set
            {
                m_CurrentTime = value;
            }
        }
        public double Tolerance { get; set; }
        #endregion

        public void Solve()
        {
            Init();
            this.DoCalculate();
        }
        public void Init()//Reset the RMP Model 
        {
            InitRMPModel();
            CreateFeasibleSolution();
        }
        public void StepForward()
        {

        }   
        public virtual void DoCalculate()//Calculate the Bid-Price of current time
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


                if (IsOptimal)
                {
                    print("--------已经达到最优！", System.DateTime.Now.ToLongTimeString());
                    UpdateValues();
                    UpdateBidPrice();
                    break;
                }
            }
        }
        public bool TestValidation()
        {
            return true;
        }
        public void ClearUnboundedConstraints()
        {
            ;
        }

        protected void AddConstraint(int t, IALPDecision a)//Add a column into RMP model
        {
            if (constraints[t].ContainsKey(a)) return;
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

        protected void UpdateValues()
        {
            print("--------{0}更新参数开始--------", System.DateTime.Now.ToLongTimeString());
            for (int t = 0; t < CurrentTime; t++)
            {
                foreach (IALPResource re in Data.RS)
                {
                    V[t][Data.RS.IndexOf(re)] = 0;
                }
                Sita[t] = 0;
            }
            for (int t = CurrentTime; t < Data.TimeHorizon; t++)
            {
                V[t] = RMPModel.GetValues(Var1[t]);
            }
            Sita = RMPModel.GetValues(Var2);
            print("--------{0}更新参数结束--------", System.DateTime.Now.ToLongTimeString());
        }
        protected void UpdateBidPrice()
        {
            BidPrice = V;
        }

        protected void InitRMPModel()
        {
            Var1 = new INumVar[Data.TimeHorizon][];
            V = new double[Data.TimeHorizon][];
            Var2 = RMPModel.NumVarArray(Data.TimeHorizon, 0, double.MaxValue);
            Sita = new double[Data.TimeHorizon];
            constraints = new Dictionary<IALPDecision, IRange>[Data.TimeHorizon];

            #region //////////////生成变量//////////////
            for (int i = 0; i < Data.TimeHorizon; i++)
            {
                constraints[i] = new Dictionary<IALPDecision, IRange>();
                Var1[i] = RMPModel.NumVarArray(Data.RS.Count, 0, double.MaxValue);// new INumVar[aff.DS.Count];
                V[i] = new double[Data.RS.Count];
            }
            #endregion

            #region //////////////生成目标//////////////
            INumExpr exp5 = RMPModel.NumExpr();
            exp5 = RMPModel.Sum(exp5, Var2[0]);
            foreach (IALPResource re in Data.RS)
            {
                exp5 = RMPModel.Sum(exp5, RMPModel.Prod((Data.InitialState as IALPState)[re], Var1[0][Data.RS.IndexOf(re)]));
            }
            IObjective cost = RMPModel.AddMinimize(exp5);
            #endregion

            #region //////////////生成基本约束//////////////
            for (int t = 0; t < Data.TimeHorizon; t++)
            {
                if (t < Data.TimeHorizon - 1)
                {
                    foreach (IALPResource re in Data.RS)
                    {
                        INumExpr exp2 = RMPModel.NumExpr();
                        exp2 = RMPModel.Sum(Var1[t][Data.RS.IndexOf(re)], RMPModel.Prod(-1, Var1[t + 1][Data.RS.IndexOf(re)]));
                        RMPModel.AddGe(exp2, 0);
                    }
                    INumExpr exp3 = RMPModel.NumExpr();
                    exp3 = RMPModel.Sum(exp3, Var2[t], RMPModel.Prod(-1, Var2[t + 1]));
                    RMPModel.AddGe(exp3, 0);
                }
            }
            #endregion

            RMPModel.SetOut(this.SolverTextWriter);
        }
        protected void InitSubModel()
        {
            #region //////////////生成变量//////////////
            for (int t = 0; t < Data.TimeHorizon; t++)
            {
                foreach (IALPDecision a in Data.DS)
                {
                    AddConstraint(t, a);
                }
            }
            #endregion
        }    
        protected virtual bool CG(int t, out IALPDecision deci_a)
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

        protected abstract void CreateFeasibleSolution();
        //{
        //    IALPDecision d = (Data.DS as IALPDecisionSpace).CloseAllDecision() as IALPDecision;
        //    for (int t = CurrentTime; t < Data.TimeHorizon; t++)
        //    {
        //        AddConstraint(t, d);
        //    }
        //}
      
        protected virtual void SA()
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
                        s += Data.DS.IndexOf(c.Key) + ",";
                    }
                }
                foreach (var a in b)
                {
                    if (a == 0) i++;
                }
                print("Time:{0},Number of Bounded Constraints:{1}/{2} | {3}", t, i, b.Length, s);
            }
        }
    }
    
}
