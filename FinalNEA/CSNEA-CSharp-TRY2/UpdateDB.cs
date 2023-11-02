using HtmlAgilityPack;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CSNEA_CSharp_TRY2
{
    public partial class UpdateDB : Form
    {
        readonly FormControls controls = new FormControls();
        public UpdateDB()
        {
            InitializeComponent();
        }

        private Button TeamInfo = new Button(); //Click this to get data for teams
        private Button PlayerInfo = new Button(); //Click this to get data for players
        private CheckedListBox League = new CheckedListBox(); //Checkboxes for the different leagues to get data on
        private Label totalApiCalls = new Label(); //The total API calls made in the past 24 hours, used to track rate limits
        private HttpClient client = new HttpClient(); //HTTP client to make API requests
        private int apiCalls = 0;
        private CheckBox InsertingOrUpdating = new CheckBox(); //Can get either INSERT or UPDATE statements

        private async void UpdateDB_Load(object sender, EventArgs e)
        {
            LoadMenu();

            client.DefaultRequestHeaders.Add("x-apisports-key", "69edd0c93cce82599cc881539dd9a0a9");

            var response = await client.GetAsync("https://v3.football.api-sports.io/status");
            var json = await response.Content.ReadAsStringAsync();
            var root = JsonConvert.DeserializeObject<StatusRootObject>(json);
            var currentRequests = root.response.requests.current;

            totalApiCalls.Text += currentRequests + "/100";
            apiCalls = currentRequests;
        }
        private void LoadMenu()
        {
            League = controls.CreateCheckListBox(Controls, "League", new string[] { "Premier League", "Bundesliga", "Serie A", "La Liga", "Ligue 1" }, 100, 220, 10, 10);
            League.Font = new Font("Microsoft Sans Serif", 12);
            League.BackColor = Color.DarkGreen;
            League.ForeColor = Color.White;
            League.SelectionMode = SelectionMode.None;
            League.MouseDown += MatchSettings_MouseDown;

            InsertingOrUpdating = controls.CreateCheckBox(Controls, "InsertingOrUpdating", "Update statements (Insert otherwise)", 40, 500, 10, 120);
            InsertingOrUpdating.Font = new Font("Microsoft Sans Serif", 12);
            InsertingOrUpdating.ForeColor = Color.White;

            //Teams Info Button
            TeamInfo = controls.CreateButton(Controls, "TeamInfo", "Get Teams", 40, 100, 10, 200);
            TeamInfo.TextAlign = ContentAlignment.MiddleCenter;
            TeamInfo.Font = new Font("Microsoft Sans Serif", 12);
            TeamInfo.BackColor = Color.DarkGreen;
            TeamInfo.ForeColor = Color.White;
            TeamInfo.Click += ShowGetTeams;

            //Get Player Info Button
            PlayerInfo = controls.CreateButton(Controls, "PlayerInfo", "Get Players", 40, 100, 120, 200);
            PlayerInfo.TextAlign = ContentAlignment.MiddleCenter;
            PlayerInfo.Font = new Font("Microsoft Sans Serif", 12);
            PlayerInfo.BackColor = Color.DarkGreen;
            PlayerInfo.ForeColor = Color.White;
            PlayerInfo.Click += ShowGetPlayers;

             //Total API calls label
            totalApiCalls = controls.CreateLabel(Controls, "TotalApiCalls", "API Calls: ", 50, 80, 250, 10);
            totalApiCalls.TextAlign = ContentAlignment.MiddleCenter;
            totalApiCalls.Font = new Font("Microsoft Sans Serif", 12);
            totalApiCalls.ForeColor = Color.White;
        }
        private async void ShowGetTeams(object sender, EventArgs e)
        {

            List<string> urls = new List<string>(); //This list will hold the image URLs of the team badges
            List<string> sqlLines = new List<string>();

            MessageBox.Show("Loading. A notepad window will open with the text file after all teams are loaded.");

            LoadingWithStopwatch lws = new LoadingWithStopwatch();
            lws.Show();

            foreach (var item in League.CheckedItems)
            {
                string leagueID = GetLeagueID(item.ToString());
                if (leagueID == "error")
                {
                    throw new TaskCanceledException();
                }
                var url = "https://v3.football.api-sports.io/teams?league=" + leagueID + "&season=2022";
                var response = await client.GetAsync(url);
                var json = await response.Content.ReadAsStringAsync();
                var root = JsonConvert.DeserializeObject<TeamsRootObject>(json);
                foreach (var header in response.Headers)
                {
                    Console.WriteLine($"{header.Key}: {string.Join(", ", header.Value)}");
                }
                foreach (var team in root.response)
                {
                    var sql = $"INSERT INTO Teams (TeamID, TeamName, TeamCode, LeagueID, Country, Logo) VALUES({team.team.id}, '{team.team.name}', '{team.team.code}', '{leagueID}', {team.team.country}', '{team.team.logo}');";
                    sqlLines.Add(sql);
                    urls.Add(team.team.logo);
                }

                apiCalls++;
            }
            totalApiCalls.Text = "API Calls: " + apiCalls.ToString() + "/100";

            // DownloadImages(urls);

            WriteSQLtoTextFile(sqlLines);
            lws.StopTimer();
        }

        private async void ShowGetPlayers(object sender, EventArgs e)
        {
           
            List<string> sqlLines = new List<string>();
            bool inserting = true;
            if (InsertingOrUpdating.Checked) { inserting = false; };
            MessageBox.Show("Loading. A notepad window will open with the text file after all players are loaded.");

            LoadingWithStopwatch lws = new LoadingWithStopwatch();
            lws.Show();

            foreach (var item in League.CheckedItems)
            {
                string leagueID = GetLeagueID(item.ToString());
                if (leagueID == "error")
                {
                    throw new TaskCanceledException();
                }
                var url = "https://v3.football.api-sports.io/players?season=2022&league=" + leagueID;

                //Do the first run
                var response = await client.GetAsync(url);
                var json = await response.Content.ReadAsStringAsync();
                json = json.Replace("null", "0");
                var root = JsonConvert.DeserializeObject<PlayersRootObject>(json);
                foreach (var header in response.Headers)
                {
                    Console.WriteLine($"{header.Key}: {string.Join(", ", header.Value)}");
                }
                foreach (var player in root.response)
                {
                    string sql = GetPlayerSQL(player, inserting, leagueID);
                    Console.WriteLine(sql);
                    sqlLines.Add(sql);
                }
                apiCalls++;
                totalApiCalls.Text = "API Calls: " + apiCalls.ToString() + "/100";
                var totalPages = root.paging.total;

                if (totalPages > 1)
                {
                    for (int i = 2; i < totalPages; i++)
                    {
                        url += "&page=" + i.ToString();
                        response = await client.GetAsync(url);
                        json = await response.Content.ReadAsStringAsync();
                        json = json.Replace("null", "0");

                        root = JsonConvert.DeserializeObject<PlayersRootObject>(json);
                        foreach (var player in root.response)
                        {
                            string sql = GetPlayerSQL(player, inserting, leagueID);
                            Console.WriteLine(sql);
                            sqlLines.Add(sql);
                        }

                        apiCalls++;
                        totalApiCalls.Text = "API Calls: " + apiCalls.ToString() + "/100";
                        await Task.Delay(6600);
                    }
                }

            }
            totalApiCalls.Text = "API Calls: " + apiCalls.ToString() + "/100";
            WriteSQLtoTextFile(sqlLines);
            lws.StopTimer();
        }
        private void DownloadImages(List<string> urls)
        {
            //Download all image logo files to a folder
            string fullPath = Path.GetFullPath("Logos");
            string folderPath = Path.Combine(Path.GetDirectoryName(Path.GetDirectoryName(Path.GetDirectoryName(fullPath))), "Logos");
            using (WebClient webClient = new WebClient())
            {
                foreach (string link in urls)
                {
                    string fileName = Path.GetFileName(new Uri(link).LocalPath);
                    string filePath = Path.Combine(folderPath, fileName);
                    webClient.DownloadFile(link, filePath);
                }
            }
        }

        private string GetLeagueID(string leagueName)
        {
            switch (leagueName)
            {
                case "Premier League": return "39";
                case "Bundesliga": return "78";
                case "La Liga": return "140";
                case "Ligue 1": return "61";
                case "Serie A": return "235";
                default: return "error";
            }
        }

        private string GetPlayerSQL(PlayersResponse player, bool inserting, string leagueID)
        {
            if (inserting)
            {
                return  $"INSERT INTO Players (PlayerID, PlayerName, LeagueID, TeamID, FirstName, LastName, Position, Minutes, Appearances, Shots, ShotsOnTarget, Goals, Assists, Saves, GoalsConceded, Passes, KeyPasses, Tackles, FoulsComitted, FoulsDrawn, YellowCards, RedCards, PenaltiesScored, PenaltiesMissed) VALUES('{player.player.id}', '{player.player.name}', '{leagueID}', '{player.statistics[0].team.id}', '{player.player.firstname}', '{player.player.lastname}', '{player.statistics[0].games.position}', '{player.statistics[0].games.minutes}', '{player.statistics[0].games.appearences}', '{player.statistics[0].shots.total}', '{player.statistics[0].shots.on}', '{player.statistics[0].goals.total}', '{player.statistics[0].goals.assists}', '{player.statistics[0].goals.saves}', '{player.statistics[0].goals.conceded}', '{player.statistics[0].passes.total}', '{player.statistics[0].passes.key}', '{player.statistics[0].tackles.total}', '{player.statistics[0].fouls.committed}', '{player.statistics[0].fouls.drawn}', '{player.statistics[0].cards.yellow}', '{player.statistics[0].cards.red}', '{player.statistics[0].penalty.scored}', '{player.statistics[0].penalty.missed}');";
            }
            else
            {
                 return  $"UPDATE Players SET Minutes = '{player.statistics[0].games.minutes}', Appearances = '{player.statistics[0].games.appearences}', Shots = '{player.statistics[0].shots.total}', ShotsOnTarget = '{player.statistics[0].shots.on}', Goals = '{player.statistics[0].goals.total}', Assists = '{player.statistics[0].goals.assists}', Saves = '{player.statistics[0].goals.saves}', GoalsConceded = '{player.statistics[0].goals.conceded}', Passes = '{player.statistics[0].passes.total}', KeyPasses = '{player.statistics[0].passes.key}', Tackles = '{player.statistics[0].tackles.total}', FoulsComitted = '{player.statistics[0].fouls.committed}', FoulsDrawn = '{player.statistics[0].fouls.committed}', YellowCards = '{player.statistics[0].cards.yellow}', RedCards = '{player.statistics[0].cards.red}', PenaltiesScored = '{player.statistics[0].penalty.scored}', PenaltiesMissed = '{player.statistics[0].penalty.missed}' Where PlayerID = '{player.player.id}';";
            }
        }

        private void MatchSettings_MouseDown(object sender, MouseEventArgs e)
        {
            //When the mouse is clicked on one of the checkboxes, the whole row is highlighted
            //This code bypasses that
            CheckedListBox box = (CheckedListBox)sender;
            int Index = box.IndexFromPoint(e.Location);
            box.SetItemChecked(Index, !box.GetItemChecked(Index));
        }

        public void WriteSQLtoTextFile(List<string> strings)
        {
            string fullPath = Path.GetFullPath("SQL");
            string filePath = Path.Combine(Path.GetDirectoryName(Path.GetDirectoryName(Path.GetDirectoryName(fullPath))), "SQL.txt");
            StreamWriter file = new StreamWriter(filePath);
            file.Write(string.Empty);
            file.WriteLine("Number of SQL lines: " + strings.Count);
            foreach (var line in strings)
            {
                file.WriteLine(line);
            }
            file.Close();
            Process.Start("notepad.exe", filePath);
        }
    }
}

