using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Senparc.Areas.Admin.Models.VD;
using Senparc.CO2NET.Extensions;
using Senparc.Ncf.Core.Enums;
using Senparc.Ncf.Core.Models;
using Senparc.Mvc.Filter;
using Senparc.Office;
using Senparc.Ncf.Service;
using Senparc.Ncf.Utility;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Senparc.Ncf.Service;

namespace Senparc.Areas.Admin.Controllers
{
    [MenuFilter("Account")]
    public class AccountController : BaseAdminController
    {
        private readonly AccountService _accountService;
        private readonly SenparcEntities _senparcEntities;

        public AccountController(AccountService accountService, SenparcEntities senparcEntities)
        {
            _accountService = accountService;
            _senparcEntities = senparcEntities;
        }

        public ActionResult Index(string kw = null, int pageIndex = 1)
        {
            var seh = new SenparcExpressionHelper<Account>();
            seh.ValueCompare.AndAlso(true, z => !z.Flag)
                .AndAlso(!kw.IsNullOrEmpty(), z => z.RealName.Contains(kw) || z.NickName.Contains(kw) || z.UserName.Contains(kw) || z.Phone.Contains(kw));
            var where = seh.BuildWhereExpression();

            var modelList = _accountService.GetObjectList(pageIndex, 20, where, z => z.Id, OrderingType.Descending);
            var vd = new Account_IndexVD()
            {
                AccountList = modelList,
                kw = kw
            };
            return View(vd);
        }
        public ActionResult Edit(int id = 0)
        {
            bool isEdit = id > 0;
            var vd = new Account_EditVD();
            if (isEdit)
            {
                var model = _accountService.GetObject(z => z.Id == id);
                if (model == null)
                {
                    return RenderError("信息不存在！");
                }
                vd.Id = model.Id;
                vd.UserName = model.UserName;
                vd.NickName = model.NickName;
                vd.RealName = model.RealName;
                vd.Phone = model.Phone;
                vd.Note = model.Note;
            }
            vd.IsEdit = isEdit;
            return View(vd);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(Account_EditVD model)
        {
            bool isEdit = model.Id > 0;

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            Account account = null;
            if (isEdit)
            {
                account = _accountService.GetObject(z => z.Id == model.Id);
                if (account == null)
                {
                    return RenderError("信息不存在！");
                }
            }
            else
            {
                account = new Account()
                {
                    AddTime = DateTime.Now,
                    UserName = _accountService.GetNewUserName(),
                    NickName = ""
                };
            }
            try
            {
                if (_accountService.CheckPhoneExisted(account.Id, model.Phone))
                {
                    ModelState.AddModelError("Phone", "手机号码重复");
                    return View(model);
                }

                await this.TryUpdateModelAsync(account, ""
               , z => z.RealName
               , z => z.Phone
               , z => z.Note);

                this._accountService.SaveObject(account);
                base.SetMessager(MessageType.success, $"{(isEdit ? "修改" : "新增")}成功！");
                return RedirectToAction("Index");
            }
            catch (Exception)
            {
                base.SetMessager(MessageType.danger, $"{(isEdit ? "修改" : "新增")}失败！");
                return RedirectToAction("Index");
            }
        }

        [HttpPost]
        public ActionResult Delete(List<int> ids)
        {
            try
            {
                var objList = _accountService.GetFullList(z => ids.Contains(z.Id), z => z.Id, OrderingType.Ascending);
                _accountService.DeleteAll(objList);
                SetMessager(MessageType.success, "删除成功！");
            }
            catch (Exception e)
            {
                SetMessager(MessageType.danger, $"删除失败【{e.Message}！");
            }
            return RedirectToAction("Index");
        }
    }
}
