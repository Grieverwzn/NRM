using com.foxmail.wyyuan1991.NRM.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using com.foxmail.wyyuan1991.MDP;
using com.foxmail.wyyuan1991.NRM.ALP;

namespace com.foxmail.wyyuan1991.NRM.RailwayModel
{
    public class MarketSegment : IMarketSegment, IChoiceAgent
    {
        public int MSID { get; set; }
        public string Description { get; set; }
        public TimeFunction Lamada { get; set; }
        public List<TimeValue> AggLamada;
        List<Route> m_RouteList;
        Dictionary<Route, double> m_ConsiderationDic = new Dictionary<Route, double>();

        public double Retreat { get; set; }
        public List<Route> RouteList
        {
            get
            {
                if (m_RouteList == null)
                {
                    m_RouteList = m_ConsiderationDic.Keys.ToList();
                }
                return m_RouteList;
            }
        }
        public Dictionary<Route, double> ConsiderationDic
        {
            get
            {
                return m_ConsiderationDic;
            }
        }

        public string OriSta { get; set; }
        public string DesSta { get; set; }
        public double ValueOfTime { get; set; }
        public int Transfer { get; set; }

        public override string ToString()
        {
            return this.Description;
        }

        public List<IProduct> Select(List<IProduct> ProList, double u)
        {
            List<Route> temp = RouteList.Where(r => r.TrueForAll(i => ProList.Contains(i))).ToList();
            double totalunity = temp.Sum(i => ConsiderationDic[i]) + Retreat;
            double v = 0;
            for (int i = 0; i < temp.Count; i++)
            {
                v += ConsiderationDic[temp[i]] / totalunity;
                if (v >= u) return temp[i].ToList<IProduct>();
            }
            return null;
        }

    }
    public class Market : List<MarketSegment>, IMarket
    {
        private List<OD> _ODList;
        public List<OD> ODList {
            get
            {
                if (_ODList == null)
                {
                    _ODList = new List<OD>();

                    var a = (this as List<MarketSegment>).GroupBy(x => new
                    {
                        x.OriSta,
                        x.DesSta
                    }).Select(x => x.FirstOrDefault()).ToList();

                    foreach (MarketSegment ms in a)
                    {
                        _ODList.Add(new OD { OriSta = ms.OriSta, DesSta = ms.DesSta });
                    }
                }
                return _ODList;
            }
        }

        public List<MarketSegment> getMSbyOD(OD o)
        {
            return (this as List<MarketSegment>).Where(x => x.OriSta == o.OriSta && x.DesSta == o.DesSta).ToList();
        }

        public List<TimeValue> AggRo;
        public TimeFunction Ro { get; set; }
        IMarketSegment IMarket.this[int index]
        {
            get
            {
                return this.Find(i => i.MSID == index);
            }
        }
        IEnumerator<IMarketSegment> IEnumerable<IMarketSegment>.GetEnumerator()
        {
            foreach (MarketSegment p in this)
            {
                yield return p;
            }
        }
    }

    public class OD
    {
        public string OriSta { get; set; }
        public string DesSta { get; set; }
        public double Num { get; set; }
    }
    public class TimeValue
    {
        public int Time { get; set; }//开始时刻
        public double value { get; set; }
    }

    public class Resource : IResource, IALPResource
    {
        //TrainSection
        public Train Train { get; set; }
        public string StartStation { get; set; }
        public string EndStation { get; set; }

        public int ResID { get; set; }
        public string Description { get; set; }
        public int Capacity { get; set; }
        public string Tag { get; set; }

        private List<IMetaResource> m_MetaResList = new List<IMetaResource>();
        public List<IMetaResource> MetaResList
        {
            get
            {
                return m_MetaResList;
            }

            set
            {
                m_MetaResList = value;
            }
        }

        //实现IEquatable 比较两个区间是否相同
        public bool Equals(Resource other)
        {
            if (other.StartStation == StartStation && other.EndStation == EndStation) return true;
            else return false;
        }
        public override string ToString()
        {
            return "Train:" + Train.ToString() + ",Section:" + StartStation + "->" + EndStation;
        }

        public IMDPState Clone()
        {
            throw new NotImplementedException();
        }
        public bool Equals(IMDPState other)
        {
            throw new NotImplementedException();
        }
    }
    public class ResourceSet : List<Resource>, IResourceSet, IALPResourceSpace
    {
        #region 实现IALPResourceSpace
        IALPResource IALPResourceSpace.this[int index]
        {
            get
            {
                return base[index];
            }
        }
        public int IndexOf(IALPResource item)
        {
            return base.IndexOf((item as Resource));//可能出现错误
        }
        IEnumerator<IALPResource> IEnumerable<IALPResource>.GetEnumerator()
        {
            foreach (Resource p in this)
            {
                yield return p;
            }
        }
        #endregion

