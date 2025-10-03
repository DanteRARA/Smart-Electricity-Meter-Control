// See https://aka.ms/new-console-template for more information
using System;
using System.Diagnostics;
using System.IO.Ports;
using Microsoft.Extensions.Configuration;
using Log_Utility;
using System.Threading.Tasks;

//讀取： 
//電壓： 00 03 00 00 00 01 crc 返回：01 03 02 09 1F FF DC xxx.x V 
//電流： 00 03 00 01 00 01 crc xxx.x A 
//頻率： 00 03 00 02 00 01 crc xx.xx Hz 
//有效功率： 00 03 00 03 00 01 crc xx.xx kW 
//視在功率： 00 03 00 05 00 01 crc xx.xx kVA 
//功率因數： 00 03 00 06 00 01 crc x.xxx 
//有效電能： 00 03 00 07 00 02 crc xxxxxx.xx kWh 
//序號： 00 03 00 27 00 03 crc 
//串列傳輸速率： 00 03 00 2a 00 01 crc 
//硬體版本號： 00 03 00 2e 00 02 crc 
//軟體版本號： 00 03 00 30 00 02 crc 
//ID: 00 03 00 2b 00 01 crc 
//形式: 00 03 00 3a 00 01 crc 
//規格:  00 03 00 3b 00 01 crc


public class RS485_RW
{
    //public string port_num;
    //public string BaudRate;

    static string port = "";
    static int baud_rate = 0;
    static SerialPort sp;
    //public event Action<byte[]> OnResponseReceived;
    private TaskCompletionSource<byte[]> _tcsResponse;

    // function init
    public RS485_RW()
    {
        //string port = "";
        //int baud_rate = 0;
        string project_path = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, @"../../../"));
        var config = new ConfigurationBuilder()
            .SetBasePath(project_path)
            .AddJsonFile("appsettings.json")
            .Build();

        // read setting 
        port = config["SerialConfig:Port"];
        baud_rate = int.Parse(config["SerialConfig:BaudRate"]);

        Console.WriteLine($"Port: {port}");
        Console.WriteLine($"Baud Rate: {baud_rate}");
        Logger.Info($"Port: {port}");
        Logger.Info($"Baud Rate: {baud_rate}");

        sp = new SerialPort(port, baud_rate, Parity.None, 8, StopBits.One);
        sp.DataReceived += DataReceivedHandler;

        try
        {
            sp.Open();
            Logger.Info($"{port} port RS485 is opened.");
        }catch(Exception ex)
        {
            //Console.WriteLine(ex.ToString());
            Logger.Error($"{port} port is not opened!\n {ex.Message}");
        }

    }

    private async void DataReceivedHandler(object sender, SerialDataReceivedEventArgs e)
    {
        SerialPort sp = (SerialPort)sender;
        //System.Threading.Thread.Sleep(100);
        await Task.Delay(50);

        int bytes = sp.BytesToRead;
        byte[] buffer = new byte[bytes];
        sp.Read(buffer, 0, bytes);

        Logger.Info($"Received: {BitConverter.ToString(buffer)}");

        _tcsResponse?.TrySetResult(buffer);
    }

    public async Task<byte[]> SendAndWaitAsync(string hexCmd, int timeoutMs = 3000)
    {
        byte[] request = BuildRequest(hexCmd);
        _tcsResponse = new TaskCompletionSource<byte[]>();
        RS485_Input(request);

        var task = await Task.WhenAny(_tcsResponse.Task, Task.Delay(timeoutMs));
        if(task == _tcsResponse.Task)
        {
            return await _tcsResponse.Task;
        }
        else
        {
            throw new TimeoutException("Timeout waiting for Modbus response");
        }

    }

    public void RS485_Input(byte[] data)
    {
        if (!sp.IsOpen)
        {
            Logger.Error($"{port} port is not open!");
            return;
        }
        //byte[] data = HexStringToBytes(hex);

        sp.Write(data, 0, data.Length);
        Logger.Info($"Send: {BitConverter.ToString(data)}");
        //Console.WriteLine($"Send: {BitConverter.ToString(data)}");
    }

    private byte[] HexStringToBytes(string hex)
    {
        hex = hex.Replace(" ", "");
        int len = hex.Length / 2;
        byte[] rst = new byte[len];
        for (int i = 0; i < len; i++)
        {
            rst[i] = Convert.ToByte(hex.Substring(i * 2, 2), 16);
        }
        return rst;
    }

    public ushort CRC16_Modbus(byte[] data, int length)
    {
        ushort crc = 0xFFFF;    // 2 bytes
        for (int i = 0; i < length; i++)    // 2 number to 1 byte
        {
            crc ^= data[i];
            for(int j = 0; j < 8; j++)
            {
                if((crc & 0x0001) != 0)
                {
                    crc = (ushort)((crc >> 1) ^ 0xA001); // LSB 0xA001 == MSB 0x8005
                }
                else
                {
                    crc >>= 1;
                }
            }
        }
        return crc;
    }

    public byte[] BuildRequest(string hex)
    {
        byte[] frame = HexStringToBytes(hex);
        ushort crc = CRC16_Modbus(frame, frame.Length);
        Array.Resize(ref frame, frame.Length + 2);
        frame[frame.Length - 2] = (byte)(crc & 0xFF);
        frame[frame.Length - 1] = (byte)(crc >> 8);

        return frame;
    }

    public float ParseModbusFloat(byte[] response, int startIdx)
    {
        byte[] data = new byte[4];
        data[0] = response[startIdx + 0];
        data[1] = response[startIdx + 1];
        data[2] = response[startIdx + 2];
        data[3] = response[startIdx + 3];

        Array.Reverse(data);

        return BitConverter.ToSingle(data, 0);
    }
    public float ParseFloat(byte[] response, int startIdx) {
        byte[] data = new byte[4];
        Array.Copy(response, startIdx, data, 0, 4);
        Array.Reverse(data); // Big-endian → Little-endian
        return BitConverter.ToSingle(data, 0);
    }
    public ushort ParseUint16(byte[] response, int startIdx) {
        return (ushort)(response[startIdx] << 8 | response[startIdx + 1]);
    }

    public string ParseAsciiString(byte[] response, int startIdx, int length)
    {
        byte[] strBytes = new byte[length];
        Array.Copy(response, startIdx, strBytes, 0, length);
        return System.Text.Encoding.ASCII.GetString(strBytes).TrimEnd('\0');
    }

}




//Console.WriteLine("Hello, World!");
