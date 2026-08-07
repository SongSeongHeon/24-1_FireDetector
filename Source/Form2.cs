using System;
using System.Configuration;
using System.Windows.Forms;
using FireSharp.Config;
using FireSharp.Interfaces;
using FireSharp.Response;
using System.Timers;

namespace Project_Fire
{
    public partial class Form2 : Form
    {
        private IFirebaseClient client;
        private System.Timers.Timer updateTimer;
        private bool handleCreated = false;

        public Form2()
        {
            InitializeComponent();
            this.Load += new EventHandler(Form2_Load);
            this.HandleCreated += new EventHandler(Form2_HandleCreated);

            updateTimer = new System.Timers.Timer();
            updateTimer.Interval = 3000;
            updateTimer.Elapsed += UpdateTimer_Elapsed;
        }

        private void Form2_HandleCreated(object sender, EventArgs e)
        {
            handleCreated = true;
        }

        private void Form2_Load(object sender, EventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("Form2_Load 호출됨");

            client = new FireSharp.FirebaseClient(new FirebaseConfig
            {
                AuthSecret = ConfigurationManager.AppSettings["FirebaseAuthSecret"],
                BasePath = ConfigurationManager.AppSettings["FirebaseBasePath"]
            });

            if (client != null)
            {
                MessageBox.Show("Firebase 연결 성공!");
            }
            else
            {
                MessageBox.Show("Firebase 연결 실패!");
            }

            // 타이머 시작
            updateTimer.Start();
        }

        private async void UpdateTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            await FetchAndDisplayData();
        }

        private async System.Threading.Tasks.Task FetchAndDisplayData()
        {
            try
            {
                FirebaseResponse response1 = await client.GetAsync("arduino1");
                SensorData arduino1Data = response1.ResultAs<SensorData>();

                FirebaseResponse response2 = await client.GetAsync("arduino2");
                SensorData arduino2Data = response2.ResultAs<SensorData>();

                if (arduino1Data != null && arduino2Data != null)
                {
                    if (handleCreated)
                    {
                        // UI 컨트롤 업데이트를 Invoke를 사용하여 메인 스레드에서 실행
                        this.Invoke((MethodInvoker)delegate
                        {
                            double co2Value1 = double.Parse(arduino1Data.CO2);
                            double co2Value2 = double.Parse(arduino2Data.CO2);

                            // CO2 값이 800ppm 이상인 경우 빨간색, 아니면 초록색으로 픽쳐박스 색상 설정
                            P801.BackColor = co2Value1 >= 800 ? System.Drawing.Color.Red : System.Drawing.Color.Green;
                            P802.BackColor = co2Value2 >= 800 ? System.Drawing.Color.Red : System.Drawing.Color.Green;

                            labelArduino1.Text = $"801호\nCO2: {co2Value1:F1} ppm\nTemp: {arduino1Data.Temperature} ºC\nFlame: {(arduino1Data.Flame == "1" ? "화재 감지!!" : "화재 감지 없음")}";
                            labelArduino2.Text = $"802호\nCO2: {co2Value2:F1} ppm\nTemp: {arduino2Data.Temperature} ºC\nFlame: {(arduino2Data.Flame == "1" ? "화재 감지!!" : "화재 감지 없음")}";
                        });

                        if (double.TryParse(arduino1Data.CO2, out double co2_arduino1) &&
                            double.TryParse(arduino2Data.CO2, out double co2_arduino2) &&
                            double.TryParse(arduino1Data.Temperature, out double temp_arduino1) &&
                            double.TryParse(arduino2Data.Temperature, out double temp_arduino2) &&
                            double.TryParse(arduino1Data.Flame, out double flame_arduino1) &&
                            double.TryParse(arduino2Data.Flame, out double flame_arduino2))
                        {
                            double thresholdCO2 = 100.0; // CO2 절대 오차 범위
                            double thresholdTemp = 20.0; // Temperature 절대 오차 범위

                            bool co2Error = Math.Abs(co2_arduino1 - co2_arduino2) > thresholdCO2;
                            bool tempError = Math.Abs(temp_arduino1 - temp_arduino2) > thresholdTemp;

                            string errorMessage = "";

                            if (co2Error)
                                errorMessage += "CO2 센서 오류\n";
                            if (tempError)
                                errorMessage += "온도 센서 오류\n";

                            if (!string.IsNullOrEmpty(errorMessage))
                            {
                                this.Invoke((MethodInvoker)delegate
                                {
                                    MessageBox.Show(errorMessage);
                                });
                            }
                            else
                            {
                                // Arduino 1과 2에서 화재 감지 여부를 확인하는 조건식
                                double maxTemp = Math.Max(temp_arduino1, temp_arduino2);

                                bool fireDetected1 = (temp_arduino1 > maxTemp + 20) &&
                                                     (co2_arduino1 >= 800 || flame_arduino1 == 1);
                                bool fireDetected2 = (temp_arduino2 > maxTemp + 20) &&
                                                     (co2_arduino2 >= 800 || flame_arduino2 == 1);

                                if (fireDetected1)
                                {
                                    this.Invoke((MethodInvoker)delegate
                                    {
                                        textBoxFire1.Text = "화재가 발생했습니다!";
                                    });
            

                                }

                                if (fireDetected2)
                                {
                                    this.Invoke((MethodInvoker)delegate
                                    {
                                        textBoxFire2.Text = "화재가 발생했습니다!";
                                    });
                                }
                            }
                        }
                        else
                        {
                            this.Invoke((MethodInvoker)delegate
                            {
                                MessageBox.Show("데이터 형식 오류. 숫자를 확인해주세요.");
                            });
                        }
                    }
                }
                else
                {
                    this.Invoke((MethodInvoker)delegate
                    {
                        MessageBox.Show("데이터를 불러오는 데 실패했습니다.");
                    });
                }
            }
            catch (Exception ex)
            {
                this.Invoke((MethodInvoker)delegate
                {
                    MessageBox.Show($"오류 발생: {ex.Message}");
                });
            }
        }

        internal void DisplaySensorData(SensorData arduino1Data, SensorData arduino2Data)
        {
            if (handleCreated)
            {
                // Form1에서 데이터를 받아서 즉시 표시
                this.Invoke((MethodInvoker)delegate
                {
                    double co2Value1 = double.Parse(arduino1Data.CO2);
                    double co2Value2 = double.Parse(arduino2Data.CO2);

                    // CO2 값이 800ppm 이상인 경우 빨간색, 아니면 초록색으로 픽쳐박스 색상 설정
                    P801.BackColor = co2Value1 >= 800 ? System.Drawing.Color.Red : System.Drawing.Color.Green;
                    P802.BackColor = co2Value2 >= 800 ? System.Drawing.Color.Red : System.Drawing.Color.Green;

                    labelArduino1.Text = $"801호\nCO2: {co2Value1:F1} ppm\nTemp: {arduino1Data.Temperature} ºC\nFlame: {(arduino1Data.Flame == "1" ? "화재 감지!!" : "화재 감지 없음")}";
                    labelArduino2.Text = $"802호\nCO2: {co2Value2:F1} ppm\nTemp: {arduino2Data.Temperature} ºC\nFlame: {(arduino2Data.Flame == "1" ? "화재 감지!!" : "화재 감지 없음")}";
                });
            }
        }
    }

    public class SensorData
    {
        public string CO2 { get; set; }
        public string Temperature { get; set; }
        public string Flame { get; set; }
    }
}
