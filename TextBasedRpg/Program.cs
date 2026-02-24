using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.IO;
using System.Threading;

namespace TextBasedRpg
{
    internal class Program
    {
        static string[,] mapa = new string[10, 10]
        {
            {".",".",".",".","F","F","F",".",".","."},
            {".",".","M","M","F",".",".",".","T","T"},
            {".",".",".",".","F",".",".",".",".","."},
            {".","M","M",".",".",".",".","M","M","."},
            {".",".",".",".",".",".",".",".",".","."},
            {".",".","F","F","F",".",".",".",".","."},
            {".",".",".",".",".",".","C","C","C","."},
            {".",".",".","M","M","M",".",".",".","."},
            {".",".",".",".",".",".",".","T","T","."},
            {".",".",".",".",".",".",".",".",".","."}
        };

        // Single static Random instance
        static Random rnd = new Random();

        static void Main(string[] args)
        {
            while (true)
            {
                Console.WriteLine("=== GLAVNI MENI ===");

                if (File.Exists("karakter.txt"))
                    Console.WriteLine("1 - Naloži obstoječ karakter");
                else
                    Console.WriteLine("1 - Ustvari novega igralca");

                Console.WriteLine("2 - Začni igro");
                Console.WriteLine("0 - Izhod");
                Console.Write("Izberite: ");

                string vnos = Console.ReadLine();
                Igralec igralec = null;

                // 1 → Ustvari ali naloži
                if (vnos == "1")
                {
                    if (File.Exists("karakter.txt"))
                    {
                        igralec = Igralec.NaloziKarakter("karakter.txt");
                        Console.WriteLine("\nKarakter uspešno naložen!");
                    }
                    else
                    {
                        igralec = UstvariIgralca();
                        Console.WriteLine("\nIgralec ustvarjen!");
                        igralec.ShraniVDatoteko("karakter.txt");
                        Console.WriteLine("Karakter shranjen.");
                    }

                    igralec.PrikazStatov();
                    continue;
                }

                // 2 → Začni igro
                if (vnos == "2")
                {
                    if (!File.Exists("karakter.txt"))
                    {
                        Console.WriteLine("Najprej ustvari karakter!");
                        continue;
                    }

                    igralec = Igralec.NaloziKarakter("karakter.txt");

                    int x = 5, y = 5;

                    while (true)
                    {
                        Console.WriteLine($"\nSi na lokaciji ({x},{y}) – teren: {mapa[y, x]}");
                        Console.WriteLine("Kam želiš iti? (W/A/S/D, Q = nazaj) ");

                        string input = Console.ReadLine().ToUpper();
                        if (input == "Q")
                        {
                            igralec.ShraniVDatoteko("karakter.txt");
                            break;
                        }

                        int nx = x, ny = y;

                        if (input == "W") ny--;
                        if (input == "S") ny++;
                        if (input == "A") nx--;
                        if (input == "D") nx++;

                        if (nx < 0 || nx >= 10 || ny < 0 || ny >= 10)
                        {
                            Console.WriteLine("Ne moreš izven mape!");
                            continue;
                        }

                        x = nx;
                        y = ny;

                        Console.WriteLine("\n=== MAPA ==="); 
                        IzpisiMapo(x, y);


                        if (rnd.Next(100) < 30)
                        {
                            var events = PreberiEvente();
                            if (events.Count > 0)
                            {
                                var e = NakljucniEvent(events);
                                Console.WriteLine($"\nEncounter: {e.Opis}!");

                                if (e.Pogoj == "combat")
                                {
                                    var zver = NakljucnaZver(UstvariPošasti());
                                    Console.WriteLine($"Napade te {zver.Ime}!");
                                    Boj(igralec, zver);
                                }
                                else
                                {
                                    IzvediEvent(igralec, e);
                                }

                                igralec.ShraniVDatoteko("karakter.txt");
                            }
                        }
                        else
                        {
                            Console.WriteLine("Ni se zgodilo nič posebnega.");
                        }
                    }
                }

                // 0 → izhod
                else if (vnos == "0")
                {
                    Console.WriteLine("Nasvidenje!");
                    return;
                }
                else
                {
                    Console.WriteLine("Neveljavna izbira.");
                }
            }
        }

        // USTVARJANJE IGRALCA
        static Igralec UstvariIgralca()
        {
            Console.Write("Vnesi ime igralca: ");
            string ime = Console.ReadLine();

            int tocke = 20;

            int str = 1, dex = 1, intel = 1, con = 1, spd = 1;
            tocke -= 5;

            while (tocke > 0)
            {
                Console.WriteLine($"\nPreostale točke: {tocke}");
                Console.WriteLine($"1 - STR ({str})");
                Console.WriteLine($"2 - DEX ({dex})");
                Console.WriteLine($"3 - INT ({intel})");
                Console.WriteLine($"4 - CON ({con})");
                Console.WriteLine($"5 - SPD ({spd})");
                Console.Write("Izberi: ");

                switch (Console.ReadLine())
                {
                    case "1": if (str < 10) { str++; tocke--; } break;
                    case "2": if (dex < 10) { dex++; tocke--; } break;
                    case "3": if (intel < 10) { intel++; tocke--; } break;
                    case "4": if (con < 10) { con++; tocke--; } break;
                    case "5": if (spd < 10) { spd++; tocke--; } break;
                }
            }

            return new Igralec(ime, spd, str, dex, intel, con);
        }

