﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace TianYiSdtdServerTools.Client.Services.Primitives.UI
{
    public interface IDialogService
    {

        /// <summary>
        /// 显示一个只有确定按钮无返回值的消息框
        /// </summary>
        /// <param name="message"></param>
        /// <param name="title"></param>
        void ShowInformation(string message, string title = "提示");

        /// <summary>
        /// 显示消息框
        /// </summary>
        /// <param name="message"></param>
        /// <param name="title"></param>
        /// <returns></returns>
        bool ShowYesNo(string message, string title = "提示");

        /// <summary>
        /// 显示消息框
        /// </summary>
        /// <param name="message"></param>
        /// <param name="title"></param>
        /// <returns></returns>
        bool ShowOKCancel(string message, string title = "提示");
    }
}