using Newtonsoft.Json.Linq;
using Dapper;
using System;
using System.Configuration;
using System.Data.SqlClient;
using System.Web.Http;
using System.Data;
using ShippingLabelManager.Models;
using System.Activities.Statements;
using System.Net.Http;
using System.IO;
using System.Web;
using System.Net;
using System.Net.Http.Headers;
using System.Linq;
using System.Collections.Generic;

namespace ShippingLabelManager.Controllers
{
    public class DataController : ApiController
    {
        SqlConnection conn;
        string ConnectionString_ShippingLabel;
        string ConnectionString_KINSUN;
        string ConnectionString_KSERP;

        public DataController()
        {
            ConnectionString_ShippingLabel = ConfigurationManager.ConnectionStrings["ShippingLabel"].ConnectionString;
            ConnectionString_KINSUN = ConfigurationManager.ConnectionStrings["KINSUN"].ConnectionString;
            ConnectionString_KSERP = ConfigurationManager.ConnectionStrings["KSERP"].ConnectionString;
        }

        /// <summary>
        /// 取得客戶清單
        /// </summary>
        /// <returns></returns>
        public object GetCustomerList()
        {
            string sqlStr = @"SELECT RTRIM(MA001) AS id,MA002 AS text 
                                FROM COPMA 
                               WHERE MA002 NOT LIKE @disable 
                                 AND MA002 NOT LIKE @void
                            ORDER BY MA001;";

            DynamicParameters parameters = new DynamicParameters();
            parameters.Add("@disable", "%停用%", DbType.String);
            parameters.Add("@void", "%作廢%", DbType.String);

            using (conn = new SqlConnection(ConnectionString_KINSUN))
            {
                var result = conn.Query(sqlStr, parameters, commandType: CommandType.Text);
                return result;
            }
        }

        /// <summary>
        /// 根據客戶代號取得客戶資訊
        /// </summary>
        /// <param name="customId">客戶代號</param>
        /// <returns></returns>
        public object GetCustomerById(string customerNo)
        {
            string sqlStr = @"SELECT MA001 AS id,MA002 AS text 
                                FROM COPMA WHERE MA001 = @customerNo;";

            DynamicParameters parameters = new DynamicParameters();
            parameters.Add("@customerNo", customerNo, DbType.String);

            using (conn = new SqlConnection(ConnectionString_KINSUN))
            {
                var result = conn.Query(sqlStr, parameters, commandType: CommandType.Text);
                return result;
            }
        }

        /// <summary>
        /// 從訂單單號反查客戶代號
        /// </summary>
        /// <param name="orderNo">訂單單號</param>
        /// <returns></returns>
        [HttpGet]
        public string GetCustomerByOrderNo(string orderNo)
        {
            string sqlStr = @"SELECT A.TC004 As CustomerId 
                                FROM COPTC A,COPTD B
                               WHERE A.TC001 = B.TD001
                                 AND A.TC002 = B.TD002
                                 AND B.TD001 = @TD001
                                 AND B.TD002 = @TD002
                                 AND B.TD003 = @TD003;";

            string CustomerId = null;

            var TD = orderNo.Split('-');

            if (TD.Length != 3)
            {
                return null;
            }

            DynamicParameters parameters = new DynamicParameters();
            parameters.Add("@TD001", TD[0], DbType.String);
            parameters.Add("@TD002", TD[1], DbType.String);
            parameters.Add("@TD003", TD[2], DbType.String);

            using (conn = new SqlConnection(ConnectionString_KINSUN))
            {
                try
                {
                    conn.Open();
                    CustomerId = conn.QueryFirst<string>(sqlStr, parameters);
                }
                catch (Exception)
                {

                }
                finally
                {
                    conn.Close();
                }
            }

            return CustomerId;
        }

