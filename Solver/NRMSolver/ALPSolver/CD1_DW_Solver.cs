using ILOG.Concert;
using ILOG.CPLEX;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System;
using System.Collections.Concurrent;
using com.foxmail.wyyuan1991.MDP;
using com.foxmail.wyyuan1991.NRM.ALP;

namespace com.foxmail.wyyuan1991.NRMSolver
{
    /// <summary>
    /// CD1模型的DW分解+列生成方法
    /// </summary>
    public class CD1_DW_Solver: NRM_Solver
    {
        #region variables 变量
        //主问题模型
        public Cplex RMPModel = new Cplex();
        //子问题模型
        public Cplex SubModel = new Cplex();
        //主问题第一类对偶变量
        private double[][] DualValue1;
        //主问题第二类对偶变量
        private double[] DualValue2;
        //主问题目标
        IObjective cost;
        //主问题第一类约束
        IRange[][] constraint1;
        //主问题第二类约束
        IRange[] constraint2;
        //RCG问题变量
        INumVar[] h;
        INumVar[] r;
        //RCG问题的目标函数
        INumExpr[][] subObject = new INumExpr[2][];
        //RCG问题的第二类约束
        IRange[] constraint_sub2;
        //State-Active Space
        internal StateActiveSpace SAS;//= new StateActiveSpace();
        internal StateActiveSpace curSAS = new StateActiveSpace();
        //是否达到最优
        bool IsOptimal = false;
        //改进容许值
        double threshold = 0.1;
        //允许在容许值外的次数
        int ObeyTime = 10;
        //初始时间
        private int m_CurrentTime = 0;
        ConcurrentStack<StateActive> tempSA = new ConcurrentStack<StateActive>();
        #endregion

        #region Attributes 属性
        public double Tolerance { get; set; }
        public int CurrentTime //To calculate bid price in the time period t.
        {
            get
            {
                return m_CurrentTime;
            }
            set
            {
                m_CurrentTime = value;
                for (int i = 0; i <= CurrentTime; i++)  //Reset the constraints with time ticking
                {
                    foreach (IALPResource re in Data.RS)
                    {
                        if (i == CurrentTime)
                        {
                            constraint1[i][Data.RS.IndexOf(re)].SetBounds((Data.InitialState as IALPState)[re], (Data.InitialState as IALPState)[re]);
                        }
                        else
                        {
                            constraint2[i].SetBounds(0, 0);
                            constraint1[i][Data.RS.IndexOf(re)].SetBounds(0, 0);
                        }
                    }
                }
            }
        }
        #endregion

        public CD1_DW_Solver()
        {
        }
        public CD1_DW_Solver(IALPFTMDP _aff)
        {
            Data = _aff;
        }

        public void Init()//Reset the RMP Model 
        {
            NumOfThreads = 4;
            print("--------{0}初始化问题开始--------", System.DateTime.Now.ToLongTimeString());
            m_CurrentTime = 0;
            InitRMPModel();
            InitRCGModel();
            curSAS.Clear();
            DoCalculate();
            SAS = curSAS.Clone();
            SAS.GetBasis(RMPModel);
            print("--------{0}初始化问题完成--------", System.DateTime.Now.ToLongTimeString());
        }
        public void Reset()
        {
            print("--------{0}重置问题开始--------", System.DateTime.Now.ToLongTimeString());
            InitRMPModel();
            //ReSetRMPModel();
            m_CurrentTime = 0;
            //foreach (StateActive sa in curSAS)
            //{
            //    RMPModel.Delete(sa.Var);
            //}
            curSAS.Clear();
            foreach (StateActive sa in SAS)
            {
                AddCol(sa.T, sa.S, sa.D);
            }
            print("--------{0}重置问题完成--------", System.DateTime.Now.ToLongTimeString());
        }
        public void DoCalculate()//Calculate the Bid-Price of current time
        {
            print("--------{0}开始求解--------", System.DateTime.Now.ToLongTimeString());
            IsOptimal = false;
            double tempObj = 0;
            int tol = ObeyTime;

            //生成可行解
            CreateFeasibleSolution();
            //检验是否有违背State的变量
            CheckValidateState();

            for (int iter = 1; ; iter++)
            {
                print("--------{1}开始进行第{0}次循环--------", iter, System.DateTime.Now.ToLongTimeString());
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
                IsOptimal = true;
                print("--------{1}目标值:{0}--------", RMPModel.ObjValue, System.DateTime.Now.ToLongTimeString());
                //更新对偶值
                UpdateDualValues();
                //生成子问题目标函数
                GenSubModelObj();
                #region 判断是否终止
                if (tol < 0)
                {
                    print("--------{0}算法达到终止条件而退出--------", System.DateTime.Now.ToLongTimeString());
                    break;
                }
                #endregion
                //求解子问题
                SolveSubProblem();
                if (IsOptimal)
                {
                    print("--------已经达到最优！", System.DateTime.Now.ToLongTimeString());
                    UpdateDualValues();
                    break;
                }
            }
        }

