using com.foxmail.wyyuan1991.NRM.ALP;
using com.foxmail.wyyuan1991.NRMSolver;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Schedulers;

namespace com.foxmail.wyyuan1991.NRMSolver
{
    /// <summary>
    /// 描述NRM求解器基本结构
    /// </summary>
    public abstract class NRM_Solver
    {
        public virtual IALPFTMDP Data { get; set; }//数据集合引用

        #region Parallel 并行计算支持
        // Create a scheduler 
        protected LimitedConcurrencyLevelTaskScheduler lcts;
        protected CancellationTokenSource cts = new CancellationTokenSource();
        protected List<Task> tasks = new List<Task>();
        // Create a TaskFactory and pass it our custom scheduler. 
        protected TaskFactory factory;
        private int m_NumOfThreads = 2;
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

        #region SetOut 输出设定
        private TextWriter m_TextWriter;
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
        protected void print(string str)
        {
            if (this.m_TextWriter != null)
            {
                m_TextWriter.WriteLine(str);
            }
        }
        protected void print(string format, params object[] arg)
        {
            if (this.m_TextWriter != null)
            {
                m_TextWriter.WriteLine(format, arg);
            }
        }
        #endregion

        #region Dynamic Curve 动态投标价格表达
        public double[][] BidPrice;//[时间][资源编号] 的投标价格
        public event EventHandler<IterationCompletedEventArgs> CalCompleted;
        protected virtual void SendEvent(IterationCompletedEventArgs args)
        {
            if (CalCompleted != null)
            {
                CalCompleted(this, args);
            }
        }
        private double lipschitz = 0.001;
        protected int[] findturnningpoint()
        {
            int[] tp = new int[Data.RS.Count];
            double[][] R = Rotate(BidPrice);
            foreach (IALPResource re in Data.RS)
            {
                tp[Data.RS.IndexOf(re)] = tstar(R[Data.RS.IndexOf(re)]);             
            }
            return tp;
        }
        protected int tstar(double[] d)
        {
            for (int i = 1; i < d.Length - 1; i++)
            {
                if (d[i - 1] - d[i] < d[i] * lipschitz && d[i] - d[i + 1] > d[i] * lipschitz) return i;
            }
            return d.Length - 1;
        }
        protected static double[][] Rotate(double[][] array)
        {
            int x = array.GetUpperBound(0); //一维 
            int y = array[0].GetUpperBound(0); //二维 
            double[][] newArray = new double[y + 1][]; //构造转置二维数组
            for (int i = 0; i <= y; i++)
            {
                newArray[i] = new double[x + 1];
                for (int j = 0; j <= x; j++)
                {
                    newArray[i][j] = array[j][i];
                }
            }
            return newArray;
        }
        public void SaveBidPrice(string path)//保存投标价格
        {
            FileStream fs = new FileStream(path, FileMode.Create);
            StreamWriter sw = new StreamWriter(fs);
            for (int i = 0; i < BidPrice.Length; i++)
            {
                for (int j = 0; j < BidPrice[i].Length; j++)
                {
                    if (j == BidPrice[i].Length - 1)
                    {
                        sw.Write(BidPrice[i][j]);
                    }
                    else
                    {
                        sw.Write(BidPrice[i][j] + ",");
                    }
                }
                sw.WriteLine();
            }
            //清空缓冲区
            sw.Flush();
            //关闭流
            sw.Close();
            fs.Close();
        }
        #endregion
    }

    /// <summary>
    /// 迭代完成事件参数
    /// </summary>
    public class IterationCompletedEventArgs : EventArgs
    {
        public double[][] BidPrice;//投标价格
        public int[] TurnningPoint;//不同资源拐点时间
        public double ObjValue;//目标值
        public int Alpha;//参数α取值
    }
}
