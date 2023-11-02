using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using MySql.Data.MySqlClient;
using System.Data.SqlClient;

namespace CSNEA_CSharp_TRY2
{
    public partial class SelectTeams : Form
    {
        readonly FormControls controls = new FormControls();
        public SelectTeams()
        {
            InitializeComponent();
        }

        public bool UsingDatabase = false;

        public MySqlConnection connection = new MySqlConnection("server=172.16.11.231;uid=16AffanKhan;pwd=WyNGcQPO;database=16AffanKhan_NEA"); //Connection for school database
        public SqlConnection homeConnection = new SqlConnection("Data Source=LAPTOP-6M69CTOI;Initial Catalog=CS_NEA_DB;Integrated Security=True"); //Connection for home database

        List<League> Leagues = new List<League>(); //List of all leagues from the database
        List<Team> Teams = new List<Team>(); //List of all teams from the database

        List<Team> curTeamsH = new List<Team>(); //The teams from the currently selected league
        List<Team> curTeamsA = new List<Team>();

        private int leagueCounterH = 0; //Using leagueCounter as a pointer for the Leagues list
        private int leagueCounterA = 0;

        private int teamCounterH = 0; //Using teamCounter as a pointer for the curTeams list
        private int teamCounterA = 0;

        private PictureBox homeLogo = new PictureBox();
        private PictureBox awayLogo = new PictureBox();

        private readonly string fullPath = Path.GetFullPath("Logos"); //File path of the logos folder

        private string homeLeagueID, awayLeagueID, logosFolderPath;
        private void SelectTeams_Load(object sender, EventArgs e)
        {

            if (UsingDatabase)
            {
                //------------------------------------- USING SCHOOL DATABASE -------------------------------------
                connection.Open();
                string Sql = "SELECT LeagueID, LeagueName FROM `Leagues`";
                MySqlCommand cmd = new MySqlCommand();
                cmd.Connection = connection;
                cmd.CommandText = Sql;

                MySqlDataReader result;
                result = cmd.ExecuteReader();
                //Read the data
                while (result.Read())
                {
                    Leagues.Add(new League() { LeagueID = Int32.Parse(result.GetString(0)), LeagueName = result.GetString(1) });
                }
                result.Close();

                Leagues = Leagues.OrderByDescending(l => l.LeagueName.Length).ToList();

                homeLeagueID = Leagues[0].LeagueID.ToString();
                awayLeagueID = Leagues[0].LeagueID.ToString();
                //Get the teams from the database
                Sql = "SELECT TeamID, TeamName, LeagueID FROM `Teams` ORDER BY `LeagueID` ASC";
                cmd = new MySqlCommand();
                cmd.Connection = connection;
                cmd.CommandText = Sql;

                result = cmd.ExecuteReader();
                //Read the data
                while (result.Read())
                {
                    string teamID = result.GetString(0);
                    string teamName = result.GetString(1);
                    string leagueID = result.GetString(2);
                    
                    Teams.Add(new Team() { TeamID = Int32.Parse(result.GetString(0)), TeamName = result.GetString(1), League = Leagues.Find(l => l.LeagueID == Int32.Parse(result.GetString(2))) });
                }
                result.Close();
                Teams.Sort((x, y) => string.Compare(x.TeamName, y.TeamName));
                connection.Close();
            }
            else
            {
                //For home database, use this code instead
                homeConnection.Open();
                string Sql = "SELECT LeagueID, LeagueName FROM \"Leagues\"";
                SqlCommand cmd = new SqlCommand();
                cmd.Connection = homeConnection;
                cmd.CommandText = Sql;

                SqlDataReader result;
                result = cmd.ExecuteReader();
                //Read the data
                while (result.Read())
                {
                    Leagues.Add(new League() { LeagueID = Int32.Parse(result.GetString(0)), LeagueName = result.GetString(1) });
                }
                result.Close();

                Leagues = Leagues.OrderByDescending(l => l.LeagueName.Length).ToList();

                homeLeagueID = Leagues[0].LeagueID.ToString();
                awayLeagueID = Leagues[0].LeagueID.ToString();
                //Get the teams from the database
                Sql = "SELECT TeamID, TeamName, LeagueID FROM \"Teams\" ORDER BY \"LeagueID\" ASC";
                cmd = new SqlCommand();
                cmd.Connection = homeConnection;
                cmd.CommandText = Sql;

                result = cmd.ExecuteReader();
                //Read the data
                while (result.Read())
                {
                    string teamID = result.GetInt32(0).ToString();
                    string teamName = result.GetString(1);
                    string leagueID = result.GetInt32(2).ToString();

                    Teams.Add(new Team() { TeamID = Int32.Parse(teamID), TeamName = teamName, League = Leagues.Find(l => l.LeagueID == Int32.Parse(leagueID)) });
                }
                result.Close();
                Teams.Sort((x, y) => string.Compare(x.TeamName, y.TeamName));
                homeConnection.Close();
            }

            logosFolderPath = Path.Combine(Path.GetDirectoryName(Path.GetDirectoryName(Path.GetDirectoryName(fullPath))), "Logos");

            LoadMenu();

            ResetTeamsArray("Hteam", ref curTeamsH, leagueCounterH);
            ResetTeamsArray("Ateam", ref curTeamsA, leagueCounterA);

            int teamId = curTeamsH[teamCounterH].TeamID; //Get the team object by look at the object at current index in the list of current teams
            homeLogo.ImageLocation = Path.Combine(logosFolderPath, teamId.ToString() + ".png");
            teamId = curTeamsA[teamCounterA].TeamID;
            awayLogo.ImageLocation = Path.Combine(logosFolderPath, teamId.ToString() + ".png");
        }

