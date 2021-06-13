using Senparc.Ncf.Core.Models;
using Senparc.Xncf.CodeBuilder.Models.DatabaseModel.Dto;
using System;
using System.ComponentModel.DataAnnotations.Schema;


namespace Senparc.Xncf.CodeBuilder.Request
{
    /// <summary>
	/// 代码生成器的表信息
	/// </summary>
    public class CreateVueReq 
    {
        /// <summary>
        /// 代码生成模版Id
        /// </summary>
        public int Id { get; set; }
        /// <summary>
        /// vue项目所在的根目录，如：D:\OpenAuth.Pro\Client
        /// </summary>
        public string VueProjRootPath { get; set; }
    }
}