        #region 实现IResourceSpace
        IEnumerator<IResource> IEnumerable<IResource>.GetEnumerator()
        {
            foreach (Resource p in this)
            {
                yield return p;
            }
        }
        #endregion
    }

    public class Product : List<Resource>, IProduct
    {
        #region 实现IProduct
        public int ProID { get; set; }
        public string Description { get; set; }
        public double Fare { get; set; }

        public bool Contains(IResource r)
        {
            return base.Contains(r as Resource);
        }
        IEnumerator<IResource> IEnumerable<IResource>.GetEnumerator()
        {
            foreach (Resource p in this)
            {
                yield return p;
            }
        }
        #endregion

        public Train Train { get; set; }
        public string StartStation { get; set; }
        public string EndStation { get; set; }
        public DateTime StartTime
        {
            get
            {
                return Train.StaList.FirstOrDefault(i => i.StationName == StartStation).DepTime;
            }
        }
        public DateTime EndTime
        {
            get
            {
                return Train.StaList.FirstOrDefault(i => i.StationName == EndStation).ArrTime;
            }
        }
        public TimeSpan TimeNeeded
        {
            get
            {
                return Train.TravelTime(StartStation, EndStation);
            }
        }
        //public bool IsVirtualProduct//是否为虚拟产品（换乘）
        //{
        //    get
        //    {
        //        return StartStation == EndStation;
        //    }
        //}   

        public override string ToString()
        {
            return StartStation + "->" + EndStation + " via " + Train.ToString();
        }

    }
    public class ProductSet : List<Product>, IProductSet
    {
        IProduct IProductSet.this[int index]
        {
            get
            {
                return base[index];
            }
        }
        IEnumerator<IProduct> IEnumerable<IProduct>.GetEnumerator()
        {
            foreach (Product p in this)
            {
                yield return p;
            }
        }
    }

    public class Route : List<Product>
    {
        public Route()
        {

        }
        public Route(List<Product> prolist)
        {
            this.AddRange(prolist);
            StartStation = this.First().StartStation;
            EndStation = this.Last().EndStation;
            TrainList = this.Select(i => i.Train).ToList();
        }

        #region Attributes
        public string StartStation { get; set; }
        public string EndStation { get; set; }
        public List<Train> TrainList { get; set; }
        public int RouteID { get; set; }
        #endregion

        #region Methods
        /// <summary>
        /// 路径花费
        /// </summary>
        public double TicketPrice
        {
            get
            {
                return this.Sum(i => i.Fare);
            }
        }
        /// <summary>
        /// 路径所花费时间
        /// </summary>
        public TimeSpan TimeCost
        {
            get
            {
                return this.Last().EndTime - this.First().StartTime;
            }
        }
        public override string ToString()
        {
            string s = "From " + StartStation + " to " + EndStation + ": | ";
            foreach (Product p in this)
            {
                s += p.ToString() + " | ";
            }
            return s;
        }
        public bool Equals(Route p)
        {
            if (this.SequenceEqual(p)) return true;
            else return false;
        }
        #endregion
    }

    public class Seat : ISeat
    {
        public int SeatID { get; set; }
        public int IDinTrain { get; set; }//在同一列车上排序
        public string Tag { get; set; }
        public List<IMetaResource> MetaResList
        {
            get
            {
                return metaResList;
            }

            set
            {
                metaResList = value;
            }
        }
        private List<IMetaResource> metaResList = new List<IMetaResource>();
        public IMetaResource getMetaByRes(IResource r)
        {
            return metaResList.FirstOrDefault(i => (i as MetaResource).Resource == r);
        }
    }
    public class SeatSet : List<Seat>, ISeatSet
    {
        IEnumerator<ISeat> IEnumerable<ISeat>.GetEnumerator()
        {
            foreach (Seat p in this)
            {
                yield return p;
            }
        }
    }

    public class MetaResource : IMetaResource
    {
        public string Name { get; set; }
        public int ResID { get; set; }
        public int SeatID { get; set; }
        public Seat Seat { get; set; }
        public Resource Resource { get; set; }
        public string Description { get; set; }
    }
    public class MetaResourceSet : List<IMetaResource>, IMetaResourceSet
    {
        public ResourceSet ResSet { get; set; }
        public SeatSet SeatSet { get; set; }

        public void Add(Seat seat)
        {
            this.SeatSet.Add(seat);
            this.AddRange(seat.MetaResList);
        }
        public void Remove(Seat seat)
        {
            this.RemoveAll(i => seat.MetaResList.Contains(i));
            this.SeatSet.Remove(seat);
        }

        IEnumerator<IMetaResource> IEnumerable<IMetaResource>.GetEnumerator()
        {
            foreach (MetaResource p in this)
            {
                yield return p;
            }
        }
    }
}