        private void StartMatch(object sender, EventArgs e)
        {
            controls.ClearForm(this.Controls);
            Label loading = controls.CreateLabel(Controls, "loading", "Loading...", 50, 250, 0, 0);
            loading.Dock = DockStyle.Fill;
            loading.TextAlign = ContentAlignment.MiddleCenter;
            loading.Font = new Font("Microsoft Sans Serif", 40);
            loading.ForeColor = Color.White;
            loading.AutoSize = false;

            Form preMatch = new PreMatch(curTeamsH[teamCounterH], curTeamsA[teamCounterA], UsingDatabase);
            preMatch.Show();
            this.Hide();
        }

        private void UpdateDisplay(object sender, EventArgs e, string homeOrAway, string teamOrLeague, string direction, ref int counter, ref string leagueID, ref List<Team> curTeams, PictureBox logo)
        {
            if (teamOrLeague == "league")
            {
                if (direction == "left") {
                    counter = (counter + Leagues.Count - 1) % Leagues.Count;
                }
                else {
                    counter = (counter + Leagues.Count + 1) % Leagues.Count;
                }
                Controls.Find(homeOrAway + teamOrLeague, false)[0].Text = Leagues[counter].LeagueName;
                leagueID = Leagues[counter].LeagueID.ToString();
                ResetTeamsArray(homeOrAway + "team", ref curTeams, counter);
                int teamID = curTeams[0].TeamID;
                logo.ImageLocation = Path.Combine(logosFolderPath, teamID.ToString() + ".png");
            }
            else
            {
                if (direction == "left") {
                    counter = (counter + curTeams.Count - 1) % curTeams.Count;
                }
                else {
                    counter = (counter + curTeams.Count + 1) % curTeams.Count;
                }
                Controls.Find(homeOrAway + "team", false)[0].Text = curTeams[counter].TeamName;
                int teamID = curTeams[counter].TeamID;
                logo.ImageLocation = Path.Combine(logosFolderPath, teamID.ToString() + ".png");
            }
        }

        private void ResetTeamsArray(string labelName, ref List<Team> teamNames, int leagueCounter)
        {
            string curID = Leagues[leagueCounter].LeagueID.ToString();
            teamNames.Clear();
            foreach (var team in Teams)
            {
                if (team.League.LeagueID.ToString() == curID) {
                    teamNames.Add(team);
                }
            }
            Controls.Find(labelName, false)[0].Text = teamNames[0].TeamName;
        }

