﻿using BaseFun;
using HtmlAgilityPack;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Net;
using System.Text;

namespace TimoControl
{
    public static class platBB
    {
        private static string url_betBase { get; set; }
        private static string acc { get; set; }
        private static string pwd { get; set; }
        private static string pwd_new { get; set; }
        private static CookieContainer ct_bb { get; set; }

        //public static betData bb { get; set; }
        //private static int[] rolltimes = new int[12];
        //private static string[] gameName_Arr;
        /// <summary>
        /// 登录BB后台
        /// </summary>
        /// <returns></returns>
        public static bool loginBB_old()
        {
            try
            {
                string s2 = appSittingSet.readAppsettings("BB");

                acc = s2.Split('|')[0];
                pwd = s2.Split('|')[1];
                url_betBase = s2.Split('|')[2];
            }
            catch (Exception ex)
            {
                appSittingSet.Log("获取配置文件失败" + ex.Message);
                return false;
            }
            HttpWebRequest request = null;
            HttpWebResponse response = null;
            StreamReader reader = null;
            try
            {

                CookieContainer cookie = new CookieContainer();
                //request.CookieContainer = cookie;


                //先获取cookie
                request = WebRequest.Create(url_betBase) as HttpWebRequest;
                request.Method = "GET";
                request.UserAgent = "Mozilla/4.0";
                request.KeepAlive = true;
                request.Accept = "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8";
                request.ContentType = "application/x-www-form-urlencoded; charset=utf-8";
                request.Host = url_betBase.Replace("https://", "").Replace("/", "");
                request.Headers.Add("DNT", "1");
                request.Headers.Add("Upgrade-Insecure-Requests", "1");
                //证书错误
                ServicePointManager.ServerCertificateValidationCallback = delegate { return true; };
                request.ProtocolVersion = HttpVersion.Version11;
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

                //解决502错误的网关
                //request.Proxy = GlobalProxySelection.GetEmptyWebProxy();

                //WebProxy proxy = new WebProxy("http://127.0.0.1:8888", true);
                //proxy.Credentials = new NetworkCredential("user", "pwd", "ADomain");
                //request.Proxy = proxy;
                //request.Credentials = System.Net.CredentialCache.DefaultNetworkCredentials;

                //cookie.Add(new Cookie("T0_IPL_AVRbbbbbbbbbbbbbbbb", "MNOOFMKDHBHCAMEKCNLEPIDGAOCKOCNCCJHEINNAEMJIJJBHDEOHNLINDKNMDEDOHPCEBNGIKNEDKPIMDFOLFBBFMDMAGMAJDINMBMMNKKJDELNMKMDKMEOLEJKIONNL", "/", "."+request.Host));
                //cookie.Add(new Cookie("dtCookie", "1$46210DB143BBD25913E039035E6B51D5|RUM+Default+Application|1", "/", "."+request.Host));
                //cookie.Add(new Cookie("T0_IPL_AVRbbbbbbbbbbbbbbbb", "MNOOFMKDHBHCAMEKCNLEPIDGAOCKOCNCCJHEINNAEMJIJJBHDEOHNLINDKNMDEDOHPCEBNGIKNEDKPIMDFOLFBBFMDMAGMAJDINMBMMNKKJDELNMKMDKMEOLEJKIONNL", "/", "."+request.Host));
                cookie.Add(new Cookie("dtSa", "-", "/", "." + request.Host));
                //request.CookieContainer = cookie;
                request.CookieContainer = cookie;
                response = (HttpWebResponse)request.GetResponse();
                //reader = new StreamReader(response.GetResponseStream(), Encoding.UTF8);
                //string s = reader.ReadToEnd();
                cookie.Add(response.Cookies);
                ct_bb = cookie;
                response.Close();
                request.Abort();

                


                request = WebRequest.Create(url_betBase + "user/login") as HttpWebRequest;
                request.Method = "POST";
                request.UserAgent = "Mozilla/4.0";
                request.KeepAlive = true;
                request.Accept = "application/json, text/plain, */*";
                request.ContentType = "application/x-www-form-urlencoded; charset=utf-8";
                string postdata = string.Format("lang=zh-cn&username={0}&passwd={1}", acc, pwd);

                request.ProtocolVersion = HttpVersion.Version11;
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                //证书错误
                ServicePointManager.ServerCertificateValidationCallback = delegate { return true; };
                //下面三个必须要 不然一直是更改密码
                request.Headers.Add("DNT", "1");
                request.Headers.Add("X-DevTools-Emulate-Network-Conditions-Client-Id", "F07BF0CC-7DA2-4F85-BA71-B82D0DD0CF19");
                request.Headers.Add("X-Requested-With", "XMLHttpRequest");

                byte[] bytes = Encoding.UTF8.GetBytes(postdata);
                request.ContentLength = bytes.Length;
                Stream newStream = request.GetRequestStream();
                newStream.Write(bytes, 0, bytes.Length);
                newStream.Close();

                //CookieContainer cookie = new CookieContainer();
                request.CookieContainer = ct_bb;

                response = (HttpWebResponse)request.GetResponse();
                reader = new StreamReader(response.GetResponseStream(), Encoding.UTF8);
                cookie.Add(response.Cookies);
                ct_bb = cookie;
                /*
                 {"code":200,"status":"OK","data":{"result":true,"session_id":"810c09c9f56ba0b6922446b61e66b5eba01a5a8f","userinfo":{"id":463488589,"parent_id":"94489788","all_parents":[94489788,33746282,33746237,33746121,3819831],"alias":"q","domain":3819831,"role":2,"username":"dqazw","enable":true,"block":false,"sub":true},"redirect":"\/user\/home\/note"}}
                 */
                string ret_html = reader.ReadToEnd();
                if (ret_html.Contains("top.window.location"))
                {
                    //维护
                    appSittingSet.Log("BB维护");
                    return false;
                    //尝试登陆新站
                    //bool b = LoginBB2();
                    //if (b)
                    //    return true;
                    //else
                    //    return false;
                }
                JsonSerializerSettings js = new JsonSerializerSettings();
                js.DateParseHandling = DateParseHandling.DateTime;
                js.DateTimeZoneHandling = DateTimeZoneHandling.Local;
                JObject jo = (JObject)JsonConvert.DeserializeObject(ret_html.ToString(), js);
                if (jo["data"]!=null)
                {
                    if ((bool)jo["data"]["result"])
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
                else
                {
                    return false;
                }
                /*
                if (ret_html.Contains("局查询"))
                {
                    return true;
                }
                else if (ret_html.Contains("网站维护"))
                {
                    appSittingSet.txtLog(string.Format("BB登录失败：{0}    错误：网站维护", postdata));
                    return false;
                }
                else if (ret_html.Contains("130102012"))
                {
                    //错误：请稍等5秒后，再重新登入 (ErrorCode:130102012)
                    //这个状态其实是已经登录了的 ？
                    return true;
                }
                else if (ret_html.Contains("变更密码") || ret_html.Contains("请更新密码"))
                {
                    //需要更新密码
                    //bool bf = ModifyPwd();
                    bool bf = false;
                    return bf;
                }
                else
                {
                    appSittingSet.txtLog(string.Format("BB登录失败：{0}    错误：{1}", postdata, ret_html));
                    return false;
                }
                */
            }
            catch (WebException ex)
            {
                appSittingSet.Log(string.Format("BB站登录失败：{0}   ", ex.Message));
                return false;
            }
            finally
            {
                if (response != null)
                {
                    response.Close();
                    response.Dispose();
                }
                if (reader != null)
                {
                    reader.Close();
                    reader.Dispose();
                }
                if (request != null)
                {
                    request.Abort();
                }
            }
        }

        /*

        /// <summary>
        /// 获取注单详情，计算需要充值的钱
        /// </summary>
        /// <param name="bb"></param>
        /// <returns></returns>
        public static betData getBetDetail(betData bb)
        {

            try
            {
                string url_betorderList = url_betBase + "game/betrecord_search/kind5?SearchData=BetQuery&BarID=2&GameKind=5&Wagersid={0}&Limit=100&Sort=DESC";
                HttpWebRequest request = WebRequest.Create(string.Format(url_betorderList, bb.betno)) as HttpWebRequest;

                request.Method = "GET";
                request.UserAgent = "Mozilla/4.0";
                //request.KeepAlive = true;
                //request.Accept = "application/json, text/plain, * /*";
                request.ContentType = "application/x-www-form-urlencoded; charset=utf-8";
                request.CookieContainer = ct_bb;

                appSittingSet.Init();//证书错误
                HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                StreamReader reader = new StreamReader(response.GetResponseStream(), Encoding.UTF8);
                string ret_html = reader.ReadToEnd();
                reader.Close();
                //   /html/body/div[1]/div[4]/table/tbody/tr/td[7]/span/input
                HtmlAgilityPack.HtmlDocument htmlDocument = new HtmlAgilityPack.HtmlDocument();
                htmlDocument.LoadHtml(ret_html);

                HtmlNode node_tr = htmlDocument.DocumentNode.SelectSingleNode("//table//tbody//tr");
                if (node_tr == null)
                {
                    //注单号不存在
                    bb.passed = false;
                    bb.msg = "请您提供正确的注单号! R";
                    return bb;
                }
                //游戏名称
                string node_game_name = node_tr.SelectSingleNode("//td[@class='text-center gametype']").InnerText;
                bb.gamename = node_game_name; 

                //时间
                string betTime = htmlDocument.DocumentNode.SelectNodes("//table//tbody//tr//td")[0].InnerText;
                DateTime bt = Convert.ToDateTime(betTime).Date;
                DateTime now_d = DateTime.Now.AddHours(-12).Date;
                bb.betTime = betTime;

                if (bt != now_d)
                {
                    bb.passed = false;
                    bb.msg = "您好，非美东时间当天注单，申请不通过！R";
                    return bb;
                }

                //DateTime amestime = AMESTime.BeijingTimeToAMESTime(DateTime.Now);
                //if (bt>amestime.Date && bt<amestime.Date.AddDays(1).AddSeconds(-1))
                //{
                //    bb.passed = false;
                //    bb.msg = "您好，非美东时间当天注单，申请不通过R！";
                //}

                //if (bt > subt.Date && bt < subt.Date.AddDays(1).AddSeconds(-1))
                //{

                //}
                //else
                //{
                //    bb.passed = false;
                //    bb.msg = "您好，非美东时间当天注单，申请不通过！R";
                //    return bb;
                //}



                //判断游戏名称
                bool flag2 = false;
                foreach (var item in gameName_Arr)
                {
                    if (item==node_game_name)
                    {
                        flag2 = true;
                        break;
                    }
                }
                if (!flag2)
                {
                    bb.passed = false;
                    bb.msg = "此活动仅限BBIN电子游艺“连环夺宝”“连环夺宝2”“糖果派对”“糖果派对2”四款游戏 R";
                    return bb;
                }


                //if (node_game_name.Contains(gameName_Arr[0]) || node_game_name.Contains(gameName_Arr[1]) || node_game_name.Contains(gameName_Arr[2]) || node_game_name.Contains(gameName_Arr[3]) )
                //{

                //}

                //if (node_game_name.Contains("糖果派对") || node_game_name.Contains("糖果派对1") || node_game_name.Contains("糖果派对2") || node_game_name.Contains("连环夺宝") || node_game_name.Contains("连环夺宝2"))
                //{

                //}
                //else
                //{
                //    bb.passed = false;
                //    bb.msg = "此活动仅限BBIN电子游艺“连环夺宝”“连环夺宝2”“糖果派对”“糖果派对2”四款游戏 R";
                //    return bb;
                //}


                //钱包账户
                string wallate = htmlDocument.DocumentNode.SelectNodes("//table//tbody//tr//td")[4].InnerText;
                bb.wallet = wallate;
                //子链接
                HtmlNode node = node_tr.SelectSingleNode("//td[@class='text-center  modal-btn ']//span//input");
                string url = node.GetAttributeValue("Value", "");
                //投注金额
                HtmlNode node2 = node_tr.SelectSingleNode("//td[@class='text-center  modal-btn ']//span");
                string betMoney = node2.InnerText.Trim();
                decimal dbetMoney = Convert.ToDecimal(betMoney);//投注金额
                
                //免费游戏直接驳回 不通过
                if (dbetMoney==0)
                {
                    bb.passed = false;
                    bb.msg = "你好，免费游戏注单不享受此活动！R";
                    return bb;
                }

                //bb.betMoney = dbetMoney;

                //HtmlNodeCollection collection = htmlDocument.DocumentNode.SelectSingleNode("//table//tbody//tr//td[@class='text-center  modal-btn ']");//跟Xpath一样，轻松的定位到相应节点下

                //2018-11-22 10:49:37 增加重复提交判断
                //同一注单只能一次 同游戏同用户一天只能一次 美东时间的一天内
                string sql = "select * from record where (betno='" + bb.betno + "' and pass=1) or (username='" + bb.username + "' and gamename='" + bb.gamename + "' and pass=1 and subtime > '" + DateTime.Now.AddHours(-12).ToString("yyyy-MM-dd") + " 00:00:01' and  subtime < '" + DateTime.Now.AddHours(-12).ToString("yyyy-MM-dd") + " 23:59:59') ";
                //string sql = "select * from record where (betno='" + bb.betno + "' and pass=1) or (username='" + bb.username + "' and gamename='" + bb.gamename + "' and pass=1 and date(subtime)='" + DateTime.Now.AddHours(-12).Date + "') ";
                //string sql = "select * from record where (betno='" + bb.betno + "' and pass=1) or (username='" + bb.username + "' and gamename='" + bb.gamename + "' and pass=1 and date(date(subtime),time(subtime),'-0.5 days')='" + DateTime.Now.Date + "') ";
                //string sql = "select * from record where (betno='"+bb.betno+"') or (username='"+bb.username+"' and gamename='"+bb.gamename+"' and pass=1 and subtime > '" + DateTime.Now.Date.ToString("yyyy-MM-dd HH:mm:ss") + "' and  subtime < '" + DateTime.Now.Date.AddDays(1).AddSeconds(-1).ToString("yyyy-MM-dd HH:mm:ss") + "') ";
                if (appSittingSet.recorderDbCheck(sql))
                {
                    bb.passed = false;
                    bb.msg = "您好，同一游戏一天内只能申请一次，申请不通过！R";
                    return bb;
                }

                request = WebRequest.Create(url_betBase + "//" + url) as HttpWebRequest;
                request.CookieContainer = ct_bb;
                response = (HttpWebResponse)request.GetResponse();
                reader = new StreamReader(response.GetResponseStream(), Encoding.UTF8);
                ret_html = reader.ReadToEnd();
                reader.Close();

                //获取次数
                string stimes = "0";
                htmlDocument.LoadHtml(ret_html);
                HtmlNodeCollection node3 = htmlDocument.DocumentNode.SelectNodes("//div[@class='detail-box']//table//tbody//tr");
                
                //从后面王前数 第6个 ？
                if (node3.Count > 6)
                {
                    string[] sa = node3[node3.Count - 6].InnerText.Split(new char[] { '\r', '\n', ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    if (sa.Length > 0)
                    {
                        stimes = sa[0];
                    }
                }

                int itimes;
                //if  (!string.IsNullOrEmpty(stimes))
                int.TryParse(stimes,out itimes);
                //int itimes = Convert.ToInt16(stimes);
                if (itimes==0) //如果没有取到值 有些复杂格式的
                {
                    //找到 小计 行 再往前2行
                    for (int i = 0; i < node3.Count-1; i++)
                    {
                        if (node3[i].InnerHtml.Contains("小计") && node3[i].InnerHtml.Contains("colspan"))
                        {
                            string[] sa = node3[i-2].InnerText.Split(new char[] { '\r', '\n', ' ' }, StringSplitOptions.RemoveEmptyEntries);
                            if (sa.Length > 0)
                            {
                                stimes = sa[0];
                            }
                            break;
                        }
                    }

                    int.TryParse(stimes,out itimes);
                }


                //计算规则 多少次 送多少倍
                if (itimes >= rolltimes[0] && itimes < rolltimes[1])
                {
                    dbetMoney *= rolltimes[2];
                }
                else if (itimes >= rolltimes[3] && itimes < rolltimes[4])
                {
                    dbetMoney *= rolltimes[5];
                }
                else if (itimes >= rolltimes[6] && itimes < rolltimes[7])
                {
                    dbetMoney *= rolltimes[8];
                }
                else if (itimes >= rolltimes[9])
                {
                    dbetMoney *= rolltimes[11];
                }
                else
                {
                    dbetMoney *= 0;
                    bb.msg = "此活动仅限消除 " + rolltimes[0] + " 次及以上，您提供的注单消除次数为：" + itimes + "次，申请不通过！R";
                    bb.passed = false;
                    return bb;
                }

                bb.passed = true;
                bb.betTimes = itimes;
                bb.betMoney = dbetMoney;
                return bb;
            }
            catch (WebException ex)
            {
                if (ex.HResult == -2146233079 || ex.Message.Contains("404"))
                {
                    bb.passed = false;
                    return bb;
                }
                appSittingSet.txtLog(string.Format("用户 {0} 注单{1} bb平台获取注单错误：{2}", bb.username, bb.betno, ex.Message));
                return null;
            }
        }

        */

        /// <summary>
        /// 获取注单旋转次数 GET
        /// </summary>
        /// <param name="bb"></param>
        /// <returns></returns>
        public static betData getBetDetail_Bet_Times(betData bb)
        {
            try
            {

                HttpWebRequest request = WebRequest.Create(url_betBase + "//" + bb.links) as HttpWebRequest;
                request.CookieContainer = ct_bb;
                HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                StreamReader reader = new StreamReader(response.GetResponseStream(), Encoding.UTF8);
                string  ret_html = reader.ReadToEnd();
                reader.Close();
                response.Close();
                response.Dispose();

                //获取次数
                string stimes = "0";
                HtmlDocument htmlDocument = new HtmlDocument();
                htmlDocument.LoadHtml(ret_html);

                if (ret_html.Contains("网站维护"))
                {
                    appSittingSet.Log(string.Format("BB网站维护：{0}    错误：网站维护", bb.betno));
                    bb.passed = false;
                    bb.msg = "平台维护，请稍后提交! R";
                    bb.betTimes = 0;
                    return bb;
                }

                HtmlNodeCollection node3 = htmlDocument.DocumentNode.SelectNodes("//div[@class='detail-box']//table//tbody//tr");

                //从后面王前数 第6个 ？
                //if (node3.Count > 6)
                //{
                //    string[] sa = node3[node3.Count - 6].InnerText.Split(new char[] { '\r', '\n', ' ' }, StringSplitOptions.RemoveEmptyEntries);
                //    if (sa.Length > 0)
                //    {
                //        stimes = sa[0];
                //    }
                //}
                //else
                //{
                //    bb.betTimes = 0;
                //    bb.passed = false;
                //    bb.msg = "旋转次数不足";
                //    return bb;
                //}

                //免费游戏的 包含 FreeGame 字样


                //次数不足的


                int itimes;
                //int.TryParse(stimes, out itimes);
                //找到 小计 行 再往前1行 得到的数字 -1 
                for (int i = node3.Count-1; i >0; i--)
                {
                    if (node3[i].InnerHtml.Contains("小计") && node3[i].InnerHtml.Contains("colspan"))
                    {
                        string[] sa = node3[i - 1].InnerText.Split(new char[] { '\r', '\n', ' ' }, StringSplitOptions.RemoveEmptyEntries);
                        if (sa.Length > 0)
                        {
                            stimes = sa[0];
                        }
                        break;
                    }
                }

                int.TryParse(stimes, out itimes);

                bb.betTimes = itimes - 1 ;
                return bb;
            }
            catch (WebException ex)
            {
                appSittingSet.Log("BB获取次数失败：" + ex.Message);
                bb.betTimes = 0;
                return bb;
            }
        }
        /// <summary>
        /// 获取注单详情
        /// </summary>
        /// <param name="bb"></param>
        /// <returns></returns>
        public static betData getBet_Details(betData bb)
        {
            try
            {
                string url_betorderList = $"{url_betBase}game/betrecord_search/kind5?SearchData=BetQuery&BarID=2&GameKind=5&Wagersid={bb.betno}&Limit=100&Sort=DESC";
                HttpWebRequest request = WebRequest.Create(url_betorderList) as HttpWebRequest;

                request.Method = "GET";
                request.UserAgent = "Mozilla/4.0";
                //request.KeepAlive = true;
                //request.Accept = "application/json, text/plain, */*";
                request.ContentType = "application/x-www-form-urlencoded; charset=utf-8";
                request.CookieContainer = ct_bb;

                request.ProtocolVersion = HttpVersion.Version11;
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

                ServicePointManager.ServerCertificateValidationCallback = delegate { return true; };

                HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                StreamReader reader = new StreamReader(response.GetResponseStream(), Encoding.UTF8);
                string ret_html = reader.ReadToEnd();
                reader.Close();
                //   /html/body/div[1]/div[4]/table/tbody/tr/td[7]/span/input
                HtmlDocument htmlDocument = new HtmlDocument();
                htmlDocument.LoadHtml(ret_html);



                //这里需要判断账号从其他地方登陆的状态 return null 请重新登入,谢谢(ErrorCode: 130102025)
                if (ret_html.Contains("130102025)") || ret_html.Contains("130102029") ||  ret_html.Contains("请重新登入") )
                {
                    loginBB();
                    return null;
                }

                if (ret_html.Contains("网站维护") )
                {
                    appSittingSet.Log(string.Format("BB网站维护：{0}    错误：网站维护", bb.betno));
                    bb.passed = false;
                    bb.msg = "平台维护，请稍后提交! R";
                    return bb;
                }


                HtmlNode node_tr = htmlDocument.DocumentNode.SelectSingleNode("//table//tbody//tr");
                if (node_tr == null)
                {
                    //appSittingSet.txtLog(ret_html);
                    //注单号不存在
                    bb.passed = false;
                    bb.msg = "注单信息不正确，请您提供正确的注单号! R";
                    return bb;
                }
                //游戏名称
                string node_game_name = node_tr.SelectSingleNode("//td[@class='text-center gametype']").InnerText;
                bb.gamename = node_game_name;

                //时间
                string betTime = htmlDocument.DocumentNode.SelectNodes("//table//tbody//tr//td")[0].InnerText;
                bb.betTime = betTime;

                //钱包账户
                string wallate = htmlDocument.DocumentNode.SelectNodes("//table//tbody//tr//td")[4].InnerText;
                bb.wallet = wallate;

                //子链接 //投注金额
                HtmlNode node = node_tr.SelectSingleNode("//td[@class='text-center  modal-btn ']//span//input");
                //HtmlNode node = node_tr.SelectSingleNode("//td[@class='text-center bluecolor ']//span//input");//一周之后的注单没有连接

                if (node!=null)
                {
                    bb.links  = node.GetAttributeValue("Value", "");
                    node = node_tr.SelectSingleNode("//td[@class='text-center  modal-btn ']//span");
                    decimal d = 0;
                    decimal.TryParse(node.InnerText.Trim(), out d);
                    bb.betMoney =d;
                }
                else
                {
                    node = node_tr.SelectSingleNode("//td[@class='text-center bluecolor ']//span//input");//一周之后的注单没有连接
                    bb.links  = node.GetAttributeValue("Value", "");
                    node = node_tr.SelectSingleNode("//td[@class='text-center bluecolor ']//span");//一周之后的注单没有连接
                    decimal d = 0;
                    decimal.TryParse(node.InnerText.Trim(), out d);
                    bb.betMoney =d;
                }

                //投注金额
                //HtmlNode node2 = node_tr.SelectSingleNode("//td[@class='text-center  modal-btn ']//span");
                //if (node2!=null)
                //{
                //    bb.betMoney = Convert.ToDecimal(node2.InnerText.Trim());
                //}
                //else
                //{
                //    node2 = node_tr.SelectSingleNode("//td[@class='text-center bluecolor ']//span");//一周之后的注单没有连接
                //    bb.betMoney = Convert.ToDecimal(node2.InnerText.Trim());
                //}

                bb.passed = true;
                return bb;

            }
            catch (WebException ex)
            {
                //if (ex.HResult == -2146233079 || ex.Message.Contains("404"))
                //{
                //    bb.passed = false;
                //    bb.msg = "查询不到有关注单信息";
                //    appSittingSet.txtLog(string.Format("用户 {0} 注单{1} bb平台获取注单错误：{2}", bb.username, bb.betno, ex.Message));
                //    return bb;
                //}
                appSittingSet.Log(string.Format("用户 {0} 注单{1} bb平台获取注单错误：{2}", bb.username, bb.betno, ex.Message));
                return null;
            }
        }

        /// <summary>
        /// 更新密码 
        /// 返回html 内容中包含 变更密码 请更新密码 字样
        /// </summary>
        /// <returns></returns>
        public static bool ModifyPwd()
        {
            HttpWebRequest request = null;
            HttpWebResponse response = null;
            StreamReader reader = null;
            pwd_new = Guid.NewGuid().ToString().Substring(0, 6); //{B770603A-ECF2-40AF-9E2E-67D281D858F8} 小写字母前面6位
            appSittingSet.Log("BB新密码：" + pwd_new);
            try
            {

                request = WebRequest.Create(url_betBase + "user/passwd") as HttpWebRequest;
                request.Method = "POST";
                request.UserAgent = "Mozilla/4.0";
                //request.KeepAlive = true;
                //request.Accept = "application/json, text/plain, */*";
                request.ContentType = "application/x-www-form-urlencoded; charset=utf-8";
                string postdata = string.Format("old={0}&pwd={1}&rep={1}", pwd, pwd_new);

                request.ProtocolVersion = HttpVersion.Version11;
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                //证书错误
                ServicePointManager.ServerCertificateValidationCallback = delegate { return true; };

                byte[] bytes = Encoding.UTF8.GetBytes(postdata);
                request.ContentLength = bytes.Length;
                Stream newStream = request.GetRequestStream();
                newStream.Write(bytes, 0, bytes.Length);
                newStream.Close();

                CookieContainer cookie = new CookieContainer();
                request.CookieContainer = cookie;

                response = (HttpWebResponse)request.GetResponse();
                reader = new StreamReader(response.GetResponseStream(), Encoding.UTF8);

                string ret_html = reader.ReadToEnd();
                if (ret_html.Contains("請重新登入,謝謝"))
                {
                    appSittingSet.writeAppsettings("BB", "dYHJQR|" + pwd_new + "|https://ag.casiuo.com/");
                    appSittingSet.Log("BB更新密码为 " + pwd_new);
                    loginBB();
                    return true;                    
                }
                JObject jo = JObject.Parse(ret_html);
                if (jo["status"].ToString() == "OK" && jo["code"].ToString() == "200")
                {
                    appSittingSet.writeAppsettings("BB", "dYHJQR|" + pwd_new + "|https://ag.casiuo.com/");
                    appSittingSet.Log("BB更新密码为 " + pwd_new);
                    return true;
                }
                else
                {
                    appSittingSet.Log("BB更新密码失败 " + jo["message"].ToString());
                    return false;
                }
            }
            catch (WebException ex)
            {
                appSittingSet.Log(string.Format("BB站更新密码失败：{0}   ", ex.Message));
                return false;
            }
            finally
            {
                if (response != null)
                {
                    response.Close();
                    response.Dispose();
                }
                if (reader != null)
                {
                    reader.Close();
                    reader.Dispose();
                }
                if (request != null)
                {
                    request.Abort();
                }
            }
        }

        public static bool loginBB()
        {
            try
            {
                string s2 = appSittingSet.readAppsettings("BB");

                acc = s2.Split('|')[0];
                pwd = s2.Split('|')[1];
                url_betBase = s2.Split('|')[2];
            }
            catch (Exception ex)
            {
                appSittingSet.Log("获取配置文件失败" + ex.Message);
                return false;
            }
            HttpWebRequest request = null;
            HttpWebResponse response = null;
            StreamReader reader = null;

            try
            {
                CookieContainer cookie = new CookieContainer();
                //先获取cookie
                //request = WebRequest.Create(url_betBase + "vi/") as HttpWebRequest;
                //request.Method = "GET";
                //request.UserAgent = "Mozilla/4.0";
                //request.KeepAlive = true;
                //request.Accept = "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8";
                //request.ContentType = "application/x-www-form-urlencoded; charset=utf-8";
                //request.Host = url_betBase.Replace("https://", "").Replace("/", "");
                //request.Headers.Add("DNT", "1");
                //request.Headers.Add("Upgrade-Insecure-Requests", "1");
                ////证书错误
                //ServicePointManager.ServerCertificateValidationCallback = delegate { return true; };
                //request.ProtocolVersion = HttpVersion.Version11;
                //ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Ssl3 | SecurityProtocolType.Tls | SecurityProtocolType.Tls11;
                //cookie.Add(new Cookie("T0_IPL_AVRbbbbbbbbbbbbbbbb", "DJHIACLIACKJMGICMAMNAFMEHAOCLFEHHGDEENPIHCMPBCBMOOLKJHPLFPEKEOKDJLFFIBDIHNLDEKBLAMKJPHOGEMOACNKACBBJOEDFBKICMJKOLMJAKGDHKFNODHDA", "/", "." + request.Host));
                //request.CookieContainer = cookie;
                //response = (HttpWebResponse)request.GetResponse();
                //reader = new StreamReader(response.GetResponseStream(), Encoding.UTF8);
                //string s = reader.ReadToEnd();
                //cookie.Add(response.Cookies);
                //ct_bb = cookie;
                //response.Close();
                //request.Abort();




                request = WebRequest.Create(url_betBase + "hex/login") as HttpWebRequest;
                request.Method = "POST";
                request.UserAgent = "Mozilla/4.0";
                request.KeepAlive = true;
                request.Accept = "application/json, text/plain, */*";
                request.ContentType = "application/json;charset=UTF-8";
                request.Referer = url_betBase + "vi/login";
                request.Host = url_betBase.Replace("https://", "").Replace("/", "").Split(':')[0];
                request.Headers.Add("Origin", url_betBase);
                request.Headers.Add("Sec-Fetch-Mode", "cors");
                request.Headers.Add("Sec-Fetch-Site", "same-origin");


                var obj = new { username = acc, password = pwd };
                string postdata = JsonConvert.SerializeObject(obj);
                //string  postdata = "{\"username\":\"" + acc + "\",\"password\":\"" + pwd + "\"}";
                request.ProtocolVersion = HttpVersion.Version11;
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Ssl3 | SecurityProtocolType.Tls | SecurityProtocolType.Tls11;
                //证书错误
                ServicePointManager.ServerCertificateValidationCallback = delegate { return true; };
                //cookie.Add(new Cookie("T0_IPL_AVRbbbbbbbbbbbbbbbb", "PJNHJGLKJNDCGOKEMFFKJGFDNCBGGIJELNGOEBBPILMPGIHDGNGMOAAAJHDMMNJBCCOOJJGCDJJDHPBCGPBIOHMMJNPAGLFNGOONJFPMNMBDEILBJGMNNNPABMCOKLAO"));
                //cookie.Add(new Cookie("langx","zh-cn", "/", "N/A" ));
                //cookie.Add(new Cookie("langcode","zh-cn"));

                request.CookieContainer = cookie;

                byte[] bytes = Encoding.UTF8.GetBytes(postdata);
                request.ContentLength = bytes.Length;
                Stream newStream = request.GetRequestStream();
                newStream.Write(bytes, 0, bytes.Length);
                newStream.Close();

                response = (HttpWebResponse)request.GetResponse();
                reader = new StreamReader(response.GetResponseStream(), Encoding.UTF8);
                cookie.Add(response.Cookies);
                ct_bb = cookie;
                reader = new StreamReader(response.GetResponseStream(), Encoding.UTF8);
                string ret_html = reader.ReadToEnd();

                JsonSerializerSettings js = new JsonSerializerSettings();
                js.DateParseHandling = DateParseHandling.DateTime;
                js.DateTimeZoneHandling = DateTimeZoneHandling.Local;
                JObject jo = (JObject)JsonConvert.DeserializeObject(ret_html.ToString(), js);
                //{"result":true,"data":{"user":{"id":458695702,"username":"dtimo","role":2},"session_id":"ec764cc7e02c6a4f8688a8b83fecf41e7bddb960","password_reset":false,"expire":false},"response_code":"Vb2mi1547621683"}
                //{"result":false,"message":"Username is invalid","code":669001005,"response_code":"6YclV1547618491"}
                if (!(bool)jo["result"])
                {
                    //{"result":false,"message":"In maintenance","code":667046002,"data":{"maintenance":true,"infinite_time":false,"start_at":"2019-05-28 21:00:00","end_at":"2019-05-28 22:40:00"},"response_code":"KGdZi1559096693"}
                    //维护
                    return false;
                }

                if (jo["data"] != null)
                {
                    if (jo["data"]["session_id"] != null)
                    {
                        ct_bb.Add(new Cookie("sid", jo["data"]["session_id"].ToString(), "/", "." + request.Host));
                        ct_bb.Add(new Cookie("aglvl", jo["data"]["session_id"].ToString(), "/", "." + request.Host));
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
                else
                {
                    return false;
                }

            }
            catch (WebException ex)
            {
                appSittingSet.Log(string.Format("BB2站登录失败：{0}   ", ex.Message));
                return false;
            }
        }
    }
}