        static List<Pošast> UstvariPošasti()
        {
            return new List<Pošast>()
            {
                new Goblin(),
                new Volk(),
                new Okostnjak(),
                new Ork()
            };
        }

        static Pošast NakljucnaZver(List<Pošast> seznam)
        {
            return seznam[rnd.Next(seznam.Count)];
        }

        static Encounter NakljucniEvent(List<Encounter> events)
        {
            return events[rnd.Next(events.Count)];
        }

        static List<Encounter> PreberiEvente()
        {
            List<Encounter> seznam = new();
            if (!File.Exists("encounterji.txt"))
            {
                Console.WriteLine("Manjka encounterji.txt!");
                return seznam;
            }

            foreach (var line in File.ReadAllLines("encounterji.txt"))
            {
                var d = line.Split(';');
                if (d.Length == 3)
                    seznam.Add(new Encounter(d[0], d[1], d[2]));
            }
            return seznam;
        }

        //IZPIS MAPE
        static void IzpisiMapo(int playerX, int playerY)
        {
            for (int y = 0; y < 10; y++)
            {
                for (int x = 0; x < 10; x++)
                {
                    if (x == playerX && y == playerY)
                    {
                        Console.Write("P ");
                    }
                    else
                    {
                        Console.Write(mapa[y, x] + " ");
                    }
                }
                Console.WriteLine();
            }

            Console.WriteLine("\nLegenda: . = prazno, F = gozd, M = gora, T = mesto, C = grad, P = igralec");
        }


        //BOJ
        static void Boj(Igralec igralec, Pošast zver)
        {
            Console.WriteLine($"\nZačel se je boj proti: {zver.Ime}!");

            while (igralec.HP > 0 && zver.HP > 0)
            {
                Console.WriteLine($"TVOJ HP: {igralec.HP} | {zver.Ime} HP: {zver.HP}");
                Console.WriteLine("1 - Napad");
                Console.WriteLine("2 - Pobeg");
                Console.Write("Izbira: ");

                string izbira = Console.ReadLine();

                if (izbira == "1")
                {
                    zver.HP -= igralec.Napad;
                    if (zver.HP > 0)
                        igralec.HP -= zver.Napad;
                }
                else if (izbira == "2")
                {
                    if (rnd.Next(100) < igralec.Speed * 5)
                    {
                        Console.WriteLine("Uspešen pobeg!");
                        return;
                    }
                    else
                    {
                        Console.WriteLine("Neuspeh! Prejel si udarec!");
                        igralec.HP -= zver.Napad;
                    }
                }
            }

            if (igralec.HP <= 0)
            {
                Console.WriteLine("UMRL SI!");
                Environment.Exit(0);
            }

            Console.WriteLine($"Premagal si {zver.Ime}!");
            igralec.AddXP(zver.XPTocke);
        }

        static void IzvediEvent(Igralec igralec, Encounter evt)
        {
            Console.WriteLine(evt.Opis);

            switch (evt.Opis)
            {
                case "Polomljena kočija":
                    if (igralec.Dexterity >= 5)
                    {
                        Console.WriteLine("Pomagaš popraviti kočijo in dobiš 10 XP.");
                        igralec.AddXP(10);
                    }
                    else
                    {
                        Console.WriteLine("Nimaš dovolj spretnosti, da bi popravil kočijo.");
                    }
                    break;

                case "Goreča hiša":
                    if (igralec.Strength >= 6)
                    {
                        Console.WriteLine("Rešiš človeka iz ognja in dobiš 15 XP.");
                        igralec.AddXP(15);
                    }
                    else
                    {
                        Console.WriteLine("Premalo moči, da bi rešil človeka.");
                    }
                    break;

                case "Tatovi na cesti":
                    if (igralec.Dexterity >= 7)
                    {
                        Console.WriteLine("Umakneš se napadu in dobiš 5 XP.");
                        igralec.AddXP(5);
                    }
                    else
                    {
                        Console.WriteLine("Tatovi te poškodujejo (-10 HP).");
                        igralec.HP -= 10;
                        if (igralec.HP <= 0)
                        {
                            Console.WriteLine("UMRL SI!");
                            Environment.Exit(0);
                        }
                    }
                    break;

                case "Zapuščena vas":
                    Console.WriteLine("Ni se zgodilo nič posebnega…");
                    break;

                default:
                    Console.WriteLine(evt.Rezultat);
                    break;
            }
        }
    }

