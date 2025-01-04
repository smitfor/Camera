using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using DirectShowLib;
using System.Runtime.CompilerServices;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Threading;

namespace Camera
{//taskkill /F /IM ffmpeg.exe для закрытия через cmd процессов 
 //tasklist | findstr ffmpeg для просмотра того,на что распостранён ffmpage 

    public partial class Form1 : Form
    {
        public string pathFFmpeg = @"C:\Install\peg\ffmpeg.exe"; 
        public string Device = string.Empty;
        public DemoRecord record;

        public Form1()
        {
            InitializeComponent();
            int count = 0;
            var videoDevices = new List<DsDevice>(DsDevice.GetDevicesOfCat(FilterCategory.VideoInputDevice));
            foreach (var i in videoDevices)
            {
                listBox1.Items.Add($"{count + 1}: {i.Name}");
                count++;
            }
            listBox1.SelectedIndex = listBox1.Items.Count - 1;
            listBox1.SelectedIndex = -1;
        }
        private void listBox1_DoubleClick(object sender, EventArgs e)
        {
            Device = listBox1.SelectedItem.ToString().Split(':')[1].Trim();
            MessageBox.Show($"Используется {Device.Trim().ToString()}");
        }
        public class DemoRecord
        {
            public Process ffmpegProcess;
            private CancellationTokenSource cancellationTokenSource;
            public DemoRecord() { }
            public async Task StartRecordAsync(string device, string pathFfmpeg, PictureBox pictureBox)
            {
                if (string.IsNullOrEmpty(device))
                {
                    MessageBox.Show("Выберите устройство!");
                    return;
                }

                try
                {
                    ffmpegProcess = new Process();
                    cancellationTokenSource = new CancellationTokenSource();
                    ffmpegProcess.StartInfo.FileName = pathFfmpeg;
                    string outputFile = $"C:\\Users\\user\\Videos\\video_{DateTime.Now:yyyyMMdd_HHmmss}.avi";
                    ffmpegProcess.StartInfo.Arguments = $"-f dshow -i video=\"{device}\" -vcodec libx264 -preset ultrafast -t 00:10:00 \"{outputFile}\" -vf fps=100 -f image2pipe -vcodec png pipe:1";

                    ffmpegProcess.StartInfo.UseShellExecute = false;
                    ffmpegProcess.StartInfo.RedirectStandardOutput = true;
                    ffmpegProcess.StartInfo.RedirectStandardError = true;
                    ffmpegProcess.StartInfo.CreateNoWindow = true;

                    ffmpegProcess.ErrorDataReceived += (sender, e) =>
                    {
                        if (e.Data != null)
                        {
                            Console.WriteLine($"stderr: {e.Data}");
                        }
                    };

                    ffmpegProcess.Start();

                    Stream outputStream = ffmpegProcess.StandardOutput.BaseStream;

                    // Чтение данных изображения в отдельном потоке.
                    await Task.WhenAny(
                        ProcessFramesAsync(outputStream, pictureBox, cancellationTokenSource.Token),
                        Task.Run(() => ffmpegProcess.WaitForExit())
                    );
                }
                catch (Exception error)
                {
                    MessageBox.Show($"Ошибка: {error.Message}");
                }
            
        }
            public void StopRecord()
            {
                if (ffmpegProcess != null && !ffmpegProcess.HasExited)
                {
                    ffmpegProcess.Kill();
                    ffmpegProcess = null;
                    MessageBox.Show("Конец записи!");
                }
                else
                {
                    MessageBox.Show("Запись не запущена!");
                }
            }
            private async Task ProcessFramesAsync(Stream stream, PictureBox pictureBox, CancellationToken cancellationToken)
            {
                byte[] buffer = new byte[1024 * 1024]; 
                int bytesRead = 0;

                try
                {
                    while (!cancellationToken.IsCancellationRequested)
                    {
                        bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length, cancellationToken);
                        if (bytesRead <= 0)
                            break;

                        
                        using (MemoryStream ms = new MemoryStream(buffer, 0, bytesRead))
                        {
                            pictureBox.Invoke((MethodInvoker)(() =>
                            {
                                pictureBox.Image = new Bitmap(ms);
                            }));
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Ошибка обработки кадров: {ex.Message}");
                }
            }
        }

            private async void button1_Click(object sender, EventArgs e)
        {
            record = new DemoRecord();
            await record.StartRecordAsync(Device, pathFFmpeg, pictureBox1);
        }

        private void button2_Click(object sender, EventArgs e)
        {
            if (record != null)
            {
                record.StopRecord();
            }
            else
            {
                MessageBox.Show("Запись не была начата!");
            }
        }
    }
}
