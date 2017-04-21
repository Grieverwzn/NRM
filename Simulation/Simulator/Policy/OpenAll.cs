using System;
using System.Collections.Generic;
using com.foxmail.wyyuan1991.NRM.Common;
using com.foxmail.wyyuan1991.NRM.RailwayModel;
using System.Linq;

namespace com.foxmail.wyyuan1991.NRM.Simulator
{
    public class OpenAllStrategy : IController
    {
        private NRMDataAdapter m_DataAdapter;
        public NRMDataAdapter DataAdapter
        {
            get
            {
                return m_DataAdapter;
            }

            set
            {
                m_DataAdapter = value;
            }
        }

        public List<IProduct> OpenProductList(int time, ResouceState r, IConOL cl)
        {
            List<IProduct> OpenProductList = new List<IProduct>();
            foreach (IProduct p in DataAdapter.ProSpace)
            {
                if (r.CanSupportProduct(p))
                    OpenProductList.Add(p);
            }
            return OpenProductList;
        }

        public void Init()
        {
            ;//Do Nothing
        }

        public void Update()
        {
            ;//Do Nothing
        }
        public IConOL GenConOL()
        {
            return null;
        }
        public List<Ticket> PrintTickets(ResouceState rs, List<IProduct> pro, IConOL cl)
        {
            List<Ticket> res = new List<Ticket>();
            foreach (IProduct p in pro)//一张一张卖
            {
                int n = p.Min(i => i.MetaResList.Count);
                for (int i = 0; i < n; i++)
                {
                    if (p.All(j => !rs.MetaResDic[j.MetaResList[i]]))
                    {
                        Ticket t = new Ticket() { Product = p };
                        foreach (IResource r in p)
                        {
                            t.MetaResList.Add(r.MetaResList[i]);
                        }
                        res.Add(t);
                        break;
                    }
                }
            }
            return res;
        }
    }
}