    // RAZRED IGRALCA
    class Igralec
        {
            public string Ime;
            public int Strength, Dexterity, Intelligence, Constitution, Speed;
            public int HP, Napad;
            public int Level, XP, XPNeeded, FreeStatPoints;

            public Igralec(string ime, int spd, int str, int dex, int intel, int con)
            {
                Ime = ime;
                Strength = str;
                Dexterity = dex;
                Intelligence = intel;
                Constitution = con;
                Speed = spd;

                HP = 50 + con * 5;
                Napad = 5 + str;
                Level = 1;
                XP = 0;
                XPNeeded = 30;
                FreeStatPoints = 0;
            }

            public void ShraniVDatoteko(string pot)
            {
                File.WriteAllLines(pot, new string[]
                {
                Ime, Strength.ToString(), Dexterity.ToString(), Intelligence.ToString(),
                Constitution.ToString(), Speed.ToString(),
                HP.ToString(), Napad.ToString(),
                Level.ToString(), XP.ToString(), XPNeeded.ToString(), FreeStatPoints.ToString()
                });
            }

            public static Igralec NaloziKarakter(string pot)
            {
                string[] v = File.ReadAllLines(pot);

                Igralec ig = new Igralec(
                    v[0],
                    int.Parse(v[5]),
                    int.Parse(v[1]),
                    int.Parse(v[2]),
                    int.Parse(v[3]),
                    int.Parse(v[4])
                );

                ig.HP = int.Parse(v[6]);
                ig.Napad = int.Parse(v[7]);
                ig.Level = int.Parse(v[8]);
                ig.XP = int.Parse(v[9]);
                ig.XPNeeded = int.Parse(v[10]);
                ig.FreeStatPoints = int.Parse(v[11]);

                return ig;
            }

            public void PrikazStatov()
            {
                Console.WriteLine($"\nIme: {Ime}");
                Console.WriteLine($"STR: {Strength}, DEX: {Dexterity}, INT: {Intelligence}, CON: {Constitution}, SPD: {Speed}");
                Console.WriteLine($"HP: {HP}, NAPAD: {Napad}");
                Console.WriteLine($"LEVEL: {Level}, XP: {XP}/{XPNeeded}, Free Points: {FreeStatPoints}\n");
            }

            public void PorabiTočke()
            {
                while (FreeStatPoints > 0)
                {
                    Console.WriteLine($"\nImaš {FreeStatPoints} prostih točk.");
                    Console.WriteLine("1 - STR");
                    Console.WriteLine("2 - DEX");
                    Console.WriteLine("3 - INT");
                    Console.WriteLine("4 - CON");
                    Console.WriteLine("5 - SPD");
                    Console.Write("Izberi: ");

                    string izbira = Console.ReadLine();
                    switch (izbira)
                    {
                        case "1": Strength++; FreeStatPoints--; break;
                        case "2": Dexterity++; FreeStatPoints--; break;
                        case "3": Intelligence++; FreeStatPoints--; break;
                        case "4": Constitution++; FreeStatPoints--; HP += 5; break;
                        case "5": Speed++; FreeStatPoints--; break;
                        default: Console.WriteLine("Neveljavna izbira."); break;
                    }
                }

            Napad = 5 + Strength;
            HP = 50 + Constitution * 5;

            Console.WriteLine("\nStatistike posodobljene!");
            PrikazStatov();
            }


        public void AddXP(int amount)
            {
                XP += amount;
                while (XP >= XPNeeded)
                {
                    XP += amount;
                while (XP >= XPNeeded)
                {
                    XP -= XPNeeded;
                    Level++;
                    FreeStatPoints += 3;
                    XPNeeded = (int)(XPNeeded * 1.25);
                    Console.WriteLine($"Novi level! {Level} (+3 prostih točk)");

                    PorabiTočke();
                }
                }
            }
        }

        // POŠAST
        class Pošast
        {
            public string Ime;
            public int HP, Napad, XPTocke;

            public Pošast(string ime, int hp, int napad, int xp)
            {
                Ime = ime; HP = hp; Napad = napad; XPTocke = xp;
            }
        }

        // DEDOVANJE POŠASTI
        class Goblin : Pošast
        {
            public Goblin() : base("Goblin", 15, 5, 10) { }
        }

        class Volk : Pošast
        {
            public Volk() : base("Volk", 25, 7, 20) { }
        }

        class Okostnjak : Pošast
        {
            public Okostnjak() : base("Okostnjak", 30, 8, 25) { }
        }

        class Ork : Pošast
        {
            public Ork() : base("Ork", 40, 12, 35) { }
        }

        // ENCOUNTER
        class Encounter
        {
            public string Opis, Pogoj, Rezultat;
            public Encounter(string o, string p, string r)
            {
                Opis = o; Pogoj = p; Rezultat = r;
            }
        }
    }
