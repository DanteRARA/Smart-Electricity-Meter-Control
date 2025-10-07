using Log_Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Smart_ele_meter
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            string hex = "01 03 00 14 00 02";
            var rs485_rw = new RS485_RW();

            byte[] respVolt = await rs485_rw.SendAndWaitAsync(hex);

            float volt = rs485_rw.ParseFloat(respVolt, 3);
            Logger.Info($"Voltage: {volt}");

            //Console.WriteLine("已發送查詢，等待回應...");
            //Console.ReadLine(); // 程式不結束，等事件觸發
        }

    }
}