#region Response root object
class TeamsRootObject
{
    public string get { get; set; }
    public Parameters parameters { get; set; }
    public object[] errors { get; set; }
    public int results { get; set; }
    public Paging paging { get; set; }
    public TeamsResponse[] response { get; set; }
}
class PlayersRootObject
{
    public string get { get; set; }
    public Parameters parameters { get; set; }
    public object[] errors { get; set; }
    public int results { get; set; }
    public Paging paging { get; set; }
    public PlayersResponse[] response { get; set; }
}
class StatusRootObject
{
    public string get { get; set; }
    public object[] parameters { get; set; }
    public object[] errors { get; set; }
    public int results { get; set; }
    public StatusResponse response { get; set; }
}

class Account
{
    public string firstname { get; set; }
    public string lastname { get; set; }
    public string email { get; set; }
}
class Subscription
{
    public string plan { get; set; }
    public DateTime end { get; set; }
    public bool active { get; set; }
}
class Requests
{
    public int current { get; set; }
    public int limit_day { get; set; }
}
class Parameters
{
    public string league { get; set; }
    public string season { get; set; }
}
class Paging
{
    public int current { get; set; }
    public int total { get; set; }
}

class TeamsResponse
{
    public TeamAPI team { get; set; }
    public Venue venue { get; set; }
}
class PlayersResponse
{
    public PlayerAPI player { get; set; }
    public Statistics[] statistics { get; set; }
}
class StatusResponse
{
    public Account account { get; set; }
    public Subscription subscription { get; set; }
    public Requests requests { get; set; }
}

