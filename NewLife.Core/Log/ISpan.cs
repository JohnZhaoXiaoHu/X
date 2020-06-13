﻿using System;
using System.Threading;
using System.Web.Script.Serialization;
using System.Xml.Serialization;

namespace NewLife.Log
{
    /// <summary>性能跟踪片段。轻量级APM</summary>
    public interface ISpan : IDisposable
    {
        /// <summary>跟踪标识。可用于关联多个片段，建立依赖关系。当前线程上下文自动关联</summary>
        String TraceId { get; set; }

        /// <summary>时间。Unix毫秒</summary>
        Int64 Time { get; set; }

        /// <summary>错误信息</summary>
        Exception Error { get; set; }

        /// <summary>片段耗时</summary>
        Int32 Cost { get; }
    }

    /// <summary>性能跟踪片段。轻量级APM</summary>
    public class DefaultSpan : ISpan
    {
        #region 属性
        /// <summary>构建器</summary>
        [XmlIgnore, ScriptIgnore]
        public ISpanBuilder Builder { get; }

        /// <summary>跟踪标识。可用于关联多个片段，建立依赖关系。当前线程上下文自动关联</summary>
        public String TraceId { get; set; }

        /// <summary>时间。Unix毫秒</summary>
        public Int64 Time { get; set; }

        /// <summary>错误信息</summary>
        public Exception Error { get; set; }

        /// <summary>耗时。单位ms</summary>
        public Int32 Cost { get; private set; }

        private Boolean _finished;

        private static readonly ThreadLocal<String> _traceId = new ThreadLocal<String>();
        private Boolean _create_traceId;
        #endregion

        #region 构造
        /// <summary>实例化</summary>
        /// <param name="builder"></param>
        public DefaultSpan(ISpanBuilder builder)
        {
            Builder = builder;
            Time = DateTime.UtcNow.ToLong();
        }

        /// <summary>释放资源</summary>
        public void Dispose() => Dispose(true);

        /// <summary>释放资源，参数表示是否由Dispose调用。重载时先调用基类方法</summary>
        /// <param name="disposing"></param>
        protected virtual void Dispose(Boolean disposing)
        {
            if (disposing)
            {
                // 释放托管资源
                //OnDispose(disposing);

                // 告诉GC，不要调用析构函数
                GC.SuppressFinalize(this);
            }

            // 释放非托管资源
            Finish();
        }

        /// <summary>析构函数</summary>
        /// <remarks>
        /// 如果忘记调用Dispose，这里会释放非托管资源
        /// 如果曾经调用过Dispose，因为GC.SuppressFinalize(this)，不会再调用该析构函数
        /// </remarks>
        ~DefaultSpan() { Dispose(false); }
        #endregion

        #region 方法
        /// <summary>设置跟踪标识</summary>
        public void SetTracerId()
        {
            // 如果本线程已有跟踪标识，则直接使用
            if (_traceId.IsValueCreated) TraceId = _traceId.Value;
            // 否则创新新的跟踪标识，并绑定到本线程
            if (TraceId.IsNullOrEmpty())
            {
                _traceId.Value = TraceId = Guid.NewGuid() + "";
                _create_traceId = true;
            }
        }

        /// <summary>完成跟踪</summary>
        private void Finish()
        {
            if (_finished) return;
            _finished = true;

            Cost = (Int32)(DateTime.UtcNow.ToLong() - Time);

            // 从本线程中清除跟踪标识
            if (_create_traceId) _traceId.Value = null;

            Builder.Finish(this);
        }
        #endregion
    }
}