using KModkit;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Rnd = UnityEngine.Random;

public class EavesdroppingScript : MonoBehaviour
{

    static int _moduleIdCounter = 1;
    int _moduleID = 0;

    public KMBombModule Module;
    public KMBombInfo Bomb;
    public KMAudio Audio;
    public KMSelectable[] Selectables;
    public Light[] Lights;

    private KMAudio.KMAudioRef[] Sounds = new KMAudio.KMAudioRef[2];
    private Coroutine[] Running;
    private List<int> ShuffledDigits = new List<int>() { 1, 2, 3, 4, 5, 6, 7, 8, 9 };
    private List<int> ShuffledDigits2 = new List<int>() { 0, 1, 2, 3, 4, 5, 6, 7, 8 };
    private int[] Digits = new int[9];
    private int[] Solution = new int[3];
    private List<int> Specials = new List<int>();
    private int[] SpecialDigits = new int[3];
    private int Hovering = -1;
    private int Playing, PressCount;
    private string[] SoundNames = { "laptop whirr", "laptop whirr alt", "print" };
    private bool[] States = new bool[9];
    private bool Active, Solved;

    enum SoundNum
    {
        LaptopWhirr,
        LaptopWhirrAlt,
        Print
    }

    void Awake()
    {
        _moduleID = _moduleIdCounter++;
        Calculate();
        for (int i = 0; i < 9; i++)
        {
            int x = i;
            Selectables[x].OnInteract += delegate { Press(x); return false; };
            Selectables[x].OnHighlight += delegate { Hovering = x; Hover(); };
            Selectables[x].OnHighlightEnded += delegate { Hovering = -1; Unhover(); };
        }
        Module.OnActivate += delegate { Active = true; for (int i = 0; i < 9; i++) StartCoroutine(Laptop(i)); for (int i = 0; i < 9; i++) StartCoroutine(LaptopSpecial(i)); };
        Bomb.OnBombExploded += delegate { try { Sounds[0].StopSound(); } catch { } };
        Bomb.OnBombSolved += delegate { try { Sounds[0].StopSound(); } catch { } };
    }

    void Start()
    {
        float scalar = transform.lossyScale.x;
        for (var i = 0; i < Lights.Length; i++)
        {
            Lights[i].range *= scalar;
            Lights[i].intensity = 0;
        }
    }

    void Calculate()
    {
        ShuffledDigits.Shuffle();
        for (int i = 0; i < 9; i++)
            Digits[i] = ShuffledDigits[i];
        ShuffledDigits.Shuffle();
        for (int i = 0; i < 3; i++)
            SpecialDigits[i] = ShuffledDigits[i];
        ShuffledDigits2.Shuffle();
        for (int i = 0; i < 3; i++)
            Specials.Add(ShuffledDigits2[i]);
        Specials.Sort();
        Debug.LogFormat("[Eavesdropping #{0}] The referring digits for each sector are, in reading order, {1}.", _moduleID, Digits.Join(", ").Substring(0, Digits.Join(", ").Length - 3) + " and " + Digits[8].ToString());
        Debug.LogFormat("[Eavesdropping #{0}] The three special sectors are, in reading order, sectors {1}.", _moduleID, (Specials[0] + 1).ToString() + ", " + (Specials[1] + 1).ToString() + " and " + (Specials[2] + 1).ToString());
        Debug.LogFormat("[Eavesdropping #{0}] The quantity digits for each of these sectors are {1}, respectively.", _moduleID, SpecialDigits[0] + ", " + SpecialDigits[1] + " and " + SpecialDigits[2]);

        for (int i = 0; i < 3; i++)
        {
            int pos = Specials[i];
            for (int j = 0; j < SpecialDigits[i]; j++)
                pos = Digits[pos] - 1;
            Solution[i] = pos;
        }

        Debug.LogFormat("[Eavesdropping #{0}] The three sectors which need to be touched are sectors {1}.", _moduleID, (Solution[0] + 1).ToString() + ", " + (Solution[1] + 1).ToString() + " and " + (Solution[2] + 1).ToString());

    }

    void Press(int pos)
    {
        Audio.PlaySoundAtTransform("press", Selectables[pos].transform);
        Selectables[pos].AddInteractionPunch(0.5f);
        if (!Solved)
        {
            if (pos == Solution[PressCount])
            {
                Lights[PressCount].intensity = 3;
                PressCount++;
                Debug.LogFormat("[Eavesdropping #{0}] You touched sector {1}, which was correct.", _moduleID, pos + 1);
            }
            else
            {
                Module.HandleStrike();
                Debug.LogFormat("[Eavesdropping #{0}] You touched sector {1}, which was incorrect. Strike!", _moduleID, pos + 1);
            }
            if (PressCount == 3)
            {
                PressCount = 0;
                try
                {
                    Sounds[0].StopSound();
                }
                catch { }
                try
                {
                    Sounds[1].StopSound();
                }
                catch { }
                StartCoroutine(Solve());
                Solved = true;
                Debug.LogFormat("[Eavesdropping #{0}] Module solved!", _moduleID, pos + 1);
            }
        }
    }

    void Hover()
    {
        if (Active && !Solved)
            if (Hovering != -1)
                Sounds[0] = Audio.PlaySoundAtTransformWithRef(SoundNames[(int)SoundNum.LaptopWhirrAlt], Selectables[Hovering].transform);
    }

