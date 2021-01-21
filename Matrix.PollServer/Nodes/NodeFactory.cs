using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Matrix.PollServer.Nodes
{
    static class NodeFactory
    {
        public static PollNode Create(dynamic raw)
        {
            switch ((string)raw.type)
            {
                case "Tube": return new Tube.TubeNode(raw);
                case "CsdConnection": return new Csd.CsdConnectionNode(raw);
                case "CsdPort": return new Csd.CsdPort(raw);
                case "Modem": return new Csd.PoolModem(raw);
                case "MatrixConnection": return new Matrix.MatrixConnectionNode(raw);
                case "SimpleMatrixConnection": return new Matrix.SimpleMatrixNode(raw);
                case "MatrixPort": return new Matrix.MatrixPortNode(raw);
                case "MatrixSwitch": return new Matrix.MatrixSwitchNode(raw);
                case "LanPort": return new Lan.LanPort(raw);
                case "LanConnection": return new Lan.LanConnection(raw);
                case "HttpPort": return new Http.HttpPort(raw);
                case "HttpConnection": return new Http.HttpConnection(raw);
                case "TeleofisWrxConnection": return new TeleofisWrx.TeleofisWrxConnectionNode(raw);
                case "TeleofisWrxPort": return new TeleofisWrx.TeleofisWrxPortNode(raw);
                case "MatrixTerminalConnection": return new MatrixTerminal.MatrixTerminalConnectionNode(raw);
                case "MatrixTerminalPort": return new MatrixTerminal.MatrixTerminalPortNode(raw);
                case "MilurConnection": return new Milur.MilurConnectionNode(raw);
                case "MilurPort": return new Milur.MilurPortNode(raw);

                // нефинальные / работа с данными
                case "ZigbeeConnection": return new Zigbee.ZigbeeConnection(raw);
                case "ZigbeePort": return new Zigbee.ZigbeePort(raw);   // ловит события "спящий зигби вышел на связь"
                case "MxModbus": return new MxModbus.MxModbus(raw);     // ловит события "обнаружен магнит"
                case "ZliteConnection": return new Zlite.ZliteConnection(raw);
                case "ZlitePort": return new Zlite.ZlitePort(raw);

                // финальные / работа с железом
                case "TcpClient": return new Tcp.TcpClient(raw);        // постоянно держит связь с сервером
                case "ComConnection": return new Com.ComConnection(raw);// постоянно держит открытым com-порт 
                case "ComPort": return new Com.ComPort(raw);      
                          
                default: return new TransparentNode(raw); 
            }
        }
    }
}
