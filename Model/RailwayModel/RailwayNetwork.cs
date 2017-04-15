using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 将来可以用LPD项目中的借口替代这些内容，或者整个项目迁入LPD
/// </summary>
namespace com.foxmail.wyyuan1991.NRM.RailwayModel
{
    public class TrainSection : IEquatable<TrainSection>
    {
        public string StartStation { get; set; }
        public string EndStation { get; set; }

        //实现IEquatable 比较两个区间是否相同
        public bool Equals(TrainSection other)
        {
            if (other.StartStation == StartStation && other.EndStation == EndStation) return true;
            else return false;
        }
        public override string ToString()
        {
            return StartStation + "->" + EndStation;
        }
    }

    public class StopStation
    {
        private Train _Train;
        public StopStation(Train t)
        {
            _Train = t;
        }
        public Train TrainBelong { get { return _Train; } }

        /// <summary>
        /// 车站名
        /// </summary>
        public string StationName { get; set; }
        /// <summary>
        /// 到站时间
        /// </summary>
        public DateTime ArrTime { get; set; }
        /// <summary>
        /// 离站时间
        /// </summary>
        public DateTime DepTime { get; set; }
        /// <summary>
        /// 里程
        /// </summary>
        public double MileStone { get; set; }
    }

    public class Train
    {
        double priceratio = 1;

        public Train()
        {
            StaList = new List<StopStation>();
            SecList = new List<TrainSection>();
        }

        /// <summary>
        /// 停站序列
        /// </summary>
        public List<StopStation> StaList { get; set; }
        /// <summary>
        /// 区间序列
        /// </summary>
        public List<TrainSection> SecList { get;set; }//Section List
       
        /// <summary>
        ///票价等级（默认为1）
        /// </summary>
        public double PriceRatio
        {
            get
            {
                return priceratio;
            }

            set
            {
                priceratio = value;
            }
        }
        /// <summary>
        /// 车次
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// 定员
        /// </summary>
        public int Cap { get; set; }

        /// <summary>
        /// 重新生成区间序列（通过停站生成）
        /// </summary>
        public List<TrainSection> GenSecList()
        {
            SecList.Clear();
            for (int i = 0; i < StaList.Count - 1; i++)
            {
                SecList.Add(new TrainSection() {
                    //Train=this,
                    StartStation = StaList[i].StationName,
                    EndStation = StaList[i + 1].StationName,
                    //Capacity = this.Cap
                });
            }
            return SecList;
        }
        public TrainSection GetResbyStartStaOrder(int i)
        {
            return SecList[i];
        }

        /// <summary>
        /// 是否经过车站
        /// </summary>
        /// <param name="sta">车站名</param>
        public bool PassStation(string sta)
        {
            return this.StaList.Exists(i => i.StationName == sta);
        }
        /// <summary>
        /// 获取从o站到d站的旅行时间
        /// </summary>
        /// <param name="o">出发车站</param>
        /// <param name="d">到达车站</param>
        /// <returns>时间段</returns>
        public TimeSpan TravelTime(string o, string d)
        {
            if (PassStation(o) && PassStation(d))
            {
                TimeSpan ts = StaList.Find(i => i.StationName == d).ArrTime - StaList.Find(i => i.StationName == o).DepTime;
                if (ts.TotalMinutes > 0) return ts;
            }
            return TimeSpan.MaxValue;
        }
        /// <summary>
        /// 与列车t共同的停站
        /// </summary>
        /// <param name="t">输入车站</param>
        /// <returns>车站集合</returns>
        public List<string> InteractWith(Train t)
        {
            List<string> list = new List<string>();
            foreach (StopStation s in this.StaList)
            {
                foreach (StopStation st in t.StaList)
                {
                    if (s.StationName == st.StationName)
                    {
                        list.Add(s.StationName);
                    }
                }
            }
            return list;
        }
        /// <summary>
        /// 在到达s站的时间
        /// </summary>
        /// <param name="s">输入车站</param>
        /// <returns></returns>
        public DateTime getArrTime(string s)
        {
            return StaList.Find(i => i.StationName == s).ArrTime;
        }
        /// <summary>
        /// 离开s站的时间
        /// </summary>
        /// <param name="s">输入车站</param>
        /// <returns></returns>
        public DateTime getDepTime(string s)
        {
            return StaList.Find(i => i.StationName == s).DepTime;
        }
        /// <summary>
        /// 获取车站在停站序列中的顺序号，不存在为-1
        /// </summary>
        /// <param name="s">输入车站</param>
        /// <returns></returns>
        public int IndexofStation(string s)
        {
            if (StaList.Exists(k => k.StationName == s))
            {
                return StaList.FindIndex(i => i.StationName == s);
            }
            else
            {
                return -1;
            }

        }
        public override string ToString()
        {
            return Name;
        }
    }
    public class Timetable : List<Train>
    {
        public Timetable() { }

        /// <summary>
        /// 区间数量
        /// </summary>
        public int NumofSections
        {
            get
            {
                return this.Sum(i => i.SecList.Count);
            }
        }
    }
}
