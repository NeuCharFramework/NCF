using JWT.Algorithms;
using JWT.Builder;
using JWT.Exceptions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Senparc.Ncf.Core.Enums;
using Senparc.Ncf.Repository;
using Senparc.Ncf.Service;
using Senparc.Xncf.FileServer.Models.DatabaseModel.Dto;
using Senparc.Xncf.FileServer.Utility;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Senparc.Xncf.FileServer.Services
{
    public class FileRecordService : ServiceBase<FileRecord>
    {
        public FileRecordService(IRepositoryBase<FileRecord> repo, IServiceProvider serviceProvider)
            : base(repo, serviceProvider)
        {
        }
        public Task AddAsync(FileRecord fileRecord)
        {
            return base.SaveObjectAsync(fileRecord);
        }

        //TODO: 更多业务方法可以写到这里
    }
}
