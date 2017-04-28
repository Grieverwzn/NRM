using com.foxmail.wyyuan1991.Common.ExcelHelper;
using com.foxmail.wyyuan1991.NRM.Common;
using com.foxmail.wyyuan1991.NRM.RailwayModel;
using System;
using System.Collections.Generic;
using System.Data;

namespace com.foxmail.wyyuan1991.NRM.Data
{
    /// <summary>
    /// 标准数据读取类
    /// </summary>
    /// <remarks>
    /// 读取数据，生成市场，产品，资源
    /// 供 DataAdapter 调用
    /// </remarks>
    public class RailDataReader
    {
        DataSet _ds;

        public Market mar;
        public ProductSet proset;
        public ResourceSet ResSet { get { return MRS.ResSet as ResourceSet; } }
        public List<Route> pathList;
        public int TimeHorizon { get; set; }
        public MetaResouceState InitState { get; set; }
        public MetaResourceSet MRS { get; set; }

        public void ReadXLS(string path)
        {
            //Read Excel File
            ExcelHelper eh = new ExcelHelper(path);
            _ds = eh.ExcelToDataSet();
            CheckTables(new string[] { "Res", "Pro", "Path", "Mar", "Dyn", "Settings" });
            GenRes();
            GenPro();
            GenPath();
            GenMar();
            GenDyn();
            GenSettings();
        }

        private bool CheckTables(string[] args)
        {
            bool res = true;
            foreach (string ss in args)
            {
                res = !_ds.Tables.Contains(ss) && res;
            }
            return res;
        }
        private void GenRes()
        {
            DataTable Res = _ds.Tables["Res"];
            MRS = new MetaResourceSet()
            {
                ResSet = new ResourceSet(),
                SeatSet = new SeatSet()
            };
            InitState = new MetaResouceState();
            string temp = "";
            List<Seat> tempSeat = null;
            foreach (DataRow dr in Res.Rows)
            {
                Resource r = new Resource()
                {
                    ResID = Convert.ToInt32(dr["ID"]),
                    Description = dr["Des"].ToString(),
                    Tag = dr["Tag"].ToString()
                };              
                ResSet.Add(r);
                InitState.ResDic.Add(r, 0);
                int num = Convert.ToInt32(dr["Cap"]);
                if (temp != r.Tag)
                {
                    tempSeat = new List<Seat>();
                    for (int i = MRS.SeatSet.Count; i < MRS.SeatSet.Count+num; i++)
                    {
                        tempSeat.Add(new Seat() { SeatID = i, Tag = r.Tag,IDinTrain = i - MRS.SeatSet.Count });
                    }
                    MRS.SeatSet.AddRange(tempSeat);
                }
                for(int i = 0; i < num; i++)
                {
                    MetaResource mr = new MetaResource() {
                        Name =r.ResID + "_" + i,
                        ResID =r.ResID,
                        SeatID =i,
                        Resource = r,
                        Seat = tempSeat[i]
                    };
                    InitState.MetaResDic.Add(mr, false);
                    MRS.Add(mr);
                    r.MetaResList.Add(mr);
                    tempSeat[i].MetaResList.Add(mr);
                }
                temp = r.Tag;
            }
            InitState.UpdateResDic();
        }
        private void GenPro()
        {
            DataTable Pro = _ds.Tables["Pro"];
            proset = new ProductSet();
            foreach (DataRow dr in Pro.Rows)
            {
                Product p = new Product()
                {
                    ProID = Convert.ToInt32(dr["ID"]),
                    Description = dr["Des"].ToString(),
                    Fare = Convert.ToDouble(dr["Fare"])
                };
                string s = dr["Res"].ToString();

                foreach (string ss in s.Split(','))
                {
                    Resource temp = ResSet.Find(i => i.ResID == Convert.ToInt32(ss));
                    if (temp != null) { p.Add(temp); }
                }

                proset.Add(p);
            }
        }
        private void GenPath()
        {
            DataTable pat = _ds.Tables["Path"];
            pathList = new List<Route>();
            foreach (DataRow dr in pat.Rows)
            {
                Route r = new Route()
                {
                    RouteID = Convert.ToInt32(dr["ID"])
                };

                string s = dr["Pro"].ToString();
                foreach (string ss in s.Split(','))
                {
                    Product temp = proset.Find(i => i.ProID == Convert.ToInt32(ss));
                    if (temp != null) { r.Add(temp); }
                }
                pathList.Add(r);
            }
        }
        private void GenMar()
        {
            DataTable marTable = _ds.Tables["Mar"];
            mar = new Market() { AggRo = new List<TimeValue>() };

            mar.Ro = x =>
            {
                if (mar.AggRo.Count == 1) return mar.AggRo[0].value;
                for (int i = 1; i < mar.AggRo.Count; i++)
                {
                    if (x < mar.AggRo[i].Time) return mar.AggRo[i - 1].value;
                }
                return mar.AggRo[mar.AggRo.Count - 1].value;
            };

            foreach (DataRow dr in marTable.Rows)
            {
                MarketSegment ms = new MarketSegment()
                {
                    MSID = Convert.ToInt32(dr["ID"]),
                    Description = dr["Des"].ToString(),
                    Retreat = Convert.ToDouble(dr["Retreat"]),
                    AggLamada = new List<TimeValue>()
                };

                ms.Lamada = x =>
                {
                    if (ms.AggLamada.Count == 1) return ms.AggLamada[0].value;
                    for (int i = 1; i < ms.AggLamada.Count; i++)
                    {
                        if (x < ms.AggLamada[i].Time) return ms.AggLamada[i - 1].value;
                    }
                    return ms.AggLamada[ms.AggLamada.Count - 1].value;
                };

                string c = dr["ConSet"].ToString();
                string p = dr["PreVec"].ToString();
                string[] cc = c.Split(',');
                string[] pp = p.Split(',');

                for (int i = 0; i < cc.Length; i++)
                {
                    Route temp = pathList.Find(r => r.RouteID == Convert.ToInt32(cc[i]));
                    double tempvalue = Convert.ToDouble(pp[i]);
                    if (temp != null) { ms.ConsiderationDic.Add(temp, tempvalue); }
                }
                mar.Add(ms);
            }

        }
        private void GenDyn()
        {
            DataTable Dyn = _ds.Tables["Dyn"];
            for (int i = 0; i < Dyn.Rows.Count; i++)
            {
                int st = Convert.ToInt32(Dyn.Rows[i]["StartTime"]);
                mar.AggRo.Add(new TimeValue()
                {
                    Time = st,
                    value = Convert.ToDouble(Dyn.Rows[i]["alpha"])
                });
                foreach (MarketSegment ms in mar)
                {
                    ms.AggLamada.Add(new TimeValue()
                    {
                        Time = st,
                        value = Convert.ToDouble(Dyn.Rows[i]["m" + ms.MSID].ToString())
                    });
                }
            }
        }
        private void GenSettings()
        {
            DataTable Settings = _ds.Tables["Settings"];
            TimeHorizon = Convert.ToInt32(Settings.Rows[0]["TimeHorizon"]);
        }

        class OD
        {
            public string OriSta { get; set; }
            public string DesSta { get; set; }
            public double Num { get; set; }
            public double lamada { get; set; }
        }
    }
}
