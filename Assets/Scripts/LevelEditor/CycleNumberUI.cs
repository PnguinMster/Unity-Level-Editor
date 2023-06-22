using TMPro;
using UnityEngine.Events;
using UnityEngine;

public class CycleUI : MonoBehaviour
{
    [SerializeField] private int currentOption;
    [SerializeField] private int[] Options;
    [SerializeField] private TMP_Text textBox;
    [SerializeField] private UnityEvent<int> onNumberChange;

    private void Start() => UpdateTextBox();

    private void UpdateTextBox()
    {
        textBox.text = Options[currentOption].ToString();
        onNumberChange.Invoke(Options[currentOption]);
    }

    public void IncrementOption()
    {
        currentOption++;
        if (currentOption >= Options.Length)
            currentOption = 0;
        UpdateTextBox();
    }
    public void DecrementOption()
    {
        currentOption--;
        if (currentOption < 0)
            currentOption = Options.Length - 1;
        UpdateTextBox();
    }
}