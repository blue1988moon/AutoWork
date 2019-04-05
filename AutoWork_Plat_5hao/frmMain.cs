﻿using Quartz;
using Quartz.Impl;
using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;
using TimoControl;

namespace AutoWork_Plat_5hao
{
    public delegate void Write(string msg);//写lv消息
    public delegate void ClsListItem();

    public partial class frmMain : Form
    {
        static string platname;
        static string FolderPath;
        static int interval;
        static int Timeout;
        static string[] AutoCls;
        static string RunModel;

        public frmMain()
        {
            InitializeComponent();
            platname = appSittingSet.readAppsettings("platname");
            interval = int.Parse(appSittingSet.readAppsettings("Interval"));
            Timeout = int.Parse(appSittingSet.readAppsettings("Timeout"));
            AutoCls = appSittingSet.readAppsettings("AutoCls").Split('|');
            RunModel =  appSittingSet.readAppsettings("RunModel");
            FolderPath = appSittingSet.readAppsettings("FolderPath");
            //相对路径 或者 绝对路径
            if (FolderPath.StartsWith("\\"))
            {
                FolderPath = Environment.CurrentDirectory + FolderPath;
            }
            this.Text = platname;
        }

        #region 窗体事件
        private void frmMain_Load(object sender, EventArgs e)
        {
            MyWrite = Write;
            mycls = ClsListItem;
            ////打开文件监控
            //WatcherStart(FolderPath, "*.txt");

            //先登陆一次
            MyJob1 myjob1 = new MyJob1();
            myjob1.Execute(null);
            //开始调度
            start();

        }
        private void button1_Click(object sender, EventArgs e)
        {
            //bool b= plat5hao.readFromFile(FolderPath);
            //list_dep = plat5hao.listFromFile(FolderPath);

            //登录一遍
            MyJob1 myjob1 = new MyJob1();
            myjob1.Execute(null);

            appSittingSet.Log("手动操作登录");
        }

        private void button2_Click(object sender, EventArgs e)
        {
            appSittingSet.showLogFile();
        }

        private void button3_Click(object sender, EventArgs e)
        {
            string filePath = Application.ExecutablePath + ".config";
            System.Diagnostics.Process.Start("notepad.exe", filePath);
        }

