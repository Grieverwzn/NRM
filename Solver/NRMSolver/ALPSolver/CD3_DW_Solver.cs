using com.foxmail.wyyuan1991.MDP;
using com.foxmail.wyyuan1991.NRM.ALP;
using ILOG.Concert;
using ILOG.CPLEX;
using System.Collections.Generic;
using System.Linq;

namespace com.foxmail.wyyuan1991.NRMSolver
{
    /// <summary>
    /// CD3模型的DW分解+列生成方法
    /// </summary>
    public class CD3_DW_Solver : NRM_Solver
    {
        #region variables      
        IObjective cost;
        //主问题模型
        public Cplex RMPModel = new Cplex();
        //子问题模型
        //public Cplex SubModel = new Cplex();
        //主问题第一类对偶变量
        public double[][] DualValue1;
        //主问题第二类对偶变量
        public double[] DualValue2;
        //输出
        //private TextWriter m_TextWriter;
        //主问题第一类约束
        IRange[][] constraint1;
        //主问题第二类约束
        IRange[] constraint2;
        Dictionary<IALPDecision,INumVar>[] var;
        double[][] lowerbound;
        double[][] upperbound;
        //初始时间
        private int m_CurrentTime = 0;
        //改进容许值
        double threshold = 0.1;
        //允许在容许值外的次数
        int ObeyTime = 10;
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
                double[] EC = ExpectedConsume(value);
                m_CurrentTime = value;
                for (int i = 0; i < Data.TimeHorizon; i++)  //Reset the constraints with time ticking
                {
                    if (i < CurrentTime)
                    {
                        foreach (IALPResource re in Data.RS)
                        {
                            RMPModel.Delete(constraint1[i][Data.RS.IndexOf(re)]);
                            RMPModel.Delete(constraint2[i]);
                        }
                        foreach (IALPDecision d in var[i].Keys)
                        {
                            RMPModel.Delete(var[i][d]);
                        }
                        //constraint1[i][Data.RS.IndexOf(re)].SetBounds((Data.InitialState as IALPState)[re], (Data.InitialState as IALPState)[re]);
                    }
                    else
                    {
                        foreach (IALPResource re in Data.RS)
                        {
                            constraint1[i][Data.RS.IndexOf(re)].UB -= EC[Data.RS.IndexOf(re)];
                            lowerbound[i][Data.RS.IndexOf(re)] -= EC[Data.RS.IndexOf(re)];
                            upperbound[i][Data.RS.IndexOf(re)] -= EC[Data.RS.IndexOf(re)];
                        }
                    }
                }
            }
        }
        public double Tolerance { get; set; }
        #endregion

        //设置时间和资源状态
        public void SetParam(Dictionary<IALPResource, int> state)
        {
            //判断最优性
            double temp = 0;
            for (int i = CurrentTime; i < Data.TimeHorizon; i++)
            {
                foreach (IALPResource re in Data.RS)
                {
                    if(state[re]- constraint1[i][Data.RS.IndexOf(re)].UB>=0)
                    {
                        temp += (state[re] - constraint1[i][Data.RS.IndexOf(re)].UB) / 
                            (upperbound[i][Data.RS.IndexOf(re)]- constraint1[i][Data.RS.IndexOf(re)].UB);
                    }else{
                        temp += (constraint1[i][Data.RS.IndexOf(re)].UB - state[re]) / 
                            (constraint1[i][Data.RS.IndexOf(re)].UB-lowerbound[i][Data.RS.IndexOf(re)]);
                    }
                }
                System.Console.WriteLine("偏离值为：{0}", temp);
            }
        }
        public void Init()//Reset the RMP Model 
        {
            InitRMPModel();
            DoCalculate();
            //InitSubModel();
        }
        public virtual void DoCalculate()//Calculate the Bid-Price of current time
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
                UpdateDualValues();
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
                        AddCol(t, deci_a);
                    }
                    IsOptimal = IsSubOpt && IsOptimal;
                }

                if (IsOptimal)
                {
                    print("--------已经达到最优！", System.DateTime.Now.ToLongTimeString());
                    UpdateDualValues();
                    UpdateBidPrice();
                    break;
                }
            }
        }

        protected void InitRMPModel()
        {
            cost = RMPModel.AddMaximize();
            var = new Dictionary<IALPDecision, INumVar>[Data.TimeHorizon];
            DualValue1 = new double[Data.TimeHorizon][];
            DualValue2 = new double[Data.TimeHorizon];
            lowerbound = new double[Data.TimeHorizon][];
            upperbound = new double[Data.TimeHorizon][];
            #region //////////////生成约束//////////////
            constraint1 = new IRange[Data.TimeHorizon][];
            constraint2 = new IRange[Data.TimeHorizon];
            for (int i = 0; i < Data.TimeHorizon; i++)
            {
                DualValue1[i] = new double[Data.RS.Count];
                var[i] = new Dictionary<IALPDecision, INumVar>();
                constraint1[i] = new IRange[Data.RS.Count];
                lowerbound[i] = new double[Data.RS.Count];
                upperbound[i] = new double[Data.RS.Count];
                foreach (IALPResource re in Data.RS)
                {
                    constraint1[i][Data.RS.IndexOf(re)] = RMPModel.AddRange(double.MinValue, (Data.InitialState as IALPState)[re]);
                }
                //constraint1[i][0].UB -= 0.3;
                constraint2[i] = RMPModel.AddRange(1, 1);
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
                    AddCol(t, a);
                }
            }
            #endregion
        }
        protected void AddCol(int t, IALPDecision a)//Add a column into RMP model
        {
            if (var[t].ContainsKey(a)) return;
            //目标函数
            Column col = RMPModel.Column(cost, Data.Rt(t, a));
            //第一类约束
            foreach (IALPResource re in Data.RS)
            {
                for (int k = t + 1; k < Data.TimeHorizon; k++)
                {
                    col = col.And(RMPModel.Column(constraint1[k][Data.RS.IndexOf(re)], Data.Qti(t, re, a)));
                }
                if (a.UseResource(re))
                {
                    col = col.And(RMPModel.Column(constraint1[t][Data.RS.IndexOf(re)], 1));
                }
            }
            //第二类约束
            col = col.And(RMPModel.Column(constraint2[t], 1));
            INumVar v = RMPModel.NumVar(col, 0,1, NumVarType.Float);
            var[t].Add(a,v);
        }
        protected virtual bool CG(int t, out IALPDecision deci_a)
        {
            deci_a = null;
            double temp = 0;
            foreach (IALPDecision d in Data.DS)
            {
                double temp1 = 0;
                temp1 += DualValue2[t];
                foreach (IALPResource r in Data.RS)
                {
                    if (d.UseResource(r))
                    {
                        temp1 += DualValue1[t][Data.RS.IndexOf(r)];
                    }
                    double temp2 = 0;
                    for (int k = t + 1; k < Data.TimeHorizon; k++)
                    {
                        temp2 += DualValue1[k][Data.RS.IndexOf(r)];
                    }
                    temp1 += temp2 * Data.Qti(t, r, d);
                }
                temp1 = Data.Rt(t, d) - temp1;
                if (temp1 > temp)
                {
                    deci_a = d;
                    temp = temp1;
                }
            }
            if (temp <= Tolerance|| var[t].ContainsKey(deci_a))
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
            for (int t = CurrentTime; t < Data.TimeHorizon; t++)
            {
                AddCol(t, d);
            }
        }
        protected void UpdateDualValues()
        {
            print("--------{0}更新参数开始--------", System.DateTime.Now.ToLongTimeString());
            for (int t = 0; t < CurrentTime; t++)
            {
                foreach (IALPResource re in Data.RS)
                {
                    DualValue1[t][Data.RS.IndexOf(re)] = 0;
                }
                DualValue2[t] = 0;
            }
            for (int t = CurrentTime; t < Data.TimeHorizon; t++)
            {
                foreach (IALPResource re in Data.RS)
                {
                    DualValue1[t][Data.RS.IndexOf(re)] = RMPModel.GetDual(constraint1[t][Data.RS.IndexOf(re)]);
                }
                DualValue2[t] = RMPModel.GetDual(constraint2[t]);
            };
            print("--------{0}更新参数结束--------", System.DateTime.Now.ToLongTimeString());
        }
        protected void UpdateBidPrice()
        {
            BidPrice = DualValue1;
            for (int t = CurrentTime; t < Data.TimeHorizon; t++)
            {
                foreach (IALPResource re in Data.RS)
                {
                    double temp1 = 0;
                    for (int i = t; i < Data.TimeHorizon; i++)
                    {
                        temp1 += RMPModel.GetDual(constraint1[i][Data.RS.IndexOf(re)]);
                    }
                    BidPrice[t][Data.RS.IndexOf(re)] = temp1;
                }
            }
        }
        protected void SA()
        {
            //RMPModel.GetBasisStatuses(constraint1[0]);
            //敏感度分析只能得出每个约束单独变化的情况
            //double[] lowerbound = new double[Data.RS.Count];
            //double[] upperbound = new double[Data.RS.Count];

            //System.Console.WriteLine("输出各个约束的松弛值");
            for (int i = CurrentTime; i < Data.TimeHorizon; i++)
            {
                //System.Console.WriteLine("输出shijian[{0}]",i);
                RMPModel.GetRHSSA(lowerbound[i], upperbound[i], constraint1[i]);
                //输出所有
                foreach (IALPResource r in Data.RS)
                {
                    System.Console.WriteLine("t={0},re={4},b={1},LB={2},UP={3}",
                        i, constraint1[i][Data.RS.IndexOf(r)].UB,
                        lowerbound[i][Data.RS.IndexOf(r)], upperbound[i][Data.RS.IndexOf(r)], Data.RS.IndexOf(r));
                }
            }

        }
        public double[] ExpectedConsume(int t)
        {
            double[] res = new double[Data.RS.Count];
            foreach (IALPResource r in Data.RS)
            {
                double temp = 0;
                for (int k = CurrentTime + 1; k < t; k++)
                {
                    foreach (IALPDecision a in var[k].Keys)
                    {
                        if(var[k][a]!=null)
                        {
                            temp += RMPModel.GetValue(var[k][a]) * Data.Qti(k, r, a);
                        }
                    }
                }
                res[Data.RS.IndexOf(r)] = temp;
            }
            return res;
        }
    }

}
