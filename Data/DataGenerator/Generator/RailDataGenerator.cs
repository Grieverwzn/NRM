using com.foxmail.wyyuan1991.Common.ExcelHelper;
using com.foxmail.wyyuan1991.NRM.RailwayModel;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace com.foxmail.wyyuan1991.NRM.Data
{
    /// <summary>
    /// 从客流、时刻表生成资源和产品
    /// </summary>
    public class RailDataGenerator
    {
        //Read data from EXCEL
        DataSet _ds;
        Settings settings;

        //Data
        Timetable tt;
        List<OD> odfs;
        public Market mar;
        public List<Route> pathList;
        public ProductSet proset;
        public ResourceSet rs;
        public int TimeHorizon { get; set; }

        public void ReadXLS(string path)
        {
            //Read Excel File
            ExcelHelper eh = new ExcelHelper(path);
            _ds = eh.ExcelToDataSet();

            if (!_ds.Tables.Contains("Train") || !_ds.Tables.Contains("TimeTable") || !_ds.Tables.Contains("Market") || !_ds.Tables.Contains("Setting")) return;
            settings = new Settings(_ds.Tables["Setting"]);
        }

        public void ClearAll()
        {
            tt = null;
            this.mar = null;
            pathList = null;
            proset = null;
            rs = null;
        }
        //public void GenDataWithCT(double lamada, double transiteRate, double transitUtility)
        //{
        //    #region Resource   
        //    rs = new ResourceSet();
        //    Resource r1 = new Resource()
        //    {
        //        ResID = 1,
        //        Description = "Train1:A->B",
        //        Capacity = 5
        //    };

        //    Resource r2 = new Resource()
        //    {
        //        ResID = 2,
        //        Description = "Train1:B->C",
        //        Capacity = 5
        //    };
        //    Resource r3 = new Resource()
        //    {
        //        ResID = 3,
        //        Description = "Train2:A->B",
        //        Capacity = 5
        //    };
        //    Resource r4 = new Resource()
        //    {
        //        ResID = 4,
        //        Description = "Train3:B->C",
        //        Capacity = 5
        //    };
        //    //Resource r5 = new Resource()
        //    //{
        //    //    ResID = 5,
        //    //    Description = "Train4:A->C",
        //    //    Capacity = 10
        //    //};
        //    rs.Add(r1); rs.Add(r2); rs.Add(r3); rs.Add(r4);// rs.Add(r5);
        //    #endregion

        //    #region  Products
        //    proset = new ProductSet();
        //    Product pro1 = new Product() { ProID = 1, Description = "Train1:A->B", Fare = 100 }; pro1.Add(r1);
        //    Product pro2 = new Product() { ProID = 2, Description = "Train1:B->C", Fare = 100 }; pro2.Add(r2);
        //    Product pro3 = new Product() { ProID = 3, Description = "Train1:A->C", Fare = 200 }; pro3.Add(r1); pro3.Add(r2);
        //    Product pro4 = new Product() { ProID = 4, Description = "Train2:A->B", Fare = 150 }; pro4.Add(r3);
        //    Product pro5 = new Product() { ProID = 5, Description = "Train3:B->C", Fare = 150 }; pro5.Add(r4);
        //    Product pro6 = new Product() { ProID = 6, Description = "Train2:A->B&Train3:B->C", Fare = 320 }; pro6.Add(r3); pro6.Add(r4);
        //    proset.Add(pro1); proset.Add(pro2); proset.Add(pro3); proset.Add(pro4); proset.Add(pro5); proset.Add(pro6);
        //    #endregion

        //    #region  Routes
        //    pathList = new List<Route>();
        //    //A->B
        //    Route rou1 = new Route(new List<Product>() { pro1 });
        //    Route rou2 = new Route(new List<Product>() { pro4 });
        //    //B-C
        //    Route rou3 = new Route(new List<Product>() { pro2 });
        //    Route rou4 = new Route(new List<Product>() { pro5 });
        //    //A->C
        //    Route rou5 = new Route(new List<Product>() { pro3 });
        //    Route rou6 = new Route(new List<Product>() { pro6 });//联程票
        //    Route rou7 = new Route(new List<Product>() { pro4, pro5 });//换乘

        //    pathList.Add(rou1); pathList.Add(rou2); pathList.Add(rou3); pathList.Add(rou4); pathList.Add(rou5); pathList.Add(rou6); pathList.Add(rou7);
        //    #endregion

        //    #region Markets
        //    mar = new Market() { Ro = x => { return lamada; } };
        //    MarketSegment MS1 = new MarketSegment()
        //    {
        //        MSID = 1,
        //        Description = "Price sensitive, Nonstop (A→C)",
        //        Lamada = x => { return 0.5 - transiteRate; },
        //        Retreat = 0.01
        //    };
        //    MS1.ConsiderationDic.Add(rou5, 8); //MS1.ConsiderationDic.Add(rou7, 2);
        //    MarketSegment MS2 = new MarketSegment()
        //    {
        //        MSID = 2,
        //        Description = "Price insensitive, Nonstop (A→C)",
        //        Lamada = x => { return transiteRate; },
        //        Retreat = 0.01
        //    };
        //    MS2.ConsiderationDic.Add(rou5, 8); MS2.ConsiderationDic.Add(rou6, 2 + transitUtility); MS2.ConsiderationDic.Add(rou7, 2);
        //    MarketSegment MS3 = new MarketSegment()
        //    {
        //        MSID = 3,
        //        Description = "A→B",
        //        Lamada = x => { return 0.25; },
        //        Retreat = 0.01
        //    };
        //    MS3.ConsiderationDic.Add(rou1, 4); MS3.ConsiderationDic.Add(rou2, 4);
        //    MarketSegment MS4 = new MarketSegment()
        //    {
        //        MSID = 4,
        //        Description = "B→C",
        //        Lamada = x => { return 0.25; },
        //        Retreat = 0.01
        //    };
        //    MS4.ConsiderationDic.Add(rou3, 4); MS4.ConsiderationDic.Add(rou4, 4);
        //    mar.Add(MS1); mar.Add(MS2); mar.Add(MS3); mar.Add(MS4);
        //    #endregion

        //    this.TimeHorizon = 100;
        //}
        //public void GenDataWithoutCT(double lamada, double transiteRate)
        //{
        //    #region Resource
        //    rs = new ResourceSet();
        //    Resource r1 = new Resource()
        //    {
        //        ResID = 1,
        //        Description = "Train1:A->B",
        //        Capacity = 3
        //    };

        //    Resource r2 = new Resource()
        //    {
        //        ResID = 2,
        //        Description = "Train1:B->C",
        //        Capacity = 3
        //    };
        //    Resource r3 = new Resource()
        //    {
        //        ResID = 3,
        //        Description = "Train2:A->B",
        //        Capacity = 3
        //    };
        //    Resource r4 = new Resource()
        //    {
        //        ResID = 4,
        //        Description = "Train3:B->C",
        //        Capacity = 3
        //    };
        //    //Resource r5 = new Resource()
        //    //{
        //    //    ResID = 5,
        //    //    Description = "Train4:A->C",
        //    //    Capacity = 3
        //    //};
        //    rs.Add(r1); rs.Add(r2); rs.Add(r3); rs.Add(r4); //rs.Add(r5);
        //    #endregion

        //    #region  Products
        //    proset = new ProductSet();
        //    Product pro1 = new Product() { ProID = 1, Description = "Train1:A->B", Fare = 100 }; pro1.Add(r1);
        //    Product pro2 = new Product() { ProID = 2, Description = "Train1:B->C", Fare = 100 }; pro2.Add(r2);
        //    Product pro3 = new Product() { ProID = 3, Description = "Train1:A->C", Fare = 200 }; pro3.Add(r1); pro3.Add(r2);
        //    Product pro4 = new Product() { ProID = 4, Description = "Train2:A->B", Fare = 150 }; pro4.Add(r3);
        //    Product pro5 = new Product() { ProID = 5, Description = "Train3:B->C", Fare = 150 }; pro5.Add(r4);
        //    //Product pro6 = new Product() { ProID = 6, Description = "Train2:A->B&Train3:B->C", Fare = 320 }; pro6.Add(r3); pro6.Add(r4);
        //    proset.Add(pro1); proset.Add(pro2); proset.Add(pro3); proset.Add(pro4); proset.Add(pro5); //proset.Add(pro6);
        //    #endregion

        //    #region  Routes
        //    pathList = new List<Route>();
        //    //A->B
        //    Route rou1 = new Route(new List<Product>() { pro1 });
        //    Route rou2 = new Route(new List<Product>() { pro4 });
        //    //B-C
        //    Route rou3 = new Route(new List<Product>() { pro2 });
        //    Route rou4 = new Route(new List<Product>() { pro5 });
        //    //A->C
        //    Route rou5 = new Route(new List<Product>() { pro3 });
        //    //Route rou6 = new Route(new List<Product>() { pro6 });//联程票
        //    Route rou7 = new Route(new List<Product>() { pro4, pro5 });//换乘

        //    pathList.Add(rou1); pathList.Add(rou2); pathList.Add(rou3); pathList.Add(rou4); pathList.Add(rou5); pathList.Add(rou7);
        //    #endregion

        //    #region Markets
        //    mar = new Market() { Ro = x => { return lamada; } };
        //    MarketSegment MS1 = new MarketSegment()
        //    {
        //        MSID = 1,
        //        Description = "Price sensitive, Nonstop (A→C)",
        //        Lamada = x => { return 0.5 - transiteRate; },
        //        Retreat = 0.001
        //    };
        //    MS1.ConsiderationDic.Add(rou5, 8); //MS1.ConsiderationDic.Add(rou7, 2);
        //    MarketSegment MS2 = new MarketSegment()
        //    {
        //        MSID = 2,
        //        Description = "Price insensitive, Nonstop (A→C)",
        //        Lamada = x => { return transiteRate; },
        //        Retreat = 0.001
        //    };
        //    MS2.ConsiderationDic.Add(rou5, 8); MS2.ConsiderationDic.Add(rou7, 2);
        //    MarketSegment MS3 = new MarketSegment()
        //    {
        //        MSID = 3,
        //        Description = "A→B",
        //        Lamada = x => { return 0.25; },
        //        Retreat = 0.001
        //    };
        //    MS3.ConsiderationDic.Add(rou1, 4); MS3.ConsiderationDic.Add(rou2, 4);
        //    MarketSegment MS4 = new MarketSegment()
        //    {
        //        MSID = 4,
        //        Description = "B→C",
        //        Lamada = x => { return 0.25; },
        //        Retreat = 0.001
        //    };
        //    MS4.ConsiderationDic.Add(rou3, 4); MS4.ConsiderationDic.Add(rou4, 4);
        //    mar.Add(MS1); mar.Add(MS2); mar.Add(MS3); mar.Add(MS4);
        //    #endregion
        //}

        public void GenTimetable()
        {
            tt = new Timetable();
            DataTable index = _ds.Tables["Train"];
            DataTable detail = _ds.Tables["TimeTable"];
            foreach (DataRow dr in index.Rows)
            {
                Train t = new Train()
                {
                    Name = dr["ID"].ToString(),
                    PriceRatio = Convert.ToDouble(dr["PriceRatio"]),
                    Cap = Convert.ToInt32(dr["Capacity"])
                };
                //Initialize the station list.
                foreach (DataRow dr2 in detail.Rows)
                {
                    if (t.Name == dr2["ID"].ToString())
                    {
                        t.StaList.Add(new StopStation(t)
                        {
                            StationName = dr2["Station"].ToString(),
                            ArrTime = new DateTime(1, 1, 1, Convert.ToInt32(dr2["ArrTime_H"]), Convert.ToInt32(dr2["ArrTime_M"]), 0),
                            DepTime = new DateTime(1, 1, 1, Convert.ToInt32(dr2["Deptime_H"]), Convert.ToInt32(dr2["Deptime_M"]), 0),
                            MileStone = Convert.ToDouble(dr2["MileStone"])
                        });
                    }
                }
                //Initialize the section list.
                t.GenSecList();
                tt.Add(t);
            }
        }


        public void GenODFS()
        {
            odfs = new List<OD>();
            DataTable dt = _ds.Tables["Market"];
            foreach (DataRow dr in dt.Rows)
            {
                odfs.Add(new OD()
                {
                    OriSta = dr["O"].ToString(),
                    DesSta = dr["D"].ToString(),
                    Num = Convert.ToDouble(dr["Num"])
                });
            }
        }

        public void GenResSpace()
        {
            //rs = new ResourceSet();
            //foreach (Train t in tt)
            //{
            //    rs.AddRange(t.GenSecList());
            //}
        }
        public void GenProducts()
        {
            int num = 1;
            proset = new ProductSet();
            foreach (Train t in tt)
            {
                foreach (StopStation ss in t.StaList)
                {
                    for (int i = t.StaList.IndexOf(ss); i < t.StaList.Count - 1; i++)
                    {
                        Product p = new Product()
                        {
                            ProID = num++,
                            StartStation = ss.StationName,
                            EndStation = t.StaList[i + 1].StationName,
                            Train = t
                        };
                        double f = 0;
                        for (int j = t.StaList.IndexOf(ss) + 1; j <= i + 1; j++)
                        {
                            f += (t.StaList[j].MileStone - t.StaList[j - 1].MileStone) * Math.Pow(settings.PriceDeclineRatio, j - t.StaList.IndexOf(ss) - 1);
                        }
                        p.Fare = f * t.PriceRatio;
                        for (int j = t.StaList.IndexOf(ss); j < i + 1; j++)
                        {
                            //p.Add(t.SecList[j]);
                        }
                        proset.Add(p);
                    }

                }
            }
        }

        public void GenMarket()
        {
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
            DataTable dt = _ds.Tables["Market"];
            foreach (DataRow dr in dt.Rows)
            {
                MarketSegment ms = new MarketSegment()
                {
                    MSID = Convert.ToInt32(dr["ID"]),
                    OriSta = dr["O"].ToString(),
                    DesSta = dr["D"].ToString(),
                    Retreat = Convert.ToDouble(dr["Retreat"]),
                    Transfer = Convert.ToInt32(dr["TRANSFER"]),
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
                for (int s = 6; s < dt.Columns.Count; s++)
                {
                    ms.AggLamada.Add(new TimeValue()
                    {
                        value = Convert.ToDouble(dr[s])
                    });
                }
                mar.Add(ms);
            }
            int ss = 0;
            for (int i = 0; i < dt.Columns.Count - 6; i++)
            {
                double total = (mar as List<MarketSegment>).Sum(s => s.AggLamada[i].value);
                int d = number_of_intervals((Int32)Math.Ceiling(total), 0.1);
                double Lamada = total / d;

                mar.AggRo.Add(new TimeValue() { Time = ss, value = Lamada });
                foreach (MarketSegment ms in mar)
                {
                    ms.AggLamada[i].Time = ss;
                    ms.AggLamada[i].value =
                        ((ms.AggLamada[i].value / d) * Math.Exp(-ms.AggLamada[i].value / d)) / Lamada;
                }
                ss += d;
            }
            TimeHorizon = ss;
        }
        public void GenRoute()
        {
            pathList = new List<Route>();
            Dictionary<OD, List<Route>> dic = new Dictionary<OD, List<Route>>();
            foreach (MarketSegment ms in mar)
            {
                OD temp = dic.Keys.FirstOrDefault(i => i.OriSta == ms.OriSta && i.DesSta == ms.DesSta);
                if (temp == null)
                {
                    temp = new OD() { OriSta = ms.OriSta, DesSta = ms.DesSta };
                    var a = FindPath(temp, 1, 60, 5).ToList();
                    dic.Add(temp, a);
                    pathList.AddRange(a);
                }

                foreach (Route r in dic[temp])
                {
                    if (r.Count <= ms.Transfer + 1)
                    {
                        if (r.Count == 1)
                        {
                            ms.ConsiderationDic.Add(r, 8);
                        }
                        else if (r.Count > 1)
                        {
                            ms.ConsiderationDic.Add(r, 2);
                        }
                    }
                }
            }
        }

        //public void GenMarket()
        //{
        //    pathList = new List<Route>();
        //    double total = odfs.Sum(i => i.Num);
        //    TimeHorizon = number_of_intervals((Int32)Math.Ceiling(total), 0.1);
        //    double Lamada = total / TimeHorizon;            
        //    mar = new Market();
        //    mar.AggRo = new List<TimeValue>();
        //    mar.AggRo.Add(new TimeValue() { Time = 0, value = Lamada });
        //    mar.Ro = x =>
        //    {
        //        if (mar.AggRo.Count == 1) return mar.AggRo[0].value;
        //        for (int i = 1; i < mar.AggRo.Count; i++)
        //        {
        //            if (x < mar.AggRo[i].Time) return mar.AggRo[i].value;
        //        }
        //        return 0;
        //    };
        //    foreach (OD od in odfs)
        //    {
        //       od.lamada=((od.Num / TimeHorizon) * Math.Exp(-od.Num / TimeHorizon)) / Lamada;
        //        MarketSegment ms1 =new MarketSegment()
        //        {
        //            OriSta = od.OriSta,
        //            DesSta = od.DesSta,
        //            Retreat = 1,
        //            Transfer = 1
        //        };
        //        ms1.AggLamada = new List<TimeValue>();
        //        ms1.AggLamada.Add(new TimeValue() { Time = 0, value = od.lamada * 0.5 });
        //        ms1.Lamada = x => {
        //            if (ms1.AggLamada.Count == 1) return ms1.AggLamada[0].value;
        //            for (int i = 1; i < ms1.AggLamada.Count; i++)
        //            {
        //                if (x < ms1.AggLamada[i].Time) return ms1.AggLamada[i].value;
        //            }
        //            return 0;
        //        };
        //        MarketSegment ms2 = new MarketSegment()
        //        {
        //            OriSta = od.OriSta,
        //            DesSta = od.DesSta,
        //            Retreat = 1,
        //            Transfer = 0
        //        };
        //        ms2.AggLamada = new List<TimeValue>();
        //        ms2.AggLamada.Add(new TimeValue() { Time = 0, value = od.lamada * 0.5 });
        //        ms2.Lamada = x => {
        //            if (ms2.AggLamada.Count == 1) return ms2.AggLamada[0].value;
        //            for (int i = 1; i < ms2.AggLamada.Count; i++)
        //            {
        //                if (x < ms2.AggLamada[i].Time) return ms2.AggLamada[i].value;
        //            }
        //            return 0;
        //        };
        //        var a = FindPath(od, 2,60,5); pathList.AddRange(a);
        //        foreach (Route r in a)
        //        {
        //            if (r.Count == 1)
        //            {
        //                ms1.ConsiderationDic.Add(r, 8);
        //                ms2.ConsiderationDic.Add(r, 8);
        //            }
        //            else
        //            {
        //                ms1.ConsiderationDic.Add(r, 2);
        //            }
        //        }
        //        mar.Add(ms1); mar.Add(ms2);
        //    }
        //}

        private double possibility_arrival_oneorzero(double u, double v)
        {
            return 1 - Math.Exp(-u / v) - (u / v) * Math.Exp(-u / v);
        }
        private int number_of_intervals(int L, double sigma)
        {
            for (int i = 5 * L; i <= 100 * L; i += 5 * L)
            {
                if (possibility_arrival_oneorzero(L, i) < sigma) return i;
            }
            return -1;
        }

        private HashSet<Route> FindPath(OD od, int TransitUpperBound, int TransitMinutes_UB, int TransitMinutes_LB)
        {
            HashSet<Route> PathSet = new HashSet<Route>();
            //TODO:An A-Star Aglorithm to Find Out Feasible Paths.
            //If there is zero/one transit
            List<Train> tlo = tt.Where(i => i.StaList.Exists(j => j.StationName == od.OriSta)).ToList();
            List<Train> tld = tt.Where(i => i.StaList.Exists(j => j.StationName == od.DesSta)).ToList();
            foreach (Train a in tlo)
            {
                if (a.PassStation(od.DesSta))
                {
                    Product p = FindProduct(proset, a, od.OriSta, od.DesSta);
                    if (p != null) PathSet.Add(CreatePath(od, new List<Product>() { FindProduct(proset, a, od.OriSta, od.DesSta) }));
                }
                else if (TransitUpperBound >= 1)
                {
                    foreach (Train b in tld)
                    {
                        if (a.Equals(b)) continue;
                        if (b.PassStation(od.OriSta)) continue;
                        List<string> list = a.InteractWith(b);
                        foreach (string s in list)
                        {
                            if (s != od.OriSta && s != od.DesSta
                                && (b.getDepTime(s) - a.getArrTime(s)).TotalMinutes <= TransitMinutes_UB
                                && (b.getDepTime(s) - a.getArrTime(s)).TotalMinutes >= TransitMinutes_LB
                                )
                            {

                                Product p1 = FindProduct(proset, a, od.OriSta, s);
                                Product p2 = FindProduct(proset, b, s, od.DesSta);
                                if (p1 != null && p2 != null) PathSet.Add(CreatePath(od, new List<Product>() { p1, p2 }));
                            }
                        }
                    }
                }
            }
            return PathSet;
        }
        private static Route CreatePath(OD odf, List<Product> ProductList)
        {
            return new Route(ProductList);
        }
        private static Product FindProduct(List<Product> ProductList, Train t, string o, string d)
        {
            if (t.PassStation(o) && t.PassStation(d) && t.TravelTime(o, d) != TimeSpan.MaxValue)
            {
                return ProductList.FirstOrDefault(i => i.Train == t && i.StartStation == o && i.EndStation == d);
            }
            return null;
        }
    }
}
