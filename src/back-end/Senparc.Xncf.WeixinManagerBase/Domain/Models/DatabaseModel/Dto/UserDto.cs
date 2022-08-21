using Senparc.Ncf.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Senparc.Xncf.WeixinManagerBase.Domain.Models.DatabaseModel.Dto
{
    public class User_CreateOrUpdateDto: DtoBase
    {
        public int Id { get; set; }
        public string UserName { get; set; }
        public string Password { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public string MpOpenId { get; set; }
        public string UnionId { get; set; }

        public string NickName { get; set; }

        public int Sex { get; set; }
        /// <summary>
        ///用户的语言，简体中文为zh_CN
        /// </summary>
        public string Language { get; set; }

        public string City { get; set; }

        public string Province { get; set; }

        public string Country { get; set; }
        public string HeadImgUrl { get; set; }
    }

    public class User_ViewDto : DtoBase
    {
        public string UserName { get; set; }
        public string Password { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public string MpOpenId { get; set; }
        public string UnionId { get; set; }

        public string NickName { get; set; }

        public int Sex { get; set; }
        /// <summary>
        ///用户的语言，简体中文为zh_CN
        /// </summary>
        public string Language { get; set; }

        public string City { get; set; }

        public string Province { get; set; }

        public string Country { get; set; }
        public string HeadImgUrl { get; set; }
    }
}
