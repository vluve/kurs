using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Game : MonoBehaviour {
	//объявление переменных для игровых объектов
	public GameObject spr;
	public GameObject cH1, cH2, cH3, cH4, cH5, cH6, cH7, cH8, cH9, cH10, cH11, cH12, cH13;
	public GameObject cD1, cD2, cD3, cD4, cD5, cD6, cD7, cD8, cD9, cD10, cD11, cD12, cD13;
	public GameObject cC1, cC2, cC3, cC4, cC5, cC6, cC7, cC8, cC9, cC10, cC11, cC12, cC13;
	public GameObject cS1, cS2, cS3, cS4, cS5, cS6, cS7, cS8, cS9, cS10, cS11, cS12, cS13;
	public GameObject cJ1, cJ2;
	public GameObject Canvas;
	public GameObject Caravan0Start, Caravan1Start, Caravan2Start, Caravan3Start, Caravan4Start, Caravan5Start;
	public GameObject GameOverScreen, Deck1, Deck2;
	GameObject[,] cards = new GameObject[5, 13];

	const int MAXHAND = 7;
	const int MAXCARAVAN = 6;
	const int DECKLEN = 52; 
	const int MAXKINGS = 3;

	// структура игральной карты
	struct card {
		public int value; // 1 - 10 соответственно, 11 - валет, 12 - дама, 13 - король
		public int suit; // черви - 0, бубна - 1, трефы - 2, пика - 3, джокер - 4
		public int numberOfCCards; // сколько карт привязаны к этой карте
		public int numberOfQueens; //сколько из привязанных карт дам
		public GameObject[] connectedCards; // массив объектов привязанных карт
		public GameObject obj; // объект-карта (картинка)
	};

	// структура одного каравана
	struct caravan {
		public int value; // стоимость каравана
		public card[] cards; // массив карт в караване
		public bool isComplete; // истина если караван собран, ложь если нет
		public int numberOfCards; // количество карт в караване
		public bool isIncreasing; // true - возрастает, false - убывает
		public GameObject CaravanStart; //объект-начало каравана
	};

	//структура колонны
	struct column {
		public bool isWon; // колонна выиграна
		public bool isLost; // колонна проиграна
		public int howMuchToComplete; // сколько нужно до завершения
		public int suit; // масть колонны (со стороны ИИ)
	};

	caravan[] gameField = new caravan[6]; // структура игрового поля (три каравана игрока(чётные), три каравана соперника(нечётные))
	GameObject prevClickedCard;
	card[] handP1, handP2, d1, d2;
	int d_i1, d_i2, n;
	bool wasCardPlaced;
	bool isGameRunning, isD1Empty, isD2Empty;

	//пересоздаёт объект crdObj, возвращает новый объект
	GameObject ReInstantiateCard(GameObject crdObj) {
		GameObject newObj = GameObject.Instantiate(crdObj, Canvas.transform, false);
		newObj.transform.name = crdObj.transform.name;
		Destroy(crdObj);
		return newObj;
	}

	//создаёт объект crdObj на холсте с координатами posX и posY
	GameObject InstantiateCard(GameObject crdObj, float posX, float posY) {
		GameObject obj = GameObject.Instantiate(crdObj, Canvas.transform, false);
		obj.GetComponent<RectTransform>().anchoredPosition = new Vector2(posX, posY);
		return obj;
	}

	// функция получения карты из колоды, возвращает true если в колоде d ещё есть карты, иначе false, ставит на n-ое место в массиве hand d_i-ую карту из колоды d
	bool GetCard(card[] hand, card[] d, int handI, int d_i, bool isPlayerTurn) {
		if ((isPlayerTurn && isD1Empty) || (!isPlayerTurn && isD2Empty)) {
			hand[handI].value = -1;
			hand[handI].suit = -1;
			return false;
		}
		hand[handI].value = d[d_i].value;
		hand[handI].suit = d[d_i].suit;
		Vector3[] corners = new Vector3[4];
		Vector3[] cardCorners = new Vector3[4];
		Canvas.GetComponent<RectTransform>().GetLocalCorners(corners);
		cards[0, 0].GetComponent<RectTransform>().GetLocalCorners(cardCorners);
		//выдача карты игроку
		if (isPlayerTurn) {
			hand[handI].obj = InstantiateCard(cards[d[d_i].suit, d[d_i].value - 1], corners[0].x + 72f + handI * 110f, corners[0].y + cardCorners[1].y);
			Debug.Log("got a card feeling gud");
			hand[handI].obj.name = "PH" + handI.ToString();
		}
		else { //выдача карты ИИ
			hand[handI].obj = InstantiateCard(cards[d[d_i].suit, d[d_i].value - 1], corners[1].x + 72f + handI * 110f, corners[1].y - cardCorners[1].y);
			hand[handI].obj.name = "AI" + handI.ToString();
			hand[handI].obj.transform.tag = "AIHand";
		}
		d_i++;
		return (d_i < DECKLEN);
	}

	//заменяет handI-ую карту в руке hand на d_i-ую карту из колоды d, если возможно, в случае успеха возвращает true, иначе false 
	bool SwapCard(card[] hand, card[] d, int handI, int d_i, bool isPlayerTurn) {
		if (!isPlayerTurn && isD2Empty)
			return false;
		Destroy(hand[handI].obj);
		return GetCard(hand, d, handI, d_i, isPlayerTurn);
	}

	//возвращает стоимость карты с учётом лежащих на ней королей
	int CountCardValue(int crvnI, int crdI) {
		int crdVal = gameField[crvnI].cards[crdI].value;
		for (int j = 0; j < (gameField[crvnI].cards[crdI].numberOfCCards - gameField[crvnI].cards[crdI].numberOfQueens); j++)
			crdVal *= 2;
		return crdVal;
	}

	//удаляет crdI-ую карту из каравана crvnI
	void RemoveCardFromCaravan(int crvnI, int crdI) {
		int removedVal = gameField[crvnI].cards[crdI].value;
		for (int i = 0; i < (gameField[crvnI].cards[crdI].numberOfCCards - gameField[crvnI].cards[crdI].numberOfQueens); i++)
			removedVal *= 2;
		gameField[crvnI].value -= removedVal;
		bool isPlayerCaravan = (crvnI % 2) == 0;
		Destroy(gameField[crvnI].cards[crdI].obj);
		for (int i = 0; i < gameField[crvnI].cards[crdI].numberOfCCards; i++)
			Destroy(gameField[crvnI].cards[crdI].connectedCards[i]);
		for (int i = crdI; i < gameField[crvnI].numberOfCards - 1; i++)
		{
			gameField[crvnI].cards[i].value = gameField[crvnI].cards[i + 1].value;
			gameField[crvnI].cards[i].suit = gameField[crvnI].cards[i + 1].suit;
			Vector2 crdPos = gameField[crvnI].cards[i + 1].obj.GetComponent<RectTransform>().anchoredPosition;
			float yMove = 75f;
			if (!isPlayerCaravan)
				yMove *= -1;
			gameField[crvnI].cards[i + 1].obj.GetComponent<RectTransform>().anchoredPosition = new Vector2(crdPos.x, crdPos.y + yMove);
			gameField[crvnI].cards[i].obj = gameField[crvnI].cards[i + 1].obj;
			for (int j = 0; j < gameField[crvnI].cards[i + 1].numberOfCCards; j++)
			{
				crdPos = gameField[crvnI].cards[i + 1].connectedCards[j].GetComponent<RectTransform>().anchoredPosition;
				gameField[crvnI].cards[i + 1].connectedCards[j].GetComponent<RectTransform>().anchoredPosition = new Vector2(crdPos.x, crdPos.y + yMove);
			}
			gameField[crvnI].cards[i].numberOfCCards = gameField[crvnI].cards[i + 1].numberOfCCards;
		}
		gameField[crvnI].numberOfCards--;
		if (gameField[crvnI].numberOfCards > 1)
		{
			gameField[crvnI].isIncreasing = gameField[crvnI].cards[gameField[crvnI].numberOfCards - 2].value < gameField[crvnI].cards[gameField[crvnI].numberOfCards - 1].value;
			if (gameField[crvnI].cards[gameField[crvnI].numberOfCards - 1].numberOfQueens % 2 != 0)
				gameField[crvnI].isIncreasing = !gameField[crvnI].isIncreasing;
		}
		if (gameField[crvnI].numberOfCards == 0)
			gameField[crvnI].CaravanStart.SetActive(true);
		gameField[crvnI].isComplete = (gameField[crvnI].value > 20) && (gameField[crvnI].value < 27);
	}

	//ставит карту crdObj в караван, в котором лежит объект crvnCrdObj, возвращает true в случае успеха
	bool PlaceNumberCard(GameObject crdObj, GameObject crvnCrdObj) {
		Debug.Log("tryin to add your card sir.");
		int numOfCrds;
		int crvnI = crvnCrdObj.transform.name[0] - '0';
		int cardI = crdObj.transform.name[2] - '0';
		card c;
		//проверка на то, чей ход
		if (crvnI % 2 == 0)
			c = handP1[cardI];
		else
			c = handP2[cardI];
		numOfCrds = gameField[crvnI].numberOfCards;
		//проверка на масть
		if ((numOfCrds > 0) && (gameField[crvnI].cards[numOfCrds - 1].suit != c.suit)) {
			Debug.Log("wrong suit");
			return false;
		}
		//проверка на последовательность
		if ((numOfCrds > 1) && (gameField[crvnI].isIncreasing != (gameField[crvnI].cards[numOfCrds - 1].value < c.value))) {
			Debug.Log("wrong number sir.");
			return false;
		}
		Vector2 crvnCrdPos, endPos;
		if (numOfCrds == 0) {
			crvnCrdPos = crvnCrdObj.GetComponent<RectTransform>().anchoredPosition;
			endPos = new Vector2(crvnCrdPos.x, crvnCrdPos.y);
		}
		else {
			float t = 75f;
			if (crvnI % 2 == 0)
				t *= -1;
			crvnCrdPos = gameField[crvnI].cards[numOfCrds - 1].obj.GetComponent<RectTransform>().anchoredPosition;
			endPos = new Vector2(crvnCrdPos.x, crvnCrdPos.y + t);
		}
		gameField[crvnI].cards[numOfCrds].obj = InstantiateCard(crdObj, endPos.x, endPos.y);
		Destroy(crdObj);
		if (numOfCrds > 0)
			gameField[crvnI].isIncreasing = gameField[crvnI].cards[numOfCrds - 1].value < c.value;
		gameField[crvnI].cards[numOfCrds].value = c.value;
		gameField[crvnI].cards[numOfCrds].suit = c.suit;
		gameField[crvnI].cards[numOfCrds].connectedCards = new GameObject[MAXKINGS];
		gameField[crvnI].cards[numOfCrds].numberOfCCards = 0;
		gameField[crvnI].cards[numOfCrds].numberOfQueens = 0;
		gameField[crvnI].cards[numOfCrds].obj.transform.tag = "PlacedNumberCard";
		gameField[crvnI].cards[numOfCrds].obj.transform.name = crvnI.ToString() + "_" + numOfCrds.ToString();
		gameField[crvnI].cards[numOfCrds].obj.transform.localScale = new Vector3(1.0f, 1.0f, 1.0f);
		gameField[crvnI].numberOfCards++;
		gameField[crvnI].value += c.value;
		gameField[crvnI].isComplete = ((gameField[crvnI].value >= 21) && (gameField[crvnI].value < 27));
		gameField[crvnI].CaravanStart.SetActive(false);
		Debug.Log("nice sir");
		return true;
	}

	//ставит фигурную карту на поле
	bool PlaceFigureCard(GameObject crdObj, GameObject crvnCrdObj) {
		int numOfCrds;
		int crvnI = crvnCrdObj.transform.name[0] - '0';
		int cardI = crdObj.transform.name[2] - '0';
		card c;
		if (crdObj.transform.name[0] == 'P') 
			c = handP1[cardI];
		else
			c = handP2[cardI];
		float step = 20f;
		if (crvnI % 2 == 1)
			step *= -1;
		if (gameField[crvnI].numberOfCards == 0)
			return false;
		GameObject targetCard = crvnCrdObj;
		int targetCardI = targetCard.transform.name[2] - '0';
		targetCard = gameField[crvnI].cards[targetCardI].obj;
		Vector2 targetCardPos;
		//выбор действия
		switch (c.value) {
			case 11: //валет
				RemoveCardFromCaravan(crvnI, targetCardI);
				Destroy(crdObj);
				break;
			case 12: //дама
				targetCardPos = targetCard.GetComponent<RectTransform>().anchoredPosition;
				gameField[crvnI].cards[targetCardI].connectedCards[gameField[crvnI].cards[targetCardI].numberOfCCards] = InstantiateCard(crdObj, targetCardPos.x + step * (gameField[crvnI].cards[targetCardI].numberOfCCards + 1), targetCardPos.y);
				gameField[crvnI].cards[targetCardI].connectedCards[gameField[crvnI].cards[targetCardI].numberOfCCards].transform.name = crvnI.ToString() + "Q" + targetCardI.ToString();
				gameField[crvnI].cards[targetCardI].connectedCards[gameField[crvnI].cards[targetCardI].numberOfCCards].transform.tag = "PlacedFigureCard";
				gameField[crvnI].cards[targetCardI].connectedCards[gameField[crvnI].cards[targetCardI].numberOfCCards].transform.localScale = new Vector3(1.0f, 1.0f, 1.0f);
				Destroy(crdObj);
				for (int i = targetCardI + 1; i < gameField[crvnI].numberOfCards; i++) {
					gameField[crvnI].cards[i].obj = ReInstantiateCard(gameField[crvnI].cards[i].obj);
					for (int j = 0; j < gameField[crvnI].cards[i].numberOfCCards; j++)
						ReInstantiateCard(gameField[crvnI].cards[i].connectedCards[j]);
				}
				gameField[crvnI].cards[targetCardI].numberOfCCards++;
				gameField[crvnI].cards[targetCardI].numberOfQueens++;
				gameField[crvnI].cards[targetCardI].suit = c.suit;
				gameField[crvnI].isIncreasing = !gameField[crvnI].isIncreasing;
				break;
			case 13: //король
				targetCardPos = targetCard.GetComponent<RectTransform>().anchoredPosition;
				gameField[crvnI].cards[targetCardI].connectedCards[gameField[crvnI].cards[targetCardI].numberOfCCards] = InstantiateCard(crdObj, targetCardPos.x + step * (gameField[crvnI].cards[targetCardI].numberOfCCards + 1), targetCardPos.y);
				gameField[crvnI].cards[targetCardI].connectedCards[gameField[crvnI].cards[targetCardI].numberOfCCards].transform.name = crvnI.ToString() + "K" + targetCardI.ToString();
				gameField[crvnI].cards[targetCardI].connectedCards[gameField[crvnI].cards[targetCardI].numberOfCCards].transform.tag = "PlacedFigureCard";
				gameField[crvnI].cards[targetCardI].connectedCards[gameField[crvnI].cards[targetCardI].numberOfCCards].transform.localScale = new Vector3(1.0f, 1.0f, 1.0f);
				Destroy(crdObj);
				for (int i = targetCardI + 1; i < gameField[crvnI].numberOfCards; i++)
				{
					gameField[crvnI].cards[i].obj = ReInstantiateCard(gameField[crvnI].cards[i].obj);
					for (int j = 0; j < gameField[crvnI].cards[i].numberOfCCards; j++)
						ReInstantiateCard(gameField[crvnI].cards[i].connectedCards[j]);
				}
				gameField[crvnI].value += CountCardValue(crvnI, targetCardI);
				gameField[crvnI].cards[targetCardI].numberOfCCards++;
				gameField[crvnI].isComplete = (gameField[crvnI].value > 20) && (gameField[crvnI].value < 27);
				break;
			default:
				int targetValue = gameField[crvnI].cards[targetCardI].value;
				for (int i = 0; i < 6; i++)
					for (int j = 0; j < gameField[i].numberOfCards; j++)
						if (((i != crvnI) || (j != targetCardI)) && (gameField[i].cards[j].value == targetValue))
							RemoveCardFromCaravan(i, j);
				Destroy(crdObj);
				break;
		}
		return true;
	}

	bool TryToCompleteWithKing(int crvnI, int minVal, int maxVal, int kingI)
	{
		int numOfCards = gameField[crvnI].numberOfCards;
		int resultVal, startVal = gameField[crvnI].value;
		for (int i = 0; i < numOfCards; i++)
		{
			int crdVal = CountCardValue(crvnI, i);
			resultVal = startVal + crdVal;
			if ((resultVal >= minVal) && (resultVal <= maxVal))
			{
				PlaceFigureCard(handP2[kingI].obj, gameField[crvnI].cards[i].obj);
				isD2Empty = !GetCard(handP2, d2, kingI, d_i2, false);
				return true;
			}
		}
		return false;
	}

	bool TryToCompleteWithJack(int crvnI, int minVal, int maxVal, int jackI)
	{
		int numOfCards = gameField[crvnI].numberOfCards;
		int resultVal, startVal = gameField[crvnI].value;
		for (int i = 0; i < numOfCards; i++)
		{
			int crdVal = CountCardValue(crvnI, i);
			resultVal = startVal - crdVal;
			if ((resultVal >= minVal) && (resultVal <= maxVal))
			{
				PlaceFigureCard(handP2[jackI].obj, gameField[crvnI].cards[i].obj);
				isD2Empty = !GetCard(handP2, d2, jackI, d_i2, false);
				return true;
			}
		}
		return false;
	}

	void RemoveMaxCardWithJack(int crvnI, int jackI)
	{
		int numOfCards = gameField[crvnI].numberOfCards;
		int maxI, maxV;
		maxI = maxV = 0;
		for (int i = 0; i < numOfCards; i++)
		{
			int crdVal = gameField[crvnI].cards[i].value;
			for (int j = 0; j < (gameField[crvnI].cards[i].numberOfCCards - gameField[crvnI].cards[i].numberOfQueens); j++)
				crdVal *= 2;
			if (crdVal > maxV)
			{
				maxV = crdVal;
				maxI = i;
			}
		}
		PlaceFigureCard(handP2[jackI].obj, gameField[crvnI].cards[maxI].obj);
		isD2Empty = !GetCard(handP2, d2, jackI, d_i2, false);
	}

	void GameOver() {
		int pColumns = 0;
		int aiColumns = 0;
		for (int i = 0; i < 3; i++)	{
			if ((Convert.ToByte(gameField[i * 2 + 1].isComplete) >= Convert.ToByte(gameField[i * 2].isComplete)) && ((gameField[i * 2 + 1].value > gameField[i * 2].value) || (gameField[i * 2].value > 26)) && (gameField[i * 2 + 1].isComplete))
				aiColumns++;
			else if (gameField[i * 2].isComplete)
				pColumns++;
		}
		int res = pColumns - aiColumns;
		GameOverScreen.GetComponent<GameOverScript>().Setup(res);
	}

	// возвращает true, если игра закончена, иначе возвращает false
	bool IsGameOver() {
		if ((gameField[0].isComplete || gameField[1].isComplete) && (gameField[2].isComplete || gameField[3].isComplete) && (gameField[4].isComplete || gameField[5].isComplete)) {
			GameOver();
			return true;
		}
		else
			return false;
	}

	// функция хода компьютера, меняет игровое поле одним ходом, возвращает 1, если на конец хода игра не закончилась, иначе возвращает 0
	bool AITurn()
	{
		column[] columns = new column[3];
		int j = 0;
		int columnsLost = 0;
		int columnsWon = 0;
		int[] fCardsAmounts = { 0, 0, 0 };
		int[] figureCrdArray = { MAXHAND, MAXHAND, MAXHAND, MAXHAND};
		int[] suitsOccupied = { -1, -1, -1, -1 };
		// расчёт значений полей структуры для каждого столбца
		for (int i = 0; i < 3; i++)
		{
			columns[i].isWon = (Convert.ToByte(gameField[j + 1].isComplete) >= Convert.ToByte(gameField[j].isComplete)) && ((gameField[j + 1].value > gameField[j].value) || (gameField[j].value > 26)) && (gameField[j + 1].isComplete);
			if (columns[i].isWon)
				columnsWon++;
			columns[i].isLost = (Convert.ToByte(gameField[j + 1].isComplete) <= Convert.ToByte(gameField[j].isComplete)) && ((gameField[j + 1].value < gameField[j].value) || (gameField[j + 1].value > 26)) && (gameField[j].isComplete);
			if (columns[i].isLost)
				columnsLost++;
			if (gameField[j + 1].numberOfCards > 0)
			{
				columns[i].suit = gameField[j + 1].cards[gameField[j + 1].numberOfCards - 1].suit;
				suitsOccupied[columns[i].suit] = i;
			}
			else
				columns[i].suit = -1;
			columns[i].howMuchToComplete = 0;
			if (gameField[j + 1].value < 21)
				columns[i].howMuchToComplete = 21 - gameField[j + 1].value;
			else if (gameField[j + 1].value > 26)
				columns[i].howMuchToComplete = 21 - gameField[j + 1].value;
			j += 2;
		}
		// формирование массива, хранящего информацию о лежащих и отсутствующих на руках ИИ картах
		int[,] handArray = new int[5, 14];
		for (int i = 0; i < MAXHAND; i++)
		{
			if (handP2[i].suit >= 0) // (handP2[i].value > 0)
			{
				handArray[handP2[i].suit, handP2[i].value] = i + 1;
				if (handP2[i].value > 10)
				{
					figureCrdArray[handP2[i].value - 11] = i;
					fCardsAmounts[handP2[i].value - 11]++;
				}
			}
		}
		//проверка на пустые караваны
		for (int i = 0; i < 3; i++)
		{
			if (columns[i].suit == -1)
				for (int v = 10; v > 0; v--)
					for (int s = 0; s < 4; s++)
						if (suitsOccupied[s] == -1)
							if (handArray[s, v] > 0)
								if (PlaceNumberCard(handP2[handArray[s, v] - 1].obj, gameField[i * 2 + 1].CaravanStart))
								{
									isD2Empty = !GetCard(handP2, d2, handArray[s, v] - 1, d_i2++, false);
									return IsGameOver();
								}
		}
		int targetColumnI = -1;
		int rightLim, valToComplete = 30;
		int direction;
		// если два из трёх столбцов выиграны или один выигран и один проигран
		if ((columnsWon == 2) || ((columnsWon == 1) && (columnsLost == 1)))
		{
			// вычисление номера незаконченного столбца
			int lastColumnI;
			for (lastColumnI = 0; columns[lastColumnI].isWon || columns[lastColumnI].isLost; lastColumnI++) ;
			if (figureCrdArray[2] != MAXHAND)
			{
				if (TryToCompleteWithKing(lastColumnI * 2 + 1, 21, 26, figureCrdArray[2]))
					return IsGameOver();
				if (TryToCompleteWithKing(lastColumnI * 2, 21, 26, figureCrdArray[2]))
					return IsGameOver();
			}
			if ((gameField[lastColumnI * 2].value > 26) && (figureCrdArray[0] != MAXHAND))
				RemoveMaxCardWithJack(lastColumnI * 2, figureCrdArray[0]);
			int colSuit = gameField[lastColumnI * 2 + 1].cards[gameField[lastColumnI * 2 + 1].numberOfCards - 1].suit;
			int allowedValue, neededValue = columns[lastColumnI].howMuchToComplete;
			// если для выигрыша необходимо добавить карты
			if (neededValue >= 0)
			{
				if (neededValue == 0)
					neededValue = gameField[lastColumnI * 2].value - gameField[lastColumnI * 2 + 1].value + 1;
				allowedValue = 26 - gameField[lastColumnI * 2 + 1].value;
				if (gameField[lastColumnI * 2 + 1].numberOfCards > 1)
				{
					int lastCardValue = gameField[lastColumnI * 2 + 1].cards[gameField[lastColumnI * 2 + 1].numberOfCards - 1].value;
					direction = 1;
					if (!gameField[lastColumnI * 2 + 1].isIncreasing)
						direction *= -1;
					switch (direction)
					{
						case -1:
							if (allowedValue > lastCardValue)
								allowedValue = lastCardValue;
							break;
						case 1:
							if (neededValue < lastCardValue)
								neededValue = lastCardValue;
							break;
						default:
							break;
					}
					for (int i = neededValue; (i < allowedValue) && (i < 11); i++)
						if (handArray[colSuit, i] > 0)
							if (PlaceNumberCard(handP2[handArray[colSuit, i] - 1].obj, gameField[lastColumnI * 2 + 1].cards[gameField[lastColumnI * 2 + 1].numberOfCards - 1].obj))
							{
								isD2Empty = !GetCard(handP2, d2, handArray[colSuit, i] - 1, d_i2++, false);
								return IsGameOver();
							}
				}
				else
					for (int i = neededValue; (i < allowedValue) && (i < 11); i++)
						if (handArray[colSuit, i] > 0)
							if (PlaceNumberCard(handP2[handArray[colSuit, i] - 1].obj, gameField[lastColumnI * 2 + 1].cards[gameField[lastColumnI * 2 + 1].numberOfCards - 1].obj))
							{
								isD2Empty = !GetCard(handP2, d2, handArray[colSuit, i] - 1, d_i2++, false);
								return IsGameOver();
							}
			}
			// если для выигрыша необходимо убрать лишние карты
			else
			{
				if (TryToCompleteWithJack(lastColumnI * 2 + 1, 21, 26, figureCrdArray[0]))
					return IsGameOver();
			}
		}
		if (columnsLost == 2)
		{
			if (figureCrdArray[2] != MAXHAND)
				for (int i = 0; i < 3; i++)
				{
					if (!columns[i].isLost)
						continue;
					if (TryToCompleteWithKing(i * 2, 27, 1000, figureCrdArray[2]))
						return IsGameOver();
				}
			if (figureCrdArray[0] != MAXHAND)
				for (int i = 0; i < 3; i++)
				{
					if (!columns[i].isLost)
						continue;
					RemoveMaxCardWithJack(i * 2, figureCrdArray[0]);
					return IsGameOver();
				}
			for (int i = 0; i < 3; i++)
			{
				if (!columns[i].isLost)
					continue;
				if (valToComplete > columns[i].howMuchToComplete)
				{
					valToComplete = columns[i].howMuchToComplete;
					targetColumnI = i;
				}
			}
		}
		if (targetColumnI == -1)
		{
			int t = 30;
			for (int i = 0; i < 3; i++)
			{
				if (t > columns[i].howMuchToComplete)
				{
					valToComplete = columns[i].howMuchToComplete;
					targetColumnI = i;
				}
			}
		}
		if ((valToComplete < 0) && (figureCrdArray[0] < MAXHAND)) 
		{
			if (TryToCompleteWithJack(targetColumnI * 2 + 1, 21, 26, figureCrdArray[0]))
				return IsGameOver();
			else
            {
				RemoveMaxCardWithJack(targetColumnI * 2 + 1, figureCrdArray[0]);
				return IsGameOver();
			}
		}
		int crdVal, maxCrdValue = -1;
		int maxCrdI, maxCrdj;
		maxCrdI = maxCrdj = -1;
		if (fCardsAmounts[0] > 1) {
			for (int i = 0; i < 3; i++)
			{
				if (gameField[i * 2].numberOfCards == 0)
					continue;
				for (int j1 = 0; j1 < gameField[i * 2].numberOfCards; j1++)
				{
					crdVal = CountCardValue(i * 2, j1);
					if (crdVal > maxCrdValue)
					{
						maxCrdI = i;
						maxCrdj = j1;
						maxCrdValue = crdVal;
					}
				}
			}
			if (maxCrdI > -1)
				if(PlaceFigureCard(handP2[figureCrdArray[0]].obj, gameField[maxCrdI * 2].cards[maxCrdj].obj))
				{
					isD2Empty = !GetCard(handP2, d2, figureCrdArray[0], d_i2++, false);
					return IsGameOver();
				}
		}
		if (fCardsAmounts[2] > 1)
		{
			for (int i = 0; i < 3; i++)
				if (TryToCompleteWithKing(i * 2 + 1, 15, 26, figureCrdArray[2]))
					return IsGameOver();
		}
		int columnSuit, targetColumnSuit = gameField[targetColumnI * 2 + 1].cards[gameField[targetColumnI * 2 + 1].numberOfCards - 1].suit;
		int lastCardVal = gameField[targetColumnI * 2 + 1].cards[gameField[targetColumnI * 2 + 1].numberOfCards - 1].value;
		direction = 1;
		if (!gameField[targetColumnI * 2 + 1].isIncreasing)
			direction *= -1;
		rightLim = 10;
		if (rightLim > (valToComplete + 5))
			rightLim = valToComplete + 5;
		for (int i = rightLim; i > 0; i--)
			if (handArray[targetColumnSuit, i] > 0)
				if (PlaceNumberCard(handP2[handArray[targetColumnSuit, i] - 1].obj, gameField[targetColumnI * 2 + 1].cards[gameField[targetColumnI * 2 + 1].numberOfCards - 1].obj))
				{
					isD2Empty = !GetCard(handP2, d2, handArray[targetColumnSuit, i] - 1, d_i2++, false);
					return IsGameOver();
				}
		if (figureCrdArray[1] != MAXHAND)
			for (int i = 0; i < 3; i++)
				if ((gameField[targetColumnI * 2 + 1].numberOfCards > 1) && (gameField[targetColumnI * 2 + 1].cards[gameField[targetColumnI * 2 + 1].numberOfCards - 1].numberOfQueens == 0) && (((gameField[targetColumnI * 2 + 1].isIncreasing) && (gameField[targetColumnI * 2 + 1].cards[gameField[targetColumnI * 2 + 1].numberOfCards - 1].value > (26 - gameField[targetColumnI * 2 + 1].value))) || (!(gameField[targetColumnI * 2 + 1].isIncreasing) && (gameField[targetColumnI * 2 + 1].cards[gameField[targetColumnI * 2 + 1].numberOfCards - 1].value < (26 - gameField[targetColumnI * 2 + 1].value)))))
				{
					PlaceFigureCard(handP2[figureCrdArray[1]].obj, gameField[targetColumnI * 2 + 1].cards[gameField[targetColumnI * 2 + 1].numberOfCards - 1].obj);
					isD2Empty = !GetCard(handP2, d2, figureCrdArray[1], d_i2++, false);
					return IsGameOver();
				}
		for (int i = 0; i < 3; i++)
		{
			if (i == targetColumnI)
				continue;
			columnSuit = columns[i].suit;
			rightLim = 10;
			if (rightLim > (columns[i].howMuchToComplete + 5))
				rightLim = columns[i].howMuchToComplete + 5;
			for (int v = rightLim; v > 0; v--)
				if (handArray[columnSuit, v] > 0)
					if (PlaceNumberCard(handP2[handArray[columnSuit, v] - 1].obj, gameField[i * 2 + 1].cards[gameField[i * 2 + 1].numberOfCards - 1].obj))
					{
						isD2Empty = !GetCard(handP2, d2, handArray[columnSuit, v] - 1, d_i2++, false);
						return IsGameOver();
					}
		}
		if (fCardsAmounts[1] > 1)
		{
			isD2Empty = !SwapCard(handP2, d2, figureCrdArray[1], d_i2++, false);
			return IsGameOver();
		}
		for (int v = 1; v < 11; v++)
			for (int s = 0; s < 5; s++)
				if (handArray[s, v] > 0)
				{
					isD2Empty = !SwapCard(handP2, d2, handArray[s, v] - 1, d_i2++, false);
					return false;
				}
		return false;
	}

	// заряжает колоду из 52-х карт
	void ShuffleDeck(card[] d) {
		int n, t1, t2, k = 0;
		//заряжаем колоду
		for (int i = 1; i < 14; i++) {
			for (int j = 0; j < 4; j++) {
				d[k].value = i;
				d[k].suit = j;
				k++;
			}
		}
		//теперь перемешиваем
		for (int i = DECKLEN - 1; i > 0; i--) {
			n = UnityEngine.Random.Range(0, i + 1);
			t1 = d[i].value;
			d[i].value = d[n].value;
			d[n].value = t1;
			t2 = d[i].suit;
			d[i].suit = d[n].suit;
			d[n].suit = t2;
		}
	}

	// Start is called before the first frame update
	void Start()
	{
		isGameRunning = true;
		//заготовка колод и раздача карт
		GameObject[] hCards = { cH1, cH2, cH3, cH4, cH5, cH6, cH7, cH8, cH9, cH10, cH11, cH12, cH13 };
		GameObject[] DCards = { cD1, cD2, cD3, cD4, cD5, cD6, cD7, cD8, cD9, cD10, cD11, cD12, cD13 };
		GameObject[] CCards = { cC1, cC2, cC3, cC4, cC5, cC6, cC7, cC8, cC9, cC10, cC11, cC12, cC13 };
		GameObject[] SCards = { cS1, cS2, cS3, cS4, cS5, cS6, cS7, cS8, cS9, cS10, cS11, cS12, cS13 };
		GameObject[] JCards = { cJ1, cJ2 };

		for (int i = 0; i < 13; i++)
			cards[0, i] = hCards[i];
		for (int i = 0; i < 13; i++)
			cards[1, i] = DCards[i];
		for (int i = 0; i < 13; i++)
			cards[2, i] = CCards[i];
		for (int i = 0; i < 13; i++)
			cards[3, i] = SCards[i];
		for (int i = 0; i < 2; i++)
			cards[4, i] = JCards[i];
		handP1 = new card[MAXHAND];
		handP2 = new card[MAXHAND];
		d1 = new card[DECKLEN];
		d2 = new card[DECKLEN];
		ShuffleDeck(d1);
		ShuffleDeck(d2);
		for (d_i1 = 0; d_i1 < MAXHAND; d_i1++)
		{
			GetCard(handP1, d1, d_i1, d_i1, true);
			GetCard(handP2, d2, d_i1, d_i1, false);
		}
		d_i2 = d_i1;

		//очистка поля
		for (int i = 0; i < 6; i++)
		{
			gameField[i].cards = new card[MAXCARAVAN];
			gameField[i].isComplete = false;
			gameField[i].numberOfCards = 0;
			gameField[i].value = 0;
		}
		gameField[0].CaravanStart = Caravan0Start;
		gameField[1].CaravanStart = Caravan1Start;
		gameField[2].CaravanStart = Caravan2Start;
		gameField[3].CaravanStart = Caravan3Start;
		gameField[4].CaravanStart = Caravan4Start;
		gameField[5].CaravanStart = Caravan5Start;
		//прочие приготовления
		prevClickedCard = null;
		isD1Empty = false;
		isD2Empty = false;
	}

	// Update is called once per frame
	void Update()
	{
		if (isGameRunning)
		{
			if (Deck1 && isD1Empty)
				Destroy(Deck1);
			if (Deck2 && isD2Empty)
				Destroy(Deck2);
			if (isD1Empty)
			{
				bool isHandEmpty = true;
				for (int i = 0; (i < MAXHAND) && isHandEmpty; i++)
					isHandEmpty = handP1[i].value == -1;
				if (isHandEmpty)
				{
					isGameRunning = false;
					GameOver();
				}
			}
			if (Input.GetMouseButtonDown(0))
			{
				RaycastHit2D hit;
				hit = Physics2D.Raycast(Camera.main.ScreenToWorldPoint(Input.mousePosition), Vector2.zero);
				Debug.Log(hit.transform);
				if (hit.transform)
				{
					Debug.Log(hit.transform.gameObject.name);
					if (hit.transform.gameObject.tag == "NumberCard" || hit.transform.gameObject.tag == "FigureCard")
					{
						if (prevClickedCard)
							prevClickedCard.transform.localScale = new Vector3(1.0f, 1.0f, 1.0f);
						hit.transform.gameObject.transform.localScale = new Vector3(1.25f, 1.25f, 1.25f);
						prevClickedCard = hit.transform.gameObject;
					}
					else if ((hit.transform.gameObject.tag == "PlacedNumberCard") || (hit.transform.gameObject.tag == "PlacedFigureCard"))
					{
						Debug.Log("tag is ok");
						if (prevClickedCard)
						{
							if (prevClickedCard.tag == "NumberCard")
							{
								Debug.Log("it should be placed...");
								int placedCardI = prevClickedCard.transform.name[2] - '0';
								wasCardPlaced = PlaceNumberCard(prevClickedCard, hit.transform.gameObject);
								if (wasCardPlaced)
								{
									isD1Empty = !GetCard(handP1, d1, placedCardI, d_i1++, true);
									prevClickedCard = null;
									if (IsGameOver())
										isGameRunning = false;
									else if (AITurn())
										isGameRunning = false;
								}
							}
							else if (prevClickedCard.tag == "FigureCard")
							{
								int placedCardI = prevClickedCard.transform.name[2] - '0';
								wasCardPlaced = PlaceFigureCard(prevClickedCard, hit.transform.gameObject);
								if (wasCardPlaced)
								{
									isD1Empty = !GetCard(handP1, d1, placedCardI, d_i1++, true);
									prevClickedCard.transform.localScale = new Vector3(1.0f, 1.0f, 1.0f);
									prevClickedCard = null;
									if (IsGameOver())
										isGameRunning = false;
									else if (AITurn())
										isGameRunning = false;
								}
							}
						}
					}
					else if ((hit.transform.gameObject.tag == "SwapDeck") && ((prevClickedCard.tag == "NumberCard") || (prevClickedCard.tag == "FigureCard")))
					{
						isD1Empty = !SwapCard(handP1, d1, prevClickedCard.transform.name[2] - '0', d_i1++, true);
						prevClickedCard = null;
						if (IsGameOver())
							isGameRunning = false;
						else if (AITurn())
							isGameRunning = false;
					}
				}
			}
		}
	}
}
