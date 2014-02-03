using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;

//callback functions for when combat ends
public delegate void VictoryCallback();
public delegate void DefeatCallback();

//represents a turn-based battle between the player's party and an AI-controlled enemy party
//handles adding/removing CombatEntities from the fight (e.g. a monster summons allies)
//detects victory or defeat conditions for transition to appropriate game state
public class CombatSystem
{
    //the current state of the battle; determines how input is routed
    private enum CombatSystemState
    {
        TEXT,                   //displaying text the player must advance through
        MENU_SELECT,            //selecting an item from a menu (main menu, skills, items)
        SELECT_PLAYER_TARGET,   //selecting a player as the target of an action
        SELECT_ENEMY_TARGET,    //selecting an enemy as the target of an action
        POWER_METER,            //performing a power meter to determine action strength
        ENEMY_ACT,              //enemy is deciding what to do
        BATTLE_OVER             //battle is finished (either victory or defeat)
    }

    public bool IsActive = false;
    private CombatSystemState currentState;

    private MessageBox dialogue;
    private BaseMenuBox currentMenu;
    private MenuBox<string> mainMenu;
    private MenuBox<Technique> techMenu;
    //private MenuBox<Item> itemMenu;

    private PowerMeter powerMeter;
    private Texture2D backgroundImage;
    private ScreenOverlay backgroundOverlay;

    //TODO: combine list of players, inventory, gold into single class, to be shared between combat system and overworld menus
    private List<PlayerCombatEntity> playerParty = new List<PlayerCombatEntity>();
    private List<EnemyCombatEntity> enemyParty = new List<EnemyCombatEntity>();

    private PlayerCombatEntity firstLivingPlayer { get { return playerParty.Where(p => p.IsAlive).First(); } }
    private int firstLivingPlayerIndex { get { return playerParty.IndexOf(firstLivingPlayer); } }
    private PlayerCombatEntity lastLivingPlayer { get { return playerParty.Where(p => p.IsAlive).Last(); } }
    private int lastLivingPlayerIndex { get { return playerParty.IndexOf(lastLivingPlayer); } }

    //the player party member who is currently issuing commands
    private int currentPlayerIndex = -1;
    private PlayerCombatEntity currentPlayer { get { return currentPlayerIndex < 0? null : playerParty[currentPlayerIndex]; } }

    //the enemy that is currently issuing commands
    private int currentEnemyIndex = 0;
    private EnemyCombatEntity currentEnemy { get { return currentEnemyIndex < 0? null : enemyParty[currentEnemyIndex]; } }

    //the player party member that the player is currently targeting
    private int playerTargetIndex = -1;
    private PlayerCombatEntity playerTarget { get { return playerTargetIndex < 0? null : playerParty[playerTargetIndex]; } }

    //the enemy that the player is currently targeting
    private int enemyTargetIndex = 0;
    private EnemyCombatEntity enemyTarget { get { return enemyTargetIndex < 0? null : enemyParty[enemyTargetIndex]; } }

    private Technique selectedTechnique = null;
    private const int BOX_SCREEN_MARGIN = 8;

    public VictoryCallback OnVictory { get; set; }
    public DefeatCallback OnDefeat { get; set; }

    //power meter used for normal attacks
    private static readonly PowerMeterPattern DEFAULT_POWER_METER_PATTERN = new PowerMeterPattern("XX======XX", 6.0f);

    public CombatSystem(List<PlayerCombatEntity> players)
    {
        if (players == null || players.Count <= 0)
            throw new ArgumentException("Attempted to initialize combat system with empty player party.");

        playerParty = players;
        AlignPlayers(playerParty);
        backgroundOverlay = new ScreenOverlay(Color.Black, 0.6f);
    }

