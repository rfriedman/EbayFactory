using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.ServiceModel.Web;
using System.Text;
using eBay.Service.Call;
using eBay.Service.Core.Sdk;
using eBay.Service.Core.Soap;
using eBay.Service.Core;
using eBay.Services.Finding;
using eBay.Services;
using Samples.Helper;
using System.Collections;
using System.Security.Principal;
using System.Web.Security;

namespace EbayFactory
{
    // NOTE: You can use the "Rename" command on the "Refactor" menu to change the class name "Service1" in code, svc and config file together.
    public class EbayClient : IEbayClient
    {
        private FindingServicePortTypeClient m_client;
        private ClientConfig m_config;
        private ApiContext m_api;
        DataClasses1DataContext db;
        tblCategory m_cats;

       // private ApiCredential m_user;
        /* Test site
        private static readonly string findingServerAddress = "http://svcs.sandbox.ebay.com/services/search/FindingService/v1";
        private static readonly string appID = "RichardF-18df-4fec-a53e-fc4110d935dd";
        */
        /* Production Site */
        private static readonly string findingServerAddress = "http://svcs.ebay.com/services/search/FindingService/v1?";
        private static readonly string appID = "RichardF-62b4-46b0-bccd-37e659be2a82";

        public EbayClient()
        {
            m_config = new ClientConfig();
            m_config.ApplicationId = appID;
            m_config.EndPointAddress = findingServerAddress;
            m_client = FindingServiceClientFactory.getServiceClient(m_config);
            m_api = new ApiContext();
            m_api = AppSettingHelper.GetApiContext();
            m_cats = new tblCategory();
            db = new DataClasses1DataContext();
                        
        }

        public void CreateSiteDataBase()
        {
            DataClasses1DataContext context = new DataClasses1DataContext();
            
            if (false == context.DatabaseExists())
            {
                
                context.CreateDatabase();
            }
        }
        
        public List<string> GetCategories()
        {
            DataClasses1DataContext context= new DataClasses1DataContext();
            var result = from r in context.tblCategories select r.category_id;
            return result.ToList();
        }
        //get categories from item table
        //count number of occurances for each
        //insert into table vaules category - category count
        //return ordered list in decending order 
        public void SetCategoryCount()
        {
            
            DataClasses1DataContext context = new DataClasses1DataContext();
            var result = from i in context.tblItems select i.item_category;
            var d = result.Distinct();
            foreach (string s in d)
            {
                
                var cnt = context.tblItems.Count(p => p.item_category == s);
                cat_count catcnt = new cat_count();

                catcnt.category_id = s;
                catcnt.category_count = cnt;


                try
                {
                    context.cat_counts.DeleteOnSubmit(catcnt);
                    context.SubmitChanges();
                    context.cat_counts.InsertOnSubmit(catcnt);
                    context.SubmitChanges();
                }
                catch (Exception e)
                {
                    string er = e.Message;
                }
            }    
                        
        }

        public List<cat_count> GetCategoryCount()
        {
            DataClasses1DataContext context = new DataClasses1DataContext();

            return context.cat_counts.ToList().OrderByDescending(cat_count => cat_count.category_count).ToList();

        }

        public List<tblItem> ItemByCategory(string cat)
        {
            DataClasses1DataContext context = new DataClasses1DataContext();;
            var result = (from i in context.tblItems where i.item_category == cat select i);
                                                                                                                            
            return result.ToList();
        }

        public string TestConnection()
        {
            IIdentity currentUser = ServiceSecurityContext.Current.PrimaryIdentity;

            if (Roles.IsUserInRole(currentUser.Name, "Member"))
            {
                DataClasses1DataContext context = new DataClasses1DataContext();

                return string.Format(context.Connection.State + context.Connection.ToString());
            }

            return string.Format("fail");
        }

