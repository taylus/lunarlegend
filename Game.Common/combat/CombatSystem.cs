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
    private CombatSystemState previousState;
    private MessageBox dialogue;
    private MenuBox mainMenu;
    private MenuBox skills;
    private MenuBox inventory;
    private PowerMeter powerMeter;
    private Texture2D backgroundImage;
    private ScreenOverlay backgroundOverlay;

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
        mainMenu = new MenuBox(CreateMainMenuBoxTemplate(), "Attack", "Defend", "Magic", "Items");
        dialogue = new MessageBox(CreateMessageBoxTemplate(), GetEngagementText());
        powerMeter = CreatePowerMeter();

        //fail gracefully in case we entered the battle with a dead party or something else weird
        CheckVictoryOrDefeat();
        if (currentState != CombatSystemState.BATTLE_OVER)
        {
            SetState(CombatSystemState.TEXT);
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
                    if (!enemy.HasSpriteEffects) enemy.StartBlink();
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
                    //main menu transition
                    SetCurrentPlayer(firstLivingPlayerIndex);
                    dialogue.Text = "";
                    mainMenu.ResetSelection();
                    //mainMenu.Visible = true;
                    SetState(CombatSystemState.MENU_SELECT);
                }
                break;
            }
            case CombatSystemState.MENU_SELECT:
            {
                if (mainMenu.SelectedText == "Attack")
                {
                    SetState(CombatSystemState.SELECT_ENEMY_TARGET);
                    dialogue.Text = GetEnemyTargetText(enemyTarget);
                }
                break;
            }
            case CombatSystemState.SELECT_ENEMY_TARGET:
            {
                if (selectedTechnique == null)
                {
                    //normal attack
                    SetState(CombatSystemState.POWER_METER);
                    powerMeter.IsActive = powerMeter.Visible = true;
                }
                else
                {
                    //special technique
                    //TODO: load technique's PowerMeterProfile(s)
                }
                break;
            }
            case CombatSystemState.POWER_METER:
            {
                PowerMeterResult result = powerMeter.ConfirmCursor();
                if (!powerMeter.Advance())
                {
                    //at the end of the meter's levels; stop the meter and deal the damage
                    powerMeter.IsActive = false;
                    powerMeter.Visible = false;

                    currentPlayer.CriticalDamageModifier = powerMeter.DamageModifier;
                    CombatAction action = new CombatAction(currentPlayer, enemyTarget);
                    uint damageDone = action.Execute();

                    if (enemyTarget.IsDead)
                    {
                        //TODO: draw damage numbers directly on the target, instead of displaying it as dialogue
                        dialogue.Text = string.Format("{0} attacks {1} for {2} damage!\n{1} is defeated!", currentPlayer.Name, enemyTarget.FullName, damageDone);
                        enemyParty.Remove(enemyTarget);
                        enemyTargetIndex = 0;
                    }
                    else
                    {
                        //TODO: draw damage numbers directly on the target, instead of displaying it as dialogue
                        dialogue.Text = string.Format("{0} attacks {1} for {2} damage!", currentPlayer.Name, enemyTarget.FullName, damageDone);
                    }
                    powerMeter.Reset();

                    if (currentPlayerIndex < lastLivingPlayerIndex)
                    {
                        //advance to the next player
                        SetCurrentPlayer(GetNextLivingPlayerIndex(currentPlayerIndex));
                        SetState(CombatSystemState.MENU_SELECT);
                    }
                    else
                    {
                        //enemy turn
                        SetCurrentPlayer(-1);
                        currentEnemyIndex = 0;
                        SetState(CombatSystemState.ENEMY_ACT);
                    }
                }
                break;
            }
            case CombatSystemState.ENEMY_ACT:
            {
                //TODO: fix bug with flashing enemy being ahead by one because of the way the current enemy index is set

                CombatAction enemyAction = currentEnemy.DecideAction(enemyParty, playerParty);
                CombatEntity target = enemyAction.Target;
                uint damageDone = enemyAction.Execute();

                if (target.IsDead)
                {
                    dialogue.Text = string.Format("{0} attacks {1} for {2} damage!\n{1} is defeated!", currentEnemy.FullName, target.Name, damageDone);
                }
                else
                {
                    dialogue.Text = string.Format("{0} attacks {1} for {2} damage!", currentEnemy.FullName, target.Name, damageDone);
                }

                if (currentEnemyIndex < enemyParty.Count - 1)
                {
                    //advance to the next enemy
                    currentEnemyIndex++;
                }
                else
                {
                    //player turn
                    currentEnemyIndex = 0;
                    SetState(CombatSystemState.TEXT);
                }
                break;
            }
        }

        CheckVictoryOrDefeat();
    }

    public void LeftKeyPressed()
    {
        if (!IsActive) return;

        switch (currentState)
        {
            case CombatSystemState.MENU_SELECT:
                mainMenu.SelectLeftChoice();
                break;
            case CombatSystemState.SELECT_ENEMY_TARGET:
                enemyTargetIndex--;
                if (enemyTargetIndex < 0) enemyTargetIndex = 0;
                dialogue.Text = GetEnemyTargetText(enemyTarget);
                break;
        }
    }

    public void RightKeyPressed()
    {
        if (!IsActive) return;

        switch (currentState)
        {
            case CombatSystemState.MENU_SELECT:
                mainMenu.SelectRightChoice();
                break;
            case CombatSystemState.SELECT_ENEMY_TARGET:
                enemyTargetIndex++;
                if (enemyTargetIndex >= enemyParty.Count) enemyTargetIndex = enemyParty.Count - 1;
                dialogue.Text = GetEnemyTargetText(enemyTarget);
                break;
        }
    }

    public void UpKeyPressed()
    {
        if (!IsActive) return;

        switch (currentState)
        {
            case CombatSystemState.MENU_SELECT:
                mainMenu.SelectAboveChoice();
                break;
        }
    }

    public void DownKeyPressed()
    {
        if (!IsActive) return;

        switch (currentState)
        {
            case CombatSystemState.MENU_SELECT:
                mainMenu.SelectBelowChoice();
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
                SetState(previousState);
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
            SetState(CombatSystemState.BATTLE_OVER);
        }
        else if (PlayerDefeat())
        {
            //defeat: restart battle? restart at last save?
            dialogue.Text += "\nYour party has perished...";
            SetState(CombatSystemState.BATTLE_OVER);
        }
    }

    private string GetEnemyTargetText(EnemyCombatEntity enemy)
    {
        return string.Format("To: {0}", enemy.FullName);
    }

    private string GetEngagementText()
    {
        if (enemyParty.Count > 1)
            return string.Format("{0} and company attack!", enemyParty[0].Name);
        else
            return string.Format("{0} attacks!", enemyParty[0].Name);
    }

    private void SetState(CombatSystemState state)
    {
        //current and previous states should only be directly set here
        //wrap them in a class or something to enforce this?
        //TODO: maintain stack of states? (pushdown automata)
        previousState = currentState;
        currentState = state;

        mainMenu.IsActive = (currentState == CombatSystemState.MENU_SELECT);
        //UpdateEnemyTargetSelectionEffects();
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
    private MessageBox CreateMessageBoxTemplate()
    {
        MenuBox mainMenu = CreateMainMenuBoxTemplate();
        int w = BaseGame.GameWidth - mainMenu.Width - (2 * BOX_SCREEN_MARGIN);
        int h = 4;
        int x = mainMenu.X + mainMenu.Width - mainMenu.BorderWidth;
        int y = mainMenu.Y;
        return new MessageBox(x, y, w, h, BaseGame.Font);
    }

    private MenuBox CreateMainMenuBoxTemplate()
    {
        int w = 75;
        int h = 4;
        int cols = 1;
        int x = BOX_SCREEN_MARGIN;
        int y = BOX_SCREEN_MARGIN;
        return new MenuBox(x, y, w, h, cols, BaseGame.Font);
    }

    private PowerMeter CreatePowerMeter()
    {
        int w = 350;
        int h = 40;
        int x = 225;
        int y = 420;
        PowerMeter pm = new PowerMeter(x, y, w, h);
        //TODO: make specific profile layouts go with different attacks
        pm.Profiles.Add(new PowerMeterProfile("XX======XX", 6.0f));
        //pm.Profiles.Add(new PowerMeterProfile("===-XX-===", 6.0f));
        pm.IsActive = pm.Visible = false;
        return pm;
    }
}