    void Unhover()
    {
        try
        {
            Sounds[0].StopSound();
        }
        catch { }
        try
        {
            Sounds[1].StopSound();
        }
        catch { }
    }

    void ChangeSounds(int pos)
    {
        try
        {
            Sounds[0].StopSound();
        }
        catch { }
        if (States[pos])
            Sounds[0] = Audio.PlaySoundAtTransformWithRef(SoundNames[(int)SoundNum.LaptopWhirrAlt], Selectables[Hovering].transform);
        else
            Sounds[0] = Audio.PlaySoundAtTransformWithRef(SoundNames[(int)SoundNum.LaptopWhirr], Selectables[Hovering].transform);
    }

    private IEnumerator Solve(float duration = 0.4f)
    {
        Module.HandlePass();
        Audio.PlaySoundAtTransform("solve", Selectables[4].transform);
        float timer = 0f;
        for (int i = 0; i < 3; i++)
        {
            while (timer < duration)
            {
                yield return null;
                timer += Time.deltaTime;
                Lights[i].intensity = Easing.InOutSine(timer, 3f, 0f, duration);
            }
            timer = 0;
        }
        for (int i = 0; i < 3; i++)
            Lights[i].intensity = 0;
    }

    private IEnumerator Laptop(int pos)
    {
        while (!Solved)
        {
            if (int.Parse(Mathf.Floor(Bomb.GetTime()).ToString().Last().ToString()) == Digits[pos])
            {
                yield return new WaitWhile(() => int.Parse(Mathf.Floor(Bomb.GetTime()).ToString().Last().ToString()) == Digits[pos]);
                float timer = 0;
                while (timer < Rnd.Range(0.1f, 0.3f))
                {
                    yield return null;
                    timer += Time.deltaTime;
                }
            }
            else
            {
                States[pos] = !States[pos];
                if (Hovering == pos)
                    ChangeSounds(pos);
                float timer = 0;
                while (timer < Rnd.Range(0.1f, 0.3f))
                {
                    yield return null;
                    timer += Time.deltaTime;
                }
            }
        }
    }

    private IEnumerator LaptopSpecial(int pos)
    {
        if (Specials.Contains(pos))
            while (!Solved)
            {
                if (int.Parse(Mathf.Floor(Bomb.GetTime()).ToString().Last().ToString()) == SpecialDigits[Array.IndexOf(Specials.ToArray(), pos)])
                {
                    if (Hovering == pos)
                        Sounds[1] = Audio.HandlePlaySoundAtTransformWithRef(SoundNames[(int)SoundNum.Print], Selectables[Hovering].transform, false);
                    yield return new WaitWhile(() => Mathf.Floor(Bomb.GetTime() % 10) == SpecialDigits[Array.IndexOf(Specials.ToArray(), pos)]);
                }
                else
                {
                    try
                    {
                        Sounds[1].StopSound();
                    }
                    catch { }
                    yield return new WaitWhile(() => Mathf.Floor(Bomb.GetTime() % 10) != SpecialDigits[Array.IndexOf(Specials.ToArray(), pos)]);
                }
            }
    }

#pragma warning disable 414
    private string TwitchHelpMessage = "Use '!{0} 1 2 3' to touch sectors 1, 2 and 3 in order. Use '!{0} hover 1 2 3' to hover over sectors 1, 2 and 3, each for 10 seconds with a pause of half a second between them.";
#pragma warning restore 414

    IEnumerator ProcessTwitchCommand(string command)
    {
        string[] commandArray = command.Split(' ');
        int outInt = 0;
        for (int i = 0; i < commandArray.Length; i++)
        {
            if (int.TryParse(commandArray[i], out outInt))
                if (int.Parse(commandArray[i]) <= -1 && int.Parse(commandArray[i]) >= 9)
                {
                    yield return "sendtochaterror Invalid command.";
                    yield break;
                }
            else if (commandArray[i] == "hover" && i == 0)
            {
                yield return "sendtochaterror Invalid command.";
                yield break;
            }
        }
        yield return null;
        if (commandArray[0] == "hover")
        {
            for (int i = 1; i < commandArray.Length; i++)
            {
                Selectables[int.Parse(commandArray[i]) - 1].OnHighlight();
                float timer = 0;
                while (timer < 10)
                {
                    yield return "trycancel Hover command cancelled (Eavesdropping).";
                    timer += Time.deltaTime;
                }
                Selectables[int.Parse(commandArray[i]) - 1].OnHighlightEnded();
                timer = 0;
                while (timer < 0.5f)
                {
                    yield return "trycancel Hover command cancelled (Eavesdropping).";
                    timer += Time.deltaTime;
                }
            }
        }
        else
        {
            for (int i = 0; i < commandArray.Length; i++)
            {
                Selectables[int.Parse(commandArray[i]) - 1].OnInteract();
                float timer = 0;
                while (timer < 0.1f)
                {
                    yield return null;
                    timer += Time.deltaTime;
                }
            }
        }
    }

    IEnumerator TwitchHandleForcedSolve()
    {
        for (int i = 0; i < 3; i++)
        {
            if (PressCount <= i)
            {
                Selectables[Solution[i]].OnInteract();
                float timer = 0;
                while (timer < 0.05f)
                {
                    yield return null;
                    timer += Time.deltaTime;
                }
            }
        }
    }
}