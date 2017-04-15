using com.foxmail.wyyuan1991.MDP;
using com.foxmail.wyyuan1991.NRM.ALP;
using ILOG.Concert;
using ILOG.CPLEX;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Schedulers;

namespace com.foxmail.wyyuan1991.NRMSolver
{
    /// <summary>
    /// CD1模型的DW分解+列生成方法
    /// </summary>
    public class CD1_DW_Parallel_Solver : IDisposable
    {
        #region Model variables      
        //主问题模型
        public Cplex RMPModel = new Cplex();
        //子问题模型
        public Cplex SubModel = new Cplex();
        //主问题第一类对偶变量
        public double[][] DualValue1;
        //主问题第二类对偶变量
        public double[] DualValue2;
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
        StateActiveSpace sas = new StateActiveSpace();
        //是否达到最优
        bool IsOptimal = false;
        //改进容许值
        double threshold = 0.1;
        //允许在容许值外的次数
        int ObeyTime = 10;
        //初始时间
        private int m_CurrentTime = 0;
        #endregion

        #region Setting Variables
        //输出
        private TextWriter m_TextWriter;
        private int m_NumOfThreads = 2;
        // Create a scheduler 
        LimitedConcurrencyLevelTaskScheduler lcts;
        CancellationTokenSource cts = new CancellationTokenSource();
        List<Task> tasks = new List<Task>();
        // Create a TaskFactory and pass it our custom scheduler. 
        TaskFactory factory;
        #endregion

        #region Attributes
        public IALPFTMDP Data { get; set; }
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
        public TextWriter SolverTextWriter
        {
            get
            {
                return m_TextWriter;
            }
            set
            {
                m_TextWriter = value;
            }
        }
        public int NumOfThreads
        {
            get { return m_NumOfThreads; }
            set
            {
                m_NumOfThreads = value;
                lcts = new LimitedConcurrencyLevelTaskScheduler(m_NumOfThreads);
                factory = new TaskFactory(lcts);
            }
        }
        #endregion

        public CD1_DW_Parallel_Solver()
        {
        }
        public CD1_DW_Parallel_Solver(IALPFTMDP _aff)
        {
            Data = _aff;
        }

        #region SetOut
        private void print(string str)
        {
            if (this.m_TextWriter != null)
            {
                m_TextWriter.WriteLine(str);
            }
        }
        private void print(string format, params object[] arg)
        {
            if (this.m_TextWriter != null)
            {
                m_TextWriter.WriteLine(format, arg);
            }
        }
        #endregion

        public void Init()//Reset the RMP Model 
        {
            m_CurrentTime = 0;
            print("--------{0}初始化问题开始--------", System.DateTime.Now.ToLongTimeString());
            InitRMPModel();
            InitRCGModel();
            sas.Clear();
            //SubModel = new Cplex();
            print("--------{0}初始化问题完成--------", System.DateTime.Now.ToLongTimeString());
        }
        public void DoCalculate()//Calculate the Bid-Price of current time
        {
            print("--------{0}开始求解--------", System.DateTime.Now.ToLongTimeString());
            IsOptimal = false;
            CreateFeasibleSolution();

          
            //检验是否有违背State的变量
            foreach (StateActive sa in sas.RemoveState(Data.InitialState as IALPState))
            {
                RMPModel.Delete(sa.Var);
            }

            double tempObj = 0;
            int tol = ObeyTime;

            for (int iter = 1; ; iter++)
            {
                print("--------{1}开始进行第{0}次循环--------", iter, System.DateTime.Now.ToLongTimeString());
                if (RMPModel.Solve())
                {
                    if (RMPModel.GetObjValue() - tempObj < threshold)
                    {
                        tol--;
                    }
                    else
                    {
                        tol = ObeyTime;
                    }
                    tempObj = RMPModel.GetObjValue();
                }
                IsOptimal = true;
                print("--------{1}目标值:{0}--------", RMPModel.ObjValue, System.DateTime.Now.ToLongTimeString());

                print("--------{0}更新参数开始--------", System.DateTime.Now.ToLongTimeString());
                UpdateDualValues();

                print("--------{0}更新参数结束--------", System.DateTime.Now.ToLongTimeString());
                if (tol < 0)
                {
                    print("--------{0}算法达到终止条件而退出--------", System.DateTime.Now.ToLongTimeString());
                    break;
                }

                for (int t = CurrentTime; t < Data.TimeHorizon; t++)
                {
                    int iteration = t;
                    Task ta = factory.StartNew(() =>
                    {
                        //print("Problem {0} on thread {1}   ", iteration, Thread.CurrentThread.ManagedThreadId);
                        bool temp;
                        IALPState temp_s = null;
                        IMDPDecision deci_a = null;
                        lock (SubModel)
                        {
                            GenSubModelObj(iteration);
                            temp = RCG(iteration, out temp_s, out deci_a);
                        }
                        lock(RMPModel)
                        {
                            lock (sas)
                            {
                                if (!temp)
                                {
                                    AddCol(iteration, temp_s, deci_a);
                                }
                            }
                        }
                        IsOptimal = temp && IsOptimal;
                        //print("#{0}第{1}个子问题已经解决！", System.DateTime.Now.ToLongTimeString(), iteration);
                    }, cts.Token);
                    tasks.Add(ta);
                }
                Task.WaitAll(tasks.ToArray());
                tasks.Clear();
               

             

                if (IsOptimal)
                {
                    print("#已经达到最优！");
                    UpdateDualValues();
                    break;
                }
            }

        }