class TeamAPI
{
    public int? id { get; set; }
    public string name { get; set; }
    public string code { get; set; }
    public string country { get; set; }
    public int founded { get; set; }
    public bool national { get; set; }
    public string logo { get; set; }
}
class Venue
{
    public int id { get; set; }
    public string name { get; set; }
    public string address { get; set; }
    public string city { get; set; }
    public int capacity { get; set; }
    public string surface { get; set; }
    public string image { get; set; }
}
class PlayerAPI
{
    public int id { get; set; }
    public string name { get; set; }
    public string firstname { get; set; }
    public string lastname { get; set; }
    public int age { get; set; }
    public Birth birth { get; set; }
    public string nationality { get; set; }
    public string height { get; set; }
    public string weight { get; set; }
    public bool injured { get; set; }
    public string photo { get; set; }
}
class Birth
{
    public string date { get; set; }
    public string place { get; set; }
    public string country { get; set; }
}
class Statistics
{
    public TeamAPI team { get; set; }
    public LeagueAPI league { get; set; }
    public Games games { get; set; }
    public Substitutes substitutes { get; set; }
    public Shots shots { get; set; }
    public Goals goals { get; set; }
    public Passes passes { get; set; }
    public Tackles tackles { get; set; }
    public Duels duels { get; set; }
    public Dribbles dribbles { get; set; }
    public Fouls fouls { get; set; }
    public Cards cards { get; set; }
    public Penalty penalty { get; set; }
}
class LeagueAPI
{
    public int? id { get; set; }
    public string name { get; set; }
    public object country { get; set; }
    public object logo { get; set; }
    public object flag { get; set; }
    public string season { get; set; }
}
class Games
{
    public int appearences { get; set; }
    public int lineups { get; set; }
    public int minutes { get; set; }
    public object number { get; set; }
    public string position { get; set; }
    public object rating { get; set; }
    public bool captain { get; set; }
}
class Substitutes
{
    public int In { get; set; }
    public int Out { get; set; }
    public int bench { get; set; }
}
class Shots
{
    public object total { get; set; }
    public object on { get; set; }
}
class Goals
{
    public int total { get; set; }
    public object conceded { get; set; }
    public object assists { get; set; }
    public object saves { get; set; }
}
class Passes
{
    public object total { get; set; }
    public object key { get; set; }
    public object accuracy { get; set; }
}
class Tackles
{
    public object total { get; set; }
    public object blocks { get; set; }
    public object interceptions { get; set; }
}
class Duels
{
    public object total { get; set; }
    public object won { get; set; }
}
class Dribbles
{
    public object attempts { get; set; }
    public object success { get; set; }
    public object past { get; set; }
}
class Fouls
{
    public object drawn { get; set; }
    public object committed { get; set; }
}
class Cards
{
    public int yellow { get; set; }
    public int yellowred { get; set; }
    public int red { get; set; }
}
class Penalty
{
    public object won { get; set; }
    public object commited { get; set; }
    public object scored { get; set; }
    public object missed { get; set; }
    public object saved { get; set; }
}
#endregion