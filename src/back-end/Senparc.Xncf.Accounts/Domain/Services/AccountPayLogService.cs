using Senparc.CO2NET;
using Senparc.Service.ACL;
using Senparc.Ncf.Core.Enums;
using Senparc.Xncf.Accounts.Domain.Models;
using Senparc.Ncf.Log;
using Senparc.Ncf.Repository;
using System;
using Microsoft.Extensions.DependencyInjection;
using Senparc.Xncf.Accounts.Domain.Services;

namespace Senparc.Xncf.Accounts.Domain.Services
{

    public class AccountPayLogService : BaseClientService<AccountPayLog>
    {
        public AccountPayLogService(AccountPayLogRepository accountPayLogRepo, IServiceProvider serviceProvider)
            : base(accountPayLogRepo, serviceProvider)
        {
            
        }

        public AccountPayLog GetByOrderNumber(string orderNumber, params string[] includes)
        {
            return this.GetObject(z => z.OrderNumber == orderNumber, includes);
        }

        public AccountPayLog CreateOrder(FullAccount fullAccount, string orderNumber, string ip, decimal totalFee, string description, decimal getPoints, AccountPayLog_PayType payType)
        {
            AccountPayLog accountPayLog = new AccountPayLog()
            {
                OrderNumber = orderNumber, //Add的时候会自动生成
                AddTime = DateTime.Now,
                CompleteTime = DateTime.Now,
                AccountId = fullAccount.Id,
                AddIp = ip,
                PayMoney = totalFee,
                Price = totalFee,
                Fee = totalFee,
                Description = description ?? "",
                Status = (int)AccountPayLog_Status.未支付,
                Type = 1,
                GetPoints = getPoints,
                PayType = (int)payType,
                UsedPoints = 0,
                TotalPrice = totalFee,
                OrderType = 0,
                PayParam = ""
            };
            SaveObject(accountPayLog);
            return accountPayLog;
        }

        public void CancelOrder(AccountPayLog accountPayLog)
        {
            accountPayLog.CompleteTime = DateTime.Now;
            accountPayLog.Status = (int)AccountPayLog_Status.已取消;
            SaveObject(accountPayLog);
        }


        public void PayLogFinish(AccountPayLog accountPayLog)
        {
            if (accountPayLog.Status != (int)AccountPayLog_Status.未支付)
            {
                return;
            }
            //try
            //{
            AccountService accountService = _serviceProvider.GetService<AccountService>();
            Account account = accountService.GetObject(z => z.Id == accountPayLog.AccountId);
            BeginTransaction(() =>
            {
                accountPayLog.Status = (int)AccountPayLog_Status.已支付;
                accountPayLog.CompleteTime = DateTime.Now;
                account.Points += accountPayLog.PayMoney;
                accountService.SaveObject(account);
                SaveObject(accountPayLog);
            }, ex =>
            {
                LogUtility.AccountPayLog.Error($"支付完成，发生错误：{ex.Message}", ex);
            });
            //using (var transcation = BeginTransaction())
            //{

            //    transcation.Commit();
            //}
            //}
            //catch (Exception ex)
            //{

            //    throw new Exception($"支付回调失败【{ex.Message}】");
            //}
        }

        public override void SaveObject(AccountPayLog obj)
        {
            bool isInsert = base.IsInsert(obj);
            base.SaveObject(obj);
            LogUtility.WebLogger.InfoFormat("AccountPayLog{2}：{0}（ID：{1}，当前状态：{3}）", obj.OrderNumber, obj.Id,
                isInsert ? "新增" : "编辑", (AccountPayLog_Status)obj.Status);
        }

        public override void DeleteObject(AccountPayLog obj)
        {
            LogUtility.WebLogger.InfoFormat("AccountPayLog被删除：{0}（ID：{1}）", obj.OrderNumber, obj.Id);
            base.DeleteObject(obj);
        }
    }
}