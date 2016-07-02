using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Diagnostics;

class TCPconnnctionClient
{

    private ASCIIEncoding asen = new ASCIIEncoding();
    private TcpClient tcpclnt = new TcpClient();
    private Stream stm;


    public void createConnection()
    {
        try
        {

            Console.WriteLine("Connecting.....");

            tcpclnt.Connect("192.168.1.4", 9999);

            stm = tcpclnt.GetStream();
            // use the ipaddress as in the server program

            Console.WriteLine("Connected");

        }
        catch (Exception e)
        {
            Console.WriteLine("Error..... in Connection" + e.StackTrace);
            Console.ReadLine();
        }

    }


    public void sendData(String data)
    {
        try
        {

            byte[] ba;
            ba = asen.GetBytes(data);
            stm.Write(ba, 0, ba.Length);
            stm.Flush();
            // System.Threading.Thread.Sleep(100);
        }
        catch (Exception e)
        {
            Console.WriteLine("Error.....in send data" + e.StackTrace);
            Console.ReadLine();
        }

    }

    public void sendDataByte(byte[] data)
    {
        try
        {


            stm.Write(data, 0, data.Length);
            //  System.Threading.Thread.Sleep(100);
        }
        catch (Exception e)
        {
            Console.WriteLine("Error.....in send data" + e.StackTrace);
            Console.ReadLine();
        }

    }

    public void TCPConnectionClose()
    {
        Trace.WriteLine("closing connection");
        stm.Dispose();
        tcpclnt.Close();
    }


}