        private void SolveSubProblem()
        {
            for (int t = CurrentTime; t < Data.TimeHorizon; t++)
            {
                IALPState temp_s = null;
                IMDPDecision deci_a = null;
                bool IsSubOpt = RCG(t, out temp_s, out deci_a);
                if (!IsSubOpt)
                {
                    int iteration = t;
                    IALPState tempstate = temp_s;
                    IMDPDecision tempdeci = deci_a;
                    Task ta = factory.StartNew(() =>
                    {
                        StateActive temp = new StateActive() { T = iteration, S = tempstate, D = tempdeci };
                        if (curSAS.FirstOrDefault(i => i.Equals(temp)) == null)
                        {
                            tempSA.Push(temp);
                            lock (RMPModel)
                            {
                                temp.Var = AddCol(temp.T, temp.S, temp.D);
                            }
                        }
                    }, cts.Token);
                    tasks.Add(ta);
                    //StateActive tempSA = new StateActive() { T = t, S = temp_s, D = deci_a };
                    //if (curSAS.Add(tempSA))
                    //{
                    //    tempSA.Var = AddCol(t, temp_s, deci_a);
                    //}
                }
                IsOptimal = IsSubOpt && IsOptimal;
                //System.Console.WriteLine("#{0}第{1}个子问题已经解决！", System.DateTime.Now.ToLongTimeString(), t);
            }
            Task.WaitAll(tasks.ToArray());
            tasks.Clear();
            curSAS.UnionWith(tempSA);
            tempSA.Clear();
        }
        private void CheckValidateState()
        {
            List<StateActive> list = curSAS.RemoveState(Data.InitialState as IALPState);
            foreach (StateActive sa in list)
            {
                RMPModel.Delete(sa.Var);
            }
        }
        protected virtual void CreateFeasibleSolution()
        {
            print("--------{0}开始生成可行解--------", System.DateTime.Now.ToLongTimeString());
            IMDPDecision d = (Data.DS as IALPDecisionSpace).CloseAllDecision();
            for (int t = CurrentTime; t < Data.TimeHorizon; t++)
            {
                int iteration = t;
                IALPState tempstate = (Data.InitialState as IALPState);
                Task ta = factory.StartNew(() =>
                {
                    StateActive temp = new StateActive() { T = iteration, S = tempstate, D = d };
                    if (curSAS.FirstOrDefault(i => i.Equals(temp)) == null)
                    {
                        tempSA.Push(temp);
                        lock (RMPModel)
                        {
                            temp.Var = AddCol(temp.T, temp.S, temp.D);
                        }
                    }
                }, cts.Token);
                tasks.Add(ta);
            };
            Task.WaitAll(tasks.ToArray());
            tasks.Clear();
            curSAS.UnionWith(tempSA);
            tempSA.Clear();
            print("--------{0}生成可行解完毕--------", System.DateTime.Now.ToLongTimeString());
        }
        private void InitRMPModel()
        {
            RMPModel.ClearModel();
            cost = RMPModel.AddMaximize();
            constraint1 = new IRange[Data.TimeHorizon][];
            constraint2 = new IRange[Data.TimeHorizon];
            DualValue1 = new double[Data.TimeHorizon][];
            DualValue2 = new double[Data.TimeHorizon];

            #region //////////////生成约束//////////////
            //Parallel.For(0, Data.TimeHorizon, i =>
            for (int i = 0; i < Data.TimeHorizon; i++)
            {
                lock (RMPModel)
                {
                    constraint1[i] = new IRange[Data.RS.Count];
                    DualValue1[i] = new double[Data.RS.Count];
                    foreach (IALPResource re in Data.RS)
                    {
                        if (i == 0)
                        {
                            constraint1[i][Data.RS.IndexOf(re)] = RMPModel.AddRange((Data.InitialState as IALPState)[re], (Data.InitialState as IALPState)[re]);
                        }
                        else
                        {
                            constraint1[i][Data.RS.IndexOf(re)] = RMPModel.AddRange(0, 0);
                        }
                    }
                    constraint2[i] = RMPModel.AddRange(1, 1);
                }
            }
            #endregion

            //RMPModel.SetParam(Cplex.LongParam.RootAlgorithm, 1);
            RMPModel.SetOut(SolverTextWriter);
        }
        private void ReSetRMPModel()
        {
            //Parallel.For(0, Data.TimeHorizon, i =>
            for (int i = 0; i < Data.TimeHorizon; i++)
            {
                lock (RMPModel)
                {
                    foreach (IALPResource re in Data.RS)
                    {
                        if (i == 0)
                        {
                            constraint1[i][Data.RS.IndexOf(re)].SetBounds((Data.InitialState as IALPState)[re], (Data.InitialState as IALPState)[re]);
                        }
                        else
                        {
                            constraint1[i][Data.RS.IndexOf(re)].SetBounds(0, 0);
                        }
                    }
                    constraint2[i].SetBounds(1, 1);
                }
            };
        }
        private void InitRCGModel()
        {
            SubModel.ClearModel();
            h = SubModel.NumVarArray(Data.DS.Count, 0, double.MaxValue);
            r = SubModel.NumVarArray(Data.RS.Count, 0, double.MaxValue);
            constraint_sub2 = new IRange[Data.RS.Count];
            subObject[0] = new INumExpr[Data.TimeHorizon];
            subObject[1] = new INumExpr[Data.TimeHorizon];

            #region 生成约束
            //第一、四种约束
            INumExpr expr1 = SubModel.NumExpr();
            foreach (IMDPDecision d in Data.DS)
            {
                expr1 = SubModel.Sum(expr1, h[Data.DS.IndexOf(d)]);//SubModel.AddGe(h[aff.DS.IndexOf(d)], 0);
            }
            SubModel.AddEq(expr1, 1);

            //第二、三种约束
            foreach (IALPResource re in Data.RS)
            {
                constraint_sub2[Data.RS.IndexOf(re)] = SubModel.AddLe(r[Data.RS.IndexOf(re)], (Data.InitialState as IALPState)[re]);
                INumExpr expr2 = SubModel.NumExpr();
                foreach (IALPDecision a in Data.DS)
                {
                    if (a.UseResource(re))
                    {
                        expr2 = SubModel.Sum(expr2, h[Data.DS.IndexOf(a)]);
                    }
                }
                expr2 = SubModel.Sum(expr2, SubModel.Prod(-1, r[Data.RS.IndexOf(re)]));
                SubModel.AddLe(expr2, 0);
            }
            #endregion

            #region 初始化目标函数第一部分
            //Parallel.For(0, Data.TimeHorizon, t =>
            for (int t = 0; t < Data.TimeHorizon; t++)
            {
                int iteration = t;
                Task ta = factory.StartNew(() =>
                {
                    subObject[0][iteration] = SubModel.NumExpr();                  
                    foreach (IMDPDecision a in Data.DS)
                    {
                        subObject[0][iteration] = SubModel.Sum(subObject[0][iteration],
                            SubModel.Prod(Data.Rt(iteration, a), h[Data.DS.IndexOf(a)]));
                    }
                }, cts.Token);
                tasks.Add(ta);
            }
            #endregion
            Task.WaitAll(tasks.ToArray());
            tasks.Clear();

            SubModel.SetOut(null);
        }
        protected virtual INumVar AddCol(int t, IALPState s, IMDPDecision a)//Add a column into RMP model
        {
            Column col = RMPModel.Column(cost, Data.Rt(t, a));
            foreach (IALPResource re in Data.RS)
            {
                col = col.And(RMPModel.Column(constraint1[t][Data.RS.IndexOf(re)], (s as IALPState)[re]));
                if (t < Data.TimeHorizon - 1)
                {
                    col = col.And(RMPModel.Column(constraint1[t + 1][Data.RS.IndexOf(re)], (Data.Qti(t, re, a)) - (s as IALPState)[re]));
                }
            }
            col = col.And(RMPModel.Column(constraint2[t], 1));
            return RMPModel.NumVar(col, 0, double.MaxValue, NumVarType.Float);
        }
        private void UpdateDualValues()
        {
            print("--------{0}更新参数开始--------", System.DateTime.Now.ToLongTimeString());
            //Parallel.For(0, Data.TimeHorizon, t =>
            for (int t = 0; t < Data.TimeHorizon; t++)
            {
                foreach (IALPResource re in Data.RS)
                {
                    DualValue1[t][Data.RS.IndexOf(re)] = RMPModel.GetDual(constraint1[t][Data.RS.IndexOf(re)]);
                }
                DualValue2[t] = RMPModel.GetDual(constraint2[t]);
            };
            print("--------{0}更新参数结束--------", System.DateTime.Now.ToLongTimeString());
            BidPrice = DualValue1;
        }
        private void GenSubModelObj()
        {
            print("--------{0}生成子问题目标函数开始--------", System.DateTime.Now.ToLongTimeString());
            //Parallel.For(CurrentTime, Data.TimeHorizon, t =>
            for (int t = 0; t < Data.TimeHorizon; t++)
            {
                int iteration = t;
                Task ta = factory.StartNew(() =>
                {
                    #region 生成目标函数
                    if (iteration >= m_CurrentTime)
                    {
                        INumExpr expr = SubModel.NumExpr();
                        foreach (IALPResource i in Data.RS)
                        {
                            expr = SubModel.Sum(expr,
                                    SubModel.Prod(-DualValue1[iteration][Data.RS.IndexOf(i)], r[Data.RS.IndexOf(i)]));

                            if (iteration < Data.TimeHorizon - 1)
                            {
                                expr = SubModel.Sum(expr,
                                        SubModel.Prod(DualValue1[iteration + 1][Data.RS.IndexOf(i)], r[Data.RS.IndexOf(i)]));
                            }
                        }
                        foreach (IMDPDecision a in Data.DS)
                        {
                            double temp = 0;
                            if (iteration  < Data.TimeHorizon - 1)
                            {
                                temp += Data.RS.Sum(i => -DualValue1[iteration + 1][Data.RS.IndexOf(i)] * Data.Qti(iteration, i, a));
                                expr = SubModel.Sum(expr,
                                    SubModel.Prod(temp, h[Data.DS.IndexOf(a)]));//t 还是t-1
                            }
                        }
                        expr = SubModel.Sum(expr, -DualValue2[iteration]);
                        subObject[1][iteration] = SubModel.Sum(subObject[0][iteration], expr);
                        SubModel.Delete(expr);

                    }
                    #endregion
                }, cts.Token);
                tasks.Add(ta);            
            };
            Task.WaitAll(tasks.ToArray());
            tasks.Clear();
            print("--------{0}生成子问题目标函数结束--------", System.DateTime.Now.ToLongTimeString());
        }
        private bool CG(int t, out IALPState temp_s, out IMDPDecision deci_a)
        {
            temp_s = null;
            deci_a = null;
            double reduced_cost = 0;
            foreach (IALPState s in Data.SS)
            {
                foreach (IMDPDecision a in Data.GenDecisionSpace(s))
                {
                    double temp = Data.Rt(t, a) - Data.RS.Sum(i => DualValue1[t][Data.RS.IndexOf(i)] * s[i]) - DualValue2[t];

                    if (t < Data.TimeHorizon - 1)
                    {
                        temp -= Data.RS.Sum(i => DualValue1[t + 1][Data.RS.IndexOf(i)] * (Data.Qti(t, i, a) - s[i]));
                    }

                    if (temp >= Tolerance && reduced_cost < temp)
                    {
                        reduced_cost = temp;
                        temp_s = s;
                        deci_a = a;
                    }
                }
            }
            System.Console.WriteLine("CG({0})求解结束\t目标值为:{1}", t, reduced_cost);
            if (reduced_cost > 0)
            {
                return false;
            }
            else
            {
                return true;
            }
        }
        private bool RCG(int t, out IALPState temp_s, out IMDPDecision deci_a)
        {
            temp_s = null;
            deci_a = null;

            //System.Console.WriteLine("--------开始生成问题:{0}-----", System.DateTime.Now.Millisecond);
            #region 创建目标函数
            SubModel.Remove(SubModel.GetObjective());
            SubModel.AddMaximize(subObject[1][t]);
            #endregion

            foreach (IALPResource re in Data.RS)
            {
                constraint_sub2[Data.RS.IndexOf(re)].SetBounds(0, (Data.InitialState as IALPState)[re]);
            }

            //System.Console.WriteLine("--------生成问题结束:{0}-----", System.DateTime.Now.Millisecond);
            if (SubModel.Solve())
            {
                //System.Console.WriteLine("--------求解问题结束:{0}-----", System.DateTime.Now.Millisecond);
                //System.Console.WriteLine("RCG({0})求解结束\t目标值为:{1}", t, SubModel.ObjValue<0.001?0: SubModel.ObjValue);
                if (SubModel.ObjValue <= Tolerance)
                {
                    return true;
                }
                else
                {
                    //print("{0}问题违反程度{1}", t, (SubModel.ObjValue - Tolerance).ToString());
                    #region  用h,r生成一个State
                    for (int _h = 0; _h < h.Count(); _h++)
                    {
                        double temp_h = SubModel.GetValue(h[_h]);
                        if (temp_h == 1)
                        {
                            deci_a = Data.DS[_h];
                            break;
                        }
                    }

                    Dictionary<IALPResource, int> dic = new Dictionary<IALPResource, int>();
                    for (int _r = 0; _r < r.Count(); _r++)
                    {
                        dic.Add(Data.RS[_r], (int)SubModel.GetValue(r[_r]));
                    }
                    temp_s = Data.CreateOrFind(dic);
                    #endregion
                    return false;
                }
            }
            else
            {
                throw new System.Exception("子问题无解");
            }
        }
    }

