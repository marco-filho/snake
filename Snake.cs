using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace learning
{
    static class Snake
    {
        static string menuOption; //stores user selected options
        static int size = 2; //default start size will be size+1
        static string direction; //gather user input
        static char charDirection = '►'; //visuals for player
        static string input; //validation of user input
        static char[,] grid = new char[37,22]; //grid for rendering game stage
        static int whereX = grid.GetLength(0) / 2,
        whereY = grid.GetLength(1) / 2; //stores player's current position
        static int xMov = 1, yMov; //player movement, which changes depending on input
        static List<int[]> tracker = new List<int[]>(); //tracks player's whereabouts to reset grid positions
        static Random random = new Random(); //used to generate new snacks
        static bool playing = true; //a condition to ensure input thread will be terminated properly


        static void Main(string[] args)
        {   
            while(true)
            {
                Console.WriteLine("Welcome to Snake!\n" +
                "→ 'Enter' key to play\n" + 
                "→ 'Backspace' key for Leaderboard\n" +
                "→ 'Escape'(Esc) key to exit the game.");
                menuOption = Console.ReadKey().Key.ToString();
                if(menuOption == "Enter") //this will lead to the game logic
                    Play();
                else if(menuOption == "Backspace") //a leaderboard using a .txt file
                    Leaderboard(0);
                else if(menuOption == "Escape") //exits aplication
                    break;
                Console.Clear();
            }
        }

        private static void Leaderboard(int score)
        {
            if(!File.Exists(".\\Leaderboard.txt")) //checking if Leaderboard.txt file exists
                while(menuOption != "Y") //if not, asks player for creation
                {
                    Console.WriteLine("Leaderboard.txt couldn't be found. Do you want to create a new file?\n" + 
                                        "→ (Y)es\n→ (N)o\n→ (Esc) to main menu.");
                    menuOption = Console.ReadKey().Key.ToString();
                    switch(menuOption)
                    {
                    case "Y":
                        File.WriteAllLines(".\\Leaderboard.txt",
                            new string[] { "LEADERBOARD",""});
                        break;
                    case "N":
                        Console.WriteLine("No Leaderboard was created.\n(Any key to continue)");
                        Console.ReadKey();
                        return;
                    case "Escape":
                        return;
                    }
                }
                
            if(score > 0) //this will only be called after a finished game
            {
                string newEntry;
                var newLeaderboard = File.ReadLines(".\\Leaderboard.txt").ToList();;
                var regex = new Regex("^[a-zA-Z0-9]*$");
                while(true) //while loop asks for new entry nickname
                {
                    Console.WriteLine("Insert your nickname:\n(4 characters tops, alphanumeric only");
                    newEntry = Console.ReadLine();
                    if(newEntry.Length > 4 || newEntry.Length == 0 || !regex.IsMatch(newEntry))
                        Console.WriteLine("Invalid entry, try again");
                    else break;
                }
                for(int i = 1, lbCount = newLeaderboard.Count < 11 ?
                    newLeaderboard.Count : 11; i < lbCount; i++)
                {
                    if(i == lbCount-1 && lbCount < 11)
                        newLeaderboard.Insert(lbCount-1, lbCount-1 + "." + newEntry + "\t" + score);
                    else if (score > int.Parse(newLeaderboard[i]
                        .Substring(newLeaderboard[i].IndexOf("\t")+1)))
                    {
                        for (int j = lbCount-1, count = 1; j > i; j--)
                            newLeaderboard[j] = j + newLeaderboard[j-1].Remove(0,count);
                        newLeaderboard.RemoveAt(i);
                        newLeaderboard.Insert(i, i + "." + newEntry + "\t" + score);
                        newLeaderboard.Add("");
                        break;
                    }
                }
                File.WriteAllLines(".\\Leaderboard.txt", newLeaderboard); //updating Leaderboard.txt by overwriting 
            }

            Console.WriteLine("\n" + File.ReadAllText(".\\Leaderboard.txt") + "(Any key to continue)"); //printing Leaderboard
            Console.ReadKey();
        }

        private static void Play()
        {  
            Console.WriteLine("Use arrow keys to move and catch some snacks ( + ).\n" +
            "Wall and self hits means game over, so be careful!\n(Any key to start)");
            Console.ReadKey();
            playing = true;

            try //a try catch block is useful for exiting any rendering logic with clean code
            {
                GridRendering();
            }
            catch (IndexOutOfRangeException)
            {
                playing = false; //to end input thread while loop
                Console.WriteLine("\nOh no!!" +
                                    "\nThat's a game over :/" +
                                    "\nYour score: " + (size) +
                                    "\n(Any key to continue)");
                Console.ReadKey();
                Leaderboard(size); //calling leaderboard logic
                charDirection = '►'; xMov = 1; yMov = 0; direction = string.Empty; //resets direction logic
                size = 2; tracker.Clear(); //reset size logic
                whereX = grid.GetLength(0) / 2; whereY = grid.GetLength(1) / 2; //resets grid
                return;
            }
        }

        private static void GridRendering()
        {
            //new thread for realtime input
            new Thread(() =>
            {
                while(playing == true)
                {
                    input = Console.ReadKey().Key.ToString();
                    if(direction != input) direction = input;
                }
            }).Start();

            string output = string.Empty;

            for (int y = 0; y < grid.GetLength(1); y++)
                for (int x = 0; x < grid.GetLength(0); x++)
                    grid[x,y] = ' ';

            for(int x = 0; x < grid.GetLength(0); x++)
            {
                grid[x,0] = '▄';
                grid[x,grid.GetLength(1)-1] = '▀';
            }
            for(int y = 1; y < grid.GetLength(1)-1; y++)
            {
                grid[0,y] = '█';
                grid[grid.GetLength(0)-1,y] = '█';
            }
            
            SnackGenerator();

            //rendering cicle
            while(true)
            {
                SnakeRendering();
                output = string.Empty;
                Thread.Sleep(33);

                for (int y = 0; y < grid.GetLength(1); y++)
                {
                    for (int x = 0; x < grid.GetLength(0); x++)
                        output = string.Concat(output, grid[x,y]);
                    output = string.Concat(output, "\n");
                }
                
                Console.Clear();
                Console.Write(output);
            }
        }

        private static void SnakeRendering()
        {
            //switch to take input info into variables
            switch (direction)
            {
                case "UpArrow":
                    if(yMov == 1) break;
                    xMov = 0;
                    yMov = -1;
                    charDirection = '▲';
                    break;
                case "DownArrow":
                    if(yMov == -1) break;
                    xMov = 0;
                    yMov = 1;
                    charDirection = '▼';
                    break;
                case "RightArrow":
                    if(xMov == -1) break;
                    xMov = 1;
                    yMov = 0;
                    charDirection = '►';
                    break;
                case "LeftArrow":
                    if(xMov == 1) break;
                    xMov = -1;
                    yMov = 0;
                    charDirection = '◄';
                    break;
            }

            //game over conditions and exception throw to exit rendering logic with clean code
            if(grid[whereX+xMov,whereY+yMov] == '▲' ||
            grid[whereX+xMov,whereY+yMov] == '▼' ||
            grid[whereX+xMov,whereY+yMov] == '◄' ||
            grid[whereX+xMov,whereY+yMov] == '►' ||
            grid[whereX+xMov,whereY+yMov] == '█' ||
            grid[whereX+xMov,whereY+yMov] == '▀' ||
            grid[whereX+xMov,whereY+yMov] == '▄')
                throw new IndexOutOfRangeException();
            
            //below we're updating player surroundings
            if(grid[whereX+xMov,whereY+yMov] == '+') //if player gets a snack, generates another
                SnackGenerator();

            grid[whereX+xMov,whereY+yMov] = charDirection; //rendering next player position

            tracker.Add(new int[] { whereX+xMov,whereY+yMov }); //tracking player positions

            if(tracker.Count > size) //reseting grid positions back to empty space where player has passed
            {
                for (int i = 0; i < tracker.Count-size; i++)
                {
                    grid.SetValue(' ', tracker[i][0], tracker[i][1]);
                    tracker.RemoveAt(i); //removing unnecessary tracking information
                }
            }
            //updating player's whereabouts
            if (xMov != 0)whereX += xMov;
            else if (yMov != 0) whereY += yMov;
        }

        private static void SnackGenerator()
        {
            int x;
            int y;
            char nextSnack;
            do //validate if next snack position overwrites player length
            {
                x = random.Next(1, grid.GetLength(0)-1);
                y = random.Next(1, grid.GetLength(1)-1);
                nextSnack = grid[ x, y ];
            }
            while(nextSnack == '▲' || nextSnack == '▼' || nextSnack == '◄' || nextSnack == '►');
            grid[ x, y ] = '+';
            size++;
            Console.Beep();
        }
    }    
}