        private void button4_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("应用程序重启", "提示", MessageBoxButtons.OKCancel, MessageBoxIcon.Asterisk, MessageBoxDefaultButton.Button1) == DialogResult.OK)
            {
                notifyIcon1.Dispose();
                Application.Restart();
            }
        }

        private void frmMain_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (sched != null)
            {
                sched.Shutdown();
            }
            appSittingSet.sendEmail(platname + " 程序关闭", "程序关闭 ");

            notifyIcon1.Dispose();
        }

        private void frmMain_SizeChanged(object sender, EventArgs e)
        {
            if (this.WindowState == FormWindowState.Minimized)
            {
                this.Hide();   //隐藏窗体
                notifyIcon1.Visible = true; //使托盘图标可见
                notifyIcon1.ShowBalloonTip(6000);
            }
        }

        private void notifyIcon1_MouseClick(object sender, MouseEventArgs e)
        {
            if (this.WindowState == FormWindowState.Minimized)
            {
                this.Show();
                this.WindowState = FormWindowState.Normal;
            }
        }

        private void notifyIcon1_BalloonTipClicked(object sender, EventArgs e)
        {
            this.Show();
            this.WindowState = FormWindowState.Normal;
        }

        #endregion
        #region 文件监控 下载目录用不上
        private static void WatcherStart(string path, string filter)
        {

            FileSystemWatcher watcher = new FileSystemWatcher();
            watcher.Path = path;
            watcher.Filter = filter;
            watcher.Created += new FileSystemEventHandler(OnProcess);
            watcher.Deleted += new FileSystemEventHandler(OnProcess);
            watcher.Changed += new FileSystemEventHandler(OnProcess);
            watcher.EnableRaisingEvents = true;
            //watcher.NotifyFilter = NotifyFilters.Attributes | NotifyFilters.CreationTime | NotifyFilters.DirectoryName | NotifyFilters.FileName | NotifyFilters.LastAccess | NotifyFilters.LastWrite | NotifyFilters.Security | NotifyFilters.Size;
            watcher.NotifyFilter = NotifyFilters.CreationTime | NotifyFilters.DirectoryName | NotifyFilters.FileName |NotifyFilters.LastAccess | NotifyFilters.LastWrite ;
            watcher.IncludeSubdirectories = true;
        }

        static string LastChangeFileName;
        private static void OnProcess(object source, FileSystemEventArgs e)
        {
            if (e.ChangeType == WatcherChangeTypes.Created)
            {
                //文件创建 merge 到数据库
                bool b = plat5hao.dbFromFile(FolderPath);
                appSittingSet.Log(string.Format("文件到数据库 {0}", b? "成功" : "失败"));
            }
            else if (e.ChangeType == WatcherChangeTypes.Deleted)
            {
                //文件删除 删除 对应名字的记录 文件名不会重复 不用改状态 直接删除
                string sql = "DELETE FROM FileHistory WHERE FileName = '" + e.Name.Replace(".txt", "") + "' AND  Status = '1';";
                sql += "DELETE FROM DepositInfo  WHERE    FileName = '"+e.Name.Replace(".txt", "") + "' and status='1';";
                bool b= appSittingSet.execSql(sql,true);
            }
            else if (e.ChangeType == WatcherChangeTypes.Changed)
            {
                if (!(LastChangeFileName == e.Name))
                {
                    //文件创建 merge 到数据库
                    bool b = plat5hao.dbFromFile(FolderPath);
                    appSittingSet.Log(string.Format("文件到数据库 {0}", b? "成功" : "失败"));
                }
            }
            LastChangeFileName = e.Name;
        }


        #endregion 

        #region 写UI ListView 消息

        public static Write MyWrite;
        void Write(object msg)
        {
            if (lvRecorder.IsHandleCreated)
            {
                lvRecorder.BeginInvoke(new Action(() =>
                {
                    lvRecorder.Items.Insert(0, msg.ToString() + "  " + DateTime.Now.ToLongTimeString());
                }));
            }
        }

        public static ClsListItem mycls;
        void ClsListItem()
        {
            if (lvRecorder.IsHandleCreated)
            {
                lvRecorder.BeginInvoke(new Action(() =>
                {
                    lvRecorder.Items.Clear();
                }));
            }
        }

        #endregion

        #region 调度
        static IScheduler sched;
        /// <summary>
        /// 开始调度
        /// </summary>
        public static void start()
        {

            //创建一个作业调度池
            ISchedulerFactory schedf = new StdSchedulerFactory();
            sched = schedf.GetScheduler();

            //加入作业调度池中

            //0 6 12 18 小时执行 登陆
            sched.ScheduleJob(JobBuilder.Create<MyJob1>().Build(), TriggerBuilder.Create().WithCronSchedule("0 0 0,6,12,18 * * ? ").Build());

            //5秒一次 读取提交列表
            sched.ScheduleJob(JobBuilder.Create<MyJob2>().Build(), TriggerBuilder.Create().WithSimpleSchedule(x => x.WithIntervalInSeconds(interval).RepeatForever()).Build());
            //清除一周前的数据、日志文件
            if (AutoCls[1] == "1")
            {
                sched.ScheduleJob(JobBuilder.Create<MyJob01>().Build(), TriggerBuilder.Create().WithCronSchedule("1 0 8 1/1 * ? ").Build());
            }

            //开始运行
            sched.Start();
        }

        /// <summary>
        /// /0 6 12 18 小时登录
        /// </summary>
        public class MyJob1 : IJob
        {
            public void Execute(IJobExecutionContext context)
            {
                //清除listbox 信息
                mycls();
                string msg = string.Format("5hao站登录{0} ", plat5hao.login() ? "成功" : "失败");
                appSittingSet.Log(msg);
                MyWrite(msg);
            }
        }
        /// <summary>
        /// 每天8:00:01 执行 删除一周前的日志 数据库一周前的数据
        /// </summary>
        public class MyJob01 : IJob
        {
            public void Execute(IJobExecutionContext context)
            {
                int diff = int.Parse(AutoCls[0]);
                string sql = "delete from History where time < '" + DateTime.Now.AddDays(-diff).Date.ToString("yyyy-MM-dd") + "'";
                appSittingSet.execSql(sql);
                appSittingSet.Log("清除一周前的数据");
                appSittingSet.clsLogFiles(diff);
                appSittingSet.Log("清除一周前的日志");
            }
        }


        /// <summary>
        /// 遍历数据库新数据，处理网页数据
        /// </summary>
        [DisallowConcurrentExecution]
        public class MyJob2 : IJob
        {
            public void Execute(IJobExecutionContext context)
            {
                //获取网页等待处理的数据
                List<Recharge> list_rc = plat5hao.getList_apply();
                if (list_rc == null)
                {
                    MyWrite("没有获取到申请信息，等待下次执行 ");
                    return;
                }
                if (list_rc.Count == 0)
                {
                    MyWrite("没有新的申请信息，等待下次执行 ");
                    return;
                }

                foreach (var item_rc in list_rc)
                {
                    //如果是1分钟内的重复提交 数据 取消掉一笔
                    if (!item_rc.IsRepeat)
                    {
                        //对比历史记录，是否重复提交 金额 余额？？没有必要

                        //查到用户姓名
                        Recharge rc = plat5hao.getBankAccName(item_rc);
                        if (rc.RealName.Count == 0)
                        {
                            MyWrite(" 没有获取到银行卡信息，等待下次执行 ");
                            //取消 通过 还是不做操作
                            return;
                        }

                        //从文件夹获取数据 文件创建 merge 到数据库
                        bool b = plat5hao.dbFromFile(FolderPath);
                        appSittingSet.Log(string.Format("文件到数据库 {0}", b ? "成功" : "失败"));

                        //前一天12:00:00点以后的数据 同网站后台一样
                        List<DepositInfo> list_dep = plat5hao.getLits_db(3);
                        if (list_dep.Count == 0)
                        {
                            MyWrite("没有新的本地银行记录信息，等待下次执行 ");
                            return;
                        }

                        foreach (DepositInfo item_d in list_dep)
                        {
                            if (item_rc.RealName.Contains( item_d.Account) && item_rc.RechargeMoney == item_d.Deposit)
                            {
                                item_rc.OperateType = 1;
                                item_rc.Name = item_d.Account;
                                break;
                            }
                            else
                            {
                                item_rc.OperateType = 3;
                                continue;
                            }
                        }
                    }
                    else
                    {
                        //通过 回填 不通过
                        item_rc.OperateType = 3;
                        bool r = plat5hao.confirm(item_rc);
                        string msg = string.Format("用户{0}，存入金额{1} ,处理结果为：取消(重复提交) ,处理{2}", item_rc.UserName, item_rc.RechargeMoney, r ? "成功" : "失败");
                        MyWrite(msg);
                        appSittingSet.Log(msg);
                    }

                    if (item_rc.OperateType == 1)
                    {
                        //通过 回填 通过
                        bool r = plat5hao.confirm(item_rc);
                        //bool r = true;//测试
                        string msg = string.Format("用户{0}，申请金额{1} ,处理结果为：通过 ,处理{2}", item_rc.UserName, item_rc.RechargeMoney, r ? "成功" : "失败");
                        MyWrite(msg);
                        appSittingSet.Log(msg);

                        //记录到通过的信息到数据库
                        string sql = "INSERT INTO History ( UserName, RechargeMoney, Status, Time )  VALUES ( '" + item_rc.UserName + "', " + item_rc.RechargeMoney + ", '" + item_rc.OperateType + "', '" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "' );";
                        //更改标识 0-1 (如果有>1笔以上的 应该更新 余额最小的一笔)
                        //sql += "UPDATE DepositInfo SET   Status = '1' WHERE Account = '" + item_rc.Name + "' AND  Deposit = '" + item_rc.RechargeMoney + "' AND Status = '0' ;";
                        sql += "UPDATE DepositInfo set Status = '1' WHERE Account = '"+item_rc.Name+ "' AND  Deposit = '" + item_rc.RechargeMoney + "'  AND  Status = '0' And rowid =(select min(rowid) from DepositInfo where Account = '"+item_rc.Name+ "'  AND  Deposit = '" + item_rc.RechargeMoney + "'  AND  Status = '0');";
                        r = appSittingSet.execSql(sql);
                    }
                    else
                    {
                        //不通过等待 timeout 10分钟 如果超时了 就取消掉
                        if (item_rc.AddTime.AddMinutes(Timeout) < DateTime.Now)
                        {
                            //通过 回填 不通过
                            bool r = plat5hao.confirm(item_rc);
                            //bool r = true;//测试
                            string msg = string.Format("用户{0}，申请金额{1} ,处理结果为：取消(超时) ,处理{2}", item_rc.UserName, item_rc.RechargeMoney, r ? "成功" : "失败");
                            MyWrite(msg);
                            appSittingSet.Log(msg);
                            //更新数据库信息0-1  不存在的记录 不用更新
                            //string sql = "UPDATE DepositInfo SET   Status = '1' WHERE Account = '" + item_rc.UserName + "' AND  Deposit = '" + item_rc.RechargeMoney + "' AND Status = '0' ;";
                            //appSittingSet.execSql(sql);
                        }
                        else
                        {
                            string msg = string.Format("用户{0}，申请金额{1} ,流水号{2}：等待处理，已经经过{3}分{4}秒 ", item_rc.UserName, item_rc.RechargeMoney,item_rc.SerialNumber, DateTime.Now.Subtract(item_rc.AddTime).Minutes, DateTime.Now.Subtract( item_rc.AddTime).Seconds);
                            MyWrite(msg);
                        }
                    }
                }
            }
        }


        #endregion

    }
}