    internal class StateActive : IEquatable<StateActive>
    {
        public int T;
        public IALPState S;
        public IMDPDecision D;
        public INumVar Var;

        public bool Equals(StateActive other)
        {
            return this.T == other.T && this.S.Equals(other.S) && this.D.Equals(other.D);
        }
    }
    internal class StateActiveSpace : HashSet<StateActive>
    {
        public new bool Add(StateActive sa)
        {
            if (this.FirstOrDefault(i => i.Equals(sa)) == null)
            {
                base.Add(sa);
                return true;
            }
            else
            {
                return false;
            }
        }
        public List<StateActive> RemoveState(IALPState other)
        {
            List<StateActive> list = this.Where(i => !i.S.Within(other)).ToList();
            foreach (StateActive sa in list)
            {
                Remove(sa);
            }
            return list;
        }
        public void GetBasis(Cplex cplex)
        {
            if (cplex.GetStatus() == Cplex.Status.Optimal)
            {
                //var a = this.Where(i => cplex.GetBasisStatus(i.Var) == Cplex.BasisStatus.NotABasicStatus).ToList();
                var b = this.Where(i => cplex.GetBasisStatus(i.Var) != Cplex.BasisStatus.Basic).ToList();
                foreach (StateActive sa in b)
                {
                    //cplex.Delete(sa.Var);
                    this.Remove(sa);
                }
            }
        }
        public StateActiveSpace Clone()
        {
            StateActiveSpace sas = new StateActiveSpace();
            sas.UnionWith(this);
            return sas;
        }
    }
}
