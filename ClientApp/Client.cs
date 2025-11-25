using System;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using Common;

namespace ClientApp
{
    public class ChatClient
    {
        private TcpClient? _client;
        private NetworkStream? _stream;
        private readonly string _ip;
        private readonly int _port;

        private readonly UserSession _session;
        private StreamReader? _reader;

        public ChatClient(string ip, int port)
        {
            _ip = ip;
            _port = port;
            _session = new UserSession();
        }

        public void Start()
        {
            try
            {
                _client = new TcpClient();
                _client.Connect(_ip, _port);

                _stream = _client.GetStream();
                _reader = new StreamReader(_stream, Encoding.UTF8);


                Console.WriteLine($"Kết nối đến server {_ip}:{_port} thành công!");

                // Luồng nhận tin nhắn song song
                Thread receiveThread = new Thread(ReceiveLoop) { IsBackground = true };
                receiveThread.Start();

                Console.WriteLine("Server yêu cầu bạn REGISTER hoặc LOGIN...\n");

                AuthLoop();     // Giai đoạn đăng ký / đăng nhập
                ChatLoop();     // Giai đoạn chat
            }
            catch (Exception ex)
            {
                Console.WriteLine("Lỗi kết nối: " + ex.Message);
            }
        }

        private void AuthLoop()
        {
            while (!_session.IsLoggedIn)
            {
                ConsoleUI.ShowAuthMenu();
                string choice = Console.ReadLine();

                if (choice == "1") Register();
                else if (choice == "2") Login();
                else if (choice == "0") Environment.Exit(0);
                else Console.WriteLine("Sai lựa chọn.");
            }
        }

        private void ChatLoop()
        {
            ConsoleUI.ShowChatCommands();

            while (true)
            {
                string? msg = Console.ReadLine();
                if (msg == "exit")
                {
                    Send("exit");
                    break;
                }
                Send(msg);
            }
        }

        private void Register()
        {
            Console.Write("Username: ");
            string? u = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(u))
            {
                Console.WriteLine("Tên không hợp lệ.");
                return;
            }


            Console.Write("Password: ");
            string p = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(p))
            {
                Console.WriteLine("Mât khẩu không hợp lệ.");
                return;
            }

            Send(Protocol.BuildRegister(u, p));
            Thread.Sleep(1000);
        }

        private void Login()
        {
            Console.Write("Username: ");
            string? u = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(u))
            {
                Console.WriteLine("Tên không hợp lệ.");
                return;
            }

            Console.Write("Password: ");
            string p = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(p))
            {
                Console.WriteLine("Mât khẩu không hợp lệ.");
                return;
            }

            Send(Protocol.BuildLogin(u, p));
            Thread.Sleep(1000);
        }

        private void ReceiveLoop()
        {
            while (true)
            {
                string? msg = _reader.ReadLine();
                if (msg == null) return;

                // ===== XỬ LÝ AUTH =====
                if (msg.StartsWith(Protocol.LOGIN_OK))
                {
                    string username = msg.Split('|')[1];
                    Console.WriteLine($"Đăng nhập thành công! Xin chào {username}");
                    _session.IsLoggedIn = true;
                    continue;
                }

                if (msg.StartsWith(Protocol.LOGIN_FAIL))
                {
                    Console.WriteLine("Đăng nhập thất bại: " + msg.Split('|')[1]);
                    continue;
                }

                if (msg.StartsWith(Protocol.REGISTER_OK))
                {
                    Console.WriteLine("Đăng ký thành công! Hãy đăng nhập.");
                    continue;
                }

                if (msg.StartsWith(Protocol.REGISTER_FAIL))
                {
                    Console.WriteLine("Đăng ký thất bại: " + msg.Split('|')[1]);
                    continue;
                }

                // ===== TIN NHẮN CHAT =====
                Console.WriteLine(msg);
            }
        }

        private void Send(string msg)
        {
            byte[] data = Encoding.UTF8.GetBytes(msg + "\n");
            _stream.Write(data, 0, data.Length);
        }
    }
}
