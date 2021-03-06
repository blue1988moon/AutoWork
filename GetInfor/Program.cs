﻿using BaseFun;
using Quartz;
using Quartz.Impl;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Data;
using System.Threading.Tasks;
using TimoControl;

namespace GetInfor
{
    class Program
    {
        static Hashtable myConfig = appSittingSet.readConfig("appconfig");
        static string[] connectionString = myConfig["MySqlConnect"].ToString().Split('|');
        static int[] interval = Array.ConvertAll(myConfig["Interval"].ToString().Split('|'), int.Parse);
        static int[] nums = Array.ConvertAll(myConfig["nums"].ToString().Split('|'), int.Parse);

        static void Main(string[] args)
        {
            bool b = platGPK.loginGPK();
            if (b)
            {
                Console.WriteLine("开始处理详细数据");
                //执行
                TestAsyncJob();
                Console.ReadLine();
            }
            else
            {
                Console.WriteLine("GPK登陆失败");
            }
        }

        [DisallowConcurrentExecution]
        public class job_GetAcc : IJob
        {
            public Task Execute(IJobExecutionContext context)
            {
                if (myConfig["dbpath"].ToString().Contains("2"))
                    ToDB2();
                else
                    ToDB0();
                return Task.CompletedTask;
            }
        }

        static async Task TestAsyncJob()
        {
            var props = new NameValueCollection
            {
                { "quartz.serializer.type", "binary" }
            };
            StdSchedulerFactory schedFact = new StdSchedulerFactory(props);

            IScheduler sched = await schedFact.GetScheduler();
            await sched.Start();


            //间隔
            await sched.ScheduleJob(JobBuilder.Create<job_GetAcc>().Build(), TriggerBuilder.Create().WithSimpleSchedule(x => x.WithIntervalInSeconds(interval[0]).RepeatForever()).Build());

            //间隔10分钟
            //await sched.ScheduleJob(JobBuilder.Create<job_2DB>().Build(), TriggerBuilder.Create().WithSimpleSchedule(x => x.WithIntervalInSeconds(interval[1]).RepeatForever()).Build());

            //间隔20分钟
            //await sched.ScheduleJob(JobBuilder.Create<job_Addiction>().Build(), TriggerBuilder.Create().WithSimpleSchedule(x => x.WithIntervalInSeconds(interval[2]).RepeatForever()).Build());
        }

        private static void ToDB0()
        {
            string sql = "select lastid from maxid order by rowid desc limit 1";
            int maxid = SQLiteHelper.SQLiteHelper.execScalarSql<int>(sql);
            //maxid = maxid == "" ? "0" : maxid;
            sql = $"select  DISTINCT(account),id from give where `status`=1 and id> {maxid} ORDER BY id  limit {nums[0]};";
            //获取用户名 
            MySQLHelper.MySQLHelper.connectionString = connectionString[0];
            DataTable dt = MySQLHelper.MySQLHelper.Query(sql).Tables[0];
            if (dt.Rows.Count > 0)
            {
                int count = 1;
                List<string> sql_list = new List<string>();
                foreach (DataRow item in dt.Rows)
                {
                    //如果存在就跳过 数据库已经配置好了唯一约束
                    sql_list.Add(string.Format("insert  or ignore  into infor (account,id)  values('{0}',{1});", item[0].ToString().Replace("'",""), item[1]));
                    if (sql_list.Count == 500 || sql_list.Count == dt.Rows.Count % 500)
                    {
                        SQLiteHelper.SQLiteHelper.execSql(sql_list);
                        sql_list.Clear();
                    }
                    Console.WriteLine($"正在获取数据 { +count + "/" + dt.Rows.Count }--{ DateTime.Now}");
                    maxid = int.Parse(item[1].ToString());
                    count++;
                }
                bool r = SQLiteHelper.SQLiteHelper.execSql($" insert into maxid (lastid) values ({maxid});");
                //插入详细
                ToDB1(maxid);
                //补充数据
                ToDB2();
            }
            else
            {
                Console.WriteLine("没有数据" + DateTime.Now.ToString());
            }
        }

