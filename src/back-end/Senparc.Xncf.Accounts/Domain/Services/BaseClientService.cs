using AutoMapper;
using Microsoft.EntityFrameworkCore.Storage;
using Senparc.Ncf.Core.Models;
using Senparc.Ncf.Repository;
using Senparc.Ncf.Service;
using System;

namespace Senparc.Xncf.Accounts.Domain
{
    public interface IBaseClientService<T> : IClientServiceBase<T> where T : EntityBase//global::System.Data.Objects.DataClasses.EntityObject, new()
    {
        
    }


    public class BaseClientService<T> : ClientServiceBase<T>, IBaseClientService<T> where T : EntityBase//global::System.Data.Objects.DataClasses.EntityObject, new()
    {
        public BaseClientService(IClientRepositoryBase<T> repo, IServiceProvider serviceProvider)
            : base(repo, serviceProvider)
        {

        }
    }
}