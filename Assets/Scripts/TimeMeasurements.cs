using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public static class TimeMeasurements
{
    private static System.DateTime timeStampBeginning;

    /// <summary>
    /// Starts the time measuring:
    /// writes the start time and the name of the process to the txt file
    /// </summary>
    /// <param name="processName">Name of the process that is measured</param>
    public static void StartingMeasurement(string processName)
    {
        WriteToFile(""); // for an empty line before new measurement
        WriteToFile("----------Time measurement of process " + processName + "----------");

        timeStampBeginning = getCurrentTimeStamp();
        WriteToFile("Beginning: " + timeStampBeginning.ToString());
    }

    /// <summary>
    /// Ends the time measuring:
    /// Writes the end time and the time it took to the txt file
    /// </summary>
    public static void StoppingMeasurement()
    {
        System.DateTime timeStampEnding = getCurrentTimeStamp();
        WriteToFile("Ending: " + timeStampEnding.ToString());
        WriteToFile("Duration: " + (timeStampEnding - timeStampBeginning).ToString());
    }

    /// <summary>
    /// Writes the string to the txt-file
    /// </summary>
    /// <param name="textToWrite">String to write to the file</param>
    private static void WriteToFile(string textToWrite)
    {
        string filePath = Application.persistentDataPath + "/TimeMeasurement.txt";
        StreamWriter writer = File.AppendText(Application.persistentDataPath + "/TimeMeasurement.txt");

        writer.WriteLine(textToWrite);
        writer.Close();
    }

    /// <summary>
    /// Gets current time stamp
    /// </summary>
    /// <returns></returns>
    private static System.DateTime getCurrentTimeStamp()
    {
        return System.DateTime.Now;
    }
}
