using NLog;
using System;
using System.Linq;
using System.Net;
using System.Net.Mail;

namespace GWServerPx
{
    public static class Mail
    {
        static Logger Logger = LogManager.GetCurrentClassLogger();

        static string EmailFrom;
        static string EmailFromName;
        static int EmailPort = 0;
        static string EmailServer;
        static string EmailFromPsw;
        static bool EnableSSL = false;
        static bool IsHTML = false;

        public static void InitMail(string server, int port, string pswd, bool useSsl, string from, string fromName, bool isHtml = false)
        {
            EmailFrom = from;
            EmailFromName = fromName;
            EmailPort = port;
            EmailServer = server;
            EmailFromPsw = pswd;
            EnableSSL = useSsl;
            IsHTML = isHtml;
        }

        public static void SendMail(string receivers, string subject, string message, string attach = "")
        {
            // отправитель - устанавливаем адрес и отображаемое в письме имя
            MailAddress from = new MailAddress(
                EmailFrom,
                EmailFromName);

            // создаем объект сообщения
            MailMessage m = new MailMessage
            {
                From = from
            };

            // кому отправляем
            var receiverList = receivers.Split(';');

            if (receiverList.Count() == 0)
            {
                Logger.Error("Ошибка при отправки email. Не указаны получатели!");
                return;
            }

            foreach (string receiver in receiverList)
            {
                try
                {
                    MailAddress to = new MailAddress(receiver);
                    m.To.Add(to);
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, ex.Message + "\n" + ex.StackTrace);
                    return;
                }
            }

            // тема письма
            m.Subject = subject;

            // текст письма
            m.Body = message;

            // письмо представляет код html
            m.IsBodyHtml = IsHTML;

            // вложение/я
            var attachList = attach.Split(';');
            if (attachList.Count() > 0)
            {
                foreach (string file in attachList)
                {
                    if (!String.IsNullOrEmpty(file))
                        m.Attachments.Add(new Attachment(file));
                }
            }

            // адрес smtp-сервера и порт, с которого будем отправлять письмо
            using (var smtp = new SmtpClient(EmailServer, EmailPort))
            {
                try
                {
                    // логин и пароль
                    smtp.Credentials = new NetworkCredential(EmailFrom, EmailFromPsw);
                    smtp.EnableSsl = EnableSSL;
                    smtp.Send(m);
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, ex.Message + "\n" + ex.StackTrace);
                }
            }

        }
    }
}
