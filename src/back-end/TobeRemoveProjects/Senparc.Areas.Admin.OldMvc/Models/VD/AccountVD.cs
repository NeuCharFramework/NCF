using Senparc.Ncf.Core.Enums;
using Senparc.Ncf.Core.Models;
using System.ComponentModel.DataAnnotations;

namespace Senparc.Areas.Admin.Models.VD
{
    public class BaseAccountVD : BaseAdminVD
    {
    }

    public class Account_IndexVD : BaseAccountVD
    {
        public PagedList<Account> AccountList { get; set; }
        public string kw { get; set; }
    }
    public class Account_EditVD : BaseAccountVD
    {
        /// <summary>
        /// Id
        /// </summary>		
        public int Id { get; set; }

        /// <summary>
        /// NickName
        /// </summary>	
        public string NickName { get; set; }
        /// <summary>
        /// RealName
        /// </summary>		
        [Required(ErrorMessage = "请输入真实姓名")]
        [MaxLength(10, ErrorMessage = "真实姓名不能超过10个字符")]
        public string RealName { get; set; }
        /// <summary>
        /// Phone
        /// </summary>	
        [Required(ErrorMessage = "请输入手机号")]
        [Phone(ErrorMessage = "请输入正确手机号")]
        public string Phone { get; set; }
        public string Email { get; set; }
        /// <summary>
        /// Note
        /// </summary>		
        public string Note { get; set; }

        /// <summary>
        /// 岗位
        /// </summary>
        public string Post { get; set; }
        /// <summary>
        /// 部门
        /// </summary>
        public string Branch { get; set; }

        public bool IsEdit { get; set; }
    }
}