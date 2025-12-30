using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace Send_Receive_over_Ethernet
{
    public class Send_Receive
    {
        private string Respond;
        private IPAddress Address;

        

        public string SendandGetStringoverEthernet(string IP4_address, string SendBinary, Int32 Port)
        {
            
            
            //Die übergebene IP Adresse von String in IPAddress parsen
            try
            {
                Address = IPAddress.Parse(IP4_address);
            }
            catch
            {
                return ("ERROR: Invalid IP Address");
            }



            try
            {

                //Erzeugen eines TCP Clients:
                TcpClient SGM = new TcpClient(IP4_address, Port);

                //Die zu sendende Zeichenfolge in ASCII umwalndeln und als Byte Array speichern
                Byte[] data = System.Text.Encoding.ASCII.GetBytes(SendBinary);

                //Erzeugen eines Streams 
                NetworkStream stream = SGM.GetStream();

                //Und abschicken
                stream.Write(data, 0, data.Length);

                System.Threading.Thread.Sleep(10000); //Sollte Ausführung um 1 sek. stoppen um Antwort zu verpassen


                data = new Byte[1024];
                Int32 bytes = stream.Read(data, 0, data.Length);
                Respond = System.Text.Encoding.ASCII.GetString(data, 0, bytes);

                //Verbindung abbauen
                stream.Close();
                SGM.Close();
            }

            catch (ArgumentNullException)
            {
                return "ERROR: Wrong Argument";
            }
            catch (SocketException)
            {
                return "ERROR: Error Socket";
            }

            return Respond;
        }



    }
}
