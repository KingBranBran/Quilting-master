using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;

public class QuiltingScript : MonoBehaviour {
    public KMBombModule module;
    public KMBombInfo info;
    public KMAudio audio;
    public KMSelectable[] buttons;
    public KMSelectable[] pieceButtons;
    public KMSelectable submit;
    public Material[] materials;
    public Material quilt;
    public GameObject back;
    private string[] submitedPuzzle;
    private string[] submitedSolution;
    private string[,] puzzleList;
    private string[,] puzzleSolutions;
    private string[] currentPuzzle;
    private string[] puzzle;
    private string[] solution;
    private int correctButton;
    private int pieceSelected = 5; // When you see 'pieceSelected = 5', it means that no button is selected.
    private bool modulePass;
    private string serialNumber;
    public bool editorMode; // Used for making puzzles.
    private bool locked = true;
    public int puzzleNum;


    //-----------------------------
    // Failed attempt at making a unique puzzle generator. Anyone reading this can try making a unique puzzle generator and I will implement it. Until then, I will have pre-made puzzles.
    //-----------------------------

    void Start () {

        serialNumber = KMBombInfoExtensions.GetSerialNumber(info);
        module.OnActivate += ActivateModule;
        
        // First # is button number, Second # is label of button. Label '10' is a blank square.
        puzzleList = new string[,]
        {
            {"3 2", "11 2", "14 1", "15 0", "21 1"},
            {"1 2", "9 2", "12 4", "15 2", "24 1"},
            {"1 2", "4 1", "20 1", "24 2", "x"},
            {"1 2", "4 2", "23 2", "24 0", "x"},
            {"10 3", "19 1", "22 10", "23 1", "x"},
            {"0 2", "3 2", "14 2", "20 10", "22 1"},
            {"0 2", "12 2", "14 10", "22 10", "x"},
            {"0 2", "4 0", "20 0", "24 2", "x"},
            {"2 3", "14 3", "20 2", "x", "x"},
            {"4 2", "10 10", "12 3", "24 10", "x"},
            {"0 10", "2 0", "7 1", "12 3", "20 1"},
        };

        //The space after each index is nessisary.
        //{" ", " ", " ", " "},
        puzzleSolutions = new string[,]
        {
            {"1 17 ", "2 8 18 ", "6 12 22 ", "13 23 "},
            {"2 5 13 16 ", "3 6 14 17 ", "7 10 18 21 ", "8 11 19 22 "},
            {"2 6 10 13 17 ", "3 14 ", "15 22 ", "8 12 16 19 23 "},
            {"2 6 10 ", "3 9 ", "15 21 ", "14 18 22 "},
            {"0 3 15 ", "1 4 7 13 16 ", "5 8 11 17 20 ", "9 18 21 "},
            {"1 5 18 " , "2 8 19 " , "10 16 23 " , "13 17 24 "},
            {"1 3 5 15 18 ", "2 4 16 19 ", "8 10 20 23 ", "7 9 11 21 24 "},
            {"1 5 ", "2 8 14 ", "10 16 22 ", "19 23 "},
            {"0 3 7 10 18 ", "1 4 11 17 19 ", "5 12 15 21 23 ", "6 9 13 22 24 "},
            {"0 2 15 17 ", "1 3 9 16 18 ", "5 7 13 20 22 ", "6 14 21 23 "},
            {"5 13 16 ", "6 14 17 ", "10 18 21 ", "11 19 22 "},
        };

        submitedSolution = new string[4];
        currentPuzzle = new string[25] { "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "" };

        submit.OnInteract += SubmitPressed();
        
        for (int i = 0; i < buttons.Length; i++)
        {
            buttons[i].OnInteract += ButtonPressed(i);
        }

        for (int i = 0; i < pieceButtons.Length; i++)
        {
            pieceButtons[i].OnInteract += PieceButtonPressed(i);
        }
    }


    private KMSelectable.OnInteractHandler PieceButtonPressed(int button)
    {
        return delegate
        {
            audio.PlaySoundAtTransform("LowTick", module.transform);
            if (modulePass || locked) { return false; }
            pieceSelected = button == pieceSelected ? 5 : button;
            for (int i = 0; i < pieceButtons.Length; i++)
            {
                pieceButtons[i].GetComponent<Renderer>().material = i == pieceSelected ? materials[i + 12] : materials[i + 5];
            }
            return false;
        };
    }

    private KMSelectable.OnInteractHandler ButtonPressed(int button)
    {
        return delegate
        {
            audio.PlaySoundAtTransform("HighTick", module.transform);
            if (locked)
            {
                if (correctButton == button)
                {
                    locked = false;
                    StartPuzzle();
                }
                else
                {
                    module.HandleStrike();
                }
                return false;
            }
            if (modulePass == true) { return false; }
            if (ButtonIsHint(button) && !editorMode) { return false; }
            if (editorMode && pieceSelected == 5)
            {
                if ( new[] {"TL", "TR", "BL", "BR", "Note"}.Any(ix => currentPuzzle[button] == ix))
                {
                    currentPuzzle[button] = "";
                    buttons[button].GetComponent<Renderer>().material = materials[11];
                }
                else
                {
                    int mat = NextEditPiece(button);
                    buttons[button].GetComponent<Renderer>().material = materials[mat];
                }
            }
            else
            {
                if (pieceSelected == 5)
                {
                    currentPuzzle[button] = "";
                    buttons[button].GetComponent<Renderer>().material = materials[11];
                    return false;
                }


                buttons[button].GetComponent<Renderer>().material = materials[pieceSelected + 5];

                if (pieceSelected == 1)
                {
                    currentPuzzle[button] = "TL";
                }
                else if (pieceSelected == 2)
                {
                    currentPuzzle[button] = "TR";
                }
                else if (pieceSelected == 3)
                {
                    currentPuzzle[button] = "BL";
                }
                else if (pieceSelected == 4)
                {
                    currentPuzzle[button] = "BR";
                }
                else if (pieceSelected == 0)
                {
                    currentPuzzle[button] = "Note";
                }

                Debug.Log("Button " + button + ": " + currentPuzzle[button]);
                return false;
            }
            return false;
        };
    }

    private int NextEditPiece(int button)
    {
        int num = currentPuzzle[button] == "" ? 10 : Int32.Parse(currentPuzzle[button]) + 1;
        if (num == 5) {
            num = 11;
            currentPuzzle[button] = "";
        }
        else
        {
            if (num == 11)
            {
                num = 0;
            }
            currentPuzzle[button] = "" + num;
        }
     
        return num;
    }

    private bool ButtonIsHint(int button)
    {
        foreach (string hint in puzzle) {
            if (hint != " ")
            {
                var hints = hint.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                string buttonString = button.ToString();
                if (buttonString == hints[0])
                {
                    return true;
                }
            }
        }
        return false;
    }

    private KMSelectable.OnInteractHandler SubmitPressed()
    {
        return delegate
        {
            audio.PlaySoundAtTransform("HighTick", module.transform);
            int puzzleButtonCounter = 0; 
            if (modulePass || locked) { return false; }

            submitedPuzzle = new string[5] { "", "", "", "", "" };
            submitedSolution = new string[4] { "", "", "", "" };
            for (int i = 0; i < currentPuzzle.Length; i++)
            {
                string j = i.ToString();
                if (currentPuzzle[i] == "TL")
                {
                    submitedSolution[0] += j + " ";
                }
                else if (currentPuzzle[i] == "TR")
                {
                    submitedSolution[1] += j + " ";
                }
                else if (currentPuzzle[i] == "BL")
                {
                    submitedSolution[2] += j + " ";
                }
                else if (currentPuzzle[i] == "BR")
                {
                    submitedSolution[3] += j + " ";
                }
                else if (currentPuzzle[i] == "10")
                {
                    submitedPuzzle[puzzleButtonCounter] = j + " " + "10";
                    puzzleButtonCounter++;
                }
                else if (currentPuzzle[i] == "0")
                {
                    submitedPuzzle[puzzleButtonCounter] = j + " " + "0";
                    puzzleButtonCounter++;
                }
                else if (currentPuzzle[i] == "1")
                {
                    submitedPuzzle[puzzleButtonCounter] = j + " " + "1";
                    puzzleButtonCounter++;
                }
                else if (currentPuzzle[i] == "2")
                {
                    submitedPuzzle[puzzleButtonCounter] = j + " " + "2";
                    puzzleButtonCounter++;
                }
                else if (currentPuzzle[i] == "3")
                {
                    submitedPuzzle[puzzleButtonCounter] = j + " " + "3";
                    puzzleButtonCounter++;
                }
                else if (currentPuzzle[i] == "4")
                {
                    submitedPuzzle[puzzleButtonCounter] = j + " " + "4";
                    puzzleButtonCounter++;
                }
            }
            while (puzzleButtonCounter < 5)
            {
                submitedPuzzle[puzzleButtonCounter] = "x";
                puzzleButtonCounter++;
            }
            if (editorMode)
            {
                Debug.Log("Puzzle: {\"" + submitedPuzzle[0] + "\", \"" + submitedPuzzle[1] + "\", \"" + submitedPuzzle[2] + "\", \"" + submitedPuzzle[3] + "\", \"" + submitedPuzzle[4] + "\"},");
                Debug.Log("Solution: {\"" + submitedSolution[0] + "\", \"" + submitedSolution[1] + "\", \"" + submitedSolution[2] + "\", \"" + submitedSolution[3] + "\"},");
            }
            if (solution[0] == submitedSolution[0] && solution[1] == submitedSolution[1] && solution[2] == submitedSolution[2] && solution[3] == submitedSolution[3] && !editorMode)
            {
                ModuleSolved();
                
            }
            else if (!editorMode)
            {
                module.HandleStrike();
            }

            return false;
        };
        
    }

    private void ModuleSolved()
    {
        modulePass = true;
        audio.PlaySoundAtTransform("Harp", module.transform);
        back.GetComponent<Renderer>().material = quilt;
        back.transform.localPosition = new Vector3(0.00017f, 0.0162f, 0.02105f);
        module.HandlePass();
    }

    private void ActivateModule()
    {
        correctButton = FindSolutionButton();
    }

    private int FindSolutionButton()
    {
        int coord_1 = (char.ToUpper(serialNumber[3]) - 64) % 5;
        coord_1 = coord_1 == 0 ? 5 : coord_1;
        int coord_2 = (char.ToUpper(serialNumber[4]) - 64) % 5;
        coord_2 = coord_2 == 0 ? 5 : coord_2;
        coord_1 -= 1;
        coord_2 -= 1;
        Debug.LogFormat("Starting Coords: {0} {1}", coord_1 + 1, coord_2 + 1);
        return coord_2 * 5 + coord_1;
    }

    private void StartPuzzle()
    {
        PickPuzzle();
        DisplayPuzzle();
    }
    private void PickPuzzle()
    {
        int i = Random.Range(0, puzzleList.GetLength(1));
        if (puzzleNum != -1 && puzzleNum != -2) // -1: Blank for editor mode | -2: default (pick random)
        {
            i = puzzleNum;
        }
        else
        {
            if (puzzleNum == -1)
            {
                puzzle = new string[5] { " ", " ", " ", " ", " " };
                solution = new string[4] { "", "", "", "" };
                return;
            }
        }
        puzzle = new string[5] { puzzleList[i, 0], puzzleList[i, 1], puzzleList[i, 2], puzzleList[i, 3], puzzleList[i, 4]};
        solution = new string[4] { puzzleSolutions[i, 0], puzzleSolutions[i, 1], puzzleSolutions[i, 2], puzzleSolutions[i, 3]};
    }

    void Update () {
		
	}

    private void DisplayPuzzle()
    {
        foreach (string hint in puzzle)
        {
            if (hint == "x" || hint == " ") return;

            var hints = hint.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            int position = Int32.Parse(hints[0]);
            int number = Int32.Parse(hints[1]);   
            buttons[position].GetComponent<Renderer>().material = materials[number];
            currentPuzzle[position] = number + "";
        }
    }
}