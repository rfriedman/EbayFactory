using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.ServiceModel.Web;
using System.Text;
using eBay.Service.Core.Soap;

namespace EbayFactory
{
    // NOTE: You can use the "Rename" command on the "Refactor" menu to change the interface name "IService1" in both code and config file together.
    [ServiceContract(Namespace = "EbayClient")]
    public interface IEbayClient:IEbayClientWrite,IEbayClientRead
    {

    }
    [ServiceContract(Namespace = "EbayClient")]
    public interface IEbayClientRead
    {
        
            [OperationContract]
            List<string> GetCategories();
            [OperationContract]
            List<tblItem> ItemByCategory(string cat);
            [OperationContract]
            string TestConnection();
            [OperationContract]
            List<cat_count> GetCategoryCount();
            [OperationContract]
            List<tblItem> FindByKeyWord(String items);
            [OperationContract]
            List<string> GetLog();

        // TODO: Add your service operations here
    }

    [ServiceContract(Namespace = "EbayClient")]
    public interface IEbayClientWrite
    {
            [OperationContract]
            //to do:  code method for auctions with at least 24 hrs left
            void FindByCategory(String[] items);
            [OperationContract]
            void EbayTopLevelCategories();
            [OperationContract]
            void CreateSiteDataBase();
            [OperationContract]
            void SetCategoryCount();
            [OperationContract]
            void DeleteTablesThenPopulate();

    }

}
