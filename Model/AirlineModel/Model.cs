using System.Collections.Generic;
using System.Linq;
using com.foxmail.wyyuan1991.NRM.Common;
using com.foxmail.wyyuan1991.NRM.ALP;
using com.foxmail.wyyuan1991.MDP;
using System;

/// <summary>
/// Models of airline passenger Transporation
/// </summary>
namespace com.foxmail.wyyuan1991.NRM.AirlineModel
{
    public class MarketSegment : IMarketSegment,IChoiceAgent
    {
        List<IProduct> m_ProList;
        Dictionary<IProduct, double> m_ConsiderationDic = new Dictionary<IProduct, double>();

        public int MSID { get; set; }
        public string Description { get; set; }
        public List<IProduct> ProList
        {
            get
            {
                if (m_ProList == null)
                {
                    m_ProList = m_ConsiderationDic.Keys.ToList();
                }
                return m_ProList;
            }
        }
        public TimeFunction Lamada { get; set; }

        public double Retreat { get; set; }

        public Dictionary<IProduct, double> ConsiderationDic
        {
            get
            {
                return m_ConsiderationDic;
            }
        }


        //选择行为
        public List<IProduct> Select(List<IProduct> OpenProList, double u)
        {
            List<IProduct> temp = ProList.Intersect(OpenProList).ToList();
            double totalunity = temp.Sum(i => ConsiderationDic[i]) + Retreat;
            double v = 0;
            for(int i=0;i<temp.Count;i++)
            {
                v += ConsiderationDic[temp[i]];
                if (v >= u) return new List<IProduct>() { temp[i] };
            }
            return null;
        }
    }
    public class Market : List<MarketSegment>, IMarket
    {
        IMarketSegment IMarket.this[int index]
        {
            get
            {
                return this.Find(i => i.MSID == index);
            }
        }

        public TimeFunction Ro { get; set; }

        IEnumerator<IMarketSegment> IEnumerable<IMarketSegment>.GetEnumerator()
        {
            foreach (MarketSegment p in this)
            {
                yield return p;
            }
        }
    }

    public class Resource : IResource,IALPResource
    {
        public int ResID { get; set; }
        public string Description { get; set; }
        public int Capacity { get; set; }

        public override string ToString()
        {
            return Description;
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
        public int ProID { get; set; }
        public string Description { get; set; }
        public double Fare { get; set; }

        public override string ToString()
        {
            return Description;
        }
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

 
    }
    public class ProductSet: List<Product>,IProductSet
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

}
