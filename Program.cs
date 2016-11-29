using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using pavver;

namespace whirlvidBot
{
    class Program
    {
        static void Main(string[] args)
        {

            Http = new HttpMaster();
            _rand = new Random();

            // Загружаем настройки приложения
            _options = new Ini(Environment.CurrentDirectory + "/options.ini");

            string _login = _options.IniReadValue("Auth", "Login");
            string _pass = _options.IniReadValue("Auth", "Pass");

            int sleep = 5000;
            try
            {
                sleep = int.Parse(_options.IniReadValue("data", "sleep").Trim());
            }
            catch (Exception)
            {
                _options.IniWriteValue("data", "sleep", "1");
            }

            Http.NavigateBegin("Http://whirlvid.com");
            var html = Http.NavigateEnd().Html;
            if (html.IndexOf(">My Profile<", StringComparison.Ordinal) < 0)
                Login(_login, _pass);
            else Msg.Info("Уже авторизован, пропускаю вход");

            while (true)
            {
                var v = LastVideo();
                var index = uint.Parse(_options.IniReadValue("data", "id").Trim());

                for (var i = index; i < v; i++)
                {
                    VideoLoad(i);
                    _options.IniWriteValue("data", "id", i.ToString());
                    Thread.Sleep(sleep);
                }

                Msg.Info("Все видео просмотрены, жду 10 минут");
                Thread.Sleep(10*60);
            }
        }

        private static Ini _options;
        public static HttpMaster Http;
        private static Random _rand;

        private static uint LastVideo()
        {
            Http.NavigateBegin("Http://whirlvid.com/login");
            var html = Http.NavigateEnd().Html;
            html = html.ReturnAfter("Recent Dailymotion");
            html = html.MiddleReturn("<a href=\"", "\"");
            Http.NavigateBegin($"http://whirlvid.com{html}");
            html = Http.NavigateEnd().Html;
            html = html.ReturnAfter("<!doctype html>").MiddleReturn("id=", "&");
            return uint.Parse(html);
        }

        private static void VideoLoad(uint id)
        {
            var html = "";
            try
            {
                html =
                    Http.Navigate(
                        $"http://whirlvid.com/index.php?option=com_contushdvideoshare&view=player&tmpl=component&id={id}&rate={_rand.Next(1, 5)}&nocache = {_rand.NextDouble()}")
                        .Html;
                html = html.ReturnAfter("\r\n\r\n");
                var i = uint.Parse(html);
                Msg.Info($"Видео номер {id} оценено");
            }
            catch (FormatException)
            {
                Msg.Info($"Видео номер {id} ошибка оценки, текст ошибки - {html}");
            }
        }

        private static void Login(string login, string pass)
        {
            Msg.Info("Вхожу");
            Http.NavigateBegin("Http://whirlvid.com/login");
            var html = Http.NavigateEnd().Html;

            Http.NavigateBegin("Http://whirlvid.com/login");
            Http.AddData("task", "user.login", HttpMaster.RquestType.Get);
            html = html.MiddleReturn("<form action=\"", "</form>");
            int i;
            while ((i = html.IndexOf("<input", StringComparison.Ordinal)) > 0)
            {
                html = html.Substring(i + 5);
                string name = html.MiddleReturn("name=\"", "\"");
                string value = name == "username"
                    ? login
                    : name == "password" ? pass : html.MiddleReturn("value=\"", "\"");
                Http.AddData(name, value, HttpMaster.RquestType.Post);
            }
            html = Http.NavigateEnd().Html;

            if (html.IndexOf(">My Profile<", StringComparison.Ordinal) <= 0)
            {
                if (html.IndexOf(">Warning<", StringComparison.Ordinal) > 0)
                {
                    var msg = html.MiddleReturn("alert-message\">", "</div>").Trim();
                    msg = msg == "Username and password do not match or you do not have an account yet."
                        ? "Логин или пароль введены неверно"
                        : msg;
                    throw new Exception(msg);
                }
                Msg.Info("Не получилось войти");
                Msg.DebugMesseg(html);
            }
            else
                Msg.Info("Вошел");
        }

    }
}