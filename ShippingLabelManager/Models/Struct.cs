using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ShippingLabelManager.Models
{
    /// <summary>
    /// 標籤資料(主表)
    /// </summary>
    public class LabelData
    {
        public string labelId { get; set; }
        public string labelName { get; set; }
        public string customerNo { get; set; }
        public string orderNo { get; set; }
        public string labelSize { get; set; }
        public string remark { get; set; }
        public string CreateUser { get; set; }
        public DateTime CreateTime { get; set; }
        public string UpdateUser { get; set; }
        public DateTime UpdateTime { get; set; }
        public bool Enable { get; set; }
    }
    /// <summary>
    /// 標籤資料(明細)
    /// </summary>
    public class LabelElm
    {
        public string labelId { get; set; }
        public string elmId { get; set; }
        public string type { get; set; }
        public string content { get; set; }
        public string relational { get; set; }
        public string attributes { get; set; }
        public decimal x { get; set; }
        public decimal y { get; set; }
        public decimal width { get; set; }
        public decimal height { get; set; }
        public bool isQty { get; set; }
        public bool isSN { get; set; }
        public string CreateUser { get; set; }
        public DateTime CreateTime { get; set; }
        public string UpdateUser { get; set; }
        public DateTime UpdateTime { get; set; }
        public bool Enable { get; set; }
    }
    /// <summary>
    /// 資源定義
    /// </summary>
    public class ResourceDefn
    {
        public string resId { get; set; }
        public string resFileName { get; set; }
        public string resType { get; set; }
        public string CreateUser { get; set; }
        public DateTime CreateTime { get; set; }
        public string UpdateUser { get; set; }
        public DateTime UpdateTime { get; set; }
        public bool Enable { get; set; }
    }

    /// <summary>
    /// 訂單查詢條件
    /// </summary>
    public class OrderSearchConditions
    {
        public string orderNo { get; set; }
        public string customerNo { get; set; }
        public string moNo { get; set; }
        public string shippingDate_Start { get; set; }
        public string shippingDate_End { get; set; }
    }
}