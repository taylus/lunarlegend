using System;
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
        BATTLE_OVER             //battle is finished (either victory or defeat)
    }

    private CombatSystemState currentState;
    private CombatSystemState previousState;
    private MessageBox dialogue;
    private MenuBox mainMenu;
    private MenuBox skills;
    private MenuBox inventory;
    private Texture2D background;

    private List<PlayerCombatEntity> playerParty = new List<PlayerCombatEntity>();
    private List<EnemyCombatEntity> enemyParty = new List<EnemyCombatEntity>();
    private int playerTargetIndex;
    private int enemyTargetIndex;
    private PlayerCombatEntity playerTarget { get { return playerParty[playerTargetIndex]; } }
    private EnemyCombatEntity enemyTarget { get { return enemyParty[enemyTargetIndex]; } }

    private PlayerCombatEntity currentPlayer;
    private Technique selectedTechnique;

    public CombatSystem(List<PlayerCombatEntity> players)
    {
        playerParty = players;
        AlignPlayers(playerParty);

    }

    public void Engage(string bgFile, List<EnemyCombatEntity> enemies)
    {
        background = BaseGame.LoadTexture(bgFile, true);

        if (enemies == null || enemies.Count <= 0)
            throw new ArgumentException("Attempted to enter combat with empty enemy party.");

        currentPlayer = playerParty.First();
        enemyParty = enemies;
        enemyTargetIndex = 0;
        AlignEnemies(enemyParty);
        mainMenu = new MenuBox(BattleDemo.CreateMainMenuBoxTemplate(), "Attack", "Defend", "Magic", "Items");
        dialogue = new MessageBox(BattleDemo.CreateMessageBoxTemplate(), GetEngagementText());
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

        foreach (CombatEntity enemy in enemyParty)
        {
            enemy.Draw(sb, currentState == CombatSystemState.SELECT_ENEMY_TARGET && enemyTarget != enemy);
        }

        foreach (CombatEntity player in playerParty)
        {
            player.Draw(sb, currentState == CombatSystemState.SELECT_PLAYER_TARGET && playerTarget != player);
        }

        dialogue.Draw(sb);
        mainMenu.Draw(sb);
    }

    public void Update()
    {

    }

    public void ConfirmKeyPressed()
    {
        switch(currentState)
        {
            case CombatSystemState.BATTLE_OVER:
                Environment.Exit(0);
                break;
            case CombatSystemState.TEXT:
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
                    dialogue.Text = "";
                    SetState(CombatSystemState.MENU_SELECT);
                }
                break;
            case CombatSystemState.MENU_SELECT:
                if (mainMenu.SelectedText == "Attack")
                {
                    SetState(CombatSystemState.SELECT_ENEMY_TARGET);
                    dialogue.Text = GetEnemyTargetText(enemyTarget);
                }
                break;
            case CombatSystemState.SELECT_ENEMY_TARGET:
                if (selectedTechnique == null)
                {
                    //normal attack the enemy
                    SetState(CombatSystemState.MENU_SELECT);
                    uint damageDone = currentPlayer.Attack(enemyTarget);

                    if (enemyTarget.IsDead)
                    {
                        //TODO: draw damage numbers directly on the target, instead of displaying it in the dialogue box?
                        dialogue.Text = string.Format("{0} attacks {1} for {2} damage!\n{1} is defeated!", currentPlayer.Name, enemyTarget.Name, damageDone);
                        enemyParty.Remove(enemyTarget);
                        enemyTargetIndex = 0;
                    }
                    else
                    {
                        //TODO: draw damage numbers directly on the target, instead of displaying it in the dialogue box?
                        dialogue.Text = string.Format("{0} attacks {1} for {2} damage!", currentPlayer.Name, enemyTarget.Name, damageDone);
                    }
                }
                else
                {
                    //use technique on the enemy
                }
                break;
        }

        //check victory or defeat conditions in every state
        if (PlayerVictory())
        {
            //victory: get exp, items, then go back to overworld
            dialogue.Text = string.Format("A winner is you!");
            SetState(CombatSystemState.BATTLE_OVER);
        }
        else if (PlayerDefeat())
        {
            //defeat: restart at last save...
            dialogue.Text = string.Format("Your party has perished...");
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
        previousState = currentState;
        currentState = state;

        mainMenu.IsActive = (currentState == CombatSystemState.MENU_SELECT);
    }

    //TODO: generalize these two functions into one that aligns any number of CombatEntities
    //within a given rectangle, that will be determined differently for enemies and players

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

//represents a player or enemy's action for one turn
public class CombatAction
{
    public string Description;
    public SoundEffect Sound;
    public Color? ScreenFlash;
    public CombatEntity Source;
    public CombatEntity Target;
    public Technique Technique;

    public void Perform()
    {
        if (Technique != null)
        {
            Source.Attack(Target, Technique);
        }
        else
        {
            Source.Attack(Target);
        }
    }

    public CombatAction(CombatEntity source, CombatEntity target)
    {
        Source = source;
        Target = target;

        if (Technique == null) 
            Description = string.Format("{0} attacks {1}!", source.Name, target.Name);
        else 
            Description = string.Format("{0} casts {1} on {2}!", source.Name, Technique.Name, target.Name);
    }
}