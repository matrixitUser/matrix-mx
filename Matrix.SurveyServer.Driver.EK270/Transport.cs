using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace Matrix.SurveyServer.Driver.EK270
{
    public partial class Driver
    {
        //    private byte[] Send(byte[] data)
        //    {
        //        request(data);
        //        response();
        //        //log(string.Format("> {0}", string.Join(",", data.Select(b => b.ToString("X2")))));
        //        //log("--> " + Encoding.GetEncoding(1252).GetString(data));
        //        var buffer = new List<byte>();
        //        var timeout = 10000;
        //        var sleep = 250;
        //        var isCollecting = false;
        //        var waitCollected = 0;
        //        var isCollected = false;
        //        while ((timeout -= sleep) > 0 && !isCollected)
        //        {
        //            Thread.Sleep(sleep);

        //            var buf = response();
        //            if (buf.Any())
        //            {
        //                isCollecting = true;
        //                buffer.AddRange(buf);
        //                //log(string.Format("сбор +{0}=>{1} при wc={2}", buf.Count(), buffer.Count(), waitCollected));
        //                isCollected = true;
        //                waitCollected = 0;
        //            }
        //            else
        //            {
        //                if (isCollecting)
        //                {
        //                    //isCollected = true;
        //                    waitCollected++;
        //                    if (waitCollected == 9)
        //                    {
        //                        isCollected = true;
        //                        //log(string.Format("конец сбора {0}", buffer.Count()));
        //                    }
        //                    else
        //                    {
        //                        ////log("ждем порцию");
        //                    }
        //                }
        //                else
        //                {
        //                    ////log("ждем начала");
        //                }
        //            }
        //        }
        //        //log(string.Format("< {0}", string.Join(",", buffer.Select(b => b.ToString("X2")))));
        //        //log("<-- " + Encoding.GetEncoding(1252).GetString(buffer.ToArray()));
        //        return buffer.ToArray();
        //    }

        //private byte[] SendSimple(byte[] data, bool braceCount, bool hexMode = false)
        //{
        //    var buffer = new List<byte>();
        //    var str = "";

        //    if (debugMode)
        //    {
        //        for (var i = 0; i < data.Length; i += 200)
        //        {
        //            var part = data.Skip(i).Take(200).ToArray();
        //            log(string.Format(">({1},{2}) \"{0}\"", Encoding.GetEncoding(1252).GetString(part), i, part.Length));
        //        }
        //    }

        //    response();
        //    request(data);

        //    var timeout = TIMEOUT_TIME;

        //    var isCollecting = false;
        //    var waitCollected = 0;
        //    var isCollected = false;
        //    while ((timeout -= SLEEP_TIME) > 0 && !isCollected)
        //    {
        //        Thread.Sleep(SLEEP_TIME);

        //        var buf = response();
        //        if (buf.Any())
        //        {
        //            isCollecting = true;
        //            buffer.AddRange(buf);
        //            waitCollected = 0;
        //            str += Encoding.ASCII.GetString(buf);
        //        }
        //        else
        //        {
        //            if (isCollecting)
        //            {
        //                waitCollected++;
        //                if (waitCollected == COLLECT_MUL)
        //                {
        //                    isCollected = !braceCount || (!string.IsNullOrEmpty(str) && (str.Count(c => c == '(') == str.Count(c => c == ')')));
        //                }
        //            }
        //        }
        //    }

        //    if (debugMode)
        //    {
        //        if(hexMode)
        //        {
        //            for (var i = 0; i < buffer.Count(); i += 200)
        //            {
        //                var part = buffer.Skip(i).Take(200).ToArray();
        //                log(string.Format("<({1},{2}) \"{0}\"", string.Join(",", part.Select(b => b.ToString("X2"))), i, part.Length));
        //            }
        //        }
        //        else
        //        {
        //            for (var i = 0; i < buffer.Count(); i += 200)
        //            {
        //                var part = buffer.Skip(i).Take(200).ToArray();
        //                log(string.Format("<({1},{2}) \"{0}\"", Encoding.GetEncoding(1252).GetString(part), i, part.Length));
        //            }
        //        }
        //    }

        //    return buffer.ToArray();
        //}

        private int mid = 0;

        private byte[] Send(byte[] data)
        {
            request(data);

            for (var i = 0; i < data.Length; i += 200)
            {
                var part = data.Skip(i).Take(200).ToArray();
                log(string.Format("{3:X}>({1},{2}) \"{0}\"", Encoding.GetEncoding(1252).GetString(part), i, part.Length, mid), level: 3);
            }

            var timeout = 10000;
            var sleep = 150;
            List<byte> range = new List<byte>();
            while ((timeout -= sleep) > 0)
            {
                Thread.Sleep(sleep);
                var buffer = response();

                if (buffer.Any())
                {
                    do
                    {
                        range.AddRange(buffer);
                        Thread.Sleep(200);
                        buffer = response();
                    } while (buffer.Any());
                    break;
                }
            }

            if (range.Any())
            {
                for (var i = 0; i < range.Count(); i += 200)
                {
                    var part = range.Skip(i).Take(200).ToArray();
                    log(string.Format("{3:X}<({1},{2}) \"{0}\"", Encoding.GetEncoding(1252).GetString(part), i, part.Length, mid), level: 3);
                }
            }
            mid++;

            return range.ToArray();
            //return SendSimple(data, false);
        }

        private byte[] SendBraces(byte[] data)
        {
            request(data);
            for (var i = 0; i < data.Length; i += 200)
            {
                var part = data.Skip(i).Take(200).ToArray();
                log(string.Format("{3:X}>({1},{2}) \"{0}\"", Encoding.GetEncoding(1252).GetString(part), i, part.Length, mid), level: 3);
            }

            var timeout = TIMEOUT_TIME;
            var sleep = SLEEP_TIME;
            List<byte> range = new List<byte>();
            var str = "";

            var isCollecting = false;
            var waitCollected = 0;
            var isCollected = false;

            //если не пришло ни байта - ждём 10 секунд
            //если хоть 1 байт пришёл - дособираем (
            while (((timeout -= sleep) > 0) && (!isCollected || string.IsNullOrEmpty(str) || (str.Count(c => c == '(') != str.Count(c => c == ')'))))//(timeout -= sleep) > 0 || (string.IsNullOrEmpty(str) || str.Count(c => c == '(') != str.Count(c => c == ')')) || !isCollected)
            {
                Thread.Sleep(sleep);
                //var buffer = response();

                //if (buffer.Any())
                //{
                //    str += Encoding.ASCII.GetString(buffer);
                //    range.AddRange(buffer);
                //}

                var buffer = response();
                if (buffer.Any())
                {
                    timeout = TIMEOUT_TIME;
                    isCollecting = true;
                    waitCollected = 0;
                    str += Encoding.ASCII.GetString(buffer);
                    range.AddRange(buffer);
                }
                else
                {
                    if (isCollecting)
                    {
                        waitCollected++;
                        if (waitCollected == COLLECT_MUL)
                        {
                            isCollected = true;
                        }
                    }
                }
            }


            if (range.Any())
            {
                for (var i = 0; i < range.Count(); i += 200)
                {
                    var part = range.Skip(i).Take(200).ToArray();
                    log(string.Format("{3:X}<({1},{2}) \"{0}\"", Encoding.GetEncoding(1252).GetString(part), i, part.Length, mid), level: 3);
                }
            }

            mid++;
            return range.ToArray();
            //return SendSimple(data, true);
        }

        private void SendInstant(byte[] data, bool hexMode = false, int timeout = 7000)
        {
            request(data);
            //if (debugMode)
            //{
            //    log(string.Format("> {0}", hexMode ? string.Join(",", data.Select(b => b.ToString("X2"))) : ("\"" + Encoding.GetEncoding(1252).GetString(data)) + "\""));
            //}
            byte[] buffer = new byte[] { };
            while (!buffer.Any() && timeout > 0)
            {
                timeout -= 100;
                Thread.Sleep(100);
                buffer = response();
            }
            //if (buffer.Any() && debugMode)
            //{
            //    log(string.Format("< {0}", hexMode ? string.Join(",", data.Select(b => b.ToString("X2"))) : ("\"" + Encoding.GetEncoding(1252).GetString(data)) + "\""));
            //}
        }
    }
}
