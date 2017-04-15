using System.Collections.Generic;

namespace com.foxmail.wyyuan1991.NRM.Common
{
    //资源的最小等级（席位-列车-区段）
    public interface IMetaResource
    {
        string Name { get; set; }
        int ResID { get; set; }
        int SeatID { get; set; }
        string Description { get; set; }
    }
    //元资源的集合 
    public interface IMetaResourceSet : IEnumerable<IMetaResource>
    {
        //List<IMetaResource> GetMetaResListbySeat(ISeat seat);
        //List<IMetaResource> GetMetaResListbyResource(IResource res);
        //IResourceSet ResSet { get; set; }
        //ISeatSet SeatSet { get; set; }
    }

    public interface ISeat
    {
        int SeatID { get; set; }
        string Tag { get; set; }
        List<IMetaResource> MetaResList { get; set; }
    }
    public interface ISeatSet : IEnumerable<ISeat>
    {

    }
    /// <summary>
    /// 资源-同种元资源的抽象
    /// </summary>
    public interface IResource
    {
        int ResID { get; set; }
        string Description { get; set; }
        string Tag { get; set; }
        List<IMetaResource> MetaResList { get; set; }
    }
    /// <summary>
    /// 资源的集合
    /// </summary>
    public interface IResourceSet : IEnumerable<IResource>//IList<IResource>
    {

    }
    /// <summary>
    /// 产品
    /// </summary>
    public interface IProduct : IEnumerable<IResource>//,IEnumerable<IMetaResource>
    {
        int ProID { get; set; }
        string Description { get; set; }
        double Fare { get; set; }

        bool Contains(IResource r);
        string ToString();
    }
    /// <summary>
    /// 产品的集合
    /// </summary>
    public interface IProductSet : IEnumerable<IProduct>//IList<IResource>
    {
        IProduct this[int index] { get; }
        int Count { get; }
    }
    /// <summary>
    /// 子市场
    /// </summary>
    public interface IMarketSegment
    {
        int MSID { get; set; }
        string Description { get; set; }
        TimeFunction Lamada { get; set; }
    }
    /// <summary>
    /// 市场
    /// </summary>
    public interface IMarket : IEnumerable<IMarketSegment>
    {
        TimeFunction Ro { get; set; }
        IMarketSegment this[int index] { get; }
    }

    /// <summary>
    /// 选择主体，在给定产品下能做出选择（购买一个或多个产品）
    /// </summary>
    public interface IChoiceAgent
    {
        List<IProduct> Select(List<IProduct> ProList, double u);
    }

    /// <summary>
    /// NRM问题的背景数据
    /// </summary>
    public interface NRMData
    {
        string Name { get; set; }
        string Description { get; set; }

        IProductSet ProSet { get; set; }
        IResource ResSet { get; set; }
        IMarket Mar { get; set; }
        int TimeHorizon { get; set; }
    }
}
