using GWServerPxLib;
using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;


namespace GWServerPx
{
    class Program
    {
        static Logger Logger = LogManager.GetCurrentClassLogger();

        public static string GetSetting(string SettingName)
        {
            return Properties.Settings.Default[SettingName].ToString();
        }

        public static string GetSettingFmt(string SettingName, params object[] args)
        {
            return GetSetting(string.Format(SettingName, args));
        }

        static void Main(string[] args)
        {
            int deadDay;
            int warnDay;

            string EServer;
            int EPort;
            string EPsw;
            string EFrom;
            string EFromName;
            bool EUseSSL;
            string EmailTo;

            string GisLogin;
            string GisPsw;

            string login;
            string password;

            try
            {
                deadDay = int.Parse(GetSetting("DeadDayLic"));
                warnDay = int.Parse(GetSetting("WarnDayLic"));

                EServer = GetSetting("EmailServer");
                EPort = int.Parse(GetSetting("EmailPort"));
                EPsw = GetSetting("EmailFromPsw");
                EFrom = GetSetting("EmailFrom");
                EFromName = GetSetting("EmailFromName");
                EUseSSL = bool.Parse(GetSetting("EmailUseSSL"));
                EmailTo = GetSetting("EmailTo");

                GisLogin = GetSetting("GisServerLogin");
                GisPsw = GetSetting("GisServerPsw");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                Logger.Error(ex, ex.Message);
                return;
            }

            Mail.InitMail(EServer, EPort, EPsw, EUseSSL, EFrom, EFromName, true);

            login = GisLogin;
            password = GisPsw;

            string subj = "GisWare Server";

            DateTime now = DateTime.Now;

            var gW = new GWServerProxy();
            string version = gW.GetVersion();
            string info = "";

            if (gW.Active == 0)
            {
                info = "<h2><font color='red'>GisWare Server не запушен!</font></h2>";
                Console.WriteLine(info);
                Logger.Error("GisWare Server не запушен!");
                Mail.SendMail(EmailTo, subj, info);
                return;
            }

            gW.Login(login, password);

            string ver = gW.Request("ver");
            // вернуть список имеющихся в HASP лицензий после обновления в текстовом формате или в формате html 

            //-запрос на обновление содержимого ключа HASP с сервера компании ИНГИТ.Возвращает 'updated', если содержимое ключа изменилось или 'no changes', если обновлений не произошло
            bool updated = gW.Request("hasp update").Contains("updated");
            if (updated)
                Logger.Info("Обновлено содержиое ключа HASP!");

            string data = gW.Request("hasp text");
            /* ответ в виде текста
            Ключ номер: #####
            Карты 	
            Автодорожная карта Вся Россия, версия 2 (активная до 23:59:59 19.02.2091) (Россия - специальные и автодорожные автодорожные карты (зона 1)) 1:1000000 08-2010	+	
            Лицензия на карту Пользователя (на использование сторонних карт, конвертированных из OSM, SXF и пр.) (активная до 23:59:59 19.02.2091) (Карты Пользователя /лицензия/) 1:1 05-2007	+	
            Системная карта мира непрерывного попрытия (активная до 23:59:59 19.02.2091) (Карты Мира (зона 1)) 1:1 01-2009	+	
            Белгород и Белгородская область, версия 3 (активная до 23:59:59 24.04.2021) (Центральный округ России (зона 1)) 1:10000 02-2016	+	
            Воронеж и Воронежская область, версия 3 (активная до 23:59:59 24.04.2021) (Центральный округ России (зона 1)) 1:10000 11-2015	+	
            Курск и Курская область, версия 3 (активная до 23:59:59 24.04.2021) (Центральный округ России (зона 1)) 1:10000 12-2015	+	
            Воронеж и Воронежская область, версия 3 (2 лицензии активные до 23:59:59 28.09.2021) (Центральный округ России (зона 1)) 1:10000 11-2015	+	
            Белгород и Белгородская область, версия 3 (2 лицензии активные до 23:59:59 03.03.2021) (Центральный округ России (зона 1)) 1:10000 02-2016	New	
            Курск и Курская область, версия 3 (2 лицензии активные до 23:59:59 03.03.2021) (Центральный округ России (зона 1)) 1:10000 12-2015	New	

            Программы 	
            Программный модуль GisMaster, версия 9.1 для самостоятельной поддержки карт, создания карт других производителей и путем импорта из официальных форматов. (активная до 23:59:59 19.02.2091) 	+	
            Лицензия на обозреватель карт с клиентом облачных адресов (активная до 23:59:59 19.02.2091) 	+	
            Лицензия на серверную часть линейки программ GISWARE - GIS сервер (активная до 23:59:59 19.02.2091) 	+	
            Лицензия на дополнительный комплект функций Логистика картографических ядер линейки GWX (активная до 23:59:59 19.02.2091) 	New	
            Лицензия на картографическое ядро GWX-CS (активная до 23:59:59 19.02.2091) 	New	
            Лицензия на дополнительный комплект функций Логистика картографических ядер линейки GWX (2 лицензии активные до 23:59:59 19.02.2091) 	New	
            Лицензия на картографическое ядро GWX-CS (2 лицензии активные до 23:59:59 19.02.2091) 	New	
            */

            gW.Logout();

            string regDateStr = @"(\d{2}:\d{2}:\d{2}) (\d{2}.\d{2}.\d{4})"; //@"(\d{2}:.*)\s(\d{2}.\d{2}.\d{4})";

            var strList = data.Split('\n');

            string keyNumber = "";

            List<Row> rows = new List<Row>();

            bool nowApp = false;

            foreach (string str in strList)
            {
                string stemp = str.Trim('\n', '\r');
                var tmp = stemp.Split('\t');
                stemp = tmp.Count() > 0 ? tmp[0] : null;

                if (string.IsNullOrEmpty(stemp))
                    continue;
                else if (stemp.StartsWith("Ключ номер:"))
                {
                    keyNumber = stemp;
                    continue;
                }
                else if (stemp.StartsWith("Программы"))
                {
                    nowApp = true;
                    continue;
                }
                else if (stemp.StartsWith("Карты"))
                {
                    nowApp = false;
                    continue;
                }

                DateTime? endDate = null;

                Regex regex = new Regex(regDateStr, RegexOptions.IgnoreCase | RegexOptions.Multiline);
                MatchCollection matches = regex.Matches(stemp);

                if (matches.Count > 0 && matches[0].Groups.Count == 3)
                {
                    endDate = DateTime.Parse(matches[0].Groups[2].Value + " " + matches[0].Groups[1].Value);
                }

                if (endDate.HasValue)
                {
                    var diff = endDate.Value - now;
                    bool needAlert = diff.Days < 5;
                    rows.Add(new Row { Name = stemp, EndDate = endDate.Value, SendAlert = needAlert, IsApp = nowApp, DiffDays = diff });
                }
            }

            List<string> alerts = new List<string>();
            string color = "<font color='blue'>";
            foreach (var row in rows.Where(w => w.SendAlert).OrderBy(o => o.IsApp))
            {
                TimeSpan diff = row.DiffDays;
                if (diff.Days <= warnDay)
                    color = "<font color='orange'>";
                if (diff.Days <= deadDay)
                    color = "<font color='red'>";
                // string deadline = string.Format("{0} д. {1}", diff.Days, diff.ToString(@"hh\:mm\:ss\.fff"));
                string deadline = string.Format("{0} д. {1}", diff.Days, diff.ToString(@"hh\:mm\:ss"));
                alerts.Add($"{row.Name} , <b>{color}срок действия до {row.EndDate}</font> (осталось {deadline})</b>");
            }

            if (alerts.Count > 0)
            {
                string msg = $"<h3>{keyNumber}, {ver}</h3>";
                msg += "<h3>Истекает срок действия лицензий!!!</h3>" + string.Join("<br/>", alerts);
                msg += $"<br><h4><font color='blue'>Текущая дата: {now}</font></h4>";
                Logger.Warn("Истекает срока действия лицензий!!!\n" + string.Join("\n", alerts));
                Mail.SendMail(EmailTo, subj, msg);
            }

            info = "";
            info += keyNumber + "\n";
            info += "Карты:\n";
            foreach (var row in rows.Where(w => !w.IsApp))
            {
                info += $"{row.Name}, дата конца: {row.EndDate}\n";
            }

            info += "Приложения:\n";
            foreach (var row in rows.Where(w => w.IsApp))
            {
                info += $"{row.Name}, дата конца: {row.EndDate}\n";
            }

            Console.WriteLine(info);
            Logger.Info(info);
        }
    }
}
