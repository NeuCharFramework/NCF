using Senparc.Ncf.Core.Models;
using System;

namespace Senparc.Areas.Admin.Models.VD
{
    public class BaseAdminUserInfoVD : BaseAdminVD
    {
    }

    public class AdminUserInfo_IndexVD : BaseAdminUserInfoVD
    {
        public PagedList<AdminUserInfo> AdminUserInfoList { get; set; }

    }
    public class AdminUserInfo_EditVD : BaseAdminUserInfoVD
    {    
      	/// <summary>
		/// Id
        /// </summary>		
        public int Id{get; set;}        
		/// <summary>
		/// 密码
        /// </summary>		
        public string Password{get; set;}        
		/// <summary>
		/// 密码盐
        /// </summary>		
        public string PasswordSalt{get; set;}        
		/// <summary>
		/// 真实姓名
        /// </summary>		
        public string RealName{get; set;}        
		/// <summary>
		/// 手机号
        /// </summary>		
        public string Phone{get; set;}        
		/// <summary>
		/// 备注
        /// </summary>		
        public string Note{get; set;}        
		/// <summary>
		/// 当前登录时间
        /// </summary>		
        public DateTime ThisLoginTime{get; set;}        
		/// <summary>
		/// 当前登录IP
        /// </summary>		
        public string ThisLoginIP{get; set;}        
		/// <summary>
		/// 上次登录时间
        /// </summary>		
        public DateTime LastLoginTime{get; set;}        
		/// <summary>
		/// 上次登录Ip
        /// </summary>		
        public string LastLoginIP{get; set;}        
		/// <summary>
		/// 添加时间
        /// </summary>		
        public DateTime AddTime{get; set;}  
        public bool IsEdit { get; set; }
	}
}

