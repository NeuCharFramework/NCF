using System;
using System.Collections.Generic;
using System.Linq;
using Senparc.CO2NET;
using Senparc.Ncf.Core.Extensions;
using Senparc.Ncf.Log;
using Senparc.Ncf.Utility;
using Microsoft.Extensions.DependencyInjection;

namespace Senparc.Xncf.Accounts.Domain.OperationQueue
{
    public enum OperationQueueType
    {
        更新用户头像,
        活动消息记录
    }


    public class OperationQueue
    {
        /// <summary>
        /// 列队数据集合
        /// </summary>
        private static Dictionary<string, OperationQueueItem> MessageQueueDictionary = new Dictionary<string, OperationQueueItem>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// 同步执行锁
        /// </summary>
        private static object MessageQueueSyncLock = new object();
        /// <summary>
        /// 立即同步所有缓存执行锁（给OperateQueue()使用）
        /// </summary>
        private static object FlushCacheLock = new object();

        /// <summary>
        /// 生成Key
        /// </summary>
        /// <param name="name">列队应用名称，如“ContainerBag”</param>
        /// <param name="senderType">操作对象类型</param>
        /// <param name="identityKey">对象唯一标识Key</param>
        /// <param name="actionName">操作名称，如“UpdateContainerBag”</param>
        /// <returns></returns>
        public static string GenerateKey(string name, Type senderType, string identityKey, string actionName)
        {
            var key = string.Format("Name@{0}||Type@{1}||Key@{2}||ActionName@{3}",
                name, senderType, identityKey, actionName);
            return key;
        }

        /// <summary>
        /// 操作列队
        /// </summary>
        public static void OperateQueue()
        {
            lock (FlushCacheLock)
            {
                var mq = new OperationQueue();
                var key = mq.GetCurrentKey(); //获取最新的Key
                var serviceProvider = SenparcDI.GetServiceProvider();
                while (!string.IsNullOrEmpty(key))
                {
                    try
                    {

                        var operationQueueService = serviceProvider.GetService<OperationQueueService>();
                        var mqItem = mq.GetItem(key); //获取任务项

                        //识别类型
                        switch (mqItem.OperationQueueType)
                        {
                            case OperationQueueType.更新用户头像:
                                operationQueueService.UpdateAccountHeadImgAsync(serviceProvider, (int)mqItem.Data[0], mqItem.Data[1] as string).Wait();
                                break;
                            default:
                                LogUtility.OperationQueue.ErrorFormat("OperationQueueType未处理：{0}", mqItem.OperationQueueType.ToString());
                                break;
                        }
                    }
                    catch (Exception ex)
                    {
                        LogUtility.OperationQueue.ErrorFormat($"OperationQueue列队操作失败，已经跳过：{ex.Message}", ex);
                    }

                    mq.Remove(key); //清除
                    key = mq.GetCurrentKey(); //获取最新的Key
                }
            }
        }

        /// <summary>
        /// 获取当前等待执行的Key
        /// </summary>
        /// <returns></returns>
        public string GetCurrentKey()
        {
            lock (MessageQueueSyncLock)
            {
                return MessageQueueDictionary.Keys.FirstOrDefault();
            }
        }

        /// <summary>
        /// 获取OperationQueueItem
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public OperationQueueItem GetItem(string key)
        {
            lock (MessageQueueSyncLock)
            {
                if (MessageQueueDictionary.ContainsKey(key))
                {
                    return MessageQueueDictionary[key];
                }
                return null;
            }
        }

        /// <summary>
        /// 添加列队成员
        /// </summary>
        /// <param name="key"></param>
        /// <param name="action"></param>
        public OperationQueueItem Add(string key, OperationQueueType operationQueueType, List<object> data, string description = null)
        {
            lock (MessageQueueSyncLock)
            {
                //if (!MessageQueueDictionary.ContainsKey(key))
                //{
                //    MessageQueueList.Add(key);
                //}
                //else
                //{
                //    MessageQueueList.Remove(key);
                //    MessageQueueList.Add(key);//移动到末尾
                //}

                var mqItem = new OperationQueueItem(key, operationQueueType, data, description);
                MessageQueueDictionary[key] = mqItem;
                return mqItem;
            }
        }

        /// <summary>
        /// 移除列队成员
        /// </summary>
        /// <param name="key"></param>
        public void Remove(string key)
        {
            lock (MessageQueueSyncLock)
            {
                if (MessageQueueDictionary.ContainsKey(key))
                {
                    MessageQueueDictionary.Remove(key);
                    //MessageQueueList.Remove(key);
                }
            }
        }

        /// <summary>
        /// 获得当前列队数量
        /// </summary>
        /// <returns></returns>
        public int GetCount()
        {
            lock (MessageQueueSyncLock)
            {
                return MessageQueueDictionary.Count;
            }
        }

    }
}
