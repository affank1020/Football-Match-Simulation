using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CSNEA_CSharp_TRY2
{
    public partial class DisplayPlayerStats : Form
    {
        readonly FormControls controls = new FormControls();

        public PlayerStats player;
        public DisplayPlayerStats(PlayerStats playerStats)
        {
            InitializeComponent();
            player = playerStats;
        }

        private void DisplayPlayerStats_Load(object sender, EventArgs e)
        {
            controls.CreateLabel(Controls, "playerName", "(" + player.PlayerObject.FormationPosition + ") " + player.PlayerObject.Name, 30, 500, 10, 10);
            controls.CreateLabel(Controls, "playerGoals", "Goals: " + player.Goals, 30, 500, 10, 100);
            controls.CreateLabel(Controls, "playerAssists", "Assists: " + player.Assists, 30, 500, 10, 130);
            controls.CreateLabel(Controls, "playerShots", "Shots: " + player.Shots, 30, 500, 10, 160);
            controls.CreateLabel(Controls, "playerShotsOT", "Shots On Target: " + player.ShotsOnTarget, 30, 500, 10, 190);
            controls.CreateLabel(Controls, "playerPasses", "Passes: " + player.Passes, 30, 500, 10, 220);
            controls.CreateLabel(Controls, "playerChancesCreated", "Chances Created: " + player.ChancesCreated, 30, 500, 10, 250);
            controls.CreateLabel(Controls, "playerFouls", "Fouls: " + player.Fouls, 30, 500, 10, 280);
            controls.CreateLabel(Controls, "playerYellowCards", "Yellow Cards: " + player.YellowCards, 30, 500, 10, 310);
            controls.CreateLabel(Controls, "playerRedCards", "Red Cards: " + player.RedCards, 30, 500, 10, 340);

            foreach (Control item in Controls)
            {
                item.ForeColor = Color.White;
                item.Font = new Font("Microsoft Sans Serif", 15);
            }
        }
    }
}
