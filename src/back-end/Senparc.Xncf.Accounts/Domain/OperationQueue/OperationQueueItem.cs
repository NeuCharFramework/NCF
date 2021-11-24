using Senparc.Xncf.Accounts.Domain.OperationQueue;
using System;
using System.Collections.Generic;

namespace Senparc.Xncf.Accounts.Domain.OperationQueue
{
    /// <summary>
    /// 操作列队项
    /// </summary>
    public class OperationQueueItem
    {
        /// <summary>
        /// 列队项唯一标识
        /// </summary>
        public string Key { get; set; }
        /// <summary>
        /// 列队项目命中触发时执行的委托
        /// </summary>
        public OperationQueueType OperationQueueType { get; set; }
        /// <summary>
        /// 此实例对象的创建时间
        /// </summary>
        public DateTime AddTime { get; set; }

        /// <summary>
        /// 储存数据
        /// </summary>
        public List<object> Data { get; set; }

        /// <summary>
        /// 项目说明（主要用于调试）
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// 初始化SenparcMessageQueue消息列队项
        /// </summary>
        /// <param name="key"></param>
        /// <param name="data"></param>
        /// <param name="description"></param>
        /// <param name="operationQueueType"></param>
        public OperationQueueItem(string key, OperationQueueType operationQueueType, List<object> data, string description = null)
        {
            Key = key;
            OperationQueueType = operationQueueType;
            Data = data;
            Description = description;
            AddTime = DateTime.Now;
        }
    }
}
