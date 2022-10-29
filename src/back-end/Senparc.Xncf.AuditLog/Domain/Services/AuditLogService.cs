using Senparc.Ncf.Core.Enums;
using Senparc.Ncf.Log;
using Senparc.Ncf.Repository;
using Senparc.Ncf.Service;
using Senparc.Xncf.AuditLog.Domain.Services;
using Senparc.Xncf.AuditLog.Models.DatabaseModel.Dto;
using System;
using System.Threading.Tasks;

namespace Senparc.Xncf.AuditLog.Domain.Services
{
    public class AuditLogService : BaseClientService<AuditLogInfo>
    {
        public AuditLogService(IClientRepositoryBase<AuditLogInfo> repo, IServiceProvider serviceProvider)
            : base(repo, serviceProvider)
        {
        }


        public async Task<AuditLogInfo> CreateAuditLogAsync(AuditLogInfo objDto)
        {
            string userName = objDto.UserName;
            string ipAddress = objDto.IpAddress;
            string actionName = objDto.ActionName;
            string actionTime = objDto.ActionTime;
            var obj = new AuditLogInfo(userName, ipAddress, actionName, actionTime);
            await SaveObjectAsync(obj);
            return obj;
        }


        public void CreateAuditLogInfo(string userName,string ipAddress,string actionName,string actionTime)
        {
            var obj = new AuditLogInfo(userName, ipAddress, actionName, actionTime);
            SaveObject(obj);
        }
        public override void SaveObject(AuditLogInfo obj)
        {
            var isInsert = base.IsInsert(obj);
            base.SaveObject(obj);
            //LogUtility.WebLogger.InfoFormat("AuditLogInfo{2}：{0}（ID：{1}）", obj.UserName, obj.Id, isInsert ? "新增" : "编辑");
        }
    }
}