        private void LoadMenu()
        {
            List<Control> bulkInstructions = new List<Control>();

            Label homeLabel = controls.CreateLabel(Controls, "HomeLabel", "Home", 30, 240, 60, 30);
            Label awayLabel = controls.CreateLabel(Controls, "AwayLabel", "Away", 30, 240, 510, 30);

            bulkInstructions.AddRange(new Label[] { homeLabel, awayLabel });
            foreach (Label item in bulkInstructions)
            {
                item.Font = new Font("Microsoft Sans Serif", 17, FontStyle.Underline);
                item.ForeColor = Color.White;
                item.AutoSize = false;
                item.TextAlign = ContentAlignment.MiddleCenter;
            }
            bulkInstructions.Clear();

            //Start button
            Button startButton = controls.CreateButton(Controls, "StartButton", "Start", 80, 110, 350, 310);
            startButton.Click += StartMatch;
            startButton.TextAlign = ContentAlignment.MiddleCenter;
            startButton.Font = new Font("Microsoft Sans Serif", 17);

            //Update Database button
            Button updateStats = controls.CreateButton(Controls, "UpdateStats", "Update DB", 40, 110, 350, 395);
            updateStats.AutoSize = false;
            updateStats.TextAlign = ContentAlignment.MiddleCenter;
            updateStats.Font = new Font("Microsoft Sans Serif", 12);
            updateStats.Click += UpdateStats_Click;
            Button HleagueLeft = controls.CreateButton(Controls, "HleagueLeft", "<", 30, 30, 30, 100);
            HleagueLeft.Click += (sender, e) => { UpdateDisplay(sender, e, "H", "league", "left", ref leagueCounterH, ref homeLeagueID, ref curTeamsH, homeLogo); };
            Button HleagueRight = controls.CreateButton(Controls, "HleagueRight", ">", 30, 30, 300, 100);
            HleagueRight.Click += (sender, e) => { UpdateDisplay(sender, e, "H", "league", "right", ref leagueCounterH, ref homeLeagueID, ref curTeamsH, homeLogo); };
            Button AleagueLeft = controls.CreateButton(Controls, "AleagueLeft", "<", 30, 30, 480, 100);
            AleagueLeft.Click += (sender, e) => { UpdateDisplay(sender, e, "A", "league", "left", ref leagueCounterA, ref awayLeagueID, ref curTeamsA, awayLogo); };
            Button AleagueRight = controls.CreateButton(Controls, "AleagueRight", ">", 30, 30, 750, 100);
            AleagueRight.Click += (sender, e) => { UpdateDisplay(sender, e, "A", "league", "right", ref leagueCounterA, ref awayLeagueID, ref curTeamsA, awayLogo); };
            Button HteamLeft = controls.CreateButton(Controls, "HteamLeft", "<", 30, 30, 30, 350);
            HteamLeft.Click += (sender, e) => { UpdateDisplay(sender, e, "H", "team", "left", ref teamCounterH, ref homeLeagueID, ref curTeamsH, homeLogo); };
            Button HteamRight = controls.CreateButton(Controls, "HteamRight", ">", 30, 30, 300, 350);
            HteamRight.Click += (sender, e) => { UpdateDisplay(sender, e, "H", "team", "right", ref teamCounterH, ref homeLeagueID, ref curTeamsH, homeLogo); };
            Button AteamLeft = controls.CreateButton(Controls, "AteamLeft", "<", 30, 30, 480, 350);
            AteamLeft.Click += (sender, e) => { UpdateDisplay(sender, e, "A", "team", "left", ref teamCounterA, ref awayLeagueID, ref curTeamsA, awayLogo); };
            Button AteamRight = controls.CreateButton(Controls, "AteamRight", ">", 30, 30, 750, 350);
            AteamRight.Click += (sender, e) => { UpdateDisplay(sender, e, "A", "team", "right", ref teamCounterA, ref awayLeagueID, ref curTeamsA, awayLogo); };
            
            bulkInstructions.AddRange(new Control[] { startButton, HleagueLeft, HleagueRight, AleagueLeft, AleagueRight, HteamLeft, HteamRight, AteamLeft, AteamRight, updateStats });
            foreach (var item in bulkInstructions)
            {
                item.BackColor = Color.DarkGreen;
                item.ForeColor = Color.White;
            }
            bulkInstructions.Clear();

            Label HleagueLabel = controls.CreateLabel(Controls, "Hleague", Leagues[leagueCounterH].LeagueName, 30, 240, 60, 100);
            Label AleagueLabel = controls.CreateLabel(Controls, "Aleague", Leagues[leagueCounterA].LeagueName, 30, 240, 510, 100);
            Label HteamLabel = controls.CreateLabel(Controls, "Hteam", "", 30, 240, 60, 350);
            Label AteamLabel = controls.CreateLabel(Controls, "Ateam", "", 30, 240, 510, 350);

            bulkInstructions.AddRange(new Control[] { HleagueLabel, AleagueLabel, HteamLabel, AteamLabel });
            foreach (Label item in bulkInstructions)
            {
                item.AutoSize = false;
                item.TextAlign = ContentAlignment.MiddleCenter;
                item.Font = new Font("Microsoft Sans Serif", 17);
                item.ForeColor = Color.White;
            }
            bulkInstructions.Clear();

            //Home logo picture box
            homeLogo = controls.CreatePictureBox(Controls, "homeLogo", 150, 240, 60, 160);
            homeLogo.SizeMode = PictureBoxSizeMode.CenterImage;

            //Away logo picture box
            awayLogo = controls.CreatePictureBox(Controls, "awayLogo", 150, 240, 510, 160);
            awayLogo.SizeMode = PictureBoxSizeMode.CenterImage;
        }

