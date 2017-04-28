using System;
using System.Collections.Generic;
using System.Linq;

namespace com.foxmail.wyyuan1991.NRM.Common
{
    /// <summary>
    /// 系统的剩余资源状态，对于每个资源都有一个整数表示剩余数量
    /// </summary>
    public class MetaResouceState
    {
        public Dictionary<IResource, int> ResDic = new Dictionary<IResource, int>();
        public Dictionary<IMetaResource, bool> MetaResDic = new Dictionary<IMetaResource, bool>();//false表示未售出

        public MetaResouceState(MetaResouceState rs)
        {
            foreach (IMetaResource r in rs.MetaResDic.Keys)
            {
                MetaResDic.Add(r, rs.MetaResDic[r]);
            }
            UpdateResDic();
        }
        public MetaResouceState()
        {

        }

        public int GetRemainNum(IResource r)
        {
            if (ResDic.Keys.Contains(r))
            {
                return ResDic[r];
            }
            return 0;
        }
        public void Add(IResource r,int i)
        {
            ResDic.Add(r, i);
        }
        /// <summary>
        /// 开放p产品需要的资源是否足够。
        /// </summary>
        public bool CanSupportProduct(IProduct p)
        {
            foreach (IResource r in p)
            {
                int x = -1;
                if (!(ResDic.TryGetValue(r, out x) && x > 0)) return false;
            }
            return true;
        }       
        public void UpdateAfterSelling(List<Ticket> rList)
        {
            if (rList == null||rList.Count==0) return;
            foreach (Ticket p in rList)
            {
                foreach (IMetaResource r in p.MetaResList)
                {
                    if (MetaResDic.Keys.Contains(r) && !MetaResDic[r])
                    {
                        MetaResDic[r] = true;
                    }
                    else
                    {
                        throw new Exception("正在出售不可用资源!");
                    }
                }
            }
            UpdateResDic();
        }
        public void UpdateResDic()//从元数据更新目前ResDic
        {
            List<IResource> list = new List<IResource>();
            list.AddRange(ResDic.Keys);
            foreach (IResource r in list)
            {
                ResDic[r] = r.MetaResList.Sum(i => MetaResDic[i] ? 0 : 1);
            }
        }
    }
    public class Ticket
    {
        private List<IMetaResource> m_MetaResList = new List<IMetaResource>();
        public IProduct Product { get; set; }
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
    }
}