    public void Engage(List<EnemyCombatEntity> enemies, string bgFile = null)
    {
        if (enemies == null || enemies.Count <= 0)
            throw new ArgumentException("Attempted to enter combat with empty enemy party.");

        IsActive = true;
        currentPlayerIndex = -1;
        currentEnemyIndex = 0;
        playerTargetIndex = -1;
        enemyTargetIndex = 0;
        if (!string.IsNullOrWhiteSpace(bgFile))
        {
            backgroundImage = BaseGame.LoadTexture(bgFile, true);
        }
        enemyParty = enemies;
        AlignEnemies(enemyParty);
        playerParty.ForEach(p => p.Restore());
        currentMenu = mainMenu = new MenuBox<string>(CreateMainMenuBoxTemplate(), "Attack", "Defend", "Magic", "Items") { IsActive = false };
        dialogue = new MessageBox(CreateMessageBoxTemplate(), GetEngagementText());
        techMenu = new MenuBox<Technique>(dialogue.X, dialogue.Y, dialogue.Width, 4, 3, BaseGame.Font, LoadTechniques()) { Visible = false };
        powerMeter = CreatePowerMeter();
        currentState = CombatSystemState.TEXT;

        //fail gracefully in case we entered the battle with a dead party or something else weird
        CheckVictoryOrDefeat();
        if (currentState != CombatSystemState.BATTLE_OVER)
        {
            currentState = CombatSystemState.TEXT;
        }
    }

    public bool PlayerVictory()
    {
        return enemyParty.All(e => e.IsDead);
    }

    public bool PlayerDefeat()
    {
        return playerParty.All(p => p.IsDead);
    }

    //are these two entities friendly to one another (on the same side)?
    public bool AreFriendly(CombatEntity ce1, CombatEntity ce2)
    {
        if (playerParty.Contains(ce1)) return playerParty.Contains(ce2);
        else if (enemyParty.Contains(ce1)) return enemyParty.Contains(ce2);
        throw new Exception("Unknown entity \"" + ce1.Name + "\"");
    }

    public bool AreHostile(CombatEntity ce1, CombatEntity ce2)
    {
        return !AreFriendly(ce1, ce2);
    }

    public void Draw(SpriteBatch sb)
    {
        if (!IsActive) return;

        if (backgroundImage != null)
        {
            //draw the background image if one was supplied
            sb.Draw(backgroundImage, BaseGame.GameWindow, Color.White);
        }
        else
        {
            //otherwise just darken the map background with an overlay
            backgroundOverlay.Draw(sb);
        }

        foreach (EnemyCombatEntity enemy in enemyParty)
        {
            enemy.Draw(sb);
        }

        foreach (PlayerCombatEntity player in playerParty)
        {
            bool grayedOut = (currentState == CombatSystemState.SELECT_PLAYER_TARGET && playerTarget != player);
            bool current = (currentPlayerIndex > 0 && currentPlayerIndex < playerParty.Count - 1 && currentPlayer == player);
            player.Draw(sb, grayedOut, current);
        }

        if(!string.IsNullOrWhiteSpace(dialogue.Text)) dialogue.Draw(sb);
        mainMenu.Draw(sb);
        techMenu.Draw(sb);
        powerMeter.Draw(sb);

        //sb.DrawString(BaseGame.Font, currentState.ToString(), new Vector2(mainMenu.X, mainMenu.Y + mainMenu.Height + 4), Color.White);
    }

    public void Update(GameTime currentGameTime)
    {
        if (!IsActive) return;

        powerMeter.Update();

        foreach (EnemyCombatEntity enemy in enemyParty)
        {
            enemy.Update(currentGameTime);

            //apply sprite effects
            //this is kinda lazy since this runs every update frame
            //but this avoids having duplicate code in several different state transitions

            //player is selecting an enemy target:
            if (currentState == CombatSystemState.SELECT_ENEMY_TARGET ||
                currentState == CombatSystemState.POWER_METER)
            {
                //blink the enemy he is selecting
                if (enemyTarget == enemy)
                {
                    if (!enemy.HasSpriteEffects) enemy.StartBlink(150);
                }
                //darken the others with a tint
                else
                {
                    enemy.Tint = Color.DarkGray;
                    enemy.StopBlink();
                }
            }
            //an enemy is acting:
            else if (currentState == CombatSystemState.ENEMY_ACT && enemy == currentEnemy)
            {
                //make it blink
                enemy.StartBlink(150);
            }
            //clear sprite effects in all other states
            else
            {
                enemy.Tint = Color.White;
                enemy.StopBlink();
            }
        }
    }