        private static void ToDB1(int maxid)
        {
            try
            {
                //int maxid = 0;
                string sql = $"select account,id from infor where status=0 order by id limit {nums[1]};";
                DataTable dt = SQLiteHelper.SQLiteHelper.getDataTableBySql(sql);
                int count = 1;
                List<string> sql_list = new List<string>();
                //普通循环
                if (dt.Rows.Count > 0)
                {
                    foreach (DataRow item in dt.Rows)
                    {
                        Gpk_UserDetail user = platGPK.GetUserDetail(item[0].ToString());
                        if (user == null)
                        {
                            //删掉就行了 更改状态为3
                            sql = "update infor set status=3 where id='" + item[1].ToString() + "';";
                            SQLiteHelper.SQLiteHelper.execSql(sql);
                            continue;
                        }

                        sql_list.Add("REPLACE INTO detail(Account,Birthday,Email,Id,Mobile,Sex,Wallet,LatestLogin_IP,LatestLogin_time,LatestLogin_Id,BankAccount,BankName,City,Province,BankMemo,RegisterDevice,RegisterUrl ,Name,QQ ) VALUES('" + user.Account + "','" + user.Birthday + "','" + user.Email + "','" + user.Id + "','" + user.Mobile + "','" + user.SexString + "'," + user.Wallet + ",'" + user.LatestLogin_IP + "','" + user.LatestLogin_time + "','" + user.LatestLogin_Id + "','" + user.BankAccount + "','" + user.BankName + "','" + user.City + "','" + user.Province + "','" + user.BankMemo + "','" + user.RegisterDevice + "', '" + user.RegisterUrl + "' ,'" + user.Name + "','" + user.QQ + "' );update infor set status=1 where id=" + item[1].ToString() + ";");
                        if (sql_list.Count == 10 || sql_list.Count == dt.Rows.Count % 10)
                        {
                            SQLiteHelper.SQLiteHelper.execSql(sql_list);
                            sql_list.Clear();
                        }
                        //maxid = int.Parse(item[1].ToString());
                        Console.WriteLine($"正在存储第{count + "/" + dt.Rows.Count}条数据，用户名{ item[0]} --{DateTime.Now}");
                        count++;
                    }
                    //更新行数 
                    bool r = SQLiteHelper.SQLiteHelper.execSql($"update maxid set rows={dt.Rows.Count } where lastid={maxid};");
                }
                else
                {
                    Console.WriteLine("没有存储数据" + DateTime.Now.ToString());
                }
            }
            catch (Exception ex)
            {
                appSittingSet.Log(ex.Message);
            }

        }

        private static void ToDB2()
        {
            try
            {
                //int maxid = 0;
                string sql = $"select account,id from detail where DepositTimes is null and DepositTotal is null order by addtime limit {nums[2]};";
                DataTable dt = SQLiteHelper.SQLiteHelper.getDataTableBySql(sql);
                int count = 1;
                List<string> sql_list = new List<string>();
                //普通循环
                if (dt.Rows.Count > 0)
                {
                    
                    foreach (DataRow item in dt.Rows)
                    {
                        //查询 存取款次数 太慢
                        Gpk_UserDetail gup = new Gpk_UserDetail() { Id = item[1].ToString(), Account = item[0].ToString() };
                        betData bb = platGPK.MemberGetDepositWithdrawInfo(gup);
                        if (bb == null)
                        {
                            bb = new betData() { WithdrawTimes = 0, WithdrawTotal = 0, DepositTotal = 0, DepositTimes = 0 };
                        }
                        sql = $"update detail  set DepositTimes={bb.DepositTimes},DepositTotal={bb.DepositTotal},WithdrawTimes={bb.WithdrawTimes},WithdrawTotal={bb.WithdrawTotal} where id={item[1]} and account='{item[0]}';";
                        sql_list.Add(sql);

                        if (sql_list.Count == 10 || sql_list.Count == dt.Rows.Count % 10)
                        {
                            SQLiteHelper.SQLiteHelper.execSql(sql_list);
                            sql_list.Clear();
                        }
                        //maxid = int.Parse(item[1].ToString());
                        Console.WriteLine($"正在补充第{count + "/" + dt.Rows.Count}条数据，用户名{item[0]} --{DateTime.Now}");
                        count++;
                    }
                    

                    /*
                    Parallel.ForEach(dt.AsEnumerable(), (item) =>
                    {
                        //查询 存取款次数 太慢
                        Gpk_UserDetail gup = new Gpk_UserDetail() { Id = item[1].ToString(), Account = item[0].ToString() };
                        betData bb = platGPK.MemberGetDepositWithdrawInfo(gup);
                        if (bb == null)
                        {
                            bb = new betData() { WithdrawTimes = 0, WithdrawTotal = 0, DepositTotal = 0, DepositTimes = 0 };
                        }
                        sql = $"update detail  set DepositTimes={bb.DepositTimes},DepositTotal={bb.DepositTotal},WithdrawTimes={bb.WithdrawTimes},WithdrawTotal={bb.WithdrawTotal} where id={item[1]} and account='{item[0]}';";
                        sql_list.Add(sql);

                        if (sql_list.Count == 10 || sql_list.Count == dt.Rows.Count % 10)
                        {
                            SQLiteHelper.SQLiteHelper.execSql(sql_list);
                            sql_list.Clear();
                        }
                        //maxid = int.Parse(item[1].ToString());
                        Console.WriteLine($"正在补充第{count + "/" + dt.Rows.Count}条数据，用户名{item[0]} --{DateTime.Now}");
                        count++;
                    });
                    */

                }
                else
                {
                    Console.WriteLine("没有补充数据" + DateTime.Now.ToString());
                }
            }
            catch (Exception ex)
            {
                appSittingSet.Log(ex.Message);
            }
        }
    }
}
