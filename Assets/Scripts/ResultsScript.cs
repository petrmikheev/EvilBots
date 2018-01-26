using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ResultsScript : MonoBehaviour {

	public Text winnerText;
	public Text titleText;
	public Text scoreText;
	public Text killText;
	public Text deathText;

	void OnEnable() {
		int maxScore = -1;
		string winner = "";
		string titles = "";
		string kills = "";
		string deaths = "";
		string scores = "";
		foreach (GameObject o in GameManager.Bots) {
			BotControl b = o.GetComponent<BotControl> ();
			if (b.Score > maxScore) {
				maxScore = b.Score;
				winner = o.name;
			}
			titles += "\n" + o.name;
			scores += "\n" + b.Score;
			kills += "\n" + b.Kills;
			deaths += "\n" + b.Deaths;
		}
		winnerText.text = "Winner: " + winner;
		titleText.text = "<b>Name</b>" + titles;
		killText.text = "<b>Kill</b>" + kills;
		deathText.text = "<b>Death</b>" + deaths;
		scoreText.text = "<b>Score</b>" + scores;
	}
}
