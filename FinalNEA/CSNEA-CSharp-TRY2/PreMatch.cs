using CSNEA_CSharp_TRY2;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace CSNEA_CSharp_TRY2
{
    public partial class PreMatch : Form
    {

        readonly FormControls controls = new FormControls();
        private Team homeTeam;
        private Team awayTeam;

        //List of combo boxes for each position displayed on the form on each side
        private List<ComboBox> homePositionComboBoxes = new List<ComboBox>();
        private List<ComboBox> awayPositionComboBoxes = new List<ComboBox>();

        //List of labels for each player name displayed on the form
        private List<Label> homePlayerLabels = new List<Label>();
        private List<Label> awayPlayerLabels = new List<Label>();

        //List of strings containing the current text in each position combo box
        //When combo box updates, I can use these lists to see what the combo box text was before the contents were changed
        private List<string> lastHomePositions = new List<string>();
        private List<string> lastAwayPositions = new List<string>();

        //List of player objects containing all data of players from the database
        private List<Player> homePlayerData = new List<Player>();
        private List<Player> awayPlayerData = new List<Player>();

        public MySqlConnection connection = new MySqlConnection("server=172.16.11.231;uid=16AffanKhan;pwd=WyNGcQPO;database=16AffanKhan_NEA");
        public SqlConnection homeConnection = new SqlConnection("Data Source=LAPTOP-6M69CTOI;Initial Catalog=CS_NEA_DB;Integrated Security=True");

        public string startHomeFormation = "";
        public string startAwayFormation = "";

        public bool UsingDatabase;
        public PreMatch(Team hT, Team aT, bool db)
        {
            InitializeComponent();

            homeTeam = hT;
            awayTeam = aT;
            UsingDatabase = db;
        }

        private void PreMatch_Load(object sender, EventArgs e)
        {
            LoadMenu();
        }

        private void LoadMenu()
        {
            this.HorizontalScroll.Maximum = 0;
            this.AutoScroll = false;
            this.VerticalScroll.Visible = false;
            this.AutoScroll = true;

            #region Create label for the name of the leagues on both teams
            string[] teamNames = new string[] { homeTeam.TeamName, awayTeam.TeamName };
            for (int i = 0; i < 2; i++)
            {
                Label teamLabel = controls.CreateLabel(Controls, "team" + i, teamNames[i], 30, 250, 30 + (i * 300), 30);
                teamLabel.AutoSize = false;
                if (i == 0) { teamLabel.TextAlign = ContentAlignment.MiddleLeft; } else { teamLabel.TextAlign = ContentAlignment.MiddleRight; }
                teamLabel.Font = new Font("Microsoft Sans Serif", 15);
                teamLabel.ForeColor = Color.White;
            }
            #endregion

            #region Create the labels for the formation on each side
            //Default formation is the 4-3-3
            Label homeFormation = controls.CreateLabel(Controls, "homeFormation", "Formation: ", 30, 250, 30, 65);
            homeFormation.TextAlign = ContentAlignment.MiddleLeft;
            homeFormation.ForeColor = Color.White;

            Label awayFormation = controls.CreateLabel(Controls, "awayFormation", "Formation: ", 30, 250, 330, 65);
            awayFormation.TextAlign = ContentAlignment.MiddleRight;
            awayFormation.ForeColor = Color.White;
            #endregion

            List<string> homePlayerNames, awayPlayerNames;

            if (UsingDatabase)
            {
                homePlayerNames = GetPlayerNames(homeTeam, "Home", ref startHomeFormation, ref homePlayerData);
                awayPlayerNames = GetPlayerNames(awayTeam, "Away", ref startAwayFormation, ref awayPlayerData);
            }
            else
            {
                homePlayerNames = GetPlayerNames(homeTeam, "Home", ref startHomeFormation, ref homePlayerData);
                awayPlayerNames = GetPlayerNames(awayTeam, "Away", ref startAwayFormation, ref awayPlayerData);
            }

            int add = 0;
            for (int i = 1; i < 19; i++)
            {
                #region Create the position combo boxes

                ComboBox homePos = controls.CreateComboBox(Controls, "homePos" + i.ToString(), 30, 45, 30, 75 + ((25 * i) + add));
                homePos.DropDownStyle = ComboBoxStyle.DropDownList;
                homePos.RightToLeft = RightToLeft.Yes;
                homePositionComboBoxes.Add(homePos);
                homePos.BackColor = Color.DarkGreen;
                homePos.ForeColor = Color.White;
                homePos.FlatStyle = FlatStyle.Popup;

                ComboBox awayPos = controls.CreateComboBox(Controls, "awayPos" + i.ToString(), 30, 45, 530, 75 + ((25 * i) + add));
                awayPos.DropDownStyle = ComboBoxStyle.DropDownList;
                awayPositionComboBoxes.Add(awayPos);
                awayPos.BackColor = Color.DarkGreen;
                awayPos.ForeColor = Color.White;
                awayPos.FlatStyle = FlatStyle.Popup;

                #endregion

                #region Create the player name labels
                Label playerH = controls.CreateLabel(Controls, "hPlayer" + i.ToString(), homePlayerNames[i - 1], 30, 250, 80, 70 + ((25 * i) + add));
                playerH.TextAlign = ContentAlignment.MiddleLeft;
                playerH.ForeColor = Color.White;

                Label playerA = controls.CreateLabel(Controls, "aPlayer" + i.ToString(), awayPlayerNames[i - 1], 30, 250, 275, 70 + ((25 * i) + add));
                playerA.TextAlign = ContentAlignment.MiddleRight;
                playerA.ForeColor = Color.White;

                homePlayerLabels.Add(playerH);
                awayPlayerLabels.Add(playerA);
                #endregion

                //This creates a gap between the starting players and the bench players, looks nice on screen
                if (i == 11) {add = 10;}

                //These are the possible items in the combo boxes
                homePos.Items.AddRange(new object[] { "GK", "DF", "MF", "FW", "S1", "S2", "S3", "S4", "S5", "S6", "S7" });
                awayPos.Items.AddRange(new object[] { "GK", "DF", "MF", "FW", "S1", "S2", "S3", "S4", "S5", "S6", "S7" });

                //If we are past the starting 11, we can add the reserve option as well. This is only available for the substitute players and the reserve players
                if (i > 11 && homePlayerNames.Count > 18) //No point adding a reserve option if there are no boxes for reserves at the end - this avoids an error
                {
                    homePos.Items.Add("RES");
                    awayPos.Items.Add("RES");
                }
            }
            add = 20;
            //If there are reserves, add extra combo boxes for them at the end of the loop, same process as before. This code avoids an error
            #region Add reserves for home team if there are any
            if (homePlayerNames.Count > 18)
            {
                for (int i = 19; i <= homePlayerNames.Count; i++)
                {
                    ComboBox homePos = controls.CreateComboBox(Controls, "homePos" + i.ToString(), 30, 45, 30, 75 + ((25 * i) + add));
                    homePos.DropDownStyle = ComboBoxStyle.DropDownList;
                    homePositionComboBoxes.Add(homePos);
                    homePos.BackColor = Color.DarkGreen;
                    homePos.ForeColor = Color.White;
                    homePos.FlatStyle = FlatStyle.Popup;
                    homePos.RightToLeft = RightToLeft.Yes;

                    Label playerH = controls.CreateLabel(Controls, "hPlayer" + i.ToString(), homePlayerNames[i - 1], 30, 250, 80, 70 + ((25 * i) + add));
                    playerH.TextAlign = ContentAlignment.MiddleLeft;
                    playerH.ForeColor = Color.White;

                    homePos.Items.AddRange(new object[] { "S1", "S2", "S3", "S4", "S5", "S6", "S7", "RES" });
                    homePlayerLabels.Add(playerH);
                }
            }
            #endregion
            #region Add reserves for away team if there are any
            if (awayPlayerNames.Count > 18)
            {
                for (int i = 19; i <= awayPlayerNames.Count; i++)
                {
                    ComboBox awayPos = controls.CreateComboBox(Controls, "awayPos" + i.ToString(), 30, 45, 530, 75 + ((25 * i) + add));
                    awayPos.DropDownStyle = ComboBoxStyle.DropDownList;
                    awayPositionComboBoxes.Add(awayPos);
                    awayPos.BackColor = Color.DarkGreen;
                    awayPos.ForeColor = Color.White;
                    awayPos.FlatStyle = FlatStyle.Popup;

                    Label playerA = controls.CreateLabel(Controls, "aPlayer" + i.ToString(), awayPlayerNames[i - 1], 30, 250, 275, 70 + ((25 * i) + add));
                    playerA.TextAlign = ContentAlignment.MiddleRight;
                    playerA.ForeColor = Color.White;

                    awayPos.Items.AddRange(new object[] { "S1", "S2", "S3", "S4", "S5", "S6", "S7", "RES" });
                    awayPlayerLabels.Add(playerA);
                }
            }
            #endregion

            homeFormation.Text = "Formation: " + startHomeFormation;
            awayFormation.Text = "Formation: " + startAwayFormation;

            InitialisePositions(homeFormation.Text, homePositionComboBoxes);
            InitialisePositions(awayFormation.Text, awayPositionComboBoxes);

            foreach (var homePos in homePositionComboBoxes)
            {
                homePos.SelectionChangeCommitted += (sender, e) => PositionChange_SelectedIndexChanged(sender, e, ref homeFormation, ref homePlayerLabels, ref homePositionComboBoxes, ref lastHomePositions);
                lastHomePositions.Add(homePos.Text);
            }

            foreach (var awayPos in awayPositionComboBoxes)
            {
                awayPos.SelectionChangeCommitted += (sender, e) => PositionChange_SelectedIndexChanged(sender, e, ref awayFormation, ref awayPlayerLabels, ref awayPositionComboBoxes, ref lastAwayPositions);
                lastAwayPositions.Add(awayPos.Text);
            }

            #region Create Start button
            Button startButton = controls.CreateButton(Controls, "startButton", "Start", 100, 130, 670, 100);
            startButton.Font = new Font("Microsoft Sans Serif", 13);
            startButton.ForeColor = Color.White;
            startButton.BackColor = Color.DarkGreen;
            startButton.BringToFront();
            startButton.Focus();
            startButton.Click += StartButton_Click;
            #endregion
        }

        private void StartButton_Click(object sender, EventArgs e)
        {
            //Finally reorder the list of player data based on the line-ups set
            homePlayerData = homePlayerData.OrderBy(player => homePlayerLabels.FindIndex(label => label.Text.Substring(5) == player.Name)).ToList();
            awayPlayerData = awayPlayerData.OrderBy(player => awayPlayerLabels.FindIndex(label => label.Text.Substring(0, label.Text.Length - 5) == player.Name)).ToList();
            
            for (int i = 0; i < homePlayerData.Count; i++) {
                homePlayerData[i].FormationPosition = homePositionComboBoxes[i].SelectedItem.ToString();
            }
            for (int i = 0; i < awayPlayerData.Count; i++) {
                awayPlayerData[i].FormationPosition = awayPositionComboBoxes[i].SelectedItem.ToString();
            }

            //Clear the form and place a large label in the middle saying "Simulating..." 
            controls.ClearForm(this.Controls);
            Label simulating = controls.CreateLabel(Controls, "simulating", "Simulating...", 50, 250, 0, 0);
            simulating.Dock = DockStyle.Fill;
            simulating.TextAlign = ContentAlignment.MiddleCenter;
            simulating.Font = new Font("Microsoft Sans Serif", 40);
            simulating.ForeColor = Color.White;
            simulating.AutoSize = false;

            //Open the results form
            Form results = new Results(homeTeam, awayTeam, homePlayerData, awayPlayerData);

            results.Show();
            this.Hide();
        }

        private void PositionChange_SelectedIndexChanged(object sender, EventArgs e, ref Label formation, ref List<Label> nameLabels, ref List<ComboBox> positionComboBoxes, ref List<string> lastPositionComboBoxes)
        {
            ComboBox comboBox = (ComboBox)sender;
            string selected = (string)comboBox.SelectedItem;
            int index = positionComboBoxes.FindIndex(x => x == comboBox);
            #region Complex code used when moving positions in the lineups
            if (selected == "GK" || selected == "S1" || selected == "S2" || selected == "S3" || selected == "S4" || selected == "S5" || selected == "S6" || selected == "S7")
            {
                //Switching to fixed position
                //Find the index of the combo box, then change the player name at that index
                int oldIndex = index;
                comboBox.SelectedItem = lastPositionComboBoxes[oldIndex];

                // The new index which has the combo box the user is trying to switch to
                int newIndex = 0;
                for (int i = 0; i < positionComboBoxes.Count; i++)
                {
                    if (positionComboBoxes[i].Text == selected) {
                        newIndex = i;
                    }
                }
                string newPos = selected;
                string lastPos = lastPositionComboBoxes[oldIndex];
                comboBox.SelectedItem = lastPos; //Reset the position
                string oldLabelCopy = nameLabels[oldIndex].Text;
                nameLabels[oldIndex].Text = nameLabels[newIndex].Text;
                nameLabels[newIndex].Text = oldLabelCopy;
            }
            else
            {
                if (lastPositionComboBoxes[index] == "GK" || lastPositionComboBoxes[index] == "S1" || lastPositionComboBoxes[index] == "S2" || lastPositionComboBoxes[index] == "S3" || lastPositionComboBoxes[index] == "S4" || lastPositionComboBoxes[index] == "S5" || lastPositionComboBoxes[index] == "S6" || lastPositionComboBoxes[index] == "S7")
                {
                    // Switching from fixed position
                    int oldIndex = index;
                    comboBox.SelectedItem = lastPositionComboBoxes[oldIndex];

                    int newIndex = 0; // The new index which has the combo box the user is trying to switch to
                    for (int i = 0; i < positionComboBoxes.Count; i++)
                    {
                        if (positionComboBoxes[i].Text == selected) {
                            newIndex = i;
                        }
                    }

                    string newPos = selected;

                    string lastPos = lastPositionComboBoxes[oldIndex];
                    comboBox.SelectedItem = lastPos; //Reset the position
                    string oldLabelCopy = nameLabels[oldIndex].Text;
                    nameLabels[oldIndex].Text = nameLabels[newIndex].Text;
                    nameLabels[newIndex].Text = oldLabelCopy;
                }
                else
                {
                    string prev = "", next = "";
                    try {
                        prev = positionComboBoxes[index - 1].Text;
                    } catch (Exception) { }
                    try {
                        next = positionComboBoxes[index + 1].Text;
                    } catch (Exception) { }

                    if (prev == positionComboBoxes[index].Text || next == positionComboBoxes[index].Text) 
                    { 
                    }
                    else
                    {
                        // Sort the list of combo boxes by positions
                        List<string> preferences = new List<string> { "GK", "DF", "MF", "FW", "S1", "S2", "S3", "S4", "S5", "S6", "S7", "RES" };
                        List<string> boxTexts = new List<string>();
                        foreach (var item in positionComboBoxes) {boxTexts.Add((string)item.SelectedItem);}
                        List<string> orderedBoxes = boxTexts.OrderBy(item => preferences.IndexOf(item)).ToList<string>();

                        int indexNew = 0;
                        for (int i = 0; i < orderedBoxes.Count; i++)
                        {
                            if (orderedBoxes[i] == comboBox.Text) {indexNew = i;}
                        }

                        for (int i = 0; i < positionComboBoxes.Count; i++) {
                            positionComboBoxes[i].SelectedItem = orderedBoxes[i];
                        }

                        List<string> newNames = new List<string>();

                        if (index != indexNew)
                        {
                            for (int i = 0; i < nameLabels.Count; i++)
                            {
                                if (i == index)
                                {
                                    i += 1;
                                    newNames.Add(nameLabels[i].Text);
                                }
                                else if (i == indexNew)
                                {
                                    newNames.Add(nameLabels[index].Text);
                                    newNames.Add(nameLabels[i].Text);
                                }
                                else {
                                    newNames.Add(nameLabels[i].Text);
                                }
                            }
                        }

                        for (int i = 0; i < newNames.Count; i++) {
                            nameLabels[i].Text = newNames[i];
                        }
                    }
                }
            }
            #endregion

            #region Update the formation text
            lastPositionComboBoxes.Clear();
            int DF = 0, MF = 0, FW = 0;
            foreach (var pos in positionComboBoxes)
            {
                lastPositionComboBoxes.Add(pos.Text);
                if (pos.Text == "DF") {DF += 1;}
                else if (pos.Text == "MF") {MF += 1;}
                else if (pos.Text == "FW") {FW += 1;}
            }

            formation.Text = "Formation: " + DF + "-" + MF + "-" + FW;
            #endregion
        }

        private void InitialisePositions(string Formation, List<ComboBox> positions)
        {
            Formation = Formation.Remove(0, 11);
            var formationParts = Formation.Split("-".ToCharArray());
            var maxDF = Int32.Parse(formationParts[0]);
            var maxMF = Int32.Parse(formationParts[1]);
            var maxFW = Int32.Parse(formationParts[2]);

            #region Set the selected item of each combo box to the positions in the formation
            positions[0].SelectedItem = "GK";
            for (int i = 1; i < 18; i++)
            {
                if (i <= maxDF) {positions[i].SelectedItem = "DF";}
                else if ((i - maxDF) <= maxMF) {
                    positions[i].SelectedItem = "MF";
                }
                else if (((i - maxDF) - maxMF) <= maxFW) {
                    positions[i].SelectedItem = "FW";
                }

                for (int j = 1; j < 8; j++) {
                    positions[j + 10].SelectedItem = "S" + j.ToString();
                }
            }
            if (positions.Count > 18)
            {
                for (int i = 19; i <= positions.Count; i++) {
                    positions[i - 1].SelectedItem = "RES";
                }
            }
            #endregion
        }
        private List<string> GetPlayerNames(Team team, string side, ref string formation, ref List<Player> playerData)
        {
            #region Create lists for the players, positions and starters/subs/reserves
            List<string> GKs = new List<string>();
            List<string> DFs = new List<string>();
            List<string> MFs = new List<string>();
            List<string> FWs = new List<string>();
            List<string> Starters = new List<string>();
            List<string> Subs = new List<string>();
            List<string> Reserves = new List<string>();
            #endregion

            List<Player> data = playerData;

            #region Get player data from database
            if (UsingDatabase)
            {
                connection.Open();
                string Sql = "SELECT * FROM `Players` WHERE TeamID=@teamID";
                MySqlCommand cmd = new MySqlCommand
                {
                    Connection = connection,
                    CommandText = Sql
                };
                cmd.Parameters.AddWithValue("@teamID", team.TeamID);

                MySqlDataReader result;
                result = cmd.ExecuteReader();
                while (result.Read())
                {
                    string pos = TweakPosition(result.GetString(6));
                    data.Add(new Player { PlayerID = result.GetString(0), Name = result.GetString(1), team = team, league = team.League, Position = pos, Minutes = result.GetString(7), Appearances = result.GetString(8), Shots = result.GetString(9), ShotsOnTarget = result.GetString(10), Goals = result.GetString(11), Assists = result.GetString(12), Saves = result.GetString(13), GoalsConceded = result.GetString(14), Passes = result.GetString(15), KeyPasses = result.GetString(16), Tackles = result.GetString(17), FoulsCommitted = result.GetString(18), FoulsDrawn = result.GetString(19), YellowCards = result.GetString(20), RedCards = result.GetString(21), PenaltiesScored = result.GetString(22), PenaltiesMissed = result.GetString(23) });
                }
                result.Close();
                connection.Close();
            }
            else
            {
                homeConnection.Open();
                string Sql = "SELECT * FROM \"Players\" WHERE TeamID=@teamID";
                SqlCommand cmd = new SqlCommand
                {
                    Connection = homeConnection,
                    CommandText = Sql
                };
                cmd.Parameters.AddWithValue("@teamID", team.TeamID);

                SqlDataReader result;
                result = cmd.ExecuteReader();
                while (result.Read())
                {
                    string pos = TweakPosition(result.GetString(6));
                    data.Add(new Player { PlayerID = result.GetString(0), Name = result.GetString(1), team = team, league = team.League, Position = pos, Minutes = result.GetString(7), Appearances = result.GetString(8), Shots = result.GetString(9), ShotsOnTarget = result.GetString(10), Goals = result.GetString(11), Assists = result.GetString(12), Saves = result.GetString(13), GoalsConceded = result.GetString(14), Passes = result.GetString(15), KeyPasses = result.GetString(16), Tackles = result.GetString(17), FoulsCommitted = result.GetString(18), FoulsDrawn = result.GetString(19), YellowCards = result.GetString(20), RedCards = result.GetString(21), PenaltiesScored = result.GetString(22), PenaltiesMissed = result.GetString(23) });
                }
                result.Close();
                homeConnection.Close();
            }
            #endregion

            try {
                data = data.OrderByDescending(player => int.Parse(player.Minutes)).ToList();
            } catch (Exception) { }

            #region Organise each player in the list into separate lists by position
            foreach (var item in data)
            {
                string playerID = item.PlayerID;
                string playerName = item.Name;
                string playerPos = item.Position;
                //Result is the row in the table
                if (side == "Away")
                {
                    if (playerPos == "GK") {GKs.Add(playerName + " (GK)");}
                    else if (playerPos == "DF") {DFs.Add(playerName + " (DF)");}
                    else if (playerPos == "MF") {MFs.Add(playerName + " (MF)");}
                    else if (playerPos == "FW") {FWs.Add(playerName + " (FW)");}
                }
                else
                {
                    if (playerPos == "GK") {GKs.Add("(GK) " + playerName);}
                    else if (playerPos == "DF") {DFs.Add("(DF) " + playerName);}
                    else if (playerPos == "MF") {MFs.Add("(MF) " + playerName);}
                    else if (playerPos == "FW") {FWs.Add("(FW) " + playerName);}
                }
            }
            #endregion

            #region Pick the formation to make then organise all the players into the formation
            //Check if the number of players in each position suits the requirements of the formation
            bool works = false;
            string curFormation = "4-3-3";
            string[] possibleFormations = new string[] { "4-3-3", "4-4-2", "5-2-3", "5-3-2", "5-4-1", "3-4-3", "3-5-2", "4-5-1", "4-2-4" };
            int counter = 0;
            while (works == false)
            {
                if (DFs.Count < Int32.Parse(curFormation.Substring(0, 1)) || MFs.Count < Int32.Parse(curFormation.Substring(2, 1)) || FWs.Count < Int32.Parse(curFormation.Substring(4, 1)))
                {
                    counter += 1;
                    curFormation = possibleFormations[counter];
                }
                else {works = true;}
            }
            //These variables store the number of players for each position in the formation, for example in a 4-3-3, totalDF = 4, totalMF - 3 etc.
            int totalDF = Int32.Parse(curFormation.Substring(0, 1));
            int totalMF = Int32.Parse(curFormation.Substring(2, 1));
            int totalFW = Int32.Parse(curFormation.Substring(4, 1));

            #region Organise the goalkeepers
            Starters.Add(GKs[0]);
            if (GKs.Count > 1) 
            {
                Subs.Add(GKs[1]);
                for (int i = 2; i < GKs.Count; i++) {
                    Reserves.Add(GKs[i]);
                }
            }
            #endregion
            #region Organise the defenders
            for (int i = 0; i < totalDF; i++) {
                Starters.Add(DFs[i]);
            }
            if (DFs.Count > totalDF)
            {
                Subs.Add(DFs[totalDF]);
                if (DFs.Count > totalDF + 1)
                {
                    Subs.Add(DFs[totalDF + 1]);
                    for (int i = totalDF + 2; i < DFs.Count; i++) {
                        Reserves.Add(DFs[i]);
                    }
                }
            }
            #endregion
            #region Organise the midfielders
            for (int i = 0; i < totalMF; i++) {
                Starters.Add(MFs[i]);
            }
            if (MFs.Count > totalMF)
            {
                Subs.Add(MFs[totalMF]);
                if (MFs.Count > totalMF + 1)
                {
                    Subs.Add(MFs[totalMF + 1]);
                    for (int i = totalMF + 2; i < MFs.Count; i++) {
                        Reserves.Add(MFs[i]);
                    }
                }
            }
            #endregion
            #region Organise the forwards
            for (int i = 0; i < totalFW; i++) {
                Starters.Add(FWs[i]);
            }
            if (FWs.Count > totalFW)
            {
                Subs.Add(FWs[totalFW]);
                if (FWs.Count > totalFW + 1)
                {
                    Subs.Add(FWs[totalFW + 1]);
                    for (int i = totalFW + 2; i < FWs.Count; i++) {
                        Reserves.Add(FWs[i]);
                    }
                }
            }
            #endregion

            //Add each player name to the lists for starters, subs and reserves then combine and return back to the main code
            List<string> squadPlayerNames = new List<string>();
            foreach (var item in Starters) {
                squadPlayerNames.Add(item);
            }
            foreach (var item in Subs) {
                squadPlayerNames.Add(item);
            }
            foreach (var item in Reserves) {
                squadPlayerNames.Add(item);
            }
            formation = curFormation;
            return squadPlayerNames;
            #endregion
        }
        private string TweakPosition(string position)
        {
            string pos = "";
            if (position == "Goalkeeper") { pos = "GK"; }
            else if (position == "Defender") { pos = "DF"; }
            else if (position == "Midfielder") { pos = "MF"; }
            else if (position == "Attacker") { pos = "FW"; }
            return pos;
        }
    }
}

public class Player
{
    public string Name = "";
    public Team team;
    public League league;
    public string PlayerID;
    public string Minutes;
    public string Appearances;
    public string Position;
    public string Shots;
    public string ShotsOnTarget;
    public string FormationPosition;
    public string Goals;
    public string Assists;
    public string Saves;
    public string GoalsConceded;
    public string Passes;
    public string KeyPasses;
    public string Tackles;
    public string FoulsCommitted;
    public string FoulsDrawn;
    public string RedCards;
    public string YellowCards;
    public string PenaltiesScored;
    public string PenaltiesMissed;
}