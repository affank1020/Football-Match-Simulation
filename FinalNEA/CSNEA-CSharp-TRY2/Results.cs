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
    public partial class Results : Form
    {
        readonly FormControls controls = new FormControls();

        private Team homeTeam, awayTeam;
        private List<Player> homePlayers, awayPlayers;

        private TeamStats homeStats = new TeamStats(), awayStats = new TeamStats();
        private List<Control> curMenuControls = new List<Control>();
        private int btnWidth = 0;
        private string[] buttonNames = new string[] { "Match Facts", "Home Players", "Away Players", "Analysis" };

        private TeamAnalysis teamAnalysis = new TeamAnalysis();

        private int NumOfSimulations = 1000;

        private List<MatchEvent> matchEvents = new List<MatchEvent>();
        private List<PlayerStats> PlayerStatsAnalysis = new List<PlayerStats>();
        public Results(Team hT, Team aT, List<Player> hPlayers, List<Player> aPlayers)
        {
            InitializeComponent();
            homeTeam = hT;
            awayTeam = aT;
            homePlayers = hPlayers;
            awayPlayers = aPlayers;
        }

        private void Results_Load(object sender, EventArgs e)
        {
            //Get rid of the reserves
            homePlayers.RemoveAll(p => p.FormationPosition == "RES");
            awayPlayers.RemoveAll(p => p.FormationPosition == "RES");

            foreach (var item in homePlayers) {PlayerStatsAnalysis.Add(new PlayerStats() { PlayerObject = item });}
            foreach (var item in awayPlayers) {PlayerStatsAnalysis.Add(new PlayerStats() { PlayerObject = item });}

            List<TeamStats> teamStats = SimulateMatch();
            homeStats = teamStats[0];
            awayStats = teamStats[1];
            //Get the analysis objects
            List<TeamStats> list = new List<TeamStats>();
            List<string> scoreLines = new List<string>();
            double homeGoals = 0,awayGoals = 0;
            for (int i = 0; i < NumOfSimulations; i++)
            {
                list = SimulateMatch();
                Console.WriteLine(i.ToString() + "---------------------------------");
                homeStats = list[0];
                awayStats = list[1];
                if (homeStats.Goals > awayStats.Goals) {teamAnalysis.HomeTeamWin += 1;}
                if (homeStats.Goals == awayStats.Goals) {teamAnalysis.Draw += 1;}
                if (homeStats.Goals < awayStats.Goals) {teamAnalysis.AwayTeamWin += 1;}
                if (homeStats.Goals >= 3) {teamAnalysis.HomeTeamScoring3 += 1;}
                if (awayStats.Goals >= 3) {teamAnalysis.AwayTeamScoring3 += 1;}
                if (homeStats.Goals == 0) {teamAnalysis.AwayTeamCleanSheet += 1;}
                if (awayStats.Goals == 0) {teamAnalysis.HomeTeamCleanSheet += 1;}
                homeGoals += homeStats.Goals;
                awayGoals += awayStats.Goals;

                scoreLines.Add(homeStats.Goals + "-" + awayStats.Goals);

                //Get the player data
                UpdatePlayerAnalysisData(homeStats);
                UpdatePlayerAnalysisData(awayStats);
            }
            homeGoals /= NumOfSimulations;
            awayGoals /= NumOfSimulations;
            teamAnalysis.AverageScoreline = homeGoals + " - " + awayGoals;

            var query = scoreLines.GroupBy(x => x).Select(group => new { Scoreline = group.Key, Count = group.Count() }).OrderByDescending(x => x.Count).Take(3);
            string result = string.Join(", ", query.Where(x => x.Scoreline != null).Select(x => $"{x.Scoreline} ({((double)x.Count / NumOfSimulations) * 100}%)"));
            teamAnalysis.MostCommonScorelines = result;
            LoadMatchFacts();
            LoadMenuButtons();
        }
        private void UpdatePlayerAnalysisData(TeamStats teamStats)
        {
            foreach (var player in teamStats.PlayerStatsList)
            {
                PlayerStats playerStats = PlayerStatsAnalysis.Find(x => x.PlayerObject == player.PlayerObject);
                playerStats.Goals += player.Goals;
                playerStats.Assists += player.Assists;
                playerStats.Passes += player.Passes;
                playerStats.Shots += player.Shots;
                playerStats.ShotsOnTarget += player.ShotsOnTarget;
                playerStats.ChancesCreated += player.ChancesCreated;
                playerStats.Fouls += player.Fouls;
                playerStats.YellowCards += player.YellowCards;
                playerStats.RedCards += player.RedCards;

            }
        }

        private List<TeamStats> SimulateMatch()
        {
            List<PlayerStats> homePlayerStats = new List<PlayerStats>();
            List<PlayerStats> awayPlayerStats = new List<PlayerStats>();
            TeamStats homeStats = new TeamStats(), awayStats = new TeamStats();

            Random random = new Random();

            double subsProb = (double)random.Next(10, 90) / 100;
            int numOfSubs = InversePoissonBisection(3, subsProb);

            List<Player> playersSentOff = new List<Player>();
            //Create a list of stats objects for each player
            //Set red cards first as well because once a player is red carded, they cannot be substituted
            for (int i = 0; i < homePlayers.Count; i++) {homePlayerStats.Add(new PlayerStats { PlayerObject = homePlayers[i] });}
            for (int i = 0; i < awayPlayers.Count; i++) {awayPlayerStats.Add(new PlayerStats { PlayerObject = awayPlayers[i] });}

            homeStats.PlayerStatsList = homePlayerStats;
            awayStats.PlayerStatsList = awayPlayerStats;

            List<Player> homeStarters = UpdateRedCards(ref homePlayers, ref playersSentOff, ref homeStats, ref homePlayerStats, "Home", random);
            List<Player> awayStarters = UpdateRedCards(ref awayPlayers, ref playersSentOff, ref awayStats, ref awayPlayerStats, "Away", random);

            //Do red cards first then substitutions

            homeStarters = homeStarters.Union(SimulateSubstitutions(numOfSubs, random, homePlayers, "Home", homeStats)).ToList();
            awayStarters = awayStarters.Union(SimulateSubstitutions(numOfSubs, random, awayPlayers, "Away", awayStats)).ToList();

            #region Calculate relative strength of home starters against away starters
            //Total appearances
            //BUG for report: Average shots per game was not accurate - this program said 10. Was actually 16 from official stats website
            //To work around this problem, take an average of each players' appearance, which will be less than the highest number of appearances which means the average number of shots will balance out
            //double homeAppearances = int.Parse(homeStarters.Aggregate((agg, next) => int.Parse(next.Appearances) > int.Parse(agg.Appearances) ? next : agg).Appearances);
            //double awayAppearances = int.Parse(awayStarters.Aggregate((agg, next) => int.Parse(next.Appearances) > int.Parse(agg.Appearances) ? next : agg).Appearances);

            double homeAppearances = homeStarters.Average(x => int.Parse(x.Appearances));
            double awayAppearances = awayStarters.Average(x => int.Parse(x.Appearances));
            //Total goals, goals conceded, shots and other stats
            //Goals conceded are only recorded for the goalkeeper, so add an average goals conceded per game per goalkeeper

            double homeGoals = 0, homeGoalsConceded = 0, homeTotalShots = 0, homeShotsOnTarget = 0, homePasses = 0, homeKeyPasses = 0, homeTackles = 0, homeSaves = 0, homeFouls = 0;
            foreach (Player player in homeStarters)
            {

                homeGoals += int.Parse(player.Goals);
                homeTotalShots += int.Parse(player.Shots);
                homeShotsOnTarget += int.Parse(player.ShotsOnTarget);
                homePasses += int.Parse(player.Passes);
                homeKeyPasses += int.Parse(player.KeyPasses);
                homeTackles += int.Parse(player.Tackles);
                homeGoalsConceded += int.Parse(player.GoalsConceded);
                homeSaves += int.Parse(player.Saves);
                homeFouls += int.Parse(player.FoulsCommitted);
            }

            double awayGoals = 0, awayGoalsConceded = 0, awayTotalShots = 0, awayShotsOnTarget = 0, awayPasses = 0, awayKeyPasses = 0, awayTackles = 0, awaySaves = 0, awayFouls = 0;
            foreach (Player player in awayStarters)
            {
                awayGoals += int.Parse(player.Goals);
                awayTotalShots += int.Parse(player.Shots);
                awayShotsOnTarget += int.Parse(player.ShotsOnTarget);
                awayPasses += int.Parse(player.Passes);
                awayKeyPasses += int.Parse(player.KeyPasses);
                awayTackles += int.Parse(player.Tackles);
                awayGoalsConceded += int.Parse(player.GoalsConceded);
                awaySaves += int.Parse(player.Saves);
                awayFouls += int.Parse(player.FoulsCommitted);
            }

            Console.WriteLine("Goals: " + homeGoals + " - " + awayGoals);
            Console.WriteLine("Total shots: " + homeTotalShots + " - " + awayTotalShots);
            Console.WriteLine("Shots on target: " + homeShotsOnTarget + " - " + awayShotsOnTarget);
            Console.WriteLine("Passes: " + homePasses + " - " + awayPasses);
            Console.WriteLine("Key passes: " + homeKeyPasses + " - " + awayKeyPasses);
            Console.WriteLine("Tackles: " + homeTackles + " - " + awayTackles);
            Console.WriteLine("Goals conceded: " + homeGoalsConceded + " - " + awayGoalsConceded);
            Console.WriteLine("Saves: " + homeSaves + " - " + awaySaves);
            Console.WriteLine("Appearances: " + homeAppearances + " - " + awayAppearances);

            double homeCompositeScore = ((0.2 * homeGoals) - (0.2 * homeGoalsConceded) + (0.15 * homeTotalShots) + (0.1 * homeShotsOnTarget) + (0.05 * homeSaves) + (0.1 * homeKeyPasses) + (0.1 * homePasses) + (0.1 * homeTackles)) / homeAppearances;
            double awayCompositeScore = ((0.2 * awayGoals) - (0.2 * awayGoalsConceded) + (0.15 * awayTotalShots) + (0.1 * awayShotsOnTarget) + (0.05 * awaySaves) + (0.1 * awayKeyPasses) + (0.1 * awayPasses) + (0.1 * awayTackles)) / awayAppearances;

            Console.WriteLine("Composite score: " + homeCompositeScore + " - " + awayCompositeScore);

            double homeRelativeRating = (homeCompositeScore / (homeCompositeScore + awayCompositeScore)) / 0.5;
            double awayRelativeRating = (awayCompositeScore / (homeCompositeScore + awayCompositeScore)) / 0.5;
            Console.WriteLine(homeRelativeRating + " VS " + awayRelativeRating);

            double homeAttackScore = (0.5 * homeGoals) + (0.25 * homeTotalShots) + (0.25 * homeShotsOnTarget);
            double awayAttackScore = (0.5 * awayGoals) + (0.25 * awayTotalShots) + (0.25 * awayShotsOnTarget);
            double homeAttackRating = (homeAttackScore / (homeAttackScore + awayAttackScore)) / 0.5;
            double awayAttackRating = (awayAttackScore / (homeAttackScore + awayAttackScore)) / 0.5;

            Console.WriteLine("Attack score: " + homeAttackScore + " - " + awayAttackScore);
            Console.WriteLine("Attack rating: " + homeAttackRating + " - " + awayAttackRating);

            double homeDefenceScore = (0.2 * homeTackles) - (0.8 * homeGoalsConceded);
            double awayDefenceScore = (0.2 * awayTackles) - (0.8 * awayGoalsConceded);
            double homeDefenceRating = (homeDefenceScore / (homeDefenceScore + awayDefenceScore)) / 0.5;
            double awayDefenceRating = (awayDefenceScore / (homeDefenceScore + awayDefenceScore)) / 0.5;

            Console.WriteLine("Defence score: " + homeDefenceScore + " - " + awayDefenceScore);
            Console.WriteLine("Defence rating: " + homeDefenceRating + " - " + awayDefenceRating);
            #endregion

            homeRelativeRating *= homeAttackRating * homeDefenceRating;
            awayRelativeRating *= awayAttackRating * awayDefenceRating;

            int avrgShots = (int)Math.Round((homeTotalShots / homeAppearances) * homeRelativeRating);
            var totalShots = InversePoissonBisection(avrgShots, random.NextDouble());
            Console.WriteLine("Home shots: " + avrgShots);
            homeStats.Shots = totalShots;

            avrgShots = (int)Math.Round((awayTotalShots / awayAppearances) * awayRelativeRating);
            totalShots = InversePoissonBisection(avrgShots, random.NextDouble());
            Console.WriteLine("Away shots: " + avrgShots);
            awayStats.Shots = totalShots;

            int avrgFouls = (int)Math.Round((homeFouls / homeAppearances) * awayRelativeRating); //Relative rating is swapped because less fouls is better
            homeStats.Fouls = InversePoissonBisection(avrgFouls, random.NextDouble());

            avrgFouls = (int)Math.Round((awayFouls / awayAppearances) * homeRelativeRating);
            awayStats.Fouls = InversePoissonBisection(avrgFouls, random.NextDouble());

            int homeAvrgPasses = GetPasses(homeStarters, random, homeRelativeRating, ref homeStats, ref homePlayerStats);
            int awayAvrgPasses = GetPasses(awayStarters, random, awayRelativeRating, ref awayStats, ref awayPlayerStats);

            homeStats.Possession = (int)Math.Round((double)homeAvrgPasses / (homeAvrgPasses + awayAvrgPasses) * 100);
            awayStats.Possession = (int)Math.Round((double)awayAvrgPasses / (homeAvrgPasses + awayAvrgPasses) * 100);

            //Sometimes reaches over 70
            if (homeStats.Possession > 70)
            {
                homeStats.Possession = random.Next(60, 70);
                awayStats.Possession = 50 - (homeStats.Possession - 50);
            }

            UpdateFouls(homeStarters, (int)homeFouls, "Home", random, ref homeStats, homePlayerStats);
            UpdateFouls(awayStarters, (int)awayFouls, "Away", random, ref awayStats, awayPlayerStats);

            UpdateShotsAndGoals(homeStarters, random, homeTotalShots, homeKeyPasses, "Home", ref homeStats, ref homePlayerStats);
            UpdateShotsAndGoals(awayStarters, random, awayTotalShots, awayKeyPasses, "Away", ref awayStats, ref awayPlayerStats);

            homeStats.PlayerStatsList = homePlayerStats;
            awayStats.PlayerStatsList = awayPlayerStats;

            return new List<TeamStats>() { homeStats, awayStats };
        }
        private List<Player> UpdateRedCards(ref List<Player> players, ref List<Player> playersSentOff, ref TeamStats teamStats, ref List<PlayerStats> playerStats, string homeOrAway, Random random)
        {
            List<Player> starters = new List<Player>();
            for (int i = 0; i < players.Count; i++)
            {
                if (players[i].FormationPosition.StartsWith("S") == false)
                {
                    //It is a starter
                    Player player = players[i];
                    double rcProb = ((double.Parse(player.RedCards) / double.Parse(player.Appearances)) + random.NextDouble()) / 50;
                    if (random.NextDouble() < rcProb) {AddRedCardEvent(new RedCard() { PlayerObject = player, HomeOrAway = homeOrAway, Minute = random.Next(1, 91).ToString() }, teamStats, playerStats);} 
                    else {starters.Add(players[i]);}
                }
            }
            return starters;
        }
        private List<Player> SimulateSubstitutions(int numOfSubs, Random random, List<Player> players, string homeOrAway, TeamStats teamStats)
        {
            List<Player> usedPlayers = new List<Player>();
            List<Player> usedSubs = new List<Player>();
            for (int i = 0; i < numOfSubs; i++)
            {
                int minute = GetSubstituteMinutes(random.NextDouble());
                Player comingOn = new Player(), comingOff = new Player();

                int subType = random.Next(1, 100);
                if (subType == 1)
                {
                    //Low chance of keeper sub
                    //Try to get a list of all keepers on the bench
                    List<Player> keepers = players.FindAll(p => p.Position == "GK");
                    if (keepers.Count > 0)
                    {
                        comingOn = keepers[random.Next(0, keepers.Count)];
                        comingOff = players[0];
                        usedPlayers.Add(comingOn);
                        usedPlayers.Add(comingOff);
                        usedSubs.Add(comingOn);
                    }
                }
                else
                {
                    comingOn = players[random.Next(11, 18)];
                    while (usedPlayers.Contains(comingOn) || comingOn.Position == "GK")
                    {
                        comingOn = players[random.Next(11, 18)];
                    }
                    usedPlayers.Add(comingOn);
                    usedSubs.Add(comingOn);

                    comingOff = players[random.Next(1, 10)];
                    while (usedPlayers.Contains(comingOff) || comingOff.Position == "GK")
                    {
                        comingOff = players[random.Next(0, 10)];
                    }
                    usedPlayers.Add(comingOff);
                }
                AddSubstitutionEvent(new Substitution { Minute = minute.ToString(), HomeOrAway = homeOrAway, ComingOff = comingOff, ComingOn = comingOn }, teamStats);
            }
            return usedSubs;
        }
        private int GetPasses(List<Player> starters, Random random, double relativeRating, ref TeamStats teamStats, ref List<PlayerStats> playerStatsList)
        {
            int avrgPasses = 0;
            foreach (Player player in starters)
            {
                PlayerStats playerStats = playerStatsList.Find(p => p.PlayerObject == player);
                if (int.Parse(player.Appearances) == 0)
                {
                    if (player.FormationPosition == "GK") {playerStats.Passes = InversePoissonBisection(15 * relativeRating, random.NextDouble());}
                    else {playerStats.Passes = InversePoissonBisection(30 * relativeRating, random.NextDouble());}
                }
                else {playerStats.Passes = InversePoissonBisection(int.Parse(player.Passes) / int.Parse(player.Appearances), random.NextDouble());}

                avrgPasses += playerStats.Passes;
            }
            return avrgPasses;
        }
        private void UpdateShotsAndGoals(List<Player> starters, Random random, double totalShots, double keyPasses, string homeOrAway, ref TeamStats teamStats, ref List<PlayerStats> playerStatsList)
        {
            int cumulativeShots = 0;
            for (int i = 0; i < teamStats.Shots; i++)
            {
                int rnd = random.Next(1, (int)totalShots + 1);
                double firstProb = 0, secondProb = 0, thirdProb = 0;
                PlayerStats playerStats = new PlayerStats();
                foreach (Player player in starters)
                {
                    playerStats = playerStatsList.Find(p => p.PlayerObject == player);
                    if (int.Parse(player.Shots) == 0)
                    {
                        //If the player has 0 shots, there is still a small chance he can get the shot, the chance depends on the position
                        double pr = random.NextDouble();
                        switch (player.FormationPosition)
                        {
                            case "GK":
                                firstProb = 0.0001; //Probability of a shot
                                secondProb = 0.01; //Probability of that shot being on target
                                thirdProb = 0.05; //Probability of that shot on target being a goal
                                break;
                            case "DF":
                                firstProb = 0.01;
                                secondProb = 0.05;
                                thirdProb = 0.1;
                                break;
                            case "MF":
                                firstProb = 0.05;
                                secondProb = 0.1;
                                thirdProb = 0.15;
                                break;
                            case "FW":
                                firstProb = 0.1;
                                secondProb = 0.15;
                                thirdProb = 0.2;
                                break;
                        }
                        if (pr < firstProb)
                        {
                            Chance chance = GetChanceCreated(starters, player, (int)keyPasses, homeOrAway, random, playerStatsList);
                            playerStats.Shots += 1;
                            //Every shot has a chance created
                            if (random.NextDouble() < secondProb)
                            {
                                playerStats.ShotsOnTarget += 1;
                                teamStats.ShotsOnTarget += 1;
                                if (random.NextDouble() < thirdProb)
                                {
                                    //int min = GetEventMinute(player, playerStatsList, random);
                                    AddGoalEvent(new Goal() { Scorer = player, Assister = chance.Creator, HomeOrAway = homeOrAway, Minute = chance.Minute }, chance, teamStats);
                                }
                            }
                            break;
                        }
                    }
                    else
                    {
                        cumulativeShots += int.Parse(player.Shots);
                        if (rnd <= cumulativeShots)
                        {
                            Chance chance = GetChanceCreated(starters, player, (int)keyPasses, homeOrAway, random, playerStatsList);
                            playerStats.Shots += 1;
                            double sotProb = ((double.Parse(player.ShotsOnTarget) / double.Parse(player.Shots)) + random.NextDouble()) / 2.5;
                            if (random.NextDouble() <= sotProb)
                            {
                                playerStats.ShotsOnTarget += 1;
                                teamStats.ShotsOnTarget += 1;
                                double goalProb = ((double.Parse(player.Goals) / double.Parse(player.ShotsOnTarget)) + random.NextDouble()) / 2.5;
                                if (random.NextDouble() < goalProb)
                                {
                                    AddGoalEvent(new Goal() { Scorer = player, Assister = chance.Creator, HomeOrAway = homeOrAway, Minute = chance.Minute }, chance, teamStats);
                                }
                            }
                            cumulativeShots = 0;
                            break;
                        }
                    }
                }
            }
        }
        private Chance GetChanceCreated(List<Player> starters, Player shotTaker, int keyPasses, string HomeOrAway, Random random, List<PlayerStats> playerStatsList)
        {
            int cumulativeShots = 0;
            int rnd = random.Next(1, keyPasses + 1);
            Player playerToReturn = new Player();
            foreach (Player player in starters)
            {
                if (player == shotTaker)
                {
                }
                else
                {

                    PlayerStats playerStats = playerStatsList.Find(p => p.PlayerObject == player);
                    if (int.Parse(player.KeyPasses) == 0)
                    {
                        //If the player has 0 key passes, there is still a small chance he can get the key pass
                        double firstProb = 0;
                        switch (player.FormationPosition)
                        {
                            case "GK":
                                firstProb = 0.0001; //Probability of creaing a chance
                                break;
                            case "DF":
                                firstProb = 0.05;
                                break;
                            case "MF":
                                firstProb = 0.1;
                                break;
                            case "FW":
                                firstProb = 0.1;
                                break;
                        }
                        double pr = random.NextDouble();
                        if (pr < firstProb)
                        {
                            playerStats.ChancesCreated += 1;
                            playerToReturn = player;
                            break;
                        }
                    }
                    else
                    {
                        cumulativeShots += int.Parse(player.KeyPasses);
                        if (rnd <= cumulativeShots)
                        {
                            playerStats.ChancesCreated += 1;
                            cumulativeShots = 0;
                            playerToReturn = player;
                            break;
                        }
                    }
                }
            }

            if (playerToReturn.Name == "")
            {
                playerToReturn = starters[random.Next(starters.Count)];
            }

            int min = GetEventMinute(playerToReturn, playerStatsList, random);
            //Check if shot taker is on the pitch at that minute;
            PlayerStats shotTakerStats = playerStatsList.Find(p => p.PlayerObject == shotTaker);
            List<Substitution> playerSub = shotTakerStats.playerSpecificEvents.OfType<Substitution>().ToList();

            PlayerStats creatorStats = playerStatsList.Find(p => p.PlayerObject == playerToReturn);
            List<Substitution> creatorSub = creatorStats.playerSpecificEvents.OfType<Substitution>().ToList();

            if (playerSub.Count > 0)
            {
                Substitution sub = playerSub[0]; //Each player can only have 1 substitution, if there is a sub, the player is either coming on or off
                if (sub.ComingOff == shotTaker)
                {
                    if (min > int.Parse(sub.Minute)) //If the player is coming off, then minute should be before sub.minute
                    {
                        min = GetEventMinute(playerToReturn, playerStatsList, random);
                    }
                }
                else if (sub.ComingOn == shotTaker)
                {
                    if (min < int.Parse(sub.Minute)) //If the player is coming on, then minute should be after sub.minute
                    {
                        min = GetEventMinute(playerToReturn, playerStatsList, random);
                    }
                }
            }

            if (creatorSub.Count > 0)
            {
                Substitution sub = creatorSub[0]; //Each player can only have 1 substitution, if there is a sub, the player is either coming on or off
                if (sub.ComingOff == playerToReturn)
                {
                    if (min > int.Parse(sub.Minute)) //If the player is coming off, then minute should be before sub.minute
                    {
                        min = GetEventMinute(playerToReturn, playerStatsList, random);
                    }
                }
                else if (sub.ComingOn == playerToReturn)
                {
                    if (min < int.Parse(sub.Minute)) //If the player is coming on, then minute should be after sub.minute
                    {
                        min = GetEventMinute(playerToReturn, playerStatsList, random);
                    }
                }
            }

            Chance chance = new Chance() { Creator = playerToReturn, Shooter = shotTaker, HomeOrAway = HomeOrAway, Minute = min.ToString() };
            return chance;
        }
        private void UpdateFouls(List<Player> starters, int totalFouls, string homeOrAway, Random random, ref TeamStats teamStats, List<PlayerStats> playerStatsList)
        {
            int cumulativeFouls = 0;
            for (int i = 0; i < teamStats.Fouls; i++)
            {
                int rnd = random.Next(1, totalFouls + 1);
                double firstProb = 0, secondProb = 0;
                PlayerStats playerStats = new PlayerStats();
                foreach (Player player in starters)
                {
                    playerStats = playerStatsList.Find(p => p.PlayerObject == player);
                    if (int.Parse(player.FoulsCommitted) == 0)
                    {
                        //If the player has 0 shots, there is still a small chance he can get the shot, the chance depends on the position
                        double pr = random.NextDouble();
                        firstProb = 0.05; //Probability of a foul
                        secondProb = 0.05; //Probability of that foul being a yellow
                        if (pr < firstProb)
                        {
                            playerStats.Fouls += 1;
                            //Every shot has a chance created
                            if (random.NextDouble() < secondProb && playerStats.playerSpecificEvents.OfType<YellowCard>().ToList().Count == 0)
                            {
                                AddYellowCardEvent(new YellowCard() { PlayerObject = player, HomeOrAway = homeOrAway, Minute = GetEventMinute(player, playerStatsList, random).ToString() }, teamStats);
                            }
                            break;
                        }
                    }
                    else
                    {
                        cumulativeFouls += int.Parse(player.FoulsCommitted);
                        if (rnd <= cumulativeFouls)
                        {
                            playerStats.Fouls += 1;
                            double ycProb = ((double.Parse(player.YellowCards) / double.Parse(player.Appearances)) + random.NextDouble()) / 3;
                            if (random.NextDouble() < ycProb && playerStats.playerSpecificEvents.OfType<YellowCard>().ToList().Count == 0)
                            {
                                AddYellowCardEvent(new YellowCard() { PlayerObject = player, HomeOrAway = homeOrAway, Minute = GetEventMinute(player, playerStatsList, random).ToString() }, teamStats);
                            }
                            cumulativeFouls = 0;
                            break;
                        }
                    }
                }
            }
        }
        private int GetEventMinute(Player player, List<PlayerStats> playerStatsList, Random random)
        {
            PlayerStats playerStats = playerStatsList.Find(p => p.PlayerObject == player);
            List<Substitution> playerSub = playerStats.playerSpecificEvents.OfType<Substitution>().ToList();

            //If the player has a red card already
            int min = 0;
            if (playerSub.Count > 0) //The player is a sub
            {
                Substitution sub = playerSub[0]; //Each player can only have 1 substitution, if there is a sub, the player is either coming on or off
                if (sub.ComingOff == player)
                {
                    min = random.Next(1, int.Parse(sub.Minute) + 1);//If the player is coming off, then take maximum at the minute of substitution 
                }
                else if (sub.ComingOn == player)
                {
                    min = random.Next(int.Parse(sub.Minute), 91);//If the player is coming on, events start after the player comes on 
                }
            }
            else
            {
                min = random.Next(1, 91);
            }

            return min;
        }

        public void LoadMatchFacts()
        {
            #region Create the home team name label
            //Label for the name of the league on the home team
            Label hLabel = controls.CreateLabel(Controls, "Hteam", homeTeam.TeamName, 100, 250, 20, 100);
            hLabel.AutoSize = true;
            if (homeTeam.TeamName.Split(" ".ToCharArray()).Length > 1)
            {
                hLabel.MaximumSize = new Size(200, 100);
            }
            hLabel.TextAlign = ContentAlignment.MiddleCenter;
            hLabel.Font = new Font("Microsoft Sans Serif", 20);
            hLabel.ForeColor = Color.White;
            #endregion

            #region Create the 2 dividers on the outer side of the table
            PictureBox hDivider = controls.CreatePictureBox(Controls, "hDivider", 320, 5, (int)(hLabel.Location.X + hLabel.Width), 270);
            hDivider.BackColor = Color.White;

            PictureBox aDivider = controls.CreatePictureBox(Controls, "aDivider", 320, 5, hDivider.Location.X + hDivider.Width + 300, 270);
            aDivider.BackColor = Color.White;
            #endregion

            #region Create the away team name label
            //Label for the name of the league on the away team
            Label aLabel = controls.CreateLabel(Controls, "Ateam", awayTeam.TeamName, 300, 250, 10, 100);
            aLabel.AutoSize = true;
            if (awayTeam.TeamName.Split(" ".ToCharArray()).Length > 1)
            {
                aLabel.MaximumSize = new Size(200, 100);
            }
            aLabel.TextAlign = ContentAlignment.MiddleCenter;
            aLabel.Location = new Point((int)(aDivider.Location.X + aDivider.Width), aLabel.Location.Y);
            aLabel.Font = new Font("Microsoft Sans Serif", 20);
            aLabel.ForeColor = Color.White;
            #endregion

            btnWidth = (aLabel.Location.X + aLabel.Width - hLabel.Location.X - (5*buttonNames.Length)) / buttonNames.Length;

            //Set the window length to be just above the bottom
            int startButtons = hDivider.Location.Y + hDivider.Height + 10;
            this.Height = startButtons;

            //Middle calculated based on the separation between the team names
            var middle = ((aDivider.Location.X - (hDivider.Location.X + hDivider.Width)) / 2) + hDivider.Location.X + hDivider.Width; 
            
            #region Create the label showing the scoreline
            Label Goals = controls.CreateLabel(Controls, "Goals", homeStats.Goals + " : " + awayStats.Goals, 30, 200, middle, 100);
            Goals.ForeColor = Color.White;
            Goals.AutoSize = true;
            Goals.Location = new Point(middle - Goals.Width, Goals.Location.Y);
            Goals.TextAlign = ContentAlignment.MiddleCenter;
            Goals.Font = new Font("Microsoft Sans Serif", 20);
            #endregion

            //Recalculate middle based on the position of the scoreline. When this is used, need to subtract the controls' width as well
            middle = (Goals.Location.X + (Goals.Width / 2)); 

            //Add the names of the goalscorers underneath the scoreline
            #region Add a small football icon using Unicode characters
            char football = '\u26bd';
            Label GoalIcon = controls.CreateLabel(Controls, "GoalIcon", football.ToString(), 50, 50, middle, Goals.Location.Y + 75);
            GoalIcon.AutoSize = true;
            GoalIcon.ForeColor = Color.White;
            GoalIcon.Location = new Point(middle - GoalIcon.Width + 5, GoalIcon.Location.Y);
            GoalIcon.Font = new Font("Microsoft Sans Serif", 15);
            #endregion

            #region Create the label for home team goalscorers
            string homeScorers = "";
            //Get the names of all the goalscorers in the home team
            List<Goal> homeGoalEvents = homeStats.TeamEvents.OfType<Goal>().ToList();
            homeGoalEvents = homeGoalEvents.OrderBy(g => g.Minute).ToList();
            for (int i = 0; i < homeGoalEvents.Count; i++)
            {
                homeScorers += "(" + homeGoalEvents[i].Minute + "') " + homeGoalEvents[i].Scorer.Name + "\n";
            }
            Label homeGoalScorers = controls.CreateLabel(Controls, "homeGoalScorers", homeScorers, 50, 50, 50, Goals.Location.Y + 75);
            homeGoalScorers.AutoSize = true;
            homeGoalScorers.ForeColor = Color.White;
            homeGoalScorers.Location = new Point(middle - homeGoalScorers.Width - homeGoalScorers.Width + 15, homeGoalScorers.Location.Y);
            homeGoalScorers.Font = new Font("Microsoft Sans Serif", 13);
            homeGoalScorers.TextAlign = ContentAlignment.MiddleRight;
            #endregion

            #region Create the label for away team goalscorers
            string awayScorers = "";
            List<Goal> awayGoalEvents = awayStats.TeamEvents.OfType<Goal>().ToList();
            awayGoalEvents = awayGoalEvents.OrderBy(g => g.Minute).ToList();
            for (int i = 0; i < awayGoalEvents.Count; i++)
            {
                awayScorers +=  awayGoalEvents[i].Scorer.Name + " (" + awayGoalEvents[i].Minute + "')\n";
            }
            Label awayGoalScorers = controls.CreateLabel(Controls, "awayGoalScorers", awayScorers, 50, 50, 50, Goals.Location.Y + 75);
            awayGoalScorers.AutoSize = true;
            awayGoalScorers.ForeColor = Color.White;
            awayGoalScorers.Location = new Point(middle + 15, awayGoalScorers.Location.Y);
            awayGoalScorers.Font = new Font("Microsoft Sans Serif", 13);
            awayGoalScorers.TextAlign = ContentAlignment.MiddleLeft;
            #endregion

            #region Add the inner dividers and the top of the table
            //Divider on the block of stats
            PictureBox dividerTop = controls.CreatePictureBox(Controls, "dividerTop", 5, 410, hDivider.Location.X - 50, 200);
            dividerTop.BackColor = Color.White;

            PictureBox addDividerLeft = controls.CreatePictureBox(Controls, "addDividerLeft", 320, 5, hDivider.Location.X - 50, 200);
            addDividerLeft.BackColor = Color.White;
            PictureBox addDividerRight = controls.CreatePictureBox(Controls, "addDividerRight", 320, 5, aDivider.Location.X + 50, 200);
            addDividerRight.BackColor = Color.White;
            #endregion

            string[] stats = new string[] { "Shots", "Shots On Target", "Possession", "Fouls", "Yellow Cards", "Red Cards" };
            
            //Resize the table columns based on how many stats are in the table
            addDividerLeft.Height = (stats.Length + 1) * 30;
            addDividerRight.Height = (stats.Length + 1) * 30;
            hDivider.Height = (stats.Length + 1) * 30;
            aDivider.Height = (stats.Length + 1) * 30;

            //Shift the table down based on where the names of the goalscorers ends
            int startTable = 300;
            if (homeScorers.Length > awayScorers.Length)
            {
                startTable = homeGoalScorers.Location.Y + homeGoalScorers.Height + 25;
            }
            else
            {
                startTable = awayGoalScorers.Location.Y + awayGoalScorers.Height + 25;
            }
            dividerTop.Location = new Point(dividerTop.Location.X, startTable);
            hDivider.Location = new Point(hDivider.Location.X, startTable);
            aDivider.Location = new Point(aDivider.Location.X, startTable);
            addDividerLeft.Location = new Point(addDividerLeft.Location.X, startTable);
            addDividerRight.Location = new Point(addDividerRight.Location.X, startTable);

            this.Width = aLabel.Location.X + aLabel.Width + 40;

            //Fill out all the names of the statistics in the table
            for (int i = 0; i < stats.Length; i++)
            {
                Label label = controls.CreateLabel(Controls, stats[i] + "_Name", stats[i], 30, 200, middle, startTable + 10 + (i * 35));
                label.ForeColor = Color.White;
                label.TextAlign = ContentAlignment.MiddleCenter;
                label.AutoSize = true;
                label.Location = new Point((Goals.Location.X + (Goals.Width/2)) - label.Width, label.Location.Y);
                label.Font = new Font("Microsoft Sans Serif", 15);
                curMenuControls.Add(label);

                //Add a divider between rows
                PictureBox divider = controls.CreatePictureBox(Controls, "divider" + i, 5, 410, hDivider.Location.X - 50, label.Location.Y + 27);
                divider.BackColor = Color.White;
                curMenuControls.Add(divider);
            }

            //Fill home statistics
            for (int i = 0; i <  stats.Length; i++)
            {
                Label label = controls.CreateLabel(Controls, stats[i] + "_Home_Value", GetStatValue(stats[i], homeStats).ToString(), 27, 50, hDivider.Location.X, startTable + 10 + (i * 35));
                label.ForeColor = Color.White;
                label.TextAlign = ContentAlignment.MiddleRight;
                label.AutoSize = true;
                label.Font = new Font("Microsoft Sans Serif", 15);
                label.Location = new Point(hDivider.Location.X - label.Width, label.Location.Y);
                curMenuControls.Add(label);
            }

            //Fill all away statistics
            for (int i = 0; i < stats.Length; i++)
            {

                Label label = controls.CreateLabel(Controls, stats[i] + "_Away_Value", GetStatValue(stats[i], awayStats).ToString(), 27, 50, aDivider.Location.X + 5, startTable + 10 + (i * 35));
                label.ForeColor = Color.White;
                label.TextAlign = ContentAlignment.MiddleLeft;
                label.AutoSize = true;
                label.Font = new Font("Microsoft Sans Serif", 15);
                curMenuControls.Add(label);
            }


            Button homeButton = controls.CreateButton(Controls, "homeButton", "Home", 40, 80, middle - 40, startTable + 20 + (stats.Length *35));
            homeButton.ForeColor = Color.White;
            homeButton.Font = new Font("Microsoft Sans Serif", 15);
            homeButton.Click += HomeButton_Click;

            //Add all the controls to the list of controls so we can remove them later if needed
            curMenuControls.AddRange(new List<Control> { hLabel, aLabel, Goals, hDivider, aDivider, addDividerLeft, addDividerRight, awayGoalScorers, homeGoalScorers, GoalIcon, dividerTop, homeButton });
        }

        private void HomeButton_Click(object sender, EventArgs e)
        {
            Form SelectTeams = new SelectTeams();
            SelectTeams.Show();
            this.Hide();
        }

        public void LoadPlayers(string teamName, ref List<Player> players, ref List<PlayerStats> playerStats)
        {
            //\u26BD
            #region Create the team name label
            //Label for the name of the league on the home team
            Label teamLabel = controls.CreateLabel(Controls, "teamLabel", teamName, 100, 250, 20, 100);
            teamLabel.AutoSize = true;
            teamLabel.TextAlign = ContentAlignment.MiddleLeft;
            teamLabel.Font = new Font("Microsoft Sans Serif", 20);
            teamLabel.ForeColor = Color.White;
            curMenuControls.Add(teamLabel);
            #endregion
            char football = '\u26BD';
            string boot = char.ConvertFromUtf32(128095);
            string yc = "YC"; string rc = "RC";
            int bottomHeightStarters = 0;
            #region Add Starters
            for (int i = 0; i < 11; i++)
            {
                Label playerLabel = controls.CreateLabel(Controls, "player" + i.ToString(), "(" + players[i].FormationPosition + ") " + players[i].Name, 20, 600, 20, 150 + (i*20));
                playerLabel.Font = new Font("Microsoft Sans Serif", 12);
                playerLabel.ForeColor = Color.White;
                curMenuControls.Add(playerLabel);
                playerLabel.TextAlign = ContentAlignment.MiddleLeft;
                bottomHeightStarters = (i * 20);

                PlayerStats curPlayerStats = playerStats[i];

                Button playerInfo = controls.CreateButton(Controls, "info" + i.ToString(), "O", 16, 16, 3, 154 + (i * 20));
                playerInfo.FlatStyle = FlatStyle.Flat;
                playerInfo.ForeColor = Color.White;
                playerInfo.Font = new Font("Microsoft Sans Serif", 5);
                playerInfo.Padding = new Padding(0, 0, 0, 0);
                curMenuControls.Add(playerInfo);
                playerInfo.Click += (sender, EventArgs) => { PlayerInfo_Click(sender, EventArgs, curPlayerStats); };

                #region Add the goals to each player in the lineup
                List<Goal> goalEvents = curPlayerStats.playerSpecificEvents.OfType<Goal>().ToList();
                goalEvents = goalEvents.OrderBy(g => g.Minute).ToList();
                //Add goals or assists
                if (goalEvents.Count == 1)
                {
                    playerLabel.Text += " " + football.ToString() + " (" + goalEvents[0].Minute + "')";
                }
                else if (goalEvents.Count > 1)
                {
                    playerLabel.Text += " " + football.ToString() + " (";
                    for (int j = 0; j < goalEvents.Count; j++)
                    {
                        playerLabel.Text += goalEvents[j].Minute + "', ";
                    }
                    playerLabel.Text = playerLabel.Text.Substring(0, playerLabel.Text.Length - 2) + ")";
                }
                #endregion

                #region Add the assists to each player in the lineup
                List<Assist> assistEvents = curPlayerStats.playerSpecificEvents.OfType<Assist>().ToList();
                assistEvents = assistEvents.OrderBy(g => g.Minute).ToList();
                //Add goals or assists
                if (assistEvents.Count == 1)
                {
                    playerLabel.Text += " " + boot + " (" + assistEvents[0].Minute + "')";
                }
                else if (assistEvents.Count > 1)
                {
                    playerLabel.Text += " " + boot + " (";
                    for (int j = 0; j < assistEvents.Count; j++)
                    {
                        playerLabel.Text += assistEvents[j].Minute + "', ";
                    }
                    playerLabel.Text = playerLabel.Text.Substring(0, playerLabel.Text.Length - 2) + ")";
                }
                #endregion

                #region Add the yellow cards to each player in the lineup
                List<YellowCard> ycEvents = curPlayerStats.playerSpecificEvents.OfType<YellowCard>().ToList();
                ycEvents = ycEvents.OrderBy(g => g.Minute).ToList();
                if (ycEvents.Count == 1)
                {
                    playerLabel.ForeColor = Color.Yellow;
                    playerLabel.Text += " " + yc + " (" + ycEvents[0].Minute + "')";
                }
                
                #endregion

                #region Add the substitutions to each player that came off in the lineup
                List<Substitution> subEvents = curPlayerStats.playerSpecificEvents.OfType<Substitution>().ToList();
                subEvents = subEvents.OrderBy(g => g.Minute).ToList();
                if (subEvents.Count == 1)
                {
                    playerLabel.Text += " " + '\u21C4' + " (" + subEvents[0].ComingOn.Name + " " + subEvents[0].Minute + "')";
                    playerLabel.ForeColor = Color.FromArgb(180, 180, 180);
                }

                #endregion

                #region Add the red cards to each player in the lineup
                if (ycEvents.Count > 1)
                {
                    playerLabel.ForeColor = Color.OrangeRed;
                    playerLabel.Text += " " + yc + " (";
                    for (int j = 0; j < ycEvents.Count; j++)
                    {
                        playerLabel.Text += ycEvents[j].Minute + "', ";
                    }
                    playerLabel.Text = playerLabel.Text.Substring(0, playerLabel.Text.Length - 2) + ")";
                }

                List<RedCard> rcEvents = curPlayerStats.playerSpecificEvents.OfType<RedCard>().ToList();
                rcEvents = rcEvents.OrderBy(g => g.Minute).ToList();
                if (rcEvents.Count == 1)
                {
                    playerLabel.Text += " " + rc + " (" + rcEvents[0].Minute + "')";
                    playerLabel.ForeColor = Color.OrangeRed;
                }
                #endregion
            }
            #endregion

            #region Add Substitutes
            for (int i = 11; i < 18; i++)
            {
                Label playerLabel = controls.CreateLabel(Controls, "player" + i.ToString(), "(" + players[i].FormationPosition + ") " + players[i].Name, 20, 600, 20, bottomHeightStarters + (i * 20));
                playerLabel.Font = new Font("Microsoft Sans Serif", 12);
                playerLabel.ForeColor = Color.FromArgb(180, 180, 180);
                curMenuControls.Add(playerLabel);
                playerLabel.TextAlign = ContentAlignment.MiddleLeft;

                PlayerStats curPlayerStats = playerStats[i];

                Button playerInfo = controls.CreateButton(Controls, "info" + i.ToString(), "O", 16, 16, 3, bottomHeightStarters + (i * 20));
                playerInfo.FlatStyle = FlatStyle.Flat;
                playerInfo.ForeColor = Color.White;
                playerInfo.Font = new Font("Microsoft Sans Serif", 5);
                playerInfo.Padding = new Padding(0, 0, 0, 0);
                curMenuControls.Add(playerInfo);
                playerInfo.Click += (sender, EventArgs) => { PlayerInfo_Click(sender, EventArgs, curPlayerStats); };

                #region Add the goals to each player in the lineup
                List<Goal> goalEvents = curPlayerStats.playerSpecificEvents.OfType<Goal>().ToList();
                goalEvents = goalEvents.OrderBy(g => g.Minute).ToList();
                //Add goals or assists
                if (goalEvents.Count == 1)
                {
                    playerLabel.Text += " " + football.ToString() + " (" + goalEvents[0].Minute + "')";
                }
                else if (goalEvents.Count > 1)
                {
                    playerLabel.Text += " " + football.ToString() + " (";
                    for (int j = 0; j < goalEvents.Count; j++)
                    {
                        playerLabel.Text += goalEvents[j].Minute + "', ";
                    }
                    playerLabel.Text = playerLabel.Text.Substring(0, playerLabel.Text.Length - 2) + ")";
                }
                #endregion

                #region Add the assists to each player in the lineup
                List<Assist> assistEvents = curPlayerStats.playerSpecificEvents.OfType<Assist>().ToList();
                assistEvents = assistEvents.OrderBy(g => g.Minute).ToList();
                //Add goals or assists
                if (assistEvents.Count == 1)
                {
                    playerLabel.Text += " " + boot + " (" + assistEvents[0].Minute + "')";
                }
                else if (assistEvents.Count > 1)
                {
                    playerLabel.Text += " " + boot + " (";
                    for (int j = 0; j < assistEvents.Count; j++)
                    {
                        playerLabel.Text += assistEvents[j].Minute + "', ";
                    }
                    playerLabel.Text = playerLabel.Text.Substring(0, playerLabel.Text.Length - 2) + ")";
                }
                #endregion

                #region Add the yellow cards to each player in the lineup
                List<YellowCard> ycEvents = curPlayerStats.playerSpecificEvents.OfType<YellowCard>().ToList();
                ycEvents = ycEvents.OrderBy(g => g.Minute).ToList();
                if (ycEvents.Count == 1)
                {
                    playerLabel.ForeColor = Color.Yellow;
                    playerLabel.Text += " " + yc + " (" + ycEvents[0].Minute + "')";
                }

                #endregion

                #region Add the substitutions to each player that came on in the lineup
                List<Substitution> subEvents = curPlayerStats.playerSpecificEvents.OfType<Substitution>().ToList();
                subEvents = subEvents.OrderBy(g => g.Minute).ToList();
                if (subEvents.Count == 1)
                {
                    playerLabel.Text += " " + '\u21C4' + " (" + subEvents[0].ComingOff.Name + " " + subEvents[0].Minute + "')";
                    playerLabel.ForeColor = Color.White;

                }
                #endregion

                #region Add the red cards to each player in the lineup
                if (ycEvents.Count > 1)
                {
                    playerLabel.ForeColor = Color.OrangeRed;
                    playerLabel.Text += " " + yc + " (";
                    for (int j = 0; j < ycEvents.Count; j++)
                    {
                        playerLabel.Text += ycEvents[j].Minute + "', ";
                    }
                    playerLabel.Text = playerLabel.Text.Substring(0, playerLabel.Text.Length - 2) + ")";
                }

                List<RedCard> rcEvents = curPlayerStats.playerSpecificEvents.OfType<RedCard>().ToList();
                rcEvents = rcEvents.OrderBy(g => g.Minute).ToList();
                if (rcEvents.Count == 1)
                {
                    playerLabel.Text += " " + rc + " (" + rcEvents[0].Minute + "')";
                    playerLabel.ForeColor = Color.OrangeRed;
                }
                #endregion
            }
            #endregion

        }

        private void PlayerInfo_Click(object sender, EventArgs e, PlayerStats player)
        {
            DisplayPlayerStats statsForm = new DisplayPlayerStats(player);
            statsForm.Show();
        }

        public void LoadAnalysis()
        {
            #region Team analysis labels
            Label mostCommonScorelines = controls.CreateLabel(Controls, "mostCommonScorelines", "Scorelines: ", 30, (btnWidth + 4) * 4, 20, 100);
            mostCommonScorelines.Text += teamAnalysis.MostCommonScorelines;
            curMenuControls.Add(mostCommonScorelines);

            Label homeTeamWin = controls.CreateLabel(Controls, "homeTeamWin", "Home Win Probability: ", 30, (btnWidth + 4) * 4, 20, 130);
            homeTeamWin.Text += ((double)teamAnalysis.HomeTeamWin / NumOfSimulations) * 100 + "%";
            curMenuControls.Add(homeTeamWin);

            Label draw = controls.CreateLabel(Controls, "draw", "Draw Probability: ", 30, (btnWidth + 4) * 4, 20, 160);
            draw.Text += ((double)teamAnalysis.Draw / NumOfSimulations) * 100 + "%";
            curMenuControls.Add(draw);

            Label awayTeamWin = controls.CreateLabel(Controls, "awayTeamWin", "Away Win Probability: ", 30, (btnWidth + 4) * 4, 20, 190);
            awayTeamWin.Text += ((double)teamAnalysis.AwayTeamWin / NumOfSimulations) * 100 + "%";
            curMenuControls.Add(awayTeamWin);

            Label homeTeamScoring3 = controls.CreateLabel(Controls, "homeTeamScoring3", "Home 3+ Goals Probability: ", 30, (btnWidth + 4) * 4, 20, 220);
            homeTeamScoring3.Text += ((double)teamAnalysis.HomeTeamScoring3 / NumOfSimulations) * 100 + "%";
            curMenuControls.Add(homeTeamScoring3);

            Label awayTeamScoring3 = controls.CreateLabel(Controls, "awayTeamScoring3", "Away 3+ Goals Probability: ", 30, (btnWidth + 4) * 4, 20, 250);
            awayTeamScoring3.Text += ((double)teamAnalysis.AwayTeamScoring3 / NumOfSimulations) * 100 + "%";
            curMenuControls.Add(awayTeamScoring3);

            Label homeCleanSheet = controls.CreateLabel(Controls, "homeCleanSheet", "Home Clean Sheet Probability: ", 30, (btnWidth + 4) * 4, 20, 280);
            homeCleanSheet.Text += ((double)teamAnalysis.HomeTeamCleanSheet / NumOfSimulations) * 100 + "%";
            curMenuControls.Add(homeCleanSheet);

            Label awayCleanSheet = controls.CreateLabel(Controls, "awayCleanSheet", "Away Clean Sheet Probability: ", 30, (btnWidth + 4) * 4, 20, 310);
            awayCleanSheet.Text += ((double)teamAnalysis.AwayTeamCleanSheet / NumOfSimulations) * 100 + "%";
            curMenuControls.Add(awayCleanSheet);

            Label averageScoreline = controls.CreateLabel(Controls, "averageScoreline", "Average Scoreline: ", 30, (btnWidth + 4) * 4, 20, 340);
            averageScoreline.Text += teamAnalysis.AverageScoreline;
            curMenuControls.Add(averageScoreline);

            foreach (var item in curMenuControls)
            {
                item.ForeColor = Color.White;
                item.Font = new Font("Microsoft Sans Serif", 15);
            }

            #endregion

            #region Player anaylysis labels

            Label dataHeading = controls.CreateLabel(Controls, "dataHeading", "Per Game", 50, 100, 390, 425);
            dataHeading.Font = new Font("Microsoft Sans Serif", 12);
            dataHeading.ForeColor = Color.White;
            curMenuControls.Add(dataHeading);

            List<Label> playerAnalysisLabels = new List<Label>();
            int counter = 0;
            foreach (var player in homePlayers)
            {
                Label playerLabel = controls.CreateLabel(Controls, "playerLabel" + counter, "(" + player.FormationPosition + ") " + player.Name, 30, 400, 20, 450 + (30 * counter));
                curMenuControls.Add(playerLabel);
                playerLabel.Font = new Font("Microsoft Sans Serif", 12);
                playerLabel.ForeColor = Color.White;

                Label dataLabel = controls.CreateLabel(Controls, player.Name, "-", 30, 100, 420, 450 + (30 * counter));
                dataLabel.Font = new Font("Microsoft Sans Serif", 12);
                dataLabel.ForeColor = Color.White;
                curMenuControls.Add(dataLabel);
                playerAnalysisLabels.Add(dataLabel);
                counter += 1;
            }

            int bottomHeight = 450 + (30 * homePlayers.Count) + 50;
            counter = 0;
            foreach (var player in awayPlayers)
            {
                Label playerLabel = controls.CreateLabel(Controls, "playerLabel" + (counter + homePlayers.Count) , "(" + player.FormationPosition + ") " + player.Name, 30, 400, 20, bottomHeight + (30 * counter));             
                curMenuControls.Add(playerLabel);
                playerLabel.Font = new Font("Microsoft Sans Serif", 12);
                playerLabel.ForeColor = Color.White;

                Label dataLabel = controls.CreateLabel(Controls, player.Name, "-", 30, 100, 420, bottomHeight + (30 * counter));
                dataLabel.Font = new Font("Microsoft Sans Serif", 12);
                dataLabel.ForeColor = Color.White;
                playerAnalysisLabels.Add(dataLabel);
                curMenuControls.Add(dataLabel);
                counter += 1;
            }
            #endregion

            ComboBox analysisCB = controls.CreateComboBox(Controls, "analysisCB", 80, (btnWidth + 4) * 4, 20, 380);
            analysisCB.Items.AddRange(new string[] { "Goals", "Assists", "Shots", "Shots On Target", "Passes", "Chances Created", "Fouls", "Yellow Cards", "Red Cards"});
            analysisCB.Font = new Font("Microsoft Sans Serif", 15);
            analysisCB.BackColor = Color.DarkGreen;
            analysisCB.ForeColor = Color.White;
            analysisCB.DropDownStyle = ComboBoxStyle.DropDown;
            analysisCB.SelectedValueChanged += (sender, e) => { AnalysisCB_SelectedValueChanged(sender, e, playerAnalysisLabels); };
            curMenuControls.Add(analysisCB);
        }

        private void AnalysisCB_SelectedValueChanged(object sender, EventArgs e, List<Label> playerLabels)
        {
            //Get the stat
            ComboBox cmb = (ComboBox)sender;

            foreach (Label label in playerLabels)
            {
                var statValue = GetAnalysisStat(label.Name, cmb.SelectedItem.ToString());
                label.Text = (double.Parse(statValue) / NumOfSimulations).ToString();
            }
        }

        private string GetAnalysisStat(string name, string selectedStat)
        {
            PlayerStats playerStats = PlayerStatsAnalysis.Find(x => x.PlayerObject.Name == name);
            switch (selectedStat)
            {
                case "Goals": return playerStats.Goals.ToString();
                case "Assists": return playerStats.Assists.ToString();
                case "Shots": return playerStats.Shots.ToString();
                case "Shots On Target": return playerStats.ShotsOnTarget.ToString();
                case "Passes": return playerStats.Passes.ToString();
                case "Chances Created": return playerStats.ChancesCreated.ToString();
                case "Fouls": return playerStats.Fouls.ToString();
                case "Yellow Cards": return playerStats.YellowCards.ToString();
                case "Red Cards": return playerStats.RedCards.ToString();
                default: return "0";
            }
        }

        private void MenuButton_Click(object sender, EventArgs e, string buttonName)
        {
            foreach (var ctrl in curMenuControls) {Controls.Remove(ctrl);}
            curMenuControls.Clear();

            if (buttonName == "Match Facts") {LoadMatchFacts();}
            else if (buttonName == "Home Players") {LoadPlayers(homeTeam.TeamName, ref homePlayers, ref homeStats.PlayerStatsList);}
            else if (buttonName == "Away Players") {LoadPlayers(awayTeam.TeamName, ref awayPlayers, ref awayStats.PlayerStatsList);}
            else if (buttonName == "Analysis") { LoadAnalysis();}
        }

        private void LoadMenuButtons()
        {
            #region Add buttons at the button to navigate the menu
            for (int i = 0; i < buttonNames.Length; i++)
            {
                Button button = controls.CreateButton(Controls, "Button" + i.ToString(), buttonNames[i], 40, btnWidth, 20 + (i * (btnWidth + 5)), 15);
                button.BackColor = Color.DarkGreen;
                button.ForeColor = Color.White;
                button.TextAlign = ContentAlignment.MiddleCenter;
                button.Font = new Font("Microsoft Sans Serif", btnWidth / 11);
                button.FlatStyle = FlatStyle.Flat;
                button.FlatAppearance.BorderColor = Color.White;
                string name = buttonNames[i];
                button.Click += (sender, args) => MenuButton_Click(sender, args, name);
            }
            #endregion
        }

        #region Add event functions
        public void AddGoalEvent(Goal goalInfo, Chance chanceInfo, TeamStats teamStats)
        {
            List<PlayerStats> playerStats = teamStats.PlayerStatsList;
            //Create a separate assist object and hook it to the goal object
            Assist assist = new Assist() { HomeOrAway = goalInfo.HomeOrAway, Minute = chanceInfo.Minute, Creator = chanceInfo.Creator, Shooter = chanceInfo.Shooter, GoalObject = goalInfo };
            goalInfo.AssistObject = assist;
            //Add the assist object to the player who assisted the goal
            playerStats.Find(ps => ps.PlayerObject == chanceInfo.Creator).playerSpecificEvents.Add(assist);
            playerStats.Find(ps => ps.PlayerObject == chanceInfo.Creator).Assists += 1;

            matchEvents.Add(goalInfo);
            matchEvents.Add(assist);
            //teamStats.Goalscorers.Add(goalInfo.Scorer);
            teamStats.Goals += 1;
            teamStats.TeamEvents.Add(goalInfo);
            teamStats.TeamEvents.Add(assist);
            playerStats.Find(player => player.PlayerObject == goalInfo.Scorer).Goals += 1;
            playerStats.Find(player => player.PlayerObject == goalInfo.Scorer).playerSpecificEvents.Add(goalInfo);
        }

        public void AddChanceEvent(Chance chanceInfo, TeamStats teamStats)
        {
            List<PlayerStats> playerStats = teamStats.PlayerStatsList;

            matchEvents.Add(chanceInfo);
            teamStats.TeamEvents.Add(chanceInfo);
            playerStats.Find(player => player.PlayerObject == chanceInfo.Creator).playerSpecificEvents.Add(chanceInfo);
        }

        public void AddYellowCardEvent(YellowCard yc, TeamStats teamStats)
        {
            List<PlayerStats> playerStats = teamStats.PlayerStatsList;

            playerStats.Find(player => player.PlayerObject == yc.PlayerObject).YellowCards += 1;
            playerStats.Find(player => player.PlayerObject == yc.PlayerObject).playerSpecificEvents.Add(yc);
            if (playerStats.Find(player => player.PlayerObject == yc.PlayerObject).YellowCards == 2)
            {
                yc.DoubleYellow = true;
                teamStats.RedCards += 1;
                playerStats.Find(player => player.PlayerObject == yc.PlayerObject).RedCards += 1;
            }
            matchEvents.Add(yc);
            teamStats.YellowCards += 1;
            teamStats.TeamEvents.Add(yc);
        }

        public void AddRedCardEvent(RedCard rc, TeamStats teamStats, List<PlayerStats> playerStats)
        {

            playerStats.Find(player => player.PlayerObject == rc.PlayerObject).RedCards += 1;
            playerStats.Find(player => player.PlayerObject == rc.PlayerObject).playerSpecificEvents.Add(rc);
            matchEvents.Add(rc);
            teamStats.RedCards += 1;
            teamStats.TeamEvents.Add(rc);
        }

        public void AddSubstitutionEvent(Substitution sub, TeamStats teamStats)
        {
            List<PlayerStats> playerStats = teamStats.PlayerStatsList;

            playerStats.Find(player => player.PlayerObject == sub.ComingOn).playerSpecificEvents.Add(sub);
            playerStats.Find(player => player.PlayerObject == sub.ComingOff).playerSpecificEvents.Add(sub);
            matchEvents.Add(sub);
            teamStats.TeamEvents.Add(sub);
        }
        #endregion

        public int GetStatValue(string StatName, TeamStats stats)
        {
            switch (StatName)
            {
                case "Shots": return stats.Shots;
                case "Shots On Target": return stats.ShotsOnTarget;
                case "Fouls": return stats.Fouls;
                case "Yellow Cards": return stats.YellowCards;
                case "Red Cards": return stats.RedCards;
                case "Possession": return stats.Possession;
                default: return 0;
            }
        }

        public int InversePoissonBisection(double lambda, double p)
        {
            int counter = 0;
            double cumulativeProbability = 0;
            while (true)
            {
                cumulativeProbability += PoissonProbability(lambda, counter);
                if (cumulativeProbability >= p) { return counter; }
                else { counter += 1; }
            }
        }

        public int GetSubstituteMinutes(double p)
        {
            double cumulativeProbability = 0;
            for (int i = 0; i < 95; i++)
            {
                cumulativeProbability += MinsFunction(i);
                if (cumulativeProbability >= p) {return i;}
            }
            return 0;
        }

        public double MinsFunction(double x)
        {
            return Math.Pow(x/95, 3.7204) - Math.Pow(x/95, 3.975);
        }

        public double PoissonProbability(double lambda, int x)
        {
            return (Math.Pow(lambda, x) / Factorial(x)) * Math.Exp(-lambda);
        }

        public double Factorial(int x)
        {
            if (x == 0) return 1; else return x * Factorial(x - 1);
        }
    }
}

