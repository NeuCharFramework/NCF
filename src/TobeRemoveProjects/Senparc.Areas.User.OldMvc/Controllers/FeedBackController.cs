using Microsoft.AspNetCore.Mvc;
using Senparc.Areas.User.Models.VD;
using Senparc.Ncf.Core.Enums;
using Senparc.Ncf.Core.Models;
using Senparc.Ncf.Log;
using Senparc.Mvc.Filter;
using Senparc.Service;
using Senparc.Ncf.Utility;
using System;
using System.Linq;
using Senparc.Ncf.Service;

namespace Senparc.Areas.User.Controllers
{
    [MenuFilter("FeedBack")]
    public class FeedBackController : BaseUserController
    {
        private readonly FeedBackService _feedBackService;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="feedBackService"></param>
        public FeedBackController(FeedBackService feedBackService)
        {
            _feedBackService = feedBackService;
        }

        /// <summary>
        /// 列表
        /// </summary>
        /// <param name="pageIndex"></param>
        /// <returns></returns>
        public ActionResult Index(int pageIndex = 1)
        {
            var vd = new FeedBack_IndexVD()
            {
            };
            return View(vd);
        }

        /// <summary>
        /// 获取反馈列表信息
        /// </summary>
        /// <param name="pageIndex"></param>
        /// <param name="pageSize"></param>
        /// <returns></returns>
        [HttpGet, Route("api/[controller]/get")]
        public IActionResult Get(int pageIndex = 1, int pageSize = 20)
        {
            var seh = new SenparcExpressionHelper<FeedBack>();
            var where = seh.BuildWhereExpression();

            var modelList = _feedBackService.GetObjectList(pageIndex, pageSize, where, z => z.AddTime, OrderingType.Descending, new string[] { nameof(Account) });

            return RenderJsonSuccessResult(true, modelList.Select(z => new
            {
                z.Id,
                z.Content,
                z.AddTime,
                z.Account?.PicUrl,
                z.Account?.Email,
                z.Account?.UserName
            }));
        }

        /// <summary>
        /// 获取反馈信息
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpGet, Route("api/[controller]/get/{id}")]
        public IActionResult Get(int id)
        {
            var obj = _feedBackService.GetObject(z => z.Id == id, new string[] { nameof(Account) });
            if (obj == null)
            {
                return RenderJsonSuccessResult(false, new
                {
                    Message = "信息不存在"
                });
            }

            return RenderJsonSuccessResult(true, new
            {
                obj.Id,
                obj.Content,
                obj.AddTime,
                obj.Account?.PicUrl,
                obj.Account?.NickName,
                obj.Account?.UserName
            });
        }

        /// <summary>
        /// 编辑页面
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public ActionResult Edit(int id = 0)
        {
            var vd = new FeedBack_EditVD();

            return View(vd);
        }

        /// <summary>
        /// 提交编辑
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost]
        public IActionResult Edit([FromBody]FeedBack model)
        {
            if (!ModelState.IsValid)
            {
                return RenderJsonSuccessResult(false, new
                {
                    Message = ModelState.Values.First(z => z.Errors.Count > 0).Errors[0].ErrorMessage
                });
            }
            try
            {
                var obj = _feedBackService.CreateOrUpdate(model.Content, FullAccount.Id, model.Id);
                return RenderJsonSuccessResult(true, new
                {
                    id = obj.Id
                });
            }
            catch (Exception ex)
            {
                LogUtility.SystemLogger.Error(ex.Message, ex);
                return RenderJsonSuccessResult(false, new
                {
                    Message = $"保存失败【{ex.Message}】"
                });
            }
        }

        /// <summary>
        /// 删除反馈
        /// </summary>
        /// <param name="ids"></param>
        /// <returns></returns>
        [HttpGet]
        public ActionResult Delete(int id)
        {
            try
            {
                var obj = _feedBackService.GetObject(z => z.Id == id && z.AccountId == FullAccount.Id);
                if (obj == null)
                {
                    return RenderJsonSuccessResult(false, new
                    {
                        Message = "信息不存在"
                    });
                }
                _feedBackService.DeleteObject(obj);
                return RenderJsonSuccessResult(true, new
                {
                    Message = "删除成功"
                });
            }
            catch (Exception ex)
            {
                LogUtility.SystemLogger.Error(ex.Message, ex);
                return RenderJsonSuccessResult(false, new
                {
                    Message = "删除失败"
                });
            }
        }
    }
}