    public void ConfirmKeyPressed()
    {
        if (!IsActive) return;

        switch(currentState)
        {
            case CombatSystemState.BATTLE_OVER:
            {
                IsActive = false;
                if (PlayerVictory() && OnVictory != null) OnVictory();
                else if (PlayerDefeat() && OnDefeat != null) OnDefeat();
                break;
            }
            case CombatSystemState.TEXT:
            {
                if (dialogue.HasMoreLinesToDisplay)
                {
                    dialogue.AdvanceLines();
                }
                else if (dialogue.Next != null)
                {
                    dialogue = dialogue.Next;
                }
                else
                {
                    //transitioning from text to a player's turn
                    dialogue.Text = "";
                    mainMenu.IsActive = true;
                    AdvancePlayer();
                }
                break;
            }
            case CombatSystemState.MENU_SELECT:
            {
                if (!currentMenu.IsActive) break;

                if (currentMenu == mainMenu)
                {
                    if (mainMenu.SelectedText == "Attack")
                    {
                        selectedTechnique = null;
                        dialogue.Text = GetTargetText(enemyTarget);
                        currentState = CombatSystemState.SELECT_ENEMY_TARGET;
                    }
                    else if (mainMenu.SelectedText == "Magic")
                    {
                        techMenu.ResetSelection();
                        techMenu.Visible = true;
                        currentMenu = techMenu;
                        dialogue.Visible = false;
                    }
                }
                else if (currentMenu == techMenu)
                {
                    selectedTechnique = techMenu.SelectedValue;
                    if (selectedTechnique.GetType() == typeof(DamageTechnique))
                    {
                        techMenu.Visible = false;
                        dialogue.Visible = true;
                        dialogue.Text = GetTargetText(enemyTarget);
                        currentState = CombatSystemState.SELECT_ENEMY_TARGET;
                    }
                    else if (selectedTechnique.GetType() == typeof(HealTechnique) ||
                             selectedTechnique.GetType() == typeof(SupportTechnique))
                    {

                    }
                }
                break;
            }
            case CombatSystemState.SELECT_ENEMY_TARGET:
            {
                if (selectedTechnique == null)
                {
                    //load power meter for normal attack
                    powerMeter.Patterns = new List<PowerMeterPattern>() {DEFAULT_POWER_METER_PATTERN};
                }
                else
                {
                    //load power meter specific to special technique
                    powerMeter.Patterns = selectedTechnique.PowerMeterPatterns;
                }

                powerMeter.IsActive = powerMeter.Visible = true;
                currentState = CombatSystemState.POWER_METER;
                break;
            }
            case CombatSystemState.POWER_METER:
            {
                PowerMeterResult result = powerMeter.ConfirmCursor();
                if (result == PowerMeterResult.MISS || !powerMeter.Advance())
                {
                    //missed, or finished the last pattern in the set, so stop the meter and deal the damage
                    powerMeter.IsActive = false;
                    powerMeter.Visible = false;

                    currentPlayer.CriticalDamageModifier = powerMeter.DamageModifier;
                    CombatAction action = new CombatAction(currentPlayer, enemyTarget, selectedTechnique);
                    dialogue.Text = action.Execute();

                    if (enemyTarget.IsDead)
                    {
                        enemyParty.Remove(enemyTarget);
                        enemyTargetIndex = 0;
                    }

                    powerMeter.Reset();

                    //finish reading text; current player is advanced in that state
                    mainMenu.IsActive = false;
                    currentState = CombatSystemState.TEXT;
                }
                break;
            }
            case CombatSystemState.ENEMY_ACT:
            {
                CombatAction enemyAction = currentEnemy.DecideAction(enemyParty, playerParty);
                CombatEntity target = enemyAction.Target;
                dialogue.Text = enemyAction.Execute();

                if (currentEnemyIndex < enemyParty.Count - 1)
                {
                    //advance to the next enemy
                    currentEnemyIndex++;
                }
                else
                {
                    //player turn
                    currentEnemyIndex = 0;
                    currentState = CombatSystemState.TEXT;
                }
                break;
            }
        }

        CheckVictoryOrDefeat();
    }

