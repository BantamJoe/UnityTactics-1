﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Player : MonoBehaviour {
	public Vector2 gridPosition = Vector2.zero;

	public Vector3 moveDestination;
	public float moveSpeed = 10.0f;

	public bool attackingPhase = false;
	public bool movingPhase = false;
	public bool highlighted = false;
	public bool marked = false;

	public string playerName = "Default";
	public int HP = 25;
	public int MaxHP = 25;

	public float attackChance = 0.75f;
	public float damageReduction = 0.15f;
	public int damageBase = 5;
	public int damageRollSides = 6;

	public int actionPoints;
	public int movePoints;
	public int attackRange = 1;

	public int startingActionPoints = 2;
	public int startingMovePoints = 5;

	//movement animation
	public List<Vector3> positionQueue = new List<Vector3>();

	bool mouseOverPlayer = false;

	void Awake () {
		moveDestination = transform.position;
		RefreshPoints ();
	}

	// Use this for initialization
	void Start () {
		HP = MaxHP;
	}
	
	// Update is called once per frame
	void Update () {
		if (GameManager.instance.players [GameManager.instance.currentPlayerIndex] == this) {
			transform.GetComponent<Renderer> ().material.color = Color.green;
		} else {
			transform.GetComponent<Renderer> ().material.color = Color.white;
		}
		if (HP < 0) {
			HP = 0;
		}
		if (HP == 0) {
			transform.rotation = Quaternion.Euler(new Vector3(90,0,0));
			transform.GetComponent<Renderer> ().material.color = Color.red;
		}
	}

	void OnMouseOver(){
		mouseOverPlayer = true;
		GameManager.instance.getTileByGridPosition (gridPosition).OnMouseOver ();
	}

	void OnMouseExit(){
		GameManager.instance.removeTileHighlights ();
		if (GameManager.instance.players [GameManager.instance.currentPlayerIndex].attackingPhase) {
			GameManager.instance.players [GameManager.instance.currentPlayerIndex].startAttackPhase();
		} else if (GameManager.instance.players [GameManager.instance.currentPlayerIndex].movingPhase) {
			GameManager.instance.players [GameManager.instance.currentPlayerIndex].startMovePhase();
		}
		mouseOverPlayer = false;
		GameManager.instance.getTileByGridPosition (gridPosition).OnMouseExit ();
	}

	void OnMouseDown(){
		Player instancePlayer = GameManager.instance.players [GameManager.instance.currentPlayerIndex];
		if (instancePlayer.attackingPhase && instancePlayer.actionPoints > 0) {;
			GameManager.instance.attackWithCurrentPlayer(GameManager.instance.getTileByGridPosition(gridPosition));
		}
	}

	public virtual void OnGUI(){
		float buttonHeight = 50;
		float buttonWidth = 150;
		if (mouseOverPlayer) {
			//Show mouseOver Statistics
			Rect playerAttributesRect = new Rect (Screen.width - buttonWidth, 0, buttonWidth, buttonHeight * 2);
			GUI.TextArea (playerAttributesRect, 
			           "Name: " + playerName + "\n" +
				"HP: " + HP + "/" + MaxHP + "\n" +
				"Damage: " + damageBase + " + 1d" + damageRollSides + "\n" +
				"Precision: " + attackChance * 100 + "%\n" +
				"Damage Reduction: " + damageReduction * 100 + "%\n" +
			    "Range: " + attackRange
			);
			Rect textRect = new Rect (Screen.width - buttonWidth,buttonHeight * 2, buttonWidth, buttonHeight);
			GUI.TextArea (textRect, "Action Points: " + startingActionPoints + "\nMove Points: " + startingMovePoints);

			GameManager.instance.highlightTilesAt(gridPosition,GameManager.targetAttackColor,attackRange);
		}
		// show HP BEGIN
		int width = 40;
		if (MaxHP > 100) width += 20;
		Vector3 location = Camera.main.WorldToScreenPoint (transform.position) + Vector3.up * 35;
		GUI.TextArea (new Rect(location.x,Screen.height - location.y, width, 20),(HP.ToString() + "/" + MaxHP.ToString()));
		// show HP END
	}

	public virtual void TurnOnGUI () {
		
	}

	public void startMovePhase(){
		GameManager.instance.removeTileHighlights();
		movingPhase = true;
		attackingPhase = false;
		GameManager.instance.highlightTilesAt(gridPosition, GameManager.targetMoveColor, GameManager.instance.players [GameManager.instance.currentPlayerIndex].movePoints);
	}

	public void startAttackPhase(){
		GameManager.instance.removeTileHighlights();
		movingPhase = false;
		attackingPhase = true;
		GameManager.instance.highlightTilesAt(gridPosition, GameManager.targetAttackColor, GameManager.instance.players [GameManager.instance.currentPlayerIndex].attackRange);
	}

	public virtual void endPlayerTurn(){
		GameManager.instance.removeTileHighlights();
		RefreshPoints();
		positionQueue.Clear();
		movingPhase = false;
		attackingPhase = false;
		setPlayerPositionImpassable ();
	}

	public void RefreshPoints () {
		actionPoints = startingActionPoints;
		movePoints = startingMovePoints;
	}
	
	public void SetStartingActionPoints (int startingPoints) {
		startingActionPoints = startingPoints;
	}
	
	public void SetStartingMovingPoints (int startingPoints) {
		startingMovePoints = startingPoints;
	}
	
	public virtual void TurnUpdate () {
		
	}

	public void updatePositionQueue () {
		if (Vector3.Distance (positionQueue [0], transform.position) > 0.1f) {
			transform.position += (positionQueue [0] - transform.position).normalized * moveSpeed * Time.deltaTime;
			
			if (Vector3.Distance (positionQueue [0], transform.position) <= 0.1f) {
				transform.position = positionQueue [0];
				positionQueue.RemoveAt (0);
				movePoints--;
			}
		}
	}

	public Tile getPlayerTile(){
		return GameManager.instance.getTileByGridPosition (gridPosition);
	}

	public void setPlayerPositionPassable(){
		GameManager.instance.getTileByGridPosition (gridPosition).impassable = false;
	}

	public void setPlayerPositionImpassable(){
		GameManager.instance.getTileByGridPosition(gridPosition).impassable = true;
	}

	public Player getPriorityTarget( List<Tile> tilesOfOpponentsInRange){
		/* PriorityTarget
		 * 	1- Lowest HP
		 *  2- Closest Player
		 */
		Player targetPlayer = null;
		int lowestHP = 0;
		if (tilesOfOpponentsInRange.Count == 0) {
			return targetPlayer;
		} else {
			foreach(Tile t in tilesOfOpponentsInRange)
			{
				if( targetPlayer == null) {
					targetPlayer = GameManager.instance.getPlayerByTile(t);
				} else if(targetPlayer.HP > GameManager.instance.getPlayerByTile(t).HP){
					targetPlayer = GameManager.instance.getPlayerByTile(t);
					lowestHP = targetPlayer.HP;
				} else if((targetPlayer.HP == GameManager.instance.getPlayerByTile(t).HP) && targetPlayer.HP <= lowestHP){
					List<Player> opponentsWithSameHP = new List<Player>();
					foreach (Tile tile in tilesOfOpponentsInRange){
						if(targetPlayer.HP == GameManager.instance.getPlayerByTile(tile).HP){
							opponentsWithSameHP.Add(GameManager.instance.getPlayerByTile(tile));
						}
						targetPlayer = getClosestPlayer(opponentsWithSameHP);
					}
				}
			}
		}
		return targetPlayer;
	}

	public Player getClosestPlayer(List<Player> players){
		Player closest = null;
		int distance = -1;
		foreach(Player p in players){
			if (p == this) continue;
			if (p.HP <= 0) continue;
			if (this.GetType() == p.GetType()) continue;
			if (distance > 0) {
				if(GameManager.getDistanceByTiles(this.getPlayerTile(),p.getPlayerTile()) < distance) {
					distance = GameManager.getDistanceByTiles(this.getPlayerTile(),p.getPlayerTile());
					closest = p;
				}
			} else {
				distance = GameManager.getDistanceByTiles(this.getPlayerTile(),p.getPlayerTile());
				closest = p;
			}
		}
		return closest;
	}
	
	public void moveToAttackRange(Player target){
		Tile destTile =  target.getPlayerTile().neighbors[0];
		foreach (Tile t in target.getPlayerTile().neighbors) {
			if( Vector3.Distance(transform.position, t.transform.position) < Vector3.Distance(transform.position,destTile.transform.position)){
				destTile = t;
			}
		}
		List<Tile> tiles = TilePathFinder.FindPath (getPlayerTile (), destTile);
		for(int i=1; i < this.attackRange; i++) {
			tiles.RemoveAt(tiles.Count-1);
		}
		destTile = tiles[tiles.Count-1];
		GameManager.instance.moveCurrentPlayer (destTile);
	}
	
	public void moveToClosestPositionFromOpponent(Player target){
		Tile destTile =  target.getPlayerTile().neighbors[0];
		foreach (Tile t in target.getPlayerTile().neighbors) {
			if( Vector3.Distance(transform.position, t.transform.position) < Vector3.Distance(transform.position,destTile.transform.position)){
				destTile = t;
			}
		}
		GameManager.instance.moveCurrentPlayer (destTile);
	}

	public void moveToFartherPositionFromOpponent(Player target){
		List<Tile> highlightedTiles = TileHighligth.FindHighlight (getPlayerTile (), movePoints);
		Tile destTile =  getPlayerTile();
		foreach (Tile t in highlightedTiles) {
			if( Vector3.Distance(target.transform.position, t.transform.position) > Vector3.Distance(target.transform.position,destTile.transform.position)){
				destTile = t;
			}
		}
		GameManager.instance.moveCurrentPlayer (destTile);
	}

	public void doDamageTo(Player target){
		int amountOfDamage = this.damageBase + Random.Range(1,this.damageRollSides);
		Damage damage = new Damage(amountOfDamage,DamageType.CONTUSION);
		Damage.doDamageTo (target, damage);
	}

}
