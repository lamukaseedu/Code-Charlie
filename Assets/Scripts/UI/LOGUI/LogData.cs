using UnityEngine;

/*
 * Author: Andres Rondon-Villarmosa
 * Created: 10/08/2026
 */

[CreateAssetMenu(fileName = "NewLog", menuName = "Terminal Log")]
public class LogData : ScriptableObject
{
    public string subject;
    public string log;
    public string date;
    public string author;

    [TextArea(5, 15)]
    public string message;
}