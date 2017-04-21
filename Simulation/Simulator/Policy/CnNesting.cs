using System;
using System.Collections.Generic;
using com.foxmail.wyyuan1991.NRM.Common;
using System.Linq;
using com.foxmail.wyyuan1991.NRM.RailwayModel;
using System.IO;
using System.Text;

namespace com.foxmail.wyyuan1991.NRM.Simulator
{
    public class CnNesting : IController
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

        public string Path { get; set; }//存储读取的方案

        public IConOL GenConOL()
        {
            CnNestingOL res = new CnNestingOL();

            string[] a; string temp = "";
            BucketGroup train = new BucketGroup();
            StreamReader sr = new StreamReader(Path, Encoding.Default);
            for (int i = 0; sr.EndOfStream == false; i++)
            {
                string line = sr.ReadLine();
                a = line.Split(';');
                if (a[0] != temp)
                {
                    if (temp!="") res.TrainGroupList.Add(train);
                    train = new BucketGroup();
                    temp = a[0];
                }
                if (a[1] == "0")
                {
                    train.LBucket.Seats.AddRange((m_DataAdapter.MetaResSpace.SeatSet as List<Seat>).Where(k => k.IDinTrain >= Convert.ToInt32(a[2]) && k.IDinTrain <= Convert.ToInt32(a[3]) && k.Tag == a[0]));
                    string[] aa = a[4].Split(',');
                    foreach (string s in aa)
                    {
                        train.LBucket.ProList.Add((m_DataAdapter.ProSpace as List<Product>).First(proid => proid.ProID == Convert.ToInt32(s)));
                    }
                    train.ProList.AddRange(train.LBucket.ProList);
                    res.NeedToRefresh.Add(train.LBucket);
                }else
                {
                    Bucket bucket = new Bucket();
                    bucket.Seats.AddRange((m_DataAdapter.MetaResSpace.SeatSet as List<Seat>).Where(k => k.IDinTrain >= Convert.ToInt32(a[2]) && k.IDinTrain <= Convert.ToInt32(a[3]) && k.Tag == a[0]));
                    string[] aa = a[4].Split(',');
                    foreach (string s in aa)
                    {
                        bucket.ProList.Add((m_DataAdapter.ProSpace as List<Product>).First(proid=>proid.ProID==Convert.ToInt32(s)));
                    }
                    train.Buckets.Add(bucket);
                    res.NeedToRefresh.Add(bucket);
                }
            }
            res.TrainGroupList.Add(train);
            return res;
        }

        public List<IProduct> OpenProductList(int time, ResouceState r, IConOL cl)
        {
            return (cl as CnNestingOL).OpenProductList(r);
        }

        public List<Ticket> PrintTickets(ResouceState rs, List<IProduct> pro, IConOL cl)
        {
            //(cl as CnNestingOL).NeedToRefresh.Clear();
            List<Ticket> res = new List<Ticket>();
            foreach (IProduct p in pro)
            {
                //从一个包含产品的列车中打印车票
                BucketGroup tg = (cl as CnNestingOL).TrainGroupList.FirstOrDefault(i => i.ProList.Contains(p));
                List<Bucket> temp = null;
                res.Add(tg.PrintTicket(rs, p, out temp));
                (cl as CnNestingOL).NeedToRefresh.AddRange(temp);
            }
            return res;
        }
    }
    //在线控制
    public class CnNestingOL : IConOL
    {
        internal List<Bucket> NeedToRefresh = new List<Bucket>();
        internal List<BucketGroup> TrainGroupList = new List<BucketGroup>();
        internal List<IProduct> OpenProductList(ResouceState r)
        {
            List<IProduct> prolist = new List<IProduct>();
            foreach (BucketGroup tg in TrainGroupList)
            {
                prolist = prolist.Union(tg.OpenProduct(r)).ToList();
            }
            return prolist;
        }
        public void Update(ResouceState r)
        {
            //完成所有更新
            foreach (Bucket b in NeedToRefresh)
            {
                b.RefreshOpenProduct(r);
            }
            NeedToRefresh.Clear();
        }
    }
    //桶
    internal class Bucket
    {
        internal SeatSet Seats = new SeatSet();
        internal List<IProduct> ProList = new List<IProduct>(); //所有能出售的产品
        internal List<IProduct> OpenProduct = new List<IProduct>();
        //查找最零散的可售产品
        private List<IMetaResource> getMetaRes(ResouceState r, IProduct p)
        {
            List<IMetaResource> res = new List<IMetaResource>();
            //找到出售一个产品的资源，如果不可售返回空
            foreach (Seat s in Seats)
            {
                if (p.All(i => s.getMetaByRes(i) != null && r.MetaResDic[s.getMetaByRes(i)] == false))//有待优化
                {
                    foreach (IResource re in p)
                    {
                        res.Add(s.getMetaByRes(re));
                    }
                    break;
                }
            }
            return res;
        }
        //刷新开放产品
        internal void RefreshOpenProduct(ResouceState r)
        {
            OpenProduct.Clear();
            //遍历所有能出售的产品
            foreach (IProduct pro in ProList)
            {
                //判断产品是否可售
                List<IMetaResource> imr = getMetaRes(r, pro);
                if (imr.Count > 0)
                {
                    OpenProduct.Add(pro);
                }
            }
        }
        //出票
        internal Ticket PrintTicket(ResouceState r, IProduct p)
        {
            return new Ticket() { MetaResList = getMetaRes(r, p) };
        }
    }
    //按照列车划分
    internal class BucketGroup
    {
        internal Bucket LBucket = new Bucket();
        internal List<Bucket> Buckets = new List<Bucket>();//0为通售

        internal List<IProduct> ProList = new List<IProduct>(); //所有能出售的产品

        //当前开放的产品
        internal List<IProduct> OpenProduct(ResouceState r)
        {
            List<IProduct> prolist = new List<IProduct>();
            prolist = prolist.Union(LBucket.OpenProduct).ToList();
            foreach (Bucket b in Buckets)
            {
                prolist = prolist.Union(b.OpenProduct).ToList();
            }
            return prolist;
        }
        //出票
        internal Ticket PrintTicket(ResouceState r, IProduct p, out List<Bucket> Rf)
        {
            Rf = new List<Bucket>();
            Ticket res = new Ticket();
            if (LBucket.ProList.Contains(p))
            {
                res = LBucket.PrintTicket(r, p);
                if (res.MetaResList.Count > 0)
                {
                    Rf.Add(LBucket);
                    return res;
                }
            }
            Bucket b = Buckets.FirstOrDefault(i => i.OpenProduct.Contains(p));//先卖的放到上面
            if (b != null)
            {
                res = b.PrintTicket(r, p);
                Seat s = (res.MetaResList[0] as MetaResource).Seat;
                b.Seats.Remove(s);
                LBucket.Seats.Add(s);
                Rf.Add(b);//.RefreshOpenProduct();
                Rf.Add(LBucket);
            }
            else
            {
                res = null;
            }

            return res;
        }
    }
}