    //a player has completed their turn
    //move onto the next, or start the enemy turn if it was the last player
    private void AdvancePlayer()
    {
        mainMenu.ResetSelection();
        if (currentPlayerIndex < lastLivingPlayerIndex)
        {
            //advance to the next player
            SetCurrentPlayer(GetNextLivingPlayerIndex(currentPlayerIndex));
            currentMenu = mainMenu;
            currentState = CombatSystemState.MENU_SELECT;
        }
        else
        {
            //enemy turn
            SetCurrentPlayer(-1);
            currentEnemyIndex = 0;
            currentState = CombatSystemState.ENEMY_ACT;
            mainMenu.IsActive = false;
        }
    }

    public void LeftKeyPressed()
    {
        if (!IsActive) return;

        switch (currentState)
        {
            case CombatSystemState.MENU_SELECT:
                currentMenu.SelectLeftChoice();
                break;
            case CombatSystemState.SELECT_ENEMY_TARGET:
                enemyTargetIndex--;
                if (enemyTargetIndex < 0) enemyTargetIndex = 0;
                dialogue.Text = GetTargetText(enemyTarget);
                break;
        }
    }

    public void RightKeyPressed()
    {
        if (!IsActive) return;

        switch (currentState)
        {
            case CombatSystemState.MENU_SELECT:
                currentMenu.SelectRightChoice();
                break;
            case CombatSystemState.SELECT_ENEMY_TARGET:
                enemyTargetIndex++;
                if (enemyTargetIndex >= enemyParty.Count) enemyTargetIndex = enemyParty.Count - 1;
                dialogue.Text = GetTargetText(enemyTarget);
                break;
        }
    }

    public void UpKeyPressed()
    {
        if (!IsActive) return;

        switch (currentState)
        {
            case CombatSystemState.MENU_SELECT:
                currentMenu.SelectAboveChoice();
                break;
        }
    }

    public void DownKeyPressed()
    {
        if (!IsActive) return;

        switch (currentState)
        {
            case CombatSystemState.MENU_SELECT:
                currentMenu.SelectBelowChoice();
                break;
        }
    }

    public void CancelKeyPressed()
    {
        if (!IsActive) return;

        switch (currentState)
        {
            case CombatSystemState.SELECT_ENEMY_TARGET:
                dialogue.Text = "";
                currentState = CombatSystemState.MENU_SELECT;
                if (currentMenu == techMenu)
                {
                    //cancelled while targetting a technique, go back to technique menu
                    techMenu.Visible = true;
                    dialogue.Visible = false;
                }
                break;
            case CombatSystemState.MENU_SELECT:
                if (currentMenu == techMenu)
                {
                    currentMenu = mainMenu;
                    techMenu.Visible = false;
                    dialogue.Visible = true;
                }
                break;
        }
    }

    //check for victory or defeat conditions and advance to the appropriate state
    public void CheckVictoryOrDefeat()
    {
        if (PlayerVictory())
        {
            //victory: get exp, items, then go back to overworld
            dialogue.Text += "\nA winner is you!";
            currentState = CombatSystemState.BATTLE_OVER;
        }
        else if (PlayerDefeat())
        {
            //defeat: restart battle? restart at last save?
            dialogue.Text += "\nYour party has perished...";
            currentState = CombatSystemState.BATTLE_OVER;
        }
    }

    private string GetTargetText(CombatEntity target)
    {
        return string.Format("To: {0}", target.FullName);
    }

    private string GetEngagementText()
    {
        if (enemyParty.Count > 1)
            return string.Format("{0} and company attack!", enemyParty[0].Name);
        else
            return string.Format("{0} attacks!", enemyParty[0].Name);
    }