        /// <summary>
        /// 依照條件查詢訂單
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        public object GetOrders(OrderSearchConditions conditions)
        {
            string sqlStr = @"SELECT A.TA003 AS 'shippingDate',(B.TB004 + '-' + B.TB005 + '-' + B.TB006) AS 'orderNo' 
                                FROM EPSTAA A,EPSTB B
                               WHERE A.TA001 = B.TB001
                                 AND (A.TA004 LIKE @customerNo)
                                 AND (B.TB004 LIKE @orderNo1 AND B.TB005 LIKE @orderNo2 AND B.TB006 LIKE @orderNo3)
                                 AND (B.TB041 LIKE @moNo1 AND B.TB042 LIKE @moNo2)
                                 AND (A.TA003 BETWEEN @shippingDate_Start AND @shippingDate_End);";

            DynamicParameters parameters = new DynamicParameters();

            if (conditions.customerNo == null)
            {
                parameters.Add("@customerNo", "%", DbType.String);
            }
            else
            {
                parameters.Add("@customerNo", conditions.customerNo, DbType.String);
            }

            if (conditions.orderNo == null)
            {
                parameters.Add("@orderNo1", "%", DbType.String);
                parameters.Add("@orderNo2", "%", DbType.String);
                parameters.Add("@orderNo3", "%", DbType.String);
            }
            else
            {
                var orderNo = conditions.orderNo.Split('-');
                parameters.Add("@orderNo1", orderNo[0] + "%", DbType.String);
                parameters.Add("@orderNo2", orderNo[1] + "%", DbType.String);
                parameters.Add("@orderNo3", orderNo[2] + "%", DbType.String);
            }

            if (conditions.moNo == null)
            {
                parameters.Add("@moNo1", "%", DbType.String);
                parameters.Add("@moNo2", "%", DbType.String);
            }
            else
            {
                var moNo = conditions.moNo.Split('-');
                parameters.Add("@moNo1", moNo[0] + "%", DbType.String);
                parameters.Add("@moNo2", moNo[1] + "%", DbType.String);
            }

            var shippingDate_Start = conditions.shippingDate_Start == null ? "19000101" : conditions.shippingDate_Start;
            var shippingDate_End = conditions.shippingDate_End == null ? "99991231" : conditions.shippingDate_End;

            parameters.Add("@shippingDate_Start", shippingDate_Start, DbType.String);
            parameters.Add("@shippingDate_End", shippingDate_End, DbType.String);

            using (conn = new SqlConnection(ConnectionString_KSERP))
            {
                var result = conn.Query(sqlStr, parameters, commandType: CommandType.Text);
                return result;
            }
        }

        #region 標籤API
        /// <summary>
        /// 依照客戶代號與訂單單號取得標籤設定識別ID
        /// </summary>
        /// <param name="customerNo">客戶代號</param>
        /// <param name="orderNo">訂單單號</param>
        /// <returns></returns>
        public string GetLabelIdByCustomerAndOrderNo(string customerNo, string orderNo)
        {
            string sqlStr = @"SELECT labelId 
                                FROM LabelData 
                               WHERE customerNo = @customerNo 
                                 AND orderNo = @orderNo";

            string labelId = "";

            if (orderNo == null) { orderNo = ""; }

            using (conn = new SqlConnection(ConnectionString_ShippingLabel))
            {
                try
                {
                    conn.Open();
                    labelId = conn.ExecuteScalar<string>(sqlStr, new { customerNo, orderNo });
                    //若指定訂單沒找到,改找不指定訂單的標籤設定
                    if (labelId == null)
                    {
                        orderNo = "";
                        labelId = conn.ExecuteScalar<string>(sqlStr, new { customerNo, orderNo });
                    }

                }
                catch (Exception)
                {

                }
                finally
                {
                    conn.Close();
                }
            }

            return labelId;
        }

        /// <summary>
        /// 儲存標籤資訊
        /// </summary>
        /// <param name="labelData"></param>
        /// <returns></returns>
        public object PutLabel(JObject labelData)
        {
            string labelId = labelData.GetValue("labelId").ToString();
            JArray labelElements = (JArray)labelData["elements"];
            bool result = false;

            string sqlStr_CheckLabelData = @"SELECT COUNT(*) FROM LabelData WHERE labelId = @labelId";

            string sqlStr_InsertMaster = @"INSERT INTO LabelData VALUES(
                                @labelId,
                                @labelName,
                                @customerNo,
                                @orderNo,
                                @labelSize,
                                @remark,
                                @CreateUser,
                                @CreateTime,
                                @UpdateUser,
                                @UpdateTime,
                                @Enable)";