        public void EbayTopLevelCategories()
        {
            GetCategoriesCall Categories = new GetCategoriesCall(m_api);
            Categories.LevelLimit = 2;
            Categories.DetailLevelList.Add(DetailLevelCodeType.ReturnAll);

            CategoryTypeCollection cats =Categories.GetCategories();

            foreach (CategoryType category in cats)
            {
                m_cats = new tblCategory();
                m_cats.category_id= category.CategoryID;
                m_cats.category_level = category.CategoryLevel.ToString();
                m_cats.category_name = category.CategoryName;
                m_cats.category_parent = category.CategoryParentID[0].ToString();
                DataClasses1DataContext db = new DataClasses1DataContext();

                try
                {
                    DataClasses1DataContext context = new DataClasses1DataContext();
                  //  context.Connection.ConnectionString = System.Configuration.ConfigurationManager.ConnectionStrings["EbayFactoryConnectionString"].ConnectionString;
                    context.tblCategories.InsertOnSubmit(m_cats);
                    
                    context.SubmitChanges();
                }
                catch(Exception e)
                {
                    string er = e.Message;
                }
                

            }
            
            
        }

        public List<tblItem> FindByKeyWord(String items)
        {
            List<tblItem> prodlist = new List<tblItem> { };
            FindItemsAdvancedRequest request = new FindItemsAdvancedRequest();
            //request.affiliate.trackingId = null;
            //request.affiliate.networkId = "9";
            // Set request parameters
            request.keywords = items;
            ItemFilter filter1 = new ItemFilter();
            ItemFilter filter2 = new ItemFilter();
            ItemFilter filter3 = new ItemFilter();
            filter3.name = ItemFilterType.Condition;
            filter3.value = new string[] { "1000" };

            ItemFilter[] filters = { filter3/*, filter2, filter3*/ };

            request.itemFilter = filters;
            //request.categoryId = items;
            if (request.keywords == null)
            {
                request.keywords = "ipod";
            }
            PaginationInput pi = new PaginationInput();
            pi.entriesPerPage = 100;
            pi.entriesPerPageSpecified = true;
            request.paginationInput = pi;

            // Call the service
            FindItemsAdvancedResponse response = m_client.findItemsAdvanced(request);

            SearchItem[] listing = response.searchResult.item;



            if (listing != null)
            {

                foreach (SearchItem i in listing)
                {
                    tblItem items_tbl = new tblItem();
                    items_tbl.item_category = i.primaryCategory.categoryId;
                    items_tbl.item_title = i.title;
                    items_tbl.item_id = i.itemId;
                    items_tbl.gallery_url = i.galleryURL;
                    items_tbl.listing_url = i.viewItemURL;

                    prodlist.Add(items_tbl);
                    try
                    {
                        DataClasses1DataContext context = new DataClasses1DataContext();
                        context.tblItems.InsertOnSubmit(items_tbl);
                        context.SubmitChanges();
                    }
                    catch (Exception e)
                    {
                        string s = e.Message;
                    }

                }

                return prodlist;
            }

            return prodlist;
        }

        public void FindByCategory(String[] items)
        {
            FindItemsAdvancedRequest request = new FindItemsAdvancedRequest();
            //request.affiliate.trackingId = null;
            //request.affiliate.networkId = "9";
            // Set request parameters
        //    request.keywords = items;
            ItemFilter filter1 = new ItemFilter();
            ItemFilter filter2 = new ItemFilter();
            ItemFilter filter3 = new ItemFilter();
            filter3.name = ItemFilterType.Condition;
            filter3.value = new string[] { "1000" };

            ItemFilter[] filters = { filter3/*, filter2, filter3*/ };

            request.itemFilter = filters;
            request.categoryId = items;
           // if (request.keywords == null)
           // {
             //   request.keywords = "ipod";
            //}
            PaginationInput pi = new PaginationInput();
            pi.entriesPerPage = 100;
            pi.entriesPerPageSpecified = true;
            request.paginationInput = pi;

            // Call the service
            FindItemsAdvancedResponse response = m_client.findItemsAdvanced(request);

            SearchItem[] listing = response.searchResult.item;



            if (listing != null)
            {

                foreach (SearchItem i in listing)
                {
                    tblItem items_tbl = new tblItem();
                    items_tbl.item_category = i.primaryCategory.categoryId;
                    items_tbl.item_title = i.title;
                    items_tbl.item_id = i.itemId;
                    items_tbl.gallery_url = i.galleryURL;
                    items_tbl.listing_url = i.viewItemURL;

                    try
                    {
                        DataClasses1DataContext context = new DataClasses1DataContext();
                        context.tblItems.InsertOnSubmit(items_tbl);
                        context.SubmitChanges();
                    }
                    catch (Exception e)
                    {
                        string s = e.Message;
                    }


                }
            }


        }
    
    }
}
