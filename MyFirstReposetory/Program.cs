using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace MyFirstRepository
{
    // Creature class
    public class Creature
    {
        public string Name { get; set; }
        public int Health { get; set; }
        public int MaxHealth { get; set; }
        public int Attack { get; set; }
        public int Defense { get; set; }
        public int Speed { get; set; }
        public List<Move> Moves { get; set; }
        public Color Color { get; set; }

        public Creature(string name, int maxHealth, int attack, int defense, int speed, Color color)
        {
            Name = name;
            Health = maxHealth;
            MaxHealth = maxHealth;
            Attack = attack;
            Defense = defense;
            Speed = speed;
            Color = color;
            Moves = new List<Move>();
        }

        public void AddMove(Move move)
        {
            if (Moves.Count < 4)
                Moves.Add(move);
        }

        public bool IsAlive => Health > 0;

        public void TakeDamage(int damage)
        {
            int actualDamage = Math.Max(1, damage - Defense / 2);
            Health -= actualDamage;
            if (Health < 0) Health = 0;
        }

        public void Heal(int amount)
        {
            int oldHealth = Health;
            Health = Math.Min(MaxHealth, Health + amount);
        }
    }

    // Move class
    public class Move
    {
        public string Name { get; set; }
        public int Power { get; set; }
        public int Accuracy { get; set; }
        public MoveType Type { get; set; }

        public Move(string name, int power, int accuracy, MoveType type)
        {
            Name = name;
            Power = power;
            Accuracy = accuracy;
            Type = type;
        }
    }

    // Move types
    public enum MoveType
    {
        Attack,
        SpecialAttack,
        Heal,
        Defense
    }

    // Battle Game Form
    public class BattleForm : Form
    {
        private Creature playerCreature;
        private Creature opponentCreature;
        private Random random = new Random();
        private string battleMessage = "Choose your move!";
        private int turn = 0;
        private Button[] moveButtons = new Button[4];
        private Button itemButton;
        private Button playAgainButton;
        private bool isPlayerTurn = true;
        private bool battleOver = false;
        private bool itemUsed = false;
        private int damageFlashTimer = 0;
        private int opponentDamageFlashTimer = 0;
        private System.Windows.Forms.Timer gameTimer;
        private System.Windows.Forms.Timer battleTimer;
        private bool isWaitingForOpponent = false;
        private bool isExecutingOpponentMove = false;

        public BattleForm()
        {
            // Form settings
            this.Text = "Battle Arena";
            this.Size = new Size(1400, 940);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = Color.FromArgb(30, 40, 60);
            this.DoubleBuffered = true;
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;

            // Create creatures
            playerCreature = new Creature("Flame Wolf", 100, 15, 8, 10, Color.FromArgb(255, 120, 50));
            playerCreature.AddMove(new Move("Flame Burst", 25, 90, MoveType.Attack));
            playerCreature.AddMove(new Move("Inferno", 35, 75, MoveType.SpecialAttack));
            playerCreature.AddMove(new Move("Howl", 0, 100, MoveType.Defense));
            playerCreature.AddMove(new Move("Restore", 20, 100, MoveType.Heal));

            opponentCreature = new Creature("Aqua Tiger", 110, 12, 12, 8, Color.FromArgb(50, 150, 255));
            opponentCreature.AddMove(new Move("Water Slash", 20, 95, MoveType.Attack));
            opponentCreature.AddMove(new Move("Tsunami", 40, 70, MoveType.SpecialAttack));
            opponentCreature.AddMove(new Move("Armor Up", 0, 100, MoveType.Defense));
            opponentCreature.AddMove(new Move("Recover", 30, 100, MoveType.Heal));

            // Create move buttons
            for (int i = 0; i < 4; i++)
            {
                int index = i;
                moveButtons[i] = new Button();
                moveButtons[i].Text = playerCreature.Moves[i].Name;
                moveButtons[i].Size = new Size(240, 60);
                moveButtons[i].Location = new Point(250 + (i % 2) * 280, 820 - (i / 2) * 70);
                moveButtons[i].Click += (s, e) => PlayerSelectMove(index);
                moveButtons[i].Font = new Font("Arial", 12, FontStyle.Bold);
                moveButtons[i].BackColor = Color.FromArgb(70, 130, 180);
                moveButtons[i].ForeColor = Color.White;
                moveButtons[i].FlatStyle = FlatStyle.Flat;
                moveButtons[i].FlatAppearance.BorderColor = Color.FromArgb(100, 160, 220);
                moveButtons[i].FlatAppearance.BorderSize = 2;
                this.Controls.Add(moveButtons[i]);
            }

            // Create item button (one-time grenade)
            itemButton = new Button();
            itemButton.Text = "GRENADE";
            itemButton.Size = new Size(240, 60);
            itemButton.Location = new Point(830, 820);
            itemButton.Font = new Font("Arial", 12, FontStyle.Bold);
            itemButton.BackColor = Color.FromArgb(180, 80, 80);
            itemButton.ForeColor = Color.White;
            itemButton.FlatStyle = FlatStyle.Flat;
            itemButton.FlatAppearance.BorderColor = Color.FromArgb(220, 120, 120);
            itemButton.FlatAppearance.BorderSize = 2;
            itemButton.Click += (s, e) => UseGrenade();
            this.Controls.Add(itemButton);

            // Main animation timer
            gameTimer = new System.Windows.Forms.Timer();
            gameTimer.Interval = 50;
            gameTimer.Tick += (s, e) =>
            {
                damageFlashTimer = Math.Max(0, damageFlashTimer - 1);
                opponentDamageFlashTimer = Math.Max(0, opponentDamageFlashTimer - 1);
                this.Invalidate();
            };
            gameTimer.Start();

            // Battle sequence timer
            battleTimer = new System.Windows.Forms.Timer();
            battleTimer.Interval = 2000;
            battleTimer.Tick += BattleTimer_Tick;

            // Create Play Again button (hidden initially)
            playAgainButton = new Button();
            playAgainButton.Text = "PLAY AGAIN";
            playAgainButton.Size = new Size(200, 60);
            playAgainButton.Location = new Point(830, 740);
            playAgainButton.Font = new Font("Arial", 14, FontStyle.Bold);
            playAgainButton.BackColor = Color.FromArgb(50, 200, 100);
            playAgainButton.ForeColor = Color.White;
            playAgainButton.FlatStyle = FlatStyle.Flat;
            playAgainButton.FlatAppearance.BorderColor = Color.FromArgb(100, 255, 150);
            playAgainButton.FlatAppearance.BorderSize = 2;
            playAgainButton.Click += (s, e) => ResetBattle();
            playAgainButton.Visible = false;
            this.Controls.Add(playAgainButton);

            this.Paint += BattleForm_Paint;
            this.KeyDown += BattleForm_KeyDown;
        }

        private void ResetBattle()
        {
            // Reset creatures
            playerCreature = new Creature("Flame Wolf", 100, 15, 8, 10, Color.FromArgb(255, 120, 50));
            playerCreature.AddMove(new Move("Flame Burst", 25, 90, MoveType.Attack));
            playerCreature.AddMove(new Move("Inferno", 35, 75, MoveType.SpecialAttack));
            playerCreature.AddMove(new Move("Howl", 0, 100, MoveType.Defense));
            playerCreature.AddMove(new Move("Restore", 20, 100, MoveType.Heal));

            opponentCreature = new Creature("Aqua Tiger", 110, 12, 12, 8, Color.FromArgb(50, 150, 255));
            opponentCreature.AddMove(new Move("Water Slash", 20, 95, MoveType.Attack));
            opponentCreature.AddMove(new Move("Tsunami", 40, 70, MoveType.SpecialAttack));
            opponentCreature.AddMove(new Move("Armor Up", 0, 100, MoveType.Defense));
            opponentCreature.AddMove(new Move("Recover", 30, 100, MoveType.Heal));

            // Reset game state
            turn = 0;
            isPlayerTurn = true;
            battleOver = false;
            isWaitingForOpponent = false;
            isExecutingOpponentMove = false;
            damageFlashTimer = 0;
            opponentDamageFlashTimer = 0;
            battleMessage = "Choose your move!";
            itemUsed = false;

            // Hide play again button and enable buttons
            playAgainButton.Visible = false;
            itemButton.Enabled = true;
            foreach (var btn in moveButtons)
                btn.Enabled = true;

            this.Invalidate();
        }

        private void BattleTimer_Tick(object? sender, EventArgs e)
        {
            battleTimer.Stop();
            
            if (isWaitingForOpponent && !isExecutingOpponentMove)
            {
                isExecutingOpponentMove = true;
                OpponentTurn();
            }
        }

        private void BattleForm_Paint(object? sender, PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

            // Draw gradient background
            using (LinearGradientBrush brush = new LinearGradientBrush(
                new Point(0, 0), new Point(0, this.Height),
                Color.FromArgb(30, 40, 60), Color.FromArgb(50, 70, 100)))
            {
                e.Graphics.FillRectangle(brush, this.ClientRectangle);
            }

            // Draw title with shadow
            e.Graphics.DrawString("⚔ BATTLE ARENA ⚔", new Font("Arial", 32, FontStyle.Bold), 
                Brushes.DarkGray, 370, 8);
            e.Graphics.DrawString("⚔ BATTLE ARENA ⚔", new Font("Arial", 32, FontStyle.Bold), 
                Brushes.Gold, 368, 5);

            // Draw opponent (top right)
            DrawCreature(e.Graphics, opponentCreature, 900, 50, opponentDamageFlashTimer);

            // Draw battle message box in middle
            Rectangle messageBox = new Rectangle(50, 380, 1300, 90);
            e.Graphics.FillRectangle(new SolidBrush(Color.FromArgb(40, 50, 80)), messageBox);
            e.Graphics.DrawRectangle(new Pen(Color.FromArgb(100, 150, 200), 2), messageBox);
            e.Graphics.DrawString(battleMessage, new Font("Arial", 14, FontStyle.Bold), 
                new SolidBrush(Color.FromArgb(200, 220, 255)), messageBox.X + 20, messageBox.Y + 20);

            // Draw player (bottom left)
            DrawCreature(e.Graphics, playerCreature, 150, 600, damageFlashTimer);

            // Draw turn indicator
            if (!battleOver)
            {
                string turnText = isPlayerTurn ? "YOUR TURN" : "OPPONENT'S TURN";
                Brush turnBrush = isPlayerTurn ? Brushes.LimeGreen : Brushes.OrangeRed;
                Font turnFont = new Font("Arial", 13, FontStyle.Bold);
                e.Graphics.DrawString($"Turn: {turn}  |  {turnText}", turnFont, turnBrush, 50, 495);
            }
            else
            {
                Brush winBrush = playerCreature.IsAlive ? Brushes.LimeGreen : Brushes.Red;
                string resultText = playerCreature.IsAlive ? "🎉 YOU WIN! 🎉" : "💀 YOU LOSE! 💀";
                e.Graphics.DrawString(resultText, new Font("Arial", 22, FontStyle.Bold), winBrush, 450, 495);
            }
        }

        private void DrawCreature(Graphics g, Creature creature, int x, int y, int flashTimer)
        {
            // Determine color with damage flash
            Color displayColor = creature.Color;
            if (flashTimer > 0)
            {
                int intensity = (flashTimer * 255) / 15;
                displayColor = Color.FromArgb(
                    Math.Min(255, creature.Color.R + intensity),
                    100,
                    100
                );
            }

            // Draw creature name
            g.DrawString(creature.Name, new Font("Arial", 13, FontStyle.Bold), 
                Brushes.White, x + 20, y - 35);

            if (creature.Name == "Flame Wolf")
            {
                DrawFlameWolf(g, x, y, displayColor, creature.Color);
            }
            else
            {
                DrawAquaTiger(g, x, y, displayColor, creature.Color);
            }

            // Draw HP bar BELOW creature - well separated and large
            Rectangle hpBarBg = new Rectangle(x - 80, y + 160, 220, 40);
            g.FillRectangle(new SolidBrush(Color.FromArgb(40, 40, 40)), hpBarBg);
            g.DrawRectangle(new Pen(Color.White, 2), hpBarBg);

            // Draw HP bar fill
            int hpWidth = (creature.Health * 216) / creature.MaxHealth;
            Color hpColor = creature.Health > creature.MaxHealth * 0.5 ? Color.FromArgb(50, 200, 50) : 
                           creature.Health > creature.MaxHealth * 0.25 ? Color.FromArgb(255, 200, 50) :
                           Color.FromArgb(255, 50, 50);
            Rectangle hpBarFill = new Rectangle(x - 78, y + 162, hpWidth, 36);
            g.FillRectangle(new SolidBrush(hpColor), hpBarFill);

            // Draw HP text - large and clear
            g.DrawString($"{creature.Health}/{creature.MaxHealth} HP", new Font("Arial", 13, FontStyle.Bold), 
                Brushes.White, x - 60, y + 166);
        }

        private void DrawFlameWolf(Graphics g, int x, int y, Color displayColor, Color originalColor)
        {
            // Body
            g.FillEllipse(new SolidBrush(displayColor), x + 20, y + 40, 100, 80);
            
            // Head
            g.FillEllipse(new SolidBrush(displayColor), x + 50, y, 70, 70);
            
            // Ears
            Point[] ear1 = { new Point(x + 55, y + 5), new Point(x + 70, y - 15), new Point(x + 75, y + 10) };
            g.FillPolygon(new SolidBrush(displayColor), ear1);
            Point[] ear2 = { new Point(x + 105, y + 5), new Point(x + 120, y - 15), new Point(x + 115, y + 10) };
            g.FillPolygon(new SolidBrush(displayColor), ear2);
            
            // Snout
            g.FillEllipse(new SolidBrush(Color.FromArgb(displayColor.R - 30, displayColor.G - 30, displayColor.B - 30)), 
                x + 65, y + 35, 40, 30);
            
            // Eyes
            g.FillEllipse(Brushes.White, x + 70, y + 15, 12, 14);
            g.FillEllipse(Brushes.White, x + 95, y + 15, 12, 14);
            g.FillEllipse(Brushes.Black, x + 73, y + 18, 6, 8);
            g.FillEllipse(Brushes.Black, x + 98, y + 18, 6, 8);
            
            // Nose
            g.FillEllipse(Brushes.Black, x + 82, y + 48, 8, 6);
            
            // Mouth
            g.DrawArc(new Pen(Brushes.Black, 2), x + 75, y + 50, 20, 15, 0, 180);
            
            // Flame on head
            g.FillEllipse(new SolidBrush(Color.FromArgb(255, 150, 0)), x + 75, y - 25, 20, 30);
            g.FillEllipse(new SolidBrush(Color.FromArgb(255, 200, 0)), x + 78, y - 20, 14, 20);
            
            // Tail
            Point[] tail = { new Point(x + 120, y + 70), new Point(x + 160, y + 50), new Point(x + 140, y + 100) };
            g.FillPolygon(new SolidBrush(displayColor), tail);
            
            // Outline
            g.DrawEllipse(new Pen(Color.White, 2), x + 50, y, 70, 70);
            g.DrawEllipse(new Pen(Color.White, 2), x + 20, y + 40, 100, 80);
        }

        private void DrawAquaTiger(Graphics g, int x, int y, Color displayColor, Color originalColor)
        {
            // Body
            g.FillEllipse(new SolidBrush(displayColor), x + 15, y + 30, 110, 90);
            
            // Head
            g.FillEllipse(new SolidBrush(displayColor), x + 55, y - 10, 75, 75);
            
            // Stripes
            g.DrawLine(new Pen(Color.FromArgb(30, 120, 200), 3), x + 30, y + 50, x + 90, y + 40);
            g.DrawLine(new Pen(Color.FromArgb(30, 120, 200), 3), x + 35, y + 80, x + 110, y + 75);
            
            // Ears/Fins on head
            Point[] fin1 = { new Point(x + 60, y), new Point(x + 50, y - 20), new Point(x + 75, y + 5) };
            g.FillPolygon(new SolidBrush(displayColor), fin1);
            Point[] fin2 = { new Point(x + 115, y), new Point(x + 130, y - 20), new Point(x + 110, y + 5) };
            g.FillPolygon(new SolidBrush(displayColor), fin2);
            
            // Snout
            g.FillEllipse(new SolidBrush(Color.FromArgb(80, 180, 255)), x + 75, y + 25, 45, 35);
            
            // Eyes
            g.FillEllipse(Brushes.White, x + 75, y + 10, 14, 16);
            g.FillEllipse(Brushes.White, x + 105, y + 10, 14, 16);
            g.FillEllipse(Brushes.Black, x + 79, y + 13, 7, 10);
            g.FillEllipse(Brushes.Black, x + 109, y + 13, 7, 10);
            
            // Nostrils
            g.FillEllipse(Brushes.Black, x + 90, y + 50, 5, 4);
            g.FillEllipse(Brushes.Black, x + 100, y + 50, 5, 4);
            
            // Teeth
            for (int i = 0; i < 4; i++)
            {
                g.DrawLine(new Pen(Brushes.White, 1), x + 85 + (i * 5), y + 62, x + 87 + (i * 5), y + 68);
            }
            
            // Dorsal fin
            Point[] dorsalFin = { new Point(x + 60, y + 25), new Point(x + 55, y - 10), new Point(x + 65, y + 25) };
            g.FillPolygon(new SolidBrush(Color.FromArgb(30, 120, 200)), dorsalFin);
            
            // Tail
            Point[] tail = { new Point(x + 120, y + 80), new Point(x + 180, y + 50), new Point(x + 170, y + 120) };
            g.FillPolygon(new SolidBrush(displayColor), tail);
            
            // Tail details
            g.DrawLine(new Pen(Color.FromArgb(30, 120, 200), 2), x + 140, y + 70, x + 160, y + 60);
            
            // Outline
            g.DrawEllipse(new Pen(Color.White, 2), x + 55, y - 10, 75, 75);
            g.DrawEllipse(new Pen(Color.White, 2), x + 15, y + 30, 110, 90);
        }

        private void PlayerSelectMove(int moveIndex)
        {
            if (!isPlayerTurn || battleOver || isWaitingForOpponent || moveIndex < 0 || 
                moveIndex >= playerCreature.Moves.Count)
                return;

            // Disable all buttons during this turn
            foreach (var btn in moveButtons)
                btn.Enabled = false;
            itemButton.Enabled = false;

            isPlayerTurn = false;
            isWaitingForOpponent = true;
            Move playerMove = playerCreature.Moves[moveIndex];
            battleMessage = $"{playerCreature.Name} uses {playerMove.Name}!";
            this.Invalidate();

            ExecuteMove(playerCreature, opponentCreature, playerMove);

            if (!opponentCreature.IsAlive)
            {
                battleTimer.Stop();
                battleOver = true;
                playAgainButton.Visible = true;
                battleMessage = $"Victory! {playerCreature.Name} defeated {opponentCreature.Name}!";
                return;
            }

            // Schedule opponent's turn
            battleTimer.Start();
        }

        private void UseGrenade()
        {
            if (!isPlayerTurn || battleOver || isWaitingForOpponent || itemUsed)
                return;

            // Disable all buttons during item use
            foreach (var btn in moveButtons)
                btn.Enabled = false;
            itemButton.Enabled = false;

            isPlayerTurn = false;
            isWaitingForOpponent = true;
            itemUsed = true;
            battleMessage = $"{playerCreature.Name} throws a grenade!";
            this.Invalidate();

            // Grenade does massive damage to opponent
            int grenadeDamage = 70 + random.Next(-5, 6);
            opponentCreature.TakeDamage(grenadeDamage);
            battleMessage = $"Grenade hits {opponentCreature.Name} for {grenadeDamage} damage!";
            opponentDamageFlashTimer = 20;
            this.Invalidate();

            if (!opponentCreature.IsAlive)
            {
                battleTimer.Stop();
                battleOver = true;
                playAgainButton.Visible = true;
                battleMessage = $"Victory! {playerCreature.Name} defeated {opponentCreature.Name}!";
                return;
            }

            battleTimer.Start();
        }

        private void OpponentTurn()
        {
            if (battleOver) return;

            Move opponentMove = opponentCreature.Moves[random.Next(opponentCreature.Moves.Count)];
            battleMessage = $"{opponentCreature.Name} uses {opponentMove.Name}!";
            this.Invalidate();

            System.Threading.Thread.Sleep(500);
            ExecuteMove(opponentCreature, playerCreature, opponentMove);

            if (!playerCreature.IsAlive)
            {
                battleTimer.Stop();
                battleOver = true;
                playAgainButton.Visible = true;
                battleMessage = $"Defeat! {opponentCreature.Name} defeated {playerCreature.Name}!";
                return;
            }

            turn++;
            isPlayerTurn = true;
            isWaitingForOpponent = false;
            isExecutingOpponentMove = false;
            battleMessage = "Your turn! Choose your move!";

            // Re-enable buttons
            foreach (var btn in moveButtons)
                btn.Enabled = true;

            this.Invalidate();
        }

        private void ExecuteMove(Creature attacker, Creature defender, Move move)
        {
            // Check accuracy
            if (random.Next(100) > move.Accuracy)
            {
                battleMessage = $"{move.Name} missed!";
                this.Invalidate();
                return;
            }

            switch (move.Type)
            {
                case MoveType.Attack:
                    int damage = move.Power + attacker.Attack / 2 + random.Next(-5, 6);
                    int actualDamage = Math.Max(1, damage - defender.Defense / 2);
                    defender.TakeDamage(damage);
                    battleMessage = $"{attacker.Name} deals {actualDamage} damage with {move.Name}!";
                    if (defender == playerCreature)
                        damageFlashTimer = 15;
                    else
                        opponentDamageFlashTimer = 15;
                    break;

                case MoveType.SpecialAttack:
                    int specialDamage = move.Power + attacker.Attack + random.Next(-10, 11);
                    int actualSpecialDamage = Math.Max(1, specialDamage - defender.Defense / 2);
                    defender.TakeDamage(specialDamage);
                    battleMessage = $"{attacker.Name} uses powerful {move.Name}! {actualSpecialDamage} damage!";
                    if (defender == playerCreature)
                        damageFlashTimer = 15;
                    else
                        opponentDamageFlashTimer = 15;
                    break;

                case MoveType.Heal:
                    int healAmount = move.Power;
                    attacker.Heal(healAmount);
                    battleMessage = $"{attacker.Name} uses {move.Name}! Heals {healAmount} HP!";
                    break;

                case MoveType.Defense:
                    attacker.Defense += 5;
                    battleMessage = $"{attacker.Name} uses {move.Name}! Defense increased!";
                    break;
            }

            this.Invalidate();
        }

        private void BattleForm_KeyDown(object? sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape)
                this.Close();
        }
    }

    // Main program
    static class Program
    {
        [STAThread]
        static void Main()
        {
            ApplicationConfiguration.Initialize();
            Application.Run(new BattleForm());
        }
    }
}