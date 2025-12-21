using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;

public class senseiSudokuScript : MonoBehaviour
{
	public TextMesh[] digits;
	public SpriteRenderer eyes;
	public Sprite[] eyesSprites;
	public KMSelectable Module;
	public TextMesh submitText;
	public KMBombInfo BombInfo;
	public KMAudio Audio;

	private Color[] colors;
	
	private string initialPuzzle;
	private string initSolution;
	private int N;
	private int ANS1, ANS2;
	private int SUB1 = -1, SUB2 = -1;
	private bool terminate = false;
	
	static int ModuleIdCounter = 1;
	int ModuleId;
	
	void initializeSudoku()
	{
		initSolution = sudokuGenerator.solutions[Random.Range(0, sudokuGenerator.solutions.Length)];
		initialPuzzle = sudokuGenerator.GeneratePuzzle(initSolution);
		List<char> map = Enumerable.Range(1, 4).Select(x => (char)('0'+x)).ToList().Shuffle();
		initSolution = initSolution.Replace('X', map[0]).Replace('Y', map[1]).Replace('Z', map[2]).Replace('W', map[3]);
		initialPuzzle = initialPuzzle.Replace('X', map[0]).Replace('Y', map[1]).Replace('Z', map[2]).Replace('W', map[3]);
		Debug.LogFormat("[Sudoku #{0}] Puzzle is {1} with solution of {2}.", ModuleId, initialPuzzle, initSolution);
	}

	void initializeSeed()
	{
		int A = Rank4of16(Enumerable.Range(0, 16).Where(x => initialPuzzle[x]!='.').OrderBy(x=>x).ToArray());
		int B = Enumerable.Range(0, 16).Where(x => initialPuzzle[x]!='.').OrderBy(x=>x).Select(x=>initialPuzzle[x]-'1').Aggregate((x,y)=>x*4+y);
		N = 256 * A + B + 465920 * Random.Range(0, 6);
		int[] config = seedToActualValues(N);
		Debug.LogFormat("[Sudoku #{0}] Configuration array G: [{1}]. N = {2}, N%465920 = {3}", ModuleId, 
			config[0]+ "," + config[1] + "," + config[2] +  "," + config[3] + "," + config[4], N, N%465920);
		for (int i = 0; i < 12; i++) digits[i].text = "";
		if (config[4] >= config[3]) config[4]++;
		int[] fix = Unrank2of12(config[2]);
		digits[fix[0]].text = (config[3]+1).ToString();
		digits[fix[1]].text = (config[4]+1).ToString();
		digits[fix[0]].color = colors[config[0]];
		digits[fix[1]].color = colors[config[1]];
	}
	
	static int Choose(int n, int r)
	{
		if (r < 0 || r > n) return 0;
		if (r == 0 || r == n) return 1;
		int res = 1;
		for (int i = 1; i <= r; i++)
			res = res * (n - r + i) / i;
		return res;
	}

	static int Rank4of16(int[] pos)
	{
		int A = 0;
		int X = 0;
		for (int i = 0; i < 4; i++)
		{
			while (X < pos[i])
			{
				A += Choose(15 - X, 3-i);
				X++;
			}
			X++;
		}
		return A;
	}

	static int[] Unrank2of12(int A)
	{
		int p0 = 0;
		while (A >= 11 - p0)
		{
			A -= 11 - p0;
			p0++;
		}
		return new[]{p0, p0 + 1 + A};
	}

	
	int[] seedToActualValues(int seed)
	{
		int[] ans = new int[5];
		int[] multipliers = { 25, 25, 66, 9, 8 };
		for (int i = 4; i >= 0; i--)
		{
			ans[i] = seed % multipliers[i];
			seed /= multipliers[i];
		}
		return ans;
	}

	IEnumerator Timer()
	{
		SUB2 = -1;
		submitText.text = SUB1 + "-*";
		while (SUB2 < 32)
		{
			SUB2++;
			yield return new WaitForSeconds(.4f);
			if (terminate) yield break;
			submitText.text = SUB1 + "-" + ((SUB2%2==1)?"*":(SUB2/2).ToString());
		}

		SUB2 = -1;
		submitText.text = "";
	}
	
	void selected()
	{
		terminate = false;
		submitText.color = Color.black;
		SUB1 = (int)BombInfo.GetTime() % 16;
		StartCoroutine(Timer());
	}

	void deselected()
	{
		terminate = true;
		if (submitText.text.Contains('*') || submitText.text == "")
		{
			submitText.text = "";
			return;
		}
		if (submitText.text == (ANS1 + "-" + ANS2))
		{
			GetComponent<KMBombModule>().HandlePass();
			eyes.sprite = eyesSprites[1];
			Audio.HandlePlayGameSoundAtTransform(KMSoundOverride.SoundEffect.CorrectChime, transform);
			submitText.color = Color.green / 2f;
			Module.OnFocus -= selected;
			Module.OnDefocus -= deselected;
		}
		else
		{
			GetComponent<KMBombModule>().HandleStrike();
			submitText.color = Color.red;
		}
	}
	void Awake() { ModuleId = ModuleIdCounter++; }
	void Start ()
	{
		colors = Enumerable.Range(1,25).Select(x => new Color(x/9/2f,x/3%3/2f,x%3/2f)).ToArray();
		initializeSudoku();
		initializeSeed();
		submitText.text = "";
		ANS1 = (initSolution[5] - '1') * 4 + initSolution[6] - '1';
		ANS2 = (initSolution[9] - '1') * 4 + initSolution[10] - '1';
		Debug.LogFormat("[Sudoku #{0}] Your answer is: {1}.", ModuleId, ANS1 + "-" + ANS2);
		Module.OnFocus += selected;
		Module.OnDefocus += deselected;
	}
	
	
	#pragma warning disable 414
    private readonly string TwitchHelpMessage = @"Use !{0} #-# to submit your solution.";
#pragma warning restore 414
    public IEnumerator ProcessTwitchCommand(string Command)
    {
	    Module.OnFocus -= selected;
	    Module.OnDefocus -= deselected;
        yield return null;
        string[] commands = Command.Split('-');
        if (commands.Length != 2){yield return "sendtochaterror Invalid command."; yield break;}
        int?[] numbers = commands.Select(x => x.TryParseInt()).ToArray();
        if (numbers.Contains(null) || numbers.Length!=2 || numbers[0]<0 || numbers[1]<0 || numbers[0]>15 || numbers[1]>15)
        {yield return "sendtochaterror Invalid command."; yield break;}
        submitText.text = numbers[0] + "-" + numbers[1];
        if (numbers[0]==ANS1 &&  numbers[1]==ANS2)
        {
	        GetComponent<KMBombModule>().HandlePass();
	        eyes.sprite = eyesSprites[1];
	        Audio.HandlePlayGameSoundAtTransform(KMSoundOverride.SoundEffect.CorrectChime, transform);
	        submitText.color = Color.green / 2f;
        }
        else
        {
	        GetComponent<KMBombModule>().HandleStrike();
	        submitText.color = Color.red;
        }
    }

    public IEnumerator TwitchHandleForcedSolve()
    {
	    Module.OnFocus -= selected;
	    Module.OnDefocus -= deselected;
        yield return null;
        submitText.text = ANS1 + "-" + ANS2;
	    GetComponent<KMBombModule>().HandlePass();
	    eyes.sprite = eyesSprites[1];
	    Audio.HandlePlayGameSoundAtTransform(KMSoundOverride.SoundEffect.CorrectChime, transform);
	    submitText.color = Color.green / 2f;
    }
}