        private void UpdateStats_Click(object sender, EventArgs e)
        {
            UpdateDB updateDB = new UpdateDB();
            updateDB.Show();

        }
    }

    public class Team
    {
        public int TeamID;
        public string TeamName;
        public League League;
    }
    public class League
    {
        public int LeagueID;
        public string LeagueName;
    }
    class FormControls
    {
        public Button CreateButton(Control.ControlCollection Controls, string name, string text, int height, int width, int locationX, int locationY)
        {
            // Create a Button object  
            Button button = new Button
            {
                // Set Button properties  
                Height = height,
                Width = width,
                Location = new Point(locationX, locationY),
                Text = text,
                Name = name
            };

            Controls.Add(button);
            return button;
        }

        public Label CreateLabel(Control.ControlCollection Controls, string name, string text, int height, int width, int locationX, int locationY)
        {
            // Create a label object  
            Label label = new Label
            {
                // Set label properties  
                Height = height,
                Width = width,
                Location = new Point(locationX, locationY),
                Text = text,
                Name = name
            };

            Controls.Add(label);
            return label;
        }

        public PictureBox CreatePictureBox(Control.ControlCollection Controls, string name, int height, int width, int locationX, int locationY)
        {
            // Create a Picture Box object
            PictureBox picBox = new PictureBox
            {
               //Set Picture Box properties
               Height = height,
               Width = width,
               Location = new Point(locationX, locationY),
               Name = name,
            };
            Controls.Add(picBox);
            return picBox;
        }

        public ComboBox CreateComboBox(Control.ControlCollection Controls, string name, int height, int width, int locationX, int locationY)
        {
            ComboBox comboBox = new ComboBox
            {
                Name = name,
                Location = new Point(locationX, locationY),
                Height = height,
                Width = width,
            };
            Controls.Add(comboBox);
            return comboBox;
        }

        public CheckBox CreateCheckBox(Control.ControlCollection Controls, string name, string text, int height, int width, int locationX, int locationY)
        {
            CheckBox checkBox = new CheckBox
            {
                Name = name,
                Location = new Point(locationX, locationY),
                Height = height,
                Width = width,
                Text = text,
            };
            Controls.Add(checkBox);
            return checkBox;
        }

        public CheckedListBox CreateCheckListBox(Control.ControlCollection Controls, string name, string[] texts, int height, int width, int locationX, int locationY)
        {
            CheckedListBox checkedListBox = new CheckedListBox
            {
                Name = name,
                Location = new Point(locationX, locationY),
                Height = height,
                Width = width,
            };
            foreach (var item in texts)
            {
                checkedListBox.Items.Add(item);
            }
            Controls.Add(checkedListBox);
            return checkedListBox;
        }

        public void ClearForm(Control.ControlCollection Controls)
        {
            foreach (Control control in Controls)
            {
                control.Hide();
            }
        }
    }
}
