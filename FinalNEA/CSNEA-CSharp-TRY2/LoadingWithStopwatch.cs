using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Diagnostics;

namespace CSNEA_CSharp_TRY2
{
    public partial class LoadingWithStopwatch : Form
    {
        readonly FormControls controls = new FormControls();
        public LoadingWithStopwatch()
        {
            InitializeComponent();
        }

        Label timeLabel = new Label();

        Stopwatch stopWatch = new Stopwatch();
        Timer timer = new Timer();
        private void LoadingWithStopwatch_Load(object sender, EventArgs e)
        {
            timer.Start();
            stopWatch.Start();

            LoadMenu();

            timer.Tick += Timer_Tick;
        }

        private void LoadMenu()
        {
            timeLabel = controls.CreateLabel(Controls, "timeLabel", "", 500, 500, 0, 0);
            timeLabel.Font = new Font("Microsoft Sans Serif", 30);
            timeLabel.TextAlign = ContentAlignment.MiddleCenter;
            timeLabel.Dock = DockStyle.Fill;

            Label loadingInfo = controls.CreateLabel(Controls, "loadingInfo", Environment.NewLine + "Loading SQL", 100, 500, 0, 50);
            loadingInfo.Font = new Font("Microsoft Sans Serif", 30);
            loadingInfo.TextAlign = ContentAlignment.TopCenter;
            loadingInfo.Dock = DockStyle.Top;
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            TimeSpan elapsed = stopWatch.Elapsed;
            timeLabel.Text = string.Format("{0:00}:{1:00}:{2:00}:{3:00}", Math.Floor(elapsed.TotalHours), elapsed.Minutes, elapsed.Seconds, elapsed.Milliseconds);
        }

        public void StopTimer()
        {
            stopWatch.Stop();
        }
    }
}
