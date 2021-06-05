using Senparc.Ncf.Core.Models;
using Senparc.Xncf.CodeBuilder.Models.DatabaseModel.Dto;
using System;
using System.ComponentModel.DataAnnotations.Schema;


namespace Senparc.Xncf.CodeBuilder.Request
{
    public class PageReq
    {
        public int page { get; set; }
        public int limit { get; set; }

        public string key { get; set; }

        public PageReq()
        {
            page = 1;
            limit = 10;
        }
    }
}