        private void CreateFeasibleSolution()
        {
            for (int t = CurrentTime; t < Data.TimeHorizon; t++)
            {
                foreach (IALPState s in Data.SS)
                {
                    AddCol(t, s, (Data.DS as IALPDecisionSpace).CloseAllDecision());
                }
            }
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
            Parallel.For(0, Data.TimeHorizon, i =>
            //for (int i = 0; i < Data.TimeHorizon; i++)
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
            });
            #endregion

            //RMPModel.SetParam(Cplex.LongParam.RootAlgorithm, 1);
            RMPModel.SetOut(SolverTextWriter);
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
            Parallel.For(0, Data.TimeHorizon, t =>
            //for (int t = 0; t < Data.TimeHorizon; t++)
            {
                subObject[0][t] = SubModel.NumExpr();
                foreach (IMDPDecision a in Data.DS)
                {
                    subObject[0][t] = SubModel.Sum(subObject[0][t],
                        SubModel.Prod(Data.Rt(t, a), h[Data.DS.IndexOf(a)]));
                }
            });
            #endregion

            SubModel.SetOut(null);
        }
        private bool AddCol(int t, IALPState s, IMDPDecision a)//Add a column into RMP model
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
            INumVar var = RMPModel.NumVar(col, 0, double.MaxValue, NumVarType.Float);
            sas.Add(new StateActive() { S = s, D = a, Var = var });
            return true;
        }
        private void UpdateDualValues()
        {
            Parallel.For(0, Data.TimeHorizon, t =>
            //for (int t = 0; t < Data.TimeHorizon; t++)
            {
                foreach (IALPResource re in Data.RS)
                {
                    DualValue1[t][Data.RS.IndexOf(re)] = RMPModel.GetDual(constraint1[t][Data.RS.IndexOf(re)]);
                }
                DualValue2[t] = RMPModel.GetDual(constraint2[t]);
            });
        }
        private void GenSubModelObj(int t)//Generate the objective expression for the submodel
        {
            if (t >= m_CurrentTime)
            {
                INumExpr expr = SubModel.NumExpr();
                foreach (IALPResource i in Data.RS)
                {
                    expr = SubModel.Sum(expr,
                            SubModel.Prod(-DualValue1[t][Data.RS.IndexOf(i)], r[Data.RS.IndexOf(i)]));

                    if (t < Data.TimeHorizon - 1)
                    {
                        expr = SubModel.Sum(expr,
                                SubModel.Prod(DualValue1[t + 1][Data.RS.IndexOf(i)], r[Data.RS.IndexOf(i)]));
                    }
                }
                foreach (IMDPDecision a in Data.DS)
                {
                    double temp = 0;
                    if (t < Data.TimeHorizon - 1)
                    {
                        temp += Data.RS.Sum(i => -DualValue1[t + 1][Data.RS.IndexOf(i)] * Data.Qti(t, i, a));
                        expr = SubModel.Sum(expr,
                            SubModel.Prod(temp, h[Data.DS.IndexOf(a)]));//t 还是t-1
                    }
                }
                expr = SubModel.Sum(expr, -DualValue2[t]);
                subObject[1][t] = SubModel.Sum(subObject[0][t], expr);
                SubModel.Delete(expr);
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

        public void Dispose()
        {
            cts.Dispose();
        }
    }
}