    //utility method to advance to a new current player
    private void SetCurrentPlayer(int index)
    {
        //index of -1 is a flag value to indicate that there is no current player
        if (index < -1 || index >= playerParty.Count)
            throw new ArgumentException(string.Format("Player index {0} out of bounds for party size {1}", index, playerParty.Count));

        currentPlayerIndex = index;
        playerParty.ForEach(p => p.IsCurrent = (p == currentPlayer));
    }

    //return the next living player after the given index
    //returns -1 if there is none
    private int GetNextLivingPlayerIndex(int index)
    {
        //index of -1 is a flag value to indicate that there is no current player
        if (index < -1 || index >= playerParty.Count)
            throw new ArgumentException(string.Format("Player index {0} out of bounds for party size {1}", index, playerParty.Count));

        if (index >= lastLivingPlayerIndex)
        {
            return -1;
        }
        else
        {
            PlayerCombatEntity nextLivingPlayer = playerParty.Where(p => p.IsAlive && playerParty.IndexOf(p) > index).First();
            return playerParty.IndexOf(nextLivingPlayer);
        }
    }

    private void AlignEnemies(List<EnemyCombatEntity> enemies)
    {
        if (enemies.Count <= 0) return;

        //splits the screen into n slices, and centers each enemy in the slice
        //TODO: group them closer when there's only a few?
        int horizontalStep = BaseGame.GameWindow.Width / enemies.Count;
        for (int i = 0; i < enemies.Count; i++)
        {
            int x = (horizontalStep / 2) + (horizontalStep * i);
            int y = BaseGame.GameWindow.Center.Y;
            enemies[i].CenterOn(x, y);
        }
    }

    private void AlignPlayers(List<PlayerCombatEntity> players)
    {
        if (players.Count <= 0) return;

        //splits the screen into n slices, and centers each player in the slice
        int horizontalStep = BaseGame.GameWindow.Width / players.Count;
        for (int i = 0; i < players.Count; i++)
        {
            int x = (horizontalStep / 2) + (horizontalStep * i);
            int y = BaseGame.GameWindow.Height - (players[i].Height / 2) - BOX_SCREEN_MARGIN;
            players[i].CenterOn(x, y);
        }

        //TODO: change this to group players together better
        //divide the screen in half vertically, and center the player party 
        //on that, with a small margin between each player
    }

    //TODO: find a better place for these template methods to live
    private MenuBox<string> CreateMainMenuBoxTemplate()
    {
        int w = 75;
        int h = 4;
        int cols = 1;
        int x = BOX_SCREEN_MARGIN;
        int y = BOX_SCREEN_MARGIN;
        return new MenuBox<string>(x, y, w, h, cols, BaseGame.Font);
    }

    private MessageBox CreateMessageBoxTemplate()
    {
        MenuBox<string> mainMenu = CreateMainMenuBoxTemplate();
        int w = BaseGame.GameWidth - mainMenu.Width - (2 * BOX_SCREEN_MARGIN);
        int h = 4;
        int x = mainMenu.X + mainMenu.Width - mainMenu.BorderWidth;
        int y = mainMenu.Y;
        return new MessageBox(x, y, w, h, BaseGame.Font);
    }

    private PowerMeter CreatePowerMeter()
    {
        int w = 350;
        int h = 40;
        int x = 225;
        int y = 420;
        PowerMeter pm = new PowerMeter(x, y, w, h);
        pm.IsActive = pm.Visible = false;
        return pm;
    }

    private Technique[] LoadTechniques()
    {
        return new Technique[] 
        { 
            Techniques.LoadByName("Fireball"),
            Techniques.LoadByName("Firestorm"),
            Techniques.LoadByName("Hellfire"),
            new DamageTechnique("Frostbite", 10, DamageType.Water),
            new DamageTechnique("Spark", 10, DamageType.Wind),
            new DamageTechnique("Thunderstorm", 20, DamageType.Wind),
            new DamageTechnique("Boulder", 10, DamageType.Earth),
            new DamageTechnique("Earthquake", 20, DamageType.Earth),
            new HealTechnique("Heal", 20),
            new SupportTechnique("Empower"),
            new SupportTechnique("Chant"),
            new SupportTechnique("Fortify"),
        };
    }
}