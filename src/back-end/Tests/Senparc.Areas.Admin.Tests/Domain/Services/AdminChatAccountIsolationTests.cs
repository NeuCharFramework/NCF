/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：AdminChatAccountIsolationTests.cs
    文件功能描述：AdminChat 按账号隔离与超级管理员用量统计测试

----------------------------------------------------------------*/
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Senparc.Areas.Admin.Domain.Models;
using Senparc.Areas.Admin.Domain.Models.DatabaseModel;
using Senparc.Areas.Admin.Domain.Services;
using Senparc.Areas.Admin.OHS.Local.AppService;
using Senparc.Ncf.UnitTestExtension;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Senparc.Areas.Admin.Tests.Domain.Services
{
    [TestClass]
    public class AdminChatAccountIsolationTests : TestBase
    {
        private readonly AdminChatSessionService _sessionService;
        private readonly AdminChatMessageService _messageService;

        public AdminChatAccountIsolationTests()
        {
            _sessionService = base._serviceProvider.GetRequiredService<AdminChatSessionService>();
            _messageService = base._serviceProvider.GetRequiredService<AdminChatMessageService>();
        }

        [TestMethod]
        public async Task SessionIsolation_CrossUserAccess_ShouldBeDenied()
        {
            var owner = await GetSeedUserAsync(0);
            var intruder = await GetSeedUserAsync(1);

            var session = await _sessionService.CreateSessionAsync("隔离测试", owner.Id);
            Assert.IsNotNull(session);
            Assert.AreEqual(owner.Id, session.UserId);

            // 属主可正常访问
            Assert.IsNotNull(await _sessionService.GetSessionByIdAsync(session.Id, owner.Id));
            // 其他账号无法访问
            Assert.IsNull(await _sessionService.GetSessionByIdAsync(session.Id, intruder.Id));
            Assert.IsFalse(await _sessionService.ArchiveSessionAsync(session.Id, intruder.Id));
            Assert.IsFalse(await _sessionService.DeleteSessionAsync(session.Id, intruder.Id));
        }

        [TestMethod]
        public async Task MessageOwnership_CrossUserFeedback_ShouldBeDenied()
        {
            var owner = await GetSeedUserAsync(2);
            var intruder = await GetSeedUserAsync(3);

            var session = await _sessionService.CreateSessionAsync("消息归属测试", owner.Id);
            var message = await _messageService.AddMessageAsync(session.Id, ChatMessageRoleType.Assistant, "hello isolation");

            // 属主消息可获取，可设置反馈
            Assert.IsNotNull(await _messageService.GetOwnedMessageAsync(message.Id, owner.Id));
            Assert.IsTrue(await _messageService.SetMessageFeedbackAsync(message.Id, owner.Id, MessageFeedbackType.Like));

            // 其他账号无法获取或修改消息
            Assert.IsNull(await _messageService.GetOwnedMessageAsync(message.Id, intruder.Id));
            Assert.IsFalse(await _messageService.SetMessageFeedbackAsync(message.Id, intruder.Id, MessageFeedbackType.Dislike));
            Assert.IsNull(await _messageService.GetOwnedMessageAsync(-1, owner.Id));
        }

        [TestMethod]
        public async Task UserStats_CountsOnly_ShouldReflectPerUserSessionsAndMessages()
        {
            var userA = await GetSeedUserAsync(4);
            var userB = await GetSeedUserAsync(5);

            var sessionA = await _sessionService.CreateSessionAsync("统计A", userA.Id);
            await _messageService.AddMessageAsync(sessionA.Id, ChatMessageRoleType.User, "msg1");
            await _messageService.AddMessageAsync(sessionA.Id, ChatMessageRoleType.Assistant, "msg2");
            await _sessionService.ArchiveSessionAsync(sessionA.Id, userA.Id);

            var sessionB = await _sessionService.CreateSessionAsync("统计B", userB.Id);
            await _messageService.AddMessageAsync(sessionB.Id, ChatMessageRoleType.User, "msg3");
            await _sessionService.DeleteSessionAsync(sessionB.Id, userB.Id);

            var stats = await _sessionService.GetUserSessionStatsAsync();
            var statA = stats.Single(z => z.UserId == userA.Id);
            var statB = stats.Single(z => z.UserId == userB.Id);

            Assert.AreEqual(1, statA.TotalSessionCount);
            Assert.AreEqual(1, statA.ArchivedSessionCount);
            Assert.AreEqual(0, statA.ActiveSessionCount);
            Assert.AreEqual(0, statA.DeletedSessionCount);
            Assert.IsTrue(statA.LastActiveTime != default);

            Assert.AreEqual(1, statB.TotalSessionCount);
            Assert.AreEqual(1, statB.DeletedSessionCount);

            var messageCounts = await _messageService.GetMessageCountByUserAsync();
            Assert.AreEqual(2, messageCounts.Single(z => z.UserId == userA.Id).Count);
            Assert.AreEqual(1, messageCounts.Single(z => z.UserId == userB.Id).Count);
        }

        [TestMethod]
        public void SuperAdminRoleCheck_Claims_ShouldDetectAdministratorRoleOnly()
        {
            var appService = new SuperAdminProbeService(base._serviceProvider);

            // administrator 角色（单值/多值/大小写/空格）均为超级管理员
            Assert.IsTrue(appService.IsCurrentAdminSuperAdmin(BuildHttpContext(new[] { "administrator" })));
            Assert.IsTrue(appService.IsCurrentAdminSuperAdmin(BuildHttpContext(new[] { "Administrator, operator" })));
            Assert.IsFalse(appService.IsCurrentAdminSuperAdmin(BuildHttpContext(new[] { "Admin, operator" })));
            Assert.IsTrue(appService.IsCurrentAdminSuperAdmin(BuildHttpContext(new[] { "ADMINISTRATOR" })));

            // 普通角色或无角色均不是超级管理员
            Assert.IsFalse(appService.IsCurrentAdminSuperAdmin(BuildHttpContext(new[] { "operator" })));
            Assert.IsFalse(appService.IsCurrentAdminSuperAdmin(BuildHttpContext(Array.Empty<string>())));
        }

        [TestMethod]
        public void CurrentAdminId_Claims_ShouldResolveNameIdentifier()
        {
            var appService = new SuperAdminProbeService(base._serviceProvider);

            var context = new DefaultHttpContext();
            context.User = new ClaimsPrincipal(new ClaimsIdentity(
                new[] { new Claim(ClaimTypes.NameIdentifier, "42") }, "test"));
            Assert.AreEqual(42, appService.GetCurrentAdminUserInfoId(context));

            var emptyContext = new DefaultHttpContext();
            emptyContext.User = new ClaimsPrincipal(new ClaimsIdentity(new Claim[] { }, "test"));
            Assert.AreEqual(-1, appService.GetCurrentAdminUserInfoId(emptyContext));
        }

        private static async Task<Senparc.Areas.Admin.Domain.Models.AdminUserInfo> GetSeedUserAsync(int index)
        {
            var dataList = BaseNcfUnitTest.GlobalDataList.GetDataList<Senparc.Areas.Admin.Domain.Models.AdminUserInfo>();
            return dataList[index];
        }

        private static HttpContext BuildHttpContext(string[] roleClaimValues)
        {
            var context = new DefaultHttpContext();
            var identity = new ClaimsIdentity(new Claim[] { }, "test");
            foreach (var value in roleClaimValues)
            {
                identity.AddClaim(new Claim(ClaimTypes.Role, value));
            }
            context.User = new ClaimsPrincipal(identity);
            return context;
        }

        /// <summary>
        /// LocalAppServiceBase 的具体实现，用于测试角色判定逻辑
        /// </summary>
        private class SuperAdminProbeService : LocalAppServiceBase
        {
            public SuperAdminProbeService(IServiceProvider serviceProvider) : base(serviceProvider)
            {
            }
        }
    }
}
