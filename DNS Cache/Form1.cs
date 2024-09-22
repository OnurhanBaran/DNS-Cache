using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;  // Process sınıfını kullanmak için
using System.Drawing;
using System.Linq;
using System.Net;
using System.Net.Sockets;  // Socket sınıfını kullanmak için
using System.ServiceProcess;  // ServiceController sınıfını kullanmak için
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace neetpro146
{
    public partial class Form1 : Form
    {
        string izlenecek = "Dnscache"; // DNS İstemcisi
        int sirano = 0;
        bool durum;
        ServiceController service2;
        string bilgisayarAdi;
        string ipAdresi;
        Dictionary<string, DateTime> processStartTimes = new Dictionary<string, DateTime>();

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            int sn = 0, aktif = 0, pasif = 0;
            int i;

            // Bilgisayarın temel bilgileri listelenmektedir
            bilgisayarAdi = Dns.GetHostName();
            label4.Text = bilgisayarAdi;
            ipAdresi = Dns.GetHostByName(bilgisayarAdi).AddressList[0].ToString();
            label3.Text = ipAdresi;

            // Grid olarak service hizmetleri listelenmektedir
            dataGridView1.ColumnCount = 5;
            dataGridView1.Columns[0].Name = "No";
            dataGridView1.Columns[0].Width = 20;
            dataGridView1.Columns[1].Name = "Adı";
            dataGridView1.Columns[2].Name = "Ekran";
            dataGridView1.Columns[3].Name = "Tipi";
            dataGridView1.Columns[4].Name = "Aktifmi";

            foreach (ServiceController service in ServiceController.GetServices())
            {
                string serviceName = service.ServiceName;
                string serviceDisplayName = service.DisplayName;
                string serviceType = service.ServiceType.ToString();
                string status = service.Status.ToString();
                if (status.Substring(0, 1) == "R") { aktif++; } else { pasif++; }
                if (serviceName == izlenecek)
                {
                    sirano = sn;
                    service2 = service;
                    durum = status.Substring(0, 1) == "R";
                }
                dataGridView1.Rows.Add(sn++, serviceName, serviceDisplayName, serviceType, status);
            }

            // Arka planda çalışan programların listesi
            dataGridView2.ColumnCount = 2;
            dataGridView2.Columns[0].Name = "No";
            dataGridView2.Columns[0].Width = 20;
            dataGridView2.Columns[1].Name = "Program";
            Process[] p = Process.GetProcesses();
            for (i = 0; i < p.Length; i++)
            {
                dataGridView2.Rows.Add(i, p[i].ProcessName.ToString());
                // processStartTimes[p[i].ProcessName] = p[i].StartTime; // Bu satırı kaldırdık
            }

            // Ekran çıktısı olarak listBox1 kullanılmaktadır
            listBox1.Items.Clear();
            listBox1.Items.Add("Program aktif oldu>>> " + DateTime.Now);
            listBox1.Items.Add(sn + " adet servisten, aktif servis sayısı: " + aktif);
            listBox1.Items.Add("Pasif servis sayısı: " + pasif);
            listBox1.Items.Add("İzlenecek servis adı (sıra no:" + sirano + "): " + izlenecek);
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            timer1.Enabled = checkBox1.Checked;
            listBox1.Items.Add("Gerçek zamanlı kontrol: " + checkBox1.Checked);
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            // Zaman bazlı kontrol.... Gerçek zamanlı servis durumunda değişiklik olması durumunda devreye girer
            if (durum != (service2.Status.ToString().Substring(0, 1) == "R"))
            {
                listBox1.Items.Add("İzlenecek servis durum değiştirmiştir!!");
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            listBox2.Items.Clear();

            // Şu an çalışan işlemlerin listesini al
            Process[] currentProcesses = Process.GetProcesses();

            // Şu an çalışan işlemleri listBox2'ye ekle
            for (int i = 0; i < currentProcesses.Length; i++)
            {
                listBox2.Items.Add("Var olan>>" + currentProcesses[i].ProcessName.ToString());
            }

            listBox2.Items.Add("Farklı olanlar>>");

            // dataGridView2'den işlemler listesini al
            List<string> previousProcesses = new List<string>();
            foreach (DataGridViewRow row in dataGridView2.Rows)
            {
                if (row.Cells[1].Value != null)
                {
                    previousProcesses.Add(row.Cells[1].Value.ToString());
                }
            }

            // Şu an çalışan ve dataGridView2'de olmayan işlemleri bul ve listBox2'ye ekle
            foreach (Process proc in currentProcesses)
            {
                if (!previousProcesses.Contains(proc.ProcessName))
                {
                    listBox2.Items.Add(proc.ProcessName);
                }
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            listBox2.Items.Clear();
            try
            {
                using (Socket listener = new Socket(AddressFamily.InterNetwork, SocketType.Raw, ProtocolType.IP))
                {
                    listener.Bind(new IPEndPoint(IPAddress.Parse(ipAdresi), 0));
                    byte[] inValue = new byte[4] { 1, 0, 0, 0 };
                    byte[] outValue = new byte[4] { 1, 0, 0, 0 };
                    listener.IOControl(IOControlCode.ReceiveAll, inValue, outValue);
                    byte[] buffer = new byte[1000000];
                    int read = listener.Receive(buffer);
                    if (read >= 20)
                    {
                        listBox2.Items.Add("Okunan paket bilgisi sayısı: " + read);
                        string bilgiler = "";
                        for (int i = 0; i < read; i++)
                        {
                            bilgiler = bilgiler + "(" + buffer[i] + ")" + ",";
                        }
                        listBox2.Items.Add("Elde edilen paket bilgisi: " + bilgiler);
                        listBox2.Items.Add(Encoding.ASCII.GetString(buffer, 0, read));
                    }
                }
            }
            catch (Exception ex)
            {
                listBox2.Items.Add("Hata: " + ex.Message);
            }
        }
    }
}