public class Stats
{
    public int Goals = 0;
    public int Shots = 0;
    public int ShotsOnTarget = 0;
    public int Fouls = 0;
    public int YellowCards = 0;
    public int RedCards = 0;
}

public class TeamStats : Stats //Inheritance - keeps the total value of each stat for the team and includes additional fields for team stats
{
    public int Possession = 0;
    public List<MatchEvent> TeamEvents = new List<MatchEvent>();
    public List<PlayerStats> PlayerStatsList = new List<PlayerStats>();
}

public class PlayerStats : Stats //Inheritance - keeps the total value of each stat for individual players
{
    public Player PlayerObject = new Player(); //The player this stats object is conneced to
    public List<MatchEvent> playerSpecificEvents = new List<MatchEvent>();
    public int Assists = 0;
    public int ChancesCreated = 0;
    public int Passes = 0;
}

public class MatchEvent
{
    public string Minute = "";
    public string HomeOrAway = "";
}

public class Goal : MatchEvent //Inheritance - Adds the goalscorer and assister to the event class
{
    public Player Scorer = new Player();
    public Player Assister = new Player();
    public Assist AssistObject;
}

public class Chance : MatchEvent
{
    public Player Creator = new Player();
    public Player Shooter = new Player();
}

public class Assist: Chance
{
    public Goal GoalObject;
}

public class YellowCard: MatchEvent
{
    public Player PlayerObject = new Player();
    public bool DoubleYellow = false;
}

public class RedCard: MatchEvent
{
    public Player PlayerObject = new Player();
}

public class Substitution: MatchEvent
{
    public Player ComingOff = new Player();
    public Player ComingOn = new Player();
}

public struct TeamAnalysis
{
    public string MostCommonScorelines;
    public string AverageScoreline;
    public int HomeTeamWin;
    public int AwayTeamWin;
    public int Draw;
    public int HomeTeamCleanSheet;
    public int AwayTeamCleanSheet;
    public int HomeTeamScoring3;
    public int AwayTeamScoring3;
}