            string sqlStr_InsertDetial = @"INSERT INTO LabelElm VALUES(
                                @labelId,
                                @elmId,
                                @type,
                                @content,
                                @relational,
                                @attributes,
                                @x,
                                @y,
                                @width,
                                @height,
                                @isQty,
                                @isSN,
                                @CreateUser,
                                @CreateTime,
                                @UpdateUser,
                                @UpdateTime,
                                @Enable)";

            string sqlStr_UpdateMaster = @"UPDATE LabelData SET 
                                labelName = @labelName,
                                customerNo = @customerNo,
                                orderNo = @orderNo,
                                labelSize = @labelSize,
                                remark = @remark,
                                CreateUser = @CreateUser,
                                CreateTime = @CreateTime,
                                UpdateUser = @UpdateUser,
                                UpdateTime = @UpdateTime 
                                WHERE labelId = @labelId;";

            string sqlStr_DeleteDetial = @"DELETE FROM LabelElm WHERE labelId = @labelId;";


            using (conn = new SqlConnection(ConnectionString_ShippingLabel))
            {
                conn.Open();
                var trans = conn.BeginTransaction();
                TransactionScope scope = new TransactionScope();
                try
                {
                    //Check exists
                    decimal count = conn.ExecuteScalar<decimal>(sqlStr_CheckLabelData, new { labelId }, transaction: trans);

                    var paramaters_LabelData = new LabelData
                    {
                        labelId = labelData.GetValue("labelId").ToString(),
                        labelName = labelData.GetValue("labelName").ToString(),
                        customerNo = labelData.GetValue("customerNo").ToString(),
                        orderNo = labelData.GetValue("orderNo").ToString(),
                        labelSize = labelData.GetValue("labelSize").ToString(),
                        remark = labelData.GetValue("remark").ToString(),
                        CreateUser = "",
                        CreateTime = DateTime.Now,
                        UpdateUser = "",
                        UpdateTime = DateTime.Now,
                        Enable = true
                    };

                    //已有標籤資訊,用UPDATE更新標籤單頭,並刪除同ID標籤元件資訊
                    if (count > 0)
                    {
                        //Update Master
                        conn.Execute(sqlStr_UpdateMaster, paramaters_LabelData, transaction: trans);
                        conn.Execute(sqlStr_DeleteDetial, new { labelId }, transaction: trans);
                    }
                    else
                    {
                        //Insert Master
                        conn.Execute(sqlStr_InsertMaster, paramaters_LabelData, transaction: trans);
                    }

                    //Add Detial
                    foreach (var item in labelElements)
                    {
                        var paramaters_LabelElm = new LabelElm
                        {
                            labelId = labelData.GetValue("labelId").ToString(),
                            elmId = item["elmId"].Value<string>(),
                            type = item["type"].Value<string>(),
                            content = item["content"].Value<string>(),
                            relational = item["relational"].Value<string>(),
                            attributes = item["attributes"].ToString(),
                            x = item["x"].Value<decimal>(),
                            y = item["y"].Value<decimal>(),
                            width = item["width"].Value<decimal>(),
                            height = item["height"].Value<decimal>(),
                            isQty = item["isQty"].Value<bool>(),
                            isSN = item["isSN"].Value<bool>(),
                            CreateUser = "",
                            CreateTime = DateTime.Now,
                            UpdateUser = "",
                            UpdateTime = DateTime.Now,
                            Enable = true
                        };

                        conn.Execute(sqlStr_InsertDetial, paramaters_LabelElm, transaction: trans);
                    }

                    trans.Commit();
                    result = true;
                }
                catch (Exception)
                {
                    trans.Rollback();
                }
                finally
                {
                    conn.Close();
                }
            }
            return result;
        }

        /// <summary>
        /// 讀取標籤主檔
        /// </summary>
        /// <param name="labelId"></param>
        /// <returns></returns>
        public object LoadLabelData(JObject labelId)
        {
            string sqlStr = "SELECT * FROM LabelData WHERE labelId = @labelId";

            DynamicParameters parameters = new DynamicParameters();
            parameters.Add("@labelId", labelId.GetValue("labelId").ToString(), DbType.String);

            using (conn = new SqlConnection(ConnectionString_ShippingLabel))
            {
                var result = conn.Query<dynamic>(sqlStr, parameters, commandType: CommandType.Text);
                return result;
            }
        }

        /// <summary>
        /// 讀取標籤明細檔
        /// </summary>
        /// <param name="labelId"></param>
        /// <returns></returns>
        public object LoadLabelElm(JObject labelId)
        {
            string sqlStr = "SELECT * FROM LabelElm WHERE labelId = @labelId";

            DynamicParameters parameters = new DynamicParameters();
            parameters.Add("@labelId", labelId.GetValue("labelId").ToString(), DbType.String);

            using (conn = new SqlConnection(ConnectionString_ShippingLabel))
            {
                var result = conn.Query<LabelElm>(sqlStr, parameters, commandType: CommandType.Text);
                return result;
            }
        }

        /// <summary>
        /// 刪除標籤
        /// </summary>
        /// <param name="labelId"></param>
        /// <returns></returns>
        public bool DeleteLabel(JObject labelId)
        {
            bool result = false;

            string sqlStr_DeleteMaster = @"DELETE FROM LabelData WHERE labelId = @labelId";
            string sqlStr_DeleteDetail = @"DELETE FROM LabelElm WHERE labelId = @labelId";

            DynamicParameters parameters = new DynamicParameters();
            parameters.Add("@labelId", labelId.GetValue("labelId").ToString(), DbType.String);

            using (conn = new SqlConnection(ConnectionString_ShippingLabel))
            {
                conn.Open();
                var trans = conn.BeginTransaction();

                try
                {
                    conn.Execute(sqlStr_DeleteMaster, parameters, transaction: trans);
                    conn.Execute(sqlStr_DeleteDetail, parameters, transaction: trans);

                    trans.Commit();
                    result = true;
                }
                catch (Exception)
                {
                    trans.Rollback();
                }
                finally
                {
                    conn.Close();
                }
            }
            return result;
        }
        #endregion

        #region 圖片相關API
        /// <summary>
        /// 檢查資源名稱是否重複
        /// </summary>
        /// <param name="resId"></param>
        /// <returns></returns>
        [HttpGet]
        public bool CkeckResourceExist(string resId)
        {
            string sqlStr = "SELECT COUNT(*) FROM ResourceDefn WHERE resId = @resId";
            int count;

            DynamicParameters parameters = new DynamicParameters();
            parameters.Add("@resId", resId, DbType.String);

            try
            {
                using (conn = new SqlConnection(ConnectionString_ShippingLabel))
                {
                    conn.Open();
                    count = conn.QueryFirst<int>(sqlStr, parameters, commandType: CommandType.Text);
                    conn.Close();
                }
            }
            catch (Exception)
            {
                count = 1;
            }

            return count >= 1 ? true : false;
        }

        /// <summary>
        /// 設定資源定義
        /// </summary>
        /// <param name="resId">資源識別碼</param>
        /// <param name="resUrl">資源路徑</param>
        /// <param name="type">資源類型</param>
        /// <returns></returns>
        public object PutResourceDefn(JObject data)
        {
            bool result = false;

            string sqlStr = @"INSERT INTO ResourceDefn VALUES(
                                @resId,
                                @resFileName,
                                @resType,
                                @CreateUser,
                                @CreateTime,
                                @UpdateUser,
                                @UpdateTime,
                                @Enable)";

            var defn = new ResourceDefn()
            {
                resId = data.GetValue("resId").ToString(),
                resFileName = data.GetValue("resFileName").ToString(),
                resType = data.GetValue("resType").ToString(),
                CreateUser = "",
                CreateTime = DateTime.Now,
                UpdateUser = "",
                UpdateTime = DateTime.Now,
                Enable = true
            };

            using (conn = new SqlConnection(ConnectionString_ShippingLabel))
            {
                conn.Open();
                var trans = conn.BeginTransaction();

                try
                {
                    conn.Execute(sqlStr, defn, transaction: trans);

                    trans.Commit();
                    result = true;
                }
                catch (Exception)
                {
                    trans.Rollback();
                }
                finally
                {
                    conn.Close();
                }
            }
            return result;
        }

        /// <summary>
        /// 取得圖片資源清單
        /// </summary>
        /// <param name="forAutoComplete">是否用於自動完成清單?</param>
        /// <returns></returns>
        public object GetImageResourceList(bool forAutoComplete)
        {
            string sqlStr = "SELECT resId FROM ResourceDefn WHERE resType = @resType";

            if (forAutoComplete)
            {
                sqlStr = @"SELECT resId AS id, 
                                  resId AS text 
                             FROM ResourceDefn 
                            WHERE resType = @resType";
            }

            DynamicParameters parameters = new DynamicParameters();
            parameters.Add("@resType", "Image", DbType.String);

            using (conn = new SqlConnection(ConnectionString_ShippingLabel))
            {
                var result = conn.Query(sqlStr, parameters, commandType: CommandType.Text);
                return result;
            }
        }

        /// <summary>
        /// 取得單一圖片資源設定項目
        /// </summary>
        /// <param name="resId"></param>
        /// <returns></returns>
        public object GetImageResourceById(string resId)
        {
            string sqlStr = @"SELECT resId AS id, 
                                     resId AS text 
                                FROM ResourceDefn 
                               WHERE resId = @resId";

            DynamicParameters parameters = new DynamicParameters();
            parameters.Add("@resId", resId, DbType.String);

            using (conn = new SqlConnection(ConnectionString_ShippingLabel))
            {
                var result = conn.Query(sqlStr, parameters, commandType: CommandType.Text);
                return result;
            }
        }

        /// <summary>
        /// 上傳圖片
        /// </summary>
        /// <returns></returns>
        public bool UploadImage()
        {
            var httpRequest = HttpContext.Current.Request;
            HttpPostedFileBase file = new HttpPostedFileWrapper(httpRequest.Files[0]) as HttpPostedFileBase;

            bool result = false;

            //檢查上傳目標目錄是否存在,若不存在則建立
            var savePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "App_Data");

            if (!Directory.Exists(savePath))
            {
                Directory.CreateDirectory("App_Data");
            }

            try
            {
                savePath = Path.Combine(savePath + "\\", file.FileName);
                file.SaveAs(savePath);
                result = true;
            }
            catch (Exception)
            {

            }
            return result;
        }

        /// <summary>
        /// 刪除圖片
        /// </summary>
        /// <param name="imgId">圖片識別碼</param>
        /// <returns></returns>
        public bool DeleteImage(string imgId)
        {
            string sqlStr_getFileName = "SELECT resFileName FROM ResourceDefn WHERE resId = @resId";
            string sqlStr_deleteResourceDefn = "DELETE FROM ResourceDefn WHERE resId = @resId";
            string fileName, path;
            bool result;

            DynamicParameters parameters = new DynamicParameters();
            parameters.Add("@resId", imgId, DbType.String);

            using (conn = new SqlConnection(ConnectionString_ShippingLabel))
            {
                conn.Open();
                var trans = conn.BeginTransaction();

                try
                {
                    //取得檔名
                    fileName = conn.QuerySingle<string>(sqlStr_getFileName, parameters, transaction: trans);
                    //刪除檔案
                    path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "App_Data");
                    path = Path.Combine(path + "\\", fileName);

                    FileInfo imgFile = new FileInfo(path);
                    imgFile.Delete();
                    //刪除定義資料
                    conn.Execute(sqlStr_deleteResourceDefn, parameters, transaction: trans);

                    trans.Commit();
                    result = true;
                }
                catch (Exception)
                {
                    trans.Rollback();
                    result = false;
                }
            }

            return result;
        }

        /// <summary>
        /// 取得圖片檔
        /// </summary>
        /// <param name="resId">圖片資源識別碼</param>
        /// <returns></returns>
        public IHttpActionResult GetImage(string resId)
        {
            string sqlStr = "SELECT resFileName FROM ResourceDefn WHERE resId = @resId";
            string fileName, path;

            DynamicParameters parameters = new DynamicParameters();
            parameters.Add("@resId", resId, DbType.String);

            using (conn = new SqlConnection(ConnectionString_ShippingLabel))
            {
                fileName = conn.QuerySingle<string>(sqlStr, parameters, commandType: CommandType.Text);
            }
            path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "App_Data");
            path = Path.Combine(path + "\\", fileName);

            var stream = File.OpenRead(path);
            //建立HTTP回應物件
            var response = new HttpResponseMessage(HttpStatusCode.OK);
            response.Content = new StreamContent(stream);

            response.Content.Headers.ContentType = new MediaTypeHeaderValue("image/jpg");
            response.Content.Headers.ContentLength = stream.Length;

            return ResponseMessage(response);
        }
        #endregion


    }
}
