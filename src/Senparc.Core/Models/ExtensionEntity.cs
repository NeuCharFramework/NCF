using Senparc.CO2NET.Extensions;
using Senparc.Ncf.Core.Enums;
using Senparc.Ncf.Core.Models;
using Senparc.Ncf.Core.Utility;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

//using WURFL;

namespace Senparc.Core.Models
{

    #region 数据库实体扩展



    #endregion

    #region Email

    /// <summary>
    /// 自动发送
    /// </summary>
    public class AutoSendEmail
    {
        public int Id { get; set; }

        public string Address { get; set; }

        public string Subject { get; set; }

        public string Body { get; set; }

        public string UserName { get; set; }

        public DateTime LastSendTime { get; set; }

        public int SendCount { get; set; }
    }

    /// <summary>
    /// 自动发送完成
    /// </summary>
    public class AutoSendEmailBak
    {
        public int Id { get; set; }

        public string Address { get; set; }

        public string Subject { get; set; }

        public string Body { get; set; }

        public string UserName { get; set; }

        public DateTime SendTime { get; set; }
    }

    public class EmailUser
    {
        public int Id { get; set; }

        public string Name { get; set; }

        public string DisplayName { get; set; }

        public string EmailAddress { get; set; }

        public string Password { get; set; }

        public string SmtpHost { get; set; }

        public int SmtpPort { get; set; }

        public bool NeedCredentials { get; set; }

        public string Note { get; set; }
    }

    #endregion

    #region XML Config格式
    /// <summary>
    /// Email
    /// </summary>
    public class XmlConfig_Email
    {
        public string ToUse { get; set; }

        public string Subject { get; set; }

        public string Body { get; set; }

        public string Holders { get; set; }

        public DateTime UpdateTime { get; set; }
    }

    #endregion

    #region 省、市、区XML数据格式
    [DataContract]
    [Serializable]
    public class AreaXML_Provinces
    {
        [DataMember]
        public int ID { get; set; }

        [DataMember]
        public string ProvinceName { get; set; }

        /// <summary>
        /// 地区代码
        /// </summary>
        [DataMember]
        public string DivisionsCode { get; set; }

        /// <summary>
        /// 缩写（去掉“省”“市”“自治区”等）
        /// </summary>
        [DataMember]
        public string ShortName { get; set; }

        public AreaXML_Provinces(int id, string provinceName, string divisionsCode, string shortName)
        {
            this.ID = id;
            this.ProvinceName = provinceName;
            this.DivisionsCode = divisionsCode;
            this.ShortName = shortName;
        }
    }

    [DataContract]
    [Serializable]
    public class AreaXML_Cities
    {
        [DataMember]
        public int ID { get; set; }

        [DataMember]
        public int PID { get; set; }

        [DataMember]
        public string CityName { get; set; }

        [DataMember]
        public string ZipCode { get; set; }

        [DataMember]
        public string CityCode { get; set; }

        [DataMember]
        public int MaxShopId { get; set; }

        public AreaXML_Cities(int id, int pID, string cityName, string zipCode, string cityCode, int maxShopId)
        {
            this.ID = id;
            this.PID = pID;
            this.CityName = cityName;
            this.ZipCode = zipCode;
            this.CityCode = cityCode;
            this.MaxShopId = maxShopId;
        }
    }

    [DataContract]
    [Serializable]
    public class AreaXML_Districts
    {
        [DataMember]
        public int ID { get; set; }

        [DataMember]
        public int CID { get; set; }

        [DataMember]
        public string DistrictName { get; set; }

        public AreaXML_Districts(int id, int cID, string districtName)
        {
            this.ID = id;
            this.CID = cID;
            this.DistrictName = districtName;
        }
    }

    #endregion
}

