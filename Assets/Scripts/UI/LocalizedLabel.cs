using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Text))]
public class LocalizedLabel : MonoBehaviour
{
    private string english;
    private Text label;
    public void SetKey(string value) { english = value; Refresh(); }
    private void OnEnable() { GamePreferences.LanguageChanged += Refresh; Refresh(); }
    private void OnDisable() { GamePreferences.LanguageChanged -= Refresh; }
    private void Refresh()
    {
        if (label == null) label = GetComponent<Text>();
        if (english != null) label.text = GameText.T(english);
    }
}
