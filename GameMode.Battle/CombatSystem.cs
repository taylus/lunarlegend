﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;

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

    private CombatSystemState currentState;
    private CombatSystemState previousState;
    private MessageBox dialogue;
    private MenuBox mainMenu;
    private MenuBox skills;
    private MenuBox inventory;
    private PowerMeter powerMeter;
    private Texture2D background;

    private List<PlayerCombatEntity> playerParty = new List<PlayerCombatEntity>();
    private List<EnemyCombatEntity> enemyParty = new List<EnemyCombatEntity>();

    private PlayerCombatEntity firstLivingPlayer { get { return playerParty.Where(p => p.IsAlive).First(); } }
    private int firstLivingPlayerIndex { get { return playerParty.IndexOf(firstLivingPlayer); } }
    private PlayerCombatEntity lastLivingPlayer { get { return playerParty.Where(p => p.IsAlive).Last(); } }
    private int lastLivingPlayerIndex { get { return playerParty.IndexOf(lastLivingPlayer); } }

    //the player party member who is currently issuing commands
    private int currentPlayerIndex = -1;
    private PlayerCombatEntity currentPlayer { get { return playerParty[currentPlayerIndex]; } }

    //the enemy that is currently issuing commands
    private int currentEnemyIndex = 0;
    private EnemyCombatEntity currentEnemy { get { return enemyParty[currentEnemyIndex]; } }

    //vertical offset (in pixels) to push the current player's status box up when it's their turn
    private const int CURRENT_PLAYER_OFFSET = 20;

    //the player party member that the player is currently targeting
    private int playerTargetIndex = -1;
    private PlayerCombatEntity playerTarget { get { return playerParty[playerTargetIndex]; } }

    //the enemy that the player is currently targeting
    private int enemyTargetIndex = 0;
    private EnemyCombatEntity enemyTarget { get { return enemyParty[enemyTargetIndex]; } }

    private Technique selectedTechnique = null;

    public CombatSystem(List<PlayerCombatEntity> players)
    {
        if (players == null || players.Count <= 0)
            throw new ArgumentException("Attempted to initialize combat system with empty player party.");

        playerParty = players;
        AlignPlayers(playerParty);
    }

    public void Engage(string bgFile, List<EnemyCombatEntity> enemies)
    {
        if (enemies == null || enemies.Count <= 0)
            throw new ArgumentException("Attempted to enter combat with empty enemy party.");

        background = BaseGame.LoadTexture(bgFile, true);
        enemyParty = enemies;
        AlignEnemies(enemyParty);
        mainMenu = new MenuBox(BattleDemo.CreateMainMenuBoxTemplate(), "Attack", "Defend", "Magic", "Items");
        dialogue = new MessageBox(BattleDemo.CreateMessageBoxTemplate(), GetEngagementText());
        powerMeter = BattleDemo.CreatePowerMeter();
        SetState(CombatSystemState.TEXT);
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
        sb.Draw(background, BaseGame.GameWindow, Color.White);

        foreach (EnemyCombatEntity enemy in enemyParty)
        {
            //player is selecting an enemy target:
            //draw a solid overlay on the target(s) he is NOT selecting
            //draw a blinking overlay on the target he is selecting
            if (currentState == CombatSystemState.SELECT_ENEMY_TARGET)
            {
                enemy.DrawOverlay = true;
                enemy.Overlay.Color = Color.Black;
                enemy.Overlay.BlinkEnabled = (enemyTarget == enemy);
            }
            else if (currentState == CombatSystemState.ENEMY_ACT && enemy == currentEnemy)
            {
                enemy.DrawOverlay = true;
                enemy.Overlay.Color = Color.Gray;
                enemy.Overlay.BlinkEnabled = true;
            }
            else
            {
                enemy.DrawOverlay = false;
            }

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
    }

    public void Update(GameTime currentGameTime)
    {
        powerMeter.Update();

        foreach (EnemyCombatEntity enemy in enemyParty)
        {
            enemy.Update(currentGameTime);
        }
    }

    public void ConfirmKeyPressed()
    {
        switch(currentState)
        {
            case CombatSystemState.BATTLE_OVER:
            {
                Environment.Exit(0);
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
                        SetState(CombatSystemState.ENEMY_ACT);
                    }
                }
                break;
            }
            case CombatSystemState.ENEMY_ACT:
            {
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

        //check victory or defeat conditions in every state
        if (PlayerVictory())
        {
            //victory: get exp, items, then go back to overworld
            dialogue.Text += "\nA winner is you!";
            SetState(CombatSystemState.BATTLE_OVER);
        }
        else if (PlayerDefeat())
        {
            //defeat: restart at last save...
            dialogue.Text += "\nYour party has perished...";
            SetState(CombatSystemState.BATTLE_OVER);
        }
    }

    public void LeftKeyPressed()
    {
        switch (currentState)
        {
            case CombatSystemState.MENU_SELECT:
                mainMenu.SelectLeftChoice();
                break;
            case CombatSystemState.SELECT_ENEMY_TARGET:
                enemyTargetIndex--;
                if (enemyTargetIndex < 0) enemyTargetIndex = 0;
                enemyTarget.Overlay.Reset();
                dialogue.Text = GetEnemyTargetText(enemyTarget);
                break;
        }
    }

    public void RightKeyPressed()
    {
        switch (currentState)
        {
            case CombatSystemState.MENU_SELECT:
                mainMenu.SelectRightChoice();
                break;
            case CombatSystemState.SELECT_ENEMY_TARGET:
                enemyTargetIndex++;
                if (enemyTargetIndex >= enemyParty.Count) enemyTargetIndex = enemyParty.Count - 1;
                enemyTarget.Overlay.Reset();
                dialogue.Text = GetEnemyTargetText(enemyTarget);
                break;
        }
    }

    public void UpKeyPressed()
    {
        switch (currentState)
        {
            case CombatSystemState.MENU_SELECT:
                mainMenu.SelectAboveChoice();
                break;
        }
    }

    public void DownKeyPressed()
    {
        switch (currentState)
        {
            case CombatSystemState.MENU_SELECT:
                mainMenu.SelectBelowChoice();
                break;
        }
    }

    public void CancelKeyPressed()
    {
        switch (currentState)
        {
            case CombatSystemState.SELECT_ENEMY_TARGET:
                dialogue.Text = "";
                SetState(previousState);
                break;
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
    }

    //utility method to advance to a new current player
    private void SetCurrentPlayer(int index)
    {
        //index of -1 is a flag value to indicate that there is no current player
        if (index < -1 || index >= playerParty.Count)
            throw new ArgumentException(string.Format("Player index {0} out of bounds for party size {1}", index, playerParty.Count));

        //restores the last player's status box position, and moves the new one
        if(currentPlayerIndex >= 0) currentPlayer.StatusBox.Y += CURRENT_PLAYER_OFFSET;
        currentPlayerIndex = index;
        if (currentPlayerIndex >= 0) currentPlayer.StatusBox.Y -= CURRENT_PLAYER_OFFSET;
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
        //TODO: generalize for multiple players and screen resolutions
        //foreach (PlayerCombatEntity player in players)
        //{
        //    int x = BaseGame.GameWindow.Center.X - (player.StatusBox.Width / 2);
        //    int y = BaseGame.GameWindow.Height - (player.StatusBox.Height + player.StatusBox.Margin);
        //    player.StatusBox.MoveTo(x, y);
        //}
        players[0].StatusBox.MoveTo(220, 492);
        players[1].StatusBox.MoveTo(420, 492);
    }
}