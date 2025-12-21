using System.Collections.Generic;
using System.Linq;
using System.Text;

public static class sudokuGenerator {

	public static readonly string[] solutions = {
		"XYZWZWXYYXWZWZYX", "XYZWZWXYYZWXWXYZ", "XYZWZWXYWXYZYZWX", "XYZWZWXYWZYXYXWZ",
		"XYZWZWYXYXWZWZXY", "XYZWZWYXWZXYYXWZ", "XYZWWZXYYXWZZWYX", "XYZWWZXYZWYXYXWZ",
		"XYZWWZYXYXWZZWXY", "XYZWWZYXYWXZZXWY", "XYZWWZYXZXWYYWXZ", "XYZWWZYXZWXYYXWZ"
	};
	
	static List<char> possibilities(string puzzle, int index)
	{
		int quadrant = (index > 7 ? 2 : 0) + (index % 4 > 1 ? 1 : 0);
		int[][] quadrantsIndices = { new[]{ 0, 1, 4, 5 }, new[]{ 2, 3, 6, 7 }, new[]{ 8, 9, 12, 13 }, new[]{ 10, 11, 14, 15 } };
		List<char> digits = new List<char>();
		foreach (int puzzleIndex in quadrantsIndices[quadrant]) if (puzzle[puzzleIndex] != '.') digits.Add(puzzle[puzzleIndex]);
		for (int i = 0; i < 4; i++) if(puzzle[4*i+index%4]!='.') digits.Add(puzzle[4*i+index%4]);
		for (int i = 0; i < 4; i++) if(puzzle[index/4*4+i]!='.') digits.Add(puzzle[index/4*4+i]);
		return new List<char>{'X','Y','Z','W'}.Where(x => !digits.Contains(x)).ToList();
	}
	static string fill(string puzzle)
	{
		StringBuilder result = new StringBuilder().Append(puzzle);
		List<char>[] pos = Enumerable.Range(0,16).Select(x => possibilities(puzzle, x)).ToArray();
		for (int i = 0; i < puzzle.Length; i++)
		{
			if (puzzle[i] != '.') continue;
			if (pos[i].Count == 0) return puzzle;
			if (pos[i].Count == 1) {result[i] = pos[i][0]; return result.ToString();}

			List<int> column = Enumerable.Range(0, 4).Select(x => 4 * x + i % 4).Where(x => x!=i).ToList();
			List<int> row = Enumerable.Range(0, 4).Select(x => i/4*4+x).Where(x => x!=i).ToList();
			List<int> quadrant = new[]
				{ new List<int>{ 0, 1, 4, 5 }, new List<int>{ 2, 3, 6, 7 }, new List<int>{ 8, 9, 12, 13 }, new List<int>{ 10, 11, 14, 15 } }[
					(i > 7 ? 2 : 0) + (i % 4 > 1 ? 1 : 0)
				];
			quadrant.RemoveAll(x => x==i);
			foreach (char c in pos[i])
			{
				if (result[i]!='.') continue;
				if (!column.Select(x => pos[x].Contains(c)).Contains(true)) {result[i] = c; return result.ToString();}
				if (!row.Select(x => pos[x].Contains(c)).Contains(true)) {result[i] = c; return result.ToString();}
				if (!quadrant.Select(x => pos[x].Contains(c)).Contains(true)) {result[i] = c; return result.ToString();}
			}
		}
		return result.ToString();
	}
	static string GenerateCandidate(string solution, int clues)
	{
		var chars = solution.ToCharArray();
		var indices = Enumerable.Range(0, 16).ToList().Shuffle().GetRange(0, 16-clues);
		foreach (int i in indices) chars[i] = '.';
		return new string(chars);
	}
	
	static bool CountSolutions(string puzzle)
	{
		bool count = false;
		foreach (var sol in solutions) {
			bool fits = true;
			for (int i = 0; i < 16; i++)
				if (puzzle[i] != '.' && puzzle[i] != sol[i])
				{
					fits = false; break;
				}
			if (fits) { if (count) return false; count = true;}
		}
		return count;
	}

	static bool SolvableByDeduction(string puzzle, string solution)
	{
		string p = puzzle;
		string prev;
		do
		{
			prev = p;
			p = fill(p);
		}
		while (p != prev);
		return p == solution;
	}

	public static string GeneratePuzzle(string solution)
	{
		while (true)
		{
			string puzzle = GenerateCandidate(solution, clues: 4);
			if (!CountSolutions(puzzle)) continue;
			if (!SolvableByDeduction(puzzle, solution)) continue;
			return puzzle;
		}
	}
}
