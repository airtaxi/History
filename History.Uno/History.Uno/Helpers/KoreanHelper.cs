using System.Text;

namespace History.MobileClient.Helpers;

public static class KoreanHelper
{
	public const string Chosung = "ㄱㄲㄴㄷㄸㄹㅁㅂㅃㅅㅆㅇㅈㅉㅊㅋㅌㅍㅎ";
	public const string Jungsung = "ㅏㅐㅑㅒㅓㅔㅕㅖㅗㅘㅙㅚㅛㅜㅝㅞㅟㅠㅡㅢㅣ";
	public const string Jongsung = " ㄱㄲㄳㄴㄵㄶㄷㄹㄺㄻㄼㄽㄾㄿㅀㅁㅂㅄㅅㅆㅇㅈㅊㅋㅌㅍㅎ";
	private const ushort UnicodeStart = 0xAC00;
	private const ushort UnicodeEnd = 0xD79F;

	public static bool IsKoreanCharacrer(char character)
	{
		var unicode = Convert.ToUInt16(character);
		return unicode >= UnicodeStart && unicode <= UnicodeEnd;
	}

	public static char MergeCharacters(params char[] sungs)
	{
		var sungCount = sungs.Length;
		if (sungCount == 1 && !IsKoreanCharacrer(sungs[0])) return sungs[0];

		int charCode;
		if (sungCount == 1) return sungs[0];
		else if (sungCount == 2) charCode = UnicodeStart + (Chosung.IndexOf(sungs[0]) * 21 + Jungsung.IndexOf(sungs[1])) * 28;
		else charCode = UnicodeStart + (Chosung.IndexOf(sungs[0]) * 21 + Jungsung.IndexOf(sungs[1])) * 28 + Jongsung.IndexOf(sungs[2]);

		var character = Convert.ToChar(charCode);
		return character;
	}

	public static char[] SplitCharacter(char character)
	{
		var unicode = Convert.ToUInt16(character);
		var isKoreanCharacter = IsKoreanCharacrer(character);
		if (!isKoreanCharacter) return [character];
		else
		{
			var koreanUnicode = unicode - UnicodeStart;
			var chosungIndex = koreanUnicode / (21 * 28);
			var jungsungIndex = koreanUnicode % (21 * 28) / 28;
			var jongsungIndex = koreanUnicode % (21 * 28) % 28;
			return [Chosung[chosungIndex], Jungsung[jungsungIndex], Jongsung[jongsungIndex]];
		}
	}

	public static string SplitToChosung(string text)
	{
		var result = new StringBuilder();
		foreach (var character in text)
		{
			if (IsKoreanCharacrer(character))
			{
				var split = SplitCharacter(character);
				result.Append(split[0]);
			}
			else result.Append(character);
		}
		return result.ToString();
    }
}
